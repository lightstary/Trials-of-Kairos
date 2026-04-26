using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TimeScaleMeter : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float fillLerpSpeed = 12f;

    public bool AlwaysShowZones { get; set; }

    private const float BAR_W = 480f;
    private const float TOTAL_H = 60f;
    private const float GLOW_SPEED = 2.5f;

    // ── Standard colors ─────────────────────────────────────────────────
    private static readonly Color BG_COL     = new Color(0.020f, 0.025f, 0.050f, 0.80f);
    private static readonly Color BAR_BG     = new Color(0.050f, 0.060f, 0.100f, 0.90f);
    private static readonly Color FWD_COL    = new Color(0.961f, 0.784f, 0.259f, 1f);
    private static readonly Color FRZ_COL    = new Color(0.353f, 0.706f, 0.941f, 1f);
    private static readonly Color REV_COL    = new Color(0.608f, 0.365f, 0.898f, 1f);
    private static readonly Color DANGER_COL = new Color(0.898f, 0.196f, 0.106f, 1f);
    private static readonly Color ZERO_COL   = new Color(0.910f, 0.918f, 0.965f, 0.45f);
    private static readonly Color LABEL_COL  = new Color(0.910f, 0.918f, 0.965f, 0.55f);
    private static readonly Color VALUE_COL  = new Color(0.910f, 0.918f, 0.965f, 0.95f);
    private static readonly Color WARN_COL   = new Color(0.961f, 0.596f, 0.106f, 1f);

    // ── Boss B colors ───────────────────────────────────────────────────
    private static readonly Color BOSS_COL       = new Color(0.2f, 1f, 0.9f, 1f);
    private static readonly Color CYAN_BG        = new Color(0f, 0.85f, 0.95f, 0.25f);
    private static readonly Color MAGENTA_BG     = new Color(0.95f, 0.20f, 0.65f, 0.25f);
    private static readonly Color DEATH_PTR_COL  = new Color(1f, 0.15f, 0.55f, 1f);
    private static readonly Color CONTEST_COL    = new Color(0.2f, 1f, 0.5f, 0.6f);

    private bool  _built;
    private float _display;

    // ── Standard meter elements ─────────────────────────────────────────
    private Image           _posFill, _negFill, _posMarker, _dangerL, _dangerR, _warnL, _warnR;
    private RectTransform   _posFillRT, _negFillRT, _posMarkerRT;
    private TextMeshProUGUI _valueTMP;
    private Image           _barBgImage;
    private GameObject      _barGO;

    // ── Boss pointer (shared for A / C) ─────────────────────────────────
    private Image         _bossMarker;
    private RectTransform _bossMarkerRT;
    private Image         _bossGlowRing;
    private RectTransform _bossGlowRingRT;
    private float         _bossPointerValue;
    private float         _bossMinV = -10f;
    private float         _bossMaxV = 10f;
    private bool          _bossContesting;
    private Image         _contestFlash;

    // ── Boss B dedicated elements ───────────────────────────────────────
    private Image           _bossBCyanBg, _bossBMagentaBg;
    private Image           _deathPtrMarker;
    private RectTransform   _deathPtrMarkerRT;
    private bool            _wasBossBActive;

    /// <summary>Called by BossBFight every frame to update boss pointer position and contesting state.</summary>
    public void SetBossPointer(float value, float min, float max, bool contesting = false)
    {
        _bossPointerValue = value;
        _bossMinV = min;
        _bossMaxV = max;
        _bossContesting = contesting;
    }

    void Update()
    {
        if (!_built) Build();

        float raw = 0f, minV = -10f, maxV = 10f, dng = 8f, wrn = 5f;
        TimeState.State state = TimeState.State.Frozen;
        TimeScaleLogic.ThreatState threat = TimeScaleLogic.ThreatState.Safe;

        if (TimeScaleLogic.Instance != null)
        {
            raw  = TimeScaleLogic.Instance.CurrentValue;
            minV = TimeScaleLogic.Instance.minValue;
            maxV = TimeScaleLogic.Instance.maxValue;
            dng  = TimeScaleLogic.Instance.dangerZone;
            wrn  = TimeScaleLogic.Instance.warningZone;
            threat = TimeScaleLogic.Instance.CurrentThreatState;
        }
        if (TimeState.Instance != null)
            state = TimeState.Instance.currentState;

        _display = raw;

        float totalRange = maxV - minV;
        float zeroAnchor = totalRange > 0f ? (-minV / totalRange) : 0.5f;
        float valNorm    = totalRange > 0f ? (_display - minV) / totalRange : 0.5f;
        valNorm = Mathf.Clamp01(valNorm);

        // ── Player fill bars ────────────────────────────────────────────
        if (_posFillRT != null)
        {
            float end = Mathf.Max(valNorm, zeroAnchor);
            _posFillRT.anchorMin = new Vector2(zeroAnchor, 0.1f);
            _posFillRT.anchorMax = new Vector2(end, 0.9f);
            _posFillRT.offsetMin = _posFillRT.offsetMax = Vector2.zero;
        }
        if (_negFillRT != null)
        {
            float start = Mathf.Min(valNorm, zeroAnchor);
            _negFillRT.anchorMin = new Vector2(start, 0.1f);
            _negFillRT.anchorMax = new Vector2(zeroAnchor, 0.9f);
            _negFillRT.offsetMin = _negFillRT.offsetMax = Vector2.zero;
        }
        if (_posFill != null) _posFill.enabled = _display >= 0f;
        if (_negFill != null) _negFill.enabled = _display < 0f;

        // ── Player marker ───────────────────────────────────────────────
        if (_posMarkerRT != null)
        {
            _posMarkerRT.anchorMin = new Vector2(valNorm - 0.004f, -0.3f);
            _posMarkerRT.anchorMax = new Vector2(valNorm + 0.004f,  1.3f);
            _posMarkerRT.offsetMin = _posMarkerRT.offsetMax = Vector2.zero;
        }

        // ── Value text ──────────────────────────────────────────────────
        if (_valueTMP != null)
        {
            string sign = raw > 0.005f ? "+" : "";
            _valueTMP.text = $"{sign}{raw:F1}";
        }

        Color stateCol = FWD_COL;
        switch (state)
        {
            case TimeState.State.Forward: stateCol = FWD_COL; break;
            case TimeState.State.Frozen:  stateCol = FRZ_COL; break;
            case TimeState.State.Reverse: stateCol = REV_COL; break;
        }
        if (_posFill != null) _posFill.color = stateCol;
        if (_negFill != null) _negFill.color = stateCol;

        bool bossActive  = BossFight.Instance  != null && BossFight.Instance.bossActive;
        bool bossBActive = BossBFight.Instance != null && BossBFight.Instance.bossActive;
        bool bossCActive = BossCFight.Instance != null && BossCFight.Instance.bossActive;
        bool anyBossActive = bossActive || bossBActive || bossCActive;
        bool showZones = anyBossActive || AlwaysShowZones;

        // ── Threat coloring ─────────────────────────────────────────────
        if (anyBossActive)
        {
            float absDisplay = Mathf.Abs(_display);
            if (absDisplay >= dng)
            {
                if (_posFill != null && _display >= 0f) _posFill.color = DANGER_COL;
                if (_negFill != null && _display <  0f) _negFill.color = DANGER_COL;
            }
            else if (absDisplay >= wrn)
            {
                if (_posFill != null && _display >= 0f) _posFill.color = WARN_COL;
                if (_negFill != null && _display <  0f) _negFill.color = WARN_COL;
            }
        }

        bool inWarning = anyBossActive && threat >= TimeScaleLogic.ThreatState.Warning;
        float wp = inWarning ? Mathf.Sin(Time.time * 3f) * 0.15f + 0.25f : 0.08f;
        if (_warnR != null) { _warnR.enabled = showZones; Color c = WARN_COL; c.a = wp * (raw >= wrn ? 1f : 0.3f); _warnR.color = c; }
        if (_warnL != null) { _warnL.enabled = showZones; Color c = WARN_COL; c.a = wp * (raw <= -wrn ? 1f : 0.3f); _warnL.color = c; }

        bool inDanger = anyBossActive && threat >= TimeScaleLogic.ThreatState.Danger;
        float dp = inDanger ? Mathf.Sin(Time.time * 6f) * 0.3f + 0.7f : 0.15f;
        if (_dangerR != null) { _dangerR.enabled = showZones; Color c = DANGER_COL; c.a = dp * (raw >= dng ? 1f : 0.3f); _dangerR.color = c; }
        if (_dangerL != null) { _dangerL.enabled = showZones; Color c = DANGER_COL; c.a = dp * (raw <= -dng ? 1f : 0.3f); _dangerL.color = c; }

        if (_posMarker != null)
        {
            float g = (Mathf.Sin(Time.time * GLOW_SPEED) + 1f) * 0.5f;
            Color mc;
            if (inDanger) mc = DANGER_COL;
            else if (inWarning) mc = WARN_COL;
            else mc = stateCol;
            mc.a = Mathf.Lerp(0.75f, 1f, g);
            _posMarker.color = mc;
        }

        // ═════════════════════════════════════════════════════════════════
        //  BOSS B — DEATH POINTER
        // ═════════════════════════════════════════════════════════════════
        if (bossBActive)
        {
            // Show cyan/magenta split background
            if (_bossBCyanBg != null)    _bossBCyanBg.enabled = true;
            if (_bossBMagentaBg != null)  _bossBMagentaBg.enabled = true;
            if (_barBgImage != null)      _barBgImage.color = new Color(0.02f, 0.03f, 0.06f, 0.95f);

            // Normalized boss pointer position on bar
            float bossRange = _bossMaxV - _bossMinV;
            float bossNorm = bossRange > 0f
                ? Mathf.Clamp01((_bossPointerValue - _bossMinV) / bossRange)
                : 0.5f;

            // Death pointer marker: wide bar, contained inside the bar area
            if (_deathPtrMarkerRT != null && _deathPtrMarker != null)
            {
                _deathPtrMarker.enabled = true;
                float halfW = 0.008f;
                _deathPtrMarkerRT.anchorMin = new Vector2(bossNorm - halfW, 0f);
                _deathPtrMarkerRT.anchorMax = new Vector2(bossNorm + halfW, 1f);
                _deathPtrMarkerRT.offsetMin = _deathPtrMarkerRT.offsetMax = Vector2.zero;

                // Fast pulse when uncontested, calm when blocked
                float pulseRate = _bossContesting ? 2f : 5f;
                float g = (Mathf.Sin(Time.time * pulseRate) + 1f) * 0.5f;
                Color dc = _bossContesting
                    ? Color.Lerp(DEATH_PTR_COL, CONTEST_COL, 0.4f)
                    : DEATH_PTR_COL;
                dc.a = Mathf.Lerp(0.80f, 1f, g);
                _deathPtrMarker.color = dc;
            }
        }
        else
        {
            // Hide Boss B elements
            if (_bossBCyanBg != null)    _bossBCyanBg.enabled = false;
            if (_bossBMagentaBg != null)  _bossBMagentaBg.enabled = false;
            if (_deathPtrMarker != null)  _deathPtrMarker.enabled = false;

            if (_wasBossBActive && _barBgImage != null)
                _barBgImage.color = BAR_BG;
        }

        _wasBossBActive = bossBActive;

        // ═════════════════════════════════════════════════════════════════
        //  LEGACY BOSS POINTER (Boss A / C thin cyan marker)
        // ═════════════════════════════════════════════════════════════════
        if (_bossMarkerRT != null && _bossMarker != null)
        {
            bool showLegacy = (bossActive || bossCActive) && !bossBActive;
            _bossMarker.enabled = showLegacy;

            if (showLegacy)
            {
                float bossRange = _bossMaxV - _bossMinV;
                float bossNorm = bossRange > 0f
                    ? Mathf.Clamp01((_bossPointerValue - _bossMinV) / bossRange)
                    : 0.5f;

                _bossMarkerRT.anchorMin = new Vector2(bossNorm - 0.004f, -0.5f);
                _bossMarkerRT.anchorMax = new Vector2(bossNorm + 0.004f,  1.5f);
                _bossMarkerRT.offsetMin = _bossMarkerRT.offsetMax = Vector2.zero;

                float pulseSpeed = _bossContesting ? GLOW_SPEED : GLOW_SPEED * 2f;
                float g = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
                Color bc = _bossContesting
                    ? Color.Lerp(BOSS_COL, CONTEST_COL, 0.5f)
                    : BOSS_COL;
                bc.a = Mathf.Lerp(0.7f, 1f, g);
                _bossMarker.color = bc;
            }
        }

        // ── Boss pointer glow ring (tutorial only) ──────────────────────
        if (_bossGlowRingRT != null && _bossGlowRing != null)
        {
            bool showGlow = BossBFight._showPointerGlow;
            _bossGlowRing.enabled = showGlow;

            if (showGlow)
            {
                float bossRange2 = _bossMaxV - _bossMinV;
                float bossNorm2 = bossRange2 > 0f
                    ? Mathf.Clamp01((_bossPointerValue - _bossMinV) / bossRange2)
                    : 0.5f;

                _bossGlowRingRT.anchorMin = new Vector2(bossNorm2 - 0.03f, -1.2f);
                _bossGlowRingRT.anchorMax = new Vector2(bossNorm2 + 0.03f,  2.2f);
                _bossGlowRingRT.offsetMin = _bossGlowRingRT.offsetMax = Vector2.zero;

                float gp = (Mathf.Sin(Time.unscaledTime * 4f) + 1f) * 0.5f;
                Color gc = BOSS_COL;
                gc.a = Mathf.Lerp(0.3f, 0.7f, gp);
                _bossGlowRing.color = gc;
            }
        }

        // ── Contesting flash overlay ────────────────────────────────────
        if (_contestFlash != null)
        {
            bool showContest = anyBossActive && _bossContesting;
            _contestFlash.enabled = showContest;
            if (showContest)
            {
                float cf = (Mathf.Sin(Time.time * 4f) + 1f) * 0.5f;
                Color cc = CONTEST_COL;
                cc.a = Mathf.Lerp(0.02f, 0.08f, cf);
                _contestFlash.color = cc;
            }
        }
    }

    public void SetValue(float value) { }

    public bool IsInDanger() => TimeScaleLogic.Instance != null
        && TimeScaleLogic.Instance.CurrentThreatState >= TimeScaleLogic.ThreatState.Danger;

    public bool IsInWarning() => TimeScaleLogic.Instance != null
        && TimeScaleLogic.Instance.CurrentThreatState >= TimeScaleLogic.ThreatState.Warning;

    // ════════════════════════════════════════════════════════════════════
    //  BUILD
    // ════════════════════════════════════════════════════════════════════

    private void Build()
    {
        _built = true;

        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        RectTransform myRT = GetComponent<RectTransform>();
        if (myRT != null)
        {
            myRT.anchorMin = new Vector2(0.5f, 1f);
            myRT.anchorMax = new Vector2(0.5f, 1f);
            myRT.pivot = new Vector2(0.5f, 1f);
            myRT.anchoredPosition = new Vector2(0f, -8f);
            myRT.sizeDelta = new Vector2(BAR_W + 40f, TOTAL_H);
        }

        Image bg = GetComponent<Image>();
        if (bg == null) bg = gameObject.AddComponent<Image>();
        bg.color = BG_COL; bg.raycastTarget = false;

        // Value text — top row
        _valueTMP = MkT("Val", transform, V(0.04f, 0.60f), V(0.96f, 0.95f),
            "+0.0", 17f, VALUE_COL, TextAlignmentOptions.Center, true);

        // Bar — middle row
        _barGO = MkR("Bar", transform, V(0.04f, 0.22f), V(0.96f, 0.55f));
        _barBgImage = I(_barGO, BAR_BG);

        float minV = TimeScaleLogic.Instance != null ? TimeScaleLogic.Instance.minValue : -10f;
        float maxV = TimeScaleLogic.Instance != null ? TimeScaleLogic.Instance.maxValue :  10f;
        float dng  = TimeScaleLogic.Instance != null ? TimeScaleLogic.Instance.dangerZone : 8f;
        float wrn  = TimeScaleLogic.Instance != null ? TimeScaleLogic.Instance.warningZone : 5f;
        float total = maxV - minV;
        float zA = total > 0 ? (-minV / total) : 0.5f;
        float dHi = total > 0 ? Mathf.Clamp01((dng - minV) / total) : 0.8f;
        float dLo = total > 0 ? Mathf.Clamp01((-dng - minV) / total) : 0.2f;
        float wHi = total > 0 ? Mathf.Clamp01((wrn - minV) / total) : 0.75f;
        float wLo = total > 0 ? Mathf.Clamp01((-wrn - minV) / total) : 0.25f;

        // ── Boss B: cyan/magenta split backgrounds (hidden until fight) ──
        _bossBCyanBg = I(MkR("BCyan", _barGO.transform, V(0, 0), V(zA, 1)), CYAN_BG);
        _bossBCyanBg.enabled = false;
        _bossBMagentaBg = I(MkR("BMag", _barGO.transform, V(zA, 0), V(1, 1)), MAGENTA_BG);
        _bossBMagentaBg.enabled = false;

        // Warning / danger zone overlays
        _warnL = I(MkR("WL", _barGO.transform, V(0, 0), V(Mathf.Max(wLo, 0), 1)),
            new Color(WARN_COL.r, WARN_COL.g, WARN_COL.b, 0.08f));
        _warnR = I(MkR("WR", _barGO.transform, V(Mathf.Min(wHi, 1), 0), V(1, 1)),
            new Color(WARN_COL.r, WARN_COL.g, WARN_COL.b, 0.08f));

        _dangerL = I(MkR("DL", _barGO.transform, V(0, 0), V(Mathf.Max(dLo, 0), 1)),
            new Color(DANGER_COL.r, DANGER_COL.g, DANGER_COL.b, 0.15f));
        _dangerR = I(MkR("DR", _barGO.transform, V(Mathf.Min(dHi, 1), 0), V(1, 1)),
            new Color(DANGER_COL.r, DANGER_COL.g, DANGER_COL.b, 0.15f));

        // Fill bars
        _negFill = I(MkR("NF", _barGO.transform, V(zA, 0.1f), V(zA, 0.9f)), REV_COL);
        _negFillRT = _negFill.GetComponent<RectTransform>();
        _posFill = I(MkR("PF", _barGO.transform, V(zA, 0.1f), V(zA, 0.9f)), FWD_COL);
        _posFillRT = _posFill.GetComponent<RectTransform>();

        // Zero line
        I(MkR("ZL", _barGO.transform, V(zA - 0.002f, -0.1f), V(zA + 0.002f, 1.1f)), ZERO_COL);

        // Player marker
        _posMarker = I(MkR("PM", _barGO.transform, V(zA - 0.004f, -0.3f), V(zA + 0.004f, 1.3f)), FWD_COL);
        _posMarkerRT = _posMarker.GetComponent<RectTransform>();

        // ── Boss B: Death Pointer marker (contained inside bar) ─────────
        _deathPtrMarker = I(MkR("DP", _barGO.transform, V(zA - 0.008f, 0f), V(zA + 0.008f, 1f)), DEATH_PTR_COL);
        _deathPtrMarkerRT = _deathPtrMarker.GetComponent<RectTransform>();
        _deathPtrMarker.enabled = false;

        // ── Legacy boss marker (Boss A / C only) ────────────────────────
        _bossMarker = I(MkR("BM", _barGO.transform, V(zA - 0.004f, -0.5f), V(zA + 0.004f, 1.5f)), BOSS_COL);
        _bossMarkerRT = _bossMarker.GetComponent<RectTransform>();
        _bossMarker.enabled = false;

        _bossGlowRing = I(MkR("BossGlow", _barGO.transform, V(zA - 0.03f, -1.2f), V(zA + 0.03f, 2.2f)),
            new Color(BOSS_COL.r, BOSS_COL.g, BOSS_COL.b, 0f));
        _bossGlowRingRT = _bossGlowRing.GetComponent<RectTransform>();
        _bossGlowRing.enabled = false;

        // Contest flash
        _contestFlash = I(MkR("ContestFlash", _barGO.transform, V(0, 0), V(1, 1)),
            new Color(CONTEST_COL.r, CONTEST_COL.g, CONTEST_COL.b, 0f));
        _contestFlash.enabled = false;

        // ── Bottom labels ───────────────────────────────────────────────
        string minS = Mathf.RoundToInt(minV).ToString();
        string maxS = (maxV > 0 ? "+" : "") + Mathf.RoundToInt(maxV).ToString();
        MkT("Mn", transform, V(0.04f, 0), V(0.20f, 0.22f), minS, 13f, LABEL_COL, TextAlignmentOptions.Left);
        MkT("Zr", transform, V(zA - 0.04f, 0), V(zA + 0.04f, 0.22f), "0", 13f, ZERO_COL, TextAlignmentOptions.Center);
        MkT("Mx", transform, V(0.80f, 0), V(0.96f, 0.22f), maxS, 13f, LABEL_COL, TextAlignmentOptions.Right);
    }

    // ════════════════════════════════════════════════════════════════════
    //  HELPERS
    // ════════════════════════════════════════════════════════════════════

    private static Vector2 V(float x, float y) => new Vector2(x, y);

    private GameObject MkR(string n, Transform p, Vector2 mn, Vector2 mx)
    {
        GameObject go = new GameObject(n); go.transform.SetParent(p, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = mn; rt.anchorMax = mx;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return go;
    }

    private Image I(GameObject go, Color c)
    { Image i = go.AddComponent<Image>(); i.color = c; i.raycastTarget = false; return i; }

    private TextMeshProUGUI MkT(string n, Transform p, Vector2 mn, Vector2 mx,
        string txt, float sz, Color c, TextAlignmentOptions a, bool b = false)
    {
        GameObject go = MkR(n, p, mn, mx);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.text = txt; t.fontSize = sz; t.color = c; t.alignment = a;
        t.fontStyle = b ? FontStyles.Bold : FontStyles.Normal;
        t.overflowMode = TextOverflowModes.Overflow; t.raycastTarget = false;
        CinzelFontHelper.Apply(t, b);
        return t;
    }
}
