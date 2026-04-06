using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Central broadcaster for time-state color changes.
/// All UI elements register here to receive state color updates.
/// </summary>
public class TimeStateUIManager : MonoBehaviour
{
    public static TimeStateUIManager Instance { get; private set; }

    [Header("State Colors")]
    public Color forwardColor = new Color(0.961f, 0.784f, 0.259f);  // #F5C842
    public Color frozenColor  = new Color(0.353f, 0.706f, 0.941f);  // #5AB4F0
    public Color reverseColor = new Color(0.608f, 0.365f, 0.898f);  // #9B5DE5

    [Header("UI Colors")]
    public Color dangerColor      = new Color(0.898f, 0.196f, 0.106f); // #E5321B
    public Color goldColor        = new Color(1f, 0.843f, 0f);         // #FFD700
    public Color voidDark         = new Color(0.031f, 0.043f, 0.078f); // #080B14
    public Color cosmicMid        = new Color(0.059f, 0.102f, 0.188f); // #0F1A30
    public Color starfieldWhite   = new Color(0.910f, 0.918f, 0.965f); // #E8EAF6

    [Header("Vignette")]
    [SerializeField] private Image vignetteOverlay;

    private const float VIGNETTE_FADE_DURATION = 0.5f;
    private const float VIGNETTE_MAX_ALPHA     = 0.15f;

    /// <summary>Fired when the active state color changes.</summary>
    public event Action<Color> OnStateColorChanged;

    /// <summary>Fired when the time state changes.</summary>
    public event Action<TimeState.State> OnTimeStateChanged;

    private Color  currentVignetteTarget;
    private float  vignetteTimer;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()
    {
        if (TimeState.Instance != null)
            TimeState.Instance.OnStateChanged += HandleTimeStateChanged;
    }

    void OnDisable()
    {
        if (TimeState.Instance != null)
            TimeState.Instance.OnStateChanged -= HandleTimeStateChanged;
    }

    void Start()
    {
        // Late-bind in case TimeState initialises after us
        if (TimeState.Instance != null)
        {
            TimeState.Instance.OnStateChanged -= HandleTimeStateChanged;
            TimeState.Instance.OnStateChanged += HandleTimeStateChanged;
            HandleTimeStateChanged(TimeState.Instance.currentState);
        }
    }

    void Update()
    {
        UpdateVignette();
    }

    /// <summary>Returns the color mapped to a given time state.</summary>
    public Color GetStateColor(TimeState.State state)
    {
        switch (state)
        {
            case TimeState.State.Forward: return forwardColor;
            case TimeState.State.Frozen:  return frozenColor;
            case TimeState.State.Reverse: return reverseColor;
            default:                      return forwardColor;
        }
    }

    /// <summary>Returns the currently active state color.</summary>
    public Color GetCurrentStateColor()
    {
        if (TimeState.Instance == null) return forwardColor;
        return GetStateColor(TimeState.Instance.currentState);
    }

    private void HandleTimeStateChanged(TimeState.State newState)
    {
        Color stateColor = GetStateColor(newState);
        OnStateColorChanged?.Invoke(stateColor);
        OnTimeStateChanged?.Invoke(newState);

        if (vignetteOverlay != null)
        {
            currentVignetteTarget   = stateColor;
            currentVignetteTarget.a = VIGNETTE_MAX_ALPHA;
            vignetteTimer           = 0f;
        }
    }

    private void UpdateVignette()
    {
        if (vignetteOverlay == null) return;
        vignetteTimer += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(vignetteTimer / VIGNETTE_FADE_DURATION);
        vignetteOverlay.color = Color.Lerp(vignetteOverlay.color, currentVignetteTarget, t);
    }
}
