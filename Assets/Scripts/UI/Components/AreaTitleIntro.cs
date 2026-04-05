using System.Collections;
using UnityEngine;
using TMPro;

namespace TrialsOfKairos.UI
{
    /// <summary>
    /// Plays the area title strip animation:
    /// slides in from top, holds, then slides back out.
    /// </summary>
    public class AreaTitleIntro : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform   stripRect;
        [SerializeField] private TextMeshProUGUI titleLabel;
        [SerializeField] private TextMeshProUGUI subLabel;

        [Header("Animation")]
        [SerializeField] private float slideInDuration  = 0.40f;
        [SerializeField] private float holdDuration     = 2.50f;
        [SerializeField] private float slideOutDuration = 0.35f;
        [SerializeField] private float offScreenY       = 120f; // pixels above screen

        private Vector2   _shownPosition;
        private Coroutine _sequence;

        private void Awake()
        {
            if (stripRect != null)
            {
                _shownPosition          = stripRect.anchoredPosition;
                Vector2 hiddenPos       = _shownPosition;
                hiddenPos.y            += offScreenY;
                stripRect.anchoredPosition = hiddenPos;
            }
            gameObject.SetActive(false);
        }

        /// <summary>Plays the area title intro with the given names.</summary>
        public void Play(string trialName, string subtitle = "")
        {
            if (titleLabel != null) titleLabel.text = trialName.ToUpper();
            if (subLabel   != null) subLabel.text   = subtitle.ToUpper();

            gameObject.SetActive(true);
            if (_sequence != null) StopCoroutine(_sequence);
            _sequence = StartCoroutine(PlaySequence());
        }

        private IEnumerator PlaySequence()
        {
            Vector2 hiddenPos = _shownPosition + new Vector2(0f, offScreenY);
            stripRect.anchoredPosition = hiddenPos;

            yield return UIAnimationUtils.SlideRect(stripRect, _shownPosition, slideInDuration);
            yield return new WaitForSecondsRealtime(holdDuration);
            yield return UIAnimationUtils.SlideRect(stripRect, hiddenPos, slideOutDuration);

            gameObject.SetActive(false);
        }
    }
}
