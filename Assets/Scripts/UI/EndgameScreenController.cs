using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Endgame screen shown when all trials are complete.
/// </summary>
public class EndgameScreenController : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject      endgamePanel;
    [SerializeField] private CanvasGroup     canvasGroup;

    [Header("Title")]
    [SerializeField] private TextMeshProUGUI titleLabel;
    [SerializeField] private TextMeshProUGUI subtitleLabel;

    [Header("Stats")]
    [SerializeField] private TextMeshProUGUI totalTimeLabel;
    [SerializeField] private TextMeshProUGUI trialsClearedLabel;
    [SerializeField] private TextMeshProUGUI stancesMasteredLabel;
    [SerializeField] private TextMeshProUGUI rankLabel;

    [Header("Trial Diamonds")]
    [SerializeField] private Image[] trialDiamonds;

    [Header("Buttons")]
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button playAgainButton;

    [Header("Visual")]
    [SerializeField] private Image nebulaOverlay;

    [Header("Config")]
    [SerializeField] private string mainMenuSceneName  = "MainMenu";
    [SerializeField] private string firstTrialSceneName = "Level1";

    private const string TITLE_TEXT    = "CHRONOS  DEFIED";
    private const string SUBTITLE_TEXT = "ALL TRIALS COMPLETE";
    private const float  STAT_DUR      = 1.0f;
    private const float  STAT_DELAY    = 0.4f;

    void Start()
    {
        if (mainMenuButton  != null) mainMenuButton.onClick.AddListener(GoToMainMenu);
        if (playAgainButton != null) playAgainButton.onClick.AddListener(PlayAgain);
        if (endgamePanel    != null) endgamePanel.SetActive(false);
    }

    /// <summary>Shows the endgame screen with full stats.</summary>
    public void Show(float totalTime, int cleared, int total,
                     bool forward, bool frozen, bool reverse, string rank)
    {
        if (endgamePanel != null) endgamePanel.SetActive(true);
        if (titleLabel    != null) titleLabel.text    = TITLE_TEXT;
        if (subtitleLabel != null) subtitleLabel.text = SUBTITLE_TEXT;
        StartCoroutine(Animate(totalTime, cleared, total, forward, frozen, reverse, rank));
    }

    private IEnumerator Animate(float totalTime, int cleared, int total,
                                bool forward, bool frozen, bool reverse, string rank)
    {
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        ClearStats();

        if (nebulaOverlay != null) StartCoroutine(AnimateNebula());

        float elapsed = 0f, dur = 0.8f;
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            if (canvasGroup != null) canvasGroup.alpha = Mathf.Clamp01(elapsed / dur);
            yield return null;
        }
        if (canvasGroup != null) canvasGroup.alpha = 1f;

        yield return new WaitForSecondsRealtime(STAT_DELAY);

        if (totalTimeLabel != null) yield return CountUpTime(totalTime);

        yield return new WaitForSecondsRealtime(STAT_DELAY);

        if (trialsClearedLabel != null) trialsClearedLabel.text = $"{cleared} / {total}";

        Color gold = TimeStateUIManager.Instance != null
            ? TimeStateUIManager.Instance.goldColor : new Color(1f, 0.843f, 0f);
        if (trialDiamonds != null)
        {
            for (int i = 0; i < trialDiamonds.Length; i++)
            {
                if (trialDiamonds[i] != null)
                    trialDiamonds[i].color = i < cleared ? gold : new Color(0.3f, 0.3f, 0.3f, 0.5f);
                yield return new WaitForSecondsRealtime(0.1f);
            }
        }

        yield return new WaitForSecondsRealtime(STAT_DELAY);

        if (stancesMasteredLabel != null)
        {
            string s = "";
            if (forward) s += "FORWARD";
            if (frozen)  s += (s.Length > 0 ? " \u00B7 " : "") + "FROZEN";
            if (reverse) s += (s.Length > 0 ? " \u00B7 " : "") + "REVERSED";
            stancesMasteredLabel.text = s;
        }

        yield return new WaitForSecondsRealtime(STAT_DELAY);
        if (rankLabel != null) rankLabel.text = $"{rank}  \u25C6";
    }

    private IEnumerator CountUpTime(float target)
    {
        float elapsed = 0f;
        while (elapsed < STAT_DUR)
        {
            elapsed += Time.unscaledDeltaTime;
            float cur = Mathf.Lerp(0f, target, Mathf.Clamp01(elapsed / STAT_DUR));
            int h = Mathf.FloorToInt(cur / 3600f),
                m = Mathf.FloorToInt((cur % 3600f) / 60f),
                s = Mathf.FloorToInt(cur % 60f);
            totalTimeLabel.text = $"{h}:{m:D2}:{s:D2}";
            yield return null;
        }
        int fh = Mathf.FloorToInt(target / 3600f),
            fm = Mathf.FloorToInt((target % 3600f) / 60f),
            fs = Mathf.FloorToInt(target % 60f);
        totalTimeLabel.text = $"{fh}:{fm:D2}:{fs:D2}";
    }

    private IEnumerator AnimateNebula()
    {
        Color[] colors = TimeStateUIManager.Instance != null
            ? new[] { TimeStateUIManager.Instance.forwardColor, TimeStateUIManager.Instance.frozenColor, TimeStateUIManager.Instance.reverseColor }
            : new[] { new Color(0.961f, 0.784f, 0.259f), new Color(0.353f, 0.706f, 0.941f), new Color(0.608f, 0.365f, 0.898f) };

        float elapsed = 0f, dur = 3f;
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / dur;
            int   ci = Mathf.FloorToInt(t * colors.Length) % colors.Length;
            int   ni = (ci + 1) % colors.Length;
            Color c  = Color.Lerp(colors[ci], colors[ni], (t * colors.Length) % 1f);
            c.a = 0.15f; nebulaOverlay.color = c;
            yield return null;
        }
    }

    private void ClearStats()
    {
        if (totalTimeLabel       != null) totalTimeLabel.text       = "";
        if (trialsClearedLabel   != null) trialsClearedLabel.text   = "";
        if (stancesMasteredLabel != null) stancesMasteredLabel.text = "";
        if (rankLabel            != null) rankLabel.text            = "";
    }

    private void GoToMainMenu()
    {
        if (ScreenTransitionManager.Instance != null)
            ScreenTransitionManager.Instance.FadeToScene(mainMenuSceneName);
        else
            SceneManager.LoadScene(mainMenuSceneName);
    }

    private void PlayAgain()
    {
        if (ScreenTransitionManager.Instance != null)
            ScreenTransitionManager.Instance.FadeToScene(firstTrialSceneName);
        else
            SceneManager.LoadScene(firstTrialSceneName);
    }
}
