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

    // Boss pointer color — cyan so it's clearly different from player
    private static readonly Color BOSS_COL   = new Color(0.2f, 1f, 0.9f, 1f);

    private bool  _built;
    private float _display;

    private Image           _posFill, _negFill, _posMarker, _dangerL, _dangerR, _warnL, _warnR;
    private RectTransform   _posFillRT, _negFillRT, _posMarkerRT;
    private TextMeshProUGUI _valueTMP, _dirLabel;

    // Boss pointer
    private Image         _bossMarker;
    private RectTransform _bossMarkerRT;
    private float         _bossPointerValue = 0f;
    private float         _bossMinV = -10f;
    private float         _bossMaxV = 10f;

    /// <summary>Called by BossBFight every frame to update boss pointer position.</summary>
    public void SetBossPointer(float value, float min, float max)
    {
        _bossPointerValue = value;
        _bossMinV = min;
        _bossMaxV = max;
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

        if (state == TimeState.State.Frozen)
            _display = raw;
        else
            _display = Mathf.Lerp(_display, raw, fillLerpSpeed * Time.deltaTime);

        float totalRange = maxV - minV;
        float zeroAnchor = totalRange > 0f ? (-minV / totalRange) : 0.5f;
        float valNorm    = totalRange > 0f ? (_display - minV) / totalRange : 0.5f;
        valNorm = Mathf.Clamp01(valNorm);

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

        if (_posMarkerRT != null)
        {
            _posMarkerRT.anchorMin = new Vector2(valNorm - 0.004f, -0.3f);
            _posMarkerRT.anchorMax = new Vector2(valNorm + 0.004f,  1.3f);
            _posMarkerRT.offsetMin = _posMarkerRT.offsetMax = Vector2.zero;
        }

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

        bool bossActive = BossFight.Instance != null && BossFight.Instance.bossActive;
        bool bossBActive = BossBFight.Instance != null && BossBFight.Instance.bossActive;
        bool showZones = bossActive || bossBActive || AlwaysShowZones;

        if (bossActive)
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

        bool inWarning = bossActive && threat >= TimeScaleLogic.ThreatState.Warning;
        float wp = inWarning ? Mathf.Sin(Time.time * 3f) * 0.15f + 0.25f : 0.08f;
        if (_warnR != null) { _warnR.enabled = showZones; Color c = WARN_COL; c.a = wp * (raw >= wrn ? 1f : 0.3f); _warnR.color = c; }
        if (_warnL != null) { _warnL.enabled = showZones; Color c = WARN_COL; c.a = wp * (raw <= -wrn ? 1f : 0.3f); _warnL.color = c; }

        bool inDanger = bossActive && threat >= TimeScaleLogic.ThreatState.Danger;
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

        // ── Boss B pointer ───────────────────────────────────────────────
        if (_bossMarkerRT != null && _bossMarker != null)
        {
            _bossMarker.enabled = bossBActive;

            if (bossBActive)
            {
                float bossRange = _bossMaxV - _bossMinV;
                float bossNorm = bossRange > 0f
                    ? Mathf.Clamp01((_bossPointerValue - _bossMinV) / bossRange)
                    : 0.5f;

                _bossMarkerRT.anchorMin = new Vector2(bossNorm - 0.004f, -0.5f);
                _bossMarkerRT.anchorMax = new Vector2(bossNorm + 0.004f,  1.5f);
                _bossMarkerRT.offsetMin = _bossMarkerRT.offsetMax = Vector2.zero;

                // Pulse the boss marker
                float g = (Mathf.Sin(Time.time * GLOW_SPEED * 1.5f) + 1f) * 0.5f;
                Color bc = BOSS_COL;
                bc.a = Mathf.Lerp(0.7f, 1f, g);
                _bossMarker.color = bc;
            }
        }
    }

    public void SetValue(float value) { }

    public bool IsInDanger() => TimeScaleLogic.Instance != null
        && TimeScaleLogic.Instance.CurrentThreatState >= TimeScaleLogic.ThreatState.Danger;

    public bool IsInWarning() => TimeScaleLogic.Instance != null
        && TimeScaleLogic.Instance.CurrentThreatState >= TimeScaleLogic.ThreatState.Warning;

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

        _valueTMP = MkT("Val", transform, V(0.04f, 0.60f), V(0.96f, 0.95f),
            "+0.0", 17f, VALUE_COL, TextAlignmentOptions.Center, true);

        GameObject barGO = MkR("Bar", transform, V(0.04f, 0.22f), V(0.96f, 0.55f));
        I(barGO, BAR_BG);

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

        _warnL = I(MkR("WL", barGO.transform, V(0, 0), V(Mathf.Max(wLo, 0), 1)),
            new Color(WARN_COL.r, WARN_COL.g, WARN_COL.b, 0.08f));
        _warnR = I(MkR("WR", barGO.transform, V(Mathf.Min(wHi, 1), 0), V(1, 1)),
            new Color(WARN_COL.r, WARN_COL.g, WARN_COL.b, 0.08f));

        _dangerL = I(MkR("DL", barGO.transform, V(0, 0), V(Mathf.Max(dLo, 0), 1)),
            new Color(DANGER_COL.r, DANGER_COL.g, DANGER_COL.b, 0.15f));
        _dangerR = I(MkR("DR", barGO.transform, V(Mathf.Min(dHi, 1), 0), V(1, 1)),
            new Color(DANGER_COL.r, DANGER_COL.g, DANGER_COL.b, 0.15f));

        _negFill = I(MkR("NF", barGO.transform, V(zA, 0.1f), V(zA, 0.9f)), REV_COL);
        _negFillRT = _negFill.GetComponent<RectTransform>();
        _posFill = I(MkR("PF", barGO.transform, V(zA, 0.1f), V(zA, 0.9f)), FWD_COL);
        _posFillRT = _posFill.GetComponent<RectTransform>();

        I(MkR("ZL", barGO.transform, V(zA - 0.002f, -0.1f), V(zA + 0.002f, 1.1f)), ZERO_COL);

        _posMarker = I(MkR("PM", barGO.transform, V(zA - 0.004f, -0.3f), V(zA + 0.004f, 1.3f)), FWD_COL);
        _posMarkerRT = _posMarker.GetComponent<RectTransform>();

        // ── Boss pointer marker (cyan, slightly taller than player marker) ──
        _bossMarker = I(MkR("BM", barGO.transform, V(zA - 0.004f, -0.5f), V(zA + 0.004f, 1.5f)), BOSS_COL);
        _bossMarkerRT = _bossMarker.GetComponent<RectTransform>();
        _bossMarker.enabled = false;

        string minS = Mathf.RoundToInt(minV).ToString();
        string maxS = (maxV > 0 ? "+" : "") + Mathf.RoundToInt(maxV).ToString();
        MkT("Mn", transform, V(0.04f, 0), V(0.20f, 0.22f), minS, 13f, LABEL_COL, TextAlignmentOptions.Left);
        MkT("Zr", transform, V(zA - 0.04f, 0), V(zA + 0.04f, 0.22f), "0", 13f, ZERO_COL, TextAlignmentOptions.Center);
        MkT("Mx", transform, V(0.80f, 0), V(0.96f, 0.22f), maxS, 13f, LABEL_COL, TextAlignmentOptions.Right);
    }

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