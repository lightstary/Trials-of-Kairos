using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls reference screen with Cinzel font, single centered column,
/// staggered fade-in per row, and origin-aware back navigation.
/// Sections: MOVEMENT, TIME MECHANICS, ACTIONS, SYSTEM.
/// </summary>
public class ControlsScreenController : MonoBehaviour
{
    [Header("Existing References (auto-discovered if null)")]
    [SerializeField] private GameObject existingCard;

    /// <summary>Where this screen was opened from.</summary>
    public enum ControlsOrigin { MainMenu, PauseMenu }

    /// <summary>Set before enabling to control where B/back returns to.</summary>
    public ControlsOrigin Origin { get; set; } = ControlsOrigin.MainMenu;

    // ── Colors ───────────────────────────────────────────────────────────────
    private static readonly Color Gold       = new Color(0.961f, 0.784f, 0.259f, 1f);
    private static readonly Color Blue       = new Color(0.353f, 0.706f, 0.941f, 1f);
    private static readonly Color Purple     = new Color(0.608f, 0.365f, 0.898f, 1f);
    private static readonly Color Green      = new Color(0.271f, 0.761f, 0.275f, 1f);
    private static readonly Color Red        = new Color(0.898f, 0.196f, 0.106f, 1f);
    private static readonly Color XBlue      = new Color(0.224f, 0.478f, 0.918f, 1f);
    private static readonly Color Slate      = new Color(0.180f, 0.250f, 0.380f, 1f);
    private static readonly Color LabelWhite = new Color(0.91f, 0.918f, 0.965f, 1f);
    private static readonly Color TitleGold  = new Color(1f, 0.843f, 0f, 0.85f);
    private static readonly Color DescDim    = new Color(0.91f, 0.918f, 0.965f, 0.45f);
    private static readonly Color BtnNormal  = new Color(0.10f, 0.15f, 0.28f, 0.95f);
    private static readonly Color BtnHover   = new Color(0.961f, 0.784f, 0.259f, 1f);
    private static readonly Color BtnPress   = new Color(0.7f, 0.55f, 0.1f, 1f);

    // ── Row data ─────────────────────────────────────────────────────────────
    private struct RowData
    {
        public string badge;
        public Color  color;
        public string action;
        public string desc;
    }

    /// <summary>Returns movement rows for the current input mode.</summary>
    private static RowData[] GetMovementRows()
    {
        if (InputPromptManager.IsKeyboardMouse)
        {
            return new[]
            {
                new RowData { badge = "W",     color = Slate, action = "Forward", desc = "W \u2014 roll forward" },
                new RowData { badge = "A",     color = Slate, action = "Left",    desc = "A \u2014 roll left" },
                new RowData { badge = "S",     color = Slate, action = "Back",    desc = "S \u2014 roll backward" },
                new RowData { badge = "D",     color = Slate, action = "Right",   desc = "D \u2014 roll right" },
                new RowData { badge = "MOUSE", color = Slate, action = "Look",    desc = "Mouse \u2014 rotate camera" },
            };
        }
        return new[]
        {
            new RowData { badge = "LS", color = Slate, action = "Move", desc = "Left stick \u2014 roll in all directions" },
            new RowData { badge = "RS", color = Slate, action = "Look", desc = "Right stick \u2014 rotate camera" },
        };
    }

    /// <summary>Returns action rows for the current input mode.</summary>
    private static RowData[] GetActionRows() => new[]
    {
        new RowData { badge = ControllerIcons.ConfirmBadge, color = Green, action = "Confirm", desc = ControllerIcons.ConfirmDesc },
        new RowData { badge = ControllerIcons.CancelBadge,  color = Red,   action = "Cancel",  desc = ControllerIcons.CancelDesc },
    };

    /// <summary>Returns system rows for the current input mode.</summary>
    private static RowData[] GetSystemRows() => new[]
    {
        new RowData { badge = ControllerIcons.PauseBadge, color = Slate, action = "Pause", desc = ControllerIcons.PauseDesc },
    };

    // ── Constants ────────────────────────────────────────────────────────────
    private const float ROW_HEIGHT          = 90f;
    private const float SECTION_TITLE_HEIGHT = 44f;
    private const float SPACER_HEIGHT       = 20f;
    private const float BADGE_SIZE          = 72f;
    private const float ACTION_FONT_SIZE    = 30f;
    private const float DESC_FONT_SIZE      = 21f;
    private const float TITLE_FONT_SIZE     = 22f;
    private const float TITLE_SPACING       = 18f;
    private const float BACK_BTN_WIDTH      = 160f;
    private const float BACK_BTN_HEIGHT     = 50f;
    private const float FADE_STAGGER        = 0.04f;
    private const float FADE_DURATION       = 0.35f;

    // ── Tracked mutable elements ─────────────────────────────────────────
    private struct TrackedRow
    {
        public Image badgeImg;
        public GameObject badgeTxtGO;
        public TextMeshProUGUI descTMP;
    }

    // ── State ────────────────────────────────────────────────────────────────
    private bool _built;
    private GameObject _layoutRoot;
    private TMP_FontAsset _cinzelFont;
    private ScrollRect _scrollRect;
    private InputPromptManager.InputMode _builtForMode;
    private readonly System.Collections.Generic.List<TrackedRow> _trackedRows
        = new System.Collections.Generic.List<TrackedRow>();
    private TextMeshProUGUI _modeLabelTMP;
    private TextMeshProUGUI _backBtnLabel;
    private Image _backBtnIcon;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    void OnEnable()
    {
        InputPromptManager.OnInputModeChanged += OnInputModeChanged;
        if (!_built) BuildLayout();
        else if (_builtForMode != InputPromptManager.CurrentMode) UpdateTrackedElements();
        StartCoroutine(PlayEntranceAnimation());
    }

    void OnDisable()
    {
        InputPromptManager.OnInputModeChanged -= OnInputModeChanged;
    }

    private void OnInputModeChanged(InputPromptManager.InputMode newMode)
    {
        if (!gameObject.activeInHierarchy) return;
        _builtForMode = newMode;
        RebuildLayout();
    }

    /// <summary>Tears down and rebuilds the entire controls layout for the new input mode.</summary>
    private void RebuildLayout()
    {
        _trackedRows.Clear();
        _modeLabelTMP = null;
        _backBtnLabel = null;
        _backBtnIcon = null;
        _scrollRect = null;

        // Destroy dynamically created children (ControlsBG and ControlsScroll)
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name == "ControlsBG" || child.name == "ControlsScroll")
                Destroy(child.gameObject);
        }

        _layoutRoot = null;
        _built = false;
        BuildLayout();
        StartCoroutine(PlayEntranceAnimation());
    }

    /// <summary>Updates all tracked mutable elements in place.</summary>
    private void UpdateTrackedElements()
    {
        // Mode label
        if (_modeLabelTMP != null)
            _modeLabelTMP.text = InputPromptManager.IsKeyboardMouse ? "KEYBOARD + MOUSE" : "CONTROLLER";

        // Regenerate row data
        RowData[] allData = CombineAllRows();
        for (int i = 0; i < _trackedRows.Count && i < allData.Length; i++)
        {
            TrackedRow tr = _trackedRows[i];
            RowData rd = allData[i];

            // Update badge: always try sprite first
            Sprite icon = GetBadgeSprite(rd.badge);
            if (icon != null)
            {
                tr.badgeImg.sprite = icon;
                tr.badgeImg.preserveAspect = true;
                tr.badgeImg.color = Color.white;
                if (tr.badgeTxtGO != null) tr.badgeTxtGO.SetActive(false);
            }
            else
            {
                tr.badgeImg.sprite = null;
                tr.badgeImg.preserveAspect = false;
                tr.badgeImg.color = rd.color;
                if (tr.badgeTxtGO != null)
                {
                    tr.badgeTxtGO.SetActive(true);
                    TextMeshProUGUI txt = tr.badgeTxtGO.GetComponent<TextMeshProUGUI>();
                    if (txt != null)
                    {
                        txt.text = rd.badge;
                        txt.fontSize = rd.badge.Length > 3 ? 12f : (rd.badge.Length > 2 ? 15f : 24f);
                        txt.color = IsLight(rd.color)
                            ? new Color(0.031f, 0.043f, 0.078f, 1f)
                            : new Color(0.910f, 0.918f, 0.965f, 1f);
                    }
                }
            }

            // Update description
            if (tr.descTMP != null) tr.descTMP.text = rd.desc;
        }

        // Back button icon swap
        if (_backBtnIcon != null)
        {
            Sprite newBackSprite = InputPromptManager.IsKeyboardMouse ? ControllerIcons.KeyEsc : ControllerIcons.CtrlB;
            if (newBackSprite != null)
            {
                _backBtnIcon.sprite = newBackSprite;
                _backBtnIcon.gameObject.SetActive(true);
            }
            else
            {
                _backBtnIcon.gameObject.SetActive(false);
            }
        }
    }

    private RowData[] CombineAllRows()
    {
        RowData[] m = GetMovementRows();
        RowData[] a = GetActionRows();
        RowData[] s = GetSystemRows();
        RowData[] all = new RowData[m.Length + a.Length + s.Length];
        int idx = 0;
        foreach (var r in m) all[idx++] = r;
        foreach (var r in a) all[idx++] = r;
        foreach (var r in s) all[idx++] = r;
        return all;
    }

    /// <summary>Maps badge text to the icon sprite, or null for text-only badges.</summary>
    private static Sprite GetBadgeSprite(string badge)
    {
        switch (badge)
        {
            case "A":      return InputPromptManager.IsKeyboardMouse ? ControllerIcons.KeyA : ControllerIcons.CtrlA;
            case "B":      return ControllerIcons.CtrlB;
            case "LS":     return ControllerIcons.CtrlLeftStick;
            case "RS":     return ControllerIcons.CtrlRightStick;
            case "PAUSE":  return ControllerIcons.CtrlPause;
            case "W":      return ControllerIcons.KeyW;
            case "S":      return ControllerIcons.KeyS;
            case "D":      return ControllerIcons.KeyD;
            case "WASD":   return ControllerIcons.KeyW;
            case "CLICK":  return ControllerIcons.MouseLeft;
            case "MOUSE":  return ControllerIcons.MouseIcon;
            case "ESC":    return ControllerIcons.KeyEsc;
            default:       return null;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton1))
            GoBack();

        // Scroll with right stick vertical axis for controller users
        if (_scrollRect != null)
        {
            float scrollInput = Input.GetAxis("Vertical");
            if (Mathf.Abs(scrollInput) > 0.1f)
                _scrollRect.verticalNormalizedPosition = Mathf.Clamp01(
                    _scrollRect.verticalNormalizedPosition + scrollInput * Time.unscaledDeltaTime * 2f);
        }
    }

    /// <summary>Navigates back to the origin screen.</summary>
    private void GoBack()
    {
        if (Origin == ControlsOrigin.PauseMenu)
        {
            gameObject.SetActive(false);
            PauseMenuController pmc = FindObjectOfType<PauseMenuController>();
            if (pmc != null) pmc.ReturnFromControls();
        }
        else
        {
            MainMenuController mmc = FindObjectOfType<MainMenuController>();
            if (mmc != null) mmc.CloseControls();
        }
    }

    // ── Font discovery ───────────────────────────────────────────────────────

    /// <summary>Finds the Cinzel SDF font from loaded assets.</summary>
    private TMP_FontAsset FindCinzelFont()
    {
        TMP_FontAsset[] allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        TMP_FontAsset best = null;
        foreach (var font in allFonts)
        {
            if (font.name.Contains("Cinzel"))
            {
                // Prefer SemiBold or Bold for UI readability
                if (font.name.Contains("SemiBold") || font.name.Contains("Bold"))
                    return font;
                if (best == null)
                    best = font;
            }
        }
        return best != null ? best : TMP_Settings.defaultFontAsset;
    }

    // ── Layout builder ───────────────────────────────────────────────────────

    private void BuildLayout()
    {
        _built = true;
        _builtForMode = InputPromptManager.CurrentMode;
        _cinzelFont = FindCinzelFont();
        _trackedRows.Clear();
        _modeLabelTMP = null;
        _backBtnLabel = null;

        // Hide existing Card
        if (existingCard == null)
        {
            Transform card = transform.Find("Card");
            if (card != null) existingCard = card.gameObject;
        }
        if (existingCard != null) existingCard.SetActive(false);

        // Add a solid dark background so this screen is self-contained
        // (not transparent over whatever was behind it)
        GameObject bgGO = new GameObject("ControlsBG");
        bgGO.transform.SetParent(transform, false);
        bgGO.transform.SetAsFirstSibling();
        RectTransform bgRT = bgGO.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.02f, 0.025f, 0.05f, 0.95f);
        bgImg.raycastTarget = false;

        // Scroll view for controller navigation
        GameObject scrollGO = new GameObject("ControlsScroll");
        scrollGO.transform.SetParent(transform, false);
        RectTransform scrollRT = scrollGO.AddComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0.15f, 0.04f);
        scrollRT.anchorMax = new Vector2(0.85f, 0.96f);
        scrollRT.offsetMin = Vector2.zero;
        scrollRT.offsetMax = Vector2.zero;

        _scrollRect = scrollGO.AddComponent<ScrollRect>();
        _scrollRect.horizontal = false;
        _scrollRect.vertical   = true;
        _scrollRect.movementType = ScrollRect.MovementType.Elastic;
        _scrollRect.scrollSensitivity = 30f;
        _scrollRect.inertia = true;
        _scrollRect.decelerationRate = 0.12f;

        // RectMask2D clips without needing an Image alpha
        scrollGO.AddComponent<RectMask2D>();

        // Viewport
        GameObject viewportGO = new GameObject("Viewport");
        viewportGO.transform.SetParent(scrollGO.transform, false);
        RectTransform vpRT = viewportGO.AddComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = Vector2.zero; vpRT.offsetMax = Vector2.zero;

        // Content root
        _layoutRoot = new GameObject("Content");
        _layoutRoot.transform.SetParent(viewportGO.transform, false);
        RectTransform rootRT = _layoutRoot.AddComponent<RectTransform>();
        rootRT.anchorMin = new Vector2(0f, 1f);
        rootRT.anchorMax = new Vector2(1f, 1f);
        rootRT.pivot     = new Vector2(0.5f, 1f);
        rootRT.offsetMin = Vector2.zero;
        rootRT.offsetMax = Vector2.zero;

        VerticalLayoutGroup rootVLG = _layoutRoot.AddComponent<VerticalLayoutGroup>();
        rootVLG.spacing                = 6f;
        rootVLG.childAlignment         = TextAnchor.UpperCenter;
        rootVLG.childControlWidth      = true;
        rootVLG.childControlHeight     = false;
        rootVLG.childForceExpandWidth  = true;
        rootVLG.childForceExpandHeight = false;
        rootVLG.padding                = new RectOffset(0, 0, 8, 80);

        ContentSizeFitter csf = _layoutRoot.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _scrollRect.content  = rootRT;
        _scrollRect.viewport = vpRT;

        // ── Screen title ─────────────────────────────────────────────────
        CreateScreenTitle(_layoutRoot.transform, "CONTROLS", 46f);
        CreateSpacer(_layoutRoot.transform, 6f);

        // ── Mode label ───────────────────────────────────────────────────
        string modeText = InputPromptManager.IsKeyboardMouse ? "KEYBOARD + MOUSE" : "CONTROLLER";
        CreateModeLabel(_layoutRoot.transform, modeText);
        CreateSpacer(_layoutRoot.transform, 12f);

        // ── Two-column container ─────────────────────────────────────────
        GameObject columnsGO = new GameObject("Columns");
        columnsGO.transform.SetParent(_layoutRoot.transform, false);
        RectTransform colRT = columnsGO.AddComponent<RectTransform>();
        colRT.sizeDelta = new Vector2(0f, 500f); // will be adjusted by content
        columnsGO.AddComponent<CanvasGroup>();

        HorizontalLayoutGroup colHLG = columnsGO.AddComponent<HorizontalLayoutGroup>();
        colHLG.spacing = 40f;
        colHLG.childAlignment = TextAnchor.UpperCenter;
        colHLG.childControlWidth = true;
        colHLG.childControlHeight = false;
        colHLG.childForceExpandWidth = true;
        colHLG.childForceExpandHeight = false;
        colHLG.padding = new RectOffset(20, 20, 0, 0);

        // ── LEFT column: MOVEMENT ────────────────────────────────────────
        GameObject leftCol = new GameObject("LeftColumn");
        leftCol.transform.SetParent(columnsGO.transform, false);
        RectTransform leftRT = leftCol.AddComponent<RectTransform>();
        VerticalLayoutGroup leftVLG = leftCol.AddComponent<VerticalLayoutGroup>();
        leftVLG.spacing = 6f;
        leftVLG.childAlignment = TextAnchor.UpperCenter;
        leftVLG.childControlWidth = true;
        leftVLG.childControlHeight = false;
        leftVLG.childForceExpandWidth = true;
        leftVLG.childForceExpandHeight = false;

        CreateSectionTitle(leftCol.transform, "MOVEMENT");
        foreach (RowData row in GetMovementRows())
            CreateRow(leftCol.transform, row);

        // ── RIGHT column: ACTIONS + SYSTEM ───────────────────────────────
        GameObject rightCol = new GameObject("RightColumn");
        rightCol.transform.SetParent(columnsGO.transform, false);
        RectTransform rightRT = rightCol.AddComponent<RectTransform>();
        VerticalLayoutGroup rightVLG = rightCol.AddComponent<VerticalLayoutGroup>();
        rightVLG.spacing = 6f;
        rightVLG.childAlignment = TextAnchor.UpperCenter;
        rightVLG.childControlWidth = true;
        rightVLG.childControlHeight = false;
        rightVLG.childForceExpandWidth = true;
        rightVLG.childForceExpandHeight = false;

        CreateSectionTitle(rightCol.transform, "ACTIONS");
        foreach (RowData row in GetActionRows())
            CreateRow(rightCol.transform, row);

        CreateSpacer(rightCol.transform, SPACER_HEIGHT);

        CreateSectionTitle(rightCol.transform, "SYSTEM");
        foreach (RowData row in GetSystemRows())
            CreateRow(rightCol.transform, row);

        // ── Calculate column height ──────────────────────────────────────
        // Left: section title + movement rows
        RowData[] movRows = GetMovementRows();
        float leftHeight = SECTION_TITLE_HEIGHT + movRows.Length * (ROW_HEIGHT + 6f);
        // Right: section title + action rows + spacer + section title + system rows
        RowData[] actRows = GetActionRows();
        RowData[] sysRows = GetSystemRows();
        float rightHeight = SECTION_TITLE_HEIGHT + actRows.Length * (ROW_HEIGHT + 6f)
                          + SPACER_HEIGHT + SECTION_TITLE_HEIGHT + sysRows.Length * (ROW_HEIGHT + 6f);
        float maxHeight = Mathf.Max(leftHeight, rightHeight);
        colRT.sizeDelta = new Vector2(0f, maxHeight);

        CreateSpacer(_layoutRoot.transform, 20f);

        // ── Back button ──────────────────────────────────────────────────
        CreateBackButton(_layoutRoot.transform);
    }

    // ── Entrance animation ───────────────────────────────────────────────────

    private IEnumerator PlayEntranceAnimation()
    {
        if (_layoutRoot == null) yield break;

        CanvasGroup[] rows = _layoutRoot.GetComponentsInChildren<CanvasGroup>(true);
        foreach (var cg in rows) cg.alpha = 0f;

        yield return null; // let layout settle

        for (int i = 0; i < rows.Length; i++)
        {
            StartCoroutine(FadeInElement(rows[i]));
            yield return new WaitForSecondsRealtime(FADE_STAGGER);
        }
    }

    private IEnumerator FadeInElement(CanvasGroup cg)
    {
        RectTransform rt = cg.GetComponent<RectTransform>();
        Vector2 target = rt.anchoredPosition;
        rt.anchoredPosition = target + Vector2.down * 12f;

        float elapsed = 0f;
        while (elapsed < FADE_DURATION)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / FADE_DURATION);
            float ease = 1f - Mathf.Pow(1f - t, 3f);
            cg.alpha = t;
            rt.anchoredPosition = Vector2.Lerp(target + Vector2.down * 12f, target, ease);
            yield return null;
        }
        cg.alpha = 1f;
        rt.anchoredPosition = target;
    }

    // ── Element builders ─────────────────────────────────────────────────────

    private void CreateScreenTitle(Transform parent, string text, float height)
    {
        GameObject go = new GameObject("ScreenTitle");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, height);
        go.AddComponent<CanvasGroup>();

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text             = text;
        tmp.font             = _cinzelFont;
        tmp.fontSize         = 36f;
        tmp.characterSpacing = 24f;
        tmp.alignment        = TextAlignmentOptions.Center;
        tmp.color            = LabelWhite;
        tmp.raycastTarget    = false;
    }

    private void CreateModeLabel(Transform parent, string text)
    {
        GameObject go = new GameObject("ModeLabel");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, 22f);
        go.AddComponent<CanvasGroup>();

        _modeLabelTMP = go.AddComponent<TextMeshProUGUI>();
        _modeLabelTMP.text             = text;
        _modeLabelTMP.font             = _cinzelFont;
        _modeLabelTMP.fontSize         = 15f;
        _modeLabelTMP.characterSpacing = 12f;
        _modeLabelTMP.alignment        = TextAlignmentOptions.Center;
        _modeLabelTMP.color            = new Color(LabelWhite.r, LabelWhite.g, LabelWhite.b, 0.35f);
        _modeLabelTMP.raycastTarget    = false;
    }

    private void CreateSectionTitle(Transform parent, string text)
    {
        GameObject go = new GameObject($"Section_{text}");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, SECTION_TITLE_HEIGHT);
        go.AddComponent<CanvasGroup>();

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text             = text;
        tmp.font             = _cinzelFont;
        tmp.fontSize         = TITLE_FONT_SIZE;
        tmp.characterSpacing = TITLE_SPACING;
        tmp.alignment        = TextAlignmentOptions.Center;
        tmp.color            = TitleGold;
        tmp.raycastTarget    = false;
    }

    private void CreateRow(Transform parent, RowData row)
    {
        GameObject rowGO = new GameObject($"Row_{row.action}");
        rowGO.transform.SetParent(parent, false);
        RectTransform rowRT = rowGO.AddComponent<RectTransform>();
        rowRT.sizeDelta = new Vector2(0f, ROW_HEIGHT);
        rowGO.AddComponent<CanvasGroup>();

        HorizontalLayoutGroup hlg = rowGO.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing               = 18f;
        hlg.childAlignment        = TextAnchor.MiddleCenter;
        hlg.childControlWidth     = false;
        hlg.childControlHeight    = false;
        hlg.childForceExpandWidth = false;
        hlg.padding               = new RectOffset(0, 0, 2, 2);

        TrackedRow tracked = CreateBadge(rowGO.transform, row);
        tracked.descTMP = CreateTextColumn(rowGO.transform, row);
        _trackedRows.Add(tracked);
    }

    private TrackedRow CreateBadge(Transform parent, RowData row)
    {
        TrackedRow tracked = new TrackedRow();
        Sprite iconSprite = GetBadgeSprite(row.badge);

        GameObject badgeGO = new GameObject("Badge");
        badgeGO.transform.SetParent(parent, false);
        Image badgeImg = badgeGO.AddComponent<Image>();
        badgeImg.raycastTarget = false;
        RectTransform bRT = badgeGO.GetComponent<RectTransform>();
        bRT.sizeDelta = new Vector2(BADGE_SIZE, BADGE_SIZE);
        tracked.badgeImg = badgeImg;

        // Always create text child (shown only when no sprite)
        GameObject badgeLblGO = new GameObject("BadgeTxt");
        badgeLblGO.transform.SetParent(badgeGO.transform, false);
        TextMeshProUGUI badgeTxt = badgeLblGO.AddComponent<TextMeshProUGUI>();
        badgeTxt.font      = _cinzelFont;
        badgeTxt.fontStyle = FontStyles.Bold;
        badgeTxt.alignment = TextAlignmentOptions.Center;
        badgeTxt.raycastTarget = false;
        RectTransform blRT = badgeLblGO.GetComponent<RectTransform>();
        blRT.anchorMin = Vector2.zero; blRT.anchorMax = Vector2.one;
        blRT.offsetMin = Vector2.zero; blRT.offsetMax = Vector2.zero;
        tracked.badgeTxtGO = badgeLblGO;

        if (iconSprite != null)
        {
            badgeImg.sprite = iconSprite;
            badgeImg.preserveAspect = true;
            badgeImg.color = Color.white;
            badgeLblGO.SetActive(false);
        }
        else
        {
            badgeImg.color = row.color;
            badgeTxt.text      = row.badge;
            badgeTxt.fontSize  = row.badge.Length > 3 ? 12f : (row.badge.Length > 2 ? 15f : 24f);
            badgeTxt.color     = IsLight(row.color)
                ? new Color(0.031f, 0.043f, 0.078f, 1f)
                : new Color(0.910f, 0.918f, 0.965f, 1f);
        }

        return tracked;
    }

    private TextMeshProUGUI CreateTextColumn(Transform parent, RowData row)
    {
        GameObject textColGO = new GameObject("TextCol");
        textColGO.transform.SetParent(parent, false);
        VerticalLayoutGroup vlg = textColGO.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment         = TextAnchor.MiddleLeft;
        vlg.childControlHeight     = false;
        vlg.childControlWidth      = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing                = 2f;
        RectTransform tcRT = textColGO.GetComponent<RectTransform>();
        tcRT.sizeDelta = new Vector2(440f, ROW_HEIGHT);

        // Action name
        GameObject actionGO = new GameObject("Action");
        actionGO.transform.SetParent(textColGO.transform, false);
        TextMeshProUGUI actionTxt = actionGO.AddComponent<TextMeshProUGUI>();
        actionTxt.text             = row.action.ToUpper();
        actionTxt.font             = _cinzelFont;
        actionTxt.fontSize         = ACTION_FONT_SIZE;
        actionTxt.characterSpacing = 6f;
        actionTxt.alignment        = TextAlignmentOptions.Left;
        actionTxt.color            = new Color(LabelWhite.r, LabelWhite.g, LabelWhite.b, 0.95f);
        actionTxt.raycastTarget    = false;
        RectTransform aRT = actionGO.GetComponent<RectTransform>();
        aRT.sizeDelta = new Vector2(440f, 34f);

        // Description (tracked for mode changes)
        GameObject descGO = new GameObject("Desc");
        descGO.transform.SetParent(textColGO.transform, false);
        TextMeshProUGUI descTxt = descGO.AddComponent<TextMeshProUGUI>();
        descTxt.text             = row.desc;
        descTxt.font             = _cinzelFont;
        descTxt.fontSize         = DESC_FONT_SIZE;
        descTxt.characterSpacing = 2f;
        descTxt.alignment        = TextAlignmentOptions.Left;
        descTxt.color            = DescDim;
        descTxt.raycastTarget    = false;
        RectTransform dRT = descGO.GetComponent<RectTransform>();
        dRT.sizeDelta = new Vector2(440f, 26f);

        return descTxt;
    }

    private void CreateSpacer(Transform parent, float height)
    {
        GameObject go = new GameObject("Spacer");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, height);
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.minHeight       = height;
        le.preferredHeight = height;
    }

    private void CreateBackButton(Transform parent)
    {
        // Wrapper to center the button within the full-width VLG
        GameObject wrapper = new GameObject("BackButtonWrapper");
        wrapper.transform.SetParent(parent, false);
        RectTransform wrapperRT = wrapper.AddComponent<RectTransform>();
        wrapperRT.sizeDelta = new Vector2(0f, BACK_BTN_HEIGHT);
        wrapper.AddComponent<CanvasGroup>();

        LayoutElement wrapperLE = wrapper.AddComponent<LayoutElement>();
        wrapperLE.preferredHeight = BACK_BTN_HEIGHT;

        // The actual button inside, centered via anchors
        GameObject go = new GameObject("BackButton");
        go.transform.SetParent(wrapper.transform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(BACK_BTN_WIDTH, BACK_BTN_HEIGHT);

        Image bg = go.AddComponent<Image>();
        bg.color = Color.white;
        bg.raycastTarget = true;

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = bg;
        ColorBlock cb       = btn.colors;
        cb.normalColor      = BtnNormal;
        cb.highlightedColor = BtnHover;
        cb.pressedColor     = BtnPress;
        cb.selectedColor    = BtnNormal;
        cb.fadeDuration     = 0.1f;
        btn.colors          = cb;

        // Explicit navigation
        Navigation nav = btn.navigation;
        nav.mode = Navigation.Mode.Automatic;
        btn.navigation = nav;

        btn.onClick.AddListener(() => GoBack());

        // HorizontalLayoutGroup for icon + label — controls both width and height
        HorizontalLayoutGroup hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 12f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.padding = new RectOffset(16, 16, 6, 6);

        // Icon — first child = left side
        Sprite backSprite = InputPromptManager.IsKeyboardMouse ? ControllerIcons.KeyEsc : ControllerIcons.CtrlB;
        GameObject iconGO = new GameObject("BackIcon");
        iconGO.transform.SetParent(go.transform, false);
        _backBtnIcon = iconGO.AddComponent<Image>();
        _backBtnIcon.sprite = backSprite;
        _backBtnIcon.preserveAspect = true;
        _backBtnIcon.raycastTarget = false;
        LayoutElement iconLE = iconGO.AddComponent<LayoutElement>();
        iconLE.preferredWidth = 36f;
        iconLE.preferredHeight = 36f;
        iconLE.minWidth = 36f;
        iconLE.minHeight = 36f;

        // Label — second child = right side
        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        _backBtnLabel = labelGO.AddComponent<TextMeshProUGUI>();
        _backBtnLabel.text             = "BACK";
        _backBtnLabel.font             = _cinzelFont;
        _backBtnLabel.fontSize         = 22f;
        _backBtnLabel.characterSpacing = 6f;
        _backBtnLabel.alignment        = TextAlignmentOptions.Midline;
        _backBtnLabel.color            = LabelWhite;
        _backBtnLabel.raycastTarget    = false;
        LayoutElement lblLE = labelGO.AddComponent<LayoutElement>();
        lblLE.preferredWidth = 80f;
        lblLE.preferredHeight = 36f;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool IsLight(Color c) =>
        (c.r * 0.299f + c.g * 0.587f + c.b * 0.114f) > 0.50f;
}
