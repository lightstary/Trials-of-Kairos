using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Full-screen How To Play tutorial screen with three pages:
/// Movement, Time States, and Goal. Procedurally built UI using Cinzel font.
/// Controller-friendly: A = next page, B = back/exit.
/// Features animated color pulses and smooth page transitions.
/// </summary>
public class HowToPlayController : MonoBehaviour
{
    /// <summary>Where this screen was opened from.</summary>
    public enum HTPOrigin { HubFirstVisit, PauseMenu }

    /// <summary>Set before Show() to control exit behavior.</summary>
    public HTPOrigin Origin { get; set; } = HTPOrigin.HubFirstVisit;

    /// <summary>True when the screen is currently visible.</summary>
    public bool IsOpen => _root != null && _root.activeSelf;

    /// <summary>Static check for any HowToPlayController instance being open.</summary>
    public static bool IsAnyOpen { get; private set; }

    // ── Colors ───────────────────────────────────────────────────────────
    private static readonly Color BG_COLOR      = new Color(0.02f, 0.025f, 0.05f, 0.96f);
    private static readonly Color TITLE_GOLD    = new Color(1f, 0.843f, 0f, 1f);
    private static readonly Color HEADER_WHITE  = new Color(0.91f, 0.918f, 0.965f, 1f);
    private static readonly Color BODY_DIM      = new Color(0.91f, 0.918f, 0.965f, 0.65f);
    private static readonly Color HINT_DIM      = new Color(0.91f, 0.918f, 0.965f, 0.35f);
    private static readonly Color FORWARD_COLOR = new Color(0.961f, 0.784f, 0.259f, 1f);
    private static readonly Color FROZEN_COLOR  = new Color(0.353f, 0.706f, 0.941f, 1f);
    private static readonly Color REVERSE_COLOR = new Color(0.608f, 0.365f, 0.898f, 1f);
    private static readonly Color CARD_BG       = new Color(0.04f, 0.05f, 0.09f, 0.85f);
    private static readonly Color DIVIDER_COLOR = new Color(0.91f, 0.918f, 0.965f, 0.12f);

    // ── Layout ───────────────────────────────────────────────────────────
    private const float PAGE_TRANSITION = 0.3f;
    private const float FADE_IN_TIME    = 0.4f;
    private const float GLOW_SPEED      = 2.5f;

    // ── State ────────────────────────────────────────────────────────────
    private GameObject       _root;
    private CanvasGroup      _rootCG;
    private List<GameObject> _pages = new List<GameObject>();
    private int              _currentPage;
    private bool             _transitioning;
    private bool             _built;
    private TMP_FontAsset    _cinzelFont;
    private TMP_FontAsset    _cinzelBold;

    // Animated elements
    private Image            _forwardGlow, _frozenGlow, _reverseGlow;
    private Image            _cubeStandingImg, _cubeFlatImg, _cubeFlippedImg;
    private TextMeshProUGUI  _pageCounter;

    // Bottom bar icon hint containers (one per mode, toggled on swap)
    private GameObject       _kbmNavHints;
    private GameObject       _ctrlNavHints;
    private TextMeshProUGUI  _kbmConfirmLabel;
    private TextMeshProUGUI  _kbmBackLabel;
    private TextMeshProUGUI  _ctrlConfirmLabel;
    private TextMeshProUGUI  _ctrlBackLabel;

    // ── Public API ───────────────────────────────────────────────────────

    /// <summary>Shows the How To Play screen.</summary>
    public void Show()
    {
        if (!_built) Build();
        _currentPage = 0;
        for (int i = 0; i < _pages.Count; i++)
            _pages[i].SetActive(i == 0);
        UpdatePageCounter();
        _root.SetActive(true);
        IsAnyOpen = true;
        Time.timeScale = 0f;
        StartCoroutine(FadeIn());
    }

    /// <summary>Hides the How To Play screen.</summary>
    public void Hide()
    {
        StartCoroutine(FadeOutAndClose());
    }

    // ── MonoBehaviour ────────────────────────────────────────────────────

    void OnEnable()
    {
        InputPromptManager.OnInputModeChanged += OnInputModeChanged;
    }

    void OnDisable()
    {
        InputPromptManager.OnInputModeChanged -= OnInputModeChanged;
    }

    private void OnInputModeChanged(InputPromptManager.InputMode newMode)
    {
        RefreshNavHints();
    }

    void Update()
    {
        if (!IsOpen) return;

        // Animate glow pulses on time state indicators
        AnimateGlows();

        // Controller + keyboard input
        if (_transitioning) return;

        bool nextPressed = Input.GetKeyDown(KeyCode.JoystickButton0)
                        || Input.GetKeyDown(KeyCode.Return)
                        || Input.GetKeyDown(KeyCode.Space)
                        || Input.GetKeyDown(KeyCode.RightArrow)
                        || Input.GetKeyDown(KeyCode.D);

        bool backPressed = Input.GetKeyDown(KeyCode.JoystickButton1)
                        || Input.GetKeyDown(KeyCode.Escape)
                        || Input.GetKeyDown(KeyCode.Backspace)
                        || Input.GetKeyDown(KeyCode.LeftArrow)
                        || Input.GetKeyDown(KeyCode.A);

        if (nextPressed)
        {
            if (_currentPage < _pages.Count - 1)
                StartCoroutine(TransitionToPage(_currentPage + 1));
            else
                Hide();
        }
        else if (backPressed)
        {
            if (_currentPage > 0)
                StartCoroutine(TransitionToPage(_currentPage - 1));
            else
                Hide();
        }
    }

    // ── Build ────────────────────────────────────────────────────────────

    private void Build()
    {
        _built = true;
        FindFonts();

        // Root overlay
        _root = new GameObject("HowToPlayRoot");
        _root.transform.SetParent(transform, false);
        RectTransform rootRT = _root.AddComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero; rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = Vector2.zero; rootRT.offsetMax = Vector2.zero;
        _rootCG = _root.AddComponent<CanvasGroup>();
        _rootCG.alpha = 0f;

        // Dark background
        Image bg = _root.AddComponent<Image>();
        bg.color = BG_COLOR;
        bg.raycastTarget = true;

        // Page container
        GameObject pageContainer = new GameObject("Pages");
        pageContainer.transform.SetParent(_root.transform, false);
        RectTransform pcRT = pageContainer.AddComponent<RectTransform>();
        pcRT.anchorMin = Vector2.zero; pcRT.anchorMax = Vector2.one;
        pcRT.offsetMin = new Vector2(0, 60f); pcRT.offsetMax = new Vector2(0, -20f);

        // Build pages
        _pages.Add(BuildMovementPage(pageContainer.transform));
        _pages.Add(BuildTimeStatesPage(pageContainer.transform));
        _pages.Add(BuildGoalPage(pageContainer.transform));

        // Page counter + nav hint at bottom
        BuildBottomBar(_root.transform);

        _root.SetActive(false);
    }

    // ── PAGE 1: MOVEMENT ─────────────────────────────────────────────────

    private GameObject BuildMovementPage(Transform parent)
    {
        GameObject page = CreatePage(parent, "MovementPage");
        RectTransform content = page.GetComponent<RectTransform>();

        // Two-column layout
        GameObject leftCol  = CreateColumn(page.transform, "LeftCol",  0f, 0.45f);
        GameObject rightCol = CreateColumn(page.transform, "RightCol", 0.48f, 1f);

        // ── Left: Visual diagram ──
        // Joystick icon (text-based)
        CreateLabel(leftCol.transform, "JOYSTICK_ICON", "\u25CE", 80f,
            HEADER_WHITE, TextAlignmentOptions.Center, new Vector2(0, 0.65f), new Vector2(1, 0.95f));

        CreateLabel(leftCol.transform, "StickLabel", "LEFT STICK", 18f,
            HINT_DIM, TextAlignmentOptions.Center, new Vector2(0, 0.56f), new Vector2(1, 0.64f));

        // Direction arrows diagram
        CreateLabel(leftCol.transform, "ArrowUp", "\u25B2", 28f,
            FORWARD_COLOR, TextAlignmentOptions.Center, new Vector2(0.35f, 0.42f), new Vector2(0.65f, 0.52f));
        CreateLabel(leftCol.transform, "ArrowDown", "\u25BC", 28f,
            FORWARD_COLOR, TextAlignmentOptions.Center, new Vector2(0.35f, 0.18f), new Vector2(0.65f, 0.28f));
        CreateLabel(leftCol.transform, "ArrowLeft", "\u25C0", 28f,
            FORWARD_COLOR, TextAlignmentOptions.Center, new Vector2(0.12f, 0.30f), new Vector2(0.38f, 0.40f));
        CreateLabel(leftCol.transform, "ArrowRight", "\u25B6", 28f,
            FORWARD_COLOR, TextAlignmentOptions.Center, new Vector2(0.62f, 0.30f), new Vector2(0.88f, 0.40f));

        // Center cube icon
        CreateCubeVisual(leftCol.transform, new Vector2(0.35f, 0.28f), new Vector2(0.65f, 0.44f),
            FORWARD_COLOR, "StandingCube");

        // ── Right: Explanation text ──
        CreateLabel(rightCol.transform, "Title", "MOVEMENT", 40f,
            TITLE_GOLD, TextAlignmentOptions.Left, new Vector2(0, 0.82f), new Vector2(1, 0.95f), true);

        CreateDivider(rightCol.transform, 0.78f, 0.80f);

        CreateLabel(rightCol.transform, "Body1", "Your character is a cube that rolls\nacross a grid of tiles.", 22f,
            HEADER_WHITE, TextAlignmentOptions.Left, new Vector2(0, 0.62f), new Vector2(1, 0.78f));

        CreateLabel(rightCol.transform, "Body2",
            "Use the left stick or D-pad to roll\nin four directions.\n\n" +
            "Each roll moves exactly one tile.\nThe cube physically rotates as it rolls,\nchanging which face points upward.", 19f,
            BODY_DIM, TextAlignmentOptions.Left, new Vector2(0, 0.28f), new Vector2(1, 0.62f));

        CreateLabel(rightCol.transform, "Hint",
            "The orientation of your cube determines\nthe flow of time.", 18f,
            FORWARD_COLOR, TextAlignmentOptions.Left, new Vector2(0, 0.12f), new Vector2(1, 0.28f));

        return page;
    }

    // ── PAGE 2: TIME STATES ──────────────────────────────────────────────

    private GameObject BuildTimeStatesPage(Transform parent)
    {
        GameObject page = CreatePage(parent, "TimeStatesPage");

        // Two-column layout
        GameObject leftCol  = CreateColumn(page.transform, "LeftCol",  0f, 0.42f);
        GameObject rightCol = CreateColumn(page.transform, "RightCol", 0.45f, 1f);

        // ── Left: Three state cards ──
        float cardH = 0.28f;
        float gap   = 0.04f;

        // Forward card
        float y1Top = 0.96f;
        float y1Bot = y1Top - cardH;
        _forwardGlow = CreateStateCard(leftCol.transform, "ForwardCard",
            y1Bot, y1Top, FORWARD_COLOR, "\u2191", "UPRIGHT");
        _cubeStandingImg = CreateCubeIcon(leftCol.transform,
            new Vector2(0.6f, y1Bot + 0.02f), new Vector2(0.9f, y1Top - 0.02f), FORWARD_COLOR);

        // Frozen card
        float y2Top = y1Bot - gap;
        float y2Bot = y2Top - cardH;
        _frozenGlow = CreateStateCard(leftCol.transform, "FrozenCard",
            y2Bot, y2Top, FROZEN_COLOR, "\u2194", "ON SIDE");
        _cubeFlatImg = CreateCubeIcon(leftCol.transform,
            new Vector2(0.6f, y2Bot + 0.02f), new Vector2(0.95f, y2Top - 0.02f), FROZEN_COLOR);

        // Reverse card
        float y3Top = y2Bot - gap;
        float y3Bot = y3Top - cardH;
        _reverseGlow = CreateStateCard(leftCol.transform, "ReverseCard",
            y3Bot, y3Top, REVERSE_COLOR, "\u2193", "UPSIDE DOWN");
        _cubeFlippedImg = CreateCubeIcon(leftCol.transform,
            new Vector2(0.6f, y3Bot + 0.02f), new Vector2(0.9f, y3Top - 0.02f), REVERSE_COLOR);

        // ── Right: Explanation text ──
        CreateLabel(rightCol.transform, "Title", "TIME STATES", 40f,
            TITLE_GOLD, TextAlignmentOptions.Left, new Vector2(0, 0.82f), new Vector2(1, 0.95f), true);

        CreateDivider(rightCol.transform, 0.78f, 0.80f);

        CreateLabel(rightCol.transform, "Intro",
            "Your cube's orientation controls the\nflow of time in the world.", 22f,
            HEADER_WHITE, TextAlignmentOptions.Left, new Vector2(0, 0.66f), new Vector2(1, 0.78f));

        // Forward section
        CreateLabel(rightCol.transform, "FwdLabel", "FORWARD", 24f,
            FORWARD_COLOR, TextAlignmentOptions.Left, new Vector2(0, 0.56f), new Vector2(1, 0.64f), true);
        CreateLabel(rightCol.transform, "FwdDesc",
            "Cube upright \u2014 time moves forward.\nPlatforms shift, hazards activate.", 18f,
            BODY_DIM, TextAlignmentOptions.Left, new Vector2(0, 0.46f), new Vector2(1, 0.56f));

        // Frozen section
        CreateLabel(rightCol.transform, "FrzLabel", "FROZEN", 24f,
            FROZEN_COLOR, TextAlignmentOptions.Left, new Vector2(0, 0.38f), new Vector2(1, 0.46f), true);
        CreateLabel(rightCol.transform, "FrzDesc",
            "Cube on its side \u2014 time stops.\nEverything freezes in place.", 18f,
            BODY_DIM, TextAlignmentOptions.Left, new Vector2(0, 0.28f), new Vector2(1, 0.38f));

        // Reverse section
        CreateLabel(rightCol.transform, "RevLabel", "REVERSE", 24f,
            REVERSE_COLOR, TextAlignmentOptions.Left, new Vector2(0, 0.20f), new Vector2(1, 0.28f), true);
        CreateLabel(rightCol.transform, "RevDesc",
            "Cube upside down \u2014 time reverses.\nPlatforms return, effects rewind.", 18f,
            BODY_DIM, TextAlignmentOptions.Left, new Vector2(0, 0.10f), new Vector2(1, 0.20f));

        return page;
    }

    // ── PAGE 3: GOAL ─────────────────────────────────────────────────────

    private GameObject BuildGoalPage(Transform parent)
    {
        GameObject page = CreatePage(parent, "GoalPage");

        GameObject leftCol  = CreateColumn(page.transform, "LeftCol",  0f, 0.42f);
        GameObject rightCol = CreateColumn(page.transform, "RightCol", 0.45f, 1f);

        // ── Left: Goal visual ──
        // Large diamond / goal icon
        CreateLabel(leftCol.transform, "GoalIcon", "\u25C6", 100f,
            TITLE_GOLD, TextAlignmentOptions.Center, new Vector2(0.1f, 0.50f), new Vector2(0.9f, 0.85f));

        // Hazard warning icon
        CreateLabel(leftCol.transform, "HazardIcon", "\u26A0", 40f,
            new Color(0.898f, 0.196f, 0.106f), TextAlignmentOptions.Center,
            new Vector2(0.1f, 0.22f), new Vector2(0.45f, 0.42f));

        // Clock icon (time puzzle)
        CreateLabel(leftCol.transform, "ClockIcon", "\u231A", 40f,
            FROZEN_COLOR, TextAlignmentOptions.Center,
            new Vector2(0.55f, 0.22f), new Vector2(0.9f, 0.42f));

        CreateLabel(leftCol.transform, "HazardLbl", "HAZARDS", 14f,
            new Color(0.898f, 0.196f, 0.106f, 0.7f), TextAlignmentOptions.Center,
            new Vector2(0.1f, 0.16f), new Vector2(0.45f, 0.24f));

        CreateLabel(leftCol.transform, "PuzzleLbl", "PUZZLES", 14f,
            new Color(FROZEN_COLOR.r, FROZEN_COLOR.g, FROZEN_COLOR.b, 0.7f), TextAlignmentOptions.Center,
            new Vector2(0.55f, 0.16f), new Vector2(0.9f, 0.24f));

        // ── Right: Explanation ──
        CreateLabel(rightCol.transform, "Title", "YOUR GOAL", 40f,
            TITLE_GOLD, TextAlignmentOptions.Left, new Vector2(0, 0.82f), new Vector2(1, 0.95f), true);

        CreateDivider(rightCol.transform, 0.78f, 0.80f);

        CreateLabel(rightCol.transform, "Body1",
            "Reach the golden goal tile to\ncomplete each trial.", 22f,
            HEADER_WHITE, TextAlignmentOptions.Left, new Vector2(0, 0.64f), new Vector2(1, 0.78f));

        CreateLabel(rightCol.transform, "Body2",
            "Along the way you will face:", 19f,
            BODY_DIM, TextAlignmentOptions.Left, new Vector2(0, 0.56f), new Vector2(1, 0.64f));

        CreateLabel(rightCol.transform, "Bullet1",
            "\u25B8  Moving platforms that shift with time", 19f,
            BODY_DIM, TextAlignmentOptions.Left, new Vector2(0, 0.48f), new Vector2(1, 0.56f));

        CreateLabel(rightCol.transform, "Bullet2",
            "\u25B8  Hazards that activate in certain states", 19f,
            BODY_DIM, TextAlignmentOptions.Left, new Vector2(0, 0.40f), new Vector2(1, 0.48f));

        CreateLabel(rightCol.transform, "Bullet3",
            "\u25B8  Puzzles that require switching time", 19f,
            BODY_DIM, TextAlignmentOptions.Left, new Vector2(0, 0.32f), new Vector2(1, 0.40f));

        CreateLabel(rightCol.transform, "Tip",
            "Master the relationship between your\ncube's orientation and time to survive\neach trial.", 19f,
            FORWARD_COLOR, TextAlignmentOptions.Left, new Vector2(0, 0.12f), new Vector2(1, 0.30f));

        return page;
    }

    // ── Bottom Bar ───────────────────────────────────────────────────────

    private void BuildBottomBar(Transform parent)
    {
        GameObject bar = new GameObject("BottomBar");
        bar.transform.SetParent(parent, false);
        RectTransform barRT = bar.AddComponent<RectTransform>();
        barRT.anchorMin = new Vector2(0, 0); barRT.anchorMax = new Vector2(1, 0);
        barRT.offsetMin = new Vector2(40, 10); barRT.offsetMax = new Vector2(-40, 55);

        // Page counter: "1 / 3"
        _pageCounter = CreateTextElement(bar.transform, "PageCounter", "1 / 3", 24f,
            HINT_DIM, TextAlignmentOptions.Center, new Vector2(0.4f, 0), new Vector2(0.6f, 1));

        // Title hint (left side)
        CreateTextElement(bar.transform, "TitleHint", "HOW TO PLAY", 18f,
            new Color(TITLE_GOLD.r, TITLE_GOLD.g, TITLE_GOLD.b, 0.4f),
            TextAlignmentOptions.Left, new Vector2(0, 0), new Vector2(0.4f, 1));

        // ── Nav hint area (right side) with icon+label pairs ─────────────
        GameObject navArea = new GameObject("NavHintArea");
        navArea.transform.SetParent(bar.transform, false);
        RectTransform naRT = navArea.AddComponent<RectTransform>();
        naRT.anchorMin = new Vector2(0.55f, 0); naRT.anchorMax = new Vector2(1, 1);
        naRT.offsetMin = Vector2.zero; naRT.offsetMax = Vector2.zero;

        // KB/M hints: [MouseLeft] NEXT   [ESC] BACK
        _kbmNavHints = BuildNavHintRow(navArea.transform, "KBMHints",
            ControllerIcons.MouseLeft, "NEXT",
            ControllerIcons.KeyEsc, "BACK",
            out _kbmConfirmLabel, out _kbmBackLabel);

        // Controller hints: [A] NEXT   [B] BACK
        _ctrlNavHints = BuildNavHintRow(navArea.transform, "CtrlHints",
            ControllerIcons.CtrlA, "NEXT",
            ControllerIcons.CtrlB, "BACK",
            out _ctrlConfirmLabel, out _ctrlBackLabel);

        UpdateNavHintsVisibility();
    }

    /// <summary>Builds a horizontal row with two icon+label pairs.</summary>
    private GameObject BuildNavHintRow(Transform parent, string name,
        Sprite confirmIcon, string confirmText,
        Sprite backIcon, string backText,
        out TextMeshProUGUI confirmLabel, out TextMeshProUGUI backLabel)
    {
        GameObject row = new GameObject(name);
        row.transform.SetParent(parent, false);
        RectTransform rowRT = row.AddComponent<RectTransform>();
        rowRT.anchorMin = Vector2.zero; rowRT.anchorMax = Vector2.one;
        rowRT.offsetMin = Vector2.zero; rowRT.offsetMax = Vector2.zero;

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 24f;
        hlg.childAlignment = TextAnchor.MiddleRight;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.padding = new RectOffset(0, 8, 0, 0);

        // Confirm icon+label
        GameObject confirmGO = ControllerIcons.CreateIconLabel(row.transform,
            confirmIcon, confirmText,
            iconSize: 40f, fontSize: 20f, labelColor: HINT_DIM, spacing: 8f);
        confirmLabel = confirmGO.GetComponentInChildren<TextMeshProUGUI>();

        // Back icon+label
        GameObject backGO = ControllerIcons.CreateIconLabel(row.transform,
            backIcon, backText,
            iconSize: 40f, fontSize: 20f, labelColor: HINT_DIM, spacing: 8f);
        backLabel = backGO.GetComponentInChildren<TextMeshProUGUI>();

        return row;
    }

    /// <summary>Shows the correct nav hint row for the current input mode.</summary>
    private void UpdateNavHintsVisibility()
    {
        bool kb = InputPromptManager.IsKeyboardMouse;
        if (_kbmNavHints != null) _kbmNavHints.SetActive(kb);
        if (_ctrlNavHints != null) _ctrlNavHints.SetActive(!kb);
    }

    /// <summary>Refreshes nav hint visibility when input mode changes.</summary>
    private void RefreshNavHints()
    {
        UpdateNavHintsVisibility();
    }

    // ── UI Helpers ───────────────────────────────────────────────────────

    private GameObject CreatePage(Transform parent, string name)
    {
        GameObject page = new GameObject(name);
        page.transform.SetParent(parent, false);
        RectTransform rt = page.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(60, 0); rt.offsetMax = new Vector2(-60, 0);
        page.SetActive(false);
        return page;
    }

    private GameObject CreateColumn(Transform parent, string name, float xMin, float xMax)
    {
        GameObject col = new GameObject(name);
        col.transform.SetParent(parent, false);
        RectTransform rt = col.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(xMin, 0); rt.anchorMax = new Vector2(xMax, 1);
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        return col;
    }

    private TextMeshProUGUI CreateLabel(Transform parent, string name, string text, float fontSize,
        Color color, TextAlignmentOptions align, Vector2 anchorMin, Vector2 anchorMax, bool bold = false)
    {
        return CreateTextElement(parent, name, text, fontSize, color, align, anchorMin, anchorMax, bold);
    }

    private TextMeshProUGUI CreateTextElement(Transform parent, string name, string text, float fontSize,
        Color color, TextAlignmentOptions align, Vector2 anchorMin, Vector2 anchorMax, bool bold = false)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = align;
        tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        tmp.font = bold ? (_cinzelBold != null ? _cinzelBold : _cinzelFont) : _cinzelFont;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;

        return tmp;
    }

    private void CreateDivider(Transform parent, float yMin, float yMax)
    {
        GameObject go = new GameObject("Divider");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, yMin); rt.anchorMax = new Vector2(0.3f, yMax);
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        Image img = go.AddComponent<Image>();
        img.color = DIVIDER_COLOR;
        img.raycastTarget = false;
    }

    private Image CreateStateCard(Transform parent, string name, float yMin, float yMax,
        Color stateColor, string arrow, string label)
    {
        // Card background
        GameObject card = new GameObject(name);
        card.transform.SetParent(parent, false);
        RectTransform cardRT = card.AddComponent<RectTransform>();
        cardRT.anchorMin = new Vector2(0.05f, yMin); cardRT.anchorMax = new Vector2(0.55f, yMax);
        cardRT.offsetMin = Vector2.zero; cardRT.offsetMax = Vector2.zero;
        Image cardBg = card.AddComponent<Image>();
        cardBg.color = CARD_BG;
        cardBg.raycastTarget = false;

        // Glow border (left edge)
        GameObject glow = new GameObject("Glow");
        glow.transform.SetParent(card.transform, false);
        RectTransform glowRT = glow.AddComponent<RectTransform>();
        glowRT.anchorMin = new Vector2(0, 0); glowRT.anchorMax = new Vector2(0.03f, 1);
        glowRT.offsetMin = Vector2.zero; glowRT.offsetMax = Vector2.zero;
        Image glowImg = glow.AddComponent<Image>();
        glowImg.color = stateColor;
        glowImg.raycastTarget = false;

        // Arrow
        CreateTextElement(card.transform, "Arrow", arrow, 26f,
            stateColor, TextAlignmentOptions.Center, new Vector2(0.08f, 0.1f), new Vector2(0.35f, 0.9f));

        // State label
        CreateTextElement(card.transform, "Label", label, 12f,
            new Color(stateColor.r, stateColor.g, stateColor.b, 0.7f),
            TextAlignmentOptions.Center, new Vector2(0.08f, 0f), new Vector2(0.98f, 0.25f));

        return glowImg;
    }

    private Image CreateCubeIcon(Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        GameObject go = new GameObject("CubeIcon");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        // Simple colored square representing the cube
        Image img = go.AddComponent<Image>();
        img.color = new Color(color.r, color.g, color.b, 0.25f);
        img.raycastTarget = false;

        // Inner label
        CreateTextElement(go.transform, "CubeLabel", "\u25A0", 30f,
            color, TextAlignmentOptions.Center, Vector2.zero, Vector2.one);

        return img;
    }

    private void CreateCubeVisual(Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        Color color, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        Image img = go.AddComponent<Image>();
        img.color = new Color(color.r, color.g, color.b, 0.15f);
        img.raycastTarget = false;

        CreateTextElement(go.transform, "Icon", "\u25A3", 36f,
            new Color(color.r, color.g, color.b, 0.6f), TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one);
    }

    // ── Fonts ────────────────────────────────────────────────────────────

    private void FindFonts()
    {
        TMP_FontAsset[] allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        foreach (TMP_FontAsset f in allFonts)
        {
            string lower = f.name.ToLowerInvariant();
            if (!lower.Contains("cinzel")) continue;

            if (lower.Contains("bold") || lower.Contains("black"))
            {
                if (_cinzelBold == null) _cinzelBold = f;
            }
            else if (_cinzelFont == null || lower.Contains("regular") || lower.Contains("medium"))
            {
                _cinzelFont = f;
            }
        }

        if (_cinzelFont == null) _cinzelFont = _cinzelBold;
        if (_cinzelBold == null) _cinzelBold = _cinzelFont;

        // Fallback to default TMP font
        if (_cinzelFont == null)
            _cinzelFont = TMP_Settings.defaultFontAsset;
    }

    // ── Animation ────────────────────────────────────────────────────────

    private void AnimateGlows()
    {
        float t = Time.unscaledTime;

        if (_forwardGlow != null)
        {
            float pulse = (Mathf.Sin(t * GLOW_SPEED) + 1f) * 0.5f;
            _forwardGlow.color = Color.Lerp(
                new Color(FORWARD_COLOR.r, FORWARD_COLOR.g, FORWARD_COLOR.b, 0.4f),
                FORWARD_COLOR, pulse);
        }
        if (_frozenGlow != null)
        {
            float pulse = (Mathf.Sin(t * GLOW_SPEED + 2.1f) + 1f) * 0.5f;
            _frozenGlow.color = Color.Lerp(
                new Color(FROZEN_COLOR.r, FROZEN_COLOR.g, FROZEN_COLOR.b, 0.4f),
                FROZEN_COLOR, pulse);
        }
        if (_reverseGlow != null)
        {
            float pulse = (Mathf.Sin(t * GLOW_SPEED + 4.2f) + 1f) * 0.5f;
            _reverseGlow.color = Color.Lerp(
                new Color(REVERSE_COLOR.r, REVERSE_COLOR.g, REVERSE_COLOR.b, 0.4f),
                REVERSE_COLOR, pulse);
        }
    }

    // ── Page Transitions ─────────────────────────────────────────────────

    private IEnumerator TransitionToPage(int targetPage)
    {
        _transitioning = true;
        CanvasGroup oldCG = EnsureCanvasGroup(_pages[_currentPage]);
        _pages[targetPage].SetActive(true);
        CanvasGroup newCG = EnsureCanvasGroup(_pages[targetPage]);
        newCG.alpha = 0f;

        float elapsed = 0f;
        while (elapsed < PAGE_TRANSITION)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / PAGE_TRANSITION);
            float smooth = t * t * (3f - 2f * t);
            oldCG.alpha = 1f - smooth;
            newCG.alpha = smooth;
            yield return null;
        }

        _pages[_currentPage].SetActive(false);
        oldCG.alpha = 1f;
        newCG.alpha = 1f;
        _currentPage = targetPage;
        UpdatePageCounter();
        _transitioning = false;
    }

    private IEnumerator FadeIn()
    {
        float elapsed = 0f;
        while (elapsed < FADE_IN_TIME)
        {
            elapsed += Time.unscaledDeltaTime;
            _rootCG.alpha = Mathf.Clamp01(elapsed / FADE_IN_TIME);
            yield return null;
        }
        _rootCG.alpha = 1f;
    }

    private IEnumerator FadeOutAndClose()
    {
        _transitioning = true;
        float elapsed = 0f;
        while (elapsed < FADE_IN_TIME)
        {
            elapsed += Time.unscaledDeltaTime;
            _rootCG.alpha = 1f - Mathf.Clamp01(elapsed / FADE_IN_TIME);
            yield return null;
        }

        _root.SetActive(false);
        _rootCG.alpha = 0f;
        _transitioning = false;
        IsAnyOpen = false;

        // Restore time if not coming from pause menu (pause menu manages its own timeScale)
        if (Origin == HTPOrigin.HubFirstVisit)
            Time.timeScale = 1f;

        // If from pause menu, return to pause menu
        if (Origin == HTPOrigin.PauseMenu)
        {
            PauseMenuController pause = FindObjectOfType<PauseMenuController>();
            if (pause != null) pause.ReturnFromHowToPlay();
        }
    }

    private void UpdatePageCounter()
    {
        if (_pageCounter != null)
            _pageCounter.text = $"{_currentPage + 1} / {_pages.Count}";

        // Determine confirm/back labels based on page position
        string confirmText, backText;
        if (_currentPage == _pages.Count - 1)
        {
            confirmText = "CLOSE";
            backText = "BACK";
        }
        else if (_currentPage == 0)
        {
            confirmText = "NEXT";
            backText = "CLOSE";
        }
        else
        {
            confirmText = "NEXT";
            backText = "BACK";
        }

        // Update both KB/M and controller label text
        if (_kbmConfirmLabel != null) _kbmConfirmLabel.text = confirmText;
        if (_kbmBackLabel != null) _kbmBackLabel.text = backText;
        if (_ctrlConfirmLabel != null) _ctrlConfirmLabel.text = confirmText;
        if (_ctrlBackLabel != null) _ctrlBackLabel.text = backText;
    }

    private CanvasGroup EnsureCanvasGroup(GameObject go)
    {
        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();
        return cg;
    }
}
