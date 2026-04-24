using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Builds the full Options menu UI at runtime with three tabs: GAME, AUDIO, VIDEO.
/// Matches the project's dark-blue/gold visual style.
/// </summary>
public static class OptionsMenuBuilder
{
    // ── Colors (matching HubPauseMenuBuilder palette) ────────────────────
    private static readonly Color BG_COLOR      = new Color(0.059f, 0.102f, 0.188f, 0.97f);
    private static readonly Color BTN_NORMAL    = new Color(0.08f, 0.12f, 0.22f, 1f);
    private static readonly Color BTN_HIGHLIGHT = new Color(0.22f, 0.20f, 0.35f, 1f);
    private static readonly Color BTN_SELECTED  = new Color(0.22f, 0.20f, 0.35f, 1f);
    private static readonly Color BTN_PRESSED   = new Color(0.15f, 0.14f, 0.28f, 1f);
    private static readonly Color LABEL_WHITE   = new Color(0.91f, 0.918f, 0.965f, 1f);
    private static readonly Color LABEL_DIM     = new Color(0.91f, 0.918f, 0.965f, 0.4f);
    private static readonly Color GOLD          = new Color(0.961f, 0.784f, 0.259f, 1f);
    private static readonly Color GOLD_DIM      = new Color(0.961f, 0.784f, 0.259f, 0.35f);
    private static readonly Color SLIDER_BG     = new Color(0.06f, 0.08f, 0.14f, 1f);
    private static readonly Color SLIDER_FILL   = new Color(0.961f, 0.784f, 0.259f, 0.8f);
    private static readonly Color TOGGLE_OFF    = new Color(0.12f, 0.16f, 0.26f, 1f);
    private static readonly Color TOGGLE_ON     = GOLD;
    private static readonly Color ARROW_COLOR   = new Color(0.91f, 0.918f, 0.965f, 0.6f);

    private const float PANEL_WIDTH  = 680f;
    private const float PANEL_HEIGHT = 700f;
    private const float TAB_HEIGHT   = 52f;
    private const float ROW_HEIGHT   = 48f;
    private const float ROW_SPACING  = 10f;
    private const float SIDE_PAD     = 28f;

    /// <summary>Builds the complete options menu under the given parent transform.</summary>
    public static OptionsMenuController Build(Transform parent)
    {
        // ── Root ────────────────────────────────────────────────────────
        GameObject root = MakeRect("OptionsScreen", parent, Vector2.zero, Vector2.one);
        CanvasGroup rootCG = root.AddComponent<CanvasGroup>();

        OptionsMenuController ctrl = root.AddComponent<OptionsMenuController>();
        ctrl.canvasGroup = rootCG;

        // ── Background panel (centered card) ────────────────────────────
        GameObject panelGO = MakeRect("OptionsPanel", root.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        RectTransform panelRT = panelGO.GetComponent<RectTransform>();
        panelRT.sizeDelta = new Vector2(PANEL_WIDTH, PANEL_HEIGHT);
        Image panelBg = panelGO.AddComponent<Image>();
        panelBg.color = BG_COLOR;
        panelBg.raycastTarget = true;

        // Panel border
        Outline panelOutline = panelGO.AddComponent<Outline>();
        panelOutline.effectColor = GOLD_DIM;
        panelOutline.effectDistance = new Vector2(2f, 2f);

        // ── Title ───────────────────────────────────────────────────────
        GameObject titleGO = MakeRect("Title", panelGO.transform,
            new Vector2(0f, 1f), new Vector2(1f, 1f));
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.sizeDelta = new Vector2(-28f, 56f);
        titleRT.anchoredPosition = new Vector2(0f, -12f);
        TextMeshProUGUI titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "OPTIONS";
        titleTMP.fontSize = 46f;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color = LABEL_WHITE;
        titleTMP.characterSpacing = 6f;
        titleTMP.raycastTarget = false;
        CinzelFontHelper.Apply(titleTMP, true);

        // ── Top accent line ─────────────────────────────────────────────
        MakeAccentLine("TopLine", panelGO.transform,
            new Vector2(0.04f, 1f), new Vector2(0.96f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -72f));

        // ── Tab bar ─────────────────────────────────────────────────────
        string[] tabNames = { "GAME", "AUDIO", "VIDEO" };
        Button[] tabBtns = new Button[tabNames.Length];

        float tabBarY = -78f;
        float tabWidth = (PANEL_WIDTH - SIDE_PAD * 2f) / tabNames.Length;

        for (int i = 0; i < tabNames.Length; i++)
        {
            GameObject tabGO = MakeRect("Tab_" + tabNames[i], panelGO.transform,
                new Vector2(0f, 1f), new Vector2(0f, 1f));
            RectTransform tabRT = tabGO.GetComponent<RectTransform>();
            tabRT.pivot = new Vector2(0f, 1f);
            tabRT.sizeDelta = new Vector2(tabWidth, TAB_HEIGHT);
            tabRT.anchoredPosition = new Vector2(SIDE_PAD + i * tabWidth, tabBarY);

            // Image MUST be added before Button for proper raycast
            Image tabImg = tabGO.AddComponent<Image>();
            tabImg.color = Color.white;
            tabImg.raycastTarget = true;

            Button tabBtn = tabGO.AddComponent<Button>();
            tabBtn.targetGraphic = tabImg;
            ColorBlock cb = tabBtn.colors;
            cb.normalColor      = new Color(0f, 0f, 0f, 0f);
            cb.highlightedColor = new Color(1f, 1f, 1f, 0.08f);
            cb.selectedColor    = new Color(0f, 0f, 0f, 0f);
            cb.pressedColor     = new Color(1f, 1f, 1f, 0.12f);
            tabBtn.colors = cb;

            Navigation nav = tabBtn.navigation;
            nav.mode = Navigation.Mode.None;
            tabBtn.navigation = nav;

            // Tab label
            GameObject lblGO = MakeRect("Label", tabGO.transform, Vector2.zero, Vector2.one);
            TextMeshProUGUI lbl = lblGO.AddComponent<TextMeshProUGUI>();
            lbl.text = tabNames[i];
            lbl.fontSize = 22f;
            lbl.alignment = TextAlignmentOptions.Center;
            lbl.color = i == 0 ? GOLD : LABEL_DIM;
            lbl.raycastTarget = false;
            CinzelFontHelper.Apply(lbl, true);

            // Tab underline (gold bar under active tab)
            GameObject underGO = MakeRect("Underline", tabGO.transform,
                new Vector2(0.12f, 0f), new Vector2(0.88f, 0f));
            RectTransform underRT = underGO.GetComponent<RectTransform>();
            underRT.pivot = new Vector2(0.5f, 0f);
            underRT.sizeDelta = new Vector2(0f, 3f);
            underRT.anchoredPosition = Vector2.zero;
            Image underImg = underGO.AddComponent<Image>();
            underImg.color = GOLD;
            underImg.raycastTarget = false;
            underGO.SetActive(i == 0);

            tabBtns[i] = tabBtn;
        }

        ctrl.tabButtons = tabBtns;

        // ── LB / RB hints ───────────────────────────────────────────────
        ctrl.lbHint = MakeTabHint("LB_Hint", panelGO.transform, "LB", true, tabBarY, TAB_HEIGHT);
        ctrl.rbHint = MakeTabHint("RB_Hint", panelGO.transform, "RB", false, tabBarY, TAB_HEIGHT);

        // ── Content area ────────────────────────────────────────────────
        float contentTop = tabBarY - TAB_HEIGHT - 10f;
        float contentBottom = 68f;

        // GAME tab content
        GameObject gameTab = MakeContentPanel("GameContent", panelGO.transform, contentTop, contentBottom);
        BuildGameTab(gameTab.transform, ctrl);

        // AUDIO tab content
        GameObject audioTab = MakeContentPanel("AudioContent", panelGO.transform, contentTop, contentBottom);
        BuildAudioTab(audioTab.transform, ctrl);

        // VIDEO tab content
        GameObject videoTab = MakeContentPanel("VideoContent", panelGO.transform, contentTop, contentBottom);
        BuildVideoTab(videoTab.transform, ctrl);

        ctrl.tabContents = new[] { gameTab, audioTab, videoTab };
        audioTab.SetActive(false);
        videoTab.SetActive(false);

        // ── Controller tooltip (hidden by default, renders on top) ────────
        ctrl.controllerTooltip = MakeControllerTooltip(panelGO.transform, contentTop);
        ctrl.tooltipRT = ctrl.controllerTooltip.GetComponent<RectTransform>();
        // Ensure tooltip renders above all other panel children
        ctrl.controllerTooltip.transform.SetAsLastSibling();

        // ── Bottom accent line ──────────────────────────────────────────
        MakeAccentLine("BottomLine", panelGO.transform,
            new Vector2(0.04f, 0f), new Vector2(0.96f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, 62f));

        // ── Back button ─────────────────────────────────────────────────
        ctrl.backButton = MakeButton("BackButton", panelGO.transform, "BACK", -PANEL_HEIGHT / 2f + 32f);

        // ── Initialize AFTER all references are assigned ────────────────
        ctrl.InitializeAfterBuild();

        return ctrl;
    }

    // ── Tab content builders ────────────────────────────────────────────

    private static void BuildGameTab(Transform parent, OptionsMenuController ctrl)
    {
        float y = -10f;

        // Camera header
        GameObject cameraHeaderGO;
        y = MakeSectionHeaderInternal(parent, "CAMERA", y, out cameraHeaderGO);
        ctrl.cameraHeader = cameraHeaderGO;

        // Mouse sensitivity — only visible on KB/M
        GameObject mouseSensRow = MakeSliderRowGO(parent, "Mouse Sensitivity", ref y,
            GameSettings.MOUSE_SENS_MIN, GameSettings.MOUSE_SENS_MAX, GameSettings.DEFAULT_MOUSE_SENS,
            out ctrl.mouseSensSlider, out ctrl.mouseSensValue, false);
        ctrl.mouseSensRow = mouseSensRow;

        // Stick sensitivity — only visible on controller
        GameObject stickSensRow = MakeSliderRowGO(parent, "Stick Sensitivity", ref y,
            GameSettings.STICK_SENS_MIN, GameSettings.STICK_SENS_MAX, GameSettings.DEFAULT_STICK_SENS,
            out ctrl.stickSensSlider, out ctrl.stickSensValue, false);
        ctrl.stickSensRow = stickSensRow;

        // Stick deadzone sub-header — only visible on controller
        ctrl.deadzoneHeader = MakeSectionHeaderGO(parent, "STICK DEADZONE", ref y);

        // Left stick deadzone — only visible on controller
        GameObject leftDzRow = MakeSliderRowGO(parent, "Left Stick", ref y,
            GameSettings.STICK_DEAD_MIN, GameSettings.STICK_DEAD_MAX, GameSettings.DEFAULT_LEFT_STICK_DEADZONE,
            out ctrl.leftDeadzoneSlider, out ctrl.leftDeadzoneValue, false);
        ctrl.leftDeadzoneRow = leftDzRow;

        // Right stick deadzone — only visible on controller
        GameObject rightDzRow = MakeSliderRowGO(parent, "Right Stick", ref y,
            GameSettings.STICK_DEAD_MIN, GameSettings.STICK_DEAD_MAX, GameSettings.DEFAULT_RIGHT_STICK_DEADZONE,
            out ctrl.rightDeadzoneSlider, out ctrl.rightDeadzoneValue, false);
        ctrl.rightDeadzoneRow = rightDzRow;

        // Invert Y Axis — always visible
        ctrl.invertYToggle = MakeToggleRow(parent, "Invert Y Axis (Camera)", ref y, false);
        // Store the row GO for repositioning (toggle row is named "Row_Invert Y Axis (Camera)")
        ctrl.invertYRow = ctrl.invertYToggle.transform.parent.gameObject;

        // Reset to Default
        EnsureResetButtons(ctrl, 3);
        ctrl.resetButtons[0] = MakeResetButton(parent, ref y);
        ctrl.gameResetRow = ctrl.resetButtons[0].gameObject;
    }

    private static void BuildAudioTab(Transform parent, OptionsMenuController ctrl)
    {
        float y = -10f;

        y = MakeSectionHeader(parent, "VOLUME", y);

        ctrl.masterVolSlider = MakeSliderRow(parent, "Master", ref y, 0f, 1f, GameSettings.DEFAULT_MASTER_VOL,
            out ctrl.masterVolValue, true);

        ctrl.musicVolSlider = MakeSliderRow(parent, "Music", ref y, 0f, 1f, GameSettings.DEFAULT_MUSIC_VOL,
            out ctrl.musicVolValue, true);

        ctrl.sfxVolSlider = MakeSliderRow(parent, "SFX", ref y, 0f, 1f, GameSettings.DEFAULT_SFX_VOL,
            out ctrl.sfxVolValue, true);

        Selectable[] audioSelectables = { ctrl.masterVolSlider, ctrl.musicVolSlider, ctrl.sfxVolSlider };
        WireVerticalNav(audioSelectables);

        // Reset to Default
        EnsureResetButtons(ctrl, 3);
        ctrl.resetButtons[1] = MakeResetButton(parent, ref y);
    }

    private static void BuildVideoTab(Transform parent, OptionsMenuController ctrl)
    {
        float y = -10f;

        y = MakeSectionHeader(parent, "DISPLAY", y);

        MakeCycleRow(parent, "Resolution", ref y,
            out ctrl.resolutionLabel,
            delta => ctrl.CycleResolution(delta));

        MakeCycleRow(parent, "Display Mode", ref y,
            out ctrl.displayModeLabel,
            delta => ctrl.CycleDisplayMode(delta));

        ctrl.vsyncToggle = MakeToggleRow(parent, "VSync", ref y, true);

        y -= 12f;
        y = MakeSectionHeader(parent, "QUALITY", y);

        MakeCycleRow(parent, "Quality Preset", ref y,
            out ctrl.qualityLabel,
            delta => ctrl.CycleQuality(delta));

        Selectable[] videoSelectables = { ctrl.vsyncToggle };
        WireVerticalNav(videoSelectables);

        // Reset to Default
        EnsureResetButtons(ctrl, 3);
        ctrl.resetButtons[2] = MakeResetButton(parent, ref y);
    }

    // ── UI element factories ────────────────────────────────────────────

    private static float MakeSectionHeader(Transform parent, string text, float y)
    {
        GameObject go;
        return MakeSectionHeaderInternal(parent, text, y, out go);
    }

    /// <summary>Creates a section header and returns the GameObject for visibility control.</summary>
    private static GameObject MakeSectionHeaderGO(Transform parent, string text, ref float y)
    {
        GameObject go;
        y = MakeSectionHeaderInternal(parent, text, y, out go);
        return go;
    }

    private static float MakeSectionHeaderInternal(Transform parent, string text, float y, out GameObject go)
    {
        go = MakeRect("Header_" + text, parent,
            new Vector2(0f, 1f), new Vector2(1f, 1f));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(-SIDE_PAD * 2f, 28f);
        rt.anchoredPosition = new Vector2(0f, y);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 16f;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.color = GOLD;
        tmp.characterSpacing = 4f;
        tmp.raycastTarget = false;
        CinzelFontHelper.Apply(tmp, true);

        return y - 34f;
    }

    private static Slider MakeSliderRow(Transform parent, string label, ref float y,
        float min, float max, float defaultVal,
        out TextMeshProUGUI valueLabel, bool asPercent)
    {
        Slider slider;
        MakeSliderRowGO(parent, label, ref y, min, max, defaultVal,
            out slider, out valueLabel, asPercent);
        return slider;
    }

    private static GameObject MakeSliderRowGO(Transform parent, string label, ref float y,
        float min, float max, float defaultVal,
        out Slider slider, out TextMeshProUGUI valueLabel, bool asPercent)
    {
        GameObject row = MakeRect("Row_" + label, parent,
            new Vector2(0f, 1f), new Vector2(1f, 1f));
        RectTransform rowRT = row.GetComponent<RectTransform>();
        rowRT.pivot = new Vector2(0.5f, 1f);
        rowRT.sizeDelta = new Vector2(-SIDE_PAD * 2f, ROW_HEIGHT);
        rowRT.anchoredPosition = new Vector2(0f, y);

        // Label (left 32%)
        GameObject lblGO = MakeRect("Label", row.transform,
            new Vector2(0f, 0f), new Vector2(0.32f, 1f));
        TextMeshProUGUI lbl = lblGO.AddComponent<TextMeshProUGUI>();
        lbl.text = label;
        lbl.fontSize = 20f;
        lbl.alignment = TextAlignmentOptions.MidlineLeft;
        lbl.color = LABEL_WHITE;
        lbl.raycastTarget = false;
        CinzelFontHelper.Apply(lbl);

        // Value (right 12%)
        GameObject valGO = MakeRect("Value", row.transform,
            new Vector2(0.88f, 0f), new Vector2(1f, 1f));
        valueLabel = valGO.AddComponent<TextMeshProUGUI>();
        valueLabel.text = asPercent
            ? Mathf.RoundToInt(defaultVal * 100f) + "%"
            : defaultVal.ToString("F2");
        valueLabel.fontSize = 18f;
        valueLabel.alignment = TextAlignmentOptions.MidlineRight;
        valueLabel.color = LABEL_DIM;
        valueLabel.raycastTarget = false;
        CinzelFontHelper.Apply(valueLabel);

        // Slider (middle 54%)
        slider = MakeSlider(row.transform, min, max, defaultVal);

        y -= ROW_HEIGHT + ROW_SPACING;
        return row;
    }

    private static Slider MakeSlider(Transform parent, float min, float max, float defaultVal)
    {
        GameObject sliderGO = MakeRect("Slider", parent,
            new Vector2(0.33f, 0.15f), new Vector2(0.86f, 0.85f));

        Image bg = sliderGO.AddComponent<Image>();
        bg.color = SLIDER_BG;

        Slider slider = sliderGO.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = defaultVal;
        slider.wholeNumbers = false;

        // Fill area
        GameObject fillArea = MakeRect("FillArea", sliderGO.transform,
            new Vector2(0f, 0f), new Vector2(1f, 1f));
        RectTransform fillAreaRT = fillArea.GetComponent<RectTransform>();
        fillAreaRT.offsetMin = Vector2.zero;
        fillAreaRT.offsetMax = Vector2.zero;

        GameObject fill = MakeRect("Fill", fillArea.transform,
            new Vector2(0f, 0f), new Vector2(0f, 1f));
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = SLIDER_FILL;
        RectTransform fillRT = fill.GetComponent<RectTransform>();
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;

        slider.fillRect = fillRT;

        // Handle slide area
        GameObject handleArea = MakeRect("HandleArea", sliderGO.transform,
            new Vector2(0f, 0f), new Vector2(1f, 1f));
        RectTransform handleAreaRT = handleArea.GetComponent<RectTransform>();
        handleAreaRT.offsetMin = Vector2.zero;
        handleAreaRT.offsetMax = Vector2.zero;

        GameObject handle = MakeRect("Handle", handleArea.transform,
            new Vector2(0f, 0f), new Vector2(0f, 1f));
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = GOLD;
        RectTransform handleRT = handle.GetComponent<RectTransform>();
        handleRT.sizeDelta = new Vector2(10f, 0f);
        handleRT.offsetMin = new Vector2(-5f, 0f);
        handleRT.offsetMax = new Vector2(5f, 0f);

        slider.handleRect = handleRT;
        slider.targetGraphic = handleImg;

        Navigation nav = slider.navigation;
        nav.mode = Navigation.Mode.Explicit;
        slider.navigation = nav;

        ColorBlock cb = slider.colors;
        cb.normalColor      = GOLD;
        cb.highlightedColor = Color.white;
        cb.selectedColor    = Color.white;
        cb.pressedColor     = GOLD;
        slider.colors = cb;

        return slider;
    }

    private static Toggle MakeToggleRow(Transform parent, string label, ref float y, bool defaultVal)
    {
        GameObject row = MakeRect("Row_" + label, parent,
            new Vector2(0f, 1f), new Vector2(1f, 1f));
        RectTransform rowRT = row.GetComponent<RectTransform>();
        rowRT.pivot = new Vector2(0.5f, 1f);
        rowRT.sizeDelta = new Vector2(-SIDE_PAD * 2f, ROW_HEIGHT);
        rowRT.anchoredPosition = new Vector2(0f, y);

        // Label (left 70%)
        GameObject lblGO = MakeRect("Label", row.transform,
            new Vector2(0f, 0f), new Vector2(0.7f, 1f));
        TextMeshProUGUI lbl = lblGO.AddComponent<TextMeshProUGUI>();
        lbl.text = label;
        lbl.fontSize = 20f;
        lbl.alignment = TextAlignmentOptions.MidlineLeft;
        lbl.color = LABEL_WHITE;
        lbl.raycastTarget = false;
        CinzelFontHelper.Apply(lbl);

        // Toggle (right side)
        GameObject toggleGO = MakeRect("Toggle", row.transform,
            new Vector2(0.72f, 0.12f), new Vector2(0.95f, 0.88f));

        Image toggleBg = toggleGO.AddComponent<Image>();
        toggleBg.color = defaultVal ? TOGGLE_ON : TOGGLE_OFF;

        Toggle toggle = toggleGO.AddComponent<Toggle>();
        toggle.targetGraphic = toggleBg;
        toggle.isOn = defaultVal;

        // ON/OFF label
        GameObject checkGO = MakeRect("StatusLabel", toggleGO.transform,
            new Vector2(0f, 0f), new Vector2(1f, 1f));
        TextMeshProUGUI statusTMP = checkGO.AddComponent<TextMeshProUGUI>();
        statusTMP.text = defaultVal ? "ON" : "OFF";
        statusTMP.fontSize = 18f;
        statusTMP.alignment = TextAlignmentOptions.Center;
        statusTMP.color = new Color(0.05f, 0.08f, 0.15f, 1f);
        statusTMP.raycastTarget = false;
        CinzelFontHelper.Apply(statusTMP, true);

        toggle.onValueChanged.AddListener(on =>
        {
            toggleBg.color = on ? TOGGLE_ON : TOGGLE_OFF;
            statusTMP.text = on ? "ON" : "OFF";
        });

        toggle.graphic = null;

        Navigation nav = toggle.navigation;
        nav.mode = Navigation.Mode.Explicit;
        toggle.navigation = nav;

        y -= ROW_HEIGHT + ROW_SPACING;
        return toggle;
    }

    private static void MakeCycleRow(Transform parent, string label, ref float y,
        out TextMeshProUGUI valueLabel, System.Action<int> onCycle)
    {
        GameObject row = MakeRect("Row_" + label, parent,
            new Vector2(0f, 1f), new Vector2(1f, 1f));
        RectTransform rowRT = row.GetComponent<RectTransform>();
        rowRT.pivot = new Vector2(0.5f, 1f);
        rowRT.sizeDelta = new Vector2(-SIDE_PAD * 2f, ROW_HEIGHT);
        rowRT.anchoredPosition = new Vector2(0f, y);

        // Label (left 38%)
        GameObject lblGO = MakeRect("Label", row.transform,
            new Vector2(0f, 0f), new Vector2(0.38f, 1f));
        TextMeshProUGUI lbl = lblGO.AddComponent<TextMeshProUGUI>();
        lbl.text = label;
        lbl.fontSize = 20f;
        lbl.alignment = TextAlignmentOptions.MidlineLeft;
        lbl.color = LABEL_WHITE;
        lbl.raycastTarget = false;
        CinzelFontHelper.Apply(lbl);

        // Left arrow button
        Button leftBtn = MakeArrowButton("LeftArrow", row.transform,
            new Vector2(0.52f, 0.08f), new Vector2(0.60f, 0.92f), "<");
        leftBtn.onClick.AddListener(() => onCycle(-1));

        // Value label (center)
        GameObject valGO = MakeRect("Value", row.transform,
            new Vector2(0.60f, 0f), new Vector2(0.88f, 1f));
        valueLabel = valGO.AddComponent<TextMeshProUGUI>();
        valueLabel.text = "---";
        valueLabel.fontSize = 18f;
        valueLabel.alignment = TextAlignmentOptions.Center;
        valueLabel.color = LABEL_WHITE;
        valueLabel.raycastTarget = false;
        CinzelFontHelper.Apply(valueLabel);

        // Right arrow button
        Button rightBtn = MakeArrowButton("RightArrow", row.transform,
            new Vector2(0.88f, 0.08f), new Vector2(0.98f, 0.92f), ">");
        rightBtn.onClick.AddListener(() => onCycle(1));

        y -= ROW_HEIGHT + ROW_SPACING;
    }

    private static Button MakeArrowButton(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, string arrow)
    {
        GameObject go = MakeRect(name, parent, anchorMin, anchorMax);
        Image img = go.AddComponent<Image>();
        img.color = BTN_NORMAL;
        img.raycastTarget = true;

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        ColorBlock cb = btn.colors;
        cb.normalColor      = BTN_NORMAL;
        cb.highlightedColor = BTN_HIGHLIGHT;
        cb.selectedColor    = BTN_SELECTED;
        cb.pressedColor     = BTN_PRESSED;
        cb.fadeDuration     = 0.05f;
        btn.colors = cb;

        Navigation nav = btn.navigation;
        nav.mode = Navigation.Mode.None;
        btn.navigation = nav;

        GameObject lblGO = MakeRect("Label", go.transform, Vector2.zero, Vector2.one);
        TextMeshProUGUI tmp = lblGO.AddComponent<TextMeshProUGUI>();
        tmp.text = arrow;
        tmp.fontSize = 24f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = ARROW_COLOR;
        tmp.raycastTarget = false;
        CinzelFontHelper.Apply(tmp, true);

        return btn;
    }

    private static GameObject MakeTabHint(string name, Transform parent, string text,
        bool isLeft, float tabBarY, float tabHeight)
    {
        Vector2 anchor = isLeft ? new Vector2(0f, 1f) : new Vector2(1f, 1f);

        GameObject go = MakeRect(name, parent, anchor, anchor);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.pivot = isLeft ? new Vector2(0f, 1f) : new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(32f, tabHeight);
        rt.anchoredPosition = new Vector2(isLeft ? 6f : -6f, tabBarY);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 14f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = LABEL_DIM;
        tmp.raycastTarget = false;
        CinzelFontHelper.Apply(tmp);

        return go;
    }

    /// <summary>Creates the floating controller tooltip that follows the selected slider.</summary>
    private static GameObject MakeControllerTooltip(Transform parent, float contentTop)
    {
        GameObject go = MakeRect("ControllerTooltip", parent,
            new Vector2(0.08f, 0.5f), new Vector2(0.92f, 0.5f));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, 34f);
        rt.anchoredPosition = Vector2.zero;

        // Override sorting to render above everything else
        Canvas tipCanvas = go.AddComponent<Canvas>();
        tipCanvas.overrideSorting = true;
        tipCanvas.sortingOrder = 100;
        go.AddComponent<GraphicRaycaster>();

        // CanvasGroup for alpha fade
        CanvasGroup cg = go.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.blocksRaycasts = false;

        Image bg = go.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.06f, 0.12f, 0.95f);
        bg.raycastTarget = false;

        Outline border = go.AddComponent<Outline>();
        border.effectColor = new Color(0.961f, 0.784f, 0.259f, 0.5f);
        border.effectDistance = new Vector2(1f, 1f);

        GameObject lblGO = MakeRect("Label", go.transform, Vector2.zero, Vector2.one);
        TextMeshProUGUI tmp = lblGO.AddComponent<TextMeshProUGUI>();
        tmp.text = "Hold  A  +  Left Stick  to adjust";
        tmp.fontSize = 16f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = GOLD;
        tmp.raycastTarget = false;
        CinzelFontHelper.Apply(tmp);

        // Stays active — visibility controlled by CanvasGroup alpha
        return go;
    }

    private static GameObject MakeContentPanel(string name, Transform parent,
        float topOffset, float bottomOffset)
    {
        GameObject go = MakeRect(name, parent,
            new Vector2(0f, 0f), new Vector2(1f, 1f));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.offsetMin = new Vector2(0f, bottomOffset);
        rt.offsetMax = new Vector2(0f, topOffset);
        return go;
    }

    private static void MakeAccentLine(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position)
    {
        GameObject go = MakeRect(name, parent, anchorMin, anchorMax);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.pivot = pivot;
        rt.sizeDelta = new Vector2(0f, 2f);
        rt.anchoredPosition = position;
        Image img = go.AddComponent<Image>();
        img.color = GOLD_DIM;
        img.raycastTarget = false;
    }

    private static Button MakeButton(string name, Transform parent, string label, float yOffset)
    {
        GameObject btnGO = MakeRect(name, parent,
            new Vector2(0.15f, 0.5f), new Vector2(0.85f, 0.5f));
        RectTransform rt = btnGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, 50f);
        rt.anchoredPosition = new Vector2(0f, yOffset);

        Image btnImg = btnGO.AddComponent<Image>();
        btnImg.color = Color.white;
        btnImg.raycastTarget = true;

        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        ColorBlock cb = btn.colors;
        cb.normalColor      = BTN_NORMAL;
        cb.highlightedColor = BTN_HIGHLIGHT;
        cb.selectedColor    = BTN_SELECTED;
        cb.pressedColor     = BTN_PRESSED;
        cb.fadeDuration     = 0.05f;
        btn.colors = cb;

        Navigation nav = btn.navigation;
        nav.mode = Navigation.Mode.None;
        btn.navigation = nav;

        GameObject lblGO = MakeRect("Label", btnGO.transform, Vector2.zero, Vector2.one);
        TextMeshProUGUI tmp = lblGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 22f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = LABEL_WHITE;
        tmp.raycastTarget = false;
        CinzelFontHelper.Apply(tmp);

        return btn;
    }

    private static void EnsureResetButtons(OptionsMenuController ctrl, int count)
    {
        if (ctrl.resetButtons == null)
            ctrl.resetButtons = new Button[count];
    }

    private static Button MakeResetButton(Transform parent, ref float y)
    {
        y -= 8f;
        GameObject btnGO = MakeRect("ResetButton", parent,
            new Vector2(0f, 1f), new Vector2(1f, 1f));
        RectTransform rt = btnGO.GetComponent<RectTransform>();
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(-SIDE_PAD * 2f, 36f);
        rt.anchoredPosition = new Vector2(0f, y);

        Image img = btnGO.AddComponent<Image>();
        img.color = Color.white;
        img.raycastTarget = true;

        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = img;
        ColorBlock cb = btn.colors;
        cb.normalColor      = new Color(0.08f, 0.10f, 0.18f, 0.6f);
        cb.highlightedColor = new Color(0.22f, 0.20f, 0.35f, 0.8f);
        cb.selectedColor    = new Color(0.22f, 0.20f, 0.35f, 0.8f);
        cb.pressedColor     = new Color(0.15f, 0.14f, 0.28f, 0.8f);
        cb.fadeDuration     = 0.05f;
        btn.colors = cb;

        Navigation nav = btn.navigation;
        nav.mode = Navigation.Mode.None;
        btn.navigation = nav;

        GameObject lblGO = MakeRect("Label", btnGO.transform, Vector2.zero, Vector2.one);
        TextMeshProUGUI tmp = lblGO.AddComponent<TextMeshProUGUI>();
        tmp.text = "RESET TO DEFAULT";
        tmp.fontSize = 15f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = LABEL_DIM;
        tmp.raycastTarget = false;
        CinzelFontHelper.Apply(tmp);

        y -= 44f;
        return btn;
    }

    private static void WireVerticalNav(Selectable[] items)
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null) continue;
            Navigation nav = items[i].navigation;
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnUp   = i > 0 ? items[i - 1] : items[items.Length - 1];
            nav.selectOnDown = i < items.Length - 1 ? items[i + 1] : items[0];
            items[i].navigation = nav;
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static GameObject MakeRect(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax)
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
