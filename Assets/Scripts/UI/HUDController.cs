using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the in-game HUD: time-state indicator, objective panel,
/// TimeScale meter, area title, and pause button.
/// </summary>
public class HUDController : MonoBehaviour
{
    [Header("Time State Indicator")]
    [SerializeField] private Image           hourglassIcon;
    [SerializeField] private Image           hourglassOrbitRing;
    [SerializeField] private TextMeshProUGUI orientationArrowLabel;
    [SerializeField] private TextMeshProUGUI stateLabel;

    [Header("Objective Panel")]
    [SerializeField] private GameObject      objectivePanel;
    [SerializeField] private CanvasGroup     objectiveCanvasGroup;
    [SerializeField] private Image           objectiveDiamond;
    [SerializeField] private TextMeshProUGUI objectiveText;
    [SerializeField] private TextMeshProUGUI trialProgressText;

    [Header("TimeScale Meter")]
    [SerializeField] private TimeScaleMeter timeScaleMeter;

    [Header("Area Title")]
    [SerializeField] private AreaTitleIntro areaTitleIntro;

    [Header("Pause")]
    [SerializeField] private Button             pauseButton;
    [SerializeField] private PauseMenuController pauseMenu;

    private const float ORBIT_SPEED            = 15f;   // deg/s
    private const float HOURGLASS_ROTATE_SPEED = 10f;
    private const float OBJECTIVE_FADE         = 0.5f;

    private float targetHourglassAngle;
    private float currentHourglassAngle;
    private bool  objectiveCompleted;

    void OnEnable()
    {
        if (TimeStateUIManager.Instance != null)
        {
            TimeStateUIManager.Instance.OnTimeStateChanged  += HandleTimeStateChanged;
            TimeStateUIManager.Instance.OnStateColorChanged += HandleColorChanged;
        }
    }

    void OnDisable()
    {
        if (TimeStateUIManager.Instance != null)
        {
            TimeStateUIManager.Instance.OnTimeStateChanged  -= HandleTimeStateChanged;
            TimeStateUIManager.Instance.OnStateColorChanged -= HandleColorChanged;
        }
    }

    void Start()
    {
        if (pauseButton != null && pauseMenu != null)
            pauseButton.onClick.AddListener(() => TogglePause());

        if (TimeState.Instance != null)
            HandleTimeStateChanged(TimeState.Instance.currentState);

        if (TimeStateUIManager.Instance != null)
            HandleColorChanged(TimeStateUIManager.Instance.GetCurrentStateColor());
    }

    void Update()
    {
        // Smooth hourglass rotation
        currentHourglassAngle = Mathf.LerpAngle(
            currentHourglassAngle, targetHourglassAngle,
            Time.deltaTime * HOURGLASS_ROTATE_SPEED);

        if (hourglassIcon != null)
            hourglassIcon.rectTransform.localRotation =
                Quaternion.Euler(0f, 0f, currentHourglassAngle);

        // Orbit ring slow rotation
        if (hourglassOrbitRing != null)
            hourglassOrbitRing.rectTransform.Rotate(0f, 0f, -ORBIT_SPEED * Time.deltaTime);

        // Gold pulse on completed objective diamond
        if (objectiveCompleted && objectiveDiamond != null)
        {
            Color gold = TimeStateUIManager.Instance != null
                ? TimeStateUIManager.Instance.goldColor : Color.yellow;
            gold.a = Mathf.Sin(Time.time * 6f) * 0.3f + 0.7f;
            objectiveDiamond.color = gold;
        }
    }

    /// <summary>Toggles the pause menu with sub-screen and cooldown checks.</summary>
    private void TogglePause()
    {
        if (pauseMenu == null) return;
        if (pauseMenu.IsPaused)
        {
            if (pauseMenu.HasSubScreenOpen || pauseMenu.IsInInputCooldown) return;
            pauseMenu.Resume();
            return;
        }
        pauseMenu.gameObject.SetActive(true);
        pauseMenu.Pause();
    }

    /// <summary>Sets objective text and trial progress display.</summary>
    public void SetObjective(string text, int current, int total)
    {
        objectiveCompleted = false;
        if (objectiveText      != null) objectiveText.text      = text;
        if (trialProgressText  != null) trialProgressText.text  = $"TRIAL {current} / {total}";
        if (objectivePanel     != null) objectivePanel.SetActive(true);
        if (objectiveCanvasGroup != null) StartCoroutine(FadeObjectiveIn());
    }

    /// <summary>Marks the current objective as complete.</summary>
    public void CompleteObjective() => objectiveCompleted = true;

    /// <summary>Triggers the area title slide-in.</summary>
    public void ShowAreaTitle(string trialNumber, string areaName)
    {
        if (areaTitleIntro != null)
            areaTitleIntro.ShowTitle(trialNumber, areaName);
    }

    private void HandleTimeStateChanged(TimeState.State state)
    {
        switch (state)
        {
            case TimeState.State.Forward:
                targetHourglassAngle = 0f;
                if (stateLabel             != null) stateLabel.text             = "FORWARD";
                if (orientationArrowLabel  != null) orientationArrowLabel.text  = "\u2191";
                break;
            case TimeState.State.Frozen:
                targetHourglassAngle = 90f;
                if (stateLabel             != null) stateLabel.text             = "FROZEN";
                if (orientationArrowLabel  != null) orientationArrowLabel.text  = "\u2194";
                break;
            case TimeState.State.Reverse:
                targetHourglassAngle = 180f;
                if (stateLabel             != null) stateLabel.text             = "REVERSED";
                if (orientationArrowLabel  != null) orientationArrowLabel.text  = "\u2193";
                break;
        }
    }

    private void HandleColorChanged(Color color)
    {
        if (hourglassOrbitRing  != null) hourglassOrbitRing.color = color;
        if (stateLabel          != null) stateLabel.color          = color;
        if (orientationArrowLabel != null) orientationArrowLabel.color = color;
    }

    private IEnumerator FadeObjectiveIn()
    {
        float elapsed = 0f;
        objectiveCanvasGroup.alpha = 0f;
        while (elapsed < OBJECTIVE_FADE)
        {
            elapsed += Time.deltaTime;
            objectiveCanvasGroup.alpha = Mathf.Clamp01(elapsed / OBJECTIVE_FADE);
            yield return null;
        }
        objectiveCanvasGroup.alpha = 1f;
    }
}
