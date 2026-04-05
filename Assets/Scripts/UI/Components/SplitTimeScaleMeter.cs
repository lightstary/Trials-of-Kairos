using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TrialsOfKairos.UI
{
    /// <summary>
    /// Top-center HUD element: the primary time-scale readout.
    /// Split horizontal bar — left half = reverse zone, right half = forward zone,
    /// centre needle = current time progression.
    /// Glowing edges reflect the active time state.
    /// Boss encounters add a red danger zone overlay.
    /// </summary>
    public class SplitTimeScaleMeter : MonoBehaviour
    {
        // ── References ─────────────────────────────────────────────────────────────
        [Header("Bar Halves")]
        [SerializeField] private Image   leftFill;           // fills right-to-left (reverse zone)
        [SerializeField] private Image   rightFill;          // fills left-to-right (forward zone)
        [SerializeField] private Image   centerDivider;      // 2 px vertical line

        [Header("Needle")]
        [SerializeField] private RectTransform needleRect;   // slides across full bar width
        [SerializeField] private Image         needleImage;

        [Header("Edge Glows")]
        [SerializeField] private Image   leftGlow;           // left-edge glow panel
        [SerializeField] private Image   rightGlow;          // right-edge glow panel

        [Header("Labels")]
        [SerializeField] private TextMeshProUGUI centerLabel; // e.g. "0.74×" or "FROZEN"
        [SerializeField] private TextMeshProUGUI leftTag;     // "← REVERSE"
        [SerializeField] private TextMeshProUGUI rightTag;    // "FORWARD →"

        [Header("Container")]
        [SerializeField] private RectTransform barContainer;

        [Header("Danger Zone (Boss)")]
        [SerializeField] private Image dangerOverlay;        // red tint shown during boss danger
        [SerializeField] private float dangerThreshold = 0.80f;

        // ── Settings ────────────────────────────────────────────────────────────────
        [Header("Animation")]
        [SerializeField] private float fillLerpSpeed       = 7f;
        [SerializeField] private float glowPulseFrequency  = 0.9f;
        [SerializeField] private float glowMinAlpha        = 0.08f;
        [SerializeField] private float glowMaxAlpha        = 0.55f;

        // ── State ────────────────────────────────────────────────────────────────────
        private float     _targetValue   = 0.5f;   // 0 = full reverse, 0.5 = neutral, 1 = full forward
        private float     _currentValue  = 0.5f;
        private Color     _stateColor    = new Color(0.961f, 0.784f, 0.259f, 1f);
        private bool      _inDanger      = false;
        private bool      _bossMode      = false;
        private Coroutine _shakeRoutine;
        private Coroutine _dangerRoutine;

        private static readonly Color NeutralFill   = new Color(0.910f, 0.918f, 0.965f, 0.18f);
        private static readonly Color DangerColor   = new Color(0.898f, 0.196f, 0.106f, 1f);

        // ── Lifecycle ────────────────────────────────────────────────────────────────
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
            if (dangerOverlay != null)
            {
                Color c = dangerOverlay.color; c.a = 0f;
                dangerOverlay.color = c;
            }

            // Replace any non-Cinzel arrow glyphs with ASCII safe equivalents
            if (leftTag   != null && leftTag.text.Contains("\u2190"))   leftTag.text   = "REVERSE";
            if (rightTag  != null && rightTag.text.Contains("\u2192"))  rightTag.text  = "FORWARD";

            ApplyStateColors(_stateColor);
        }

        private void Update()
        {
            _currentValue = Mathf.Lerp(_currentValue, _targetValue, fillLerpSpeed * Time.unscaledDeltaTime);
            ApplyFills(_currentValue);
            ApplyNeedle(_currentValue);
            ApplyGlowBreath();
            ApplyCenterLabel(_currentValue);
            CheckDanger(_currentValue);
        }

        // ── Public API ────────────────────────────────────────────────────────────────
        /// <summary>Set the meter value.
        /// 0 = fully reversed, 0.5 = neutral/frozen, 1 = fully forward.</summary>
        public void SetValue(float normalizedValue)
        {
            _targetValue = Mathf.Clamp01(normalizedValue);

            if (_targetValue >= dangerThreshold && _bossMode)
                TriggerDangerShake();
        }

        /// <summary>Enable or disable boss danger-zone mode (adds red overlay on threshold breach).</summary>
        public void SetBossMode(bool enabled, float threshold = 0.80f)
        {
            _bossMode      = enabled;
            dangerThreshold = threshold;
            if (!enabled) ClearDanger();
        }

        // ── Private ───────────────────────────────────────────────────────────────────
        private void ApplyFills(float v)
        {
            // Left half fills from center toward left as value drops below 0.5
            if (leftFill  != null) leftFill.fillAmount  = Mathf.Clamp01((0.5f - v) * 2f);
            // Right half fills from center toward right as value rises above 0.5
            if (rightFill != null) rightFill.fillAmount = Mathf.Clamp01((v - 0.5f) * 2f);
        }

        private void ApplyNeedle(float v)
        {
            if (needleRect == null || barContainer == null) return;
            float width = barContainer.rect.width;
            float x     = Mathf.Lerp(-width * 0.5f, width * 0.5f, v);
            needleRect.anchoredPosition = new Vector2(x, 0f);
        }

        private void ApplyGlowBreath()
        {
            float t     = (Mathf.Sin(Time.unscaledTime * glowPulseFrequency * Mathf.PI * 2f) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(glowMinAlpha, glowMaxAlpha, t);

            if (leftGlow  != null)
            {
                Color c = _stateColor; c.a = alpha * Mathf.Clamp01((0.5f - _currentValue) * 2f + 0.1f);
                leftGlow.color = c;
            }
            if (rightGlow != null)
            {
                Color c = _stateColor; c.a = alpha * Mathf.Clamp01((_currentValue - 0.5f) * 2f + 0.1f);
                rightGlow.color = c;
            }
        }

        private void ApplyCenterLabel(float v)
        {
            if (centerLabel == null) return;
            if (Mathf.Abs(v - 0.5f) < 0.03f)
            {
                centerLabel.text = "FROZEN";
            }
            else
            {
                float display = (v - 0.5f) * 2f;
                centerLabel.text = display >= 0f
                    ? $"+{display:0.00}×"
                    : $"{display:0.00}×";
            }
        }

        private void CheckDanger(float v)
        {
            bool nowDanger = _bossMode && (v >= dangerThreshold || v <= (1f - dangerThreshold));
            if (nowDanger == _inDanger) return;
            _inDanger = nowDanger;

            if (_inDanger)
            {
                if (_dangerRoutine != null) StopCoroutine(_dangerRoutine);
                if (dangerOverlay  != null)
                    _dangerRoutine = StartCoroutine(
                        UIAnimationUtils.PulseGlow(dangerOverlay, DangerColor, 1.4f, 0f, 0.4f));
            }
            else
            {
                ClearDanger();
            }
        }

        private void ClearDanger()
        {
            _inDanger = false;
            if (_dangerRoutine != null) { StopCoroutine(_dangerRoutine); _dangerRoutine = null; }
            if (dangerOverlay  != null)
            {
                Color c = dangerOverlay.color; c.a = 0f;
                dangerOverlay.color = c;
            }
        }

        private void TriggerDangerShake()
        {
            ScreenTransitionManager.Instance?.FlashRed(0.2f);
            if (_shakeRoutine != null) StopCoroutine(_shakeRoutine);
            if (barContainer  != null)
                _shakeRoutine = StartCoroutine(UIAnimationUtils.ShakeRect(barContainer, 4f, 0.25f));
        }

        private void ApplyStateColors(Color color)
        {
            _stateColor = color;
            if (leftFill       != null) leftFill.color       = color;
            if (rightFill      != null) rightFill.color      = color;
            if (needleImage    != null) needleImage.color    = color;
            if (centerDivider  != null)
            {
                Color dc = color; dc.a = 0.55f;
                centerDivider.color = dc;
            }
            if (leftTag        != null) leftTag.color        = new Color(color.r, color.g, color.b, 0.55f);
            if (rightTag       != null) rightTag.color       = new Color(color.r, color.g, color.b, 0.55f);
            if (centerLabel    != null) centerLabel.color    = color;
        }

        private void HandleStateChange(TimeState.State state, Color color)
        {
            ApplyStateColors(color);
        }
    }
}
