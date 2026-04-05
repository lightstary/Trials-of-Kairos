using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TrialsOfKairos.UI
{
    /// <summary>
    /// DISABLED — dev overlay removed.
    /// This class is kept as a stub to prevent missing-script errors on any scene
    /// GameObjects that still hold the component reference. Remove the component.
    /// </summary>
    public class UITestHarness : MonoBehaviour
    {
        // Intentionally empty — overlay creation has been removed.
        // The UITestHarness component can be safely removed from UIRoot.
        private void Awake() { Destroy(this); }
    }
}
