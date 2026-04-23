using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Manages the trial selection screen with controller-first navigation:
///   - LB/RB (bumpers) or Left/Right arrows to browse trial cards
///   - D-pad horizontal also navigates with repeat delay
///   - A / Space to confirm the currently browsed node
///   - Two-phase selection: browse -> A confirm -> Enter Trial activates
///   - LB/RB hint pills flanking the card row, gold-flash on press
///   - Sand/dust particle background via MenuParticlesController
///   - MainMenuAtmosphere for rotating squares + grain
///
/// Attach to the TrialSelectScreen GameObject. Integrates with the
/// existing hierarchy (TrialGrid, BackButton) and MainMenuController.
/// </summary>
public class TrialSelectController : MonoBehaviour
{
    [Header("References (auto-discovered if null)")]
    [SerializeField] private Transform trialGrid;
    [SerializeField] private Button    backButton;

    [Header("Scene Names (per card, index 0 = first card)")]
    [Tooltip("Scene names matching each trial card. Card 0 = HUB if created dynamically.")]
    [SerializeField] private string[] sceneNames;

    private const string HUB_SCENE_NAME = "HubScene";

    // ── State ────────────────────────────────────────────────────────────────
    private Button[]    _cards;
    private Image[]     _cardBgs;
    private Color[]     _cardOrigColors;
    private int         _selectedIndex;
    private bool        _confirmed;
    private bool        _confirmBlocked;
    private float       _navCooldown;
    private Image       _lbHintBg;
    private Image       _rbHintBg;
    private bool        _hintsBuilt;
    private bool[]      _isLocked;

    // Enter Trial button (created in code)
    private Button      _enterButton;
    private CanvasGroup _enterGroup;
    private GameObject  _enterGO;
    private string[]    _sceneMap;

    private bool        _needsHintBuild;
    private Vector2     _lastMousePos;

    private float[]     _cardTargetScales;
    private const float SCALE_LERP_SPEED    = 10f;
    private const float SELECTED_SCALE      = 1.08f;
    private const float CONFIRMED_SCALE     = 1.08f;
    private const float UNSELECTED_SCALE    = 1.0f;

    private const float DPAD_REPEAT_DELAY = 0.25f;

    // ── Colors ────────────────────────────────────────────────────────────────
    private static readonly Color SelectedGold = new Color(1f, 0.82f, 0.10f, 1f);
    private static readonly Color HintResting  = new Color(0.08f, 0.08f, 0.16f, 0.72f);
    private static readonly Color HintPressed  = new Color(0.95f, 0.78f, 0.10f, 0.95f);
    private static readonly Color EnterBtnBg   = new Color(0.059f, 0.102f, 0.188f, 0.85f);
    private static readonly Color EnterBtnGold = new Color(0.961f, 0.784f, 0.259f, 1f);
    private static readonly Color LabelWhite   = new Color(0.91f, 0.918f, 0.965f, 1f);

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void OnEnable()
    {
        // Take exclusive control of gamepad input while this screen is active.
        // UIGamepadNavigator must not navigate/submit/auto-select during trial select.
        UIGamepadNavigator.SuppressInput = true;

        EnsureHubCard();
        GatherCards();
        BuildSceneNameMap();
        ApplyLockVisuals();
        RefreshBestTimes();
        EnsureEnterButton();
        UpdateBackButtonLabel();

        _confirmed      = false;
        _confirmBlocked = false;
        SetEnterButtonState(false);

        // Set initial card selection WITHOUT coroutines.
        // StartCoroutine fails if gameObject.activeInHierarchy is false
        // (can happen during scene init before parent hierarchy is fully active).
        if (_cards != null && _cards.Length > 0)
        {
            _selectedIndex = 0;
            for (int i = 0; i < _cards.Length; i++)
            {
                if (_cardBgs[i] == null) continue;
                if (i == 0)
                {
                    Color gold = _cardOrigColors[i];
                    gold = Color.Lerp(gold, SelectedGold, 0.25f);
                    _cardBgs[i].color = gold;
                    _cardTargetScales[i] = SELECTED_SCALE;
                }
                else
                {
                    Color dim = _cardOrigColors[i];
                    dim.a = 0.5f;
                    _cardBgs[i].color = dim;
                    _cardTargetScales[i] = UNSELECTED_SCALE;
                }
            }
        }

        // Defer hint building to first Update frame to avoid inactive GO coroutine error
        _needsHintBuild = !_hintsBuilt;

        InputPromptManager.OnInputModeChanged += OnInputModeChanged;
    }

    void OnDisable()
    {
        // Release input back to UIGamepadNavigator.
        UIGamepadNavigator.SuppressInput = false;
        InputPromptManager.OnInputModeChanged -= OnInputModeChanged;
    }

    private void OnInputModeChanged(InputPromptManager.InputMode newMode)
    {
        UpdateBackButtonLabel();
    }

    void Update()
    {
        if (_cards == null || _cards.Length == 0) return;

        // Deferred hint build (avoids StartCoroutine on inactive GO during scene init)
        if (_needsHintBuild && gameObject.activeInHierarchy)
        {
            _needsHintBuild = false;
            StartCoroutine(BuildHintsDeferred());
        }

        // ── Mouse hover → sync _selectedIndex ────────────────────────────
        // Track mouse position over cards for KBM users.
        Vector2 mousePos = Input.mousePosition;
        bool mouseMoved = (mousePos - _lastMousePos).sqrMagnitude > 1f;
        _lastMousePos = mousePos;

        if (mouseMoved)
        {
            Camera uiCam = null;
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                uiCam = canvas.worldCamera;

            for (int i = 0; i < _cards.Length; i++)
            {
                if (_cards[i] == null) continue;
                RectTransform cardRT = _cards[i].GetComponent<RectTransform>();
                if (cardRT != null && RectTransformUtility.RectangleContainsScreenPoint(cardRT, mousePos, uiCam))
                {
                    if (i != _selectedIndex)
                    {
                        _confirmed = false;
                        SetEnterButtonState(false);
                        SelectCard(i);
                    }
                    break;
                }
            }
        }

        // ── Orb cursor hover → sync _selectedIndex ─────────────────────────
        // When the orb cursor (stick or mouse mode) is over a card, select it.
        if ((UIStickCursor.IsStickMode || UIStickCursor.IsMouseMode) && UIStickCursor.IsCursorVisible)
        {
            Vector2 cursorScreen = RectTransformUtility.WorldToScreenPoint(null, UIStickCursor.CursorWorldPosition);
            for (int i = 0; i < _cards.Length; i++)
            {
                if (_cards[i] == null) continue;
                RectTransform cardRT = _cards[i].GetComponent<RectTransform>();
                if (cardRT != null && RectTransformUtility.RectangleContainsScreenPoint(cardRT, cursorScreen, null))
                {
                    if (i != _selectedIndex)
                    {
                        _confirmed = false;
                        SetEnterButtonState(false);
                        SelectCard(i);
                    }
                    break;
                }
            }
        }

        // ── Navigation: LB/RB, arrow keys ─────────────────────────────────
        bool navLeft  = Input.GetKeyDown(KeyCode.LeftArrow)
                     || Input.GetKeyDown(KeyCode.A)
                     || Input.GetKeyDown(KeyCode.JoystickButton4);   // LB
        bool navRight = Input.GetKeyDown(KeyCode.RightArrow)
                     || Input.GetKeyDown(KeyCode.D)
                     || Input.GetKeyDown(KeyCode.JoystickButton5);   // RB

        if (navLeft)  { NavigateCard(-1); FlashHint(_lbHintBg); }
        if (navRight) { NavigateCard( 1); FlashHint(_rbHintBg); }

        // D-pad ONLY with repeat delay (NOT left stick — that drives the free cursor)
        float dpadH = 0f;
        try { dpadH = Input.GetAxisRaw("DPadHorizontal"); } catch { }

        if (_navCooldown <= 0f)
        {
            if (dpadH < -0.4f) { NavigateCard(-1); FlashHint(_lbHintBg); _navCooldown = DPAD_REPEAT_DELAY; }
            if (dpadH >  0.4f) { NavigateCard( 1); FlashHint(_rbHintBg); _navCooldown = DPAD_REPEAT_DELAY; }
        }
        else
        {
            _navCooldown -= Time.unscaledDeltaTime;
            if (Mathf.Abs(dpadH) < 0.2f) _navCooldown = 0f;
        }

        // ── A button / Space / Enter = confirm or enter ─────────────────────
        // Single code path for ALL input modes. Card onClick listeners also
        // feed through OnCardClicked() for mouse users.
        // AConsumedThisFrame is set to block UIStickCursor and UIGamepadNavigator
        // from also processing this press.
        if (Input.GetKeyDown(KeyCode.Space)
            || Input.GetKeyDown(KeyCode.Return)
            || Input.GetKeyDown(KeyCode.JoystickButton0))
        {
            UIGamepadNavigator.AConsumedThisFrame = true;

            if (!_confirmBlocked)
            {
                if (_confirmed)
                    EnterTrial();
                else
                    ConfirmSelection();
            }
        }

        // ── B button / Escape = go back ───────────────────────────────────
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton1))
        {
            MainMenuController mmc = FindObjectOfType<MainMenuController>();
            if (mmc != null) mmc.CloseTrialSelect();
        }

        // ── Smooth card scale lerp (replaces coroutine-based animation) ──
        if (_cards != null && _cardTargetScales != null)
        {
            float lerpT = 1f - Mathf.Exp(-SCALE_LERP_SPEED * Time.unscaledDeltaTime);
            for (int i = 0; i < _cards.Length; i++)
            {
                if (_cards[i] == null) continue;
                RectTransform rt = _cards[i].GetComponent<RectTransform>();
                if (rt == null) continue;
                float current = rt.localScale.x;
                float target = _cardTargetScales[i];
                float next = Mathf.Lerp(current, target, lerpT);
                rt.localScale = new Vector3(next, next, 1f);
            }
        }
    }

    // ── Card navigation ───────────────────────────────────────────────────────

    /// <summary>Gathers all Button children of trialGrid.</summary>
    private void GatherCards()
    {
        if (trialGrid == null)
            trialGrid = transform.Find("TrialGrid");
        if (trialGrid == null) return;

        int count = trialGrid.childCount;
        _cards          = new Button[count];
        _cardBgs        = new Image[count];
        _cardOrigColors = new Color[count];
        _cardTargetScales = new float[count];

        for (int i = 0; i < count; i++)
        {
            Transform child = trialGrid.GetChild(i);
            _cards[i]   = child.GetComponent<Button>();
            _cardBgs[i] = child.GetComponent<Image>();
            _cardOrigColors[i] = _cardBgs[i] != null ? _cardBgs[i].color : Color.clear;
            _cardTargetScales[i] = UNSELECTED_SCALE;

            // Remove ALL existing onClick listeners (including persistent ones
            // set in the Inspector, e.g. Card_Trial1 → BeginTrial on MainMenuController).
            // Then wire up our own click handler for mouse support.
            if (_cards[i] != null)
            {
                _cards[i].onClick.RemoveAllListeners();
                int cardIndex = i; // capture for closure
                _cards[i].onClick.AddListener(() => OnCardClicked(cardIndex));
            }
        }
    }

    /// <summary>
    /// Called when a card is clicked with the mouse (via Unity's built-in pointer events).
    /// Implements the same two-step confirm flow as the gamepad A button.
    /// </summary>
    private void OnCardClicked(int index)
    {
        if (_confirmBlocked) return;

        if (index != _selectedIndex)
        {
            // Clicking a different card selects it (resets confirmation)
            _confirmed = false;
            SetEnterButtonState(false);
            SelectCard(index);
        }
        else if (_confirmed)
        {
            EnterTrial();
        }
        else
        {
            ConfirmSelection();
        }
    }

    /// <summary>Selects a card visually — highlights it gold, dims others.</summary>
    private void SelectCard(int index)
    {
        if (_cards == null || _cards.Length == 0) return;
        _selectedIndex = Mathf.Clamp(index, 0, _cards.Length - 1);

        for (int i = 0; i < _cards.Length; i++)
        {
            if (_cardBgs[i] == null) continue;

            if (i == _selectedIndex)
            {
                Color gold = _cardOrigColors[i];
                gold = Color.Lerp(gold, SelectedGold, 0.25f);
                _cardBgs[i].color = gold;
                _cardTargetScales[i] = SELECTED_SCALE;
            }
            else
            {
                Color dim = _cardOrigColors[i];
                dim.a = 0.5f;
                _cardBgs[i].color = dim;
                _cardTargetScales[i] = UNSELECTED_SCALE;
            }
        }
    }

    /// <summary>Navigates to next/prev card, resetting confirmation.</summary>
    private void NavigateCard(int delta)
    {
        if (_cards == null || _cards.Length == 0) return;

        _confirmed = false;
        SetEnterButtonState(false);

        int next = (_selectedIndex + delta + _cards.Length) % _cards.Length;
        SelectCard(next);
    }

    /// <summary>
    /// Confirms the currently browsed card. Activates Enter Trial button after a short delay.
    /// Blocks for several frames so the same A press doesn't also fire Enter Trial.
    /// </summary>
    private void ConfirmSelection()
    {
        if (_confirmed || _confirmBlocked) return;
        if (!gameObject.activeInHierarchy) return;

        _confirmed = true;
        StartCoroutine(BlockConfirmFrames());
        StartCoroutine(DelayedShowEnterButton());

        // Visual pulse on selected card
        if (_selectedIndex >= 0 && _selectedIndex < _cards.Length && _cardBgs[_selectedIndex] != null)
        {
            RectTransform rt = _cardBgs[_selectedIndex].GetComponent<RectTransform>();
            if (rt != null) StartCoroutine(ConfirmPulse(rt));
        }
    }

    /// <summary>Shows the Enter button after a short delay to prevent same-frame activation.</summary>
    private IEnumerator DelayedShowEnterButton()
    {
        yield return null;
        yield return null;
        yield return null;
        SetEnterButtonState(true);
        UpdateEnterButtonLabel();
    }

    /// <summary>Updates the Enter button label text based on the selected card.</summary>
    private void UpdateEnterButtonLabel()
    {
        if (_enterGO == null) return;
        TextMeshProUGUI lbl = _enterGO.GetComponentInChildren<TextMeshProUGUI>();
        if (lbl == null) return;

        string sceneName = GetSelectedSceneName();
        lbl.text = (sceneName == HUB_SCENE_NAME) ? "ENTER HUB" : "ENTER TRIAL";
    }

    private IEnumerator BlockConfirmFrames()
    {
        _confirmBlocked = true;
        yield return null;
        yield return null;
        _confirmBlocked = false;
    }

    private IEnumerator ConfirmPulse(RectTransform rt)
    {
        // Quick pop-out then settle back
        int idx = _selectedIndex;
        _cardTargetScales[idx] = 1.2f;
        yield return new WaitForSecondsRealtime(0.1f);
        _cardTargetScales[idx] = CONFIRMED_SCALE;
    }

    // ── Enter Trial button ────────────────────────────────────────────────────

    /// <summary>Creates an Enter Trial button below the trial grid.</summary>
    private void EnsureEnterButton()
    {
        if (_enterGO != null) return;

        _enterGO = new GameObject("EnterTrialButton");
        _enterGO.transform.SetParent(transform, false);

        RectTransform rt = _enterGO.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0f);
        rt.anchorMax        = new Vector2(0.5f, 0f);
        rt.pivot            = new Vector2(0.5f, 0f);
        rt.sizeDelta        = new Vector2(220f, 50f);
        rt.anchoredPosition = new Vector2(0f, 120f);

        Image bg       = _enterGO.AddComponent<Image>();
        bg.color       = Color.white;
        bg.raycastTarget = true;

        _enterButton = _enterGO.AddComponent<Button>();
        _enterButton.targetGraphic = bg;
        ColorBlock cb       = _enterButton.colors;
        cb.normalColor      = EnterBtnBg;
        cb.highlightedColor = EnterBtnGold;
        cb.pressedColor     = new Color(0.7f, 0.55f, 0.1f, 1f);
        cb.selectedColor    = EnterBtnBg;
        cb.fadeDuration     = 0.1f;
        _enterButton.colors = cb;
        _enterButton.onClick.AddListener(EnterTrial);

        _enterGroup = _enterGO.AddComponent<CanvasGroup>();

        // Label
        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(_enterGO.transform, false);
        TextMeshProUGUI label = labelGO.AddComponent<TextMeshProUGUI>();
        label.text             = "ENTER TRIAL";
        label.fontSize         = 16f;
        label.characterSpacing = 10f;
        label.alignment        = TextAlignmentOptions.Center;
        label.color            = LabelWhite;
        label.raycastTarget    = false;
        CinzelFontHelper.Apply(label, true);

        RectTransform lrt = labelGO.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;

        SetEnterButtonState(false);
    }

    /// <summary>Dims or activates the Enter Trial button.</summary>
    private void SetEnterButtonState(bool active)
    {
        if (_enterGroup == null) return;
        _enterGroup.alpha          = active ? 1f : 0.30f;
        _enterGroup.interactable   = active;
        _enterGroup.blocksRaycasts = active;
    }

    /// <summary>Loads the scene for the selected trial.</summary>
    private void EnterTrial()
    {
        string targetScene = GetSelectedSceneName();
        if (string.IsNullOrEmpty(targetScene)) return;

        Time.timeScale = 1f;

        // Skip the main menu for any scene that has a MainMenuController.
        // Without this, GardenScene/ClockScene show the menu instead of gameplay.
        if (targetScene != HUB_SCENE_NAME)
            MainMenuController.SkipMenuOnLoad = true;

        Debug.Log($"[TrialSelect] Loading scene: {targetScene} (index {_selectedIndex})");

        if (ScreenTransitionManager.Instance != null)
            ScreenTransitionManager.Instance.FadeToScene(targetScene);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(targetScene);
    }

    /// <summary>Returns the scene name for the currently selected card.</summary>
    private string GetSelectedSceneName()
    {
        if (_sceneMap != null && _selectedIndex >= 0 && _selectedIndex < _sceneMap.Length)
            return _sceneMap[_selectedIndex];
        return null;
    }

    /// <summary>Creates the HUB card as the first child of TrialGrid if it doesn't exist.</summary>
    private void EnsureHubCard()
    {
        if (trialGrid == null)
            trialGrid = transform.Find("TrialGrid");
        if (trialGrid == null) return;

        // Check if HUB card already exists
        Transform existing = trialGrid.Find("Card_Hub");
        if (existing != null) return;

        // Clone style from the first existing card
        if (trialGrid.childCount == 0) return;
        Transform template = trialGrid.GetChild(0);

        GameObject hubCard = Object.Instantiate(template.gameObject, trialGrid);
        hubCard.name = "Card_Hub";
        hubCard.transform.SetAsFirstSibling();

        // Update labels
        TextMeshProUGUI[] labels = hubCard.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI lbl in labels)
        {
            string objName = lbl.gameObject.name.ToLowerInvariant();
            if (objName.Contains("num"))        lbl.text = "*";        // hub marker
             else if (objName.Contains("title")) lbl.text = "THE\nHUB";
             else if (objName.Contains("sub"))   lbl.text = "Tutorial";
        }

        // Give it a slightly different tint
        Image cardBg = hubCard.GetComponent<Image>();
        if (cardBg != null)
        {
            Color c = cardBg.color;
            c = Color.Lerp(c, new Color(0.961f, 0.784f, 0.259f, 1f), 0.15f);
            cardBg.color = c;
        }
    }

    /// <summary>Builds the scene name array and lock state for each card.</summary>
    private void BuildSceneNameMap()
    {
        if (_cards == null) return;

        _sceneMap  = new string[_cards.Length];
        _isLocked  = new bool[_cards.Length];

        int nonHubIndex = 0;
        for (int i = 0; i < _cards.Length; i++)
        {
            if (_cards[i] != null && _cards[i].gameObject.name == "Card_Hub")
            {
                _sceneMap[i]  = HUB_SCENE_NAME;
                _isLocked[i]  = false;
            }
            else
            {
                switch (nonHubIndex)
                {
                    case 0: // Chapter I — Citadel
                        _sceneMap[i]  = "MainScene";
                        _isLocked[i]  = false;
                        break;
                    case 1: // Chapter II — Garden
                        _sceneMap[i]  = "GardenScene";
                        _isLocked[i]  = false;
                        break;
                    case 2: // Chapter III — Clock
                        _sceneMap[i]  = "ClockScene";
                        _isLocked[i]  = false;
                        break;
                    default:
                        _sceneMap[i]  = "";
                        _isLocked[i]  = false;
                        break;
                }
                nonHubIndex++;
            }
        }
    }

    /// <summary>
    /// Ensures all cards are unlocked and visually consistent.
    /// Resets any stale locked visuals baked into scene data.
    /// </summary>
    private void ApplyLockVisuals()
    {
        if (_cards == null) return;

        // Reference color from Card_Trial1 (Citadel) — the known-good card
        Color referenceBg = new Color(0.059f, 0.09f, 0.165f, 1f);
        Color referenceTitle = new Color(0.91f, 0.918f, 0.965f, 1f);
        Color referenceNum = new Color(0.91f, 0.918f, 0.965f, 0.7f);

        for (int i = 0; i < _cards.Length; i++)
        {
            if (_cards[i] == null) continue;

            // Ensure interactable
            _cards[i].interactable = true;

            // Skip hub card — it has its own styling
            if (_cards[i].gameObject.name == "Card_Hub") continue;

            // Reset background color to match Citadel
            if (_cardBgs[i] != null)
            {
                _cardBgs[i].color = referenceBg;
                _cardOrigColors[i] = referenceBg;
            }

            // Reset title and chapter number colors
            TextMeshProUGUI[] labels = _cards[i].GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (TextMeshProUGUI lbl in labels)
            {
                string n = lbl.gameObject.name.ToLowerInvariant();
                if (n.Contains("title"))
                    lbl.color = referenceTitle;
                else if (n.Contains("num"))
                    lbl.color = referenceNum;
            }
        }
    }

    /// <summary>Checks if a level has been completed this session.</summary>
    public static bool IsLevelComplete(string levelKey)
    {
        return BestTimeTracker.Has(levelKey);
    }

    /// <summary>Updates the back button — shows correct icon for current input mode.</summary>
    private void UpdateBackButtonLabel()
    {
        if (backButton == null)
        {
            Transform bb = transform.Find("BackButton");
            if (bb != null) backButton = bb.GetComponent<Button>();
        }
        if (backButton == null) return;

        // Keep button interactable for mouse clicks
        backButton.interactable = true;
        backButton.onClick.RemoveAllListeners();
        backButton.onClick.AddListener(() =>
        {
            MainMenuController mmc = FindObjectOfType<MainMenuController>();
            if (mmc != null) mmc.CloseTrialSelect();
        });

        // Minimal background
        Image bg = backButton.GetComponent<Image>();
        if (bg != null) bg.color = new Color(0.08f, 0.08f, 0.16f, 0.5f);

        // Add HorizontalLayoutGroup for icon + label side by side
        HorizontalLayoutGroup hlg = backButton.GetComponent<HorizontalLayoutGroup>();
        if (hlg == null)
        {
            hlg = backButton.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.padding = new RectOffset(10, 10, 0, 0);
        }

        // Add or update icon (must be first sibling for left positioning)
        Sprite backSprite = InputPromptManager.IsKeyboardMouse ? ControllerIcons.KeyEsc : ControllerIcons.CtrlB;
        Transform existingIcon = backButton.transform.Find("BackIcon");
        if (existingIcon != null)
        {
            Image existingImg = existingIcon.GetComponent<Image>();
            if (existingImg != null && backSprite != null)
                existingImg.sprite = backSprite;
            existingIcon.SetAsFirstSibling();
        }
        else if (backSprite != null)
        {
            Image iconImg = ControllerIcons.CreateIcon(backButton.transform, backSprite, 28f);
            if (iconImg != null)
            {
                iconImg.gameObject.name = "BackIcon";
                iconImg.transform.SetAsFirstSibling();
                LayoutElement iconLE = iconImg.gameObject.AddComponent<LayoutElement>();
                iconLE.preferredWidth = 28f;
                iconLE.preferredHeight = 28f;
            }
        }

        // Update label (must come after icon in sibling order)
        TextMeshProUGUI lbl = backButton.GetComponentInChildren<TextMeshProUGUI>();
        if (lbl != null)
        {
            lbl.text = "BACK";
            lbl.fontSize = 16f;
            lbl.characterSpacing = 4f;
            lbl.color = new Color(0.91f, 0.918f, 0.965f, 0.6f);
            CinzelFontHelper.Apply(lbl, true);

            // Remove the full-stretch anchors so the HLG controls layout
            RectTransform lblRT = lbl.GetComponent<RectTransform>();
            if (lblRT != null)
            {
                lblRT.anchorMin = new Vector2(0.5f, 0.5f);
                lblRT.anchorMax = new Vector2(0.5f, 0.5f);
                lblRT.pivot = new Vector2(0.5f, 0.5f);
                lblRT.sizeDelta = new Vector2(60f, 30f);
            }

            LayoutElement lblLE = lbl.GetComponent<LayoutElement>();
            if (lblLE == null) lblLE = lbl.gameObject.AddComponent<LayoutElement>();
            lblLE.preferredWidth = 60f;
            lblLE.preferredHeight = 30f;

            lbl.transform.SetAsLastSibling();
        }
    }

    // ── LB / RB hint pills ───────────────────────────────────────────────────

    private IEnumerator BuildHintsDeferred()
    {
        yield return null; // wait for layout

        if (_cards == null || _cards.Length == 0) yield break;

        DestroyChild("LBHint");
        DestroyChild("RBHint");

        RectTransform firstRT = _cardBgs[0] != null ? _cardBgs[0].GetComponent<RectTransform>() : null;
        RectTransform lastRT  = _cardBgs[_cards.Length - 1] != null
            ? _cardBgs[_cards.Length - 1].GetComponent<RectTransform>() : null;
        RectTransform selfRT  = transform as RectTransform;
        if (firstRT == null || lastRT == null || selfRT == null) yield break;

        Vector3[] firstCorners = new Vector3[4];
        Vector3[] lastCorners  = new Vector3[4];
        firstRT.GetWorldCorners(firstCorners);
        lastRT.GetWorldCorners(lastCorners);

        Vector2 firstLeft  = selfRT.InverseTransformPoint(firstCorners[0]);
        Vector2 lastRight  = selfRT.InverseTransformPoint(lastCorners[3]);
        float nodeCenterY  = selfRT.InverseTransformPoint(
            (firstCorners[0] + firstCorners[1]) * 0.5f).y;

        const float HINT_W = 58f;
        const float HINT_H = 30f;
        const float GAP    = 20f;

        Vector2 lbPos = new Vector2(firstLeft.x - GAP - HINT_W * 0.5f, nodeCenterY);
        Vector2 rbPos = new Vector2(lastRight.x + GAP + HINT_W * 0.5f, nodeCenterY);

        _lbHintBg = SpawnHint("LBHint", "LB", lbPos, HINT_W, HINT_H, isLeft: true);
        _rbHintBg = SpawnHint("RBHint", "RB", rbPos, HINT_W, HINT_H, isLeft: false);

        _hintsBuilt = true;
    }

    private Image SpawnHint(string goName, string label, Vector2 pos, float w, float h, bool isLeft)
    {
        GameObject root = new GameObject(goName);
        root.transform.SetParent(transform, false);

        RectTransform rt = root.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(w, h);
        rt.anchoredPosition = pos;

        Image bg         = root.AddComponent<Image>();
        bg.color         = HintResting;
        bg.raycastTarget = false;

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(root.transform, false);
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.richText      = true;
        tmp.text          = isLeft
            ? "<color=#F5C842>\u2039</color> <color=#E8EAF0>" + label + "</color>"
            : "<color=#E8EAF0>" + label + "</color> <color=#F5C842>\u203A</color>";
        tmp.fontSize      = 14f;
        tmp.fontStyle     = FontStyles.Bold;
        tmp.alignment     = TextAlignmentOptions.Center;
        tmp.color         = Color.white;
        tmp.raycastTarget = false;
        CinzelFontHelper.Apply(tmp, true);

        RectTransform trt = textGO.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(3f, 0f); trt.offsetMax = new Vector2(-3f, 0f);

        return bg;
    }

    /// <summary>Briefly flashes a hint pill gold on LB/RB press.</summary>
    private void FlashHint(Image hint)
    {
        if (hint == null || !gameObject.activeInHierarchy) return;
        StartCoroutine(HintFlashRoutine(hint));
    }

    private IEnumerator HintFlashRoutine(Image img)
    {
        img.color = HintPressed;
        yield return new WaitForSecondsRealtime(0.12f);
        float elapsed = 0f;
        while (elapsed < 0.20f)
        {
            elapsed += Time.unscaledDeltaTime;
            img.color = Color.Lerp(HintPressed, HintResting, elapsed / 0.20f);
            yield return null;
        }
        img.color = HintResting;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void DestroyChild(string name)
    {
        Transform t = transform.Find(name);
        if (t != null) Destroy(t.gameObject);
    }

    private static float EaseOut(float t) => 1f - Mathf.Pow(1f - t, 3f);

    // ── Best time display ────────────────────────────────────────────────────

    private static readonly string[] BEST_TIME_LEVEL_KEYS = {
        BestTimeTracker.KEY_CITADEL,
        BestTimeTracker.KEY_GARDEN,
        BestTimeTracker.KEY_CLOCK
    };

    /// <summary>Updates the TrialSub label on each card with session best times and completion state.</summary>
    private void RefreshBestTimes()
    {
        if (_cards == null) return;

        int trialIndex = 0;
        for (int i = 0; i < _cards.Length; i++)
        {
            if (_cards[i] == null) continue;
            if (_cards[i].gameObject.name == "Card_Hub") continue;

            Transform subT = _cards[i].transform.Find("TrialSub");
            TextMeshProUGUI sub = subT != null ? subT.GetComponent<TextMeshProUGUI>() : null;

            string key = trialIndex < BEST_TIME_LEVEL_KEYS.Length ? BEST_TIME_LEVEL_KEYS[trialIndex] : null;
            bool completed = key != null && BestTimeTracker.Has(key);

            // Update sub-label text
            if (sub != null)
            {
                if (completed)
                {
                    float best = BestTimeTracker.Get(key);
                    sub.text = $"\u2713  BEST  —  {BestTimeTracker.Format(best)}";
                    sub.color = new Color(0.2f, 1f, 0.45f, 0.85f); // green for completed
                }
                else
                {
                    sub.text = "BEST  —  --:--.--";
                    sub.color = new Color(0.91f, 0.918f, 0.965f, 0.45f);
                }
                CinzelFontHelper.Apply(sub);
            }

            // Visual state for completed levels — gold border tint on card background
            if (completed && _cardBgs[i] != null)
            {
                Color c = _cardOrigColors[i];
                c = Color.Lerp(c, new Color(0.2f, 0.9f, 0.35f, 1f), 0.12f); // subtle green tint
                _cardOrigColors[i] = c;
                if (i != _selectedIndex) _cardBgs[i].color = c;
            }

            trialIndex++;
        }
    }

    /// <summary>Formats a time value in seconds as M:SS.mm.</summary>
    private static string FormatTime(float seconds)
    {
        return BestTimeTracker.Format(seconds);
    }
}
