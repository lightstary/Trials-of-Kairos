using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TrialsOfKairos.UI
{
    /// <summary>
    /// Drives the red vignette on the Game Over screen:
    /// fade in, then loop-pulse indefinitely until Stop() is called.
    /// </summary>
    public class RedVignettePulser : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image vignetteImage;

        [Header("Intro")]
        [SerializeField] private float fadeinDuration  = 0.5f;
        [SerializeField] private float introAlpha      = 0.45f;

        [Header("Pulse Loop")]
        [SerializeField] private float pulseMinAlpha   = 0.25f;
        [SerializeField] private float pulseMaxAlpha   = 0.65f;
        [SerializeField] private float pulseFrequency  = 0.6f;  // cycles/sec

        private bool      _pulsing     = false;
        private Coroutine _introRoutine;

        private void Awake()
        {
            if (vignetteImage == null) vignetteImage = GetComponent<Image>();
            SetAlpha(0f);
        }

        /// <summary>Fade in then begin pulsing.</summary>
        public void StartPulse()
        {
            if (_introRoutine != null) StopCoroutine(_introRoutine);
            _introRoutine = StartCoroutine(IntroAndLoop());
        }

        /// <summary>Immediately stop pulsing and fade the vignette out.</summary>
        public void Stop()
        {
            _pulsing = false;
            if (_introRoutine != null) StopCoroutine(_introRoutine);
            StartCoroutine(UIAnimationUtils.LerpImageColor(vignetteImage,
                new Color(vignetteImage.color.r, vignetteImage.color.g, vignetteImage.color.b, 0f), 0.3f));
        }

        private IEnumerator IntroAndLoop()
        {
            // Fade in
            yield return UIAnimationUtils.LerpImageColor(vignetteImage,
                new Color(0.898f, 0.196f, 0.106f, introAlpha), fadeinDuration);

            _pulsing = true;
            while (_pulsing)
            {
                float t   = (Mathf.Sin(Time.unscaledTime * pulseFrequency * Mathf.PI * 2f) + 1f) * 0.5f;
                float a   = Mathf.Lerp(pulseMinAlpha, pulseMaxAlpha, t);
                SetAlpha(a);
                yield return null;
            }
        }

        private void SetAlpha(float a)
        {
            if (vignetteImage == null) return;
            Color c = vignetteImage.color;
            c.r = 0.898f; c.g = 0.196f; c.b = 0.106f;
            c.a = a;
            vignetteImage.color = c;
        }
    }
}
