using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TrialsOfKairos.UI
{
    /// <summary>
    /// Game over screen: fracture lines draw from center, red vignette intensifies,
    /// title slams in, Chronos quote fades.
    /// </summary>
    public class GameOverScreen : UIScreenBase
    {
        [Header("Fracture")]
        [SerializeField] private CanvasGroup   fractureOverlay;
        [SerializeField] private float         fractureDrawDuration = 0.8f;

        [Header("Red Vignette")]
        [SerializeField] private Image            redVignette;
        [SerializeField] private RedVignettePulser vignettePulser;

        [Header("Labels")]
        [SerializeField] private RectTransform   titleRect;
        [SerializeField] private CanvasGroup     titleGroup;
        [SerializeField] private TextMeshProUGUI quoteLabel;
        [SerializeField] private CanvasGroup     quoteGroup;

        [Header("Buttons")]
        [SerializeField] private KairosButton retryButton;
        [SerializeField] private KairosButton hubButton;

        private void Start()
        {
            if (retryButton != null) retryButton.OnClicked.AddListener(() =>
                ScreenTransitionManager.Instance?.CrossFade(0.3f, 0.1f, 0.4f,
                    () => UIManager.Instance.ShowScreen(UIScreenType.HUD)));
            if (hubButton != null) hubButton.OnClicked.AddListener(() =>
                ScreenTransitionManager.Instance?.CrossFade(0.3f, 0.1f, 0.4f,
                    () => UIManager.Instance.ShowScreen(UIScreenType.LevelSelect)));
        }

        protected override void OnBeforeShow()
        {
            if (fractureOverlay  != null) fractureOverlay.alpha = 0f;
            if (redVignette      != null)
            {
                Color c = redVignette.color; c.a = 0f;
                redVignette.color = c;
            }
            if (titleGroup  != null) titleGroup.alpha  = 0f;
            if (quoteGroup  != null) quoteGroup.alpha  = 0f;
            if (titleRect   != null) titleRect.localScale = Vector3.one * 1.1f;

            // Ensure pulser is stopped before the sequence restarts
            vignettePulser?.Stop();
        }

        protected override void OnShown()
        {
            StartCoroutine(GameOverSequence());
        }

        private IEnumerator GameOverSequence()
        {
            // Fracture draws in
            if (fractureOverlay != null)
                yield return UIAnimationUtils.FadeCanvasGroup(fractureOverlay, 1f, fractureDrawDuration);

            // Red vignette pulsing (if pulser present, delegate to it; else do plain lerp)
            if (vignettePulser != null)
            {
                vignettePulser.StartPulse();
            }
            else if (redVignette != null)
            {
                yield return UIAnimationUtils.LerpImageColor(redVignette,
                    new Color(0.898f, 0.196f, 0.106f, 0.5f), 0.4f);
            }

            ScreenTransitionManager.Instance?.FlashRed(0.3f);

            // Title slams in from scale + shake
            if (titleGroup != null && titleRect != null)
            {
                yield return UIAnimationUtils.ScaleRect(titleRect, Vector3.one * 1.1f, Vector3.one, 0.15f,
                    UIAnimationUtils.Overshoot);
                StartCoroutine(UIAnimationUtils.ShakeRect(titleRect, 8f, 0.25f));
                yield return UIAnimationUtils.FadeCanvasGroup(titleGroup, 1f, 0.1f);
            }

            yield return new WaitForSecondsRealtime(0.5f);

            // Chronos quote typewriter reveal
            if (quoteGroup != null)
            {
                quoteGroup.alpha = 1f;
                if (quoteLabel != null)
                {
                    string full = quoteLabel.text;
                    yield return UIAnimationUtils.TypewriterReveal(quoteLabel, full, 1.2f);
                }
            }
        }
    }
}
