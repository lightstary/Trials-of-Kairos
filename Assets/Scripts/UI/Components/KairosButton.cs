using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace TrialsOfKairos.UI
{
    /// <summary>
    /// Custom styled button. Hover (mouse) and Select (controller) both trigger
    /// the same visible highlight:
    ///   - bright gold border
    ///   - brighter label text (full white)
    ///   - 5% scale-up
    ///   - animated glow pulse on the border
    ///
    /// The selected state is unmistakably obvious from a distance.
    /// Requires a Selectable so EventSystem routes D-Pad/stick navigation.
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    public class KairosButton : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerClickHandler, IPointerDownHandler, IPointerUpHandler,
        ISelectHandler, IDeselectHandler, ISubmitHandler
    {
        [Header("Visual References")]
        [SerializeField] private Image           backgroundImage;
        [SerializeField] private Image           borderImage;
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private RectTransform   rectTransform;

        [Header("Resting Colors")]
        [SerializeField] private Color restingBgColor     = new Color(0.059f, 0.102f, 0.188f, 0.65f);
        [SerializeField] private Color restingBorderColor = new Color(0.55f,  0.75f,  1.0f,   0.18f);
        [SerializeField] private Color restingTextColor   = new Color(0.85f,  0.90f,  0.97f,  0.75f);

        [Header("Focused / Selected Colors")]
        [SerializeField] private Color focusBgColor     = new Color(0.059f, 0.102f, 0.188f, 0.90f);
        [SerializeField] private Color focusBorderColor = new Color(0.50f,  0.80f,  1.0f,   1f);
        [SerializeField] private Color focusTextColor   = new Color(1f,     1f,     1f,     1f);

        [Header("Focus Pulse (border glow)")]
        [SerializeField] private float focusPulseFrequency = 1.8f;
        [SerializeField] private float focusPulseMinAlpha  = 0.6f;
        [SerializeField] private float focusPulseMaxAlpha  = 1.0f;

        [Header("Animation")]
        [SerializeField] private float transitionDuration = 0.10f;
        [SerializeField] private float focusScale         = 1.05f;
        [SerializeField] private float pressScale         = 0.96f;

        public UnityEvent OnClicked = new UnityEvent();

        private bool      _isHighlighted = false;
        private bool      _useTimeStateColor = true; // set false to lock colors to ice-blue
        private Coroutine _transitionCoroutine;
        private Coroutine _pulseCoroutine;

        // ── Lifecycle ─────────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();

            // Resting: dark navy + ice-blue border, dim text
            restingBgColor     = new Color(0.059f, 0.102f, 0.188f, 0.65f);
            restingBorderColor = new Color(0.55f,  0.75f,  1.0f,   0.18f);
            restingTextColor   = new Color(0.85f,  0.90f,  0.97f,  0.75f);

            // Focus/hover: warm gold border, gold-tinted background, bright white text
            focusBorderColor   = new Color(1.0f,  0.82f, 0.15f, 1.0f);
            focusBgColor       = new Color(0.18f, 0.12f, 0.02f, 0.92f);
            focusTextColor     = new Color(1.0f,  0.92f, 0.60f, 1.0f);

            Selectable sel = GetComponent<Selectable>();
            if (sel != null)
            {
                Navigation nav = sel.navigation;
                if (nav.mode == Navigation.Mode.Automatic)
                {
                    nav.mode       = Navigation.Mode.Vertical;
                    nav.wrapAround = true;
                    sel.navigation = nav;
                }
            }

            ApplyResting(instant: true);
        }

        private void OnEnable()
        {
            TimeStateUIManager.OnTimeStateChanged += OnTimeStateChanged;
        }

        private void OnDisable()
        {
            TimeStateUIManager.OnTimeStateChanged -= OnTimeStateChanged;
            _isHighlighted = false;
            StopAllHighlightCoroutines();
        }

        private void OnTimeStateChanged(TimeState.State state, Color color)
        {
            if (!_useTimeStateColor) return;
            focusBorderColor = color;
            if (_isHighlighted) StartPulse();
        }

        // ── Pointer (mouse) ────────────────────────────────────────────────────────────
        public void OnPointerEnter(PointerEventData _) => EnterHighlight();
        public void OnPointerExit(PointerEventData _)  => ExitHighlight();

        public void OnPointerDown(PointerEventData _)
        {
            if (rectTransform != null) rectTransform.localScale = Vector3.one * pressScale;
        }

        public void OnPointerUp(PointerEventData _)
        {
            if (rectTransform != null) rectTransform.localScale = _isHighlighted
                ? Vector3.one * focusScale
                : Vector3.one;
        }

        public void OnPointerClick(PointerEventData _) => OnClicked.Invoke();

        // ── Controller ────────────────────────────────────────────────────────────────
        public void OnSelect(BaseEventData _)   => EnterHighlight();
        public void OnDeselect(BaseEventData _) => ExitHighlight();

        public void OnSubmit(BaseEventData _)
        {
            StartCoroutine(SubmitFlash());
            OnClicked.Invoke();
        }

        // ── Shared highlight logic ─────────────────────────────────────────────────────
        private void EnterHighlight()
        {
            _isHighlighted = true;
            StopAllHighlightCoroutines();
            _transitionCoroutine = StartCoroutine(TransitionTo(
                focusBgColor, focusBorderColor, focusTextColor, transitionDuration));
            // Scale up
            if (rectTransform != null)
                StartCoroutine(ScaleTo(focusScale, transitionDuration));
            // Start pulsing border
            StartPulse();
        }

        private void ExitHighlight()
        {
            _isHighlighted = false;
            StopAllHighlightCoroutines();
            _transitionCoroutine = StartCoroutine(TransitionTo(
                restingBgColor, restingBorderColor, restingTextColor, transitionDuration));
            if (rectTransform != null)
                StartCoroutine(ScaleTo(1f, transitionDuration));
        }

        private void StartPulse()
        {
            if (_pulseCoroutine != null) StopCoroutine(_pulseCoroutine);
            if (borderImage != null)
                _pulseCoroutine = StartCoroutine(PulseBorder());
        }

        private void StopAllHighlightCoroutines()
        {
            if (_transitionCoroutine != null) { StopCoroutine(_transitionCoroutine); _transitionCoroutine = null; }
            if (_pulseCoroutine      != null) { StopCoroutine(_pulseCoroutine);      _pulseCoroutine      = null; }
        }

        private void ApplyResting(bool instant)
        {
            if (!instant) return;
            if (backgroundImage != null) backgroundImage.color = restingBgColor;
            if (borderImage     != null) borderImage.color     = restingBorderColor;
            if (label           != null) label.color           = restingTextColor;
            if (rectTransform   != null) rectTransform.localScale = Vector3.one;
        }

        // ── Coroutines ─────────────────────────────────────────────────────────────────
        private IEnumerator TransitionTo(Color targetBg, Color targetBorder, Color targetText, float dur)
        {
            Color startBg     = backgroundImage != null ? backgroundImage.color : targetBg;
            Color startBorder = borderImage     != null ? borderImage.color     : targetBorder;
            Color startText   = label           != null ? label.color           : targetText;
            float elapsed     = 0f;
            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                float t  = Mathf.Clamp01(elapsed / dur);
                if (backgroundImage != null) backgroundImage.color = Color.Lerp(startBg,     targetBg,     t);
                if (borderImage     != null) borderImage.color     = Color.Lerp(startBorder, targetBorder, t);
                if (label           != null) label.color           = Color.Lerp(startText,   targetText,   t);
                yield return null;
            }
            if (backgroundImage != null) backgroundImage.color = targetBg;
            if (borderImage     != null) borderImage.color     = targetBorder;
            if (label           != null) label.color           = targetText;
        }

        private IEnumerator PulseBorder()
        {
            // On focus: pulse the border between deep gold and bright gold-white
            Color dimGold    = new Color(1.0f, 0.75f, 0.10f, focusPulseMinAlpha);
            Color brightGold = new Color(1.0f, 0.95f, 0.60f, focusPulseMaxAlpha);
            while (_isHighlighted)
            {
                if (borderImage == null) yield break;
                float t = (Mathf.Sin(Time.unscaledTime * focusPulseFrequency * Mathf.PI * 2f) + 1f) * 0.5f;
                if (borderImage != null) borderImage.color = Color.Lerp(dimGold, brightGold, t);
                yield return null;
            }
        }

        private IEnumerator ScaleTo(float target, float dur)
        {
            if (rectTransform == null) yield break;
            Vector3 from = rectTransform.localScale;
            Vector3 to   = Vector3.one * target;
            float elapsed = 0f;
            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                rectTransform.localScale = Vector3.Lerp(from, to, Mathf.Clamp01(elapsed / dur));
                yield return null;
            }
            rectTransform.localScale = to;
        }

        private IEnumerator SubmitFlash()
        {
            if (rectTransform != null) rectTransform.localScale = Vector3.one * pressScale;
            yield return new WaitForSecondsRealtime(0.08f);
            if (rectTransform != null) rectTransform.localScale = Vector3.one * focusScale;
        }

        /// <summary>Programmatically selects this button in the EventSystem.</summary>
        public void Select()
        {
            GetComponent<Selectable>()?.Select();
        }

        /// <summary>Overrides the focus color; call to match a time state.</summary>
        public void SetStateColor(Color c) => focusBorderColor = c;

        /// <summary>
        /// When false, this button ignores TimeState color changes and keeps the ice-blue scheme.
        /// Call from screens that always want consistent blue buttons (e.g. MainMenuScreen).
        /// </summary>
        public void LockColorScheme(bool locked) => _useTimeStateColor = !locked;
    }
}
