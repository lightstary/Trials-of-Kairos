using UnityEngine;

/// <summary>
/// Smoothly transitions camera background, ambient light, and fog color
/// based on the current TimeState (Forward / Frozen / Reverse).
/// Kept very subtle — the edge vignette in TimeStateUIManager is the
/// primary stance feedback; these world tints are barely perceptible.
/// </summary>
public class ColorFeedback : MonoBehaviour
{
    [Header("Camera Background (subtle shift)")]
    public Color forwardColor = new Color(0.07f, 0.06f, 0.04f);
    public Color frozenColor  = new Color(0.04f, 0.06f, 0.10f);
    public Color reverseColor = new Color(0.06f, 0.03f, 0.08f);

    [Header("Ambient Tint (very subtle)")]
    public Color forwardAmbient = new Color(0.12f, 0.10f, 0.06f);
    public Color frozenAmbient  = new Color(0.06f, 0.08f, 0.14f);
    public Color reverseAmbient = new Color(0.10f, 0.05f, 0.13f);

    [Header("Fog Tint")]
    public Color forwardFog = new Color(0.08f, 0.07f, 0.04f);
    public Color frozenFog  = new Color(0.04f, 0.06f, 0.12f);
    public Color reverseFog = new Color(0.07f, 0.03f, 0.09f);

    [Header("Transition")]
    [Tooltip("Duration in seconds for the full color crossfade.")]
    public float transitionDuration = 1.2f;

    private Camera _cam;

    private Color _fromBg, _fromAmbient, _fromFog;
    private Color _toBg,   _toAmbient,   _toFog;
    private float _fadeTimer;
    private bool  _initialized;

    void Start()
    {
        _cam = GetComponent<Camera>();
    }

    void OnEnable()
    {
        if (TimeState.Instance != null)
            TimeState.Instance.OnStateChanged += HandleStateChanged;
    }

    void OnDisable()
    {
        if (TimeState.Instance != null)
            TimeState.Instance.OnStateChanged -= HandleStateChanged;
    }

    void Update()
    {
        if (TimeState.Instance == null) return;

        // Late-bind on first frame if TimeState was not ready during OnEnable
        if (!_initialized)
        {
            TimeState.Instance.OnStateChanged -= HandleStateChanged;
            TimeState.Instance.OnStateChanged += HandleStateChanged;
            SnapToState(TimeState.Instance.currentState);
            _initialized = true;
        }

        _fadeTimer += Time.deltaTime;
        float t = Mathf.Clamp01(_fadeTimer / transitionDuration);

        // Smooth ease-in-out (hermite) to match the vignette crossfade feel
        t = t * t * (3f - 2f * t);

        if (_cam != null)
            _cam.backgroundColor = Color.Lerp(_fromBg, _toBg, t);

        RenderSettings.ambientLight = Color.Lerp(_fromAmbient, _toAmbient, t);

        if (RenderSettings.fog)
            RenderSettings.fogColor = Color.Lerp(_fromFog, _toFog, t);
    }

    /// <summary>Called once on the first frame to set colors without a transition.</summary>
    private void SnapToState(TimeState.State state)
    {
        GetTargetColors(state, out _toBg, out _toAmbient, out _toFog);
        _fromBg      = _toBg;
        _fromAmbient = _toAmbient;
        _fromFog     = _toFog;
        _fadeTimer   = transitionDuration; // already at target
    }

    /// <summary>Handles state changes — begins a smooth crossfade.</summary>
    private void HandleStateChanged(TimeState.State newState)
    {
        // Snapshot current interpolated values as the new starting point
        float t = Mathf.Clamp01(_fadeTimer / transitionDuration);
        t = t * t * (3f - 2f * t);

        _fromBg      = Color.Lerp(_fromBg,      _toBg,      t);
        _fromAmbient = Color.Lerp(_fromAmbient,  _toAmbient, t);
        _fromFog     = Color.Lerp(_fromFog,      _toFog,     t);

        GetTargetColors(newState, out _toBg, out _toAmbient, out _toFog);
        _fadeTimer = 0f;
    }

    /// <summary>Maps a time state to its target color set.</summary>
    private void GetTargetColors(TimeState.State state, out Color bg, out Color ambient, out Color fog)
    {
        switch (state)
        {
            case TimeState.State.Frozen:
                bg = frozenColor; ambient = frozenAmbient; fog = frozenFog;
                return;
            case TimeState.State.Reverse:
                bg = reverseColor; ambient = reverseAmbient; fog = reverseFog;
                return;
            default:
                bg = forwardColor; ambient = forwardAmbient; fog = forwardFog;
                return;
        }
    }
}
