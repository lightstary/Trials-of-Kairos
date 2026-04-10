using UnityEngine;

public class TimeScaleLogic : MonoBehaviour
{
    public static TimeScaleLogic Instance;

    [Header("References")]
    public TimeScaleMeter meter;

    [Header("Settings")]
    public float tickInterval = 1f;
    public float minValue = -10f;
    public float maxValue = 10f;
    public float dangerZone = 8f;

    private float currentValue = 0f;
    private float tickTimer = 0f;
    private bool isDead = false;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (isDead) return;
        if (TimeState.Instance == null) return;

        switch (TimeState.Instance.currentState)
        {
            case TimeState.State.Forward:
                if (currentValue >= maxValue) return;
                tickTimer += Time.deltaTime;
                if (tickTimer >= tickInterval)
                {
                    tickTimer = 0f;
                    currentValue += 1f;
                    currentValue = Mathf.Clamp(currentValue, minValue, maxValue);
                    UpdateMeter();
                    CheckDeath();
                }
                break;

            case TimeState.State.Frozen:
                tickTimer = 0f;
                break;

            case TimeState.State.Reverse:
                if (currentValue <= minValue) return;
                tickTimer += Time.deltaTime;
                if (tickTimer >= tickInterval)
                {
                    tickTimer = 0f;
                    currentValue -= 1f;
                    currentValue = Mathf.Clamp(currentValue, minValue, maxValue);
                    UpdateMeter();
                    CheckDeath();
                }
                break;
        }
    }

    void UpdateMeter()
    {
        if (meter == null) return;

        // Map -10 to +10 onto 0 to 100 for the meter for now (daniel can change that)
        float mapped = Mathf.InverseLerp(minValue, maxValue, currentValue) * 100f;
        meter.SetValue(mapped);
    }

    void CheckDeath()
    {
        // Only kill during boss fight
        if (BossFight.Instance == null) return;
        if (!BossFight.Instance.bossActive) return;

        if (currentValue >= maxValue || currentValue <= minValue)
        {
            isDead = true;
            if (BossPopup.Instance != null)
                BossPopup.Instance.ShowLose();
            FindObjectOfType<FallDetection>().Respawn();
        }
    }

    public void ResetMeter()
    {
        currentValue = 0f;
        isDead = false;
        tickTimer = 0f;
        UpdateMeter();
    }
}