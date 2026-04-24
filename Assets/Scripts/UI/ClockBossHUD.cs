using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Clock face HUD for Boss C (The Clock). Cosmic-styled circular clock
/// with procedural particles, asymmetric ring warp, orbiting center motes,
/// smooth hand, pulsing targets, tutorial integration, full-clock color
/// state, and dynamic objective updates.
/// Reads directly from BossCFight — no duplicated gameplay logic.
/// </summary>
public class ClockBossHUD : MonoBehaviour
{
    // ── Layout ───────────────────────────────────────────────────────────
    private const float CLOCK_SIZE     = 280f;
    private const float GLOW_PAD       = 60f;
    private const float CONTAINER_SIZE = CLOCK_SIZE + GLOW_PAD * 2f;
    private const float MARGIN_X       = 40f - GLOW_PAD;
    private const float MARGIN_Y       = 40f - GLOW_PAD;
    private const int   HOUR_COUNT     = 12;
    private const float HALF           = CLOCK_SIZE * 0.5f;

    // Fractions of half-clock-size
    private const float NUMBER_RADIUS_F = 0.72f;
    private const float TICK_OUTER_F    = 0.90f;
    private const float TICK_LENGTH_F   = 0.07f;
    private const float TICK_LENGTH_MAJ = 0.10f;
    private const float HAND_LENGTH_F   = 0.76f;
    private const float HAND_WIDTH      = 5f;
    private const float RING_INNER_F    = 0.90f;
    private const float RING_OUTER_F    = 0.97f;
    private const float PIVOT_SIZE      = 14f;
    private const float NUM_GLOW_SIZE   = 56f;
    private const float WARP_STRENGTH   = 0.06f;

    // ── Particle counts ──────────────────────────────────────────────────
    private const int RING_PARTICLE_COUNT   = 16;
    private const int DRIFT_PARTICLE_COUNT  = 10;
    private const int INNER_PARTICLE_COUNT  = 12;
    private const int CENTER_MOTE_COUNT     = 6;

    // ── Base Colors ──────────────────────────────────────────────────────
    private static readonly Color RING_GOLD       = new Color(0.961f, 0.784f, 0.259f, 0.90f);
    private static readonly Color HAND_GOLD       = new Color(0.961f, 0.824f, 0.400f, 1f);
    private static readonly Color PIVOT_GLOW_GOLD = new Color(0.961f, 0.784f, 0.259f, 0.30f);
    private static readonly Color PIVOT_CORE_GOLD = new Color(1.000f, 0.920f, 0.550f, 1f);
    private static readonly Color COSMIC_GOLD     = new Color(0.961f, 0.784f, 0.259f, 0.22f);
    private static readonly Color RING_BLUE       = new Color(0.353f, 0.706f, 0.941f, 0.90f);
    private static readonly Color HAND_BLUE       = new Color(0.353f, 0.706f, 0.941f, 1f);
    private static readonly Color PIVOT_GLOW_BLUE = new Color(0.353f, 0.706f, 0.941f, 0.30f);
    private static readonly Color PIVOT_CORE_BLUE = new Color(0.500f, 0.800f, 1.000f, 1f);
    private static readonly Color COSMIC_BLUE     = new Color(0.353f, 0.706f, 0.941f, 0.22f);
    private static readonly Color RING_DIMMED       = new Color(0.500f, 0.400f, 0.300f, 0.18f);
    private static readonly Color HAND_DIMMED       = new Color(0.850f, 0.750f, 0.550f, 0.50f);
    private static readonly Color PIVOT_GLOW_DIMMED = new Color(0.850f, 0.700f, 0.400f, 0.10f);
    private static readonly Color PIVOT_CORE_DIMMED = new Color(0.850f, 0.780f, 0.500f, 0.60f);
    private static readonly Color COSMIC_DIMMED     = new Color(0.850f, 0.700f, 0.400f, 0.03f);

    private static readonly Color NUMBER_NORMAL   = new Color(0.860f, 0.840f, 0.780f, 0.78f);
    private static readonly Color NUMBER_MAJOR    = new Color(0.920f, 0.890f, 0.800f, 0.95f);
    private static readonly Color TARGET_GREEN    = new Color(0.200f, 1.000f, 0.400f, 1f);
    private static readonly Color TICK_NORMAL_COL = new Color(0.550f, 0.530f, 0.480f, 0.38f);
    private static readonly Color TICK_MAJOR_COL  = new Color(0.700f, 0.670f, 0.580f, 0.60f);
    private static readonly Color FEEDBACK_GREEN  = new Color(0.200f, 1.000f, 0.400f, 1f);
    private static readonly Color DANGER_RED      = new Color(0.898f, 0.196f, 0.106f, 0.75f);
    private static readonly Color TUT_GLOW_COL    = new Color(0.961f, 0.784f, 0.259f, 0.35f);
    private static readonly Color TUT_DIM_COL     = new Color(0f, 0f, 0f, 0.55f);
    private static readonly Color PARTICLE_COL    = new Color(0.961f, 0.824f, 0.400f, 0.40f);

    // ── Animation ────────────────────────────────────────────────────────
    private const float PULSE_SPEED        = 4f;
    private const float PULSE_SCALE_MIN    = 1.15f;
    private const float PULSE_SCALE_MAX    = 1.25f;
    private const float RING_ROTATE_SPEED  = 0.3f;
    private const float GLOW_BREATHE_SPEED = 1.5f;
    private const float PIVOT_PULSE_SPEED  = 2.5f;
    private const float HAND_SMOOTH_SPEED  = 6f;
    private const float FADE_OUT_TIME      = 0.25f;
    private const float RING_WOBBLE_AMP    = 0.10f;

    // ── Particle data ────────────────────────────────────────────────────
    private struct Mote
    {
        public RectTransform rt;
        public Image img;
        public float angle;
        public float radius;
        public float speed;
        public float drift;
        public float phase;
        public float baseAlpha;
        public float baseSize;
        public float maxRadius;
    }

    // ── UI element references ────────────────────────────────────────────
    private GameObject    _outerRoot;
    private GameObject    _clockFace;
    private RectTransform _clockFaceRT;
    private Image         _cosmicGlow;
    private Image         _tutorialGlow;
    private Image         _discImage;
    private RectTransform _ringRT;
    private Image         _ringImage;
    private RectTransform _handRT;
    private Image         _handBarImage;
    private Image         _handTipImage;
    private Image         _pivotCore;
    private Image         _pivotGlowImg;

    private TextMeshProUGUI[] _numberTMPs  = new TextMeshProUGUI[HOUR_COUNT];
    private RectTransform[]   _numberRTs   = new RectTransform[HOUR_COUNT];
    private Image[]           _numberGlows = new Image[HOUR_COUNT];
    private Image[]           _tickImages  = new Image[HOUR_COUNT];

    private TextMeshProUGUI _feedbackTMP;
    private Coroutine       _feedbackRoutine;

    // Particles
    private Mote[] _ringMotes;
    private Mote[] _driftMotes;
    private Mote[] _innerMotes;
    private Mote[] _centerMotes;

    // Center energy pulse
    private Image _centerPulseImg;
    private float _centerPulseAlpha;

    // Tutorial overlays
    private GameObject _tutDimRoot;
    private GameObject _meterHighlightRoot;

    // ── Sprites ──────────────────────────────────────────────────────────
    private Sprite _cosmicDiscSprite;
    private Sprite _softGlowSprite;
    private Sprite _ringSprite;
    private Sprite _circleSprite;
    private Sprite _dotSprite;
    private Sprite _arrowSprite;

    // ── State ────────────────────────────────────────────────────────────
    private bool   _built;
    private bool   _visible;
    private bool   _subscribed;
    private float  _pulsePhase;
    private float  _ringAngle;
    private float  _displayAngle;
    private int    _fadingIndex = -1;
    private float  _fadeTimer;
    private bool   _tutorialMode;
    private int    _tutorialPage = -1;
    private Canvas _overrideSortCanvas;
    private int    _lastObjectiveTarget = -1;

    // Current accent state
    private Color _curRing   = RING_GOLD;
    private Color _curHand   = HAND_GOLD;
    private Color _curPivotG = PIVOT_GLOW_GOLD;
    private Color _curPivotC = PIVOT_CORE_GOLD;
    private Color _curCosmic = COSMIC_GOLD;

    // Tutorial animation state
    private float _tutFadeAlpha;
    private Vector2 _tutCurrentPos;
    private Vector2 _tutTargetPos;
    private CanvasGroup _outerCG;

    // Tutorial positions
    private static readonly Vector2 TUT_POS_LEFT = new Vector2(MARGIN_X, -280f);  // Visible left side, vertically centered with tutorial
    private static readonly Vector2 TUT_POS_FINAL = new Vector2(MARGIN_X, -MARGIN_Y); // Normal top-left
    private const float TUT_FADE_SPEED = 2.5f;
    private const float TUT_GLIDE_SPEED = 2f;

    // ══════════════════════════════════════════════════════════════════════
    //  LIFECYCLE
    // ══════════════════════════════════════════════════════════════════════

    void OnEnable()  => BossIntroModal.OnPageChanged += HandleTutorialPageChanged;
    void OnDisable() { BossIntroModal.OnPageChanged -= HandleTutorialPageChanged; UnsubscribeBoss(); }

    void Update()
    {
        TrySubscribe();
        float dt = Time.unscaledDeltaTime;

        if (_tutorialMode && _built) { UpdateTutorialClock(dt); return; }
        if (!_visible || !_built) return;

        BossCFight boss = BossCFight.Instance;
        if (boss == null || !boss.bossActive) { SetVisible(false); return; }

        UpdateAccentColors(dt);
        UpdateHandSmooth(boss, dt);
        UpdateTargetPulse(boss, dt);
        UpdateFadingNumber(dt);
        UpdateRingRotation(dt);
        UpdateRingWarp();
        UpdateRingBreathe();
        UpdatePivotPulse();
        UpdateCosmicBreathe();
        UpdateClockBreathing();
        UpdateParticles(dt);
        UpdateCenterPulse(dt);
        UpdateObjectiveIfChanged(boss);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  SUBSCRIPTION
    // ══════════════════════════════════════════════════════════════════════

    private void TrySubscribe()
    {
        if (_subscribed || BossCFight.Instance == null) return;
        _subscribed = true;
        BossCFight.Instance.OnBossStart        += HandleBossStart;
        BossCFight.Instance.OnBossStop         += HandleBossStop;
        BossCFight.Instance.OnChallengeComplete += HandleChallengeComplete;
        BossCFight.Instance.OnBossWin          += HandleBossWin;
    }

    private void UnsubscribeBoss()
    {
        if (!_subscribed || BossCFight.Instance == null) return;
        BossCFight.Instance.OnBossStart        -= HandleBossStart;
        BossCFight.Instance.OnBossStop         -= HandleBossStop;
        BossCFight.Instance.OnChallengeComplete -= HandleChallengeComplete;
        BossCFight.Instance.OnBossWin          -= HandleBossWin;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  BOSS EVENTS
    // ══════════════════════════════════════════════════════════════════════

    private void HandleBossStart()
    {
        if (!_built) Build();
        _pulsePhase = _fadeTimer = _ringAngle = _displayAngle = 0f;
        _fadingIndex = -1;
        _lastObjectiveTarget = -1;
        ResetAllNumbers();
        ExitTutorialMode();
        SetVisible(true);
        SetObjectiveForCurrentTarget(BossCFight.Instance);
    }

    private void HandleBossStop()
    {
        SetVisible(false);
        if (HUDController.Instance != null) HUDController.Instance.ClearBossObjective();
    }

    private void HandleChallengeComplete(int completedIndex)
    {
        BossCFight boss = BossCFight.Instance;
        int reachedHour = AngleToHourIndex(boss.challengeAngles[completedIndex]);
        string reachedStr = HourToTimeStr(reachedHour);

        string nextStr = null;
        if (completedIndex + 1 < boss.TotalChallenges)
        {
            int nextHour = AngleToHourIndex(boss.challengeAngles[completedIndex + 1]);
            nextStr = HourToTimeStr(nextHour);
        }

        _fadingIndex = reachedHour;
        _fadeTimer   = FADE_OUT_TIME;
        _pulsePhase  = 0f;

        // Center energy pulse
        TriggerCenterPulse();

        if (HUDController.Instance != null)
            HUDController.Instance.ShowClockTargetReached(reachedStr, nextStr);

        if (nextStr != null)
            StartCoroutine(DelayedObjectiveUpdate(0.6f));
    }

    private void HandleBossWin()
    {
        ShowFeedback("ALL TARGETS REACHED");
        if (HUDController.Instance != null)
            HUDController.Instance.SetClockObjective("COMPLETE",
                BossCFight.Instance.TotalChallenges, BossCFight.Instance.TotalChallenges);
        StartCoroutine(DelayedHide(2f));
    }

    private IEnumerator DelayedObjectiveUpdate(float delay)
    {
        yield return new WaitForSeconds(delay);
        BossCFight boss = BossCFight.Instance;
        if (boss != null && boss.bossActive) SetObjectiveForCurrentTarget(boss);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  OBJECTIVE
    // ══════════════════════════════════════════════════════════════════════

    private void SetObjectiveForCurrentTarget(BossCFight boss)
    {
        if (HUDController.Instance == null || boss == null) return;
        if (boss.CurrentChallengeIndex >= boss.TotalChallenges) return;
        int targetHour = AngleToHourIndex(boss.TargetAngle);
        _lastObjectiveTarget = boss.CurrentChallengeIndex;
        HUDController.Instance.SetClockObjective(HourToTimeStr(targetHour),
            boss.CurrentChallengeIndex + 1, boss.TotalChallenges);
    }

    private void UpdateObjectiveIfChanged(BossCFight boss)
    {
        if (boss.CurrentChallengeIndex != _lastObjectiveTarget
            && boss.CurrentChallengeIndex < boss.TotalChallenges)
            SetObjectiveForCurrentTarget(boss);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  TUTORIAL MODE
    // ══════════════════════════════════════════════════════════════════════

    private void HandleTutorialPageChanged(int page, int totalPages)
    {
        if (BossCFight.Instance == null) return;
        if (page < 0) { ExitTutorialMode(); return; }
        if (!_built) Build();
        EnterTutorialMode(page);
    }

    private void EnterTutorialMode(int page)
    {
        _tutorialMode = true;
        _tutorialPage = page;
        if (_outerRoot != null) _outerRoot.SetActive(true);

        // Cache CanvasGroup for fade
        if (_outerCG == null && _outerRoot != null)
            _outerCG = _outerRoot.GetComponent<CanvasGroup>();

        if (page == 0)
        {
            // Fade in from invisible, position left of center
            _tutFadeAlpha = 0f;
            _tutCurrentPos = TUT_POS_LEFT;
            _tutTargetPos = TUT_POS_LEFT;
            if (_outerCG != null) _outerCG.alpha = 0f;
            RectTransform outerRT = _outerRoot.GetComponent<RectTransform>();
            if (outerRT != null) outerRT.anchoredPosition = _tutCurrentPos;
        }
        else if (page == 1)
        {
            // Glide from current position to final top-left
            _tutTargetPos = TUT_POS_FINAL;
        }

        if (_overrideSortCanvas == null && _outerRoot != null)
        {
            _overrideSortCanvas = _outerRoot.AddComponent<Canvas>();
            _overrideSortCanvas.overrideSorting = true;
            _overrideSortCanvas.sortingOrder = 210;
            _outerRoot.AddComponent<GraphicRaycaster>();
        }

        if (_tutorialGlow != null) _tutorialGlow.enabled = (page == 0);
        SetTutorialDimActive(page == 1 || page == 2);
        SetMeterHighlightActive(page == 2);
        ResetAllNumbers();
    }

    private void ExitTutorialMode()
    {
        _tutorialMode = false;
        _tutorialPage = -1;
        if (_tutorialGlow != null) _tutorialGlow.enabled = false;
        SetTutorialDimActive(false);
        SetMeterHighlightActive(false);

        // Restore position and alpha
        if (_outerCG != null) _outerCG.alpha = 1f;
        RectTransform outerRT = _outerRoot != null ? _outerRoot.GetComponent<RectTransform>() : null;
        if (outerRT != null) outerRT.anchoredPosition = TUT_POS_FINAL;

        if (_overrideSortCanvas != null && _outerRoot != null)
        {
            GraphicRaycaster gr = _outerRoot.GetComponent<GraphicRaycaster>();
            if (gr != null) Destroy(gr);
            Destroy(_overrideSortCanvas);
            _overrideSortCanvas = null;
        }

        if (BossCFight.Instance == null || !BossCFight.Instance.bossActive)
            if (_outerRoot != null) _outerRoot.SetActive(false);
    }

    private void UpdateTutorialClock(float dt)
    {
        // ── Fade in animation ──
        if (_tutFadeAlpha < 1f)
        {
            _tutFadeAlpha = Mathf.MoveTowards(_tutFadeAlpha, 1f, TUT_FADE_SPEED * dt);
            if (_outerCG != null) _outerCG.alpha = _tutFadeAlpha;
        }

        // ── Glide position animation ──
        RectTransform outerRT = _outerRoot != null ? _outerRoot.GetComponent<RectTransform>() : null;
        if (outerRT != null)
        {
            _tutCurrentPos = Vector2.Lerp(_tutCurrentPos, _tutTargetPos, TUT_GLIDE_SPEED * dt);
            outerRT.anchoredPosition = _tutCurrentPos;
        }

        UpdateRingRotation(dt);
        UpdateRingWarp();
        UpdatePivotPulse();
        UpdateCosmicBreathe();
        UpdateRingBreathe();
        UpdateClockBreathing();
        UpdateParticles(dt);
        UpdateCenterPulse(dt);

        if (_handRT != null) _handRT.localRotation = Quaternion.identity;
        if (_handBarImage != null) _handBarImage.color = HAND_GOLD;
        if (_handTipImage != null) _handTipImage.color = HAND_GOLD;

        if (_tutorialPage == 0 && _tutorialGlow != null)
        {
            float p = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2f);
            Color c = TUT_GLOW_COL; c.a = Mathf.Lerp(0.22f, 0.52f, p);
            _tutorialGlow.color = c;
        }

        if (_tutorialPage == 1 && BossCFight.Instance != null
            && BossCFight.Instance.challengeAngles.Length > 0)
        {
            _pulsePhase += dt * PULSE_SPEED;
            int demoTarget = AngleToHourIndex(BossCFight.Instance.challengeAngles[0]);
            for (int i = 0; i < HOUR_COUNT; i++)
            {
                if (_numberRTs[i] == null || _numberTMPs[i] == null) continue;
                if (i == demoTarget)
                {
                    float t = 0.5f + 0.5f * Mathf.Sin(_pulsePhase);
                    _numberRTs[i].localScale = Vector3.one * Mathf.Lerp(PULSE_SCALE_MIN, PULSE_SCALE_MAX, t);
                    _numberTMPs[i].color = TARGET_GREEN;
                    if (_numberGlows[i] != null)
                    {
                        Color gc = TARGET_GREEN; gc.a = 0.25f + 0.22f * t;
                        _numberGlows[i].color = gc; _numberGlows[i].enabled = true;
                    }
                }
                else
                {
                    _numberRTs[i].localScale = Vector3.one;
                    _numberTMPs[i].color = IsMajorHour(i) ? NUMBER_MAJOR : NUMBER_NORMAL;
                    if (_numberGlows[i] != null) _numberGlows[i].enabled = false;
                }
            }
        }

        if (_tutorialPage == 2 && _meterHighlightRoot != null)
        {
            float p = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f);
            float alpha = Mathf.Lerp(0.45f, 0.85f, p);
            foreach (Image img in _meterHighlightRoot.GetComponentsInChildren<Image>())
            {
                Color c = img.color; c.a = alpha; img.color = c;
            }
        }
    }

    // ── Tutorial overlays ────────────────────────────────────────────────

    private void SetTutorialDimActive(bool a) { if (a && _tutDimRoot == null) BuildTutorialDim(); if (_tutDimRoot != null) _tutDimRoot.SetActive(a); }
    private void SetMeterHighlightActive(bool a) { if (a && _meterHighlightRoot == null) BuildMeterHighlight(); if (_meterHighlightRoot != null) _meterHighlightRoot.SetActive(a); }

    private void BuildTutorialDim()
    {
        _tutDimRoot = new GameObject("TutorialDim");
        Canvas c = _tutDimRoot.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 199;
        _tutDimRoot.AddComponent<CanvasScaler>();
        GameObject ov = new GameObject("DimOverlay"); ov.transform.SetParent(_tutDimRoot.transform, false);
        RectTransform rt = ov.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        Image img = ov.AddComponent<Image>(); img.color = TUT_DIM_COL; img.raycastTarget = false;
        _tutDimRoot.SetActive(false);
    }

    private void BuildMeterHighlight()
    {
        _meterHighlightRoot = new GameObject("MeterDangerHL");
        Canvas c = _meterHighlightRoot.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 210;
        CanvasScaler cs = _meterHighlightRoot.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080); cs.matchWidthOrHeight = 0.5f;
        float bY = -45f, bX = 239f;
        CreateDangerGlow(_meterHighlightRoot.transform, new Vector2(-bX, bY), new Vector2(110f, 90f));
        CreateDangerGlow(_meterHighlightRoot.transform, new Vector2(bX, bY), new Vector2(110f, 90f));
        CreateDangerLabel(_meterHighlightRoot.transform, new Vector2(-bX, bY - 38f), "-10");
        CreateDangerLabel(_meterHighlightRoot.transform, new Vector2(bX, bY - 38f), "+10");
        _meterHighlightRoot.SetActive(false);
    }

    private void CreateDangerGlow(Transform p, Vector2 pos, Vector2 sz)
    {
        GameObject go = new GameObject("DG"); go.transform.SetParent(p, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f); rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = sz;
        Image img = go.AddComponent<Image>(); img.sprite = _softGlowSprite; img.color = DANGER_RED; img.raycastTarget = false;
    }

    private void CreateDangerLabel(Transform p, Vector2 pos, string text)
    {
        GameObject go = new GameObject("DL"); go.transform.SetParent(p, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f); rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(80f, 30f);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = 22f; tmp.color = DANGER_RED;
        tmp.alignment = TextAlignmentOptions.Center; tmp.fontStyle = FontStyles.Bold; tmp.raycastTarget = false;
        CinzelFontHelper.Apply(tmp, true);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  UPDATE — GAMEPLAY
    // ══════════════════════════════════════════════════════════════════════

    private void UpdateAccentColors(float dt)
    {
        Color tR = RING_GOLD, tH = HAND_GOLD, tPG = PIVOT_GLOW_GOLD, tPC = PIVOT_CORE_GOLD, tC = COSMIC_GOLD;
        if (TimeState.Instance != null)
        {
            switch (TimeState.Instance.currentState)
            {
                case TimeState.State.Frozen:
                    tR = RING_BLUE; tH = HAND_BLUE; tPG = PIVOT_GLOW_BLUE; tPC = PIVOT_CORE_BLUE; tC = COSMIC_BLUE; break;
                case TimeState.State.Reverse:
                    tR = RING_DIMMED; tH = HAND_DIMMED; tPG = PIVOT_GLOW_DIMMED; tPC = PIVOT_CORE_DIMMED; tC = COSMIC_DIMMED; break;
            }
        }
        float s = 6f * dt;
        _curRing = Color.Lerp(_curRing, tR, s); _curHand = Color.Lerp(_curHand, tH, s);
        _curPivotG = Color.Lerp(_curPivotG, tPG, s); _curPivotC = Color.Lerp(_curPivotC, tPC, s);
        _curCosmic = Color.Lerp(_curCosmic, tC, s);
        if (_handBarImage != null) _handBarImage.color = _curHand;
        if (_handTipImage != null) _handTipImage.color = _curHand;
        if (_pivotCore != null) _pivotCore.color = _curPivotC;
    }

    private void UpdateHandSmooth(BossCFight boss, float dt)
    {
        if (_handRT == null) return;
        float target = boss.CurrentHandAngle;
        _displayAngle = Mathf.MoveTowardsAngle(_displayAngle, target, 120f * dt);
        _displayAngle = Mathf.LerpAngle(_displayAngle, target, HAND_SMOOTH_SPEED * dt);
        _handRT.localRotation = Quaternion.Euler(0f, 0f, -_displayAngle);
    }

    private void UpdateTargetPulse(BossCFight boss, float dt)
    {
        if (boss.CurrentChallengeIndex >= boss.TotalChallenges) return;
        int targetIdx = AngleToHourIndex(boss.TargetAngle);
        _pulsePhase += dt * PULSE_SPEED;
        for (int i = 0; i < HOUR_COUNT; i++)
        {
            if (_numberRTs[i] == null || _numberTMPs[i] == null) continue;
            if (i == _fadingIndex) continue;
            if (i == targetIdx)
            {
                float t = 0.5f + 0.5f * Mathf.Sin(_pulsePhase);
                _numberRTs[i].localScale = Vector3.one * Mathf.Lerp(PULSE_SCALE_MIN, PULSE_SCALE_MAX, t);
                _numberTMPs[i].color = TARGET_GREEN;
                if (_numberGlows[i] != null)
                { Color gc = TARGET_GREEN; gc.a = 0.22f + 0.22f * t; _numberGlows[i].color = gc; _numberGlows[i].enabled = true; }
            }
            else
            {
                _numberRTs[i].localScale = Vector3.one;
                _numberTMPs[i].color = IsMajorHour(i) ? NUMBER_MAJOR : NUMBER_NORMAL;
                if (_numberGlows[i] != null) _numberGlows[i].enabled = false;
            }
        }
    }

    private void UpdateFadingNumber(float dt)
    {
        if (_fadingIndex < 0 || _fadingIndex >= HOUR_COUNT) return;
        _fadeTimer -= dt;
        float t = Mathf.Clamp01(_fadeTimer / FADE_OUT_TIME);
        if (_numberRTs[_fadingIndex] != null)
            _numberRTs[_fadingIndex].localScale = Vector3.one * Mathf.Lerp(1f, PULSE_SCALE_MIN, t);
        if (_numberTMPs[_fadingIndex] != null)
        { Color n = IsMajorHour(_fadingIndex) ? NUMBER_MAJOR : NUMBER_NORMAL; _numberTMPs[_fadingIndex].color = Color.Lerp(n, TARGET_GREEN, t); }
        if (_numberGlows[_fadingIndex] != null)
        { Color gc = TARGET_GREEN; gc.a = 0.22f * t; _numberGlows[_fadingIndex].color = gc; if (t <= 0f) _numberGlows[_fadingIndex].enabled = false; }
        if (_fadeTimer <= 0f) _fadingIndex = -1;
    }

    private void UpdateRingRotation(float dt)
    {
        if (_ringRT == null) return;
        float wobble = 1f + RING_WOBBLE_AMP * Mathf.Sin(Time.unscaledTime * 0.15f);
        _ringAngle += RING_ROTATE_SPEED * wobble * dt;
        _ringRT.localRotation = Quaternion.Euler(0f, 0f, _ringAngle);
    }

    /// <summary>Asymmetric ring scale creates a slow, subtle gravitational distortion.</summary>
    private void UpdateRingWarp()
    {
        if (_ringRT == null) return;
        float t = Time.unscaledTime;
        // Slow, gentle asymmetric breathing — barely perceptible unless watching closely
        float sx = 1.04f + 0.012f * Mathf.Sin(t * 0.22f) + 0.006f * Mathf.Sin(t * 0.45f + 0.8f);
        float sy = 1.05f + 0.016f * Mathf.Sin(t * 0.16f + 1.2f) + 0.008f * Mathf.Sin(t * 0.33f + 2.1f);
        _ringRT.sizeDelta = new Vector2(CLOCK_SIZE * sx, CLOCK_SIZE * sy);
    }

    private void UpdateRingBreathe()
    {
        if (_ringImage == null) return;
        float b = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * GLOW_BREATHE_SPEED);

        bool isReverse = TimeState.Instance != null
            && TimeState.Instance.currentState == TimeState.State.Reverse;

        Color c = _curRing;
        if (isReverse)
        {
            // Flat, dim alpha — no glow in Reverse
            c.a = 0.15f;
        }
        else
        {
            // Vibrant glow pulsing between high alpha values
            c.a = Mathf.Lerp(0.70f, 1f, b);
        }
        _ringImage.color = c;
    }

    private void UpdatePivotPulse()
    {
        if (_pivotGlowImg == null) return;
        float p = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * PIVOT_PULSE_SPEED);
        Color c = _curPivotG; c.a = Mathf.Lerp(0.22f, 0.50f, p);
        _pivotGlowImg.color = c;
        // Subtle scale pulse on the pivot glow
        float s = 1f + 0.08f * p;
        _pivotGlowImg.rectTransform.localScale = Vector3.one * s;
    }

    private void UpdateCosmicBreathe()
    {
        if (_cosmicGlow == null) return;
        float b = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * GLOW_BREATHE_SPEED * 0.7f);

        bool isReverse = TimeState.Instance != null
            && TimeState.Instance.currentState == TimeState.State.Reverse;

        Color c = _curCosmic;
        if (isReverse)
            c.a = Mathf.Lerp(0.02f, 0.05f, b);
        else
            c.a = Mathf.Lerp(0.12f, 0.30f, b);

        _cosmicGlow.color = c;
    }

    /// <summary>Asymmetric scale breathing — slow and barely perceptible.</summary>
    private void UpdateClockBreathing()
    {
        if (_clockFaceRT == null) return;
        float t = Time.unscaledTime;
        // Very slow layered sine waves — only noticeable if you stare at it
        float sx = 1f + 0.005f * Mathf.Sin(t * 0.20f) + 0.003f * Mathf.Sin(t * 0.48f + 1.0f);
        float sy = 1f + 0.007f * Mathf.Sin(t * 0.15f + 0.5f) + 0.003f * Mathf.Sin(t * 0.38f + 2.3f);
        _clockFaceRT.localScale = new Vector3(sx, sy, 1f);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  PARTICLES
    // ══════════════════════════════════════════════════════════════════════

    private void UpdateParticles(float dt)
    {
        float t = Time.unscaledTime;
        Color pCol = new Color(_curHand.r, _curHand.g, _curHand.b, 1f);

        // Ring particles: orbit the outer edge
        if (_ringMotes != null)
        {
            for (int i = 0; i < _ringMotes.Length; i++)
            {
                ref Mote m = ref _ringMotes[i];
                if (m.rt == null) continue;
                m.angle += m.speed * dt;
                float r = m.radius + 2f * Mathf.Sin(t * 1.3f + m.phase);
                m.rt.anchoredPosition = new Vector2(Mathf.Sin(m.angle) * r, Mathf.Cos(m.angle) * r);
                float a = m.baseAlpha * (0.5f + 0.5f * Mathf.Sin(t * 2f + m.phase));
                Color c = pCol; c.a = a;
                m.img.color = c;
            }
        }

        // Drift particles: emanate outward from ring, reset when far
        if (_driftMotes != null)
        {
            for (int i = 0; i < _driftMotes.Length; i++)
            {
                ref Mote m = ref _driftMotes[i];
                if (m.rt == null) continue;
                m.radius += m.drift * dt;
                if (m.radius > m.maxRadius)
                {
                    m.radius = HALF * 0.92f;
                    m.angle += 1.3f;
                }
                m.rt.anchoredPosition = new Vector2(Mathf.Sin(m.angle) * m.radius, Mathf.Cos(m.angle) * m.radius);
                float fadeOut = 1f - Mathf.Clamp01((m.radius - HALF * 0.92f) / (m.maxRadius - HALF * 0.92f));
                Color c = pCol; c.a = m.baseAlpha * fadeOut * (0.6f + 0.4f * Mathf.Sin(t * 1.5f + m.phase));
                m.img.color = c;
            }
        }

        // Inner specks: float gently inside the disc
        if (_innerMotes != null)
        {
            for (int i = 0; i < _innerMotes.Length; i++)
            {
                ref Mote m = ref _innerMotes[i];
                if (m.rt == null) continue;
                m.angle += m.speed * dt;
                float r = m.radius + 4f * Mathf.Sin(t * 0.7f + m.phase);
                m.rt.anchoredPosition = new Vector2(Mathf.Sin(m.angle) * r, Mathf.Cos(m.angle) * r);
                float a = m.baseAlpha * (0.4f + 0.6f * Mathf.Sin(t * 1.8f + m.phase));
                Color c = pCol; c.a = a;
                m.img.color = c;
            }
        }

        // Center motes: tight orbit near pivot
        if (_centerMotes != null)
        {
            for (int i = 0; i < _centerMotes.Length; i++)
            {
                ref Mote m = ref _centerMotes[i];
                if (m.rt == null) continue;
                m.angle += m.speed * dt;
                float r = m.radius + 2f * Mathf.Sin(t * 2.5f + m.phase);
                m.rt.anchoredPosition = new Vector2(Mathf.Sin(m.angle) * r, Mathf.Cos(m.angle) * r);
                float a = m.baseAlpha * (0.5f + 0.5f * Mathf.Sin(t * 3f + m.phase));
                Color c = pCol; c.a = a;
                m.img.color = c;
            }
        }
    }

    /// <summary>Triggers a brief center energy pulse (e.g., on target reached).</summary>
    private void TriggerCenterPulse() => _centerPulseAlpha = 0.8f;

    private void UpdateCenterPulse(float dt)
    {
        if (_centerPulseImg == null) return;
        if (_centerPulseAlpha > 0.01f)
        {
            _centerPulseAlpha = Mathf.Lerp(_centerPulseAlpha, 0f, 2.5f * dt);
            Color c = _curHand; c.a = _centerPulseAlpha;
            _centerPulseImg.color = c;
            float scale = Mathf.Lerp(1f, 3.5f, 1f - _centerPulseAlpha / 0.8f);
            _centerPulseImg.rectTransform.localScale = Vector3.one * scale;
            _centerPulseImg.enabled = true;
        }
        else
        {
            _centerPulseImg.enabled = false;
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  VISIBILITY & NUMBERS
    // ══════════════════════════════════════════════════════════════════════

    private void SetVisible(bool v) { _visible = v; if (_outerRoot != null) _outerRoot.SetActive(v); }

    private IEnumerator DelayedHide(float d) { yield return new WaitForSecondsRealtime(d); SetVisible(false); }

    private void ResetAllNumbers()
    {
        for (int i = 0; i < HOUR_COUNT; i++)
        {
            if (_numberRTs[i] != null) _numberRTs[i].localScale = Vector3.one;
            if (_numberTMPs[i] != null) _numberTMPs[i].color = IsMajorHour(i) ? NUMBER_MAJOR : NUMBER_NORMAL;
            if (_numberGlows[i] != null) _numberGlows[i].enabled = false;
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  FEEDBACK
    // ══════════════════════════════════════════════════════════════════════

    private void ShowFeedback(string msg)
    {
        if (_feedbackTMP == null) return;
        if (_feedbackRoutine != null) StopCoroutine(_feedbackRoutine);
        _feedbackRoutine = StartCoroutine(FeedbackRoutine(msg));
    }

    private IEnumerator FeedbackRoutine(string msg)
    {
        _feedbackTMP.text = msg; _feedbackTMP.color = FEEDBACK_GREEN;
        _feedbackTMP.gameObject.SetActive(true);
        RectTransform rt = _feedbackTMP.GetComponent<RectTransform>();
        float e = 0f;
        while (e < 0.15f) { e += Time.unscaledDeltaTime; if (rt) rt.localScale = Vector3.one * Mathf.Lerp(0.7f, 1f, e / 0.15f); yield return null; }
        if (rt) rt.localScale = Vector3.one;
        yield return new WaitForSecondsRealtime(1f);
        e = 0f;
        while (e < 0.5f) { e += Time.unscaledDeltaTime; Color c = FEEDBACK_GREEN; c.a = 1f - Mathf.Clamp01(e / 0.5f); _feedbackTMP.color = c; yield return null; }
        _feedbackTMP.gameObject.SetActive(false);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  BUILD
    // ══════════════════════════════════════════════════════════════════════

    private void Build()
    {
        _built = true;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;
        Canvas rootCanvas = canvas.rootCanvas != null ? canvas.rootCanvas : canvas;

        CreateSprites();

        // Outer root
        _outerRoot = new GameObject("ClockFaceHUD");
        _outerRoot.transform.SetParent(rootCanvas.transform, false);
        RectTransform outerRT = _outerRoot.AddComponent<RectTransform>();
        outerRT.anchorMin = new Vector2(0f, 1f); outerRT.anchorMax = new Vector2(0f, 1f);
        outerRT.pivot = new Vector2(0f, 1f);
        outerRT.anchoredPosition = new Vector2(MARGIN_X, -MARGIN_Y);
        outerRT.sizeDelta = new Vector2(CONTAINER_SIZE, CONTAINER_SIZE);
        CanvasGroup cg = _outerRoot.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false; cg.interactable = false;

        // Cosmic glow
        GameObject cosmicGO = CenteredGO("CosmicGlow", _outerRoot.transform, CONTAINER_SIZE * 1.3f, CONTAINER_SIZE * 1.3f);
        _cosmicGlow = cosmicGO.AddComponent<Image>();
        _cosmicGlow.sprite = _softGlowSprite; _cosmicGlow.color = COSMIC_GOLD; _cosmicGlow.raycastTarget = false;

        // Tutorial glow
        GameObject tutGO = CenteredGO("TutGlow", _outerRoot.transform, CONTAINER_SIZE * 1.55f, CONTAINER_SIZE * 1.55f);
        _tutorialGlow = tutGO.AddComponent<Image>();
        _tutorialGlow.sprite = _softGlowSprite; _tutorialGlow.color = TUT_GLOW_COL;
        _tutorialGlow.raycastTarget = false; _tutorialGlow.enabled = false;

        // Clock face
        _clockFace = new GameObject("ClockFace");
        _clockFace.transform.SetParent(_outerRoot.transform, false);
        _clockFaceRT = _clockFace.AddComponent<RectTransform>();
        _clockFaceRT.anchorMin = _clockFaceRT.anchorMax = new Vector2(0.5f, 0.5f);
        _clockFaceRT.pivot = new Vector2(0.5f, 0.5f);
        _clockFaceRT.anchoredPosition = Vector2.zero;
        _clockFaceRT.sizeDelta = new Vector2(CLOCK_SIZE, CLOCK_SIZE);

        BuildDisc();
        BuildInnerShadow();
        BuildOuterRing();
        BuildParticles();
        BuildTickMarks();
        BuildNumbers();
        BuildHand();
        BuildCenterPivot();
        BuildFeedbackLabel(rootCanvas.transform);

        _outerRoot.SetActive(false);
    }

    private void BuildDisc()
    {
        GameObject go = StretchedGO("Disc", _clockFace.transform);
        _discImage = go.AddComponent<Image>();
        _discImage.sprite = _cosmicDiscSprite; _discImage.color = Color.white; _discImage.raycastTarget = false;
    }

    private void BuildInnerShadow()
    {
        GameObject go = StretchedGO("InnerShadow", _clockFace.transform);
        Image img = go.AddComponent<Image>();
        img.sprite = _ringSprite; img.color = new Color(0f, 0f, 0f, 0.25f); img.raycastTarget = false;
    }

    private void BuildOuterRing()
    {
        GameObject go = CenteredGO("OuterRing", _clockFace.transform, CLOCK_SIZE * 1.06f, CLOCK_SIZE * 1.08f);
        _ringRT = go.GetComponent<RectTransform>();
        _ringImage = go.AddComponent<Image>();
        _ringImage.sprite = _ringSprite; _ringImage.color = RING_GOLD; _ringImage.raycastTarget = false;
    }

    private void BuildParticles()
    {
        System.Random rng = new System.Random(99);

        // Ring particles — orbit outside the ring
        _ringMotes = new Mote[RING_PARTICLE_COUNT];
        for (int i = 0; i < RING_PARTICLE_COUNT; i++)
            _ringMotes[i] = SpawnMote(_outerRoot.transform, rng,
                HALF * 0.93f, HALF * 1.10f, 0.25f, 0.7f, 0.15f, 0.35f, 2.5f, 5.5f);

        // Drift particles — emanate outward from ring edge
        _driftMotes = new Mote[DRIFT_PARTICLE_COUNT];
        for (int i = 0; i < DRIFT_PARTICLE_COUNT; i++)
        {
            _driftMotes[i] = SpawnMote(_outerRoot.transform, rng,
                HALF * 0.90f, HALF * 0.98f, 0f, 0.08f, 0.10f, 0.25f, 2f, 4.5f);
            _driftMotes[i].drift = RandRange(rng, 10f, 22f);
            _driftMotes[i].maxRadius = HALF * 1.40f;
        }

        // Inner specks — float gently inside the disc
        _innerMotes = new Mote[INNER_PARTICLE_COUNT];
        for (int i = 0; i < INNER_PARTICLE_COUNT; i++)
            _innerMotes[i] = SpawnMote(_clockFace.transform, rng,
                HALF * 0.12f, HALF * 0.58f, 0.12f, 0.40f, 0.08f, 0.18f, 2f, 4f);

        // Center motes — tight orbit near pivot
        _centerMotes = new Mote[CENTER_MOTE_COUNT];
        for (int i = 0; i < CENTER_MOTE_COUNT; i++)
            _centerMotes[i] = SpawnMote(_clockFace.transform, rng,
                6f, 25f, 0.8f, 2.2f, 0.18f, 0.38f, 2f, 4f);

        // Center energy pulse ring (larger for more dramatic effect)
        GameObject pulseGO = CenteredGO("CenterPulse", _clockFace.transform, PIVOT_SIZE * 5f, PIVOT_SIZE * 5f);
        _centerPulseImg = pulseGO.AddComponent<Image>();
        _centerPulseImg.sprite = _softGlowSprite; _centerPulseImg.color = Color.clear;
        _centerPulseImg.raycastTarget = false; _centerPulseImg.enabled = false;
    }

    private Mote SpawnMote(Transform parent, System.Random rng,
        float rMin, float rMax, float spdMin, float spdMax,
        float aMin, float aMax, float szMin, float szMax)
    {
        Mote m = new Mote();
        float sz = RandRange(rng, szMin, szMax);
        GameObject go = CenteredGO("M", parent, sz, sz);
        m.rt = go.GetComponent<RectTransform>();
        m.img = go.AddComponent<Image>();
        m.img.sprite = _dotSprite;
        m.img.color = PARTICLE_COL;
        m.img.raycastTarget = false;
        m.angle = RandRange(rng, 0f, Mathf.PI * 2f);
        m.radius = RandRange(rng, rMin, rMax);
        m.speed = RandRange(rng, spdMin, spdMax) * (rng.NextDouble() > 0.5 ? 1f : -1f);
        m.phase = RandRange(rng, 0f, Mathf.PI * 2f);
        m.baseAlpha = RandRange(rng, aMin, aMax);
        m.baseSize = sz;
        m.drift = 0f;
        m.maxRadius = rMax;
        return m;
    }

    private void BuildTickMarks()
    {
        for (int i = 0; i < HOUR_COUNT; i++)
        {
            float deg = i * 30f, rad = deg * Mathf.Deg2Rad;
            bool maj = IsMajorHour(i);
            float len = HALF * (maj ? TICK_LENGTH_MAJ : TICK_LENGTH_F);
            float w = maj ? 2.5f : 1.5f;
            float midR = HALF * TICK_OUTER_F - len * 0.5f;
            GameObject go = CenteredGO($"T{i}", _clockFace.transform, w, len);
            RectTransform trt = go.GetComponent<RectTransform>();
            trt.anchoredPosition = new Vector2(Mathf.Sin(rad) * midR, Mathf.Cos(rad) * midR);
            trt.localRotation = Quaternion.Euler(0f, 0f, -deg);
            _tickImages[i] = go.AddComponent<Image>();
            _tickImages[i].color = maj ? TICK_MAJOR_COL : TICK_NORMAL_COL; _tickImages[i].raycastTarget = false;
        }
    }

    private void BuildNumbers()
    {
        float numR = HALF * NUMBER_RADIUS_F;
        for (int i = 0; i < HOUR_COUNT; i++)
        {
            int dn = (i == 0) ? 12 : i;
            float deg = i * 30f, rad = deg * Mathf.Deg2Rad;
            bool maj = IsMajorHour(i);
            float wf = 1f - WARP_STRENGTH * Mathf.Max(0f, -Mathf.Cos(rad));
            float ar = numR * wf;
            float px = Mathf.Sin(rad) * ar, py = Mathf.Cos(rad) * ar;

            GameObject glowGO = CenteredGO($"NG{dn}", _outerRoot.transform, NUM_GLOW_SIZE, NUM_GLOW_SIZE);
            glowGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(px, py);
            _numberGlows[i] = glowGO.AddComponent<Image>();
            _numberGlows[i].sprite = _softGlowSprite;
            _numberGlows[i].color = new Color(TARGET_GREEN.r, TARGET_GREEN.g, TARGET_GREEN.b, 0.25f);
            _numberGlows[i].raycastTarget = false; _numberGlows[i].enabled = false;

            GameObject numGO = CenteredGO($"N{dn}", _clockFace.transform, 36f, 30f);
            _numberRTs[i] = numGO.GetComponent<RectTransform>();
            _numberRTs[i].anchoredPosition = new Vector2(px, py);
            TextMeshProUGUI tmp = numGO.AddComponent<TextMeshProUGUI>();
            tmp.text = dn.ToString(); tmp.fontSize = maj ? 20f : 17f;
            tmp.color = maj ? NUMBER_MAJOR : NUMBER_NORMAL;
            tmp.alignment = TextAlignmentOptions.Center; tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.raycastTarget = false; CinzelFontHelper.Apply(tmp, maj);
            _numberTMPs[i] = tmp;
        }
    }

    private void BuildHand()
    {
        float handLen = HALF * HAND_LENGTH_F;
        GameObject pivotGO = CenteredGO("HandPivot", _clockFace.transform, 0f, 0f);
        _handRT = pivotGO.GetComponent<RectTransform>();

        GameObject barGO = new GameObject("Bar"); barGO.transform.SetParent(pivotGO.transform, false);
        RectTransform brt = barGO.AddComponent<RectTransform>();
        brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f);
        brt.pivot = new Vector2(0.5f, 0f); brt.anchoredPosition = Vector2.zero;
        brt.sizeDelta = new Vector2(HAND_WIDTH, handLen);
        _handBarImage = barGO.AddComponent<Image>(); _handBarImage.color = HAND_GOLD; _handBarImage.raycastTarget = false;

        GameObject tipGO = new GameObject("Tip"); tipGO.transform.SetParent(barGO.transform, false);
        RectTransform trt = tipGO.AddComponent<RectTransform>();
        trt.anchorMin = new Vector2(0.5f, 1f); trt.anchorMax = new Vector2(0.5f, 1f);
        trt.pivot = new Vector2(0.5f, 0f); trt.anchoredPosition = Vector2.zero;
        trt.sizeDelta = new Vector2(HAND_WIDTH * 2.8f, 12f);
        _handTipImage = tipGO.AddComponent<Image>();
        _handTipImage.sprite = _arrowSprite;
        _handTipImage.color = HAND_GOLD; _handTipImage.raycastTarget = false;
        _handTipImage.preserveAspect = true;
    }

    private void BuildCenterPivot()
    {
        GameObject glowGO = CenteredGO("PivotGlow", _clockFace.transform, PIVOT_SIZE * 3.5f, PIVOT_SIZE * 3.5f);
        _pivotGlowImg = glowGO.AddComponent<Image>();
        _pivotGlowImg.sprite = _softGlowSprite; _pivotGlowImg.color = PIVOT_GLOW_GOLD; _pivotGlowImg.raycastTarget = false;

        GameObject coreGO = CenteredGO("PivotCore", _clockFace.transform, PIVOT_SIZE, PIVOT_SIZE);
        _pivotCore = coreGO.AddComponent<Image>();
        _pivotCore.sprite = _circleSprite; _pivotCore.color = PIVOT_CORE_GOLD; _pivotCore.raycastTarget = false;
    }

    private void BuildFeedbackLabel(Transform parent)
    {
        GameObject go = new GameObject("ClockFeedback"); go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.2f, 0.55f); rt.anchorMax = new Vector2(0.8f, 0.65f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        _feedbackTMP = go.AddComponent<TextMeshProUGUI>();
        _feedbackTMP.fontSize = 42f; _feedbackTMP.color = FEEDBACK_GREEN;
        _feedbackTMP.alignment = TextAlignmentOptions.Center; _feedbackTMP.characterSpacing = 8f;
        _feedbackTMP.raycastTarget = false; CinzelFontHelper.Apply(_feedbackTMP, true);
        go.SetActive(false);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  SPRITE GENERATION
    // ══════════════════════════════════════════════════════════════════════

    private void CreateSprites()
    {
        _circleSprite     = GenerateCircle(128);
        _softGlowSprite   = GenerateSoftGlow(128);
        _dotSprite        = GenerateSoftGlow(32);
        _cosmicDiscSprite = GenerateCosmicDisc(256);
        _ringSprite       = GenerateRing(128, RING_INNER_F, RING_OUTER_F);
        _arrowSprite      = GenerateArrow(32, 48);
    }

    private static Sprite GenerateCircle(int sz)
    {
        Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        float h = sz * 0.5f; Color[] px = new Color[sz * sz];
        for (int y = 0; y < sz; y++) for (int x = 0; x < sz; x++)
            { float d = Dist(x, y, h) / h; px[y * sz + x] = d > 1f ? Color.clear : new Color(1, 1, 1, 1f - Mathf.Clamp01((d - 0.96f) / 0.04f)); }
        tex.SetPixels(px); tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), Vector2.one * 0.5f);
    }

    private static Sprite GenerateSoftGlow(int sz)
    {
        Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        float h = sz * 0.5f; Color[] px = new Color[sz * sz];
        for (int y = 0; y < sz; y++) for (int x = 0; x < sz; x++)
            { float d = Dist(x, y, h) / h; float a = Mathf.Exp(-d * d * 5f); if (d > 0.85f) a *= Mathf.Clamp01((1f - d) / 0.15f); px[y * sz + x] = new Color(1, 1, 1, Mathf.Clamp01(a)); }
        tex.SetPixels(px); tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), Vector2.one * 0.5f);
    }

    private static Sprite GenerateCosmicDisc(int sz)
    {
        Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        float h = sz * 0.5f; Color[] px = new Color[sz * sz];
        System.Random rng = new System.Random(42);
        int sc = 25; float[] sX = new float[sc], sY = new float[sc];
        for (int s = 0; s < sc; s++) { float r = (float)rng.NextDouble() * 0.82f, a = (float)rng.NextDouble() * Mathf.PI * 2f; sX[s] = h + Mathf.Cos(a) * r * h; sY[s] = h + Mathf.Sin(a) * r * h; }
        for (int y = 0; y < sz; y++) for (int x = 0; x < sz; x++)
        {
            float d = Dist(x, y, h) / h;
            if (d > 1f) { px[y * sz + x] = Color.clear; continue; }
            float aa = 1f - Mathf.Clamp01((d - 0.96f) / 0.04f);
            float br = Mathf.Lerp(0.10f, 0.03f, d * d);
            float edge = Mathf.Clamp01((d - 0.4f) / 0.6f);
            float cr = br * (1f - edge * 0.35f), cg = br * (1f - edge * 0.25f), cb = br * (1f + edge * 0.50f);
            for (int s = 0; s < sc; s++)
            { float dx2 = x - sX[s], dy2 = y - sY[s]; float star = Mathf.Exp(-(dx2 * dx2 + dy2 * dy2) / 3.5f) * 0.12f; cr += star; cg += star * 0.92f; cb += star * 0.80f; }
            px[y * sz + x] = new Color(cr, cg, cb, aa);
        }
        tex.SetPixels(px); tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), Vector2.one * 0.5f);
    }

    private static Sprite GenerateRing(int sz, float innerF, float outerF)
    {
        Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        float h = sz * 0.5f, iR = innerF * h, oR = outerF * h, soft = 2.5f;
        Color[] px = new Color[sz * sz];
        for (int y = 0; y < sz; y++) for (int x = 0; x < sz; x++)
        {
            float d = Dist(x, y, h);
            float iA = Mathf.Clamp01((d - iR) / soft), oA = Mathf.Clamp01((oR - d) / soft);
            float a = iA * oA;
            if (d > oR) { float gd = (d - oR) / (h * 0.10f); a = Mathf.Max(a, Mathf.Exp(-gd * gd * 2f) * 0.18f); }
            a *= 1f - Mathf.Clamp01((d / h - 0.92f) / 0.08f);
            px[y * sz + x] = new Color(1, 1, 1, Mathf.Clamp01(a));
        }
        tex.SetPixels(px); tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), Vector2.one * 0.5f);
    }

    /// <summary>Generates a filled triangle/arrowhead pointing upward.</summary>
    private static Sprite GenerateArrow(int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        Color[] px = new Color[w * h];
        float hw = w * 0.5f;
        for (int y = 0; y < h; y++)
        {
            // Triangle: width narrows linearly from base (y=0) to tip (y=h-1)
            float progress = (float)y / (h - 1); // 0 at base, 1 at tip
            float halfWidth = hw * (1f - progress);
            for (int x = 0; x < w; x++)
            {
                float dx = Mathf.Abs(x - hw + 0.5f);
                if (dx <= halfWidth)
                {
                    // Anti-alias at edge
                    float aa = Mathf.Clamp01((halfWidth - dx) / 1.2f);
                    px[y * w + x] = new Color(1f, 1f, 1f, aa);
                }
                else
                {
                    px[y * w + x] = Color.clear;
                }
            }
        }
        tex.SetPixels(px); tex.Apply(false, true);
        // Pivot at bottom-center so it attaches to the top of the hand bar
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f));
    }

    // ══════════════════════════════════════════════════════════════════════
    //  HELPERS
    // ══════════════════════════════════════════════════════════════════════

    private static float Dist(int x, int y, float h) { float dx = x - h + .5f, dy = y - h + .5f; return Mathf.Sqrt(dx * dx + dy * dy); }
    private static int AngleToHourIndex(float a) { a = ((a % 360f) + 360f) % 360f; return Mathf.RoundToInt(a / 30f) % HOUR_COUNT; }
    private static bool IsMajorHour(int i) => i == 0 || i == 3 || i == 6 || i == 9;
    private static string HourToTimeStr(int i) { int d = i == 0 ? 12 : i; return d + ":00"; }
    private static float RandRange(System.Random r, float min, float max) => min + (float)r.NextDouble() * (max - min);

    private static GameObject CenteredGO(string n, Transform p, float w, float h)
    {
        GameObject go = new GameObject(n); go.transform.SetParent(p, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(w, h);
        return go;
    }

    private static GameObject StretchedGO(string n, Transform p)
    {
        GameObject go = new GameObject(n); go.transform.SetParent(p, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        return go;
    }
}
