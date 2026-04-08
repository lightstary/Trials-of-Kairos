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
    private static readonly Color BtnNormal  = new Color(0.059f, 0.102f, 0.188f, 0.80f);
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

    private static readonly RowData[] MovementRows =
    {
        new RowData { badge = "LS",   color = Slate,  action = "Move",       desc = "Left stick \u2014 move in all directions"   },
        new RowData { badge = "RS",   color = Slate,  action = "Look / Aim", desc = "Right stick \u2014 rotate camera"           },
    };

    private static readonly RowData[] TimeMechanicsRows =
    {
        new RowData { badge = "RT",    color = Gold,   action = "Time Forward",  desc = "Hold RT \u2014 sand flows, world advances"     },
        new RowData { badge = "LT",    color = Blue,   action = "Time Frozen",   desc = "Hold LT \u2014 motion arrested, world holds"   },
        new RowData { badge = "RT+LT", color = Purple, action = "Time Reversed", desc = "Hold both \u2014 time recedes, world unravels" },
    };

    private static readonly RowData[] ActionRows =
    {
        new RowData { badge = "A", color = Green, action = "Jump / Confirm",  desc = "Jump in world or confirm UI selection"          },
        new RowData { badge = "B", color = Red,   action = "Dash / Cancel",   desc = "Quick dash or cancel a selection"               },
        new RowData { badge = "X", color = XBlue, action = "Interact / Grab", desc = "Interact with objects or grab surfaces"         },
        new RowData { badge = "Y", color = Gold,  action = "Time Pulse",      desc = "Active ability \u2014 burst of time energy"     },
    };

    private static readonly RowData[] SystemRows =
    {
        new RowData { badge = "MENU", color = Slate, action = "Pause", desc = "Menu button \u2014 open pause screen" },
    };

    // ── Constants ────────────────────────────────────────────────────────────
    private const float ROW_HEIGHT          = 52f;
    private const float SECTION_TITLE_HEIGHT = 30f;
    private const float SPACER_HEIGHT       = 14f;
    private const float BADGE_SIZE          = 44f;
    private const float ACTION_FONT_SIZE    = 15f;
    private const float DESC_FONT_SIZE      = 11f;
    private const float TITLE_FONT_SIZE     = 12f;
    private const float TITLE_SPACING       = 18f;
    private const float BACK_BTN_WIDTH      = 200f;
    private const float BACK_BTN_HEIGHT     = 48f;
    private const float FADE_STAGGER        = 0.04f;
    private const float FADE_DURATION       = 0.35f;

    // ── State ────────────────────────────────────────────────────────────────
    private bool _built;
    private GameObject _layoutRoot;
    private TMP_FontAsset _cinzelFont;
    private ScrollRect _scrollRect;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    void OnEnable()
    {
        if (!_built) BuildLayout();
        StartCoroutine(PlayEntranceAnimation());
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
        _cinzelFont = FindCinzelFont();

        // Hide existing Card
        if (existingCard == null)
        {
            Transform card = transform.Find("Card");
            if (card != null) existingCard = card.gameObject;
        }
        if (existingCard != null) existingCard.SetActive(false);

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
        rootVLG.padding                = new RectOffset(0, 0, 8, 20);

        ContentSizeFitter csf = _layoutRoot.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _scrollRect.content  = rootRT;
        _scrollRect.viewport = vpRT;

        // ── Screen title ─────────────────────────────────────────────────
        CreateScreenTitle(_layoutRoot.transform, "CONTROLS", 40f);
        CreateSpacer(_layoutRoot.transform, 6f);

        // ── MOVEMENT ─────────────────────────────────────────────────────
        CreateSectionTitle(_layoutRoot.transform, "MOVEMENT");
        foreach (RowData row in MovementRows)
            CreateRow(_layoutRoot.transform, row);

        CreateSpacer(_layoutRoot.transform, SPACER_HEIGHT);

        // ── TIME MECHANICS ───────────────────────────────────────────────
        CreateSectionTitle(_layoutRoot.transform, "TIME MECHANICS");
        foreach (RowData row in TimeMechanicsRows)
            CreateRow(_layoutRoot.transform, row);

        CreateSpacer(_layoutRoot.transform, SPACER_HEIGHT);

        // ── ACTIONS ──────────────────────────────────────────────────────
        CreateSectionTitle(_layoutRoot.transform, "ACTIONS");
        foreach (RowData row in ActionRows)
            CreateRow(_layoutRoot.transform, row);

        CreateSpacer(_layoutRoot.transform, SPACER_HEIGHT);

        // ── SYSTEM ───────────────────────────────────────────────────────
        CreateSectionTitle(_layoutRoot.transform, "SYSTEM");
        foreach (RowData row in SystemRows)
            CreateRow(_layoutRoot.transform, row);

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
        tmp.fontSize         = 22f;
        tmp.characterSpacing = 24f;
        tmp.alignment        = TextAlignmentOptions.Center;
        tmp.color            = LabelWhite;
        tmp.raycastTarget    = false;
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
        hlg.spacing               = 16f;
        hlg.childAlignment        = TextAnchor.MiddleCenter;
        hlg.childControlWidth     = false;
        hlg.childControlHeight    = false;
        hlg.childForceExpandWidth = false;
        hlg.padding               = new RectOffset(0, 0, 2, 2);

        // Badge
        CreateBadge(rowGO.transform, row);

        // Text column with action + description stacked
        CreateTextColumn(rowGO.transform, row);
    }

    private void CreateBadge(Transform parent, RowData row)
    {
        GameObject badgeGO = new GameObject("Badge");
        badgeGO.transform.SetParent(parent, false);
        Image badgeImg    = badgeGO.AddComponent<Image>();
        badgeImg.color    = row.color;
        badgeImg.raycastTarget = false;
        RectTransform bRT = badgeGO.GetComponent<RectTransform>();
        bRT.sizeDelta     = new Vector2(BADGE_SIZE, BADGE_SIZE);

        // Badge label
        GameObject badgeLblGO = new GameObject("BadgeTxt");
        badgeLblGO.transform.SetParent(badgeGO.transform, false);
        TextMeshProUGUI badgeTxt = badgeLblGO.AddComponent<TextMeshProUGUI>();
        badgeTxt.text      = row.badge;
        badgeTxt.font      = _cinzelFont;
        badgeTxt.fontSize  = row.badge.Length > 2 ? 9f : 15f;
        badgeTxt.fontStyle = FontStyles.Bold;
        badgeTxt.alignment = TextAlignmentOptions.Center;
        badgeTxt.color     = IsLight(row.color)
            ? new Color(0.031f, 0.043f, 0.078f, 1f)
            : new Color(0.910f, 0.918f, 0.965f, 1f);
        badgeTxt.raycastTarget = false;
        RectTransform blRT = badgeLblGO.GetComponent<RectTransform>();
        blRT.anchorMin = Vector2.zero; blRT.anchorMax = Vector2.one;
        blRT.offsetMin = Vector2.zero; blRT.offsetMax = Vector2.zero;
    }

    private void CreateTextColumn(Transform parent, RowData row)
    {
        GameObject textColGO = new GameObject("TextCol");
        textColGO.transform.SetParent(parent, false);
        VerticalLayoutGroup vlg = textColGO.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment         = TextAnchor.MiddleLeft;
        vlg.childControlHeight     = false;
        vlg.childControlWidth      = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing                = 1f;
        RectTransform tcRT = textColGO.GetComponent<RectTransform>();
        tcRT.sizeDelta = new Vector2(380f, ROW_HEIGHT);

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
        aRT.sizeDelta = new Vector2(380f, 26f);

        // Description
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
        dRT.sizeDelta = new Vector2(380f, 18f);
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
        GameObject go = new GameObject("BackButton");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(BACK_BTN_WIDTH, BACK_BTN_HEIGHT);
        go.AddComponent<CanvasGroup>();

        LayoutElement le   = go.AddComponent<LayoutElement>();
        le.preferredWidth  = BACK_BTN_WIDTH;
        le.preferredHeight = BACK_BTN_HEIGHT;

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

        // Label
        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        TextMeshProUGUI label = labelGO.AddComponent<TextMeshProUGUI>();
        label.text             = "[ B ]  BACK";
        label.font             = _cinzelFont;
        label.fontSize         = 14f;
        label.characterSpacing = 6f;
        label.alignment        = TextAlignmentOptions.Center;
        label.color            = new Color(LabelWhite.r, LabelWhite.g, LabelWhite.b, 0.90f);
        label.raycastTarget    = false;

        RectTransform lrt = labelGO.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool IsLight(Color c) =>
        (c.r * 0.299f + c.g * 0.587f + c.b * 0.114f) > 0.50f;
}
