using UnityEngine;

/// <summary>
/// Attach to any GameObject with a ParticleSystem (e.g. "Water Particles").
/// At runtime, creates an invisible depth-writing quad behind the particles
/// so opaque objects (like the player) don't show through.
/// Requires the "DepthMask" material at Resources or a direct reference.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class WaterDepthMask : MonoBehaviour
{
    [Tooltip("Width of the depth mask quad.")]
    [SerializeField] private float maskWidth = 6f;

    [Tooltip("Height of the depth mask quad.")]
    [SerializeField] private float maskHeight = 16f;

    [Tooltip("Offset from the particle system center.")]
    [SerializeField] private Vector3 maskOffset = new Vector3(0f, 5f, 0f);

    [Tooltip("Material that writes depth only (Custom/DepthMask shader).")]
    [SerializeField] private Material depthMaskMaterial;

    private GameObject _maskQuad;

    void Start()
    {
        CreateMask();
    }

    /// <summary>Creates the depth-writing quad as a child of this transform.</summary>
    private void CreateMask()
    {
        if (depthMaskMaterial == null)
        {
            depthMaskMaterial = FindDepthMaskMaterial();
            if (depthMaskMaterial == null)
            {
                Debug.LogWarning($"[WaterDepthMask] No DepthMask material found on {name}. Skipping.");
                return;
            }
        }

        _maskQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        _maskQuad.name = "DepthMask";
        _maskQuad.transform.SetParent(transform, false);
        _maskQuad.transform.localPosition = maskOffset;
        _maskQuad.transform.localScale = new Vector3(maskWidth, maskHeight, 1f);

        // Face the camera — billboarded by LookAt in LateUpdate
        Renderer rend = _maskQuad.GetComponent<Renderer>();
        rend.material = depthMaskMaterial;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = false;

        // Remove the collider that CreatePrimitive adds
        Collider col = _maskQuad.GetComponent<Collider>();
        if (col != null) Destroy(col);
    }

    void LateUpdate()
    {
        if (_maskQuad == null) return;

        // Billboard: face the main camera so the mask always blocks the player
        Camera cam = Camera.main;
        if (cam != null)
        {
            _maskQuad.transform.rotation = Quaternion.LookRotation(
                _maskQuad.transform.position - cam.transform.position, Vector3.up);
        }
    }

    /// <summary>Attempts to find the DepthMask material by searching loaded materials.</summary>
    private static Material FindDepthMaskMaterial()
    {
        Shader shader = Shader.Find("Custom/DepthMask");
        if (shader != null)
        {
            return new Material(shader);
        }
        return null;
    }
}
