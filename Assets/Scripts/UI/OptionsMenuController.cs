using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Controls the Options menu: tab switching (click, LB/RB),
/// slider/toggle interaction with controller hold-A + LS support,
/// and origin-aware back navigation.
/// Built at runtime by <see cref="OptionsMenuBuilder"/>.
/// </summary>
public class OptionsMenuController : MonoBehaviour
{
    /// <summary>Where this screen was opened from.</summary>
    public enum OptionsOrigin { MainMenu, PauseMenu }

    /// <summary>Set before enabling to control where B/back returns to.</summary>
    public OptionsOrigin Origin { get; set; } = OptionsOrigin.MainMenu;

    // ── Assigned by OptionsMenuBuilder ───────────────────────────────────
    internal Button[] tabButtons;
    internal GameObject[] tabContents;
    internal Button backButton;
    internal CanvasGroup canvasGroup;
    internal RectTransform tooltipRT;
    internal GameObject controllerTooltip;
    internal GameObject lbHint;
    internal GameObject rbHint;

    // GAME tab — sliders
    internal Slider mouseSensSlider;
    internal Slider stickSensSlider;
    internal Slider leftDeadzoneSlider;
    internal Slider rightDeadzoneSlider;
    internal Toggle invertYToggle;

    // GAME tab — row GameObjects for input-mode visibility and repositioning
    internal GameObject cameraHeader;
    internal GameObject mouseSensRow;
    internal GameObject stickSensRow;
    internal GameObject deadzoneHeader;
    internal GameObject leftDeadzoneRow;
    internal GameObject rightDeadzoneRow;
    internal GameObject invertYRow;
    internal GameObject gameResetRow;

    // AUDIO tab
    internal Slider masterVolSlider;
    internal Slider musicVolSlider;
    internal Slider sfxVolSlider;

    // VIDEO tab
    internal Toggle vsyncToggle;
    internal TextMeshProUGUI resolutionLabel;
    internal TextMeshProUGUI displayModeLabel;
    internal TextMeshProUGUI qualityLabel;

    // Value display labels
    internal TextMeshProUGUI masterVolValue;
    internal TextMeshProUGUI musicVolValue;
    internal TextMeshProUGUI sfxVolValue;
    internal TextMeshProUGUI mouseSensValue;
    internal TextMeshProUGUI stickSensValue;
    internal TextMeshProUGUI leftDeadzoneValue;
    internal TextMeshProUGUI rightDeadzoneValue;

    // Reset buttons per tab
    internal Button[] resetButtons;

    private int _activeTab;
    private bool _initialized;
    private Resolution[] _resolutions;
    private int _resIndex;
    private int _displayMode;
    private int _qualityIndex;

    private const float TAB_INPUT_COOLDOWN = 0.2f;
    private float _tabCooldown;

    // ── Controller slider drag state ────────────────────────────────────
    private bool _isHoldingSlider;
    private Slider _dragSlider; // the slider being actively adjusted via hold-A
    private const float SLIDER_STICK_SPEED = 1.2f;
    private const float DPAD_STEP_FRACTION = 0.05f;
    private const float DPAD_REPEAT_DELAY  = 0.35f;
    private const float DPAD_REPEAT_RATE   = 0.08f;
    private float _dpadRepeatTimer;
    private int   _dpadLastDir;

    // Tooltip fade state
    private CanvasGroup _tooltipCG;
    private const float TOOLTIP_FADE_SPEED = 6f;
    private float _tooltipGraceTimer;
    private const float TOOLTIP_GRACE_PERIOD = 0.4f;

    // Game tab row layout constants (must match builder)
    private const float GAME_ROW_HEIGHT  = 48f;
    private const float GAME_ROW_SPACING = 10f;
    private const float GAME_HEADER_HEIGHT = 34f;

    // ── Tab colors ───────────────────────────────────────────────────────
    private static readonly Color TAB_ACTIVE_COLOR   = new Color(0.961f, 0.784f, 0.259f, 1f);
    private static readonly Color TAB_INACTIVE_COLOR = new Color(0.91f, 0.918f, 0.965f, 0.4f);

    /// <summary>
    /// Called by OptionsMenuBuilder AFTER all references are assigned.
    /// </summary>
    public void InitializeAfterBuild()
    {
        if (_initialized) return;
        _initialized = true;

        _resolutions  = GameSettings.GetFilteredResolutions();
        _resIndex     = Mathf.Max(0, GameSettings.ResolutionIndex);
        _displayMode  = GameSettings.DisplayMode;
        _qualityIndex = GameSettings.QualityLevel;

        if (_resolutions.Length > 0 && _resIndex >= _resolutions.Length)
            _resIndex = _resolutions.Length - 1;

        // Wire tab buttons for mouse clicks
        if (tabButtons != null)
        {
            for (int i = 0; i < tabButtons.Length; i++)
            {
                int idx = i;
                tabButtons[i].onClick.AddListener(() => ShowTab(idx));
            }
        }

        // Wire AUDIO sliders
        WireSlider(masterVolSlider, v =>
        {
            GameSettings.MasterVolume = v;
            UpdateValueLabel(masterVolValue, v, true);
            GameSettings.ApplyAudio();
        });
        WireSlider(musicVolSlider, v =>
        {
            GameSettings.MusicVolume = v;
            UpdateValueLabel(musicVolValue, v, true);
            GameSettings.ApplyAudio();
        });
        WireSlider(sfxVolSlider, v =>
        {
            GameSettings.SFXVolume = v;
            UpdateValueLabel(sfxVolValue, v, true);
            GameSettings.ApplyAudio();
        });

        // Wire GAME sliders
        WireSlider(mouseSensSlider, v =>
        {
            GameSettings.MouseSensitivity = v;
            UpdateValueLabel(mouseSensValue, v, false);
        });
        WireSlider(stickSensSlider, v =>
        {
            GameSettings.StickSensitivity = v;
            UpdateValueLabel(stickSensValue, v, false);
        });
        WireSlider(leftDeadzoneSlider, v =>
        {
            GameSettings.LeftStickDeadzone = v;
            UpdateValueLabel(leftDeadzoneValue, v, false);
        });
        WireSlider(rightDeadzoneSlider, v =>
        {
            GameSettings.RightStickDeadzone = v;
            UpdateValueLabel(rightDeadzoneValue, v, false);
        });

        // Wire toggles
        if (invertYToggle != null)
            invertYToggle.onValueChanged.AddListener(v => GameSettings.InvertYAxis = v);
        if (vsyncToggle != null)
            vsyncToggle.onValueChanged.AddListener(v =>
            {
                GameSettings.VSync = v ? 1 : 0;
                QualitySettings.vSyncCount = v ? 1 : 0;
            });

        // Wire back button
        if (backButton != null)
            backButton.onClick.AddListener(GoBack);

        // Wire reset buttons
        if (resetButtons != null)
        {
            if (resetButtons.Length > 0 && resetButtons[0] != null)
                resetButtons[0].onClick.AddListener(ResetGameTab);
            if (resetButtons.Length > 1 && resetButtons[1] != null)
                resetButtons[1].onClick.AddListener(ResetAudioTab);
            if (resetButtons.Length > 2 && resetButtons[2] != null)
                resetButtons[2].onClick.AddListener(ResetVideoTab);
        }

        // Initial state
        if (controllerTooltip != null)
        {
            _tooltipCG = controllerTooltip.GetComponent<CanvasGroup>();
            if (_tooltipCG == null) _tooltipCG = controllerTooltip.AddComponent<CanvasGroup>();
            _tooltipCG.alpha = 0f;
            controllerTooltip.SetActive(true); // keep active, control via alpha
        }

        RefreshAll();
        UpdateInputModeVisibility();
        UpdateControllerHints();
        ShowTab(0);
    }

    void OnEnable()
    {
        InputPromptManager.OnInputModeChanged += OnInputModeChanged;

        if (_initialized)
        {
            RefreshAll();
            UpdateInputModeVisibility();
            UpdateControllerHints();
            ShowTab(0);
        }
    }

    void OnDisable()
    {
        if (_isHoldingSlider)
            UIGamepadNavigator.SuppressInput = false;

        InputPromptManager.OnInputModeChanged -= OnInputModeChanged;
        _isHoldingSlider = false;
        _dragSlider = null;
        GameSettings.Save();
    }

    void Update()
    {
        if (!_initialized) return;

        if (_tabCooldown > 0f)
            _tabCooldown -= Time.unscaledDeltaTime;
        if (_tooltipGraceTimer > 0f)
            _tooltipGraceTimer -= Time.unscaledDeltaTime;

        HandleTabInput();
        HandleBackInput();
        HandleSliderDPad();
        HandleControllerSliderDrag();
        UpdateTooltipPosition();
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private void WireSlider(Slider slider, Action<float> onChange)
    {
        if (slider == null) return;
        slider.onValueChanged.AddListener(v => onChange(v));
    }

    // ── Refresh UI from current settings ────────────────────────────────

    private void RefreshAll()
    {
        SetSliderSilent(masterVolSlider,      GameSettings.MasterVolume);
        SetSliderSilent(musicVolSlider,       GameSettings.MusicVolume);
        SetSliderSilent(sfxVolSlider,         GameSettings.SFXVolume);
        SetSliderSilent(mouseSensSlider,      GameSettings.MouseSensitivity);
        SetSliderSilent(stickSensSlider,      GameSettings.StickSensitivity);
        SetSliderSilent(leftDeadzoneSlider,   GameSettings.LeftStickDeadzone);
        SetSliderSilent(rightDeadzoneSlider,  GameSettings.RightStickDeadzone);

        if (invertYToggle != null) invertYToggle.isOn = GameSettings.InvertYAxis;
        if (vsyncToggle   != null) vsyncToggle.isOn = (GameSettings.VSync == 1);

        UpdateValueLabel(masterVolValue,      GameSettings.MasterVolume, true);
        UpdateValueLabel(musicVolValue,       GameSettings.MusicVolume, true);
        UpdateValueLabel(sfxVolValue,         GameSettings.SFXVolume, true);
        UpdateValueLabel(mouseSensValue,      GameSettings.MouseSensitivity, false);
        UpdateValueLabel(stickSensValue,      GameSettings.StickSensitivity, false);
        UpdateValueLabel(leftDeadzoneValue,   GameSettings.LeftStickDeadzone, false);
        UpdateValueLabel(rightDeadzoneValue,  GameSettings.RightStickDeadzone, false);

        _resolutions  = GameSettings.GetFilteredResolutions();
        _resIndex     = Mathf.Clamp(GameSettings.ResolutionIndex, 0, Mathf.Max(0, _resolutions.Length - 1));
        _displayMode  = GameSettings.DisplayMode;
        _qualityIndex = GameSettings.QualityLevel;

        UpdateResolutionLabel();
        UpdateDisplayModeLabel();
        UpdateQualityLabel();
    }

    private void SetSliderSilent(Slider s, float v)
    {
        if (s == null) return;
        s.SetValueWithoutNotify(v);
    }

    private void UpdateValueLabel(TextMeshProUGUI label, float value, bool asPercent)
    {
        if (label == null) return;
        label.text = asPercent
            ? Mathf.RoundToInt(value * 100f) + "%"
            : value.ToString("F2");
    }

    // ── Tab navigation ──────────────────────────────────────────────────

    /// <summary>Activates the tab at the given index.</summary>
    public void ShowTab(int index)
    {
        if (tabContents == null || tabButtons == null) return;
        _activeTab = Mathf.Clamp(index, 0, tabContents.Length - 1);

        for (int i = 0; i < tabContents.Length; i++)
        {
            bool active = i == _activeTab;
            if (tabContents[i] != null) tabContents[i].SetActive(active);

            if (tabButtons[i] != null)
            {
                TextMeshProUGUI lbl = tabButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                if (lbl != null) lbl.color = active ? TAB_ACTIVE_COLOR : TAB_INACTIVE_COLOR;

                Transform underline = tabButtons[i].transform.Find("Underline");
                if (underline != null) underline.gameObject.SetActive(active);
            }
        }

        _isHoldingSlider = false;
        _tooltipGraceTimer = TOOLTIP_GRACE_PERIOD;

        if (_activeTab == 0)
            RebuildGameTabNav();

        StartCoroutine(SelectFirstInTab());
    }

    private IEnumerator SelectFirstInTab()
    {
        yield return null;

        // In stick/cursor mode, don't auto-select — let the cursor hover naturally
        if (UIStickCursor.IsStickMode) yield break;

        if (tabContents == null || _activeTab >= tabContents.Length) yield break;

        GameObject content = tabContents[_activeTab];
        if (content == null) yield break;

        Selectable[] all = content.GetComponentsInChildren<Selectable>(false);
        foreach (Selectable s in all)
        {
            if (s.gameObject.activeInHierarchy && s.interactable)
            {
                if (EventSystem.current != null)
                    EventSystem.current.SetSelectedGameObject(s.gameObject);
                yield break;
            }
        }
    }

    private void HandleTabInput()
    {
        if (_tabCooldown > 0f) return;
        if (_isHoldingSlider) return;

        bool lb = Input.GetKeyDown(KeyCode.JoystickButton4);
        bool rb = Input.GetKeyDown(KeyCode.JoystickButton5);

        if (lb) { SwitchTab(-1); return; }
        if (rb) { SwitchTab(1);  return; }
    }

    private void SwitchTab(int delta)
    {
        if (tabContents == null || tabContents.Length == 0) return;
        int next = (_activeTab + delta + tabContents.Length) % tabContents.Length;
        ShowTab(next);
        _tabCooldown = TAB_INPUT_COOLDOWN;
    }

    // ── D-pad left/right slider stepping ────────────────────────────────

    private void HandleSliderDPad()
    {
        Slider selected = GetFocusedSlider();
        if (selected == null)
        {
            _dpadLastDir = 0;
            return;
        }

        int dir = 0;
        if (Input.GetKey(KeyCode.JoystickButton12) || Input.GetKey(KeyCode.RightArrow)) dir = 1;
        else if (Input.GetKey(KeyCode.JoystickButton11) || Input.GetKey(KeyCode.LeftArrow)) dir = -1;

        if (dir == 0)
        {
            float dh = 0f;
            try { dh = Input.GetAxisRaw("DPadHorizontal"); } catch { }
            if (dh > 0.4f) dir = 1;
            else if (dh < -0.4f) dir = -1;
        }

        if (dir == 0)
        {
            _dpadRepeatTimer = 0f;
            _dpadLastDir = 0;
            return;
        }

        bool shouldStep = false;
        if (dir != _dpadLastDir)
        {
            shouldStep = true;
            _dpadRepeatTimer = DPAD_REPEAT_DELAY;
            _dpadLastDir = dir;
        }
        else
        {
            _dpadRepeatTimer -= Time.unscaledDeltaTime;
            if (_dpadRepeatTimer <= 0f)
            {
                shouldStep = true;
                _dpadRepeatTimer = DPAD_REPEAT_RATE;
            }
        }

        if (shouldStep)
        {
            float range = selected.maxValue - selected.minValue;
            float step = range * DPAD_STEP_FRACTION;
            selected.value = Mathf.Clamp(selected.value + step * dir, selected.minValue, selected.maxValue);
        }
    }

    // ── Controller slider drag (hold A + LS) ────────────────────────────

    private void HandleControllerSliderDrag()
    {
        bool isController = !InputPromptManager.IsKeyboardMouse;

        // Get focused slider: cursor hover takes priority, then D-pad selection
        Slider focusedSlider = GetFocusedSlider();

        // Tooltip: fade in when a slider is hovered/selected, fade out otherwise
        bool wantTooltip = isController && focusedSlider != null && _tooltipGraceTimer <= 0f;
        UpdateTooltipFade(wantTooltip);

        if (!isController)
        {
            if (_isHoldingSlider)
            {
                UIGamepadNavigator.SuppressInput = false;
                _isHoldingSlider = false;
                _dragSlider = null;
            }
            return;
        }

        bool aHeld = Input.GetKey(KeyCode.JoystickButton0);

        if (aHeld)
        {
            // Lock onto the slider when A is first pressed
            if (!_isHoldingSlider && focusedSlider != null)
            {
                _isHoldingSlider = true;
                _dragSlider = focusedSlider;
                UIGamepadNavigator.SuppressInput = true;
            }

            // Adjust the locked slider with left stick horizontal
            if (_isHoldingSlider && _dragSlider != null)
            {
                float lsX = Input.GetAxisRaw("Horizontal");
                if (Mathf.Abs(lsX) > 0.15f)
                {
                    float range = _dragSlider.maxValue - _dragSlider.minValue;
                    float delta = lsX * SLIDER_STICK_SPEED * range * Time.unscaledDeltaTime;
                    _dragSlider.value = Mathf.Clamp(_dragSlider.value + delta, _dragSlider.minValue, _dragSlider.maxValue);
                }
            }
        }
        else
        {
            if (_isHoldingSlider)
            {
                UIGamepadNavigator.SuppressInput = false;
                _isHoldingSlider = false;
                _dragSlider = null;
            }
        }
    }

    /// <summary>Fades the controller tooltip in or out smoothly.</summary>
    private void UpdateTooltipFade(bool wantVisible)
    {
        if (_tooltipCG == null) return;

        float target = wantVisible ? 1f : 0f;
        _tooltipCG.alpha = Mathf.MoveTowards(_tooltipCG.alpha, target, TOOLTIP_FADE_SPEED * Time.unscaledDeltaTime);
        _tooltipCG.blocksRaycasts = false;
    }

    /// <summary>Positions tooltip near the currently focused slider.</summary>
    private void UpdateTooltipPosition()
    {
        if (_tooltipCG == null || _tooltipCG.alpha <= 0f) return;
        if (tooltipRT == null) return;

        Slider focused = _isHoldingSlider ? _dragSlider : GetFocusedSlider();
        if (focused == null) return;

        RectTransform sliderParent = focused.transform.parent as RectTransform;
        if (sliderParent == null) return;

        RectTransform tooltipParent = tooltipRT.parent as RectTransform;
        if (tooltipParent == null) return;

        Vector3 worldPos = sliderParent.position;
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            tooltipParent,
            RectTransformUtility.WorldToScreenPoint(null, worldPos),
            null,
            out localPos
        );

        tooltipRT.anchoredPosition = new Vector2(0f, localPos.y + 36f);
    }

    /// <summary>
    /// Returns the slider currently under focus — either via cursor hover
    /// (UIStickCursor) or via D-pad selection (EventSystem).
    /// Cursor hover takes priority.
    /// </summary>
    private Slider GetFocusedSlider()
    {
        // Check cursor hover first (stick/cursor mode)
        Selectable hovered = UIStickCursor.HoveredSelectable;
        if (hovered != null)
        {
            Slider s = hovered as Slider;
            if (s == null) s = hovered.GetComponent<Slider>();
            if (s != null) return s;
        }

        // Fallback to D-pad EventSystem selection
        if (EventSystem.current == null) return null;
        GameObject sel = EventSystem.current.currentSelectedGameObject;
        if (sel == null) return null;
        return sel.GetComponent<Slider>();
    }

    // ── Back navigation ─────────────────────────────────────────────────

    private void HandleBackInput()
    {
        if (_isHoldingSlider) return;

        bool back = Input.GetKeyDown(KeyCode.JoystickButton1) || Input.GetKeyDown(KeyCode.Escape);
        if (back) GoBack();
    }

    /// <summary>Returns to the originating menu.</summary>
    public void GoBack()
    {
        GameSettings.Save();

        switch (Origin)
        {
            case OptionsOrigin.PauseMenu:
                gameObject.SetActive(false);
                PauseMenuController pmc = FindObjectOfType<PauseMenuController>(true);
                if (pmc != null) pmc.ReturnFromOptions();
                break;

            case OptionsOrigin.MainMenu:
                if (MainMenuController.Instance != null)
                    MainMenuController.Instance.CloseOptions();
                break;
        }
    }

    // ── Reset to defaults ───────────────────────────────────────────────

    private void ResetGameTab()
    {
        GameSettings.MouseSensitivity   = GameSettings.DEFAULT_MOUSE_SENS;
        GameSettings.StickSensitivity   = GameSettings.DEFAULT_STICK_SENS;
        GameSettings.LeftStickDeadzone  = GameSettings.DEFAULT_LEFT_STICK_DEADZONE;
        GameSettings.RightStickDeadzone = GameSettings.DEFAULT_RIGHT_STICK_DEADZONE;
        GameSettings.InvertYAxis        = GameSettings.DEFAULT_INVERT_Y;
        RefreshAll();
    }

    private void ResetAudioTab()
    {
        GameSettings.MasterVolume = GameSettings.DEFAULT_MASTER_VOL;
        GameSettings.MusicVolume  = GameSettings.DEFAULT_MUSIC_VOL;
        GameSettings.SFXVolume    = GameSettings.DEFAULT_SFX_VOL;

        // Apply directly to SoundManager
        if (SoundManager.Instance != null)
            SoundManager.Instance.SetVolumes(GameSettings.DEFAULT_MASTER_VOL,
                GameSettings.DEFAULT_MUSIC_VOL, GameSettings.DEFAULT_SFX_VOL);

        RefreshAll();
    }

    private void ResetVideoTab()
    {
        // Set defaults in GameSettings
        GameSettings.DisplayMode  = GameSettings.DEFAULT_DISPLAY_MODE;
        GameSettings.VSync        = GameSettings.DEFAULT_VSYNC;
        GameSettings.ResolutionIndex = -1;

        // Reset quality to the default quality level configured in project settings
        int defaultQuality = Mathf.Clamp(3, 0, QualitySettings.names.Length - 1); // "High" typically at index 3
        GameSettings.QualityLevel = defaultQuality;

        // Apply to Unity systems immediately
        QualitySettings.vSyncCount = GameSettings.DEFAULT_VSYNC;
        QualitySettings.SetQualityLevel(defaultQuality, true);
        GameSettings.ApplyResolution();

        // Update local state
        _displayMode  = GameSettings.DEFAULT_DISPLAY_MODE;
        _qualityIndex = defaultQuality;

        // RefreshAll uses .isOn = which triggers visual callbacks for toggles
        RefreshAll();
    }

    // ── Cycle selectors ─────────────────────────────────────────────────

    /// <summary>Cycles through available resolutions and applies immediately.</summary>
    public void CycleResolution(int delta)
    {
        if (_resolutions == null || _resolutions.Length == 0) return;
        _resIndex = (_resIndex + delta + _resolutions.Length) % _resolutions.Length;
        GameSettings.ResolutionIndex = _resIndex;
        UpdateResolutionLabel();
        GameSettings.ApplyResolution();
        UIStickCursor.SuppressMouseSwitch();
    }

    /// <summary>Cycles through display modes and applies immediately.</summary>
    public void CycleDisplayMode(int delta)
    {
        int count = GameSettings.DisplayModeCount;
        _displayMode = (_displayMode + delta + count) % count;
        GameSettings.DisplayMode = _displayMode;
        UpdateDisplayModeLabel();
        GameSettings.ApplyResolution();
        UIStickCursor.SuppressMouseSwitch();
    }

    /// <summary>Cycles through quality levels and applies immediately.</summary>
    public void CycleQuality(int delta)
    {
        int count = QualitySettings.names.Length;
        if (count == 0) return;
        _qualityIndex = (_qualityIndex + delta + count) % count;
        GameSettings.QualityLevel = _qualityIndex;
        UpdateQualityLabel();
        QualitySettings.SetQualityLevel(_qualityIndex, true);

        // Quality presets contain their own vSyncCount — re-apply user's preference
        QualitySettings.vSyncCount = GameSettings.VSync;
    }

    private void UpdateResolutionLabel()
    {
        if (resolutionLabel == null) return;
        if (_resolutions != null && _resIndex >= 0 && _resIndex < _resolutions.Length)
        {
            Resolution r = _resolutions[_resIndex];
            resolutionLabel.text = $"{r.width} x {r.height}";
        }
        else
        {
            resolutionLabel.text = $"{Screen.width} x {Screen.height}";
        }
    }

    private void UpdateDisplayModeLabel()
    {
        if (displayModeLabel != null)
            displayModeLabel.text = GameSettings.GetDisplayModeLabel(_displayMode);
    }

    private void UpdateQualityLabel()
    {
        if (qualityLabel == null) return;
        string[] names = QualitySettings.names;
        if (_qualityIndex >= 0 && _qualityIndex < names.Length)
            qualityLabel.text = names[_qualityIndex].ToUpper();
    }

    // ── Input mode visibility ───────────────────────────────────────────

    private void OnInputModeChanged(InputPromptManager.InputMode mode)
    {
        UpdateInputModeVisibility();
        UpdateControllerHints();

        if (_activeTab == 0)
        {
            RebuildGameTabNav();
            StartCoroutine(SelectFirstInTab());
        }
    }

    /// <summary>Shows/hides rows in the GAME tab based on current input device.</summary>
    private void UpdateInputModeVisibility()
    {
        bool isKBM = InputPromptManager.IsKeyboardMouse;

        if (mouseSensRow != null)     mouseSensRow.SetActive(isKBM);
        if (stickSensRow != null)     stickSensRow.SetActive(!isKBM);
        if (deadzoneHeader != null)   deadzoneHeader.SetActive(!isKBM);
        if (leftDeadzoneRow != null)  leftDeadzoneRow.SetActive(!isKBM);
        if (rightDeadzoneRow != null) rightDeadzoneRow.SetActive(!isKBM);

        RepositionGameTabRows();
    }

    /// <summary>
    /// Repositions all visible GAME tab rows top-down so there are no gaps
    /// when input-mode-specific rows are hidden.
    /// </summary>
    private void RepositionGameTabRows()
    {
        // Ordered list of all game tab elements (headers, rows, reset button)
        GameObject[] allRows = {
            cameraHeader, mouseSensRow, stickSensRow,
            deadzoneHeader, leftDeadzoneRow, rightDeadzoneRow,
            invertYRow, gameResetRow
        };

        float y = -10f;
        foreach (GameObject row in allRows)
        {
            if (row == null) continue;
            if (!row.activeSelf) continue;

            RectTransform rt = row.GetComponent<RectTransform>();
            if (rt == null) continue;

            bool isHeader = row.name.StartsWith("Header_");
            bool isReset  = row.name.StartsWith("Reset");

            if (isReset) y -= 8f; // extra gap before reset button

            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);

            if (isHeader)
                y -= GAME_HEADER_HEIGHT;
            else if (isReset)
                y -= 44f;
            else
                y -= GAME_ROW_HEIGHT + GAME_ROW_SPACING;
        }
    }

    /// <summary>Rebuilds vertical navigation for visible GAME tab selectables.</summary>
    private void RebuildGameTabNav()
    {
        List<Selectable> visible = new List<Selectable>();

        if (InputPromptManager.IsKeyboardMouse)
        {
            if (mouseSensSlider != null) visible.Add(mouseSensSlider);
        }
        else
        {
            if (stickSensSlider != null)      visible.Add(stickSensSlider);
            if (leftDeadzoneSlider != null)   visible.Add(leftDeadzoneSlider);
            if (rightDeadzoneSlider != null)  visible.Add(rightDeadzoneSlider);
        }

        if (invertYToggle != null) visible.Add(invertYToggle);

        for (int i = 0; i < visible.Count; i++)
        {
            Navigation nav = visible[i].navigation;
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnUp   = i > 0 ? visible[i - 1] : visible[visible.Count - 1];
            nav.selectOnDown = i < visible.Count - 1 ? visible[i + 1] : visible[0];
            visible[i].navigation = nav;
        }
    }

    private void UpdateControllerHints()
    {
        bool showHints = !InputPromptManager.IsKeyboardMouse;
        if (lbHint != null) lbHint.SetActive(showHints);
        if (rbHint != null) rbHint.SetActive(showHints);
    }
}
