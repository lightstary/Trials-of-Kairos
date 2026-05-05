using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

// Full-screen video cutscene player with hold-to-skip and sand dissolve transitions.
public class CutscenePlayer : MonoBehaviour
{
    public static CutscenePlayer Instance { get; private set; }

    private VideoPlayer _videoPlayer;
    private RawImage _videoDisplay;
    private RenderTexture _renderTexture;
    private Canvas _canvas;
    private GameObject _rootGO;
    private CanvasGroup _canvasGroup;

    // Skip prompt elements
    private Image _iconImage;
    private Image _holdRing;
    private TextMeshProUGUI _skipLabel;
    private GameObject _skipContainer;

    private Action _onComplete;
    private bool _isPlaying;
    private bool _skipped;
    private float _holdTime;
    private bool _videoFinished;

    private const float HOLD_TO_SKIP_DURATION = 1.2f;
    private const float FADE_IN_DURATION = 0.6f;
    private const float FADE_OUT_DURATION = 0.6f;
    private const int SORT_ORDER = 999;
    private const float ICON_SIZE = 64f;
    private const float RING_SIZE = 80f;

    // Sand grain constants for dissolve transitions
    private const int   GRAIN_COUNT        = 60;
    private const float GRAIN_SIZE_MIN     = 2f;
    private const float GRAIN_SIZE_MAX     = 6f;
    private const float GRAIN_DRIFT_MIN    = 50f;
    private const float GRAIN_DRIFT_MAX    = 120f;
    private const float GRAIN_DRIFT_X      = 30f;
    private const float GRAIN_LIFETIME_MIN = 0.4f;
    private const float GRAIN_LIFETIME_MAX = 0.9f;

    private static readonly Color RING_BG_COLOR = new Color(1f, 1f, 1f, 0.15f);
    private static readonly Color RING_FILL_COLOR = new Color(0.961f, 0.784f, 0.259f, 0.9f);
    private static readonly Color LABEL_COLOR = new Color(0.91f, 0.918f, 0.965f, 0.75f);
    private static readonly Color SAND_DARK  = new Color(0.76f, 0.60f, 0.32f, 1f);
    private static readonly Color SAND_LIGHT = new Color(0.95f, 0.85f, 0.55f, 1f);

    /// <summary>
    /// Creates a CutscenePlayer, plays the given video clip, and invokes onComplete when done.
    /// </summary>
    public static void Play(VideoClip clip, Action onComplete)
    {
        if (clip == null)
        {
            Debug.LogWarning("[CutscenePlayer] No video clip provided, skipping cutscene.");
            onComplete?.Invoke();
            return;
        }

        GameObject go = new GameObject("[CutscenePlayer]");
        DontDestroyOnLoad(go);
        CutscenePlayer player = go.AddComponent<CutscenePlayer>();
        player.StartPlayback(clip, onComplete);
    }

    private void StartPlayback(VideoClip clip, Action onComplete)
    {
        Instance = this;
        _onComplete = onComplete;
        _isPlaying = true;
        _skipped = false;
        _holdTime = 0f;
        _videoFinished = false;

        BuildUI();
        SetupVideoPlayer(clip);
        StartCoroutine(PlaybackRoutine());
    }

    private void BuildUI()
    {
        // Root canvas - renders on top of everything
        _rootGO = new GameObject("CutsceneCanvas");
        _rootGO.transform.SetParent(transform, false);

        _canvas = _rootGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = SORT_ORDER;

        CanvasScaler scaler = _rootGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        _rootGO.AddComponent<GraphicRaycaster>();

        _canvasGroup = _rootGO.AddComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;

        // Black background
        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(_rootGO.transform, false);
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = Color.black;
        bgImg.raycastTarget = true;
        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        // Video display
        GameObject displayGO = new GameObject("VideoDisplay");
        displayGO.transform.SetParent(_rootGO.transform, false);
        _videoDisplay = displayGO.AddComponent<RawImage>();
        _videoDisplay.color = Color.white;
        _videoDisplay.raycastTarget = false;
        RectTransform displayRT = displayGO.GetComponent<RectTransform>();
        displayRT.anchorMin = Vector2.zero;
        displayRT.anchorMax = Vector2.one;
        displayRT.offsetMin = Vector2.zero;
        displayRT.offsetMax = Vector2.zero;

        // Skip prompt container (bottom-center, safe from edge clipping)
        _skipContainer = new GameObject("SkipPrompt");
        _skipContainer.transform.SetParent(_rootGO.transform, false);
        RectTransform skipRT = _skipContainer.AddComponent<RectTransform>();
        skipRT.anchorMin = new Vector2(0.5f, 0f);
        skipRT.anchorMax = new Vector2(0.5f, 0f);
        skipRT.pivot = new Vector2(0.5f, 0f);
        skipRT.sizeDelta = new Vector2(340f, 90f);
        skipRT.anchoredPosition = new Vector2(0f, 40f);

        // Layout: icon with ring + label side by side
        HorizontalLayoutGroup hlg = _skipContainer.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 14f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.padding = new RectOffset(0, 0, 0, 0);

        // "HOLD" label (left of icon)
        GameObject holdLabelGO = new GameObject("HoldLabel");
        holdLabelGO.transform.SetParent(_skipContainer.transform, false);
        _skipLabel = holdLabelGO.AddComponent<TextMeshProUGUI>();
        _skipLabel.text = "HOLD";
        _skipLabel.fontSize = 22f;
        _skipLabel.alignment = TextAlignmentOptions.MidlineRight;
        _skipLabel.color = LABEL_COLOR;
        _skipLabel.raycastTarget = false;
        CinzelFontHelper.Apply(_skipLabel, true);
        LayoutElement labelLE = holdLabelGO.AddComponent<LayoutElement>();
        labelLE.preferredWidth = 80f;
        labelLE.preferredHeight = RING_SIZE;

        // Icon container (holds ring bg, ring fill, and icon)
        GameObject iconContainerGO = new GameObject("IconContainer");
        iconContainerGO.transform.SetParent(_skipContainer.transform, false);
        RectTransform iconContainerRT = iconContainerGO.AddComponent<RectTransform>();
        iconContainerRT.sizeDelta = new Vector2(RING_SIZE, RING_SIZE);
        LayoutElement iconContainerLE = iconContainerGO.AddComponent<LayoutElement>();
        iconContainerLE.preferredWidth = RING_SIZE;
        iconContainerLE.preferredHeight = RING_SIZE;

        // Ring background (full circle, dim)
        GameObject ringBgGO = new GameObject("RingBg");
        ringBgGO.transform.SetParent(iconContainerGO.transform, false);
        Image ringBgImg = ringBgGO.AddComponent<Image>();
        ringBgImg.color = RING_BG_COLOR;
        ringBgImg.type = Image.Type.Filled;
        ringBgImg.fillMethod = Image.FillMethod.Radial360;
        ringBgImg.fillAmount = 1f;
        ringBgImg.raycastTarget = false;
        RectTransform ringBgRT = ringBgGO.GetComponent<RectTransform>();
        ringBgRT.anchorMin = Vector2.zero;
        ringBgRT.anchorMax = Vector2.one;
        ringBgRT.offsetMin = Vector2.zero;
        ringBgRT.offsetMax = Vector2.zero;

        // Ring fill (radial, shows hold progress)
        GameObject ringFillGO = new GameObject("RingFill");
        ringFillGO.transform.SetParent(iconContainerGO.transform, false);
        _holdRing = ringFillGO.AddComponent<Image>();
        _holdRing.color = RING_FILL_COLOR;
        _holdRing.type = Image.Type.Filled;
        _holdRing.fillMethod = Image.FillMethod.Radial360;
        _holdRing.fillClockwise = true;
        _holdRing.fillOrigin = (int)Image.Origin360.Top;
        _holdRing.fillAmount = 0f;
        _holdRing.raycastTarget = false;
        RectTransform ringFillRT = ringFillGO.GetComponent<RectTransform>();
        ringFillRT.anchorMin = Vector2.zero;
        ringFillRT.anchorMax = Vector2.one;
        ringFillRT.offsetMin = Vector2.zero;
        ringFillRT.offsetMax = Vector2.zero;

        // Input icon (centered inside the ring)
        GameObject iconGO = new GameObject("InputIcon");
        iconGO.transform.SetParent(iconContainerGO.transform, false);
        _iconImage = iconGO.AddComponent<Image>();
        _iconImage.preserveAspect = true;
        _iconImage.raycastTarget = false;
        UpdateSkipIcon();
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0.5f, 0.5f);
        iconRT.anchorMax = new Vector2(0.5f, 0.5f);
        iconRT.pivot = new Vector2(0.5f, 0.5f);
        iconRT.sizeDelta = new Vector2(ICON_SIZE, ICON_SIZE);
        iconRT.anchoredPosition = Vector2.zero;

        // "TO SKIP" label (right of icon)
        GameObject skipTextGO = new GameObject("ToSkipLabel");
        skipTextGO.transform.SetParent(_skipContainer.transform, false);
        TextMeshProUGUI skipText = skipTextGO.AddComponent<TextMeshProUGUI>();
        skipText.text = "TO SKIP";
        skipText.fontSize = 22f;
        skipText.alignment = TextAlignmentOptions.MidlineLeft;
        skipText.color = LABEL_COLOR;
        skipText.raycastTarget = false;
        CinzelFontHelper.Apply(skipText, true);
        LayoutElement skipTextLE = skipTextGO.AddComponent<LayoutElement>();
        skipTextLE.preferredWidth = 120f;
        skipTextLE.preferredHeight = RING_SIZE;
    }

    private void UpdateSkipIcon()
    {
        if (_iconImage == null) return;

        Sprite icon = InputPromptManager.IsKeyboardMouse
            ? ControllerIcons.KeyEsc
            : ControllerIcons.CtrlB;

        _iconImage.sprite = icon;

        if (icon == null)
            _iconImage.color = Color.clear;
        else
            _iconImage.color = Color.white;
    }

    private void SetupVideoPlayer(VideoClip clip)
    {
        _videoPlayer = gameObject.AddComponent<VideoPlayer>();
        _videoPlayer.playOnAwake = false;
        _videoPlayer.clip = clip;
        _videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        _videoPlayer.isLooping = false;
        _videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;

        // Create render texture matching clip dimensions
        int width = (int)clip.width;
        int height = (int)clip.height;
        if (width <= 0) width = 1920;
        if (height <= 0) height = 1080;

        _renderTexture = new RenderTexture(width, height, 0);
        _renderTexture.Create();
        _videoPlayer.targetTexture = _renderTexture;
        _videoDisplay.texture = _renderTexture;

        _videoPlayer.loopPointReached += OnVideoFinished;
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        _videoFinished = true;
    }

    private IEnumerator PlaybackRoutine()
    {
        // Prepare and wait for the video to be ready
        _videoPlayer.Prepare();
        float prepareTimeout = 5f;
        float prepareElapsed = 0f;
        while (!_videoPlayer.isPrepared && prepareElapsed < prepareTimeout)
        {
            prepareElapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!_videoPlayer.isPrepared)
        {
            Debug.LogWarning("[CutscenePlayer] Video failed to prepare in time, skipping.");
            FinishPlayback();
            yield break;
        }

        _videoPlayer.Play();

        // Dissolve fade in: alpha ramps up while sand grains burst inward
        SpawnDissolveGrains(true);
        float fadeElapsed = 0f;
        while (fadeElapsed < FADE_IN_DURATION)
        {
            fadeElapsed += Time.unscaledDeltaTime;
            _canvasGroup.alpha = Mathf.Clamp01(fadeElapsed / FADE_IN_DURATION);
            yield return null;
        }
        _canvasGroup.alpha = 1f;

        // Wait for video to finish or skip
        while (!_videoFinished && !_skipped)
        {
            yield return null;
        }

        // Dissolve fade out: sand grains burst outward while alpha fades
        SpawnDissolveGrains(false);
        fadeElapsed = 0f;
        while (fadeElapsed < FADE_OUT_DURATION)
        {
            fadeElapsed += Time.unscaledDeltaTime;
            _canvasGroup.alpha = 1f - Mathf.Clamp01(fadeElapsed / FADE_OUT_DURATION);
            yield return null;
        }
        _canvasGroup.alpha = 0f;

        FinishPlayback();
    }

    private void SpawnDissolveGrains(bool isEntrance)
    {
        if (_rootGO == null) return;

        for (int i = 0; i < GRAIN_COUNT; i++)
        {
            StartCoroutine(AnimateGrain(isEntrance));
        }
    }

    private IEnumerator AnimateGrain(bool isEntrance)
    {
        if (_rootGO == null) yield break;

        GameObject grainGO = new GameObject("~grain");
        grainGO.transform.SetParent(_rootGO.transform, false);

        Image grainImg = grainGO.AddComponent<Image>();
        Color baseColor = Color.Lerp(SAND_DARK, SAND_LIGHT, UnityEngine.Random.value);
        grainImg.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
        grainImg.raycastTarget = false;

        RectTransform grainRT = grainGO.GetComponent<RectTransform>();
        float size = UnityEngine.Random.Range(GRAIN_SIZE_MIN, GRAIN_SIZE_MAX);
        grainRT.sizeDelta = new Vector2(size, size);

        // Random position on screen
        grainRT.anchorMin = new Vector2(0.5f, 0.5f);
        grainRT.anchorMax = new Vector2(0.5f, 0.5f);
        grainRT.pivot = new Vector2(0.5f, 0.5f);
        float startX = UnityEngine.Random.Range(-960f, 960f);
        float startY = UnityEngine.Random.Range(-540f, 540f);
        grainRT.anchoredPosition = new Vector2(startX, startY);

        // Drift direction: entrance grains fall down, exit grains drift up
        float driftY = UnityEngine.Random.Range(GRAIN_DRIFT_MIN, GRAIN_DRIFT_MAX) * (isEntrance ? -1f : 1f);
        float driftX = UnityEngine.Random.Range(-GRAIN_DRIFT_X, GRAIN_DRIFT_X);
        float lifetime = UnityEngine.Random.Range(GRAIN_LIFETIME_MIN, GRAIN_LIFETIME_MAX);

        // Stagger start
        float delay = UnityEngine.Random.Range(0f, 0.2f);
        float delayElapsed = 0f;
        while (delayElapsed < delay)
        {
            delayElapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // Animate
        Vector2 startPos = grainRT.anchoredPosition;
        float elapsed = 0f;
        while (elapsed < lifetime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / lifetime);

            if (grainRT != null)
                grainRT.anchoredPosition = startPos + new Vector2(driftX * t, driftY * t);

            // Fade: quick in, slow out
            float alpha;
            if (t < 0.2f)
                alpha = t / 0.2f;
            else
                alpha = 1f - ((t - 0.2f) / 0.8f);

            if (grainImg != null)
                grainImg.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha * 0.6f);

            yield return null;
        }

        if (grainGO != null)
            Destroy(grainGO);
    }

    void Update()
    {
        if (!_isPlaying || _skipped) return;

        // Update icon if input mode changes mid-cutscene
        UpdateSkipIcon();

        // Check hold-to-skip input
        bool holding = IsSkipHeld();

        if (holding)
        {
            _holdTime += Time.unscaledDeltaTime;
            if (_holdRing != null)
                _holdRing.fillAmount = Mathf.Clamp01(_holdTime / HOLD_TO_SKIP_DURATION);

            if (_holdTime >= HOLD_TO_SKIP_DURATION)
            {
                _skipped = true;
                Debug.Log("[CutscenePlayer] Cutscene skipped by player.");
            }
        }
        else
        {
            // Drain the hold progress when released
            _holdTime = Mathf.Max(0f, _holdTime - Time.unscaledDeltaTime * 2f);
            if (_holdRing != null)
                _holdRing.fillAmount = Mathf.Clamp01(_holdTime / HOLD_TO_SKIP_DURATION);
        }
    }

    private bool IsSkipHeld()
    {
        // ESC for keyboard, B button (JoystickButton1) for controller
        return Input.GetKey(KeyCode.Escape) || Input.GetKey(KeyCode.JoystickButton1);
    }

    private void FinishPlayback()
    {
        _isPlaying = false;
        Instance = null;

        if (_videoPlayer != null)
        {
            _videoPlayer.Stop();
            _videoPlayer.loopPointReached -= OnVideoFinished;
        }

        if (_renderTexture != null)
        {
            _renderTexture.Release();
            Destroy(_renderTexture);
        }

        Action callback = _onComplete;
        _onComplete = null;

        Destroy(gameObject);

        callback?.Invoke();
    }
}
