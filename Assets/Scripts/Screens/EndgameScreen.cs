using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TrialsOfKairos.UI
{
    /// <summary>
    /// Endgame screen: all three time-state colors bloom, hourglass ascends,
    /// total stats count up with LEFT = stats / RIGHT = summary layout.
    /// Includes: total time, trials cleared, stance usage, completion %, rank.
    /// </summary>
    public class EndgameScreen : UIScreenBase
    {
        [Header("Background Bloom")]
        [SerializeField] private Image yellowBloom;
        [SerializeField] private Image blueBloom;
        [SerializeField] private Image purpleBloom;
        [SerializeField] private float bloomDuration = 1.8f;

        [Header("Hourglass")]
        [SerializeField] private RectTransform hourglassRect;
        [SerializeField] private float         ascendDistance = 80f;

        // ── Left column: raw stats ───────────────────────────────────────────────────
        [Header("Left Stats")]
        [SerializeField] private CanvasGroup     statsGroup;
        [SerializeField] private TextMeshProUGUI totalTimeLabel;
        [SerializeField] private TextMeshProUGUI trialsLabel;
        [SerializeField] private TextMeshProUGUI completionPctLabel;

        // Stance usage (forward / frozen / reverse seconds)
        [SerializeField] private TextMeshProUGUI forwardUsageLabel;
        [SerializeField] private TextMeshProUGUI frozenUsageLabel;
        [SerializeField] private TextMeshProUGUI reverseUsageLabel;

        // ── Right column: summary ────────────────────────────────────────────────────
        [Header("Right Summary")]
        [SerializeField] private CanvasGroup     summaryGroup;
        [SerializeField] private TextMeshProUGUI rankLabel;
        [SerializeField] private TextMeshProUGUI rankExplanationLabel;

        [Header("Buttons")]
        [SerializeField] private KairosButton mainMenuButton;
        [SerializeField] private KairosButton playAgainButton;

        // ── Runtime data ─────────────────────────────────────────────────────────────
        private float  _totalTimeSeconds;
        private int    _trialsCleared;
        private int    _totalTrials;
        private string _rank;
        private float  _forwardSeconds;
        private float  _frozenSeconds;
        private float  _reverseSeconds;

        private static readonly System.Collections.Generic.Dictionary<string, string> RankExplanations
            = new System.Collections.Generic.Dictionary<string, string>
        {
            { "CHRONOKEEPER",   "All trials cleared. Time bends to your will." },
            { "TIMEWEAVER",     "Mastery of all three stances demonstrated." },
            { "CHRONOSURGEON",  "Precision. Every second accounted for." },
            { "APPRENTICE",     "A worthy start. Chronos watches." },
        };

        // ── Lifecycle ─────────────────────────────────────────────────────────────────
        private void Start()
        {
            if (mainMenuButton != null) mainMenuButton.OnClicked.AddListener(() =>
                ScreenTransitionManager.Instance?.CrossFade(0.4f, 0.1f, 0.5f,
                    () => UIManager.Instance.ShowScreen(UIScreenType.MainMenu)));
            if (playAgainButton != null) playAgainButton.OnClicked.AddListener(() =>
                ScreenTransitionManager.Instance?.CrossFade(0.4f, 0.1f, 0.5f,
                    () => UIManager.Instance.ShowScreen(UIScreenType.LevelSelect)));
        }

        // ── Public API ────────────────────────────────────────────────────────────────
        /// <summary>Set final run statistics before calling Show().</summary>
        public void SetStats(float totalTime, int trialsCleared, int totalTrials, string rank)
        {
            _totalTimeSeconds = totalTime;
            _trialsCleared    = trialsCleared;
            _totalTrials      = totalTrials;
            _rank             = rank;
        }

        /// <summary>Optionally provide per-stance usage in seconds.</summary>
        public void SetStanceUsage(float forwardSec, float frozenSec, float reverseSec)
        {
            _forwardSeconds = forwardSec;
            _frozenSeconds  = frozenSec;
            _reverseSeconds = reverseSec;
        }

        // ── Transitions ───────────────────────────────────────────────────────────────
        protected override void OnBeforeShow()
        {
            SetBloomAlpha(yellowBloom, 0f);
            SetBloomAlpha(blueBloom,   0f);
            SetBloomAlpha(purpleBloom, 0f);
            if (statsGroup   != null) statsGroup.alpha   = 0f;
            if (summaryGroup != null) summaryGroup.alpha = 0f;
        }

        protected override void OnShown()
        {
            StartCoroutine(EndgameSequence());
        }

        private IEnumerator EndgameSequence()
        {
            // Tri-color bloom
            float half = bloomDuration * 0.5f;
            StartCoroutine(BloomIn(yellowBloom, 0.40f, half));
            StartCoroutine(BloomIn(blueBloom,   0.30f, half + 0.2f));
            StartCoroutine(BloomIn(purpleBloom, 0.35f, half + 0.4f));
            yield return new WaitForSecondsRealtime(bloomDuration);

            // Hourglass ascends
            if (hourglassRect != null)
            {
                Vector2 origin = hourglassRect.anchoredPosition;
                yield return UIAnimationUtils.SlideRect(
                    hourglassRect, origin + new Vector2(0f, ascendDistance), 1.0f);
            }

            // Left stats column fades + counts
            if (statsGroup != null)
                yield return UIAnimationUtils.FadeCanvasGroup(statsGroup, 1f, 0.5f);

            if (totalTimeLabel != null)
                yield return UIAnimationUtils.CountUp(totalTimeLabel, _totalTimeSeconds, 1.2f, "0.0");

            if (trialsLabel != null)
                trialsLabel.text = $"{_trialsCleared} / {_totalTrials}";

            float total = Mathf.Max(1f, _forwardSeconds + _frozenSeconds + _reverseSeconds);
            if (completionPctLabel != null)
            {
                float pct = _totalTrials > 0 ? _trialsCleared / (float)_totalTrials * 100f : 100f;
                completionPctLabel.text = $"{pct:0}%";
            }
            if (forwardUsageLabel  != null)
                forwardUsageLabel.text  = $"{_forwardSeconds / total * 100f:0}%  FORWARD";
            if (frozenUsageLabel   != null)
                frozenUsageLabel.text   = $"{_frozenSeconds  / total * 100f:0}%  FROZEN";
            if (reverseUsageLabel  != null)
                reverseUsageLabel.text  = $"{_reverseSeconds / total * 100f:0}%  REVERSED";

            yield return new WaitForSecondsRealtime(0.3f);

            // Right summary column: rank reveal
            if (summaryGroup != null)
                yield return UIAnimationUtils.FadeCanvasGroup(summaryGroup, 1f, 0.6f);

            if (rankLabel != null)
            {
                rankLabel.text = _rank?.ToUpper() ?? "CHRONOKEEPER";
                yield return UIAnimationUtils.PulseScale(rankLabel.rectTransform, 1.18f, 0.55f);
            }

            if (rankExplanationLabel != null)
            {
                string key  = _rank?.ToUpper() ?? "CHRONOKEEPER";
                string expl = RankExplanations.TryGetValue(key, out string e) ? e : "The sands remember.";
                yield return UIAnimationUtils.TypewriterReveal(rankExplanationLabel, expl, 1.0f);
            }
        }

        private IEnumerator BloomIn(Image bloom, float targetAlpha, float delay)
        {
            if (bloom == null) yield break;
            yield return new WaitForSecondsRealtime(delay);
            Color start = bloom.color; start.a = 0f; bloom.color = start;
            Color target = bloom.color; target.a = targetAlpha;
            yield return UIAnimationUtils.LerpImageColor(bloom, target, bloomDuration * 0.6f);
        }

        private static void SetBloomAlpha(Image img, float alpha)
        {
            if (img == null) return;
            Color c = img.color; c.a = alpha; img.color = c;
        }
    }
}
