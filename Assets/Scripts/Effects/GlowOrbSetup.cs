using UnityEngine;

/// <summary>
/// Configures the renderer's material as a soft glowing orb using the Standard
/// shader in Fade mode with high emission. Adds gentle orbiting, vertical bobbing,
/// and slow rotation for a living, ambient feel. Stays outside the play corridor.
/// </summary>
[RequireComponent(typeof(MeshRenderer))]
public class GlowOrbSetup : MonoBehaviour
{
    private const float CORRIDOR_HALF_WIDTH = 7f;
    private const float CORRIDOR_Z_MIN = -8f;
    private const float CORRIDOR_Z_MAX = 42f;
    private const float CORRIDOR_Y_MAX = 4f;
    private const float MIN_SAFE_DISTANCE = 8f;

    [Header("Glow Settings")]
    [Tooltip("Core color of the orb glow.")]
    public Color glowColor = new Color(0.5f, 0.7f, 1f, 1f);

    [Tooltip("Emission intensity multiplier (HDR brightness).")]
    public float emissionIntensity = 2.5f;

    [Tooltip("Base alpha for the orb surface (0 = fully transparent, 1 = solid).")]
    [Range(0f, 1f)]
    public float baseAlpha = 0.12f;

    [Header("Movement")]
    [Tooltip("Radius of the slow orbit around home position.")]
    public float orbitRadius = 2f;

    [Tooltip("Orbit speed multiplier.")]
    public float orbitSpeed = 0.15f;

    [Tooltip("Vertical bob amplitude.")]
    public float bobHeight = 0.8f;

    [Tooltip("Vertical bob speed.")]
    public float bobSpeed = 0.3f;

    [Tooltip("Slow self-rotation speed in degrees/sec.")]
    public float rotateSpeed = 10f;

    private Vector3 _homePos;
    private float _phaseOffset;
    private Vector3 _rotateAxis;

    private void Awake()
    {
        ApplyGlowMaterial();
        InitMovement();
    }

    /// <summary>Initialises per-instance randomised movement parameters.</summary>
    private void InitMovement()
    {
        _homePos = transform.position;

        // Each orb gets a unique phase so they don't move in sync
        _phaseOffset = _homePos.x * 1.7f + _homePos.z * 0.9f + _homePos.y * 2.3f;

        // Random-ish rotation axis per orb
        _rotateAxis = new Vector3(
            Mathf.Sin(_phaseOffset * 3.1f),
            1f,
            Mathf.Cos(_phaseOffset * 2.7f)
        ).normalized;
    }

    private void Update()
    {
        float t = Time.time + _phaseOffset;

        // Orbit around home position in XZ plane
        float ox = Mathf.Sin(t * orbitSpeed) * orbitRadius;
        float oz = Mathf.Cos(t * orbitSpeed * 0.7f) * orbitRadius;

        // Vertical bob
        float oy = Mathf.Sin(t * bobSpeed) * bobHeight;

        Vector3 targetPos = _homePos + new Vector3(ox, oy, oz);

        // Safety: keep out of the play corridor
        targetPos = ClampOutsideCorridor(targetPos);

        transform.position = targetPos;

        // Gentle self-rotation
        if (rotateSpeed > 0f)
        {
            transform.Rotate(_rotateAxis, rotateSpeed * Time.deltaTime, Space.World);
        }
    }

    /// <summary>Pushes the position outside the tile corridor so orbs never overlap gameplay.</summary>
    private static Vector3 ClampOutsideCorridor(Vector3 pos)
    {
        bool inCorridorZ = pos.z > CORRIDOR_Z_MIN && pos.z < CORRIDOR_Z_MAX;
        bool inCorridorX = Mathf.Abs(pos.x) < CORRIDOR_HALF_WIDTH;
        bool belowCeiling = pos.y < MIN_SAFE_DISTANCE;

        if (inCorridorX && inCorridorZ && belowCeiling)
        {
            // Push outward along whichever axis is cheapest
            float pushX = CORRIDOR_HALF_WIDTH - Mathf.Abs(pos.x);
            float pushY = MIN_SAFE_DISTANCE - pos.y;

            if (pushX < pushY)
            {
                pos.x = pos.x >= 0f
                    ? CORRIDOR_HALF_WIDTH
                    : -CORRIDOR_HALF_WIDTH;
            }
            else
            {
                pos.y = MIN_SAFE_DISTANCE;
            }
        }

        return pos;
    }

    /// <summary>Creates and applies a glowing transparent material to this orb.</summary>
    private void ApplyGlowMaterial()
    {
        MeshRenderer rend = GetComponent<MeshRenderer>();
        if (rend == null) return;

        Shader standardShader = Shader.Find("Standard");
        if (standardShader == null)
        {
            Debug.LogWarning("[GlowOrbSetup] Standard shader not found.", this);
            return;
        }

        Material mat = new Material(standardShader);

        // Fade mode — transparent with proper blending
        mat.SetFloat("_Mode", 2f);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;

        // Base color with transparency
        Color baseColor = new Color(glowColor.r, glowColor.g, glowColor.b, baseAlpha);
        mat.SetColor("_Color", baseColor);

        // Emission provides the glow
        mat.EnableKeyword("_EMISSION");
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        Color emissionColor = glowColor * emissionIntensity;
        mat.SetColor("_EmissionColor", emissionColor);

        // Smooth surface for clean glow
        mat.SetFloat("_Metallic", 0f);
        mat.SetFloat("_Glossiness", 0.9f);

        rend.material = mat;

        // Disable shadows for a clean floating light look
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = false;
    }
}
