using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TrialsOfKairos.UI
{
    /// <summary>
    /// Win screen: radiant gold burst, stat count-up, particle bloom, looping gold glow.
    /// </summary>
    public class WinScreen : UIScreenBase
    {
        [Header("Radiance Burst")]
        [SerializeField] private Image       radianceBurst;
        [SerializeField] private float       burstExpandDuration = 1.2f;

        [Header("Gold Glow Ring (ambient loop)")]
        [SerializeField] private Image       goldGlowRing;
        [SerializeField] private float       glowFrequency = 0.7f;

        [Header("Labels")]
        [SerializeField] private TextMeshProUGUI trialCompleteLabel;
        [SerializeField] private TextMeshProUGUI bestTimeLabel;
        [SerializeField] private TextMeshProUGUI personalBestBadge;

        [Header("Stars")]
        [SerializeField] private Transform   starsContainer;

        [Header("Buttons")]
        [SerializeField] private KairosButton nextTrialButton;
        [SerializeField] private KairosButton hubButton;

        [Header("Particles")]
        [SerializeField] private ParticleSystem victoryParticles;

        private string _trialName;
        private float  _bestTime;
        private bool   _isPersonalBest;
        private int    _stars;
        private Coroutine _glowLoop;

        private void Start()
        {
            if (nextTrialButton != null) nextTrialButton.OnClicked.AddListener(() =>
                ScreenTransitionManager.Instance?.CrossFade(0.3f, 0.1f, 0.4f,
                    () => UIManager.Instance.ShowScreen(UIScreenType.HUD)));
            if (hubButton != null) hubButton.OnClicked.AddListener(() =>
                ScreenTransitionManager.Instance?.CrossFade(0.3f, 0.1f, 0.4f,
                    () => UIManager.Instance.ShowScreen(UIScreenType.LevelSelect)));
        }

        /// <summary>Provide result data before calling Show().</summary>
        public void SetResults(string trialName, float bestTime, bool isPersonalBest, int stars)
        {
            _trialName      = trialName;
            _bestTime       = bestTime;
            _isPersonalBest = isPersonalBest;
            _stars          = stars;
        }

        protected override void OnBeforeShow()
        {
            if (radianceBurst != null)
            {
                radianceBurst.transform.localScale = Vector3.zero;
                Color c = radianceBurst.color; c.a = 0.85f;
                radianceBurst.color = c;
            }
            if (goldGlowRing  != null)
            {
                Color c = goldGlowRing.color; c.a = 0f;
                goldGlowRing.color = c;
            }
            if (trialCompleteLabel != null) trialCompleteLabel.maxVisibleCharacters = 0;
        }

        protected override void OnShown()
        {
            StartCoroutine(WinSequence());
        }

        protected override void OnBeforeHide()
        {
            if (_glowLoop != null) { StopCoroutine(_glowLoop); _glowLoop = null; }
        }

        private IEnumerator WinSequence()
        {
            // Gold flash
            ScreenTransitionManager.Instance?.FlashGold(0.5f);

            // Burst expands with overshoot
            if (radianceBurst != null)
            {
                yield return UIAnimationUtils.ScaleRect(
                    radianceBurst.rectTransform,
                    Vector3.zero, Vector3.one * 2.8f, burstExpandDuration,
                    UIAnimationUtils.Overshoot);
                yield return UIAnimationUtils.LerpImageColor(radianceBurst,
                    new Color(1f, 0.843f, 0f, 0f), 0.5f);
            }

            // Particles
            if (victoryParticles != null)
            {
                victoryParticles.gameObject.SetActive(true);
                victoryParticles.Play();
            }

            // Title typewriter reveal
            if (trialCompleteLabel != null)
            {
                string text = $"{_trialName?.ToUpper() ?? "TRIAL"} — COMPLETE";
                yield return UIAnimationUtils.TypewriterReveal(trialCompleteLabel, text, 0.6f);
            }

            // Best time count-up
            if (bestTimeLabel != null && _bestTime > 0f)
                yield return UIAnimationUtils.CountUp(bestTimeLabel, _bestTime, 0.9f, "0.00");

            // Personal best badge
            if (personalBestBadge != null)
            {
                personalBestBadge.gameObject.SetActive(_isPersonalBest);
                if (_isPersonalBest)
                    StartCoroutine(UIAnimationUtils.PulseScale(
                        personalBestBadge.rectTransform, 1.2f, 0.5f));
            }

            // Ambient glow ring loop
            if (goldGlowRing != null)
            {
                _glowLoop = StartCoroutine(UIAnimationUtils.PulseGlow(
                    goldGlowRing,
                    new Color(1f, 0.843f, 0f, 1f),
                    glowFrequency, 0.05f, 0.45f));
            }
        }
    }
}
