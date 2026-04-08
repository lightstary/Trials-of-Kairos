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
    [SerializeField] private float flashDuration       = 0.15f;

    private const float FLASH_PEAK_ALPHA = 0.8f;

    public bool IsTransitioning { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        SetAlpha(fadeOverlay,  0f);
        SetAlpha(flashOverlay, 0f);
    }

    /// <summary>Fades to black, runs midpoint callback, fades back in.</summary>
    public void FadeTransition(Action onMidpoint, float duration = -1f)
    {
        if (IsTransitioning) return;
        StartCoroutine(FadeTransitionRoutine(onMidpoint, duration > 0f ? duration : defaultFadeDuration));
    }

    /// <summary>Fades out and loads a scene by name.</summary>
    public void FadeToScene(string sceneName, float duration = -1f)
        => FadeTransition(() => SceneManager.LoadScene(sceneName), duration);

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

    /// <summary>Sets the fade overlay to fully opaque black.</summary>
    public void SetBlack() => SetAlpha(fadeOverlay, 1f);

    /// <summary>Fades in from black.</summary>
    public void FadeIn(float duration = -1f, Action onComplete = null)
        => StartCoroutine(FadeAlphaRoutine(fadeOverlay, 1f, 0f,
            duration > 0f ? duration : defaultFadeDuration, onComplete));

    /// <summary>Fades out to black.</summary>
    public void FadeOut(float duration = -1f, Action onComplete = null)
        => StartCoroutine(FadeAlphaRoutine(fadeOverlay, 0f, 1f,
            duration > 0f ? duration : defaultFadeDuration, onComplete));

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
