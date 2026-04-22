using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Shows a "CHECKPOINT REACHED" text popup that fades in and out.
/// Uses unscaled time so it works even if time is paused.
/// Singleton — attach to the GameCanvas.
/// </summary>
public class CheckpointPopup : MonoBehaviour
{
    public static CheckpointPopup Instance { get; private set; }

    private const float FADE_IN_DURATION  = 0.4f;
    private const float HOLD_DURATION     = 1.2f;
    private const float FADE_OUT_DURATION = 0.6f;
    private const float GLOW_SPEED        = 4f;

    private static readonly Color TEXT_COLOR = new Color(0.961f, 0.784f, 0.259f, 1f);
    private static readonly Color SUB_COLOR  = new Color(0.91f, 0.918f, 0.965f, 0.6f);

    private CanvasGroup      _group;
    private TextMeshProUGUI  _mainText;
    private TextMeshProUGUI  _subText;
    private RectTransform    _container;
    private bool             _built;
    private Coroutine        _activeRoutine;

    void Awake()
    {
        Instance = this;
    }

    /// <summary>Triggers the checkpoint popup.</summary>
    public void Show()
    {
        if (!_built) Build();
        if (_activeRoutine != null) StopCoroutine(_activeRoutine);
        _activeRoutine = StartCoroutine(AnimatePopup());
    }

    private void Build()
    {
        _built = true;

        // Container
        GameObject container = new GameObject("CheckpointPopup");
        container.transform.SetParent(transform, false);
        _container = container.AddComponent<RectTransform>();
        _container.anchorMin = new Vector2(0.5f, 0.5f);
        _container.anchorMax = new Vector2(0.5f, 0.5f);
        _container.pivot     = new Vector2(0.5f, 0.5f);
        _container.sizeDelta = new Vector2(500f, 100f);
        _container.anchoredPosition = new Vector2(0f, 80f);

        _group = container.AddComponent<CanvasGroup>();
        _group.alpha = 0f;
        _group.interactable = false;
        _group.blocksRaycasts = false;

        // Main text
        GameObject mainGO = new GameObject("MainText");
        mainGO.transform.SetParent(container.transform, false);
        _mainText = mainGO.AddComponent<TextMeshProUGUI>();
        _mainText.text = "CHECKPOINT REACHED";
        _mainText.fontSize = 28f;
        _mainText.fontStyle = FontStyles.Bold;
        _mainText.characterSpacing = 8f;
        _mainText.alignment = TextAlignmentOptions.Center;
        _mainText.color = TEXT_COLOR;
        _mainText.raycastTarget = false;
        RectTransform mrt = mainGO.GetComponent<RectTransform>();
        mrt.anchorMin = new Vector2(0, 0.35f); mrt.anchorMax = Vector2.one;
        mrt.offsetMin = Vector2.zero; mrt.offsetMax = Vector2.zero;

        // Assign Cinzel font if available
        CinzelFontHelper.Apply(_mainText, true);

        // Subtle sub-line
        GameObject subGO = new GameObject("SubText");
        subGO.transform.SetParent(container.transform, false);
        _subText = subGO.AddComponent<TextMeshProUGUI>();
        _subText.text = "\u25C6";
        _subText.fontSize = 14f;
        _subText.alignment = TextAlignmentOptions.Center;
        _subText.color = SUB_COLOR;
        _subText.raycastTarget = false;
        CinzelFontHelper.Apply(_subText);
        RectTransform srt = subGO.GetComponent<RectTransform>();
        srt.anchorMin = Vector2.zero; srt.anchorMax = new Vector2(1, 0.35f);
        srt.offsetMin = Vector2.zero; srt.offsetMax = Vector2.zero;
    }

    private IEnumerator AnimatePopup()
    {
        _group.alpha = 0f;
        _container.localScale = Vector3.one * 0.9f;

        // Fade in
        float elapsed = 0f;
        while (elapsed < FADE_IN_DURATION)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / FADE_IN_DURATION);
            float ease = t * t * (3f - 2f * t);
            _group.alpha = ease;
            _container.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, ease);
            yield return null;
        }
        _group.alpha = 1f;
        _container.localScale = Vector3.one;

        // Hold with gold glow pulse
        elapsed = 0f;
        while (elapsed < HOLD_DURATION)
        {
            elapsed += Time.unscaledDeltaTime;
            float pulse = (Mathf.Sin(Time.unscaledTime * GLOW_SPEED) + 1f) * 0.5f;
            Color c = TEXT_COLOR;
            c.a = Mathf.Lerp(0.7f, 1f, pulse);
            if (_mainText != null) _mainText.color = c;
            yield return null;
        }

        // Fade out
        elapsed = 0f;
        while (elapsed < FADE_OUT_DURATION)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / FADE_OUT_DURATION);
            _group.alpha = 1f - t;
            _container.anchoredPosition = new Vector2(0f, 80f + t * 20f);
            yield return null;
        }
        _group.alpha = 0f;
        _container.anchoredPosition = new Vector2(0f, 80f);
        _activeRoutine = null;
    }
}
