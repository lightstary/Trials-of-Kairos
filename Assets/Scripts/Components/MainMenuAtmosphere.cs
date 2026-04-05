using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TrialsOfKairos.UI
{
    /// <summary>
    /// Drives atmospheric background visuals shared across multiple screens
    /// (Main Menu, Level Select, Controls).
    ///
    /// Effects:
    ///   - Two slow-rotating translucent gold squares (inner CW, outer CCW).
    ///   - Drifting sand/dust particles falling downward with gentle drift.
    ///   - Optional subtitle alpha breathing (disabled externally if the screen
    ///     manages the subtitle itself).
    ///
    /// All effects use Time.unscaledTime so they work when timeScale = 0.
    /// </summary>
    public class MainMenuAtmosphere : MonoBehaviour
    {
        [Header("Subtitle breathing (set null to disable)")]
        [SerializeField] private TextMeshProUGUI subtitleLabel;
        [SerializeField] private float subtitlePulseFrequency = 0.28f;

        [Header("Rotating Squares")]
        [SerializeField] private float cwSize        = 420f;
        [SerializeField] private float cwSpeed       = 11f;
        [SerializeField] private Color squareCWColor  = new Color(1f, 0.843f, 0f, 0.06f);

        [SerializeField] private float ccwSize       = 680f;
        [SerializeField] private float ccwSpeed      = 6.5f;
        [SerializeField] private Color squareCCWColor = new Color(1f, 0.843f, 0f, 0.035f);

        [Header("Sand / Dust Particles")]
        [SerializeField] private int   grainCount    = 65;
        [SerializeField] private float grainMinSize  = 2f;
        [SerializeField] private float grainMaxSize  = 6f;
        [SerializeField] private float grainMinSpeed = 22f;
        [SerializeField] private float grainMaxSpeed = 90f;
        [SerializeField] private Color grainColor    = new Color(1f, 0.843f, 0f, 0.60f);

        // ── State ─────────────────────────────────────────────────────────────────────
        private RectTransform   _cwSquare;
        private RectTransform   _ccwSquare;
        private RectTransform[] _grains;
        private Image[]         _grainImgs;
        private float[]         _grainBaseAlpha;   // immutable per-grain alpha ceiling
        private Vector2[]       _grainVelocities;
        private float[]         _grainPhase;
        private float           _w = 1920f;
        private float           _h = 1080f;

        // Call from screen code to prevent atmosphere from touching a subtitle
        // that the screen manages itself.
        public void DisableSubtitleBreathing() => subtitleLabel = null;

        private void Awake()
        {
            RectTransform rt = transform as RectTransform;
            if (rt != null && rt.rect.width > 1f) { _w = rt.rect.width; _h = rt.rect.height; }

            _ccwSquare = MakeSquare("SquareCCW", ccwSize, squareCCWColor, 45f);
            _cwSquare  = MakeSquare("SquareCW",  cwSize,  squareCWColor,  0f);
            _ccwSquare.SetAsFirstSibling();
            _cwSquare.SetSiblingIndex(1);

            MakeGrains();
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            // CW = negative Z, CCW = positive Z
            if (_cwSquare  != null) _cwSquare.Rotate(0f, 0f,  -cwSpeed  * dt);
            if (_ccwSquare != null) _ccwSquare.Rotate(0f, 0f,  ccwSpeed * dt);

            // Subtitle optional breathing
            if (subtitleLabel != null)
            {
                float t = (Mathf.Sin(Time.unscaledTime * subtitlePulseFrequency * Mathf.PI * 2f) + 1f) * 0.5f;
                Color c = subtitleLabel.color; c.a = Mathf.Lerp(0.35f, 0.85f, t);
                subtitleLabel.color = c;
            }

            DriftGrains(dt);
        }

        // ── Builders ──────────────────────────────────────────────────────────────────

        private RectTransform MakeSquare(string n, float size, Color col, float angle)
        {
            var go = new GameObject(n);
            go.transform.SetParent(transform, false);
            var img = go.AddComponent<Image>(); img.color = col; img.raycastTarget = false;
            var rt  = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition  = Vector2.zero;
            rt.localEulerAngles  = new Vector3(0f, 0f, angle);
            return rt;
        }

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
                _grainVelocities[i] = new Vector2(Random.Range(-7f, 7f),
                                                   -Random.Range(grainMinSpeed, grainMaxSpeed));
                _grainPhase[i]  = Random.Range(0f, Mathf.PI * 2f);
                _grains[i]      = rt;
                _grainImgs[i]   = img;
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
                if (p.y < -hh - 10f) p = new Vector2(Random.Range(-hw, hw), hh + 10f);
                _grains[i].anchoredPosition = p;

                // Per-grain twinkle — lerp between dim and bright using the STORED base alpha
                if (_grainImgs[i] != null)
                {
                    float twinkle = (Mathf.Sin(Time.unscaledTime * 1.4f + _grainPhase[i]) + 1f) * 0.5f;
                    Color c    = _grainImgs[i].color;
                    float base_a = _grainBaseAlpha[i];
                    c.a = Mathf.Lerp(base_a * 0.20f, base_a, twinkle);
                    _grainImgs[i].color = c;
                }
            }
        }
    }
}
