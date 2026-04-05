using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TrialsOfKairos.UI
{
    /// <summary>
    /// Central broadcaster for time state changes.
    /// All UI components that respond to time state subscribe to OnTimeStateChanged.
    /// </summary>
    public class TimeStateUIManager : MonoBehaviour
    {
        public static TimeStateUIManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private UIColorPalette colorPalette;
        [SerializeField] private Image          vignetteImage;
        [SerializeField] private float          vignetteTransitionDuration = 0.5f;

        private TimeState.State _currentState = TimeState.State.Forward;
        private Coroutine       _vignetteCoroutine;

        /// <summary>Fired whenever the time state changes. Provides (newState, stateColor).</summary>
        public static event Action<TimeState.State, Color> OnTimeStateChanged;

        public TimeState.State    CurrentState => _currentState;
        public UIColorPalette     Palette      => colorPalette;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (colorPalette == null)
            {
#if UNITY_EDITOR
                colorPalette = UnityEditor.AssetDatabase.LoadAssetAtPath<UIColorPalette>(
                    "Assets/ScriptableObjects/UIColorPalette.asset");
                if (colorPalette == null)
                    colorPalette = UnityEditor.AssetDatabase.LoadAssetAtPath<UIColorPalette>(
                        "Assets/Scripts/UI/Data/UIColorPalette.asset");
#endif
            }
            ApplyStateImmediate(_currentState);
        }

        /// <summary>Call this from gameplay code whenever the player changes their stance.</summary>
        public void SetTimeState(TimeState.State newState)
        {
            if (_currentState == newState) return;
            _currentState = newState;

            Color stateColor = colorPalette != null
                ? colorPalette.GetStateColor(newState)
                : GetFallbackColor(newState);
            OnTimeStateChanged?.Invoke(newState, stateColor);

            if (_vignetteCoroutine != null) StopCoroutine(_vignetteCoroutine);
            _vignetteCoroutine = StartCoroutine(TransitionVignette(stateColor));
        }

        private void ApplyStateImmediate(TimeState.State state)
        {
            if (colorPalette == null)
            {
                OnTimeStateChanged?.Invoke(state, GetFallbackColor(state));
                return;
            }
            Color c = colorPalette.GetStateColor(state);
            OnTimeStateChanged?.Invoke(state, c);
            if (vignetteImage == null) return;
            c.a = colorPalette.vignetteAlpha;
            vignetteImage.color = c;
        }

        private static Color GetFallbackColor(TimeState.State state) => state switch
        {
            TimeState.State.Forward => new Color(0.961f, 0.784f, 0.259f, 1f),
            TimeState.State.Frozen  => new Color(0.910f, 0.918f, 0.965f, 1f),
            TimeState.State.Reverse => new Color(0.353f, 0.706f, 0.941f, 1f),
            _                       => new Color(0.961f, 0.784f, 0.259f, 1f),
        };

        private IEnumerator TransitionVignette(Color targetStateColor)
        {
            if (vignetteImage == null || colorPalette == null) yield break;

            Color target = targetStateColor;
            target.a = colorPalette.vignetteAlpha;

            yield return UIAnimationUtils.LerpImageColor(vignetteImage, target, vignetteTransitionDuration);
        }

        [ContextMenu("Preview Forward")]  private void PreviewForward()  => SetTimeState(TimeState.State.Forward);
        [ContextMenu("Preview Frozen")]   private void PreviewFrozen()   => SetTimeState(TimeState.State.Frozen);
        [ContextMenu("Preview Reverse")]  private void PreviewReverse()  => SetTimeState(TimeState.State.Reverse);
    }
}
