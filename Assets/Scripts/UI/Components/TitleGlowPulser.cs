using System.Collections;
using UnityEngine;
using TMPro;

namespace TrialsOfKairos.UI
{
    /// <summary>
    /// Gives a TextMeshProUGUI title a subtle, rhythmic glow pulse
    /// by animating its vertex color alpha between two values.
    /// Designed for the Main Menu title — runs unscaled, loops forever.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class TitleGlowPulser : MonoBehaviour
    {
        [Header("Pulse")]
        [SerializeField] private float minAlpha    = 0.78f;
        [SerializeField] private float maxAlpha    = 1.00f;
        [SerializeField] private float frequency   = 0.45f;  // cycles/sec — slow, ceremonial

        [Header("Color")]
        [SerializeField] private Color glowTint = new Color(1f, 0.843f, 0f, 1f);  // gold

        private TextMeshProUGUI _label;
        private Coroutine       _loop;

        private void Awake()
        {
            _label = GetComponent<TextMeshProUGUI>();
        }

        private void OnEnable()
        {
            if (_loop != null) StopCoroutine(_loop);
            _loop = StartCoroutine(PulseLoop());
        }

        private void OnDisable()
        {
            if (_loop != null) { StopCoroutine(_loop); _loop = null; }
        }

        private IEnumerator PulseLoop()
        {
            while (true)
            {
                float t     = (Mathf.Sin(Time.unscaledTime * frequency * Mathf.PI * 2f) + 1f) * 0.5f;
                float alpha = Mathf.Lerp(minAlpha, maxAlpha, t);
                Color c     = glowTint;
                c.a         = alpha;
                _label.color = c;
                yield return null;
            }
        }
    }
}
