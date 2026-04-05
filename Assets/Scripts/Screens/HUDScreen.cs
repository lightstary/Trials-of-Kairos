using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TrialsOfKairos.UI
{
    /// <summary>
    /// In-game HUD screen. Manages TimeState indicator, SplitTimeScaleMeter (primary),
    /// objective display, area title intro and pause access.
    ///
    /// HUD layout (enforced by HUDLayoutEnforcer):
    ///   Top-left     → TimeStateIndicator
    ///   Top-center   → SplitTimeScaleMeter  (PRIMARY ELEMENT)
    ///   Top-right    → ObjectivePanel
    ///   Bottom-left  → PauseButton
    ///   Bottom-right → (reserved / secondary)
    /// </summary>
    public class HUDScreen : UIScreenBase
    {
        [Header("Components")]
        [SerializeField] private TimeStateIndicator timeStateIndicator;
        [SerializeField] private SplitTimeScaleMeter splitTimeScaleMeter;  // PRIMARY
        [SerializeField] private AreaTitleIntro      areaTitleIntro;

        [Header("Objective (Top-Right)")]
        [SerializeField] private TextMeshProUGUI objectiveLabel;
        [SerializeField] private TextMeshProUGUI trialProgressLabel;

        [Header("Pause (Bottom-Left)")]
        [SerializeField] private KairosButton pauseButton;

        [Header("Edge Vignette")]
        [SerializeField] private Image vignetteImage;

        [Header("Layout Enforcer")]
        [SerializeField] private HUDLayoutEnforcer layoutEnforcer;

        // JoystickButton7 = Start / Menu on Xbox controllers
        private const KeyCode StartButton = KeyCode.JoystickButton7;

        private void Start()
        {
            if (pauseButton != null) pauseButton.OnClicked.AddListener(() => UIManager.Instance.OpenPause());
        }

        /// <summary>
        /// Polls for the controller Start button to open the pause menu regardless
        /// of whether pauseButton is wired in the inspector.
        /// </summary>
        private void Update()
        {
            if (CanvasGroup == null || !CanvasGroup.interactable) return;
            if (Input.GetKeyDown(StartButton) || Input.GetKeyDown(KeyCode.Escape))
                UIManager.Instance?.OpenPause();
        }

        protected override void OnShown()
        {
            if (layoutEnforcer != null) layoutEnforcer.enabled = true;
        }

        /// <summary>Called by game logic when entering a new area.</summary>
        public void PlayAreaIntro(string trialName, string subtitle = "")
            => areaTitleIntro?.Play(trialName, subtitle);

        /// <summary>Updates the objective line (top-right panel).</summary>
        public void SetObjective(string text)
        {
            if (objectiveLabel != null) objectiveLabel.text = text;
        }

        /// <summary>Updates the trial progress indicator, e.g. "TRIAL 4 / 9".</summary>
        public void SetTrialProgress(int current, int total)
        {
            if (trialProgressLabel != null)
                trialProgressLabel.text = $"TRIAL  {current}  /  {total}";
        }

        /// <summary>
        /// Drives the SplitTimeScaleMeter.
        /// 0 = fully reversed, 0.5 = neutral / frozen, 1 = fully forward.
        /// </summary>
        public void SetTimeScale(float normalizedValue)
            => splitTimeScaleMeter?.SetValue(normalizedValue);

        /// <summary>Enable boss danger overlay on the split meter.</summary>
        public void SetBossMode(bool enabled, float dangerThreshold = 0.80f)
            => splitTimeScaleMeter?.SetBossMode(enabled, dangerThreshold);

        // Legacy shim — kept so any existing callers don't break.
        // Routes to the new SplitTimeScaleMeter.
        public void SetDangerThreshold(float normalizedThreshold)
            => splitTimeScaleMeter?.SetBossMode(true, normalizedThreshold);
    }
}
