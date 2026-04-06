using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the controls reference screen. Hides the existing Card hierarchy
/// and rebuilds a centered, vertically-stacked layout at runtime.
/// Supports origin-aware back navigation (MainMenu vs PauseMenu).
/// </summary>
public class ControlsScreenController : MonoBehaviour
{
    [Header("Existing References (auto-discovered if null)")]
    [SerializeField] private GameObject existingCard;

    /// <summary>Where this screen was opened from.</summary>
    public enum ControlsOrigin { MainMenu, PauseMenu }

    /// <summary>Set before enabling to control where B/back returns to.</summary>
    public ControlsOrigin Origin { get; set; } = ControlsOrigin.MainMenu;

    // ── Row definitions ──────────────────────────────────────────────────────
    private static readonly Color Gold   = new Color(0.961f, 0.784f, 0.259f, 1f);
    private static readonly Color Blue   = new Color(0.353f, 0.706f, 0.941f, 1f);
    private static readonly Color Purple = new Color(0.608f, 0.365f, 0.898f, 1f);
    private static readonly Color Green  = new Color(0.271f, 0.761f, 0.275f, 1f);
    private static readonly Color Red    = new Color(0.898f, 0.196f, 0.106f, 1f);
    private static readonly Color XBlue  = new Color(0.224f, 0.478f, 0.918f, 1f);
    private static readonly Color Panel  = new Color(0.118f, 0.196f, 0.353f, 1f);
    private static readonly Color LabelWhite = new Color(0.91f, 0.918f, 0.965f, 1f);

    private struct RowData
    {
        public string badge;
        public Color  color;
        public string action;
        public string desc;
    }

    private static readonly RowData[] MovementRows =
    {
        new RowData { badge = "LS",   color = Panel, action = "Move",       desc = "Left stick \u2014 move in all directions"   },
        new RowData { badge = "RS",   color = Panel, action = "Look / Aim", desc = "Right stick \u2014 rotate camera"           },
        new RowData { badge = "MENU", color = Panel, action = "Pause",      desc = "Menu button \u2014 open pause screen"       },
    };

    private static readonly RowData[] MechanicsRows =
    {
        new RowData { badge = "RT",    color = Gold,   action = "Time Forward",    desc = "Hold RT \u2014 sand flows, world advances"     },
        new RowData { badge = "LT",    color = Blue,   action = "Time Frozen",     desc = "Hold LT \u2014 motion arrested, world holds"   },
        new RowData { badge = "RT+LT", color = Purple, action = "Time Reversed",   desc = "Hold both \u2014 time recedes, world unravels" },
        new RowData { badge = "A",     color = Green,  action = "Jump / Confirm",  desc = "Jump in world or confirm UI selection"          },
        new RowData { badge = "B",     color = Red,    action = "Dash / Cancel",   desc = "Quick dash or cancel a selection"               },
        new RowData { badge = "X",     color = XBlue,  action = "Interact / Grab", desc = "Interact with objects or grab surfaces"         },
        new RowData { badge = "Y",     color = Gold,   action = "Time Pulse",      desc = "Active ability \u2014 burst of time energy"     },
    };

    // ── State ────────────────────────────────────────────────────────────────
    private bool _built;
    private GameObject _layoutRoot;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    void OnEnable()
    {
        if (!_built) BuildLayout();
    }

    void Update()
    {
        // B button / Escape = go back
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton1))
        {
            GoBack();
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

    // ── Layout builder ───────────────────────────────────────────────────────

    private void BuildLayout()
    {
        _built = true;

        // Hide the existing Card
        if (existingCard == null)
        {
            Transform card = transform.Find("Card");
            if (card != null) existingCard = card.gameObject;
        }
        if (existingCard != null) existingCard.SetActive(false);

        // Create a layout root
        _layoutRoot = new GameObject("ControlsLayout");
        _layoutRoot.transform.SetParent(transform, false);
        RectTransform rootRT = _layoutRoot.AddComponent<RectTransform>();
        rootRT.anchorMin = new Vector2(0.12f, 0.06f);
        rootRT.anchorMax = new Vector2(0.88f, 0.94f);
        rootRT.offsetMin = Vector2.zero;
        rootRT.offsetMax = Vector2.zero;

        VerticalLayoutGroup rootVLG = _layoutRoot.AddComponent<VerticalLayoutGroup>();
        rootVLG.spacing                 = 8f;
        rootVLG.childAlignment          = TextAnchor.UpperCenter;
        rootVLG.childControlWidth       = true;
        rootVLG.childControlHeight      = false;
        rootVLG.childForceExpandWidth   = true;
        rootVLG.childForceExpandHeight  = false;
        rootVLG.padding                 = new RectOffset(20, 20, 10, 10);

        // ── MOVEMENT section ──────────────────────────────────────────────
        CreateSectionTitle(_layoutRoot.transform, "MOVEMENT", 34f);
        foreach (RowData row in MovementRows)
            CreateRow(_layoutRoot.transform, row);

        CreateSpacer(_layoutRoot.transform, 18f);

        // ── TIME MECHANICS AND ACTIONS section ────────────────────────────
        CreateSectionTitle(_layoutRoot.transform, "TIME MECHANICS AND ACTIONS", 34f);
        foreach (RowData row in MechanicsRows)
            CreateRow(_layoutRoot.transform, row);

        CreateSpacer(_layoutRoot.transform, 20f);

        // ── Back button ──────────────────────────────────────────────────
        CreateBackButton(_layoutRoot.transform);
    }

    private void CreateSectionTitle(Transform parent, string text, float height)
    {
        GameObject go = new GameObject($"Title_{text}");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, height);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text             = text;
        tmp.fontSize         = 13f;
        tmp.characterSpacing = 16f;
        tmp.alignment        = TextAlignmentOptions.Center;
        tmp.color            = new Color(1f, 0.843f, 0f, 0.85f);
        tmp.raycastTarget    = false;
    }

    private void CreateRow(Transform parent, RowData row)
    {
        GameObject rowGO = new GameObject($"Row_{row.action}");
        rowGO.transform.SetParent(parent, false);
        HorizontalLayoutGroup hlg = rowGO.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing               = 12f;
        hlg.childAlignment        = TextAnchor.MiddleCenter;
        hlg.childControlWidth     = false;
        hlg.childControlHeight    = false;
        hlg.childForceExpandWidth = false;
        hlg.padding               = new RectOffset(0, 0, 3, 3);
        RectTransform rowRT = rowGO.GetComponent<RectTransform>();
        rowRT.sizeDelta = new Vector2(0f, 48f);

        // Badge
        GameObject badgeGO = new GameObject("Badge");
        badgeGO.transform.SetParent(rowGO.transform, false);
        Image badgeImg    = badgeGO.AddComponent<Image>();
        badgeImg.color    = row.color;
        RectTransform bRT = badgeGO.GetComponent<RectTransform>();
        bRT.sizeDelta     = new Vector2(42f, 42f);

        // Badge label
        GameObject badgeLblGO = new GameObject("BadgeTxt");
        badgeLblGO.transform.SetParent(badgeGO.transform, false);
        TextMeshProUGUI badgeTxt = badgeLblGO.AddComponent<TextMeshProUGUI>();
        badgeTxt.text      = row.badge;
        badgeTxt.fontSize  = row.badge.Length > 2 ? 8f : 14f;
        badgeTxt.alignment = TextAlignmentOptions.Center;
        badgeTxt.color     = IsLight(row.color)
            ? new Color(0.031f, 0.043f, 0.078f, 1f)
            : new Color(0.910f, 0.918f, 0.965f, 1f);
        RectTransform blRT = badgeLblGO.GetComponent<RectTransform>();
        blRT.anchorMin = Vector2.zero; blRT.anchorMax = Vector2.one;
        blRT.offsetMin = Vector2.zero; blRT.offsetMax = Vector2.zero;

        // Text column
        GameObject textColGO = new GameObject("TextCol");
        textColGO.transform.SetParent(rowGO.transform, false);
        VerticalLayoutGroup vlg = textColGO.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment         = TextAnchor.MiddleCenter;
        vlg.childControlHeight     = false;
        vlg.childControlWidth      = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing                = 0f;
        RectTransform tcRT = textColGO.GetComponent<RectTransform>();
        tcRT.sizeDelta = new Vector2(340f, 48f);

        // Action name
        GameObject actionGO = new GameObject("Action");
        actionGO.transform.SetParent(textColGO.transform, false);
        TextMeshProUGUI actionTxt = actionGO.AddComponent<TextMeshProUGUI>();
        actionTxt.text             = row.action.ToUpper();
        actionTxt.fontSize         = 13f;
        actionTxt.characterSpacing = 8f;
        actionTxt.alignment        = TextAlignmentOptions.Center;
        actionTxt.color            = new Color(0.910f, 0.918f, 0.965f, 0.92f);
        RectTransform aRT = actionGO.GetComponent<RectTransform>();
        aRT.sizeDelta = new Vector2(340f, 24f);

        // Description
        GameObject descGO = new GameObject("Desc");
        descGO.transform.SetParent(textColGO.transform, false);
        TextMeshProUGUI descTxt = descGO.AddComponent<TextMeshProUGUI>();
        descTxt.text             = row.desc;
        descTxt.fontSize         = 10f;
        descTxt.characterSpacing = 3f;
        descTxt.alignment        = TextAlignmentOptions.Center;
        descTxt.color            = new Color(0.910f, 0.918f, 0.965f, 0.38f);
        RectTransform dRT = descGO.GetComponent<RectTransform>();
        dRT.sizeDelta = new Vector2(340f, 16f);
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
        rt.sizeDelta = new Vector2(180f, 46f);

        LayoutElement le   = go.AddComponent<LayoutElement>();
        le.preferredWidth  = 180f;
        le.preferredHeight = 46f;

        Image bg       = go.AddComponent<Image>();
        bg.color       = Color.white;
        bg.raycastTarget = true;

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = bg;
        ColorBlock cb       = btn.colors;
        cb.normalColor      = new Color(0.059f, 0.102f, 0.188f, 0.80f);
        cb.highlightedColor = new Color(0.961f, 0.784f, 0.259f, 1f);
        cb.pressedColor     = new Color(0.7f, 0.55f, 0.1f, 1f);
        cb.selectedColor    = new Color(0.059f, 0.102f, 0.188f, 0.80f);
        cb.fadeDuration     = 0.1f;
        btn.colors          = cb;

        btn.onClick.AddListener(() => GoBack());

        // Label
        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        TextMeshProUGUI label = labelGO.AddComponent<TextMeshProUGUI>();
        label.text             = "[ B ]  BACK";
        label.fontSize         = 14f;
        label.characterSpacing = 6f;
        label.alignment        = TextAlignmentOptions.Center;
        label.color            = new Color(0.91f, 0.918f, 0.965f, 0.90f);
        label.raycastTarget    = false;

        RectTransform lrt = labelGO.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool IsLight(Color c) =>
        (c.r * 0.299f + c.g * 0.587f + c.b * 0.114f) > 0.50f;
}
