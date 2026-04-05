using UnityEngine;
using UnityEngine.UI;

namespace TrialsOfKairos.UI
{
    /// <summary>
    /// Continuously pulses the alpha of an Image between two values,
    /// creating an ambient "glow breathing" effect.
    /// Optionally syncs to the current time state color.
    /// </summary>
    public class GlowBreather : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image targetImage;

        [Header("Breathing")]
        [SerializeField] private float minAlpha    = 0.15f;
        [SerializeField] private float maxAlpha    = 0.55f;
        [SerializeField] private float frequency   = 0.8f;  // full cycles per second
        [SerializeField] private float phaseOffset = 0f;    // stagger multiple instances

        [Header("Color")]
        [SerializeField] private bool  tintToTimeState = false;

        private Color _baseColor;

        private void Awake()
        {
            if (targetImage == null) targetImage = GetComponent<Image>();
            if (targetImage != null) _baseColor  = targetImage.color;
        }

        private void OnEnable()
        {
            if (tintToTimeState)
                TimeStateUIManager.OnTimeStateChanged += HandleStateChange;
        }

        private void OnDisable()
        {
            if (tintToTimeState)
                TimeStateUIManager.OnTimeStateChanged -= HandleStateChange;
        }

        private void Update()
        {
            if (targetImage == null) return;
            float t     = (Mathf.Sin((Time.unscaledTime * frequency + phaseOffset) * Mathf.PI * 2f) + 1f) * 0.5f;
            Color c     = _baseColor;
            c.a         = Mathf.Lerp(minAlpha, maxAlpha, t);
            targetImage.color = c;
        }

        private void HandleStateChange(TimeState.State state, Color color)
        {
            _baseColor   = color;
            _baseColor.a = 1f;
        }

        /// <summary>Override the base color from code.</summary>
        public void SetBaseColor(Color color)
        {
            _baseColor = color;
            if (targetImage != null) _baseColor.a = targetImage.color.a;
        }
    }
}
