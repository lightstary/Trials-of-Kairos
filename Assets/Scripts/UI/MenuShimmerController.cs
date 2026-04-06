using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Diagonal gradient shimmer that sweeps across the screen behind all UI.
/// Cycles through time-state colors (gold, blue, purple, teal).
/// Exposes static color/intensity so other systems (e.g. rotating squares)
/// can tint themselves when the shimmer passes through.
/// Runs on unscaled time so it works during menus (timeScale = 0).
/// </summary>
public class MenuShimmerController : MonoBehaviour
{
    // ── Shimmer palette ──────────────────────────────────────────────────────
    private static readonly Color[] PALETTE = new Color[]
    {
        new Color(0.961f, 0.784f, 0.259f, 1f),   // Gold   (Forward)
        new Color(0.353f, 0.706f, 0.941f, 1f),   // Blue   (Frozen)
        new Color(0.608f, 0.365f, 0.898f, 1f),   // Purple (Reverse)
        new Color(0.200f, 0.780f, 0.860f, 1f),   // Teal   (cosmic accent)
    };

    // ── Timing ───────────────────────────────────────────────────────────────
    private const float SWEEP_DURATION   = 8f;
    private const float PAUSE_BETWEEN    = 4f;
    private const float BAND_WIDTH_RATIO = 0.55f;
    private const float MAX_ALPHA        = 0.22f;
    private const float SWEEP_ANGLE      = 30f;

    // ── Static API for other systems ─────────────────────────────────────────

    /// <summary>The current shimmer color (valid when Intensity > 0).</summary>
    public static Color CurrentColor { get; private set; } = Color.clear;

    /// <summary>Current shimmer intensity 0-1 (0 = not sweeping).</summary>
    public static float Intensity { get; private set; }

    // ── Internal ─────────────────────────────────────────────────────────────
    private RectTransform _bandRT;
    private Image         _bandImage;
    private RectTransform _canvasRT;
    private Texture2D     _gradientTex;
    private Sprite        _gradientSprite;

    private float _timer;
    private int   _colorIndex;
    private bool  _sweeping;

    void Start()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) { enabled = false; return; }
        _canvasRT = canvas.GetComponent<RectTransform>();

        _gradientTex = CreateGradientTexture();
        _gradientSprite = Sprite.Create(_gradientTex,
            new Rect(0, 0, _gradientTex.width, _gradientTex.height),
            new Vector2(0.5f, 0.5f));

        CreateBand();
        _colorIndex = Random.Range(0, PALETTE.Length);
        _timer      = 0f;
        _sweeping   = false;
        Intensity   = 0f;
    }

    void Update()
    {
        _timer += Time.unscaledDeltaTime;

        if (!_sweeping)
        {
            Intensity = 0f;
            if (_timer >= PAUSE_BETWEEN)
            {
                _timer    = 0f;
                _sweeping = true;
                _colorIndex = (_colorIndex + 1) % PALETTE.Length;
                CurrentColor = PALETTE[_colorIndex];
                ApplyColor(CurrentColor);
            }
            else
            {
                ApplyAlpha(0f);
            }
            return;
        }

        float progress = Mathf.Clamp01(_timer / SWEEP_DURATION);
        if (progress >= 1f)
        {
            _timer    = 0f;
            _sweeping = false;
            Intensity = 0f;
            ApplyAlpha(0f);
            return;
        }

        // Position: sweep across the diagonal
        float canvasW  = _canvasRT.rect.width;
        float canvasH  = _canvasRT.rect.height;
        float diagonal = Mathf.Sqrt(canvasW * canvasW + canvasH * canvasH);
        float travel   = diagonal * 1.8f;
        float posX     = Mathf.Lerp(-travel * 0.5f, travel * 0.5f, progress);

        _bandRT.anchoredPosition = new Vector2(posX, 0f);

        // Alpha: smooth cubic bell curve for fluid feel
        float bell  = Mathf.Sin(progress * Mathf.PI);
        float alpha = bell * bell * bell * MAX_ALPHA;
        ApplyAlpha(alpha);
        Intensity = bell * bell * bell;
    }

    void OnDestroy()
    {
        if (_gradientSprite != null) Destroy(_gradientSprite);
        if (_gradientTex    != null) Destroy(_gradientTex);
        Intensity = 0f;
    }

    private void CreateBand()
    {
        var go = new GameObject("ShimmerBand");
        go.transform.SetParent(transform, false);

        _bandRT = go.AddComponent<RectTransform>();
        _bandRT.anchorMin = new Vector2(0.5f, 0.5f);
        _bandRT.anchorMax = new Vector2(0.5f, 0.5f);
        _bandRT.pivot     = new Vector2(0.5f, 0.5f);

        float canvasW  = _canvasRT.rect.width;
        float canvasH  = _canvasRT.rect.height;
        float diagonal = Mathf.Sqrt(canvasW * canvasW + canvasH * canvasH);
        float bandW    = diagonal * BAND_WIDTH_RATIO;

        _bandRT.sizeDelta     = new Vector2(bandW, diagonal * 1.5f);
        _bandRT.localRotation = Quaternion.Euler(0f, 0f, SWEEP_ANGLE);

        _bandImage = go.AddComponent<Image>();
        _bandImage.raycastTarget = false;
        _bandImage.sprite = _gradientSprite;
        _bandImage.type   = Image.Type.Simple;
        _bandImage.color  = new Color(1f, 1f, 1f, 0f);
    }

    /// <summary>Creates a wide soft Gaussian gradient texture.</summary>
    private Texture2D CreateGradientTexture()
    {
        const int width = 256;
        var tex = new Texture2D(width, 1, TextureFormat.RGBA32, false)
        {
            wrapMode   = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        for (int x = 0; x < width; x++)
        {
            float t = x / (float)(width - 1);
            float bell = Mathf.Exp(-8f * (t - 0.5f) * (t - 0.5f));
            tex.SetPixel(x, 0, new Color(1f, 1f, 1f, bell));
        }
        tex.Apply();
        return tex;
    }

    private void ApplyColor(Color c)
    {
        if (_bandImage == null) return;
        float a = _bandImage.color.a;
        _bandImage.color = new Color(c.r, c.g, c.b, a);
    }

    private void ApplyAlpha(float a)
    {
        if (_bandImage == null) return;
        Color c = _bandImage.color;
        c.a = a;
        _bandImage.color = c;
    }
}
