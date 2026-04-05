using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TrialsOfKairos.UI
{
    /// <summary>
    /// Main menu screen: grand cosmic entrance with staggered button reveal.
    /// Title has a slow glow pulse (TitleGlowPulser). Cosmic ring rotates (OrbitRing).
    /// Buttons stagger in with per-button CanvasGroup fades.
    /// </summary>
    public class MainMenuScreen : UIScreenBase
    {
        [Header("Title")]
        [SerializeField] private RectTransform   titleRect;
        [SerializeField] private CanvasGroup     titleGroup;
        [SerializeField] private TextMeshProUGUI titleLabel;

        [Header("Subtitle")]
        [SerializeField] private TextMeshProUGUI subtitleLabel;

        [Header("Buttons")]
        [SerializeField] private KairosButton beginButton;
        [SerializeField] private KairosButton continueButton;
        [SerializeField] private KairosButton settingsButton;
        [SerializeField] private KairosButton quitButton;

        [Header("Background Elements")]
        [SerializeField] private RectTransform mainTimeRing;  // has OrbitRing component
        [SerializeField] private CanvasGroup   bgGroup;

        [Header("Ambient Glows")]
        [SerializeField] private Image innerGlowRing;  // optional inner halo Image
        [SerializeField] private Image outerGlowHalo;  // optional soft outer halo

        [Header("Stagger")]
        [SerializeField] private float buttonStaggerDelay = 0.08f;

        private KairosButton[]    _buttons;
        private Coroutine         _innerGlowLoop;
        private Coroutine         _outerGlowLoop;
        private Coroutine         _ambientLoop;
        private Coroutine         _titleColorLoop;
        private bool              _subtitleShown = false;

        protected override void Awake()
        {
            base.Awake();
            _buttons = new[] { beginButton, continueButton, settingsButton, quitButton };

            // Tell atmosphere not to breathe the subtitle — MainMenuScreen owns it
            MainMenuAtmosphere atm = GetComponentInChildren<MainMenuAtmosphere>(includeInactive: true);
            atm?.DisableSubtitleBreathing();
        }

        private void Start()
        {
            // ── Auto-wire null inspector refs from the live hierarchy ──────────────────
            ResolveRefs();

            // ── Button listeners ──────────────────────────────────────────────────────
            if (beginButton != null) beginButton.OnClicked.AddListener(() =>
            {
                if (ScreenTransitionManager.Instance != null)
                    ScreenTransitionManager.Instance.CrossFade(0.3f, 0.1f, 0.4f,
                        () => UIManager.Instance.ShowScreen(UIScreenType.HUD));
                else UIManager.Instance.ShowScreen(UIScreenType.HUD);
            });
            if (continueButton != null) continueButton.OnClicked.AddListener(() =>
                UIManager.Instance.ShowScreen(UIScreenType.LevelSelect));
            if (settingsButton  != null) settingsButton.OnClicked.AddListener(() =>
                UIManager.Instance.ShowScreen(UIScreenType.Controls));
            if (quitButton      != null) quitButton.OnClicked.AddListener(QuitApplication);

            SetButtonLabel(beginButton,    "BEGIN TRIAL");
            SetButtonLabel(continueButton, "TRIAL SELECTION");
            SetButtonLabel(settingsButton, "SETTINGS");
            SetButtonLabel(quitButton,     "QUIT");

            // Lock buttons to ice-blue — ignore any time-state color callbacks
            foreach (KairosButton btn in _buttons)
                btn?.LockColorScheme(true);
        }

        /// <summary>
        /// Resolves null inspector references. Title stays ice-white; subtitle is
        /// gold-tinted and managed exclusively here (atmosphere breathing disabled in Awake).
        /// </summary>
        private void ResolveRefs()
        {
            if (titleLabel != null)
            {
                titleLabel.color = new Color(0.95f, 0.97f, 1.0f, 1f);
                if (titleRect  == null) titleRect  = titleLabel.rectTransform;
                if (titleGroup == null)
                    titleGroup = titleLabel.GetComponent<CanvasGroup>()
                              ?? titleLabel.gameObject.AddComponent<CanvasGroup>();
            }

            // Subtitle: warm gold-white, alpha starts at 0 — revealed once per Show()
            if (subtitleLabel != null)
            {
                Color c = subtitleLabel.color;
                subtitleLabel.color = new Color(0.92f, 0.82f, 0.55f, 0f);
            }

            if (bgGroup == null)
            {
                Transform bg = transform.Find("Background");
                if (bg != null)
                    bgGroup = bg.GetComponent<CanvasGroup>()
                           ?? bg.gameObject.AddComponent<CanvasGroup>();
            }

            foreach (KairosButton btn in _buttons)
                if (btn != null && btn.GetComponent<CanvasGroup>() == null)
                    btn.gameObject.AddComponent<CanvasGroup>();
        }

        private static void SetButtonLabel(KairosButton btn, string text)
        {
            if (btn == null) return;
            TextMeshProUGUI lbl = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (lbl != null) lbl.text = text;
        }

        protected override void OnBeforeShow()
        {
            _subtitleShown = false;
            foreach (KairosButton btn in _buttons)
            {
                if (btn == null) continue;
                CanvasGroup cg = btn.GetComponent<CanvasGroup>();
                if (cg == null) cg = btn.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
            }
            if (titleGroup    != null) titleGroup.alpha = 0f;
            if (bgGroup       != null) bgGroup.alpha    = 0f;
            // Subtitle alpha reset — will be faded in once by EntranceSequence
            if (subtitleLabel != null)
            {
                Color c = subtitleLabel.color; c.a = 0f; subtitleLabel.color = c;
            }
            SetImageAlpha(innerGlowRing, 0f);
            SetImageAlpha(outerGlowHalo, 0f);
        }

        protected override void OnShown()
        {
            StartCoroutine(EntranceSequence());
        }

        protected override void OnBeforeHide()
        {
            StopGlowLoops();
            if (_ambientLoop    != null) { StopCoroutine(_ambientLoop);    _ambientLoop    = null; }
            if (_titleColorLoop != null) { StopCoroutine(_titleColorLoop); _titleColorLoop = null; }
        }

        private IEnumerator EntranceSequence()
        {
            // 1. Background fade
            if (bgGroup != null)
                StartCoroutine(UIAnimationUtils.FadeCanvasGroup(bgGroup, 1f, 0.85f));
            yield return new WaitForSecondsRealtime(0.15f);

            // 2. Title fades in + micro pulse
            if (titleGroup != null)
            {
                StartCoroutine(UIAnimationUtils.FadeCanvasGroup(titleGroup, 1f, 0.60f));
                if (titleRect != null)
                    StartCoroutine(UIAnimationUtils.PulseScale(titleRect, 1.04f, 0.55f));
            }
            yield return new WaitForSecondsRealtime(0.45f);

            // 3. Subtitle fades in exactly once — plain fade, no slide, no loop
            if (subtitleLabel != null && !_subtitleShown)
            {
                _subtitleShown = true;
                CanvasGroup subCG = subtitleLabel.GetComponent<CanvasGroup>();
                if (subCG == null) subCG = subtitleLabel.gameObject.AddComponent<CanvasGroup>();
                subCG.alpha = 0f;
                yield return UIAnimationUtils.FadeCanvasGroup(subCG, 0.75f, 0.40f);
                // Bake alpha into color and destroy the CanvasGroup so atmosphere can breathe it freely
                Color sc = subtitleLabel.color; sc.a = 0.75f; subtitleLabel.color = sc;
                Destroy(subCG);
            }
            yield return new WaitForSecondsRealtime(0.15f);

            // 4. Glow rings
            StartGlowLoops();

            // 5. Title color loop (white → gold → white slowly)
            if (titleLabel != null)
                _titleColorLoop = StartCoroutine(TitleColorLoop());

            // 6. Buttons stagger fade in
            for (int i = 0; i < _buttons.Length; i++)
            {
                KairosButton btn = _buttons[i];
                if (btn == null) continue;
                CanvasGroup cg = btn.GetComponent<CanvasGroup>();
                if (cg == null) continue;
                StartCoroutine(UIAnimationUtils.FadeCanvasGroup(cg, 1f, 0.38f));
                yield return new WaitForSecondsRealtime(buttonStaggerDelay);
            }

            yield return new WaitForSecondsRealtime(0.4f);

            // 7. Ambient shimmer loop
            _ambientLoop = StartCoroutine(AmbientAtmosphereLoop());
        }

        /// <summary>
        /// Continuously pulses the title between crisp white and warm gold.
        /// The cycle is slow and organic so it reads as an ambient glow shift, not a flash.
        /// Runs until OnBeforeHide stops it.
        /// </summary>
        private IEnumerator TitleColorLoop()
        {
            Color white = new Color(0.95f, 0.97f, 1.0f, 1f);
            Color gold  = new Color(1.0f,  0.82f, 0.20f, 1f);
            while (true)
            {
                // White → gold over 3.5s
                float elapsed = 0f, dur = Random.Range(3.0f, 5.0f);
                while (elapsed < dur)
                {
                    elapsed += Time.unscaledDeltaTime;
                    if (titleLabel != null)
                        titleLabel.color = Color.Lerp(white, gold, elapsed / dur);
                    yield return null;
                }
                // Hold gold briefly
                yield return new WaitForSecondsRealtime(Random.Range(0.4f, 1.2f));

                // Gold → white over 2s
                elapsed = 0f; dur = Random.Range(1.5f, 2.5f);
                while (elapsed < dur)
                {
                    elapsed += Time.unscaledDeltaTime;
                    if (titleLabel != null)
                        titleLabel.color = Color.Lerp(gold, white, elapsed / dur);
                    yield return null;
                }
                // Hold white briefly
                yield return new WaitForSecondsRealtime(Random.Range(0.8f, 2.0f));
            }
        }

        /// <summary>
        /// Ambient loop: random gold shimmer bursts on top of the continuous color loop.
        /// Uses ShimmerLabel with a bright gold tint for a sudden flash.
        /// </summary>
        private IEnumerator AmbientAtmosphereLoop()
        {
            // Static gold shimmer target (the ShimmerLabel call briefly snaps to this)
            Color shimmerGold = new Color(1.0f, 0.92f, 0.50f, 1f);
            while (true)
            {
                yield return new WaitForSecondsRealtime(Random.Range(3.0f, 6.0f));
                if (titleLabel != null)
                    yield return UIAnimationUtils.ShimmerLabel(titleLabel, shimmerGold, halfDuration: 0.30f);
            }
        }

        private void StartGlowLoops()
        {
            // Inner ring: warm gold pulse
            if (innerGlowRing != null)
                _innerGlowLoop = StartCoroutine(UIAnimationUtils.PulseGlow(
                    innerGlowRing,
                    new Color(1.0f, 0.80f, 0.20f, 1f),
                    frequency: 0.50f, minAlpha: 0.05f, maxAlpha: 0.28f));

            // Outer halo: cool blue pulse (contrasting the inner gold)
            if (outerGlowHalo != null)
                _outerGlowLoop = StartCoroutine(UIAnimationUtils.PulseGlow(
                    outerGlowHalo,
                    new Color(0.45f, 0.70f, 1.0f, 1f),
                    frequency: 0.32f, minAlpha: 0.02f, maxAlpha: 0.12f));
        }

        private void StopGlowLoops()
        {
            if (_innerGlowLoop != null) { StopCoroutine(_innerGlowLoop); _innerGlowLoop = null; }
            if (_outerGlowLoop != null) { StopCoroutine(_outerGlowLoop); _outerGlowLoop = null; }
        }

        private static void SetImageAlpha(Image img, float a)
        {
            if (img == null) return;
            Color c = img.color; c.a = a; img.color = c;
        }

        private void QuitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
