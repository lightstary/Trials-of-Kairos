using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TrialsOfKairos.UI
{
    /// <summary>
    /// Continuously rotates a UI RectTransform to create the ambient orbit ring effect.
    /// Also responds to time state changes to tint the ring color.
    /// </summary>
    public class OrbitRing : MonoBehaviour
    {
        [Header("Rotation")]
        [SerializeField] private float rotationSpeed       = 6f;  // degrees per second
        [SerializeField] private bool  reverseOnReverse    = true; // flip when time reverses

        [Header("Color")]
        [SerializeField] private Image ringImage;
        [SerializeField] private bool  tintToState         = false;
        [SerializeField] private float colorTransitionDuration = 0.5f;

        private RectTransform _rect;
        private Coroutine     _colorCoroutine;
        private float         _currentSpeed;

        private void Awake()
        {
            _rect         = GetComponent<RectTransform>();
            _currentSpeed = rotationSpeed;
        }

        private void OnEnable()
        {
            TimeStateUIManager.OnTimeStateChanged += HandleStateChange;
        }

        private void OnDisable()
        {
            TimeStateUIManager.OnTimeStateChanged -= HandleStateChange;
        }

        private void Update()
        {
            _rect.Rotate(0f, 0f, _currentSpeed * Time.unscaledDeltaTime);
        }

        private void HandleStateChange(TimeState.State state, Color color)
        {
            if (reverseOnReverse)
                _currentSpeed = state == TimeState.State.Reverse ? -Mathf.Abs(rotationSpeed) : Mathf.Abs(rotationSpeed);

            if (tintToState && ringImage != null)
            {
                if (_colorCoroutine != null) StopCoroutine(_colorCoroutine);
                _colorCoroutine = StartCoroutine(UIAnimationUtils.LerpImageColor(ringImage, color, colorTransitionDuration));
            }
        }
    }
}
