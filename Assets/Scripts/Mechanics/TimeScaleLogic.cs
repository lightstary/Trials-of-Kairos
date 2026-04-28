using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Global time value that progresses continuously based on player orientation.
/// Upright = forward, upside down = reverse, flat = frozen at exact current value.
/// Value progresses at (1 / tickInterval) * rateMultiplier units per second.
/// Reaching minValue or maxValue triggers a lose condition in all game modes.
/// </summary>
public class TimeScaleLogic : MonoBehaviour
{
    public static TimeScaleLogic Instance;

    /// <summary>Threat level for escalating UI feedback.</summary>
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
    [Tooltip("Global rate multiplier (< 1 = slower progression). Applied in all modes.")]
    [FormerlySerializedAs("bossRateMultiplier")]
    public float rateMultiplier = 0.4f;

    private float currentValue = 0f;
    private bool isDead = false;
    private ThreatState currentThreat = ThreatState.Safe;

    /// <summary>Current time scale value (continuous float, can be negative).</summary>
    public float CurrentValue => currentValue;

    /// <summary>True when the player has hit a fatal time boundary.</summary>
    public bool IsDead => isDead;

    /// <summary>Current threat level.</summary>
    public ThreatState CurrentThreatState => currentThreat;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (isDead) return;
        if (TimeState.Instance == null) return;

        bool bossActive = (BossFight.Instance != null && BossFight.Instance.bossActive)
                       || (BossBFight.Instance != null && BossBFight.Instance.bossActive)
                       || (BossCFight.Instance != null && BossCFight.Instance.bossActive);

        // Don't accrue time until the intro modal has been dismissed (hub only)
        if (!bossActive && TimeScaleIntroModal.IsTimeLocked) return;

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

    /// <summary>Evaluates threat level and triggers fail at extremes.</summary>
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

    /// <summary>Triggers the lose flow during normal gameplay (no boss active).</summary>
    private void TriggerNormalLose()
    {
        // Route through FallDetection for the disintegration effect
        FallDetection fd = FindObjectOfType<FallDetection>();
        if (fd != null)
        {
            fd.TriggerTimelineDeath("THE TIMELINE HAS COLLAPSED");
            return;
        }

        // Fallback: no FallDetection found, show screen directly
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

    /// <summary>Triggers the boss fight lose flow with proper UI.</summary>
    private void TriggerBossLose()
    {
        if (BossFight.Instance != null && BossFight.Instance.bossActive)
            BossFight.Instance.StopBossFight();
        if (BossBFight.Instance != null && BossBFight.Instance.bossActive)
            BossBFight.Instance.StopBossFight();
        if (BossCFight.Instance != null && BossCFight.Instance.bossActive)
            BossCFight.Instance.StopBossFight();

        // Route through FallDetection for the disintegration effect
        FallDetection fd = FindObjectOfType<FallDetection>();
        if (fd != null)
        {
            fd.TriggerTimelineDeath("THE TIMELINE HAS COLLAPSED");
            return;
        }

        // Fallback: no FallDetection found, show screen directly
        SoundManager sm = FindObjectOfType<SoundManager>();
        if (sm != null) sm.PlayLose();

        GameOverScreenController gosc = FindObjectOfType<GameOverScreenController>(true);
        if (gosc != null)
        {
            Time.timeScale = 0f;
            gosc.Show("THE TIMELINE HAS COLLAPSED");
            return;
        }

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

    /// <summary>Resets time to 0 and clears death state.</summary>
    public void ResetMeter()
    {
        currentValue = 0f;
        isDead = false;
        currentThreat = ThreatState.Safe;
    }
}
