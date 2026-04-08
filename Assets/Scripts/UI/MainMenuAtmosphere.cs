using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Provides atmospheric background visuals: two slowly rotating gold squares
/// and a layer of drifting grain/dust particles with a twinkle effect.
/// Works in unscaled time so it animates during timeScale = 0 menus.
///
/// Add to any UI screen root (MainMenu, TrialSelectScreen, ControlsScreen).
/// Call DisableSubtitleBreathing() from MainMenuController to prevent
/// this component from interfering with subtitle fade ownership.
/// </summary>
public class MainMenuAtmosphere : MonoBehaviour
{
    [Header("Rotating Squares")]
    [SerializeField] private float cwSize        = 420f;
    [SerializeField] private float cwSpeed       = 11f;
    [SerializeField] private Color squareCWColor  = new Color(1f, 0.843f, 0f, 0.06f);

    [SerializeField] private float ccwSize       = 680f;
    [SerializeField] private float ccwSpeed      = 6.5f;
    [SerializeField] private Color squareCCWColor = new Color(1f, 0.843f, 0f, 0.035f);

    [Header("Grain / Dust")]
    [SerializeField] private int   grainCount    = 50;
    [SerializeField] private float grainMinSize  = 1f;
    [SerializeField] private float grainMaxSize  = 4f;
    [SerializeField] private float grainMinSpeed = 8f;
    [SerializeField] private float grainMaxSpeed = 22f;
    [SerializeField] private Color grainColor    = new Color(0.95f, 0.82f, 0.35f, 0.45f);

    // ── State ─────────────────────────────────────────────────────────────────
    private RectTransform   _cwSquare;
    private RectTransform   _ccwSquare;
    private RectTransform[] _grains;
    private Image[]         _grainImgs;
    private float[]         _grainBaseAlpha;
    private Vector2[]       _grainVelocities;
    private float[]         _grainPhase;
    private float           _w = 1920f;
    private float           _h = 1080f;
    private bool            _subtitleBreathingEnabled = true;

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Prevents this component from touching the subtitle label.</summary>
    public void DisableSubtitleBreathing() => _subtitleBreathingEnabled = false;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null)
        {
            _w = rt.rect.width  > 10f ? rt.rect.width  : 1920f;
            _h = rt.rect.height > 10f ? rt.rect.height : 1080f;
        }

        MakeSquare(ref _cwSquare,  "CW_Square",  cwSize,  squareCWColor);
        MakeSquare(ref _ccwSquare, "CCW_Square", ccwSize, squareCCWColor);
        MakeGrains();
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;

        // Rotate squares
        if (_cwSquare  != null) _cwSquare.Rotate(0f, 0f,  cwSpeed  * dt);
        if (_ccwSquare != null) _ccwSquare.Rotate(0f, 0f, -ccwSpeed * dt);

        // Drift grain particles
        DriftGrains(dt);
    }

    // ── Square builders ───────────────────────────────────────────────────────

    private void MakeSquare(ref RectTransform field, string goName, float size, Color color)
    {
        var go = new GameObject(goName);
        go.transform.SetParent(transform, false);
        go.transform.SetAsFirstSibling();

        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;

        field = go.GetComponent<RectTransform>();
        field.anchorMin = field.anchorMax = new Vector2(0.5f, 0.5f);
        field.pivot     = new Vector2(0.5f, 0.5f);
        field.sizeDelta = new Vector2(size, size);
        field.anchoredPosition = Vector2.zero;
    }

    // ── Grain builders ────────────────────────────────────────────────────────

    private void MakeGrains()
    {
        _grains          = new RectTransform[grainCount];
        _grainImgs       = new Image[grainCount];
        _grainBaseAlpha  = new float[grainCount];
        _grainVelocities = new Vector2[grainCount];
        _grainPhase      = new float[grainCount];
        float hw = _w * 0.5f, hh = _h * 0.5f;

        for (int i = 0; i < grainCount; i++)
        {
            var go = new GameObject($"Grain_{i}");
            go.transform.SetParent(transform, false);

            var img = go.AddComponent<Image>();
            float baseAlpha = Random.Range(grainColor.a * 0.3f, grainColor.a);
            img.color = new Color(grainColor.r, grainColor.g, grainColor.b, baseAlpha);
            img.raycastTarget = false;

            float sz = Random.Range(grainMinSize, grainMaxSize);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(sz, sz);
            rt.anchoredPosition = new Vector2(Random.Range(-hw, hw), Random.Range(-hh, hh));

            _grainBaseAlpha[i]  = baseAlpha;
            _grainVelocities[i] = new Vector2(
                Random.Range(-7f, 7f),
                -Random.Range(grainMinSpeed, grainMaxSpeed)
            );
            _grainPhase[i] = Random.Range(0f, Mathf.PI * 2f);
            _grains[i]     = rt;
            _grainImgs[i]  = img;
        }
    }

    private void DriftGrains(float dt)
    {
        if (_grains == null) return;
        float hw = _w * 0.5f, hh = _h * 0.5f;

        for (int i = 0; i < _grains.Length; i++)
        {
            if (_grains[i] == null) continue;

            Vector2 p = _grains[i].anchoredPosition + _grainVelocities[i] * dt;
            if (p.y < -hh - 10f)
                p = new Vector2(Random.Range(-hw, hw), hh + 10f);
            _grains[i].anchoredPosition = p;

            // Per-grain twinkle using stored base alpha (prevents decay)
            if (_grainImgs[i] != null)
            {
                float twinkle = (Mathf.Sin(Time.unscaledTime * 1.4f + _grainPhase[i]) + 1f) * 0.5f;
                Color c = _grainImgs[i].color;
                float ba = _grainBaseAlpha[i];
                c.a = Mathf.Lerp(ba * 0.20f, ba, twinkle);
                _grainImgs[i].color = c;
            }
        }
    }
}
