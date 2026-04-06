using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// In-scene main menu. Freezes time while active, then fades to gameplay.
/// All coroutines use unscaled time so they run while timeScale = 0.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject trialSelectScreen;
    [SerializeField] private GameObject controlsScreen;

    [Header("Title")]
    [SerializeField] private TextMeshProUGUI titleLabel;
    [SerializeField] private TextMeshProUGUI subtitleLabel;
    [SerializeField] private TextMeshProUGUI chapterLabel;
    [SerializeField] private TextMeshProUGUI versionLabel;

    [Header("Decorative")]
    [SerializeField] private RectTransform  timeRingTransform;
    [SerializeField] private CanvasGroup    timeRingCanvasGroup;

    [Header("Buttons")]
    [SerializeField] private Button beginTrialButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button trialSelectButton;
    [SerializeField] private Button controlsButton;
    [SerializeField] private Button quitButton;

    [Header("Button Canvas Groups (stagger order)")]
    [SerializeField] private CanvasGroup[] buttonGroups;

    private const float RING_ROTATE_SPEED = 3f;
    private const float RING_SCALE_START  = 1.2f;
    private const float TITLE_SPACE_START = 60f;
    private const float TITLE_SPACE_END   = 20f;

    void Awake()
    {
        Time.timeScale = 0f;
        if (hudPanel          != null) hudPanel.SetActive(false);
        if (trialSelectScreen != null) trialSelectScreen.SetActive(false);
        if (controlsScreen    != null) controlsScreen.SetActive(false);
        if (menuPanel         != null) menuPanel.SetActive(true);
    }

    void Start()
    {
        if (beginTrialButton  != null) beginTrialButton.onClick.AddListener(BeginTrial);
        if (continueButton    != null) continueButton.onClick.AddListener(BeginTrial);
        if (trialSelectButton != null) trialSelectButton.onClick.AddListener(OpenTrialSelect);
        if (controlsButton    != null) controlsButton.onClick.AddListener(OpenControls);
        if (quitButton        != null) quitButton.onClick.AddListener(QuitGame);
        if (versionLabel      != null) versionLabel.text = $"v{Application.version}";

        // Title stays ice-white; subtitle is gold-tinted
        if (titleLabel    != null) titleLabel.color    = new Color(0.95f, 0.97f, 1.0f, 0f);
        if (subtitleLabel != null) subtitleLabel.color = new Color(0.92f, 0.82f, 0.55f, 0f);

        StartCoroutine(PlayEntrance());
    }

    void Update()
    {
        if (timeRingTransform != null)
            timeRingTransform.Rotate(0f, 0f, -RING_ROTATE_SPEED * Time.unscaledDeltaTime);

        // B button / Escape dismisses any open sub-screen
        if (Input.GetKeyDown(KeyCode.JoystickButton1) || Input.GetKeyDown(KeyCode.Escape))
        {
            if (controlsScreen    != null && controlsScreen.activeSelf)    { CloseControls();    return; }
            if (trialSelectScreen != null && trialSelectScreen.activeSelf) { CloseTrialSelect(); return; }
        }
    }

    // ── Entrance ────────────────────────────────────────────────────────────

    private IEnumerator PlayEntrance()
    {
        if (timeRingCanvasGroup != null) timeRingCanvasGroup.alpha = 0f;
        if (titleLabel    != null) { titleLabel.alpha = 0f; titleLabel.characterSpacing = TITLE_SPACE_START; }
        if (subtitleLabel != null)   subtitleLabel.alpha = 0f;
        if (chapterLabel  != null)   chapterLabel.alpha  = 0f;
        if (buttonGroups  != null)   foreach (var g in buttonGroups) if (g != null) g.alpha = 0f;

        yield return new WaitForSecondsRealtime(0.2f);

        if (timeRingTransform != null && timeRingCanvasGroup != null)
            StartCoroutine(AnimateRing());

        yield return new WaitForSecondsRealtime(0.3f);

        if (titleLabel    != null) yield return AnimateTitle();
        if (subtitleLabel != null) yield return FadeText(subtitleLabel, 0.4f);

        if (buttonGroups != null)
        {
            foreach (var g in buttonGroups)
            {
                if (g != null) StartCoroutine(FadeGroup(g, 0.3f));
                yield return new WaitForSecondsRealtime(0.08f);
            }
        }

        yield return new WaitForSecondsRealtime(0.2f);
        if (chapterLabel != null) yield return FadeText(chapterLabel, 0.4f);

        // Auto-select the first button for controller navigation
        SelectFirstButton();
    }

    /// <summary>Sets the first available button as selected for controller/keyboard navigation.</summary>
    private void SelectFirstButton()
    {
        Button first = beginTrialButton != null ? beginTrialButton
                     : trialSelectButton != null ? trialSelectButton
                     : controlsButton;
        if (first != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(first.gameObject);
        }
    }

    private IEnumerator AnimateRing()
    {
        timeRingTransform.localScale = Vector3.one * RING_SCALE_START;
        float elapsed = 0f, dur = 1.0f;
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / dur);
            timeRingCanvasGroup.alpha    = t;
            timeRingTransform.localScale = Vector3.one * Mathf.Lerp(RING_SCALE_START, 1f, EaseOut(t));
            yield return null;
        }
        timeRingCanvasGroup.alpha    = 1f;
        timeRingTransform.localScale = Vector3.one;
    }

    private IEnumerator AnimateTitle()
    {
        float elapsed = 0f, dur = 0.8f;
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / dur);
            titleLabel.alpha            = t;
            titleLabel.characterSpacing = Mathf.Lerp(TITLE_SPACE_START, TITLE_SPACE_END, EaseOut(t));
            yield return null;
        }
        titleLabel.alpha            = 1f;
        titleLabel.characterSpacing = TITLE_SPACE_END;
    }

    private IEnumerator FadeText(TextMeshProUGUI label, float dur)
    {
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            label.alpha = Mathf.Clamp01(elapsed / dur);
            yield return null;
        }
        label.alpha = 1f;
    }

    private IEnumerator FadeGroup(CanvasGroup group, float dur)
    {
        RectTransform rt  = group.GetComponent<RectTransform>();
        Vector2 end   = rt != null ? rt.anchoredPosition : Vector2.zero;
        Vector2 start = end + Vector2.down * 20f;
        if (rt != null) rt.anchoredPosition = start;

        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / dur);
            group.alpha = t;
            if (rt != null) rt.anchoredPosition = Vector2.Lerp(start, end, EaseOut(t));
            yield return null;
        }
        group.alpha = 1f;
        if (rt != null) rt.anchoredPosition = end;
    }

    // ── Navigation ──────────────────────────────────────────────────────────

    /// <summary>Hides menu and hands control to gameplay.</summary>
    public void BeginTrial()
    {
        // Always close any sub-screens first
        if (trialSelectScreen != null) trialSelectScreen.SetActive(false);
        if (controlsScreen    != null) controlsScreen.SetActive(false);

        if (ScreenTransitionManager.Instance != null)
        {
            ScreenTransitionManager.Instance.FadeTransition(() =>
            {
                if (menuPanel != null) menuPanel.SetActive(false);
                if (hudPanel  != null) hudPanel.SetActive(true);
                Time.timeScale = 1f;
            });
        }
        else
        {
            if (menuPanel != null) menuPanel.SetActive(false);
            if (hudPanel  != null) hudPanel.SetActive(true);
            Time.timeScale = 1f;
        }
    }

    /// <summary>Shows the trial selection sub-screen.</summary>
    public void OpenTrialSelect()
    {
        if (trialSelectScreen != null) trialSelectScreen.SetActive(true);
    }

    /// <summary>Hides the trial selection sub-screen.</summary>
    public void CloseTrialSelect()
    {
        if (trialSelectScreen != null) trialSelectScreen.SetActive(false);
        SelectFirstButton();
    }

    /// <summary>Shows the controls sub-screen.</summary>
    public void OpenControls()
    {
        if (controlsScreen != null) controlsScreen.SetActive(true);
    }

    /// <summary>Hides the controls sub-screen.</summary>
    public void CloseControls()
    {
        if (controlsScreen != null) controlsScreen.SetActive(false);
        SelectFirstButton();
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static float EaseOut(float t) => 1f - Mathf.Pow(1f - t, 3f);
}
