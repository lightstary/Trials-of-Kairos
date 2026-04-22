using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Builds the full PauseMenu UI at runtime for scenes that ship with an empty
/// PauseMenu shell (e.g. HubScene). Wires all serialized fields on PauseMenuController
/// via reflection so the same pause code works identically to MainScene.
/// </summary>
public static class HubPauseMenuBuilder
{
    private static readonly Color BG_COLOR     = new Color(0.059f, 0.102f, 0.188f, 0.97f);
    private static readonly Color OVERLAY_COL  = new Color(0f, 0f, 0f, 0.6f);
    private static readonly Color BTN_IMG_COL  = new Color(0.08f, 0.12f, 0.22f, 1f);
    private static readonly Color BTN_HIGHLIGHT = new Color(0.961f, 0.784f, 0.259f, 1f);
    private static readonly Color BTN_SELECTED = new Color(0.961f, 0.784f, 0.259f, 1f);
    private static readonly Color BTN_PRESSED  = new Color(0.7f, 0.5f, 0.1f, 1f);
    private static readonly Color LABEL_WHITE  = new Color(0.91f, 0.918f, 0.965f, 1f);

    /// <summary>Builds the full PauseMenu UI under the PauseMenuController's transform.</summary>
    public static void Build(PauseMenuController pmc)
    {
        Transform root = pmc.transform;
        BindingFlags bf = BindingFlags.NonPublic | BindingFlags.Instance;

        // ── Blur overlay (full screen dark background) ──
        GameObject overlayGO = MakeRect("BlurOverlay", root, Vector2.zero, Vector2.one);
        Image overlayImg = overlayGO.AddComponent<Image>();
        overlayImg.color = OVERLAY_COL;
        overlayImg.raycastTarget = true;
        SetField(pmc, "blurOverlay", overlayImg, bf);

        // ── Pause Panel (centered card) ──
        GameObject panelGO = MakeRect("PausePanel", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        RectTransform panelRT = panelGO.GetComponent<RectTransform>();
        panelRT.sizeDelta = new Vector2(440f, 600f);
        Image panelBg = panelGO.AddComponent<Image>();
        panelBg.color = BG_COLOR;
        panelBg.raycastTarget = true;
        CanvasGroup panelCG = panelGO.AddComponent<CanvasGroup>();

        SetField(pmc, "pausePanel", panelGO, bf);
        SetField(pmc, "pauseCanvasGroup", panelCG, bf);
        SetField(pmc, "panelRect", panelRT, bf);

        // ── Title ──
        GameObject titleGO = MakeRect("PauseTitle", panelGO.transform, new Vector2(0f, 1f), new Vector2(1f, 1f));
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.sizeDelta = new Vector2(-20f, 50f);
        titleRT.anchoredPosition = new Vector2(0f, -30f);
        TextMeshProUGUI titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "PAUSED";
        titleTMP.fontSize = 44f;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color = LABEL_WHITE;
        titleTMP.characterSpacing = 6f;
        titleTMP.raycastTarget = false;
        AssignFont(titleTMP);

        // ── Trial Info (bottom of panel) ──
        GameObject infoGO = MakeRect("TrialInfo", panelGO.transform, new Vector2(0f, 0f), new Vector2(1f, 0f));
        RectTransform infoRT = infoGO.GetComponent<RectTransform>();
        infoRT.pivot = new Vector2(0.5f, 0f);
        infoRT.sizeDelta = new Vector2(-20f, 28f);
        infoRT.anchoredPosition = new Vector2(0f, 16f);
        TextMeshProUGUI infoTMP = infoGO.AddComponent<TextMeshProUGUI>();
        infoTMP.text = "HUB";
        infoTMP.fontSize = 18f;
        infoTMP.alignment = TextAlignmentOptions.Center;
        infoTMP.color = new Color(LABEL_WHITE.r, LABEL_WHITE.g, LABEL_WHITE.b, 0.4f);
        infoTMP.raycastTarget = false;
        AssignFont(infoTMP);
        SetField(pmc, "trialInfoLabel", infoTMP, bf);

        // ── Buttons (center-anchored, matching MainScene layout) ──
        float btnStep = 64f;

        Button resume       = MakeButton("ResumeButton",     panelGO.transform, "RESUME",              130f);
        Button restart      = MakeButton("RestartButton",    panelGO.transform, "RESTART TRIAL",        130f - btnStep);
        Button controls     = MakeButton("ControlsButton",  panelGO.transform, "CONTROLS",             130f - btnStep * 2f);
        Button trialSelect  = MakeButton("SettingsButton",   panelGO.transform, "TRIAL SELECTION",      130f - btnStep * 3f);
        Button returnMainMenu = MakeButton("ReturnToHubButton", panelGO.transform, "RETURN TO MAIN MENU", 130f - btnStep * 4f);

        SetField(pmc, "resumeButton", resume, bf);
        SetField(pmc, "restartButton", restart, bf);
        SetField(pmc, "controlsButton", controls, bf);
        SetField(pmc, "settingsButton", trialSelect, bf);
        SetField(pmc, "returnToHubButton", returnMainMenu, bf);

        // Set vertical navigation with wrap (matches MainScene)
        Button[] allBtns = { resume, restart, controls, trialSelect, returnMainMenu };
        for (int i = 0; i < allBtns.Length; i++)
        {
            Navigation nav = new Navigation();
            nav.mode = Navigation.Mode.Vertical;
            nav.wrapAround = true;
            allBtns[i].navigation = nav;
        }

        // Wire onClick listeners directly
        WireButtonListeners(pmc, resume, restart, controls, trialSelect, returnMainMenu);

        // ── Hide the panel (Awake already ran with null refs, so manually hide) ──
        panelGO.SetActive(false);
        panelCG.interactable = false;
        panelCG.blocksRaycasts = false;
        panelCG.alpha = 0f;
        overlayImg.gameObject.SetActive(false);
    }

    /// <summary>Directly wires onClick listeners for the 5-button Hub pause menu.</summary>
    private static void WireButtonListeners(PauseMenuController pmc,
        Button resume, Button restart, Button controls, Button trialSelect, Button returnMainMenu)
    {
        BindingFlags bf = BindingFlags.NonPublic | BindingFlags.Instance;

        if (resume != null)
            resume.onClick.AddListener(pmc.Resume);

        MethodInfo restartMI  = typeof(PauseMenuController).GetMethod("RestartTrial", bf);
        MethodInfo ctrlMI     = typeof(PauseMenuController).GetMethod("ShowControls", bf);
        MethodInfo tsMI       = typeof(PauseMenuController).GetMethod("OpenTrialSelection", bf);
        MethodInfo rtmMI      = typeof(PauseMenuController).GetMethod("ReturnToMainMenu", bf);

        if (restart != null && restartMI != null)
            restart.onClick.AddListener(() => restartMI.Invoke(pmc, null));

        if (controls != null && ctrlMI != null)
            controls.onClick.AddListener(() => ctrlMI.Invoke(pmc, null));

        if (trialSelect != null && tsMI != null)
            trialSelect.onClick.AddListener(() => tsMI.Invoke(pmc, null));

        if (returnMainMenu != null && rtmMI != null)
            returnMainMenu.onClick.AddListener(() => rtmMI.Invoke(pmc, null));
    }

    private static void SetField(PauseMenuController pmc, string fieldName, object value, BindingFlags bf)
    {
        FieldInfo field = typeof(PauseMenuController).GetField(fieldName, bf);
        if (field != null)
            field.SetValue(pmc, value);
    }

    private static Button MakeButton(string name, Transform parent, string label, float yOffset)
    {
        // Stretch horizontally like MainScene (anchor 0-1 at vertical center)
        GameObject btnGO = MakeRect(name, parent, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f));
        RectTransform rt = btnGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(-40f, 56f);
        rt.anchoredPosition = new Vector2(0f, yOffset);

        Image btnImg = btnGO.AddComponent<Image>();
        btnImg.color = BTN_IMG_COL;
        btnImg.raycastTarget = true;

        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = BTN_HIGHLIGHT;
        cb.selectedColor = BTN_SELECTED;
        cb.pressedColor = BTN_PRESSED;
        cb.fadeDuration = 0.05f;
        btn.colors = cb;

        // Label child (matches MainScene: fontSize=15, charSpacing=0)
        GameObject lblGO = MakeRect("Label", btnGO.transform, Vector2.zero, Vector2.one);
        TextMeshProUGUI tmp = lblGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 20f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = LABEL_WHITE;
        tmp.characterSpacing = 0f;
        tmp.raycastTarget = false;
        AssignFont(tmp);

        return btn;
    }

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

    private static void AssignFont(TextMeshProUGUI tmp)
    {
        CinzelFontHelper.Apply(tmp, tmp.fontStyle == FontStyles.Bold);
    }
}
