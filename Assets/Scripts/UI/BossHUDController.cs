using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Boss HUD with three variant modes:
/// EdgeLimits — static min/max threshold markers on the meter.
/// MovingPointer — sweeping danger pointer that speeds up each phase.
/// RhythmPattern — scrolling danger segments across a lane.
/// </summary>
public class BossHUDController : MonoBehaviour
{
    public enum BossHUDVariant { EdgeLimits, MovingPointer, RhythmPattern }

    [Header("Variant")]
    [SerializeField] private BossHUDVariant variant = BossHUDVariant.EdgeLimits;

    [Header("Boss Health")]
    [SerializeField] private Image           healthFill;
    [SerializeField] private TextMeshProUGUI bossNameLabel;
    [SerializeField] private TextMeshProUGUI phaseLabel;

    [Header("TimeScale Meter")]
    [SerializeField] private TimeScaleMeter timeScaleMeter;

    [Header("Variant A — Edge Limits")]
    [SerializeField] private RectTransform minMarker;
    [SerializeField] private RectTransform maxMarker;
    [SerializeField] private Image         safeZoneHighlight;

    [Header("Variant B — Moving Pointer")]
    [SerializeField] private RectTransform dangerPointer;

    [Header("Variant C — Rhythm Pattern")]
    [SerializeField] private RectTransform rhythmLane;
    [SerializeField] private GameObject    dangerSegmentPrefab;

    [Header("Visual")]
    [SerializeField] private Image screenDangerVignette;

    private const float BASE_POINTER_SPEED     = 1f;
    private const float POINTER_SPEED_PER_PHASE = 2.5f;
    private const float HEALTH_LERP_SPEED      = 3f;

    private float bossMaxHealth     = 100f;
    private float bossCurrentHealth = 100f;
    private float displayHealth     = 100f;
    private int   currentPhase      = 1;

    private float pointerPos       = 0f;
    private float pointerDir       = 1f;
    private float pointerSpeed;
    private float minLimit         = 0.2f;
    private float maxLimit         = 0.8f;
    private bool  dangerActive;

    void Start()
    {
        displayHealth = bossCurrentHealth;
        pointerSpeed  = BASE_POINTER_SPEED;
        UpdatePhaseLabel();
    }

    void Update()
    {
        UpdateHealthBar();
        switch (variant)
        {
            case BossHUDVariant.EdgeLimits:    UpdateEdgeLimits();    break;
            case BossHUDVariant.MovingPointer: UpdateMovingPointer(); break;
            case BossHUDVariant.RhythmPattern: UpdateRhythmLane();    break;
        }
        UpdateDangerVignette();
    }

    /// <summary>Initialises the HUD for a boss encounter.</summary>
    public void Initialize(string bossName, float maxHealth, int phases, BossHUDVariant v)
    {
        bossMaxHealth     = maxHealth;
        bossCurrentHealth = maxHealth;
        displayHealth     = maxHealth;
        currentPhase      = 1;
        variant           = v;
        if (bossNameLabel != null) bossNameLabel.text = bossName.ToUpper();
        UpdatePhaseLabel();
    }

    /// <summary>Sets the boss health.</summary>
    public void SetHealth(float health) =>
        bossCurrentHealth = Mathf.Clamp(health, 0f, bossMaxHealth);

    /// <summary>Advances to a new phase (speeds up moving pointer).</summary>
    public void SetPhase(int phase)
    {
        currentPhase = Mathf.Max(1, phase);
        pointerSpeed = BASE_POINTER_SPEED + (currentPhase - 1) * POINTER_SPEED_PER_PHASE;
        UpdatePhaseLabel();
    }

    /// <summary>Sets edge limit fractions for Variant A (0–1).</summary>
    public void SetEdgeLimits(float min, float max)
    {
        minLimit = Mathf.Clamp01(min);
        maxLimit = Mathf.Clamp01(max);
    }

    /// <summary>Sets whether the danger vignette is pulsing.</summary>
    public void SetDangerActive(bool active) => dangerActive = active;

    /// <summary>Spawns a danger segment in the rhythm lane (Variant C).</summary>
    public void SpawnRhythmSegment(float width)
    {
        if (dangerSegmentPrefab == null || rhythmLane == null) return;
        GameObject seg = Instantiate(dangerSegmentPrefab, rhythmLane);
        RectTransform rt = seg.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.sizeDelta = new Vector2(width, rt.sizeDelta.y);
            rt.anchoredPosition = new Vector2(rhythmLane.rect.width * 0.5f, 0f);
        }
    }

    private void UpdateHealthBar()
    {
        displayHealth = Mathf.Lerp(displayHealth, bossCurrentHealth, HEALTH_LERP_SPEED * Time.deltaTime);
        if (healthFill != null) healthFill.fillAmount = displayHealth / bossMaxHealth;
    }

    private void UpdatePhaseLabel()
    {
        if (phaseLabel != null) phaseLabel.text = $"PHASE {currentPhase}";
    }

    private void UpdateEdgeLimits()
    {
        PositionMarkerAtFraction(minMarker, minLimit);
        PositionMarkerAtFraction(maxMarker, maxLimit);
        if (safeZoneHighlight != null)
        {
            Color c = TimeStateUIManager.Instance != null
                ? TimeStateUIManager.Instance.goldColor : Color.yellow;
            c.a = 0.1f; safeZoneHighlight.color = c;
        }
    }

    private void UpdateMovingPointer()
    {
        if (dangerPointer == null) return;
        pointerPos += pointerDir * pointerSpeed * Time.deltaTime;
        if (pointerPos >= 1f) { pointerPos = 1f; pointerDir = -1f; }
        else if (pointerPos <= 0f) { pointerPos = 0f; pointerDir = 1f; }
        PositionMarkerAtFraction(dangerPointer, pointerPos);
        float squish = Mathf.Min(pointerPos, 1f - pointerPos) < 0.05f ? 0.8f : 1f;
        dangerPointer.localScale = new Vector3(1f, squish, 1f);
    }

    private void UpdateRhythmLane()
    {
        if (rhythmLane == null) return;
        foreach (RectTransform child in rhythmLane)
        {
            child.anchoredPosition += Vector2.left * 100f * Time.deltaTime;
            if (child.anchoredPosition.x < -rhythmLane.rect.width * 0.5f)
                Destroy(child.gameObject);
        }
    }

    private void UpdateDangerVignette()
    {
        if (screenDangerVignette == null) return;
        if (dangerActive)
        {
            Color c = TimeStateUIManager.Instance != null
                ? TimeStateUIManager.Instance.dangerColor : Color.red;
            c.a = Mathf.Sin(Time.time * 4f) * 0.15f + 0.2f;
            screenDangerVignette.color = c;
        }
        else
        {
            Color c = screenDangerVignette.color;
            c.a = Mathf.Lerp(c.a, 0f, 5f * Time.deltaTime);
            screenDangerVignette.color = c;
        }
    }

    private static void PositionMarkerAtFraction(RectTransform marker, float fraction)
    {
        if (marker == null || marker.parent == null) return;
        RectTransform parent = marker.parent.GetComponent<RectTransform>();
        if (parent == null) return;
        float h = parent.rect.height;
        marker.anchoredPosition = new Vector2(marker.anchoredPosition.x,
            fraction * h - h * 0.5f);
    }
}
