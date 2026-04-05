using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TrialsOfKairos.UI
{
    /// <summary>
    /// Base class for every UI screen. Handles show/hide with combined fade + slide transitions,
    /// enforces a consistent lifecycle, sets controller focus on the first button,
    /// and routes the Cancel (B) input to a configurable back action.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIScreenBase : MonoBehaviour
    {
        [Header("Screen Base")]
        [SerializeField] protected float showDuration = 0.25f;
        [SerializeField] protected float hideDuration = 0.20f;

        [Header("Entrance Slide")]
        [Tooltip("Y pixels the screen starts below rest on entrance (0 = fade only).")]
        [SerializeField] private float slideEntranceY = 24f;

        [Header("Controller Navigation")]
        [Tooltip("The first button selected when this screen is shown via controller.")]
        [SerializeField] private KairosButton firstSelectedButton;

        protected CanvasGroup CanvasGroup { get; private set; }

        private RectTransform _screenRect;
        private Vector2       _restPosition;
        private Coroutine     _transitionCoroutine;
        private bool          _isVisible = false;

        protected virtual void Awake()
        {
            CanvasGroup = GetComponent<CanvasGroup>();
            _screenRect = GetComponent<RectTransform>();
            if (_screenRect != null) _restPosition = _screenRect.anchoredPosition;

            CanvasGroup.alpha          = 0f;
            CanvasGroup.interactable   = false;
            CanvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_isVisible && CanvasGroup.interactable && Input.GetButtonDown("Cancel"))
                OnCancelPressed();
        }

        /// <summary>Shows this screen with a combined fade + slide entrance.</summary>
        public void Show()
        {
            gameObject.SetActive(true);
            if (_transitionCoroutine != null) StopCoroutine(_transitionCoroutine);
            _transitionCoroutine = StartCoroutine(ShowRoutine());
        }

        /// <summary>Hides this screen with a combined fade + slide exit.</summary>
        public void Hide()
        {
            _isVisible = false;
            if (_transitionCoroutine != null) StopCoroutine(_transitionCoroutine);
            _transitionCoroutine = StartCoroutine(HideRoutine());
        }

        /// <summary>Shows immediately without any animation.</summary>
        public void ShowImmediate()
        {
            gameObject.SetActive(true);
            if (_screenRect != null) _screenRect.anchoredPosition = _restPosition;
            CanvasGroup.alpha          = 1f;
            CanvasGroup.interactable   = true;
            CanvasGroup.blocksRaycasts = true;
            _isVisible = true;
            SetControllerFocus();
            OnShown();
        }

        private IEnumerator ShowRoutine()
        {
            OnBeforeShow();
            CanvasGroup.interactable   = false;
            CanvasGroup.blocksRaycasts = false;

            // Slide in from below (parallel with fade)
            if (slideEntranceY != 0f && _screenRect != null)
            {
                _screenRect.anchoredPosition = _restPosition - new Vector2(0f, slideEntranceY);
                StartCoroutine(UIAnimationUtils.SlideRect(_screenRect, _restPosition, showDuration));
            }

            yield return UIAnimationUtils.FadeCanvasGroup(CanvasGroup, 1f, showDuration);

            CanvasGroup.interactable   = true;
            CanvasGroup.blocksRaycasts = true;
            _isVisible = true;
            SetControllerFocus();
            OnShown();
        }

        private IEnumerator HideRoutine()
        {
            CanvasGroup.interactable   = false;
            CanvasGroup.blocksRaycasts = false;
            OnBeforeHide();

            // Slide out downward (parallel with fade)
            if (slideEntranceY != 0f && _screenRect != null)
                StartCoroutine(UIAnimationUtils.SlideRect(
                    _screenRect,
                    _restPosition - new Vector2(0f, slideEntranceY * 0.5f),
                    hideDuration));

            yield return UIAnimationUtils.FadeCanvasGroup(CanvasGroup, 0f, hideDuration);

            // Reset position so the next entrance starts clean
            if (_screenRect != null) _screenRect.anchoredPosition = _restPosition;
            gameObject.SetActive(false);
            OnHidden();
        }

        protected virtual void SetControllerFocus()
        {
            if (EventSystem.current == null) return;
            if (firstSelectedButton != null) { firstSelectedButton.Select(); return; }
            KairosButton fallback = GetComponentInChildren<KairosButton>(includeInactive: false);
            fallback?.Select();
        }

        protected virtual void OnCancelPressed() { }
        protected virtual void OnBeforeShow()    { }
        protected virtual void OnShown()         { }
        protected virtual void OnBeforeHide()    { }
        protected virtual void OnHidden()        { }
    }
}
