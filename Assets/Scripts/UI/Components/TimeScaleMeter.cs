using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TrialsOfKairos.UI
{
    /// <summary>
    /// Bottom-right HUD: vertical bar showing TimeScale fill.
    /// Danger threshold marker pulses red when exceeded.
    /// Fill color matches current time state.
    /// </summary>
    public class TimeScaleMeter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image           fillImage;
        [SerializeField] private RectTransform   dangerPointer;
        [SerializeField] private Image           dangerPointerImage;
        [SerializeField] private TextMeshProUGUI valueLabel;
        [SerializeField] private RectTransform   meterTrackRect;

        [Header("Settings")]
        [SerializeField] private float dangerThresholdNormalized = 0.78f;
        [SerializeField] private float fillLerpSpeed             = 6f;

        [Header("Danger Feedback")]
        [SerializeField] private float  pulseFrequency = 12f;
        [SerializeField] private float  shakeThreshold = 0.95f;

        private static readonly Color DangerColor = new Color(0.898f, 0.196f, 0.106f, 1f);

        private float     _targetFill    = 0.35f;
        private float     _currentFill   = 0.35f;
        private Color     _stateColor    = new Color(0.961f, 0.784f, 0.259f, 1f);
        private bool      _inDanger      = false;
        private Coroutine _shakeCoroutine;

        private void OnEnable()
        {
            TimeStateUIManager.OnTimeStateChanged += HandleStateChange;
        }

        private void OnDisable()
        {
            TimeStateUIManager.OnTimeStateChanged -= HandleStateChange;
        }

        private void Start()
        {
            PositionDangerPointer();
        }

        private void Update()
        {
            // Smooth fill
            _currentFill = Mathf.Lerp(_currentFill, _targetFill, fillLerpSpeed * Time.unscaledDeltaTime);
            if (fillImage != null) fillImage.fillAmount = _currentFill;

            // Danger state
            bool nowDanger = _currentFill >= dangerThresholdNormalized;
            if (nowDanger != _inDanger)
            {
                _inDanger = nowDanger;
                OnDangerStateChanged(_inDanger);
            }

            // Danger pulsing alpha
            if (_inDanger && dangerPointerImage != null)
            {
                float alpha = Mathf.Abs(Mathf.Sin(Time.unscaledTime * pulseFrequency));
                Color c = dangerPointerImage.color;
                c.a = Mathf.Lerp(0.4f, 1f, alpha);
                dangerPointerImage.color = c;
            }

            // Fill color: red when overrun
            if (fillImage != null)
            {
                Color target = _inDanger ? DangerColor : _stateColor;
                fillImage.color = Color.Lerp(fillImage.color, target, 10f * Time.unscaledDeltaTime);
            }

            // Value label
            if (valueLabel != null)
                valueLabel.text = $"{Mathf.RoundToInt(_currentFill * 100f)}%";
        }

        /// <summary>Set the target TimeScale fill (0–1).</summary>
        public void SetFill(float normalizedValue)
        {
            _targetFill = Mathf.Clamp01(normalizedValue);

            if (_targetFill >= shakeThreshold)
            {
                if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
                if (meterTrackRect != null)
                    _shakeCoroutine = StartCoroutine(UIAnimationUtils.ShakeRect(meterTrackRect, 3f, 0.3f));
            }
        }

        /// <summary>Update the danger threshold position (0–1, normalized).</summary>
        public void SetDangerThreshold(float normalizedThreshold)
        {
            dangerThresholdNormalized = Mathf.Clamp01(normalizedThreshold);
            PositionDangerPointer();
        }

        private void HandleStateChange(TimeState.State state, Color color)
        {
            _stateColor = color;
        }

        private void OnDangerStateChanged(bool isDanger)
        {
            if (isDanger)
            {
                ScreenTransitionManager.Instance?.FlashRed(0.2f);
                if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
                if (meterTrackRect != null)
                    _shakeCoroutine = StartCoroutine(UIAnimationUtils.ShakeRect(meterTrackRect, 5f, 0.3f));
            }

            if (dangerPointerImage != null)
            {
                dangerPointerImage.color = isDanger ? DangerColor : new Color(1f, 0f, 0f, 0.7f);
            }
        }

        private void PositionDangerPointer()
        {
            if (dangerPointer == null || meterTrackRect == null) return;
            float trackHeight = meterTrackRect.rect.height;
            dangerPointer.anchoredPosition = new Vector2(0f, dangerThresholdNormalized * trackHeight - trackHeight * 0.5f);
        }
    }
}
