using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace TrialsOfKairos.UI
{
    /// <summary>
    /// A single node on the Level Select star map.
    /// Visual states: locked · unlocked · current · completed · boss · selected.
    /// Selection shows a pulsing ring and scale-up; hover gives a subtle lift.
    /// </summary>
    public class LevelNodeUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("References")]
        [SerializeField] private Image           nodeImage;
        [SerializeField] private Image           ringImage;       // pulsing ring (always present)
        [SerializeField] private Image           selectionRing;   // secondary outer ring — shown only when selected
        [SerializeField] private TextMeshProUGUI indexLabel;

        [Header("State Colors")]
        [SerializeField] private Color completedColor = new Color(1f,    0.843f, 0f,    1f);
        [SerializeField] private Color currentColor   = new Color(0.961f,0.784f, 0.259f,1f);
        [SerializeField] private Color lockedColor    = new Color(0.3f,  0.3f,  0.4f,  0.5f);
        [SerializeField] private Color bossColor      = new Color(0.898f,0.196f, 0.106f,1f);
        [SerializeField] private Color selectedColor  = new Color(1f,    1f,    1f,    1f);

        [Header("Hover / Select Animation")]
        [SerializeField] private float hoverScale    = 1.15f;
        [SerializeField] private float selectScale   = 1.22f;
        [SerializeField] private float scaleSpeed    = 10f;

        // Solid gold for the currently selected node
        private static readonly Color SolidGold   = new Color(1.0f,  0.82f, 0.10f, 1f);
        // Ice-blue shimmer used for the node being navigated to
        private static readonly Color SelectionBlue = new Color(0.45f, 0.75f, 1.0f, 1f);

        private LevelData         _data;
        private Action<LevelData> _onSelected;
        private bool              _isSelected = false;

        // Individually tracked coroutines so stopping one doesn't kill the others
        private Coroutine _ringPulse;
        private Coroutine _selectionPulse;
        private Coroutine _scaleAnim;

        /// <summary>True if this node's level is unlocked.</summary>
        public bool IsUnlocked => _data?.isUnlocked ?? false;

        /// <summary>The LevelData this node represents. Used for selection matching.</summary>
        public LevelData BoundData => _data;

        // ── Public ────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Wires Image and label references when the node is built in code (no prefab).
        /// Must be called before Initialise().
        /// </summary>
        public void ConfigureReferences(Image nodeImg, Image ringImg, Image selRing, TextMeshProUGUI label)
        {
            nodeImage     = nodeImg;
            ringImage     = ringImg;
            selectionRing = selRing;
            indexLabel    = label;
        }


        public void Initialise(LevelData data, Action<LevelData> onSelected)
        {
            _data       = data;
            _onSelected = onSelected;

            if (indexLabel != null) indexLabel.text = data.isBossLevel ? "BOSS" : data.levelIndex.ToString();

            Color c = data.isBossLevel   ? bossColor      :
                      data.isCompleted   ? completedColor :
                      data.isUnlocked    ? currentColor   :
                      lockedColor;

            if (nodeImage     != null) nodeImage.color = c;

            // Ambient ring: subtle pulse for the current (not-yet-completed) node
            if (ringImage != null)
            {
                bool showRing = data.isUnlocked && !data.isCompleted;
                ringImage.gameObject.SetActive(showRing);
                if (showRing)
                {
                    ringImage.color = c;
                    // Gentle pulse — not overpowering
                    _ringPulse = StartCoroutine(UIAnimationUtils.PulseGlow(
                        ringImage, c, frequency: 0.8f, minAlpha: 0.08f, maxAlpha: 0.30f));
                }
            }

            // Selection ring hidden until selected
            if (selectionRing != null)
            {
                selectionRing.gameObject.SetActive(false);
                selectionRing.color = selectedColor;
            }
        }

        /// <summary>Programmatically mark this node as selected or deselected.</summary>
        public void SetSelected(bool selected)
        {
            _isSelected = selected;

            if (_selectionPulse != null) { StopCoroutine(_selectionPulse); _selectionPulse = null; }

            if (selectionRing != null)
            {
                selectionRing.gameObject.SetActive(selected);
                if (selected)
                {
                    // Selected: solid gold ring — no pulse, unmistakably chosen
                    selectionRing.color = SolidGold;
                }
                else
                {
                    Color c = selectionRing.color; c.a = 0f; selectionRing.color = c;
                }
            }

            // Node image: solid gold when selected, ice-blue shimmer when navigating to,
            // data color otherwise
            if (nodeImage != null && _data != null)
            {
                Color target = selected ? SolidGold : GetDataColor(_data);
                StartCoroutine(UIAnimationUtils.LerpImageColor(nodeImage, target, 0.18f));
            }

            // Selection ring: when *just* arriving at a node (selected=true), run a single
            // blue shimmer flash on the ring before settling to solid gold
            if (selected && selectionRing != null)
                _selectionPulse = StartCoroutine(ArrivalFlash());

            if (_scaleAnim != null) { StopCoroutine(_scaleAnim); _scaleAnim = null; }
            RestartRingPulse();
            _scaleAnim = StartCoroutine(AnimateScale(selected ? selectScale : 1f));
        }

        /// <summary>
        /// When arriving at a node: flashes ice-blue once on the selection ring,
        /// then fades to solid gold — signalling "this is now selected".
        /// </summary>
        private IEnumerator ArrivalFlash()
        {
            if (selectionRing == null) yield break;

            // Flash blue
            selectionRing.color = SelectionBlue;
            yield return new WaitForSecondsRealtime(0.12f);

            // Crossfade to solid gold over 0.25s
            float elapsed = 0f;
            while (elapsed < 0.25f)
            {
                elapsed += Time.unscaledDeltaTime;
                if (selectionRing != null)
                    selectionRing.color = Color.Lerp(SelectionBlue, SolidGold, elapsed / 0.25f);
                yield return null;
            }
            if (selectionRing != null) selectionRing.color = SolidGold;
            _selectionPulse = null;
        }

        private Color GetDataColor(LevelData data)
        {
            if (data.isBossLevel) return bossColor;
            if (data.isCompleted) return completedColor;
            if (data.isUnlocked)  return currentColor;
            return lockedColor;
        }

        /// <summary>Programmatically fires the selection callback, same as clicking.</summary>
        public void SimulateClick()
        {
            if (_data?.isUnlocked ?? false)
                _onSelected?.Invoke(_data);
        }

        /// <summary>
        /// Stops all tracked coroutines. Call before Destroy() to prevent MissingReferenceException.
        /// </summary>
        public void StopAndClean()
        {
            if (_selectionPulse != null) { StopCoroutine(_selectionPulse); _selectionPulse = null; }
            if (_ringPulse      != null) { StopCoroutine(_ringPulse);      _ringPulse      = null; }
            if (_scaleAnim      != null) { StopCoroutine(_scaleAnim);      _scaleAnim      = null; }
            StopAllCoroutines();
        }

        private void OnDestroy() => StopAllCoroutines();

        // ── Pointer events ────────────────────────────────────────────────────────────
        public void OnPointerEnter(PointerEventData _)
        {
            if (!(_data?.isUnlocked ?? false)) return;
            if (_isSelected) return;
            StopScaleCoroutines();
            _scaleAnim = StartCoroutine(AnimateScale(hoverScale));
        }

        public void OnPointerExit(PointerEventData _)
        {
            if (_isSelected) return;
            StopScaleCoroutines();
            _scaleAnim = StartCoroutine(AnimateScale(1f));
        }

        public void OnPointerClick(PointerEventData _)
        {
            if (!(_data?.isUnlocked ?? false)) return;
            _onSelected?.Invoke(_data);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────────
        private void RestartRingPulse()
        {
            if (_ringPulse != null) { StopCoroutine(_ringPulse); _ringPulse = null; }
            if (ringImage != null && ringImage.gameObject.activeSelf && _data != null)
            {
                Color c = GetDataColor(_data);
                _ringPulse = StartCoroutine(UIAnimationUtils.PulseGlow(
                    ringImage, c, frequency: 1.0f, minAlpha: 0.15f, maxAlpha: 0.6f));
            }
        }

        private void StopScaleCoroutines()
        {
            if (_scaleAnim != null) { StopCoroutine(_scaleAnim); _scaleAnim = null; }
            RestartRingPulse();
        }

        private IEnumerator AnimateScale(float target)
        {
            Vector3 from = transform.localScale;
            Vector3 to   = Vector3.one * target;
            float   t    = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime * scaleSpeed;
                transform.localScale = Vector3.LerpUnclamped(from, to, UIAnimationUtils.EaseOut.Evaluate(t));
                yield return null;
            }
            transform.localScale = to;
        }
    }
}
