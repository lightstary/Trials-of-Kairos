using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Spawns and animates canvas-based dust mote particles inside the Main Menu.
/// Works with Screen Space Overlay — no 3D camera tricks required.
/// Uses unscaled time so it animates while timeScale = 0.
/// </summary>
public class MenuParticlesController : MonoBehaviour
{
    private const int   MOTE_COUNT    = 180;
    private const float MIN_LIFE      = 4f;
    private const float MAX_LIFE      = 12f;
    private const float MIN_SIZE      = 1.5f;
    private const float MAX_SIZE      = 10f;
    private const float DRIFT_X_MIN   = -25f;
    private const float DRIFT_X_MAX   = 50f;
    private const float DRIFT_Y_MIN   = 18f;
    private const float DRIFT_Y_MAX   = 75f;
    private const int   CIRCLE_TEX_SIZE = 32;

    /// <summary>Color palette matching the shimmer time-state colors.</summary>
    private static readonly Color[] MOTE_PALETTE = new Color[]
    {
        new Color(0.96f, 0.78f, 0.26f),   // Gold
        new Color(0.96f, 0.84f, 0.42f),   // Warm gold
        new Color(0.35f, 0.71f, 0.94f),   // Blue
        new Color(0.45f, 0.65f, 0.95f),   // Light blue
        new Color(0.61f, 0.37f, 0.90f),   // Purple
        new Color(0.20f, 0.78f, 0.86f),   // Teal
        new Color(0.85f, 0.75f, 0.50f),   // Sand
    };

    private struct Mote
    {
        public RectTransform rect;
        public Image          image;
        public Vector2        pos;
        public Vector2        vel;
        public float          life;
        public float          maxLife;
        public float          peakAlpha;
        public Color          baseColor;
        public float          baseSize;
    }

    private Mote[]        _motes;
    private RectTransform _canvasRect;
    private static Sprite _circleSprite;

    void Start()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        _canvasRect   = canvas != null ? canvas.GetComponent<RectTransform>() : GetComponent<RectTransform>();

        if (_circleSprite == null)
            _circleSprite = CreateCircleSprite(CIRCLE_TEX_SIZE);

        float halfW = _canvasRect.rect.width  * 0.5f;
        float halfH = _canvasRect.rect.height * 0.5f;

        _motes = new Mote[MOTE_COUNT];
        for (int i = 0; i < MOTE_COUNT; i++)
        {
            _motes[i] = CreateMote(i);
            // Stagger life and simulate drift from the bottom edge
            float staggerLife = Random.Range(0f, _motes[i].maxLife);
            _motes[i].life = staggerLife;
            float startX = Random.Range(-halfW, halfW);
            float startY = -halfH - 12f;
            _motes[i].pos = new Vector2(
                startX + _motes[i].vel.x * staggerLife,
                startY + _motes[i].vel.y * staggerLife
            );
            _motes[i].rect.anchoredPosition = _motes[i].pos;
        }
    }

    void Update()
    {
        if (_canvasRect == null || _motes == null) return;

        float dt    = Time.unscaledDeltaTime;
        float halfW = _canvasRect.rect.width  * 0.5f;
        float halfH = _canvasRect.rect.height * 0.5f;

        for (int i = 0; i < _motes.Length; i++)
        {
            ref Mote m = ref _motes[i];
            m.life += dt;

            if (m.life >= m.maxLife)
            {
                Respawn(ref m, halfW, halfH);
                continue;
            }

            // Drift
            m.pos += m.vel * dt;
            m.rect.anchoredPosition = m.pos;

            // Bell-curve fade with micro-twinkle
            float t     = m.life / m.maxLife;
            float alpha = m.peakAlpha * Mathf.Sin(t * Mathf.PI);
            alpha      *= 0.86f + 0.14f * Mathf.Sin(m.life * 4.3f + i * 1.7f);

            // Size breathe
            float s = m.baseSize * (0.80f + 0.20f * Mathf.Sin(t * Mathf.PI * 1.5f + i));
            m.rect.sizeDelta = new Vector2(s, s);

            Color c = m.baseColor;
            c.a = Mathf.Clamp01(alpha);
            m.image.color = c;
        }
    }

    private Mote CreateMote(int idx)
    {
        var go   = new GameObject($"Mote{idx:D3}", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot     = new Vector2(0.5f, 0.5f);

        var img = go.GetComponent<Image>();
        img.sprite = _circleSprite;
        img.type = Image.Type.Simple;
        img.raycastTarget = false;

        var mote = new Mote { rect = rect, image = img };
        InitValues(ref mote);
        return mote;
    }

    private void Respawn(ref Mote m, float halfW, float halfH)
    {
        InitValues(ref m);
        // Re-enter from the bottom edge
        m.pos = new Vector2(Random.Range(-halfW, halfW), -halfH - 12f);
        m.rect.anchoredPosition = m.pos;
        m.life = 0f;
    }

    private void InitValues(ref Mote m)
    {
        m.maxLife   = Random.Range(MIN_LIFE, MAX_LIFE);
        m.peakAlpha = Random.Range(0.18f, 0.60f);
        m.vel       = new Vector2(Random.Range(DRIFT_X_MIN, DRIFT_X_MAX),
                                  Random.Range(DRIFT_Y_MIN, DRIFT_Y_MAX));
        m.baseSize  = Random.Range(MIN_SIZE, MAX_SIZE);

        // Pick a random color from the palette with slight variation
        Color baseCol = MOTE_PALETTE[Random.Range(0, MOTE_PALETTE.Length)];
        m.baseColor = new Color(
            Mathf.Clamp01(baseCol.r + Random.Range(-0.06f, 0.06f)),
            Mathf.Clamp01(baseCol.g + Random.Range(-0.06f, 0.06f)),
            Mathf.Clamp01(baseCol.b + Random.Range(-0.06f, 0.06f)),
            0f
        );

        if (m.rect  != null) m.rect.sizeDelta = new Vector2(m.baseSize, m.baseSize);
        if (m.image != null) m.image.color     = new Color(m.baseColor.r, m.baseColor.g, m.baseColor.b, 0f);
    }

    /// <summary>Enable or disable the dust layer (call from MainMenuController).</summary>
    public void SetPlaying(bool playing)
    {
        enabled = playing;
        if (!playing && _motes != null)
            for (int i = 0; i < _motes.Length; i++)
            {
                Color c = _motes[i].image.color;
                c.a = 0f;
                _motes[i].image.color = c;
            }
    }

    /// <summary>Generates a soft radial gradient circle sprite for mote particles.</summary>
    private static Sprite CreateCircleSprite(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        float center = size * 0.5f;
        float maxR = center;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy) / maxR;
                float alpha = Mathf.Clamp01(1f - dist);
                alpha = alpha * alpha; // quadratic falloff for soft edges
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}
