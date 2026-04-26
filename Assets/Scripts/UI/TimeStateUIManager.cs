using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Central broadcaster for time-state color changes.
/// Applies a barely-visible flat color wash over the entire screen via a UI Image.
/// No shader, no gradient — just a uniform tint at very low alpha.
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

    [Header("Overlay")]
    [SerializeField] private Image vignetteOverlay;

    [Tooltip("Alpha of the flat color wash. Keep very low (0.03–0.08) for subtlety.")]
    [Range(0f, 0.3f)]
    [SerializeField] private float overlayAlpha = 0.045f;

    [Header("Transition")]
    [Tooltip("How long (seconds) the color crossfade takes when switching states.")]
    [Range(0.1f, 5f)]
    [SerializeField] private float transitionDuration = 1.2f;

    /// <summary>Fired when the active state color changes.</summary>
    public event Action<Color> OnStateColorChanged;

    /// <summary>Fired when the time state changes.</summary>
    public event Action<TimeState.State> OnTimeStateChanged;

    private Color _fromColor;
    private Color _currentColor;
    private Color _targetColor;
    private float _fadeTimer;

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
        // Ensure the overlay uses the default UI material (no custom shader)
        if (vignetteOverlay != null)
            vignetteOverlay.material = null;

        // Late-bind in case TimeState initialises after us
        if (TimeState.Instance != null)
        {
            TimeState.Instance.OnStateChanged -= HandleTimeStateChanged;
            TimeState.Instance.OnStateChanged += HandleTimeStateChanged;

            // Initialise to the current state color with no transition
            Color initial = GetStateColor(TimeState.Instance.currentState);
            _fromColor    = initial;
            _currentColor = initial;
            _targetColor  = initial;
            _fadeTimer    = transitionDuration; // already at target
        }

        ApplyOverlayColor();
    }

    void Update()
    {
        UpdateOverlay();
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

    // ── State change handling ───────────────────────────────────────────

    private void HandleTimeStateChanged(TimeState.State newState)
    {
        Color stateColor = GetStateColor(newState);
        OnStateColorChanged?.Invoke(stateColor);
        OnTimeStateChanged?.Invoke(newState);

        // Snapshot current color as the starting point for this transition
        _fromColor   = _currentColor;
        _targetColor = stateColor;
        _fadeTimer   = 0f;
    }

    // ── Per-frame overlay update ────────────────────────────────────────

    private void UpdateOverlay()
    {
        if (vignetteOverlay == null) return;

        // Advance the transition timer
        _fadeTimer += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(_fadeTimer / transitionDuration);

        // Smooth ease-in-out curve (hermite) for a gentle crossfade
        t = t * t * (3f - 2f * t);

        // Proper from-to lerp so the curve shape is respected
        _currentColor = Color.Lerp(_fromColor, _targetColor, t);

        ApplyOverlayColor();
    }

    /// <summary>Sets the overlay Image color with the configured alpha.</summary>
    private void ApplyOverlayColor()
    {
        if (vignetteOverlay == null) return;
        vignetteOverlay.color = new Color(_currentColor.r, _currentColor.g, _currentColor.b, overlayAlpha);
    }
}
