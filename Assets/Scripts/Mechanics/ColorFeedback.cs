using UnityEngine;

/// <summary>
/// Smoothly transitions camera background, ambient light, and fog color
/// based on the current TimeState (Forward / Frozen / Reverse).
/// </summary>
public class ColorFeedback : MonoBehaviour
{
    [Header("Time State Colors")]
    public Color forwardColor = new Color(0.1f, 0.08f, 0.05f);
    public Color frozenColor  = new Color(0.05f, 0.08f, 0.15f);
    public Color reverseColor = new Color(0.08f, 0.03f, 0.12f);

    [Header("Ambient Tint (subtle, added to base ambient)")]
    public Color forwardAmbient = new Color(0.18f, 0.14f, 0.08f);
    public Color frozenAmbient  = new Color(0.08f, 0.12f, 0.22f);
    public Color reverseAmbient = new Color(0.14f, 0.06f, 0.20f);

    [Header("Fog Tint")]
    public Color forwardFog = new Color(0.12f, 0.10f, 0.06f);
    public Color frozenFog  = new Color(0.06f, 0.09f, 0.18f);
    public Color reverseFog = new Color(0.10f, 0.04f, 0.14f);

    public float transitionSpeed = 3f;

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        if (TimeState.Instance == null) return;

        Color targetBg      = forwardColor;
        Color targetAmbient = forwardAmbient;
        Color targetFog     = forwardFog;

        switch (TimeState.Instance.currentState)
        {
            case TimeState.State.Forward:
                targetBg      = forwardColor;
                targetAmbient = forwardAmbient;
                targetFog     = forwardFog;
                break;
            case TimeState.State.Frozen:
                targetBg      = frozenColor;
                targetAmbient = frozenAmbient;
                targetFog     = frozenFog;
                break;
            case TimeState.State.Reverse:
                targetBg      = reverseColor;
                targetAmbient = reverseAmbient;
                targetFog     = reverseFog;
                break;
        }

        float lerp = transitionSpeed * Time.deltaTime;

        cam.backgroundColor = Color.Lerp(cam.backgroundColor, targetBg, lerp);
        RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, targetAmbient, lerp);

        if (RenderSettings.fog)
            RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, targetFog, lerp);
    }
}
