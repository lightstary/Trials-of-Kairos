using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages all screen fade / flash transitions.
/// Requires two full-screen overlay Images wired in the Inspector.
/// </summary>
public class ScreenTransitionManager : MonoBehaviour
{
    public static ScreenTransitionManager Instance { get; private set; }

    [Header("Overlays")]
    [SerializeField] private Image fadeOverlay;
    [SerializeField] private Image flashOverlay;

    [Header("Timing")]
    [SerializeField] private float defaultFadeDuration = 0.5f;
    #pragma warning disable CS0414 // Reserved for Inspector configuration
    [SerializeField] private float flashDuration       = 0.15f;
    #pragma warning restore CS0414

    private const float FLASH_PEAK_ALPHA = 0.8f;

    // Cosmic fade palette — deep indigo base with purple/blue/gold shimmer
    private static readonly Color COSMIC_BASE   = new Color(0.08f, 0.04f, 0.18f, 1f);
    private static readonly Color COSMIC_PURPLE = new Color(0.25f, 0.08f, 0.40f, 1f);
    private static readonly Color COSMIC_BLUE   = new Color(0.06f, 0.10f, 0.32f, 1f);
    private static readonly Color COSMIC_GOLD   = new Color(0.85f, 0.70f, 0.25f, 1f);

    private Color _savedFadeColor = Color.black;

    public bool IsTransitioning { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        SetAlpha(fadeOverlay,  0f);
        SetAlpha(flashOverlay, 0f);
    }

    /// <summary>Cosmic fade to opaque, runs midpoint callback, cosmic fade back in.</summary>
    public void FadeTransition(Action onMidpoint, float duration = -1f)
    {
        if (IsTransitioning) return;
        float dur = duration > 0f ? duration : defaultFadeDuration;
        StartCoroutine(CosmicFadeTransitionRoutine(onMidpoint, dur));
    }

    /// <summary>Cosmic fade out and loads a scene by name.</summary>
    public void FadeToScene(string sceneName, float duration = -1f)
    {
        float dur = duration > 0f ? duration : defaultFadeDuration;
        CosmicFadeOut(dur, () => SceneManager.LoadScene(sceneName));
    }

    /// <summary>Gold radiance burst (win).</summary>
    public void GoldBurst(Action onComplete = null)
    {
        if (IsTransitioning) return;
        Color gold = TimeStateUIManager.Instance != null
            ? TimeStateUIManager.Instance.goldColor : new Color(1f, 0.843f, 0f);
        StartCoroutine(FlashRoutine(gold, 1.2f, onComplete));
    }

    /// <summary>Red fracture flash (game over).</summary>
    public void RedFracture(Action onComplete = null)
    {
        if (IsTransitioning) return;
        Color danger = TimeStateUIManager.Instance != null
            ? TimeStateUIManager.Instance.dangerColor : Color.red;
        StartCoroutine(FlashRoutine(danger, 0.8f, onComplete));
    }

    /// <summary>Sets the fade overlay to fully opaque cosmic base color.</summary>
    public void SetBlack()
    {
        if (fadeOverlay == null) return;
        Color c = COSMIC_BASE;
        c.a = 1f;
        fadeOverlay.color = c;
    }

    /// <summary>Fades in from cosmic overlay.</summary>
    public void FadeIn(float duration = -1f, Action onComplete = null)
    {
        float dur = duration > 0f ? duration : defaultFadeDuration;
        StartCoroutine(CosmicFadeRoutine(1f, 0f, dur, onComplete));
    }

    /// <summary>Fades out to cosmic overlay.</summary>
    public void FadeOut(float duration = -1f, Action onComplete = null)
    {
        float dur = duration > 0f ? duration : defaultFadeDuration;
        StartCoroutine(CosmicFadeRoutine(0f, 1f, dur, onComplete));
    }

    // ── Cosmic Transitions ──────────────────────────────────────────

    /// <summary>
    /// Fades out through a cosmic color wash (deep purple → indigo → gold shimmer).
    /// The overlay sweeps through cosmic hues instead of going to flat black.
    /// </summary>
    public void CosmicFadeOut(float duration = -1f, Action onComplete = null)
    {
        float dur = duration > 0f ? duration : defaultFadeDuration;
        StartCoroutine(CosmicFadeRoutine(0f, 1f, dur, onComplete));
    }

    /// <summary>Fades back in from the cosmic wash.</summary>
    public void CosmicFadeIn(float duration = -1f, Action onComplete = null)
    {
        float dur = duration > 0f ? duration : defaultFadeDuration;
        StartCoroutine(CosmicFadeRoutine(1f, 0f, dur, onComplete));
    }

    /// <summary>Cosmic transition: fade out, run midpoint callback, fade back in.</summary>
    public void CosmicFadeTransition(Action onMidpoint, float duration = -1f)
    {
        if (IsTransitioning) return;
        float dur = duration > 0f ? duration : defaultFadeDuration;
        StartCoroutine(CosmicFadeTransitionRoutine(onMidpoint, dur));
    }

    private IEnumerator CosmicFadeTransitionRoutine(Action onMidpoint, float duration)
    {
        IsTransitioning = true;
        float half = duration * 0.5f;
        yield return CosmicFadeCore(0f, 1f, half);
        onMidpoint?.Invoke();
        yield return new WaitForSecondsRealtime(0.1f);
        yield return CosmicFadeCore(1f, 0f, half);
        IsTransitioning = false;
    }

    private IEnumerator CosmicFadeRoutine(float from, float to, float dur, Action cb)
    {
        yield return CosmicFadeCore(from, to, dur);
        cb?.Invoke();
    }

    /// <summary>
    /// Core cosmic fade: sweeps the overlay through cosmic colors as alpha changes.
    /// Phase 0.0-0.3: deep purple sweep in
    /// Phase 0.3-0.6: shift to midnight blue  
    /// Phase 0.6-0.85: touch of gold shimmer
    /// Phase 0.85-1.0: settle to deep cosmic indigo
    /// </summary>
    private IEnumerator CosmicFadeCore(float fromAlpha, float toAlpha, float dur)
    {
        if (fadeOverlay == null) yield break;

        _savedFadeColor = fadeOverlay.color;
        float elapsed = 0f;

        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / dur);
            float alpha = Mathf.Lerp(fromAlpha, toAlpha, t);

            // Sweep through cosmic palette based on progress
            Color col;
            if (t < 0.3f)
            {
                // Deep purple sweep
                col = Color.Lerp(COSMIC_BASE, COSMIC_PURPLE, t / 0.3f);
            }
            else if (t < 0.6f)
            {
                // Shift to midnight blue
                col = Color.Lerp(COSMIC_PURPLE, COSMIC_BLUE, (t - 0.3f) / 0.3f);
            }
            else if (t < 0.85f)
            {
                // Touch of gold shimmer at peak
                float goldT = (t - 0.6f) / 0.25f;
                float goldAmount = Mathf.Sin(goldT * Mathf.PI) * 0.25f;
                col = Color.Lerp(COSMIC_BLUE, COSMIC_BASE, goldT);
                col = Color.Lerp(col, COSMIC_GOLD, goldAmount);
            }
            else
            {
                // Settle to deep indigo
                col = COSMIC_BASE;
            }

            col.a = alpha;
            fadeOverlay.color = col;
            yield return null;
        }

        Color final_col = COSMIC_BASE;
        final_col.a = toAlpha;
        fadeOverlay.color = final_col;
    }

    private IEnumerator FadeTransitionRoutine(Action onMidpoint, float duration)
    {
        IsTransitioning = true;
        float half = duration * 0.5f;
        yield return FadeAlpha(fadeOverlay, 0f, 1f, half);
        onMidpoint?.Invoke();
        yield return new WaitForSecondsRealtime(0.1f);
        yield return FadeAlpha(fadeOverlay, 1f, 0f, half);
        IsTransitioning = false;
    }

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

    private IEnumerator FadeAlphaRoutine(Image img, float from, float to, float dur, Action cb)
    {
        yield return FadeAlpha(img, from, to, dur);
        cb?.Invoke();
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
