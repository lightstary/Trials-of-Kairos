using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Win screen — animated stat counters, gold radiance, and navigation.
/// </summary>
public class WinScreenController : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject      winPanel;
    [SerializeField] private CanvasGroup     canvasGroup;

    [Header("Title")]
    [SerializeField] private TextMeshProUGUI titleLabel;
    [SerializeField] private TextMeshProUGUI subtitleLabel;

    [Header("Stats")]
    [SerializeField] private TextMeshProUGUI bestTimeLabel;
    [SerializeField] private TextMeshProUGUI stancesUsedLabel;

    [Header("Completion Diamonds")]
    [SerializeField] private Image[] completionDiamonds;

    [Header("Buttons")]
    [SerializeField] private Button nextTrialButton;
    [SerializeField] private Button returnToHubButton;

    [Header("Visual")]
    [SerializeField] private Image radianceOverlay;

    [Header("Config")]
    [SerializeField] private string hubSceneName      = "HubScene";
    [SerializeField] private string nextTrialSceneName = "";

    private const string TITLE_TEXT     = "TIME  RESTORED";
    private const float  STAT_COUNT_DUR = 0.8f;
    private const float  STAT_DELAY     = 0.3f;
    private const float  RADIANCE_DUR   = 1.2f;

    private bool _shown = false;
    private bool _listenersWired = false;

    void Start()
    {
        EnsureListenersWired();

        // Only hide the panel if ShowSimple/Show hasn't already been called
        if (!_shown && winPanel != null) winPanel.SetActive(false);
    }

    private const string BEST_TIME_KEY = "BestTime_Citadel";

    /// <summary>Shows the win screen with stats.</summary>
    public void Show(string trialName, float completionTime, int stars,
                     bool isPersonalBest, bool usedForward, bool usedFrozen, bool usedReverse)
    {
        _shown = true;
        EnsureListenersWired();
        if (winPanel != null) winPanel.SetActive(true);
        if (titleLabel    != null) titleLabel.text    = TITLE_TEXT;
        if (subtitleLabel != null) subtitleLabel.text = $"{trialName} COMPLETE".ToUpper();

        SaveBestTime(completionTime);
        ApplyCinzelFonts();

        StartCoroutine(Animate(completionTime, stars, isPersonalBest, usedForward, usedFrozen, usedReverse));
    }

    /// <summary>Shows a simple win screen without detailed stats.</summary>
    public void ShowSimple()
    {
        _shown = true;
        EnsureListenersWired();
        if (winPanel != null) winPanel.SetActive(true);
        if (titleLabel    != null) titleLabel.text    = TITLE_TEXT;
        if (subtitleLabel != null) subtitleLabel.text = "TRIAL COMPLETE";

        ApplyCinzelFonts();

        StartCoroutine(SimpleShowRoutine());
    }

    /// <summary>Saves the best completion time to PlayerPrefs (lowest wins).</summary>
    private void SaveBestTime(float completionTime)
    {
        float existing = PlayerPrefs.GetFloat(BEST_TIME_KEY, float.MaxValue);
        if (completionTime < existing)
        {
            PlayerPrefs.SetFloat(BEST_TIME_KEY, completionTime);
            PlayerPrefs.Save();
        }
    }

    /// <summary>Applies Cinzel font to all TMP labels on the win screen.</summary>
    private void ApplyCinzelFonts()
    {
        CinzelFontHelper.ApplyToAll(transform, bold: true);
    }

    private IEnumerator SimpleShowRoutine()
    {
        yield return FadeIn(0.5f);
        SelectDefaultButton();
    }

    /// <summary>Wire onClick listeners if Start() hasn't run yet.</summary>
    private void EnsureListenersWired()
    {
        if (_listenersWired) return;
        _listenersWired = true;
        if (nextTrialButton   != null) nextTrialButton.onClick.AddListener(GoToNextTrial);
        if (returnToHubButton != null) returnToHubButton.onClick.AddListener(ReturnToTrialSelection);
    }

    /// <summary>Selects a button so the Xbox controller can navigate the win screen.</summary>
    private void SelectDefaultButton()
    {
        Button target = returnToHubButton != null ? returnToHubButton : nextTrialButton;
        if (target != null && UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(target.gameObject);
    }

    private IEnumerator Animate(float time, int stars, bool pb,
                                bool forward, bool frozen, bool reverse)
    {
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        ClearStats();

        if (radianceOverlay != null) StartCoroutine(AnimateRadiance());
        yield return FadeIn(0.5f);
        yield return new WaitForSecondsRealtime(STAT_DELAY);

        // Diamonds
        Color gold = TimeStateUIManager.Instance != null
            ? TimeStateUIManager.Instance.goldColor : Color.yellow;
        if (completionDiamonds != null)
        {
            for (int i = 0; i < completionDiamonds.Length; i++)
            {
                if (completionDiamonds[i] != null)
                    completionDiamonds[i].color = i < stars ? gold : new Color(0.3f, 0.3f, 0.3f, 0.5f);
                yield return new WaitForSecondsRealtime(0.15f);
            }
        }

        yield return new WaitForSecondsRealtime(STAT_DELAY);
        if (bestTimeLabel != null) yield return CountUpTime(time, pb);

        yield return new WaitForSecondsRealtime(STAT_DELAY);
        if (stancesUsedLabel != null)
        {
            string s = "";
            if (forward) s += "FORWARD";
            if (frozen)  s += (s.Length > 0 ? " \u00B7 " : "") + "FROZEN";
            if (reverse) s += (s.Length > 0 ? " \u00B7 " : "") + "REVERSED";
            stancesUsedLabel.text = s;
        }

        SelectDefaultButton();
    }

    private IEnumerator FadeIn(float dur)
    {
        if (canvasGroup == null) yield break;
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / dur);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    private IEnumerator AnimateRadiance()
    {
        Color gold = TimeStateUIManager.Instance != null
            ? TimeStateUIManager.Instance.goldColor : new Color(1f, 0.843f, 0f);

        // Initial burst
        float elapsed = 0f, half = RADIANCE_DUR * 0.4f;
        while (elapsed < half)
        {
            elapsed += Time.unscaledDeltaTime;
            gold.a = Mathf.Clamp01(elapsed / half) * 0.5f;
            if (radianceOverlay != null) radianceOverlay.color = gold;
            yield return null;
        }

        // Settle to gentle continuous pulse
        while (radianceOverlay != null && radianceOverlay.gameObject.activeInHierarchy)
        {
            float pulse = Mathf.Sin(Time.unscaledTime * 2.5f) * 0.08f + 0.12f;
            gold.a = pulse;
            radianceOverlay.color = gold;
            yield return null;
        }
    }

    private IEnumerator CountUpTime(float target, bool pb)
    {
        float elapsed = 0f;
        while (elapsed < STAT_COUNT_DUR)
        {
            elapsed += Time.unscaledDeltaTime;
            float cur = Mathf.Lerp(0f, target, Mathf.Clamp01(elapsed / STAT_COUNT_DUR));
            int m = Mathf.FloorToInt(cur / 60f), s = Mathf.FloorToInt(cur % 60f);
            bestTimeLabel.text = $"{m}:{s:D2}";
            yield return null;
        }
        int fm = Mathf.FloorToInt(target / 60f), fs = Mathf.FloorToInt(target % 60f);
        bestTimeLabel.text = $"{fm}:{fs:D2}" + (pb ? "  (Personal Best!)" : "");
    }

    private void ClearStats()
    {
        if (bestTimeLabel    != null) bestTimeLabel.text    = "";
        if (stancesUsedLabel != null) stancesUsedLabel.text = "";
        if (completionDiamonds != null)
            foreach (var d in completionDiamonds)
                if (d != null) d.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    }

    private void GoToNextTrial()
    {
        if (string.IsNullOrEmpty(nextTrialSceneName)) { ReturnToTrialSelection(); return; }
        if (ScreenTransitionManager.Instance != null)
            ScreenTransitionManager.Instance.FadeToScene(nextTrialSceneName);
        else
            SceneManager.LoadScene(nextTrialSceneName);
    }

    /// <summary>Returns to the main menu trial selection screen.</summary>
    private void ReturnToTrialSelection()
    {
        Time.timeScale = 1f;
        MainMenuController.RequestTrialSelectOnLoad();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
