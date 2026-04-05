using UnityEngine;

namespace TrialsOfKairos.UI
{
    /// <summary>
    /// ScriptableObject holding per-TimeState colors and vignette settings.
    /// </summary>
    [CreateAssetMenu(fileName = "UIColorPalette", menuName = "Trials of Kairos/UI Color Palette")]
    public class UIColorPalette : ScriptableObject
    {
        [Header("Time State Colors")]
        public Color forwardColor = new Color(0.961f, 0.784f, 0.259f, 1f);
        public Color frozenColor  = new Color(0.910f, 0.918f, 0.965f, 1f);
        public Color reverseColor = new Color(0.353f, 0.706f, 0.941f, 1f);

        [Header("Vignette")]
        [Range(0f, 1f)]
        public float vignetteAlpha = 0.06f;

        /// <summary>Returns the color for the given time state.</summary>
        public Color GetStateColor(TimeState.State state) => state switch
        {
            TimeState.State.Forward => forwardColor,
            TimeState.State.Frozen  => frozenColor,
            TimeState.State.Reverse => reverseColor,
            _                       => forwardColor,
        };
    }
}
