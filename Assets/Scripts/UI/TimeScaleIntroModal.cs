using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// A multi-page modal that explains the Time Scale mechanic in the Hub.
/// Triggers once when the player walks near its position.
/// When shown, reveals the TimeScaleMeter UI with a soft glowing highlight,
/// and highlights warning/danger zones on the relevant pages.
/// After dismissal the meter stays visible and time accrual begins.
/// </summary>
public class TimeScaleIntroModal : MonoBehaviour
{
    private static bool _hasShown = false;

    /// <summary>
    /// When true, TimeScaleLogic should not accumulate any time value.
    /// Set true on Awake, cleared when the modal is dismissed.
    /// </summary>
    public static bool IsTimeLocked { get; private set; } = false;

    private const float DETECT_RANGE = 2.0f;
    private const int GLOW_TEX_SIZE = 64;

    // Page indices for zone highlighting
    private const int PAGE_DANGER_ZONES = 2;

    private static readonly Color PANEL_BG       = new Color(0.04f, 0.06f, 0.12f, 0.95f);
    private static readonly Color ACCENT_GOLD    = new Color(0.961f, 0.784f, 0.259f, 1f);
    private static readonly Color ACCENT_ORANGE  = new Color(0.961f, 0.596f, 0.106f, 1f);
    private static readonly Color ACCENT_RED     = new Color(0.898f, 0.196f, 0.106f, 1f);
    private static readonly Color TEXT_WHITE      = new Color(0.91f, 0.918f, 0.965f, 1f);
    private static readonly Color BTN_BG         = new Color(0.08f, 0.12f, 0.22f, 1f);

    private static readonly string[] PAGES = new string[]
    {
        "<color=#F5C842><size=32>TIME SCALE</size></color>\n\n" +
        "You control a <b>global time value</b>.\n\n" +
        "It starts at <b>zero</b> when each level begins.\n\n" +
        "<color=#F5C842><b>Stand upright</b></color> to push time forward.\n" +
        "<color=#9B5DE5><b>Flip upside down</b></color> to pull it back.\n" +
        "<color=#5AB4F0><b>Lay flat</b></color> to freeze it exactly where it is.",

        "<color=#F5C842><size=32>THE METER</size></color>\n\n" +
        "Look at the bar at the top of the screen.\n\n" +
        "The <b>marker</b> shows your current time value.\n" +
        "The <b>center line</b> is zero \u2014 where you started.\n" +
        "Push time forward and the bar fills <b>right</b>.\n" +
        "Reverse and it fills <b>left</b>.\n\n" +
        "The numbers on each end show the level's\n" +
        "<b>full time range</b>.",

        "<color=#F5C842><size=32>DANGER ZONES</size></color>\n\n" +
        "During <b>boss fights</b>, thresholds appear\n" +
        "near the edges of the meter.\n\n" +
        "Push time too far and you enter the\n" +
        "<color=#F59819>warning zone</color> \u2014 the bar glows\n" +
        "<color=#F59819>orange</color> as a heads-up.\n\n" +
        "Keep going and you hit the <color=#E53219>danger zone</color>.\n" +
        "The bar pulses <color=#E53219>red</color>. If time reaches the\n" +
        "edge of the bar, you <b>lose</b>.",

        "<color=#F5C842><size=32>THE BOSS</size></color>\n\n" +
        "At the end of each level, a <b>boss</b> appears.\n\n" +
        "During the fight, time keeps moving \u2014 the\n" +
        "meter doesn't stop. The thresholds become\n" +
        "your lifeline.\n\n" +
        "Manage your position on the bar carefully.\n" +
        "Run out of room and it's over.",

        "<color=#F5C842><size=32>OBJECT LIMITS</size></color>\n\n" +
        "Each object has its own <b>time range</b>.\n\n" +
        "A platform might only move between <b>-2</b> and\n" +
        "<b>+2</b>, even if global time goes well past that.\n\n" +
        "When an object hits its limit, it <b>stops</b> \u2014\n" +
        "but everything else keeps going.\n" +
        "Use this to solve every puzzle.\n\n" +
        "<b>Good luck.</b>"
    };

    private GameObject _modalGO;
    private TextMeshProUGUI _bodyTMP;
    private TextMeshProUGUI _pageIndicator;
    private TextMeshProUGUI _btnLabelTMP;
    private Button _continueBtn;
    private Button _backBtn;
    private int _currentPage;
    private bool _isOpen;
    private float _lastPageChangeTime;

    // Tracked icons for input-mode updates
    private Image _backBtnIcon;
    private Image _continueBtnIcon;

    // Soft glow behind the TimeScaleMeter
    private GameObject _meterGlowGO;
    private Image _meterGlowImage;
    private Texture2D _glowTex;

    // Zone highlight overlays (shown on the danger-zones page)
    private readonly List<GameObject> _zoneGlowGOs = new List<GameObject>();
    private readonly List<Image> _zoneGlowImages = new List<Image>();

    void Awake()
    {
        // Reset on every scene load so hub retry re-shows the modal
        _hasShown = false;
        IsTimeLocked = true;
    }

    void Update()
    {
        if (_hasShown) return;
        if (_isOpen) return;

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.transform.position);
        if (dist < DETECT_RANGE)
        {
            _hasShown = true;
            Show();
        }
    }

    /// <summary>Shows the modal, reveals the TimeScaleMeter, and pauses gameplay.</summary>
    private void Show()
    {
        _isOpen = true;
        _currentPage = 0;
        Time.timeScale = 0f;

        InputPromptManager.OnInputModeChanged += OnInputModeChanged;

        // Hide any tutorial tile tooltips that might be showing behind the modal
        DismissTutorialPopups();

        RevealMeterWithGlow();
        BuildUI();
        UpdatePage();
    }

    /// <summary>Hides any active TutorialTilePopup shared popup so it doesn't bleed through.</summary>
    private void DismissTutorialPopups()
    {
        GameObject popup = GameObject.Find("TutorialPopup_Shared");
        if (popup != null)
            popup.SetActive(false);
    }

    // ── Soft Glow Texture ────────────────────────────────────────────────

    /// <summary>
    /// Generates an elliptical radial-gradient texture for soft glow effects.
    /// Center is fully opaque, edges fade to transparent.
    /// </summary>
    private Texture2D CreateSoftGlowTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float half = size * 0.5f;
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - half) / half;
                float dy = (y - half) / half;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                // Smooth falloff: 1 at center, 0 at edge
                float alpha = Mathf.Clamp01(1f - dist);
                alpha = alpha * alpha; // quadratic for softer falloff
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    /// <summary>Creates a soft glow Image using the radial gradient sprite.</summary>
    private Image CreateSoftGlowImage(GameObject go, Color tint)
    {
        if (_glowTex == null)
            _glowTex = CreateSoftGlowTexture(GLOW_TEX_SIZE);

        Sprite sp = Sprite.Create(_glowTex,
            new Rect(0, 0, _glowTex.width, _glowTex.height),
            new Vector2(0.5f, 0.5f), 100f);

        Image img = go.AddComponent<Image>();
        img.sprite = sp;
        img.type = Image.Type.Sliced;
        img.color = tint;
        img.raycastTarget = false;
        return img;
    }

    // ── Meter Glow ──────────────────────────────────────────────────────

    /// <summary>
    /// Finds the TimeScaleMeter, activates it, and adds a soft pulsing glow
    /// behind it so the player notices it appeared.
    /// </summary>
    private void RevealMeterWithGlow()
    {
        TimeScaleMeter meter = FindObjectOfType<TimeScaleMeter>(true);
        if (meter == null) return;

        meter.gameObject.SetActive(true);

        _meterGlowGO = new GameObject("MeterGlow", typeof(RectTransform));
        _meterGlowGO.transform.SetParent(meter.transform.parent, false);

        RectTransform glowRT = _meterGlowGO.GetComponent<RectTransform>();
        RectTransform meterRT = meter.GetComponent<RectTransform>();
        glowRT.anchorMin = meterRT.anchorMin;
        glowRT.anchorMax = meterRT.anchorMax;
        glowRT.pivot = meterRT.pivot;
        glowRT.anchoredPosition = meterRT.anchoredPosition;
        glowRT.sizeDelta = meterRT.sizeDelta + new Vector2(60f, 40f);

        // Place behind meter
        _meterGlowGO.transform.SetSiblingIndex(meter.transform.GetSiblingIndex());

        _meterGlowImage = CreateSoftGlowImage(_meterGlowGO,
            new Color(ACCENT_GOLD.r, ACCENT_GOLD.g, ACCENT_GOLD.b, 0.6f));

        StartCoroutine(PulseGlowRoutine());
    }

    /// <summary>Removes the gold meter glow.</summary>
    private void RemoveMeterGlow()
    {
        if (_meterGlowGO != null)
        {
            Destroy(_meterGlowGO);
            _meterGlowGO = null;
            _meterGlowImage = null;
        }
    }

    /// <summary>Pulses the soft glow behind the meter while it exists.</summary>
    private IEnumerator PulseGlowRoutine()
    {
        float t = 0f;
        TimeScaleMeter meter = FindObjectOfType<TimeScaleMeter>();
        RectTransform meterRT = meter != null ? meter.GetComponent<RectTransform>() : null;

        while (_isOpen && _meterGlowImage != null)
        {
            t += Time.unscaledDeltaTime * 2.0f;
            float wave = (Mathf.Sin(t) + 1f) * 0.5f;
            float a = Mathf.Lerp(0.3f, 0.7f, wave);
            _meterGlowImage.color = new Color(ACCENT_GOLD.r, ACCENT_GOLD.g, ACCENT_GOLD.b, a);

            if (meterRT != null && _meterGlowGO != null)
            {
                float expand = Mathf.Lerp(40f, 70f, wave);
                RectTransform glowRT = _meterGlowGO.GetComponent<RectTransform>();
                glowRT.sizeDelta = meterRT.sizeDelta + new Vector2(expand, expand * 0.7f);
            }
            yield return null;
        }
    }

    // ── Zone Highlights ─────────────────────────────────────────────────

    /// <summary>
    /// Creates soft glow overlays on the warning and danger zones of the meter bar.
    /// Called when navigating to the danger zones page.
    /// Also removes the gold meter glow so it doesn't compete visually.
    /// </summary>
    private void ShowZoneHighlights()
    {
        ClearZoneHighlights();
        RemoveMeterGlow();

        TimeScaleMeter meter = FindObjectOfType<TimeScaleMeter>();
        if (meter == null) return;

        // The meter bar occupies anchors (0.04, 0.22) to (0.96, 0.55) within the meter.
        // Calculate zone positions using the same math as TimeScaleMeter.Build().
        float minV = TimeScaleLogic.Instance != null ? TimeScaleLogic.Instance.minValue : -10f;
        float maxV = TimeScaleLogic.Instance != null ? TimeScaleLogic.Instance.maxValue :  10f;
        float dng  = TimeScaleLogic.Instance != null ? TimeScaleLogic.Instance.dangerZone : 8f;
        float wrn  = TimeScaleLogic.Instance != null ? TimeScaleLogic.Instance.warningZone : 5f;
        float total = maxV - minV;

        float dHi = total > 0 ? Mathf.Clamp01((dng - minV) / total) : 0.8f;
        float dLo = total > 0 ? Mathf.Clamp01((-dng - minV) / total) : 0.2f;
        float wHi = total > 0 ? Mathf.Clamp01((wrn - minV) / total) : 0.75f;
        float wLo = total > 0 ? Mathf.Clamp01((-wrn - minV) / total) : 0.25f;

        // Map bar-relative anchors to meter-relative anchors
        // Bar is at (0.04, 0.22) to (0.96, 0.55) within the meter
        const float barL = 0.04f, barR = 0.96f, barB = 0.10f, barT = 0.65f;
        float barW = barR - barL;

        // Warning zones (orange glow)
        CreateZoneGlow(meter.transform, barL, barL + barW * wLo, barB, barT,
            ACCENT_ORANGE, 0.5f);
        CreateZoneGlow(meter.transform, barL + barW * wHi, barR, barB, barT,
            ACCENT_ORANGE, 0.5f);

        // Danger zones (red glow, on top)
        CreateZoneGlow(meter.transform, barL, barL + barW * Mathf.Max(dLo, 0), barB, barT,
            ACCENT_RED, 0.6f);
        CreateZoneGlow(meter.transform, barL + barW * Mathf.Min(dHi, 1), barR, barB, barT,
            ACCENT_RED, 0.6f);

        StartCoroutine(PulseZoneGlowsRoutine());
    }

    /// <summary>Creates a single soft zone glow overlay within the meter.</summary>
    private void CreateZoneGlow(Transform parent, float xMin, float xMax, float yMin, float yMax,
        Color tint, float baseAlpha)
    {
        GameObject go = new GameObject("ZoneGlow", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        // Expand slightly beyond zone bounds for soft edge bleed
        float padX = (xMax - xMin) * 0.3f;
        float padY = (yMax - yMin) * 0.5f;
        rt.anchorMin = new Vector2(xMin - padX, yMin - padY);
        rt.anchorMax = new Vector2(xMax + padX, yMax + padY);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        Color c = new Color(tint.r, tint.g, tint.b, baseAlpha);
        Image img = CreateSoftGlowImage(go, c);

        _zoneGlowGOs.Add(go);
        _zoneGlowImages.Add(img);
    }

    /// <summary>Pulses zone glow overlays while they're visible.</summary>
    private IEnumerator PulseZoneGlowsRoutine()
    {
        float t = 0f;
        while (_zoneGlowImages.Count > 0 && _isOpen)
        {
            t += Time.unscaledDeltaTime * 3f;
            float wave = (Mathf.Sin(t) + 1f) * 0.5f;

            for (int i = 0; i < _zoneGlowImages.Count; i++)
            {
                if (_zoneGlowImages[i] == null) continue;
                Color c = _zoneGlowImages[i].color;
                // Pulse between 40% and 80% of original alpha
                c.a = Mathf.Lerp(0.25f, 0.7f, wave);
                _zoneGlowImages[i].color = c;
            }
            yield return null;
        }
    }

    /// <summary>Removes all zone glow overlays.</summary>
    private void ClearZoneHighlights()
    {
        foreach (GameObject go in _zoneGlowGOs)
        {
            if (go != null) Destroy(go);
        }
        _zoneGlowGOs.Clear();
        _zoneGlowImages.Clear();
    }

    // ── UI Build ────────────────────────────────────────────────────────

    private void BuildUI()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        // Full-screen overlay
        _modalGO = new GameObject("TimeScaleIntroModal");
        _modalGO.transform.SetParent(canvas.transform, false);
        RectTransform overlayRT = _modalGO.AddComponent<RectTransform>();
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.sizeDelta = Vector2.zero;
        Image overlayImg = _modalGO.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.6f);
        overlayImg.raycastTarget = true;

        // Center panel — positioned slightly below center to leave room for the meter
        GameObject panel = MakeRect("Panel", _modalGO.transform, new Vector2(0.5f, 0.45f), new Vector2(0.5f, 0.45f));
        RectTransform panelRT = panel.GetComponent<RectTransform>();
        panelRT.sizeDelta = new Vector2(700f, 600f);
        Image panelImg = panel.AddComponent<Image>();
        panelImg.color = PANEL_BG;

        // Body text — anchored to top, sized to stop well above buttons
        // Panel=560, top padding=35, body=360, leaving 165px for buttons+indicator
        GameObject bodyGO = MakeRect("Body", panel.transform, new Vector2(0f, 1f), new Vector2(1f, 1f));
        RectTransform bodyRT = bodyGO.GetComponent<RectTransform>();
        bodyRT.pivot = new Vector2(0.5f, 1f);
        bodyRT.sizeDelta = new Vector2(-80f, 400f);
        bodyRT.anchoredPosition = new Vector2(0f, -35f);
        _bodyTMP = bodyGO.AddComponent<TextMeshProUGUI>();
        _bodyTMP.fontSize = 26f;
        _bodyTMP.color = TEXT_WHITE;
        _bodyTMP.alignment = TextAlignmentOptions.TopLeft;
        _bodyTMP.richText = true;
        _bodyTMP.enableWordWrapping = true;
        _bodyTMP.overflowMode = TextOverflowModes.Ellipsis;
        _bodyTMP.raycastTarget = false;
        CinzelFontHelper.Apply(_bodyTMP);

        // ── Button row container — pinned to bottom with clear spacing ─────
        GameObject btnRow = MakeRect("ButtonRow", panel.transform, new Vector2(0f, 0f), new Vector2(1f, 0f));
        RectTransform btnRowRT = btnRow.GetComponent<RectTransform>();
        btnRowRT.pivot = new Vector2(0.5f, 0f);
        btnRowRT.sizeDelta = new Vector2(-60f, 64f);
        btnRowRT.anchoredPosition = new Vector2(0f, 20f);

        HorizontalLayoutGroup btnHLG = btnRow.AddComponent<HorizontalLayoutGroup>();
        btnHLG.spacing = 20f;
        btnHLG.childAlignment = TextAnchor.MiddleCenter;
        btnHLG.childControlWidth = true;
        btnHLG.childControlHeight = true;
        btnHLG.childForceExpandWidth = true;
        btnHLG.childForceExpandHeight = true;
        btnHLG.padding = new RectOffset(10, 10, 0, 0);

        // Page indicator (left side, above buttons)
        GameObject indicatorGO = MakeRect("PageIndicator", panel.transform, new Vector2(0f, 0f), new Vector2(0.5f, 0f));
        RectTransform indRT = indicatorGO.GetComponent<RectTransform>();
        indRT.pivot = new Vector2(0f, 0f);
        indRT.sizeDelta = new Vector2(0f, 28f);
        indRT.anchoredPosition = new Vector2(40f, 90f);
        _pageIndicator = indicatorGO.AddComponent<TextMeshProUGUI>();
        _pageIndicator.fontSize = 16f;
        _pageIndicator.color = new Color(TEXT_WHITE.r, TEXT_WHITE.g, TEXT_WHITE.b, 0.4f);
        _pageIndicator.alignment = TextAlignmentOptions.BottomLeft;
        _pageIndicator.raycastTarget = false;
        CinzelFontHelper.Apply(_pageIndicator);

        // ── Back button (left) ─────────────────────────────────────────
        GameObject backGO = new GameObject("BackBtn");
        backGO.transform.SetParent(btnRow.transform, false);
        Image backImg = backGO.AddComponent<Image>();
        backImg.color = BTN_BG;
        _backBtn = backGO.AddComponent<Button>();
        _backBtn.targetGraphic = backImg;
        ColorBlock cb = ColorBlock.defaultColorBlock;
        cb.normalColor = Color.white;
        cb.highlightedColor = ACCENT_GOLD;
        cb.selectedColor = ACCENT_GOLD;
        cb.pressedColor = new Color(0.7f, 0.5f, 0.1f, 1f);
        cb.fadeDuration = 0.05f;
        _backBtn.colors = cb;
        _backBtn.onClick.AddListener(OnBackClicked);

        // Back button: icon + label in HorizontalLayoutGroup
        HorizontalLayoutGroup backHLG = backGO.AddComponent<HorizontalLayoutGroup>();
        backHLG.spacing = 8f;
        backHLG.childAlignment = TextAnchor.MiddleCenter;
        backHLG.childControlWidth = false;
        backHLG.childControlHeight = false;
        backHLG.childForceExpandWidth = false;
        backHLG.childForceExpandHeight = false;
        backHLG.padding = new RectOffset(12, 12, 4, 4);

        Sprite backSprite = ControllerIcons.BackIcon;
        if (backSprite != null)
        {
            _backBtnIcon = ControllerIcons.CreateIcon(backGO.transform, backSprite, 32f);
            if (_backBtnIcon != null)
            {
                LayoutElement bLE = _backBtnIcon.gameObject.AddComponent<LayoutElement>();
                bLE.preferredWidth = 32f;
                bLE.preferredHeight = 32f;
            }
        }

        GameObject backLabelGO = new GameObject("Label");
        backLabelGO.transform.SetParent(backGO.transform, false);
        TextMeshProUGUI backLabelTMP = backLabelGO.AddComponent<TextMeshProUGUI>();
        backLabelTMP.fontSize = 20f;
        backLabelTMP.color = TEXT_WHITE;
        backLabelTMP.alignment = TextAlignmentOptions.MidlineLeft;
        backLabelTMP.text = "BACK";
        backLabelTMP.raycastTarget = false;
        CinzelFontHelper.Apply(backLabelTMP, true);
        LayoutElement backLblLE = backLabelGO.AddComponent<LayoutElement>();
        backLblLE.preferredWidth = 80f;
        backLblLE.preferredHeight = 32f;

        // ── Continue button (right) ────────────────────────────────────
        GameObject btnGO = new GameObject("ContinueBtn");
        btnGO.transform.SetParent(btnRow.transform, false);
        Image btnImg = btnGO.AddComponent<Image>();
        btnImg.color = BTN_BG;
        _continueBtn = btnGO.AddComponent<Button>();
        _continueBtn.targetGraphic = btnImg;
        _continueBtn.colors = cb;
        _continueBtn.onClick.AddListener(OnContinueClicked);

        // Continue button: label + icon in HorizontalLayoutGroup
        HorizontalLayoutGroup contHLG = btnGO.AddComponent<HorizontalLayoutGroup>();
        contHLG.spacing = 8f;
        contHLG.childAlignment = TextAnchor.MiddleCenter;
        contHLG.childControlWidth = false;
        contHLG.childControlHeight = false;
        contHLG.childForceExpandWidth = false;
        contHLG.childForceExpandHeight = false;
        contHLG.padding = new RectOffset(12, 12, 4, 4);

        GameObject btnLabelGO = new GameObject("Label");
        btnLabelGO.transform.SetParent(btnGO.transform, false);
        _btnLabelTMP = btnLabelGO.AddComponent<TextMeshProUGUI>();
        _btnLabelTMP.fontSize = 20f;
        _btnLabelTMP.color = TEXT_WHITE;
        _btnLabelTMP.alignment = TextAlignmentOptions.MidlineRight;
        _btnLabelTMP.raycastTarget = false;
        CinzelFontHelper.Apply(_btnLabelTMP, true);
        LayoutElement contLblLE = btnLabelGO.AddComponent<LayoutElement>();
        contLblLE.preferredWidth = 120f;
        contLblLE.preferredHeight = 32f;

        Sprite confirmSprite = ControllerIcons.ConfirmIcon;
        if (confirmSprite != null)
        {
            _continueBtnIcon = ControllerIcons.CreateIcon(btnGO.transform, confirmSprite, 32f);
            if (_continueBtnIcon != null)
            {
                LayoutElement cLE = _continueBtnIcon.gameObject.AddComponent<LayoutElement>();
                cLE.preferredWidth = 32f;
                cLE.preferredHeight = 32f;
            }
        }

        // Wire navigation between Continue and Back
        Navigation contNav = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = _backBtn };
        _continueBtn.navigation = contNav;
        Navigation bNav = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = _continueBtn };
        _backBtn.navigation = bNav;

        StartCoroutine(InputListenRoutine());
    }

    private const float PAGE_COOLDOWN = 0.25f;

    /// <summary>Returns true if enough time has passed since the last page change.</summary>
    private bool CanChangePage()
    {
        return Time.unscaledTime - _lastPageChangeTime >= PAGE_COOLDOWN;
    }

    /// <summary>Click handler for the Continue button.</summary>
    private void OnContinueClicked()
    {
        if (!CanChangePage()) return;
        NextPage();
    }

    /// <summary>Click handler for the Back button.</summary>
    private void OnBackClicked()
    {
        if (!CanChangePage()) return;
        PrevPage();
    }

    /// <summary>
    /// Listens for controller/keyboard input while the modal is open.
    /// yield return null works even at Time.timeScale = 0.
    /// </summary>
    private IEnumerator InputListenRoutine()
    {
        // Block for a short window so the input that opened the modal doesn't immediately advance
        _lastPageChangeTime = Time.unscaledTime;

        while (_isOpen)
        {
            if (CanChangePage())
            {
                bool nextPressed = Input.GetKeyDown(KeyCode.Space)
                                || Input.GetKeyDown(KeyCode.JoystickButton0)
                                || Input.GetKeyDown(KeyCode.Return);

                bool backPressed = Input.GetKeyDown(KeyCode.JoystickButton1)
                                || Input.GetKeyDown(KeyCode.Escape)
                                || Input.GetKeyDown(KeyCode.Backspace);

                if (nextPressed)
                    NextPage();
                else if (backPressed)
                    PrevPage();
            }

            yield return null;
        }
    }

    // ── Page Navigation ─────────────────────────────────────────────────

    private void UpdatePage()
    {
        if (_bodyTMP == null || _pageIndicator == null) return;

        _bodyTMP.text = PAGES[_currentPage];
        _pageIndicator.text = $"{_currentPage + 1} / {PAGES.Length}";

        if (_btnLabelTMP != null)
            _btnLabelTMP.text = _currentPage < PAGES.Length - 1 ? "CONTINUE" : "GOT IT";

        // Show/hide Back button on first page
        if (_backBtn != null)
            _backBtn.gameObject.SetActive(_currentPage > 0);

        // Show/hide zone highlights based on current page
        if (_currentPage == PAGE_DANGER_ZONES)
            ShowZoneHighlights();
        else
            ClearZoneHighlights();
    }

    private void NextPage()
    {
        if (!CanChangePage()) return;
        _lastPageChangeTime = Time.unscaledTime;

        _currentPage++;
        if (_currentPage >= PAGES.Length)
        {
            Dismiss();
            return;
        }
        UpdatePage();
    }

    private void PrevPage()
    {
        if (!CanChangePage()) return;
        _lastPageChangeTime = Time.unscaledTime;

        if (_currentPage > 0)
        {
            _currentPage--;
            UpdatePage();
        }
    }

    /// <summary>
    /// Closes the modal, removes glows, keeps meter visible with zones shown,
    /// and unlocks time accrual.
    /// </summary>
    private void Dismiss()
    {
        _isOpen = false;
        Time.timeScale = 1f;
        IsTimeLocked = false;

        InputPromptManager.OnInputModeChanged -= OnInputModeChanged;

        ClearZoneHighlights();
        RemoveMeterGlow();

        if (_modalGO != null)
            Destroy(_modalGO);

        if (_glowTex != null)
        {
            Destroy(_glowTex);
            _glowTex = null;
        }

        // Enable always-show zones on the meter so the player can experience
        // the warning/danger zone visuals while playing in the hub
        TimeScaleMeter meter = FindObjectOfType<TimeScaleMeter>();
        if (meter != null)
            meter.AlwaysShowZones = true;
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    /// <summary>Updates button icons when input mode changes.</summary>
    private void OnInputModeChanged(InputPromptManager.InputMode newMode)
    {
        if (!_isOpen) return;

        if (_backBtnIcon != null)
            _backBtnIcon.sprite = ControllerIcons.BackIcon;

        if (_continueBtnIcon != null)
            _continueBtnIcon.sprite = ControllerIcons.ConfirmIcon;
    }

    private static GameObject MakeRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        return go;
    }
}
