using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TrialsOfKairos.UI
{
    /// <summary>
    /// Top-left HUD component showing the current time stance.
    /// The hourglass icon physically rotates/flips on state change.
    /// The orbit ring and label tint to the state color.
    /// </summary>
    public class TimeStateIndicator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform   hourglassRect;
        [SerializeField] private Image           hourglassImage;
        [SerializeField] private Image           orbitRingImage;
        [SerializeField] private TextMeshProUGUI stateLabel;
        [SerializeField] private Image           panelBorderImage;

        [Header("State Sprites")]
        [SerializeField] private Sprite spriteForward;
        [SerializeField] private Sprite spriteFrozen;
        [SerializeField] private Sprite spriteReverse;

        [Header("Rotation Angles (Z)")]
        [SerializeField] private float angleForward =   0f;
        [SerializeField] private float angleFrozen  =  90f;
        [SerializeField] private float angleReverse = 180f;

        [Header("Transition")]
        [SerializeField] private float rotateDuration = 0.30f;
        [SerializeField] private float colorDuration  = 0.25f;

        private static readonly string[] StateLabels = { "FORWARD", "FROZEN", "REVERSED" };

        private Coroutine _rotateCoroutine;
        private Coroutine _colorCoroutine;

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
            if (TimeStateUIManager.Instance != null)
                HandleStateChange(TimeStateUIManager.Instance.CurrentState,
                                  TimeStateUIManager.Instance.Palette.GetStateColor(TimeStateUIManager.Instance.CurrentState));
        }

        private void HandleStateChange(TimeState.State state, Color color)
        {
            // Update label
            if (stateLabel != null) stateLabel.text = StateLabels[(int)state];

            // Update hourglass sprite
            if (hourglassImage != null)
            {
                hourglassImage.sprite = state switch
                {
                    TimeState.State.Forward => spriteForward,
                    TimeState.State.Frozen  => spriteFrozen,
                    TimeState.State.Reverse => spriteReverse,
                    _                       => spriteForward
                };
            }

            // Rotate the hourglass icon
            float targetAngle = state switch
            {
                TimeState.State.Forward => angleForward,
                TimeState.State.Frozen  => angleFrozen,
                TimeState.State.Reverse => angleReverse,
                _                       => angleForward
            };

            if (_rotateCoroutine != null) StopCoroutine(_rotateCoroutine);
            if (hourglassRect != null)
                _rotateCoroutine = StartCoroutine(UIAnimationUtils.RotateTo(hourglassRect, targetAngle, rotateDuration));

            // Transition colors
            if (_colorCoroutine != null) StopCoroutine(_colorCoroutine);
            _colorCoroutine = StartCoroutine(TransitionColors(color));
        }

        private IEnumerator TransitionColors(Color stateColor)
        {
            Color ringTarget   = stateColor;
            Color labelTarget  = stateColor;
            Color borderTarget = stateColor;
            borderTarget.a     = 0.5f;
            ringTarget.a       = 0.7f;

            float elapsed = 0f;
            Color ringStart   = orbitRingImage   != null ? orbitRingImage.color   : ringTarget;
            Color labelStart  = stateLabel       != null ? stateLabel.color       : labelTarget;
            Color borderStart = panelBorderImage != null ? panelBorderImage.color : borderTarget;

            while (elapsed < colorDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t  = elapsed / colorDuration;
                if (orbitRingImage   != null) orbitRingImage.color   = Color.Lerp(ringStart,   ringTarget,   t);
                if (stateLabel       != null) stateLabel.color       = Color.Lerp(labelStart,  labelTarget,  t);
                if (panelBorderImage != null) panelBorderImage.color = Color.Lerp(borderStart, borderTarget, t);
                yield return null;
            }

            if (orbitRingImage   != null) orbitRingImage.color   = ringTarget;
            if (stateLabel       != null) stateLabel.color       = labelTarget;
            if (panelBorderImage != null) panelBorderImage.color = borderTarget;
        }
    }
}
