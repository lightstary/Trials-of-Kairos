using UnityEngine;

/// <summary>
/// Global time value that progresses continuously based on player orientation.
/// Upright = forward, upside down = reverse, flat = frozen at exact current value.
/// Value progresses at (1 / tickInterval) units per second for smooth float precision.
/// During boss fights, escalating threat states trigger at warning/danger/fail thresholds.
/// </summary>
public class TimeScaleLogic : MonoBehaviour
{
    public static TimeScaleLogic Instance;

    /// <summary>Boss-fight threat level for escalating UI feedback.</summary>
    public enum ThreatState { Safe, Warning, Danger, Fail }

    [Header("References")]
    public TimeScaleMeter meter;

    [Header("Settings")]
    public float tickInterval = 1f;
    public float minValue = -10f;
    public float maxValue = 10f;
    public float warningZone = 5f;
    public float dangerZone = 8f;

    private float currentValue = 0f;
    private bool isDead = false;
    private ThreatState currentThreat = ThreatState.Safe;

    /// <summary>Current time scale value (continuous float, can be negative).</summary>
    public float CurrentValue => currentValue;

    /// <summary>True when the player has hit a fatal time boundary during boss fight.</summary>
    public bool IsDead => isDead;

    /// <summary>Current boss-fight threat level.</summary>
    public ThreatState CurrentThreatState => currentThreat;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (isDead) return;
        if (TimeState.Instance == null) return;

        // Don't accrue time until the intro modal has been dismissed
        if (TimeScaleIntroModal.IsTimeLocked) return;

        float rate = tickInterval > 0f ? (1f / tickInterval) : 1f;

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

    /// <summary>Evaluates boss-fight threat level and triggers fail at extremes.</summary>
    private void UpdateThreatState()
    {
        bool bossActive = BossFight.Instance != null && BossFight.Instance.bossActive;
        if (!bossActive)
        {
            currentThreat = ThreatState.Safe;
            return;
        }

        float absVal = Mathf.Abs(currentValue);

        if (currentValue >= maxValue || currentValue <= minValue)
        {
            if (!isDead)
            {
                isDead = true;
                currentThreat = ThreatState.Fail;
                TriggerBossLose();
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

    /// <summary>Triggers the boss fight lose flow with proper UI.</summary>
    private void TriggerBossLose()
    {
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

        if (BossPopup.Instance != null)
            BossPopup.Instance.ShowLose();

        SoundManager sm = FindObjectOfType<SoundManager>();
        if (sm != null) sm.PlayLose();
    }

    /// <summary>Resets time to 0 and clears death state.</summary>
    public void ResetMeter()
    {
        currentValue = 0f;
        isDead = false;
        currentThreat = ThreatState.Safe;
    }
}