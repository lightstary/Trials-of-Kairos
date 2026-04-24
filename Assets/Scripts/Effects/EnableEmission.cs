using UnityEngine;

/// <summary>
/// Enables the _EMISSION keyword on all materials attached to this renderer at startup.
/// Required for Standard shader emission to actually work.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class EnableEmission : MonoBehaviour
{
    void Start()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend == null) return;

        foreach (Material mat in rend.materials)
        {
            if (mat == null) continue;
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            DynamicGI.SetEmissive(rend, mat.GetColor("_EmissionColor"));
        }
    }
}
