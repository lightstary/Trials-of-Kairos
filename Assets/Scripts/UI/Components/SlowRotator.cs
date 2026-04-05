using UnityEngine;

namespace TrialsOfKairos.UI
{
    /// <summary>
    /// Continuously rotates a RectTransform at a constant speed (degrees/sec).
    /// Reverses direction on TimeState.Reverse if opted in.
    /// </summary>
    public class SlowRotator : MonoBehaviour
    {
        [Header("Rotation")]
        [SerializeField] private float degreesPerSecond  = 8f;
        [SerializeField] private bool  reverseOnReverse  = false;
        [SerializeField] private bool  useUnscaledTime   = true;

        private RectTransform _rect;
        private float         _sign = 1f;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            if (reverseOnReverse)
                TimeStateUIManager.OnTimeStateChanged += HandleStateChange;
        }

        private void OnDisable()
        {
            if (reverseOnReverse)
                TimeStateUIManager.OnTimeStateChanged -= HandleStateChange;
        }

        private void Update()
        {
            if (_rect == null) return;
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            _rect.Rotate(0f, 0f, _sign * degreesPerSecond * dt);
        }

        private void HandleStateChange(TimeState.State state, Color _)
        {
            _sign = state == TimeState.State.Reverse ? -1f : 1f;
        }
    }
}
