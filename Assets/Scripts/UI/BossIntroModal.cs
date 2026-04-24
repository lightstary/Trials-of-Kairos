using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// A reusable multi-page modal that introduces a boss fight before it starts.
/// Called by BossArenaEntry when the player reaches the boss trigger tile.
/// Blocks gameplay and pause input while open.
/// </summary>
public class BossIntroModal : MonoBehaviour
{
    // ── Static state ───────────────────────────────────────────────────
    private static BossIntroModal _instance;

    /// <summary>True while the modal is displayed. Blocks pause menu.</summary>
    public static bool IsOpen { get; private set; }

    /// <summary>Fired on every page change and on dismiss. Args: (currentPage, totalPages). Page -1 means dismissed.</summary>
    public static event Action<int, int> OnPageChanged;

    /// <summary>Current page index (0-based). -1 when closed.</summary>
    public static int CurrentPage => _instance != null && IsOpen ? _instance._currentPage : -1;

    // ── Visual constants ───────────────────────────────────────────────
    private static readonly Color PANEL_BG      = new Color(0.04f, 0.06f, 0.12f, 0.95f);
    private static readonly Color OVERLAY_BG    = new Color(0f, 0f, 0f, 0.55f);
    private static readonly Color ACCENT_GOLD   = new Color(0.961f, 0.784f, 0.259f, 1f);
    private static readonly Color TEXT_WHITE     = new Color(0.91f, 0.918f, 0.965f, 1f);
    private static readonly Color BTN_BG        = new Color(0.08f, 0.12f, 0.22f, 1f);

    private const float PANEL_WIDTH  = 740f;
    private const float PANEL_HEIGHT = 600f;
    private const float BODY_TOP_PAD = 35f;
    private const float BODY_HEIGHT  = 420f;
    private const float BTN_ROW_H    = 64f;
    private const float BTN_ROW_PAD  = 20f;
    private const float PAGE_IND_Y   = 90f;

    // ── Instance state ─────────────────────────────────────────────────
    private GameObject _modalGO;
    private TextMeshProUGUI _bodyTMP;
    private TextMeshProUGUI _pageIndicator;
    private TextMeshProUGUI _btnLabelTMP;
    private Button _continueBtn;
    private Button _backBtn;
    private Image _backBtnIcon;
    private Image _continueBtnIcon;

    private string[] _pages;
    private int _currentPage;
    private Action _onDismiss;
    private float _lastPageChangeTime;

    // ── Public API ─────────────────────────────────────────────────────

    /// <summary>
    /// Shows a boss intro modal with the given pages.
    /// When the player finishes reading, onDismiss is invoked (e.g. to start the boss fight).
    /// </summary>
    public static void Show(string[] pages, Action onDismiss)
    {
        if (IsOpen) return;
        if (pages == null || pages.Length == 0) { onDismiss?.Invoke(); return; }

        EnsureInstance();
        _instance._pages = pages;
        _instance._onDismiss = onDismiss;
        _instance.OpenModal();
    }

    // ── Lifecycle ──────────────────────────────────────────────────────

    private static void EnsureInstance()
    {
        if (_instance != null) return;
        GameObject go = new GameObject("[BossIntroModal]");
        _instance = go.AddComponent<BossIntroModal>();
        DontDestroyOnLoad(go);
    }

    void OnDestroy()
    {
        if (_instance == this) _instance = null;
        InputPromptManager.OnInputModeChanged -= OnInputModeChanged;
    }

    void Update()
    {
        if (!IsOpen) return;

        // Debounce rapid input
        if (Time.unscaledTime - _lastPageChangeTime < 0.2f) return;

        bool confirm = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)
                    || Input.GetKeyDown(KeyCode.JoystickButton0);
        bool back = Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace)
                 || Input.GetKeyDown(KeyCode.JoystickButton1);

        if (confirm) OnContinueClicked();
        else if (back) OnBackClicked();
    }

    // ── Modal open / close ─────────────────────────────────────────────

    private void OpenModal()
    {
        IsOpen = true;
        _currentPage = 0;
        Time.timeScale = 0f;
        InputPromptManager.OnInputModeChanged += OnInputModeChanged;
        BuildUI();
        UpdatePage();
    }

    private void Dismiss()
    {
        IsOpen = false;
        Time.timeScale = 1f;
        InputPromptManager.OnInputModeChanged -= OnInputModeChanged;

        OnPageChanged?.Invoke(-1, 0);

        if (_modalGO != null) Destroy(_modalGO);
        _modalGO = null;

        _onDismiss?.Invoke();
        _onDismiss = null;
    }

    // ── Navigation ─────────────────────────────────────────────────────

    private void OnContinueClicked()
    {
        _lastPageChangeTime = Time.unscaledTime;

        if (_currentPage < _pages.Length - 1)
        {
            _currentPage++;
            UpdatePage();
        }
        else
        {
            Dismiss();
        }
    }

    private void OnBackClicked()
    {
        _lastPageChangeTime = Time.unscaledTime;

        if (_currentPage > 0)
        {
            _currentPage--;
            UpdatePage();
        }
    }

    private void UpdatePage()
    {
        if (_bodyTMP != null) _bodyTMP.text = _pages[_currentPage];

        if (_pageIndicator != null)
            _pageIndicator.text = _pages.Length > 1 ? $"{_currentPage + 1} / {_pages.Length}" : "";

        if (_btnLabelTMP != null)
            _btnLabelTMP.text = _currentPage < _pages.Length - 1 ? "CONTINUE" : "BEGIN";

        if (_backBtn != null) _backBtn.gameObject.SetActive(_currentPage > 0);

        OnPageChanged?.Invoke(_currentPage, _pages.Length);
    }

    // ── Input mode updates ─────────────────────────────────────────────

    private void OnInputModeChanged(InputPromptManager.InputMode newMode)
    {
        if (!IsOpen) return;
        if (_backBtnIcon != null) _backBtnIcon.sprite = ControllerIcons.BackIcon;
        if (_continueBtnIcon != null) _continueBtnIcon.sprite = ControllerIcons.ConfirmIcon;
    }

    // ── UI Construction ────────────────────────────────────────────────

    private void BuildUI()
    {
        if (_modalGO != null) Destroy(_modalGO);

        // Root canvas
        _modalGO = new GameObject("BossIntroModal_Canvas");
        Canvas canvas = _modalGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        _modalGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _modalGO.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        _modalGO.AddComponent<GraphicRaycaster>();

        // Overlay background
        GameObject overlay = MakeRect("Overlay", _modalGO.transform, Vector2.zero, Vector2.one);
        Image overlayImg = overlay.AddComponent<Image>();
        overlayImg.color = OVERLAY_BG;
        overlayImg.raycastTarget = true;

        // Center panel
        GameObject panel = MakeRect("Panel", _modalGO.transform, new Vector2(0.5f, 0.45f), new Vector2(0.5f, 0.45f));
        RectTransform panelRT = panel.GetComponent<RectTransform>();
        panelRT.sizeDelta = new Vector2(PANEL_WIDTH, PANEL_HEIGHT);
        Image panelImg = panel.AddComponent<Image>();
        panelImg.color = PANEL_BG;

        // Body text
        GameObject bodyGO = MakeRect("Body", panel.transform, new Vector2(0f, 1f), new Vector2(1f, 1f));
        RectTransform bodyRT = bodyGO.GetComponent<RectTransform>();
        bodyRT.pivot = new Vector2(0.5f, 1f);
        bodyRT.sizeDelta = new Vector2(-80f, BODY_HEIGHT);
        bodyRT.anchoredPosition = new Vector2(0f, -BODY_TOP_PAD);
        _bodyTMP = bodyGO.AddComponent<TextMeshProUGUI>();
        _bodyTMP.fontSize = 26f;
        _bodyTMP.color = TEXT_WHITE;
        _bodyTMP.alignment = TextAlignmentOptions.TopLeft;
        _bodyTMP.richText = true;
        _bodyTMP.enableWordWrapping = true;
        _bodyTMP.overflowMode = TextOverflowModes.Overflow;
        _bodyTMP.raycastTarget = false;
        CinzelFontHelper.Apply(_bodyTMP);

        // Button row — pinned to bottom
        GameObject btnRow = MakeRect("ButtonRow", panel.transform, new Vector2(0f, 0f), new Vector2(1f, 0f));
        RectTransform btnRowRT = btnRow.GetComponent<RectTransform>();
        btnRowRT.pivot = new Vector2(0.5f, 0f);
        btnRowRT.sizeDelta = new Vector2(-60f, BTN_ROW_H);
        btnRowRT.anchoredPosition = new Vector2(0f, BTN_ROW_PAD);

        HorizontalLayoutGroup btnHLG = btnRow.AddComponent<HorizontalLayoutGroup>();
        btnHLG.spacing = 20f;
        btnHLG.childAlignment = TextAnchor.MiddleCenter;
        btnHLG.childControlWidth = true;
        btnHLG.childControlHeight = true;
        btnHLG.childForceExpandWidth = true;
        btnHLG.childForceExpandHeight = true;
        btnHLG.padding = new RectOffset(10, 10, 0, 0);

        // Page indicator
        GameObject indicatorGO = MakeRect("PageIndicator", panel.transform, new Vector2(0f, 0f), new Vector2(0.5f, 0f));
        RectTransform indRT = indicatorGO.GetComponent<RectTransform>();
        indRT.pivot = new Vector2(0f, 0f);
        indRT.sizeDelta = new Vector2(0f, 28f);
        indRT.anchoredPosition = new Vector2(40f, PAGE_IND_Y);
        _pageIndicator = indicatorGO.AddComponent<TextMeshProUGUI>();
        _pageIndicator.fontSize = 20f;
        _pageIndicator.color = new Color(TEXT_WHITE.r, TEXT_WHITE.g, TEXT_WHITE.b, 0.4f);
        _pageIndicator.alignment = TextAlignmentOptions.BottomLeft;
        _pageIndicator.raycastTarget = false;
        CinzelFontHelper.Apply(_pageIndicator);

        // Color block for buttons
        ColorBlock cb = ColorBlock.defaultColorBlock;
        cb.normalColor = Color.white;
        cb.highlightedColor = ACCENT_GOLD;
        cb.selectedColor = ACCENT_GOLD;
        cb.pressedColor = new Color(0.7f, 0.5f, 0.1f, 1f);
        cb.fadeDuration = 0.05f;

        // ── Back button ────────────────────────────────────────────────
        _backBtn = CreateButton(btnRow.transform, "BackBtn", cb, OnBackClicked,
            ControllerIcons.BackIcon, "BACK", out _backBtnIcon);

        // ── Continue button ────────────────────────────────────────────
        Button contBtn = CreateButton(btnRow.transform, "ContinueBtn", cb, OnContinueClicked,
            ControllerIcons.ConfirmIcon, "BEGIN", out _continueBtnIcon);
        _continueBtn = contBtn;

        // Get the label reference
        _btnLabelTMP = contBtn.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
    }

    /// <summary>Creates a styled button with icon + label inside an HLG.</summary>
    private Button CreateButton(Transform parent, string name, ColorBlock cb,
        UnityEngine.Events.UnityAction onClick, Sprite iconSprite, string labelText,
        out Image trackedIcon)
    {
        GameObject btnGO = new GameObject(name);
        btnGO.transform.SetParent(parent, false);
        Image btnImg = btnGO.AddComponent<Image>();
        btnImg.color = BTN_BG;
        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        btn.colors = cb;
        btn.onClick.AddListener(onClick);

        HorizontalLayoutGroup hlg = btnGO.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.padding = new RectOffset(12, 12, 4, 4);

        // Icon
        trackedIcon = null;
        if (iconSprite != null)
        {
            trackedIcon = ControllerIcons.CreateIcon(btnGO.transform, iconSprite, 32f);
            if (trackedIcon != null)
            {
                LayoutElement ile = trackedIcon.gameObject.AddComponent<LayoutElement>();
                ile.preferredWidth = 32f;
                ile.preferredHeight = 32f;
            }
        }

        // Label
        GameObject lblGO = new GameObject("Label");
        lblGO.transform.SetParent(btnGO.transform, false);
        TextMeshProUGUI tmp = lblGO.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 24f;
        tmp.color = TEXT_WHITE;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.text = labelText;
        tmp.raycastTarget = false;
        CinzelFontHelper.Apply(tmp, true);
        LayoutElement lle = lblGO.AddComponent<LayoutElement>();
        lle.preferredWidth = 120f;
        lle.preferredHeight = 32f;

        return btn;
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private static GameObject MakeRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return go;
    }
}
