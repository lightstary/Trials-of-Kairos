using UnityEngine;
using UnityEngine.Serialization;

public class TimeScaleLogic : MonoBehaviour
{
    public static TimeScaleLogic Instance;

    public enum ThreatState { Safe, Warning, Danger, Fail }

    [Header("References")]
    public TimeScaleMeter meter;

    [Header("Settings")]
    public float tickInterval = 1f;
    public float minValue = -10f;
    public float maxValue = 10f;
    public float warningZone = 5f;
    public float dangerZone = 8f;

    [Header("Rate")]
    [FormerlySerializedAs("bossRateMultiplier")]
    public float rateMultiplier = 0.4f;

    private float currentValue = 0f;
    private bool isDead = false;
    private ThreatState currentThreat = ThreatState.Safe;

    public float CurrentValue => currentValue;
    public bool IsDead => isDead;
    public ThreatState CurrentThreatState => currentThreat;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (isDead) return;
        if (TimeState.Instance == null) return;

        if (ScreenTransitionManager.Instance != null && ScreenTransitionManager.Instance.IsRevealing) return;

        bool bossActive = (BossFight.Instance != null && BossFight.Instance.bossActive)
                       || (BossBFight.Instance != null && BossBFight.Instance.bossActive)
                       || (BossCFight.Instance != null && BossCFight.Instance.bossActive);

        if (!bossActive && TimeScaleIntroModal.IsTimeLocked) return;

        // Boss B controls time scale movement directly — player accrual is paused
        if (BossBFight.Instance != null && BossBFight.Instance.bossActive) return;

        float rate = tickInterval > 0f ? (1f / tickInterval) : 1f;
        rate *= rateMultiplier;

        switch (TimeState.Instance.currentState)
        {
            case TimeState.State.Forward:
                if (currentValue < maxValue)
                {
                    currentValue += rate * Time.deltaTime;
                    currentValue = Mathf.Min(currentValue, maxValue);
                }
                break;

            case TimeState.State.Frozen:
                break;

            case TimeState.State.Reverse:
                if (currentValue > minValue)
                {
                    currentValue -= rate * Time.deltaTime;
                    currentValue = Mathf.Max(currentValue, minValue);
                }
                break;
        }

        UpdateThreatState();
    }

    private void UpdateThreatState()
    {
        bool bossActive = (BossFight.Instance != null && BossFight.Instance.bossActive)
                       || (BossBFight.Instance != null && BossBFight.Instance.bossActive)
                       || (BossCFight.Instance != null && BossCFight.Instance.bossActive);

        float absVal = Mathf.Abs(currentValue);

        if (currentValue >= maxValue || currentValue <= minValue)
        {
            if (!isDead)
            {
                isDead = true;
                currentThreat = ThreatState.Fail;

                if (bossActive)
                    TriggerBossLose();
                else
                    TriggerNormalLose();
            }
        }
        else if (absVal >= dangerZone)
        {
            currentThreat = ThreatState.Danger;
        }
        else if (absVal >= warningZone)
        {
            currentThreat = ThreatState.Warning;
        }
        else
        {
            currentThreat = ThreatState.Safe;
        }
    }

    private void TriggerNormalLose()
    {
        FallDetection fd = FindObjectOfType<FallDetection>();
        if (fd != null)
        {
            fd.TriggerTimelineDeath("THE TIMELINE HAS COLLAPSED");
            return;
        }

        SoundManager sm = FindObjectOfType<SoundManager>();
        if (sm != null) sm.PlayLose();

        GameOverScreenController gosc = FindObjectOfType<GameOverScreenController>(true);
        if (gosc != null)
        {
            Time.timeScale = 0f;
            gosc.Show("THE TIMELINE HAS COLLAPSED");
            return;
        }

        Debug.LogWarning("[TimeScaleLogic] GameOverScreenController not found. No lose screen shown.");
    }

    private void TriggerBossLose()
    {
        if (BossFight.Instance != null && BossFight.Instance.bossActive)
            BossFight.Instance.StopBossFight();
        if (BossBFight.Instance != null && BossBFight.Instance.bossActive)
            BossBFight.Instance.StopBossFight();
        if (BossCFight.Instance != null && BossCFight.Instance.bossActive)
            BossCFight.Instance.StopBossFight();

        SoundManager sm = FindObjectOfType<SoundManager>();
        if (sm != null) sm.PlayLose();

        Time.timeScale = 0f;

        BossFailUI failUI = FindObjectOfType<BossFailUI>(true);
        if (failUI == null)
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                GameObject go = new GameObject("BossFailUI");
                go.transform.SetParent(canvas.transform, false);
                failUI = go.AddComponent<BossFailUI>();
            }
        }

        if (failUI != null)
            failUI.ShowFail();
    }

    public void ResetMeter()
    {
        currentValue = 0f;
        isDead = false;
        currentThreat = ThreatState.Safe;
    }

    /// <summary>Used by boss fights to push the meter directly.</summary>
    public void SetValue(float value)
    {
        currentValue = Mathf.Clamp(value, minValue, maxValue);
        UpdateThreatState();
    }
}