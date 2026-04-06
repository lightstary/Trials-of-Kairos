using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Pause menu — toggle with Escape or P. Runs on unscaled time while paused.
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
    [SerializeField] private string hubSceneName      = "LevelSelect";
    [SerializeField] private string currentTrialInfo  = "TRIAL I";

    private const float SCALE_START        = 0.9f;
    private const float OVERLAY_MAX_ALPHA  = 0.6f;

    private bool isPaused;

    void Start()
    {
        if (resumeButton      != null) resumeButton.onClick.AddListener(Resume);
        if (restartButton     != null) restartButton.onClick.AddListener(RestartTrial);
        if (controlsButton    != null) controlsButton.onClick.AddListener(ShowControls);
        if (settingsButton    != null) settingsButton.onClick.AddListener(ShowSettings);
        if (returnToHubButton != null) returnToHubButton.onClick.AddListener(ReturnToHub);
        if (quitButton        != null) quitButton.onClick.AddListener(QuitGame);

        if (trialInfoLabel != null) trialInfoLabel.text = currentTrialInfo;
        SetVisible(false);
    }

    void Update()
    {
        // Xbox Menu button (joystick button 7 on Windows) toggles pause
        if (Input.GetKeyDown(KeyCode.JoystickButton7))
        {
            if (isPaused) Resume(); else Pause();
        }
    }

    /// <summary>Pauses the game and shows the pause menu.</summary>
    public void Pause()
    {
        if (isPaused) return;
        isPaused = true;
        Time.timeScale = 0f;
        SetVisible(true);
        HideSubPanels();
        StartCoroutine(AnimateIn());
    }

    /// <summary>Resumes the game and hides the pause menu.</summary>
    public void Resume()
    {
        if (!isPaused) return;
        isPaused = false;
        Time.timeScale = 1f;
        StartCoroutine(AnimateOut());
    }

    /// <summary>Updates the trial info footer text.</summary>
    public void SetTrialInfo(string info)
    {
        currentTrialInfo = info;
        if (trialInfoLabel != null) trialInfoLabel.text = info;
    }

    private void RestartTrial()
    {
        Time.timeScale = 1f; isPaused = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void ShowControls()  { HideSubPanels(); if (controlsPanel != null) controlsPanel.SetActive(true); }
    private void ShowSettings()  { HideSubPanels(); if (settingsPanel  != null) settingsPanel.SetActive(true);  }
    private void HideSubPanels() { if (controlsPanel != null) controlsPanel.SetActive(false); if (settingsPanel != null) settingsPanel.SetActive(false); }

    private void ReturnToHub()
    {
        Time.timeScale = 1f; isPaused = false;
        if (ScreenTransitionManager.Instance != null)
            ScreenTransitionManager.Instance.FadeToScene(hubSceneName);
        else
            SceneManager.LoadScene(hubSceneName);
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
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
