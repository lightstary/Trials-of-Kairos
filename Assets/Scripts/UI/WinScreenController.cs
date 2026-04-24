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
    [SerializeField] public string nextTrialSceneName = "";

    /// <summary>
    /// Returns the next trial scene based on the CURRENT scene name.
    /// This eliminates dependency on serialized field values.
    /// </summary>
    private string GetNextTrialScene()
    {
        string current = SceneManager.GetActiveScene().name;
        switch (current)
        {
            case "MainScene":   return "GardenScene";
            case "GardenScene": return "ClockScene";
            default:            return ""; // Clock and others have no next trial
        }
    }

    private const string TITLE_TEXT          = "TIME  RESTORED";
    private const float  STAT_COUNT_DUR      = 0.8f;
    private const float  STAT_DELAY          = 0.3f;
    private const float  RADIANCE_DUR        = 1.2f;
    private const float  TITLE_DISPLAY_TIME  = 1.8f;
    private const float  TITLE_FADE_DUR      = 0.6f;
    private const float  SUBTITLE_REVEAL_DUR = 0.8f;

    private static readonly Color NEW_RECORD_GREEN = new Color(0.2f, 0.9f, 0.3f, 1f);
    private static readonly Color BTN_BG = new Color(0.08f, 0.12f, 0.22f, 1f);
    private static readonly Color BTN_GOLD = new Color(0.961f, 0.784f, 0.259f, 1f);

    private bool _shown = false;
    private bool _listenersWired = false;
    private string _trialName = "";
    private TextMeshProUGUI _newRecordLabel;
    private Button _retryButton;

    void Start()
    {
        EnsureListenersWired();
        if (!_shown && winPanel != null) winPanel.SetActive(false);
    }

    /// <summary>Shows the win screen with stats.</summary>
    public void Show(string trialName, float completionTime, int stars,
                     bool isPersonalBest, bool usedForward, bool usedFrozen, bool usedReverse)
    {
        _shown = true;
        _trialName = trialName;
        EnsureListenersWired();
        if (winPanel != null) winPanel.SetActive(true);

        // Dismiss any lingering center flash so it doesn't overlap the win screen
        if (HUDController.Instance != null)
            HUDController.Instance.DismissCenterFlash();

        if (titleLabel    != null) { titleLabel.text = TITLE_TEXT; titleLabel.alpha = 1f; }
        if (subtitleLabel != null) { subtitleLabel.text = ""; subtitleLabel.alpha = 0f; }

        // Hide Next Trial button if no next scene
        string nextScene = GetNextTrialScene();
        if (nextTrialButton != null)
            nextTrialButton.gameObject.SetActive(!string.IsNullOrEmpty(nextScene));

        // Session-only best time — check if this is a new record
        string key = BestTimeTracker.KeyForScene(SceneManager.GetActiveScene().name);
        float previousBest = -1f;
        if (key != null)
        {
            previousBest = BestTimeTracker.Get(key);
            BestTimeTracker.Record(key, completionTime);
        }

        bool isNewRecord = previousBest > 0f && completionTime < previousBest;

        ApplyCinzelFonts();
        StartCoroutine(Animate(completionTime, stars, isPersonalBest || isNewRecord, usedForward, usedFrozen, usedReverse));
    }

    /// <summary>Shows a simple win screen without detailed stats.</summary>
    public void ShowSimple()
    {
        _shown = true;
        EnsureListenersWired();
        if (winPanel != null) winPanel.SetActive(true);

        // Dismiss any lingering center flash so it doesn't overlap the win screen
        if (HUDController.Instance != null)
            HUDController.Instance.DismissCenterFlash();
        if (titleLabel    != null) titleLabel.text    = TITLE_TEXT;
        if (subtitleLabel != null) subtitleLabel.text = "TRIAL COMPLETE";

        if (nextTrialButton != null)
            nextTrialButton.gameObject.SetActive(!string.IsNullOrEmpty(GetNextTrialScene()));

        string key = BestTimeTracker.KeyForScene(SceneManager.GetActiveScene().name);
        if (key != null) BestTimeTracker.MarkComplete(key);

        ApplyCinzelFonts();
        StartCoroutine(SimpleShowRoutine());
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

    /// <summary>Wire onClick listeners and create Retry button if needed.</summary>
    private void EnsureListenersWired()
    {
        if (_listenersWired) return;
        _listenersWired = true;

        // Create the Retry button dynamically (not in scene hierarchy)
        CreateRetryButton();

        if (nextTrialButton != null)
        {
            nextTrialButton.onClick.AddListener(GoToNextTrial);
            Debug.Log($"[WinScreen] Wired NextTrial button. nextScene='{GetNextTrialScene()}'");
        }
        if (returnToHubButton != null)
        {
            returnToHubButton.onClick.AddListener(ReturnToTrialSelection);

            // Rename the label to "TRIAL SELECTION"
            TextMeshProUGUI hubLabel = returnToHubButton.GetComponentInChildren<TextMeshProUGUI>();
            if (hubLabel != null) hubLabel.text = "TRIAL SELECTION";
        }
    }

    /// <summary>Creates a Retry Trial button as the first button in the panel.</summary>
    private void CreateRetryButton()
    {
        // Find the button container (WinPanel)
        Transform container = null;
        if (nextTrialButton != null)
            container = nextTrialButton.transform.parent;
        else if (returnToHubButton != null)
            container = returnToHubButton.transform.parent;
        if (container == null) return;

        GameObject retryGO = new GameObject("RetryTrialButton");
        retryGO.transform.SetParent(container, false);

        RectTransform retryRT = retryGO.AddComponent<RectTransform>();
        retryRT.anchorMin = new Vector2(0.5f, 0f);
        retryRT.anchorMax = new Vector2(0.5f, 0f);
        retryRT.pivot = new Vector2(0.5f, 0f);
        retryRT.sizeDelta = new Vector2(190f, 50f);
        // Position depends on whether Next Trial is visible (3 vs 2 buttons)
        bool hasNextTrial = !string.IsNullOrEmpty(GetNextTrialScene());
        float retryX = hasNextTrial ? -230f : -120f;
        retryRT.anchoredPosition = new Vector2(retryX, 50f);

        Image retryImg = retryGO.AddComponent<Image>();
        retryImg.color = BTN_BG;

        _retryButton = retryGO.AddComponent<Button>();
        _retryButton.targetGraphic = retryImg;
        ColorBlock cb = ColorBlock.defaultColorBlock;
        cb.normalColor = Color.white;
        cb.highlightedColor = BTN_GOLD;
        cb.selectedColor = BTN_GOLD;
        cb.pressedColor = new Color(0.7f, 0.55f, 0.1f, 1f);
        cb.fadeDuration = 0.05f;
        _retryButton.colors = cb;
        _retryButton.onClick.AddListener(RetryTrial);

        // Label
        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(retryGO.transform, false);
        RectTransform lblRT = labelGO.AddComponent<RectTransform>();
        lblRT.anchorMin = Vector2.zero;
        lblRT.anchorMax = Vector2.one;
        lblRT.offsetMin = Vector2.zero;
        lblRT.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text = "RETRY TRIAL";
        tmp.fontSize = 22f;
        tmp.color = new Color(0.91f, 0.918f, 0.965f, 1f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        CinzelFontHelper.Apply(tmp, true);
    }

    /// <summary>Selects a button so the Xbox controller can navigate the win screen.</summary>
    private void SelectDefaultButton()
    {
        Button target = (nextTrialButton != null && nextTrialButton.gameObject.activeSelf)
            ? nextTrialButton : returnToHubButton;
        if (target != null && UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(target.gameObject);
    }

    private IEnumerator Animate(float time, int stars, bool isNewRecord,
                                bool forward, bool frozen, bool reverse)
    {
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        ClearStats();

        if (radianceOverlay != null) StartCoroutine(AnimateRadiance());
        yield return FadeIn(0.5f);

        // ── Phase 1: Show "TIME RESTORED" alone ─────────────────────────
        yield return new WaitForSecondsRealtime(TITLE_DISPLAY_TIME);

        // ── Phase 2: Fade out title, then reveal subtitle letter-by-letter
        yield return FadeOutLabel(titleLabel, TITLE_FADE_DUR);

        if (subtitleLabel != null)
        {
            string fullText = $"{_trialName} COMPLETE".ToUpper();
            subtitleLabel.alpha = 1f;
            yield return RevealTextLeftToRight(subtitleLabel, fullText, SUBTITLE_REVEAL_DUR);
        }

        yield return new WaitForSecondsRealtime(STAT_DELAY);

        // ── Phase 3: Diamonds ───────────────────────────────────────────
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

        // ── Phase 4: Time stat ──────────────────────────────────────────
        yield return new WaitForSecondsRealtime(STAT_DELAY);
        if (bestTimeLabel != null) yield return CountUpTime(time);

        // ── Phase 5: NEW RECORD flash ───────────────────────────────────
        if (isNewRecord)
        {
            yield return new WaitForSecondsRealtime(0.3f);
            ShowNewRecordLabel();
        }

        SelectDefaultButton();
    }

    /// <summary>Fades out a TMP label over the given duration using unscaled time.</summary>
    private IEnumerator FadeOutLabel(TextMeshProUGUI label, float duration)
    {
        if (label == null) yield break;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            label.alpha = 1f - Mathf.Clamp01(elapsed / duration);
            yield return null;
        }
        label.alpha = 0f;
    }

    /// <summary>
    /// Reveals text character-by-character from left to right using TMP's
    /// maxVisibleCharacters property, creating a letter-drop effect.
    /// </summary>
    private IEnumerator RevealTextLeftToRight(TextMeshProUGUI label, string fullText, float duration)
    {
        label.text = fullText;
        label.ForceMeshUpdate();
        int totalChars = label.textInfo.characterCount;
        label.maxVisibleCharacters = 0;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // Ease out for a smooth drop-in feel
            float eased = 1f - Mathf.Pow(1f - t, 2f);
            label.maxVisibleCharacters = Mathf.FloorToInt(eased * totalChars);
            yield return null;
        }
        label.maxVisibleCharacters = totalChars;
    }

    /// <summary>Creates and animates a "NEW RECORD" label below the time stat.</summary>
    private void ShowNewRecordLabel()
    {
        if (bestTimeLabel == null) return;

        // Create the label as a sibling below the best time
        Transform parent = bestTimeLabel.transform.parent;
        if (parent == null) parent = bestTimeLabel.transform;

        GameObject go = new GameObject("NewRecordLabel");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();

        // Position below the best time label
        RectTransform bestRT = bestTimeLabel.GetComponent<RectTransform>();
        rt.anchorMin = bestRT.anchorMin;
        rt.anchorMax = bestRT.anchorMax;
        rt.pivot = bestRT.pivot;
        rt.anchoredPosition = bestRT.anchoredPosition + Vector2.down * 40f;
        rt.sizeDelta = bestRT.sizeDelta;

        _newRecordLabel = go.AddComponent<TextMeshProUGUI>();
        _newRecordLabel.text = "NEW RECORD";
        _newRecordLabel.fontSize = 28f;
        _newRecordLabel.color = NEW_RECORD_GREEN;
        _newRecordLabel.alignment = TextAlignmentOptions.Center;
        _newRecordLabel.characterSpacing = 8f;
        _newRecordLabel.raycastTarget = false;
        CinzelFontHelper.Apply(_newRecordLabel, true);

        StartCoroutine(PulseNewRecord());
    }

    /// <summary>Pulses the NEW RECORD label alpha for emphasis.</summary>
    private IEnumerator PulseNewRecord()
    {
        if (_newRecordLabel == null) yield break;

        // Flash in
        _newRecordLabel.alpha = 0f;
        float elapsed = 0f;
        while (elapsed < 0.4f)
        {
            elapsed += Time.unscaledDeltaTime;
            _newRecordLabel.alpha = Mathf.Clamp01(elapsed / 0.4f);
            yield return null;
        }

        // Gentle pulse
        while (_newRecordLabel != null && _newRecordLabel.gameObject.activeInHierarchy)
        {
            float pulse = Mathf.Sin(Time.unscaledTime * 3f) * 0.15f + 0.85f;
            _newRecordLabel.alpha = pulse;
            yield return null;
        }
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

        float elapsed = 0f, half = RADIANCE_DUR * 0.4f;
        while (elapsed < half)
        {
            elapsed += Time.unscaledDeltaTime;
            gold.a = Mathf.Clamp01(elapsed / half) * 0.5f;
            if (radianceOverlay != null) radianceOverlay.color = gold;
            yield return null;
        }

        while (radianceOverlay != null && radianceOverlay.gameObject.activeInHierarchy)
        {
            float pulse = Mathf.Sin(Time.unscaledTime * 2.5f) * 0.08f + 0.12f;
            gold.a = pulse;
            radianceOverlay.color = gold;
            yield return null;
        }
    }

    private IEnumerator CountUpTime(float target)
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
        bestTimeLabel.text = $"{fm}:{fs:D2}";
    }

    private void ClearStats()
    {
        if (bestTimeLabel    != null) bestTimeLabel.text    = "";
        if (stancesUsedLabel != null) stancesUsedLabel.text = "";
        if (_newRecordLabel  != null) Destroy(_newRecordLabel.gameObject);
        if (completionDiamonds != null)
            foreach (var d in completionDiamonds)
                if (d != null) d.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    }

    private void GoToNextTrial()
    {
        string nextScene = GetNextTrialScene();
        Debug.Log($"[WinScreen] GoToNextTrial called. nextScene='{nextScene}' (current='{SceneManager.GetActiveScene().name}')");
        Time.timeScale = 1f;
        if (string.IsNullOrEmpty(nextScene))
        {
            Debug.Log("[WinScreen] No next scene — falling back to trial selection.");
            ReturnToTrialSelection();
            return;
        }

        // For scenes that have a MainMenuController (like Garden/Clock loaded
        // from MainScene's framework), tell it to skip the menu.
        MainMenuController.SkipMenuOnLoad = true;

        Debug.Log($"[WinScreen] SceneManager.LoadScene('{nextScene}') executing now.");
        SceneManager.LoadScene(nextScene);
    }

    /// <summary>Reloads the current scene to retry the trial.</summary>
    private void RetryTrial()
    {
        Debug.Log("[WinScreen] RetryTrial — reloading current scene.");
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>Returns to the trial selection screen (always in MainScene).</summary>
    private void ReturnToTrialSelection()
    {
        Time.timeScale = 1f;
        MainMenuController.RequestTrialSelectOnLoad();
        SceneManager.LoadScene("MainScene");
    }
}
