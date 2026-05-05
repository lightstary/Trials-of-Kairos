using UnityEngine;

// Enables _EMISSION keyword on all materials at startup (required for Standard shader).
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
