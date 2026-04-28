using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages all screen transitions using a shimmer-wipe that matches
/// MenuShimmerController exactly: same speed (8s), same band width,
/// same 25-degree angle, same barely-there alpha.
///
/// How it works:
/// 1. Captures a screenshot of the current scene.
/// 2. Loads the new scene (or runs a midpoint action).
/// 3. Displays the screenshot as a full-screen RawImage overlay.
/// 4. The ShimmerWipe shader slowly dissolves the screenshot from
///    left to right, revealing the live new scene underneath.
/// </summary>
public class ScreenTransitionManager : MonoBehaviour
{
    public static ScreenTransitionManager Instance { get; private set; }

    [Header("Overlays")]
    [SerializeField] private Image fadeOverlay;
    [SerializeField] private Image flashOverlay;

    [Header("Timing")]
    #pragma warning disable CS0414
    [SerializeField] private float defaultFadeDuration = 0.5f;
    [SerializeField] private float flashDuration = 0.15f;
    #pragma warning restore CS0414

    private const float FLASH_PEAK_ALPHA = 0.8f;

    // ── Shimmer — matches MenuShimmerController exactly ─────────────
    /// <summary>Same as MenuShimmerController.SWEEP_DURATION.</summary>
    private const float SHIMMER_SWEEP_DUR = 8f;

    /// <summary>Same palette as MenuShimmerController.PALETTE.</summary>
    private static readonly Color[] SHIMMER_PALETTE = new Color[]
    {
        new Color(0.961f, 0.784f, 0.259f, 1f),   // Gold
        new Color(0.353f, 0.706f, 0.941f, 1f),   // Blue
        new Color(0.608f, 0.365f, 0.898f, 1f),   // Purple
        new Color(0.200f, 0.780f, 0.860f, 1f),   // Teal
    };

    private static readonly int PropProgress  = Shader.PropertyToID("_Progress");
    private static readonly int PropColor1    = Shader.PropertyToID("_ShimmerColor1");
    private static readonly int PropColor2    = Shader.PropertyToID("_ShimmerColor2");

    private int _shimmerColorIndex;

    // Dynamic shimmer overlay (RawImage showing the captured old scene)
    private RawImage _shimmerOverlay;
    private Material _shimmerMat;

    // ── Scene-load handoff ──────────────────────────────────────────
    /// <summary>Screenshot of the old scene. Survives scene load (not attached to any GO).</summary>
    private static Texture2D _pendingCapture;

    /// <summary>Signals the next scene to auto-reveal via shimmer wipe.</summary>
    private static bool _fadeInOnNextScene;

    /// <summary>Solid dark color shown briefly while the scene loads behind it.</summary>
    private static readonly Color SOLID_COVER = new Color(0.03f, 0.015f, 0.06f, 1f);

    // Blur removed — the menu shimmer has no blur, and neither do transitions.

    public bool IsTransitioning { get; private set; }

    /// <summary>True while the shimmer wipe is actively revealing a new scene.</summary>
    public bool IsRevealing { get; private set; }

    // ════════════════════════════════════════════════════════════════════
    //  LIFECYCLE
    // ════════════════════════════════════════════════════════════════════

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _shimmerColorIndex = UnityEngine.Random.Range(0, SHIMMER_PALETTE.Length);
        SetAlpha(flashOverlay, 0f);

        // Reset fadeOverlay to default (no custom material, transparent)
        if (fadeOverlay != null)
        {
            fadeOverlay.material = null;
            SetAlpha(fadeOverlay, 0f);
            fadeOverlay.raycastTarget = false;
        }

        if (_fadeInOnNextScene)
        {
            _fadeInOnNextScene = false;

            if (_pendingCapture != null)
            {
                // Old scene captured — cover with solid, then shimmer-reveal
                if (fadeOverlay != null)
                {
                    fadeOverlay.color = SOLID_COVER;
                    fadeOverlay.raycastTarget = true;
                }
                StartCoroutine(RevealWithCapture());
            }
            else
            {
                // No capture (fallback) — simple fade from solid cover
                if (fadeOverlay != null)
                {
                    fadeOverlay.color = SOLID_COVER;
                    fadeOverlay.raycastTarget = true;
                }
                StartCoroutine(SimpleFadeIn(0.8f));
            }
        }
    }

    /// <summary>
    /// Creates the shimmer overlay showing the old scene screenshot,
    /// hides the solid cover, then slowly wipes the screenshot away.
    /// </summary>
    private IEnumerator RevealWithCapture()
    {
        IsRevealing = true;
        yield return null; // Let scene Awake/Start finish

        CreateShimmerOverlay(_pendingCapture);

        // Shimmer overlay now covers everything with the old scene.
        // Hide the solid fadeOverlay so the live new scene is underneath.
        SetAlpha(fadeOverlay, 0f);
        fadeOverlay.raycastTarget = false;

        yield return AnimateShimmerWipe(SHIMMER_SWEEP_DUR);

        DestroyShimmerOverlay();
        ReleasePendingCapture();
        IsRevealing = false;
    }

    /// <summary>Fallback: simple alpha fade from solid to transparent.</summary>
    private IEnumerator SimpleFadeIn(float dur)
    {
        yield return null;
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            SetAlpha(fadeOverlay, 1f - Mathf.Clamp01(elapsed / dur));
            yield return null;
        }
        SetAlpha(fadeOverlay, 0f);
        fadeOverlay.raycastTarget = false;
    }

    // ════════════════════════════════════════════════════════════════════
    //  PUBLIC API — same signatures so all existing callers still work
    // ════════════════════════════════════════════════════════════════════

    /// <summary>Captures screen, runs midpoint, then shimmer-reveals the result.</summary>
    public void FadeTransition(Action onMidpoint, float duration = -1f)
    {
        if (IsTransitioning) return;
        StartCoroutine(ShimmerTransitionRoutine(onMidpoint));
    }

    /// <summary>Loads a scene with full dissolve + shimmer transition.</summary>
    public void FadeToScene(string sceneName, float duration = -1f)
    {
        StartCoroutine(DissolveAndLoadScene(sceneName));
    }

    /// <summary>Loads a scene with a quick capture (no dissolve). Use for same-level restarts.</summary>
    public void QuickReloadScene(string sceneName)
    {
        StartCoroutine(CaptureAndLoadScene(sceneName));
    }

    /// <summary>Gold radiance burst (win effect).</summary>
    public void GoldBurst(Action onComplete = null)
    {
        if (IsTransitioning) return;
        Color gold = TimeStateUIManager.Instance != null
            ? TimeStateUIManager.Instance.goldColor : new Color(1f, 0.843f, 0f);
        StartCoroutine(FlashRoutine(gold, 1.2f, onComplete));
    }

    /// <summary>Red fracture flash (game over effect).</summary>
    public void RedFracture(Action onComplete = null)
    {
        if (IsTransitioning) return;
        Color danger = TimeStateUIManager.Instance != null
            ? TimeStateUIManager.Instance.dangerColor : Color.red;
        StartCoroutine(FlashRoutine(danger, 0.8f, onComplete));
    }

    /// <summary>Covers the screen immediately with a solid dark overlay.</summary>
    public void SetBlack()
    {
        if (fadeOverlay == null) return;
        fadeOverlay.material = null;
        fadeOverlay.color = SOLID_COVER;
        fadeOverlay.raycastTarget = true;
    }

    /// <summary>Reveals the scene. Uses shimmer if a capture exists, otherwise fades.</summary>
    public void FadeIn(float duration = -1f, Action onComplete = null)
    {
        _fadeInOnNextScene = false;
        StartCoroutine(FadeInRoutine(onComplete));
    }

    /// <summary>Captures the screen, covers it, then calls the callback.</summary>
    public void FadeOut(float duration = -1f, Action onComplete = null)
    {
        StartCoroutine(CaptureAndCover(onComplete));
    }

    /// <summary>Alias for FadeOut — captures screen, covers, calls callback.</summary>
    public void CosmicFadeOut(float duration = -1f, Action onComplete = null)
    {
        StartCoroutine(CaptureAndCover(onComplete));
    }

    /// <summary>Alias for FadeIn — reveals the scene with shimmer.</summary>
    public void CosmicFadeIn(float duration = -1f, Action onComplete = null)
    {
        _fadeInOnNextScene = false;
        StartCoroutine(FadeInRoutine(onComplete));
    }

    /// <summary>Full shimmer transition: capture, midpoint, reveal.</summary>
    public void CosmicFadeTransition(Action onMidpoint, float duration = -1f)
    {
        if (IsTransitioning) return;
        StartCoroutine(ShimmerTransitionRoutine(onMidpoint));
    }

    // ════════════════════════════════════════════════════════════════════
    //  CORE ROUTINES
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Dissolves visible scene elements into sand, then captures the remaining
    /// background and loads the new scene. The new scene auto-reveals via shimmer wipe.
    /// </summary>
    private IEnumerator DissolveAndLoadScene(string sceneName)
    {
        if (fadeOverlay != null) fadeOverlay.raycastTarget = true;

        // Start music fade alongside the dissolve
        if (SoundManager.Instance != null)
            SoundManager.Instance.FadeMusicOut(2.5f);

        // Run sand dissolve sequence
        // The orchestrator is not destroyed manually — it gets destroyed
        // automatically when the scene unloads, ensuring all child
        // coroutines (element dissolves) run to completion.
        SandTransitionOrchestrator orchestrator = gameObject.AddComponent<SandTransitionOrchestrator>();
        yield return orchestrator.RunDissolve();

        // Capture the remaining background (elements are gone)
        yield return new WaitForEndOfFrame();
        ReleasePendingCapture();
        _pendingCapture = CaptureScreen();

        // Solid cover hides the gap between old scene unload and new scene render
        if (fadeOverlay != null)
        {
            fadeOverlay.material = null;
            fadeOverlay.color = SOLID_COVER;
        }
        _fadeInOnNextScene = true;

        // Tell the next scene's SoundManager to fade music in during the shimmer
        SoundManager.PendingMusicFadeIn = true;
        SoundManager.PendingFadeInDuration = SHIMMER_SWEEP_DUR;

        SceneManager.LoadScene(sceneName);
    }

    /// <summary>Captures the screen, loads a scene. New scene auto-reveals.</summary>
    private IEnumerator CaptureAndLoadScene(string sceneName)
    {
        if (fadeOverlay != null) fadeOverlay.raycastTarget = true;

        // Fade music out quickly before capture
        if (SoundManager.Instance != null)
            SoundManager.Instance.FadeMusicOut(0.4f);
        yield return new WaitForSecondsRealtime(0.4f);

        yield return new WaitForEndOfFrame();
        ReleasePendingCapture();
        _pendingCapture = CaptureScreen();

        // Solid cover hides the gap between old scene unload and new scene render
        if (fadeOverlay != null)
        {
            fadeOverlay.material = null;
            fadeOverlay.color = SOLID_COVER;
        }
        _fadeInOnNextScene = true;

        // Tell the next scene's SoundManager to fade music in during the shimmer
        SoundManager.PendingMusicFadeIn = true;
        SoundManager.PendingFadeInDuration = SHIMMER_SWEEP_DUR;

        SceneManager.LoadScene(sceneName);
    }

    /// <summary>Captures the screen, covers it, calls callback (usually triggers a scene load).</summary>
    private IEnumerator CaptureAndCover(Action onComplete)
    {
        if (fadeOverlay != null) fadeOverlay.raycastTarget = true;

        // Fade music out quickly before capture
        if (SoundManager.Instance != null)
            SoundManager.Instance.FadeMusicOut(0.4f);
        yield return new WaitForSecondsRealtime(0.4f);

        yield return new WaitForEndOfFrame();
        ReleasePendingCapture();
        _pendingCapture = CaptureScreen();

        if (fadeOverlay != null)
        {
            fadeOverlay.material = null;
            fadeOverlay.color = SOLID_COVER;
        }
        _fadeInOnNextScene = true;

        // Tell the next scene's SoundManager to fade music in during the shimmer
        SoundManager.PendingMusicFadeIn = true;
        SoundManager.PendingFadeInDuration = SHIMMER_SWEEP_DUR;

        onComplete?.Invoke();
    }

    /// <summary>In-scene transition: capture, run midpoint, shimmer-reveal the result.</summary>
    private IEnumerator ShimmerTransitionRoutine(Action onMidpoint)
    {
        IsTransitioning = true;
        if (fadeOverlay != null) fadeOverlay.raycastTarget = true;

        yield return new WaitForEndOfFrame();
        Texture2D capture = CaptureScreen();

        // Run midpoint action (rearranges UI, changes state, etc.)
        onMidpoint?.Invoke();
        yield return null; // Let changes render

        // Old scene screenshot on top, then wipe it away
        CreateShimmerOverlay(capture);

        if (fadeOverlay != null) fadeOverlay.raycastTarget = false;
        yield return AnimateShimmerWipe(SHIMMER_SWEEP_DUR);

        DestroyShimmerOverlay();
        Destroy(capture);

        IsTransitioning = false;
    }

    /// <summary>Reveals the scene — shimmer wipe if capture exists, else simple fade.</summary>
    private IEnumerator FadeInRoutine(Action onComplete)
    {
        IsRevealing = true;

        if (_pendingCapture != null)
        {
            CreateShimmerOverlay(_pendingCapture);
            if (fadeOverlay != null)
            {
                SetAlpha(fadeOverlay, 0f);
                fadeOverlay.raycastTarget = false;
            }

            yield return AnimateShimmerWipe(SHIMMER_SWEEP_DUR);

            DestroyShimmerOverlay();
            ReleasePendingCapture();
        }
        else
        {
            yield return SimpleFadeIn(0.8f);
        }

        IsRevealing = false;
        onComplete?.Invoke();
    }

    // ════════════════════════════════════════════════════════════════════
    //  SHIMMER OVERLAY
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a full-screen RawImage showing the captured old scene
    /// with the ShimmerWipe shader. Renders on top of everything.
    /// </summary>
    private void CreateShimmerOverlay(Texture2D texture)
    {
        if (_shimmerOverlay != null) DestroyShimmerOverlay();

        Transform parent = fadeOverlay.transform.parent;
        GameObject go = new GameObject("ShimmerWipeOverlay");
        go.transform.SetParent(parent, false);
        go.transform.SetAsLastSibling();

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        _shimmerOverlay = go.AddComponent<RawImage>();
        _shimmerOverlay.raycastTarget = false;
        _shimmerOverlay.texture = texture;

        Shader shader = Shader.Find("UI/ShimmerWipe");
        if (shader != null)
        {
            _shimmerMat = new Material(shader);
            _shimmerMat.SetFloat(PropProgress, 0f);
            CycleShimmerColors();
            _shimmerOverlay.material = _shimmerMat;
        }
    }

    /// <summary>
    /// Animates the shimmer wipe from 0 (old scene fully visible) to 1
    /// (old scene gone, new scene fully revealed). Linear progress.
    /// </summary>
    private IEnumerator AnimateShimmerWipe(float duration)
    {
        if (_shimmerMat == null)
        {
            // Shader missing — fall back to simple alpha fade on the overlay
            if (_shimmerOverlay != null)
            {
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    _shimmerOverlay.color = new Color(1f, 1f, 1f, 1f - t);
                    yield return null;
                }
            }
            yield break;
        }

        _shimmerMat.SetFloat(PropProgress, 0f);

        float e = 0f;
        while (e < duration)
        {
            e += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(e / duration);
            _shimmerMat.SetFloat(PropProgress, t);
            yield return null;
        }

        _shimmerMat.SetFloat(PropProgress, 1f);
    }

    /// <summary>Destroys the shimmer overlay and its material.</summary>
    private void DestroyShimmerOverlay()
    {
        if (_shimmerOverlay != null)
        {
            Destroy(_shimmerOverlay.gameObject);
            _shimmerOverlay = null;
        }
        if (_shimmerMat != null)
        {
            Destroy(_shimmerMat);
            _shimmerMat = null;
        }
    }

    /// <summary>Picks the next color pair from the palette.</summary>
    private void CycleShimmerColors()
    {
        if (_shimmerMat == null) return;
        _shimmerColorIndex = (_shimmerColorIndex + 1) % SHIMMER_PALETTE.Length;
        int next = (_shimmerColorIndex + 1) % SHIMMER_PALETTE.Length;
        _shimmerMat.SetColor(PropColor1, SHIMMER_PALETTE[_shimmerColorIndex]);
        _shimmerMat.SetColor(PropColor2, SHIMMER_PALETTE[next]);
    }

    /// <summary>Releases the static captured screenshot to free memory.</summary>
    private static void ReleasePendingCapture()
    {
        if (_pendingCapture != null)
        {
            Destroy(_pendingCapture);
            _pendingCapture = null;
        }
    }

    /// <summary>
    /// Captures the current screen using ReadPixels instead of
    /// CaptureScreenshotAsTexture to avoid a known Unity bug where
    /// colors are brighter/oversaturated in Linear color space projects.
    /// </summary>
    private static Texture2D CaptureScreen()
    {
        Texture2D tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0, false);
        tex.Apply(false, false);
        return tex;
    }

    // ════════════════════════════════════════════════════════════════════
    //  FLASH HELPERS (GoldBurst / RedFracture — unchanged)
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator FlashRoutine(Color color, float duration, Action onComplete)
    {
        IsTransitioning = true;
        if (flashOverlay != null) { color.a = 0f; flashOverlay.color = color; }
        float half = duration * 0.5f;
        yield return FadeAlpha(flashOverlay, 0f, FLASH_PEAK_ALPHA, half);
        yield return FadeAlpha(flashOverlay, FLASH_PEAK_ALPHA, 0f, half);
        IsTransitioning = false;
        onComplete?.Invoke();
    }

    private IEnumerator FadeAlpha(Image img, float from, float to, float dur)
    {
        if (img == null) yield break;
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            SetAlpha(img, Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / dur)));
            yield return null;
        }
        SetAlpha(img, to);
    }

    private static void SetAlpha(Image img, float a)
    {
        if (img == null) return;
        Color c = img.color; c.a = a; img.color = c;
    }
}
