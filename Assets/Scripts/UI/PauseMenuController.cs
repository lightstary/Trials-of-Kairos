using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Pause menu with Resume, Restart Trial, How To Play, Controls, Trial Selection, Return to Hub.
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject      pausePanel;
    [SerializeField] private CanvasGroup     pauseCanvasGroup;
    [SerializeField] private RectTransform   panelRect;
    [SerializeField] private TextMeshProUGUI trialInfoLabel;

    [Header("Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button howToPlayButton;
    [SerializeField] private Button controlsButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button returnToHubButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;

    [Header("Sub-Panels")]
    [SerializeField] private GameObject controlsPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Background")]
    [SerializeField] private Image blurOverlay;

    [Header("Config")]
    [SerializeField] private float  animationDuration = 0.2f;
    [SerializeField] private string currentTrialInfo  = "TRIAL I";

    private const float SCALE_START       = 0.9f;
    private const float OVERLAY_MAX_ALPHA = 0.6f;
    private const float INPUT_COOLDOWN    = 0.25f;

    private bool  _isPaused;
    private bool  _subScreenOpen;
    private float _inputCooldown;

    /// <summary>True when game is paused.</summary>
    public bool IsPaused => _isPaused;

    /// <summary>True when a sub-screen is covering the pause panel.</summary>
    public bool HasSubScreenOpen => _subScreenOpen;

    /// <summary>True during input cooldown after returning from a sub-screen.</summary>
    public bool IsInInputCooldown => _inputCooldown > 0f;

    void Awake()
    {
        SetVisible(false);
    }

    void Start()
    {
        if (resumeButton      != null) resumeButton.onClick.AddListener(Resume);
        if (restartButton     != null) restartButton.onClick.AddListener(RestartTrial);
        if (howToPlayButton   != null) howToPlayButton.onClick.AddListener(ShowHowToPlay);
        if (controlsButton    != null) controlsButton.onClick.AddListener(ShowControls);
        if (settingsButton    != null)
        {
            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(OpenTrialSelection);
            TextMeshProUGUI lbl = settingsButton.GetComponentInChildren<TextMeshProUGUI>();
            if (lbl != null) lbl.text = "TRIAL SELECTION";
        }
        if (returnToHubButton != null)
        {
            returnToHubButton.onClick.AddListener(ReturnToMainMenu);
            // Update label text to match the action
            TextMeshProUGUI rthLabel = returnToHubButton.GetComponentInChildren<TextMeshProUGUI>();
            if (rthLabel != null) rthLabel.text = "RETURN TO MAIN MENU";
        }
        if (mainMenuButton    != null) mainMenuButton.onClick.AddListener(GoToMainMenu);
        if (quitButton        != null) quitButton.gameObject.SetActive(false);
        if (trialInfoLabel    != null) trialInfoLabel.text = currentTrialInfo;
    }

    void Update()
    {
        if (_inputCooldown > 0f) _inputCooldown -= Time.unscaledDeltaTime;

        // ── Start / Menu button toggles pause ────────────────────────────
        bool startPressed = Input.GetKeyDown(KeyCode.JoystickButton7)
                         || Input.GetKeyDown(KeyCode.JoystickButton9)
                         || Input.GetKeyDown(KeyCode.Escape);

#if ENABLE_INPUT_SYSTEM
        if (Gamepad.current != null)
            startPressed = startPressed || Gamepad.current.startButton.wasPressedThisFrame;
#endif

        if (startPressed)
        {
            if (_isPaused)
            {
                if (_subScreenOpen || _inputCooldown > 0f) return;
                Resume();
            }
            else
            {
                bool inMainMenu = MainMenuController.Instance != null
                    && MainMenuController.Instance.menuPanel != null
                    && MainMenuController.Instance.menuPanel.activeSelf;
                if (!inMainMenu)
                    Pause();
            }
        }

        // ── B button closes pause menu (direct handler, no EventSystem dependency) ─
        if (_isPaused && !_subScreenOpen && _inputCooldown <= 0f)
        {
            if (Input.GetKeyDown(KeyCode.JoystickButton1))
                Resume();
        }

        // ── Enforce selection while paused (D-pad mode only, not cursor mode) ──
        if (_isPaused && !_subScreenOpen
            && !UIStickCursor.IsStickMode
            && EventSystem.current != null
            && EventSystem.current.currentSelectedGameObject == null)
        {
            SelectFirstPauseButton();
        }
    }

    /// <summary>Pauses the game and shows the pause menu.</summary>
    public void Pause()
    {
        if (_isPaused) return;

        // Self-bootstrap: if pausePanel is null, build the UI now
        if (pausePanel == null)
            HubPauseMenuBuilder.Build(this);

        // Ensure the controller's own CanvasGroup (if any) doesn't block child rendering
        CanvasGroup selfCG = GetComponent<CanvasGroup>();
        if (selfCG != null)
        {
            selfCG.alpha = 1f;
            selfCG.interactable = true;
            selfCG.blocksRaycasts = true;
        }

        _isPaused = true;
        Time.timeScale = 0f;

        SetVisible(true);
        HideSubPanels();
        StartCoroutine(AnimateIn());
        StartCoroutine(DelayedSelectFirstButton());
    }

    /// <summary>Resumes the game and hides the pause menu.</summary>
    public void Resume()
    {
        if (!_isPaused) return;
        _isPaused = false;
        Time.timeScale = 1f;
        StartCoroutine(AnimateOut());
    }

    /// <summary>Called by ControlsScreenController when B is pressed.</summary>
    public void ReturnFromControls()
    {
        _subScreenOpen = false;
        _inputCooldown = INPUT_COOLDOWN;
        HideSubPanels();

        // Restore the pause panel that was hidden when Controls opened
        if (pausePanel != null) pausePanel.SetActive(true);
        if (pauseCanvasGroup != null) pauseCanvasGroup.alpha = 1f;

        // Re-select first button so controller works immediately
        StartCoroutine(DelayedSelectFirstButton());
    }

    /// <summary>Called by HowToPlayController when it closes.</summary>
    public void ReturnFromHowToPlay()
    {
        _subScreenOpen = false;
        _inputCooldown = INPUT_COOLDOWN;
    }

    /// <summary>Updates the trial info footer text.</summary>
    public void SetTrialInfo(string info) { currentTrialInfo = info; if (trialInfoLabel != null) trialInfoLabel.text = info; }

    /// <summary>Selects the first visible pause button for controller navigation.</summary>
    private void SelectFirstPauseButton()
    {
        Button[] buttons = new[] { resumeButton, restartButton, controlsButton, settingsButton, returnToHubButton };
        foreach (Button b in buttons)
        {
            if (b != null && b.gameObject.activeInHierarchy && b.interactable)
            {
                EventSystem.current.SetSelectedGameObject(b.gameObject);
                return;
            }
        }
    }

    private IEnumerator DelayedSelectFirstButton()
    {
        // Wait one frame so the hierarchy is fully active after SetVisible(true)
        yield return null;
        if (_isPaused && EventSystem.current != null)
            SelectFirstPauseButton();
    }

    private void RestartTrial()
    {
        Time.timeScale = 1f; _isPaused = false;
        MainMenuController.RequestRestartTrialOnLoad();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void ShowControls()
    {
        _subScreenOpen = true;
        HideSubPanels();

        // Hide the pause panel so Controls doesn't overlap on top of it
        if (pausePanel != null) pausePanel.SetActive(false);

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;
        Transform cs = canvas.transform.Find("ControlsScreen");

        // Create ControlsScreen on demand if it doesn't exist (e.g., HubScene)
        if (cs == null)
        {
            GameObject csGO = new GameObject("ControlsScreen");
            csGO.transform.SetParent(canvas.transform, false);
            RectTransform csRT = csGO.AddComponent<RectTransform>();
            csRT.anchorMin = Vector2.zero; csRT.anchorMax = Vector2.one;
            csRT.offsetMin = csRT.offsetMax = Vector2.zero;
            csGO.AddComponent<ControlsScreenController>();
            cs = csGO.transform;
        }

        ControlsScreenController ctrl = cs.GetComponent<ControlsScreenController>();
        if (ctrl != null) ctrl.Origin = ControlsScreenController.ControlsOrigin.PauseMenu;
        cs.gameObject.SetActive(true);
    }

    private void ShowHowToPlay()
    {
        _subScreenOpen = true;
        HowToPlayController htp = FindObjectOfType<HowToPlayController>(true);
        if (htp != null)
        {
            htp.Origin = HowToPlayController.HTPOrigin.PauseMenu;
            htp.Show();
        }
    }

    private void OpenTrialSelection()
    {
        _isPaused = false;
        SetVisible(false);
        Time.timeScale = 1f;
        MainMenuController.RequestTrialSelectOnLoad();
        // Always go to MainScene for trial selection (it has the menu + trial select UI)
        if (ScreenTransitionManager.Instance != null)
            ScreenTransitionManager.Instance.FadeToScene("MainScene");
        else
            SceneManager.LoadScene("MainScene");
    }

    private void HideSubPanels()
    {
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        // Also hide the ControlsScreen if it was opened from pause
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            Transform cs = canvas.transform.Find("ControlsScreen");
            if (cs != null && cs.gameObject.activeSelf)
                cs.gameObject.SetActive(false);
        }
    }

    /// <summary>Returns to MainScene with main menu visible.</summary>
    private void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        _isPaused = false;
        SetVisible(false);

        // Load MainScene — it auto-shows the main menu on load
        if (ScreenTransitionManager.Instance != null)
            ScreenTransitionManager.Instance.FadeToScene("MainScene");
        else
            SceneManager.LoadScene("MainScene");
    }

    /// <summary>Returns to main menu without reloading the scene.</summary>
    private void GoToMainMenu()
    {
        _isPaused = false;
        SetVisible(false);
        HideSubPanels();

        MainMenuController mmc = FindObjectOfType<MainMenuController>(true);
        if (mmc != null)
        {
            mmc.ReturnToMainMenu();
        }
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    private void SetVisible(bool visible)
    {
        if (pausePanel != null) pausePanel.SetActive(visible);
        if (pauseCanvasGroup != null)
        {
            pauseCanvasGroup.interactable = visible;
            pauseCanvasGroup.blocksRaycasts = visible;
            if (!visible) pauseCanvasGroup.alpha = 0f;
        }
        if (blurOverlay != null)
        {
            Color c = blurOverlay.color; c.a = visible ? OVERLAY_MAX_ALPHA : 0f;
            blurOverlay.color = c; blurOverlay.gameObject.SetActive(visible);
        }
    }

    private IEnumerator AnimateIn()
    {
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseOut(Mathf.Clamp01(elapsed / animationDuration));
            if (panelRect        != null) panelRect.localScale        = Vector3.one * Mathf.Lerp(SCALE_START, 1f, t);
            if (pauseCanvasGroup != null) pauseCanvasGroup.alpha       = t;
            if (blurOverlay      != null) { Color c = blurOverlay.color; c.a = Mathf.Lerp(0f, OVERLAY_MAX_ALPHA, t); blurOverlay.color = c; }
            yield return null;
        }
    }

    private IEnumerator AnimateOut()
    {
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseIn(Mathf.Clamp01(elapsed / animationDuration));
            if (panelRect        != null) panelRect.localScale        = Vector3.one * Mathf.Lerp(1f, SCALE_START, t);
            if (pauseCanvasGroup != null) pauseCanvasGroup.alpha       = 1f - t;
            if (blurOverlay      != null) { Color c = blurOverlay.color; c.a = Mathf.Lerp(OVERLAY_MAX_ALPHA, 0f, t); blurOverlay.color = c; }
            yield return null;
        }
        SetVisible(false);
    }

    private static float EaseOut(float t) => 1f - Mathf.Pow(1f - t, 3f);
    private static float EaseIn(float t)  => t * t * t;
}
