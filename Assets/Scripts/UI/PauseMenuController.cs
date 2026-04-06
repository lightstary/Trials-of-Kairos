using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Pause menu with Resume, Restart Trial, Controls, Trial Selection, Return to Hub.
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
    [SerializeField] private Button controlsButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button returnToHubButton;
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
        if (controlsButton    != null) controlsButton.onClick.AddListener(ShowControls);
        if (settingsButton    != null)
        {
            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(OpenTrialSelection);
            TextMeshProUGUI lbl = settingsButton.GetComponentInChildren<TextMeshProUGUI>();
            if (lbl != null) lbl.text = "TRIAL SELECTION";
        }
        if (returnToHubButton != null) returnToHubButton.onClick.AddListener(ReturnToHub);
        if (quitButton        != null) quitButton.gameObject.SetActive(false);
        if (trialInfoLabel    != null) trialInfoLabel.text = currentTrialInfo;
    }

    void Update()
    {
        if (_inputCooldown > 0f) _inputCooldown -= Time.unscaledDeltaTime;

        if (Input.GetKeyDown(KeyCode.JoystickButton7))
        {
            if (_isPaused)
            {
                if (_subScreenOpen || _inputCooldown > 0f) return;
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    /// <summary>Pauses the game and shows the pause menu.</summary>
    public void Pause()
    {
        if (_isPaused) return;
        _isPaused = true;
        Time.timeScale = 0f;
        SetVisible(true);
        HideSubPanels();
        StartCoroutine(AnimateIn());
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
    }

    /// <summary>Updates the trial info footer text.</summary>
    public void SetTrialInfo(string info) { currentTrialInfo = info; if (trialInfoLabel != null) trialInfoLabel.text = info; }

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
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;
        Transform cs = canvas.transform.Find("ControlsScreen");
        if (cs != null)
        {
            ControlsScreenController ctrl = cs.GetComponent<ControlsScreenController>();
            if (ctrl != null) ctrl.Origin = ControlsScreenController.ControlsOrigin.PauseMenu;
            cs.gameObject.SetActive(true);
        }
    }

    private void OpenTrialSelection()
    {
        Time.timeScale = 1f; _isPaused = false;
        MainMenuController.RequestTrialSelectOnLoad();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void HideSubPanels()
    {
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    private void ReturnToHub()
    {
        Time.timeScale = 1f; _isPaused = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void SetVisible(bool visible)
    {
        if (pausePanel != null) pausePanel.SetActive(visible);
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
