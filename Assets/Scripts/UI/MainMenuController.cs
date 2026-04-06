using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// In-scene main menu with rotating squares, shimmer tinting, crossfade transitions,
/// skip-to-gameplay and trial-select-on-load flags, shared dust/shimmer layers.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject trialSelectScreen;
    [SerializeField] private GameObject controlsScreen;

    [Header("Title")]
    [SerializeField] private TextMeshProUGUI titleLabel;
    [SerializeField] private TextMeshProUGUI subtitleLabel;
    [SerializeField] private TextMeshProUGUI chapterLabel;
    [SerializeField] private TextMeshProUGUI versionLabel;

    [Header("Decorative")]
    [SerializeField] private RectTransform  timeRingTransform;
    [SerializeField] private CanvasGroup    timeRingCanvasGroup;

    [Header("Buttons")]
    [SerializeField] private Button beginTrialButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button trialSelectButton;
    [SerializeField] private Button controlsButton;
    [SerializeField] private Button quitButton;

    [Header("Button Canvas Groups (stagger order)")]
    [SerializeField] private CanvasGroup[] buttonGroups;

    private const float TITLE_SPACE_START    = 60f;
    private const float TITLE_SPACE_END      = 20f;
    private const float LARGE_SQ_SPEED       = 6.5f;
    private const float SMALL_SQ_SPEED       = 11f;
    private const float LARGE_SQ_SIZE        = 550f;
    private const float SMALL_SQ_SIZE        = 360f;
    private const float BREATH_ALPHA_MIN     = 0.72f;
    private const float BREATH_ALPHA_MAX     = 1.0f;
    private const float BREATH_SPEED         = 2.1f;
    private const float BREATH_SPACING_RANGE = 3f;
    private const float SQ_BREATH_SPEED      = 0.7f;
    private const float SQ_SCALE_RANGE       = 0.03f;
    private const float SQ_ALPHA_MIN         = 0.015f;
    private const float SQ_ALPHA_MAX_LARGE   = 0.04f;
    private const float SQ_ALPHA_MAX_SMALL   = 0.06f;
    private const float SQ_TINT_LERP_IN      = 1.5f;
    private const float SQ_TINT_LERP_OUT     = 0.6f;
    private const float SQ_TINT_MAX          = 0.7f;
    private const float CROSSFADE_DURATION   = 0.35f;

    private static readonly Color SQ_DEFAULT_COLOR = new Color(1f, 0.843f, 0f);
    private static readonly Color MENU_BG_COLOR    = new Color(0.02f, 0.025f, 0.05f, 1f);

    private RectTransform _largeSqRT, _smallSqRT;
    private Image _largeSqImg, _smallSqImg;
    private bool _entranceDone, _menuContentHidden, _transitioning;
    private Color _currentSquareHue = SQ_DEFAULT_COLOR;
    private float _shimmerBlend;
    private GameObject _titleGroup, _buttonPanel, _sharedDustLayer, _shimmerLayer, _menuBgPanel;
    private CanvasGroup _menuContentGroup, _controlsCG, _trialSelectCG;

    private static bool _openTrialSelectOnLoad;
    private static bool _restartTrialOnLoad;

    /// <summary>Call before scene reload to auto-open trial select.</summary>
    public static void RequestTrialSelectOnLoad() => _openTrialSelectOnLoad = true;

    /// <summary>Call before scene reload to restart trial without menu.</summary>
    public static void RequestRestartTrialOnLoad() => _restartTrialOnLoad = true;

    void Awake()
    {
        Time.timeScale = 0f;
        if (hudPanel          != null) hudPanel.SetActive(false);
        if (trialSelectScreen != null) trialSelectScreen.SetActive(false);
        if (controlsScreen    != null) controlsScreen.SetActive(false);
        if (menuPanel         != null) menuPanel.SetActive(true);
        if (chapterLabel      != null) chapterLabel.gameObject.SetActive(false);
        if (timeRingTransform != null) timeRingTransform.gameObject.SetActive(false);

        DiscoverContentGroups();
        DiscoverSharedLayers();
        EnsureCanvasGroups();
        EnsureMenuBackground();
        EnsureShimmerLayer();
        CreateRotatingSquares();
    }

    void Start()
    {
        if (beginTrialButton  != null) beginTrialButton.onClick.AddListener(BeginTrial);
        if (continueButton    != null) continueButton.onClick.AddListener(BeginTrial);
        if (trialSelectButton != null) trialSelectButton.onClick.AddListener(OpenTrialSelect);
        if (controlsButton    != null) controlsButton.onClick.AddListener(OpenControls);
        if (quitButton        != null) quitButton.onClick.AddListener(QuitGame);
        if (versionLabel      != null) versionLabel.text = $"v{Application.version}";
        if (titleLabel    != null) titleLabel.color    = new Color(0.95f, 0.97f, 1.0f, 0f);
        if (subtitleLabel != null) subtitleLabel.color = new Color(0.92f, 0.82f, 0.55f, 0f);

        if (_restartTrialOnLoad) { _restartTrialOnLoad = false; SkipToGameplay(); return; }
        if (_openTrialSelectOnLoad)
        {
            _openTrialSelectOnLoad = false; _entranceDone = true;
            if (titleLabel    != null) titleLabel.alpha    = 1f;
            if (subtitleLabel != null) subtitleLabel.alpha = 1f;
            if (buttonGroups  != null) foreach (var g in buttonGroups) if (g != null) g.alpha = 1f;
            OpenTrialSelect(); return;
        }
        StartCoroutine(PlayEntrance());
    }

    private void SkipToGameplay()
    {
        if (menuPanel         != null) menuPanel.SetActive(false);
        if (trialSelectScreen != null) trialSelectScreen.SetActive(false);
        if (controlsScreen    != null) controlsScreen.SetActive(false);
        if (_sharedDustLayer  != null) _sharedDustLayer.SetActive(false);
        if (_shimmerLayer     != null) _shimmerLayer.SetActive(false);
        if (_menuBgPanel      != null) _menuBgPanel.SetActive(false);
        if (hudPanel          != null) hudPanel.SetActive(true);
        Time.timeScale = 1f;
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        float t  = Time.unscaledTime;
        if (_largeSqRT != null) _largeSqRT.Rotate(0f, 0f, -LARGE_SQ_SPEED * dt);
        if (_smallSqRT != null) _smallSqRT.Rotate(0f, 0f,  SMALL_SQ_SPEED * dt);
        AnimateSquareBreathing(t, dt);

        if (_entranceDone && !_menuContentHidden)
        {
            if (titleLabel != null)
            {
                float breath = (Mathf.Sin(t * BREATH_SPEED) + 1f) * 0.5f;
                titleLabel.alpha = Mathf.Lerp(BREATH_ALPHA_MIN, BREATH_ALPHA_MAX, breath);
                titleLabel.characterSpacing = TITLE_SPACE_END + Mathf.Sin(t * 1.3f) * BREATH_SPACING_RANGE;
            }
            if (subtitleLabel != null)
            {
                float sb = (Mathf.Sin(t * 1.6f + 1.2f) + 1f) * 0.5f;
                subtitleLabel.alpha = Mathf.Lerp(0.55f, 0.85f, sb);
            }
        }

        if (menuPanel != null && menuPanel.activeSelf && !_transitioning)
        {
            if (Input.GetKeyDown(KeyCode.JoystickButton1) || Input.GetKeyDown(KeyCode.Escape))
            {
                if (controlsScreen    != null && controlsScreen.activeSelf)    { CloseControls();    return; }
                if (trialSelectScreen != null && trialSelectScreen.activeSelf) { CloseTrialSelect(); return; }
            }
        }
    }

    private void AnimateSquareBreathing(float time, float dt)
    {
        if (MenuShimmerController.Intensity > 0.01f)
        {
            float target = Mathf.Min(MenuShimmerController.Intensity, SQ_TINT_MAX);
            _shimmerBlend = Mathf.MoveTowards(_shimmerBlend, target, SQ_TINT_LERP_IN * dt);
            _currentSquareHue = Color.Lerp(SQ_DEFAULT_COLOR, MenuShimmerController.CurrentColor, _shimmerBlend);
        }
        else
        {
            _shimmerBlend = Mathf.MoveTowards(_shimmerBlend, 0f, SQ_TINT_LERP_OUT * dt);
            if (_shimmerBlend < 0.01f) _currentSquareHue = SQ_DEFAULT_COLOR;
        }

        if (_largeSqImg != null)
        {
            float w = (Mathf.Sin(time * SQ_BREATH_SPEED) + 1f) * 0.5f;
            _largeSqImg.color = new Color(_currentSquareHue.r, _currentSquareHue.g, _currentSquareHue.b,
                Mathf.Lerp(SQ_ALPHA_MIN, SQ_ALPHA_MAX_LARGE, w));
            _largeSqRT.localScale = Vector3.one * (1f + Mathf.Sin(time * SQ_BREATH_SPEED * 0.5f) * SQ_SCALE_RANGE);
        }
        if (_smallSqImg != null)
        {
            float w = (Mathf.Sin(time * SQ_BREATH_SPEED * 1.3f + 1.5f) + 1f) * 0.5f;
            _smallSqImg.color = new Color(_currentSquareHue.r, _currentSquareHue.g, _currentSquareHue.b,
                Mathf.Lerp(SQ_ALPHA_MIN, SQ_ALPHA_MAX_SMALL, w));
            _smallSqRT.localScale = Vector3.one * (1f + Mathf.Sin(time * SQ_BREATH_SPEED * 0.7f + 0.8f) * SQ_SCALE_RANGE);
        }
    }

    private IEnumerator PlayEntrance()
    {
        if (titleLabel    != null) { titleLabel.alpha = 0f; titleLabel.characterSpacing = TITLE_SPACE_START; }
        if (subtitleLabel != null) subtitleLabel.alpha = 0f;
        if (buttonGroups  != null) foreach (var g in buttonGroups) if (g != null) g.alpha = 0f;
        yield return new WaitForSecondsRealtime(0.5f);
        if (titleLabel    != null) yield return AnimateTitle();
        if (subtitleLabel != null) yield return FadeText(subtitleLabel, 0.4f);
        if (buttonGroups != null)
            foreach (var g in buttonGroups) { if (g != null) StartCoroutine(FadeGroup(g, 0.3f)); yield return new WaitForSecondsRealtime(0.08f); }
        _entranceDone = true;
        SelectFirstButton();
    }

    private void SelectFirstButton()
    {
        Button first = beginTrialButton != null ? beginTrialButton : trialSelectButton != null ? trialSelectButton : controlsButton;
        if (first != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(first.gameObject);
    }

    private IEnumerator AnimateTitle()
    {
        float elapsed = 0f, dur = 0.8f;
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime; float t = Mathf.Clamp01(elapsed / dur);
            titleLabel.alpha = t; titleLabel.characterSpacing = Mathf.Lerp(TITLE_SPACE_START, TITLE_SPACE_END, EaseOut(t));
            yield return null;
        }
        titleLabel.alpha = 1f; titleLabel.characterSpacing = TITLE_SPACE_END;
    }

    private IEnumerator FadeText(TextMeshProUGUI label, float dur)
    {
        float elapsed = 0f;
        while (elapsed < dur) { elapsed += Time.unscaledDeltaTime; label.alpha = Mathf.Clamp01(elapsed / dur); yield return null; }
        label.alpha = 1f;
    }

    private IEnumerator FadeGroup(CanvasGroup group, float dur)
    {
        RectTransform rt = group.GetComponent<RectTransform>();
        Vector2 end = rt != null ? rt.anchoredPosition : Vector2.zero, start = end + Vector2.down * 20f;
        if (rt != null) rt.anchoredPosition = start;
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime; float t = Mathf.Clamp01(elapsed / dur);
            group.alpha = t; if (rt != null) rt.anchoredPosition = Vector2.Lerp(start, end, EaseOut(t));
            yield return null;
        }
        group.alpha = 1f; if (rt != null) rt.anchoredPosition = end;
    }

    private void DiscoverContentGroups()
    {
        if (menuPanel == null) return;
        Transform tg = menuPanel.transform.Find("TitleGroup"); if (tg != null) _titleGroup = tg.gameObject;
        Transform bp = menuPanel.transform.Find("ButtonPanel"); if (bp != null) _buttonPanel = bp.gameObject;
    }

    private void DiscoverSharedLayers()
    {
        Canvas canvas = GetComponentInParent<Canvas>(); if (canvas == null) return;
        Transform dl = canvas.transform.Find("DustLayer"); if (dl != null) _sharedDustLayer = dl.gameObject;
        Transform sl = canvas.transform.Find("ShimmerLayer"); if (sl != null) _shimmerLayer = sl.gameObject;
    }

    private void EnsureCanvasGroups()
    {
        if (menuPanel != null) { _menuContentGroup = menuPanel.GetComponent<CanvasGroup>(); if (_menuContentGroup == null) _menuContentGroup = menuPanel.AddComponent<CanvasGroup>(); }
        if (controlsScreen != null) { _controlsCG = controlsScreen.GetComponent<CanvasGroup>(); if (_controlsCG == null) _controlsCG = controlsScreen.AddComponent<CanvasGroup>(); }
        if (trialSelectScreen != null) { _trialSelectCG = trialSelectScreen.GetComponent<CanvasGroup>(); if (_trialSelectCG == null) _trialSelectCG = trialSelectScreen.AddComponent<CanvasGroup>(); }
    }

    /// <summary>Creates a dark background so gameplay never shows through during transitions.</summary>
    private void EnsureMenuBackground()
    {
        Canvas canvas = GetComponentInParent<Canvas>(); if (canvas == null) return;
        _menuBgPanel = new GameObject("MenuBackground");
        _menuBgPanel.transform.SetParent(canvas.transform, false);
        RectTransform rt = _menuBgPanel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        Image img = _menuBgPanel.AddComponent<Image>(); img.color = MENU_BG_COLOR; img.raycastTarget = false;
        _menuBgPanel.transform.SetAsFirstSibling();
    }

    /// <summary>Creates the ShimmerLayer if it doesn't exist in the scene.</summary>
    private void EnsureShimmerLayer()
    {
        if (_shimmerLayer != null) return;
        Canvas canvas = GetComponentInParent<Canvas>(); if (canvas == null) return;
        _shimmerLayer = new GameObject("ShimmerLayer");
        _shimmerLayer.transform.SetParent(canvas.transform, false);
        RectTransform rt = _shimmerLayer.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        _shimmerLayer.AddComponent<MenuShimmerController>();
        _shimmerLayer.transform.SetSiblingIndex(1); // after MenuBackground, before everything else
    }

    private void HideMenuContent()
    {
        if (_menuContentHidden) return; _menuContentHidden = true;
        if (_titleGroup  != null) _titleGroup.SetActive(false);
        if (_buttonPanel != null) _buttonPanel.SetActive(false);
        if (versionLabel != null) versionLabel.gameObject.SetActive(false);
    }

    private void ShowMenuContent()
    {
        if (!_menuContentHidden) return; _menuContentHidden = false;
        if (_titleGroup  != null) _titleGroup.SetActive(true);
        if (_buttonPanel != null) _buttonPanel.SetActive(true);
        if (versionLabel != null) versionLabel.gameObject.SetActive(true);
        SelectFirstButton();
    }

    /// <summary>Hides menu and starts gameplay.</summary>
    public void BeginTrial()
    {
        if (trialSelectScreen != null) trialSelectScreen.SetActive(false);
        if (controlsScreen    != null) controlsScreen.SetActive(false);
        if (_sharedDustLayer  != null) _sharedDustLayer.SetActive(false);
        if (_shimmerLayer     != null) _shimmerLayer.SetActive(false);

        if (ScreenTransitionManager.Instance != null)
        {
            ScreenTransitionManager.Instance.FadeTransition(() =>
            {
                ShowMenuContent();
                if (menuPanel    != null) menuPanel.SetActive(false);
                if (_menuBgPanel != null) _menuBgPanel.SetActive(false);
                if (hudPanel     != null) hudPanel.SetActive(true);
                Time.timeScale = 1f;
            });
        }
        else
        {
            ShowMenuContent();
            if (menuPanel    != null) menuPanel.SetActive(false);
            if (_menuBgPanel != null) _menuBgPanel.SetActive(false);
            if (hudPanel     != null) hudPanel.SetActive(true);
            Time.timeScale = 1f;
        }
    }

    public void OpenTrialSelect() { if (_transitioning) return; StartCoroutine(CrossfadeToScreen(trialSelectScreen, _trialSelectCG)); }
    public void CloseTrialSelect() { if (_transitioning) return; StartCoroutine(CrossfadeFromScreen(trialSelectScreen, _trialSelectCG)); }

    public void OpenControls()
    {
        if (_transitioning) return;
        var ctrl = controlsScreen != null ? controlsScreen.GetComponent<ControlsScreenController>() : null;
        if (ctrl != null) ctrl.Origin = ControlsScreenController.ControlsOrigin.MainMenu;
        StartCoroutine(CrossfadeToScreen(controlsScreen, _controlsCG));
    }

    public void CloseControls() { if (_transitioning) return; StartCoroutine(CrossfadeFromScreen(controlsScreen, _controlsCG)); }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator CrossfadeToScreen(GameObject screen, CanvasGroup screenCG)
    {
        if (screen == null) yield break; _transitioning = true;
        screen.SetActive(true); if (screenCG != null) screenCG.alpha = 0f;
        float elapsed = 0f;
        while (elapsed < CROSSFADE_DURATION)
        {
            elapsed += Time.unscaledDeltaTime; float t = EaseOut(Mathf.Clamp01(elapsed / CROSSFADE_DURATION));
            if (_menuContentGroup != null) _menuContentGroup.alpha = 1f - t;
            if (screenCG != null) screenCG.alpha = t;
            yield return null;
        }
        HideMenuContent();
        if (_menuContentGroup != null) _menuContentGroup.alpha = 1f;
        if (screenCG != null) screenCG.alpha = 1f;
        _transitioning = false;
    }

    private IEnumerator CrossfadeFromScreen(GameObject screen, CanvasGroup screenCG)
    {
        if (screen == null) yield break; _transitioning = true;
        ShowMenuContent(); if (_menuContentGroup != null) _menuContentGroup.alpha = 0f;
        float elapsed = 0f;
        while (elapsed < CROSSFADE_DURATION)
        {
            elapsed += Time.unscaledDeltaTime; float t = EaseOut(Mathf.Clamp01(elapsed / CROSSFADE_DURATION));
            if (screenCG != null) screenCG.alpha = 1f - t;
            if (_menuContentGroup != null) _menuContentGroup.alpha = t;
            yield return null;
        }
        screen.SetActive(false);
        if (screenCG != null) screenCG.alpha = 1f;
        if (_menuContentGroup != null) _menuContentGroup.alpha = 1f;
        _transitioning = false; SelectFirstButton();
    }

    private void CreateRotatingSquares()
    {
        if (menuPanel == null) return;
        _largeSqRT = MkSq("CCW_Square", menuPanel.transform, LARGE_SQ_SIZE, new Color(SQ_DEFAULT_COLOR.r, SQ_DEFAULT_COLOR.g, SQ_DEFAULT_COLOR.b, 0.03f));
        _largeSqRT.SetAsFirstSibling(); _largeSqImg = _largeSqRT.GetComponent<Image>();
        _smallSqRT = MkSq("CW_Square", menuPanel.transform, SMALL_SQ_SIZE, new Color(SQ_DEFAULT_COLOR.r, SQ_DEFAULT_COLOR.g, SQ_DEFAULT_COLOR.b, 0.04f));
        _smallSqRT.SetSiblingIndex(1); _smallSqImg = _smallSqRT.GetComponent<Image>();
    }

    private static RectTransform MkSq(string n, Transform p, float sz, Color c)
    {
        var go = new GameObject(n); go.transform.SetParent(p, false);
        var img = go.AddComponent<Image>(); img.color = c; img.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(sz, sz); rt.anchoredPosition = Vector2.zero; return rt;
    }

    private static float EaseOut(float t) => 1f - Mathf.Pow(1f - t, 3f);
}
