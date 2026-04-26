using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// One-time modal popup that warns the player about death tiles in the Garden level.
/// Triggers on the first forward move (W / left-stick up) and pauses gameplay
/// until dismissed, following the same modal pattern as <see cref="BossIntroModal"/>.
/// Attach this component to any GameObject in the Garden scene.
/// </summary>
public class DeathTileTutorial : MonoBehaviour
{
    /// <summary>True while the modal is displayed. Blocks pause menu.</summary>
    public static bool IsOpen { get; private set; }

    // ── Visual constants (matches BossIntroModal style with red accent) ──
    private static readonly Color PANEL_BG    = new Color(0.04f, 0.06f, 0.12f, 0.95f);
    private static readonly Color OVERLAY_BG  = new Color(0f, 0f, 0f, 0.55f);
    private static readonly Color ACCENT_RED  = new Color(0.898f, 0.196f, 0.106f, 1f);
    private static readonly Color TEXT_WHITE   = new Color(0.91f, 0.918f, 0.965f, 1f);
    private static readonly Color TEXT_DIM     = new Color(0.91f, 0.918f, 0.965f, 0.65f);
    private static readonly Color BTN_BG      = new Color(0.85f, 0.20f, 0.15f, 1f);
    private static readonly Color BTN_HOVER   = new Color(1f, 0.35f, 0.25f, 1f);
    private static readonly Color BTN_PRESS   = new Color(0.6f, 0.12f, 0.08f, 1f);

    private const float PANEL_WIDTH  = 620f;
    private const float PANEL_HEIGHT = 340f;

    private const string TITLE_TEXT = "!!  DANGER \u2014 DEATH TILES";
    private const string BODY_TEXT  = "The white tiles ahead are <color=#E63219>deadly temporal vines</color>.\n\n" +
                                      "Stepping on them will <color=#E63219>end your trial instantly</color>.\n\n" +
                                      "Navigate carefully around them to survive.";

    private bool _triggered;
    private bool _waitingForInput;
    private GameObject _modalGO;
    private Image _btnIcon;

    void Start()
    {
        _waitingForInput = true;
    }

    void Update()
    {
        if (_triggered) return;

        if (_waitingForInput)
        {
            // Detect first forward input: W key or left stick up
            bool forwardPressed = Input.GetKeyDown(KeyCode.W)
                               || Input.GetAxis("Vertical") > 0.5f;

            if (forwardPressed)
            {
                _triggered = true;
                _waitingForInput = false;
                ShowModal();
            }
        }

        // Handle dismiss input while modal is open
        if (IsOpen)
        {
            bool confirm = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)
                        || Input.GetKeyDown(KeyCode.JoystickButton0)
                        || Input.GetMouseButtonDown(0);
            if (confirm) Dismiss();
        }
    }

    void OnDestroy()
    {
        InputPromptManager.OnInputModeChanged -= OnInputModeChanged;
        if (IsOpen) Dismiss();
    }

    // ================================================================
    //  MODAL LIFECYCLE
    // ================================================================

    private void ShowModal()
    {
        IsOpen = true;
        Time.timeScale = 0f;
        InputPromptManager.OnInputModeChanged += OnInputModeChanged;
        BuildUI();
    }

    private void Dismiss()
    {
        IsOpen = false;
        Time.timeScale = 1f;
        InputPromptManager.OnInputModeChanged -= OnInputModeChanged;
        if (_modalGO != null) Destroy(_modalGO);
        _modalGO = null;
        _btnIcon = null;
    }

    /// <summary>Swaps the button icon when input mode changes.</summary>
    private void OnInputModeChanged(InputPromptManager.InputMode newMode)
    {
        if (!IsOpen) return;
        if (_btnIcon != null) _btnIcon.sprite = ControllerIcons.ConfirmIcon;
    }

    // ================================================================
    //  UI CONSTRUCTION (follows BossIntroModal pattern)
    // ================================================================

    private void BuildUI()
    {
        if (_modalGO != null) Destroy(_modalGO);

        // Root overlay canvas (sort order 200, same as BossIntroModal)
        _modalGO = new GameObject("DeathTileTutorial_Canvas");
        Canvas canvas = _modalGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        CanvasScaler scaler = _modalGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        _modalGO.AddComponent<GraphicRaycaster>();

        // Full-screen dimmer overlay
        GameObject overlay = MakeRect("Overlay", _modalGO.transform, Vector2.zero, Vector2.one);
        Image overlayImg = overlay.AddComponent<Image>();
        overlayImg.color = OVERLAY_BG;
        overlayImg.raycastTarget = true;

        // Center panel
        GameObject panel = MakeRect("Panel", _modalGO.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        RectTransform panelRT = panel.GetComponent<RectTransform>();
        panelRT.sizeDelta = new Vector2(PANEL_WIDTH, PANEL_HEIGHT);
        Image panelImg = panel.AddComponent<Image>();
        panelImg.color = PANEL_BG;

        // Red accent bar (top edge)
        GameObject accent = MakeRect("Accent", panel.transform,
            new Vector2(0f, 1f), new Vector2(1f, 1f));
        RectTransform accentRT = accent.GetComponent<RectTransform>();
        accentRT.pivot = new Vector2(0.5f, 1f);
        accentRT.sizeDelta = new Vector2(0f, 4f);
        accentRT.anchoredPosition = Vector2.zero;
        Image accentImg = accent.AddComponent<Image>();
        accentImg.color = ACCENT_RED;
        accentImg.raycastTarget = false;

        // Title
        GameObject titleGO = MakeRect("Title", panel.transform,
            new Vector2(0f, 1f), new Vector2(1f, 1f));
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.sizeDelta = new Vector2(-60f, 50f);
        titleRT.anchoredPosition = new Vector2(0f, -20f);
        TextMeshProUGUI titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text = TITLE_TEXT;
        titleTMP.fontSize = 28f;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color = ACCENT_RED;
        titleTMP.alignment = TextAlignmentOptions.MidlineLeft;
        titleTMP.characterSpacing = 4f;
        titleTMP.raycastTarget = false;
        CinzelFontHelper.Apply(titleTMP, true);

        // Body text
        GameObject bodyGO = MakeRect("Body", panel.transform,
            new Vector2(0f, 1f), new Vector2(1f, 1f));
        RectTransform bodyRT = bodyGO.GetComponent<RectTransform>();
        bodyRT.pivot = new Vector2(0.5f, 1f);
        bodyRT.sizeDelta = new Vector2(-60f, 160f);
        bodyRT.anchoredPosition = new Vector2(0f, -80f);
        TextMeshProUGUI bodyTMP = bodyGO.AddComponent<TextMeshProUGUI>();
        bodyTMP.text = BODY_TEXT;
        bodyTMP.fontSize = 21f;
        bodyTMP.color = TEXT_WHITE;
        bodyTMP.alignment = TextAlignmentOptions.TopLeft;
        bodyTMP.richText = true;
        bodyTMP.enableWordWrapping = true;
        bodyTMP.raycastTarget = false;
        CinzelFontHelper.Apply(bodyTMP);

        // Button row — pinned to bottom
        GameObject btnRow = MakeRect("ButtonRow", panel.transform,
            new Vector2(0f, 0f), new Vector2(1f, 0f));
        RectTransform btnRowRT = btnRow.GetComponent<RectTransform>();
        btnRowRT.pivot = new Vector2(0.5f, 0f);
        btnRowRT.sizeDelta = new Vector2(-60f, 56f);
        btnRowRT.anchoredPosition = new Vector2(0f, 20f);

        HorizontalLayoutGroup hlg = btnRow.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 20f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;
        hlg.padding = new RectOffset(10, 10, 0, 0);

        // Continue button
        GameObject btnGO = new GameObject("ContinueBtn");
        btnGO.transform.SetParent(btnRow.transform, false);
        Image btnImg = btnGO.AddComponent<Image>();
        btnImg.color = BTN_BG;

        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        ColorBlock cb = ColorBlock.defaultColorBlock;
        cb.normalColor      = Color.white;
        cb.highlightedColor = BTN_HOVER;
        cb.selectedColor    = BTN_HOVER;
        cb.pressedColor     = BTN_PRESS;
        cb.fadeDuration     = 0.05f;
        btn.colors = cb;
        btn.onClick.AddListener(Dismiss);

        // Button inner layout (icon + label)
        HorizontalLayoutGroup btnHlg = btnGO.AddComponent<HorizontalLayoutGroup>();
        btnHlg.spacing = 8f;
        btnHlg.childAlignment = TextAnchor.MiddleCenter;
        btnHlg.childControlWidth = false;
        btnHlg.childControlHeight = false;
        btnHlg.childForceExpandWidth = false;
        btnHlg.childForceExpandHeight = false;
        btnHlg.padding = new RectOffset(12, 12, 4, 4);

        // Controller / mouse icon (dynamically swaps with input mode)
        Sprite confirmIcon = ControllerIcons.ConfirmIcon;
        if (confirmIcon != null)
        {
            _btnIcon = ControllerIcons.CreateIcon(btnGO.transform, confirmIcon, 32f);
            if (_btnIcon != null)
            {
                LayoutElement ile = _btnIcon.gameObject.AddComponent<LayoutElement>();
                ile.preferredWidth = 32f;
                ile.preferredHeight = 32f;
            }
        }

        // Button label
        GameObject lblGO = new GameObject("Label");
        lblGO.transform.SetParent(btnGO.transform, false);
        TextMeshProUGUI lblTMP = lblGO.AddComponent<TextMeshProUGUI>();
        lblTMP.text = "UNDERSTOOD";
        lblTMP.fontSize = 22f;
        lblTMP.color = TEXT_WHITE;
        lblTMP.alignment = TextAlignmentOptions.MidlineLeft;
        lblTMP.raycastTarget = false;
        CinzelFontHelper.Apply(lblTMP, true);
        LayoutElement lle = lblGO.AddComponent<LayoutElement>();
        lle.preferredWidth = 160f;
        lle.preferredHeight = 32f;

        // Select the button for EventSystem
        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(btnGO);
    }

    // ================================================================
    //  HELPERS
    // ================================================================

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
