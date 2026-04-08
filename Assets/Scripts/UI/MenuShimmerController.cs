using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Diagonal gradient shimmer that sweeps very slowly across the menu.
/// Exposes static color, intensity and normalized progress so other
/// systems can react (e.g. rotating squares absorb the color as the
/// band passes through them).
/// </summary>
public class MenuShimmerController : MonoBehaviour
{
    // ── Palette ──────────────────────────────────────────────────────────────
    private static readonly Color[] PALETTE = new Color[]
    {
        new Color(0.961f, 0.784f, 0.259f, 1f),   // Gold
        new Color(0.353f, 0.706f, 0.941f, 1f),   // Blue
        new Color(0.608f, 0.365f, 0.898f, 1f),   // Purple
        new Color(0.200f, 0.780f, 0.860f, 1f),   // Teal
    };

    // ── Timing ───────────────────────────────────────────────────────────────
    private const float SWEEP_DURATION   = 10f;
    private const float PAUSE_BETWEEN    = 7f;
    private const float BAND_WIDTH_RATIO = 1.2f;
    private const float MAX_ALPHA        = 0.025f;
    private const float SWEEP_ANGLE      = 25f;

    // ── Static API ───────────────────────────────────────────────────────────

    /// <summary>Current shimmer color.</summary>
    public static Color CurrentColor { get; private set; } = Color.clear;

    /// <summary>Shimmer intensity 0-1 (0 = idle).</summary>
    public static float Intensity { get; private set; }

    /// <summary>Normalized sweep progress 0-1 (0 = left edge, 1 = right edge).</summary>
    public static float Progress { get; private set; }

    /// <summary>True while the band is actively sweeping.</summary>
    public static bool IsSweeping { get; private set; }

    // ── Internal ─────────────────────────────────────────────────────────────
    private RectTransform _bandRT;
    private RawImage      _bandImage;
    private RectTransform _canvasRT;
    private Texture2D     _gradientTex;

    private float _timer;
    private int   _colorIndex;

    void Start()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) { enabled = false; return; }
        _canvasRT = canvas.GetComponent<RectTransform>();

        _gradientTex = CreateGradientTexture();
        CreateBand();
        _colorIndex = Random.Range(0, PALETTE.Length);
        _timer      = 0f;
        IsSweeping  = false;
        Intensity   = 0f;
        Progress    = 0f;
    }

    void Update()
    {
        _timer += Time.unscaledDeltaTime;

        if (!IsSweeping)
        {
            Intensity = 0f;
            Progress  = 0f;
            if (_timer >= PAUSE_BETWEEN)
            {
                _timer     = 0f;
                IsSweeping = true;
                _colorIndex  = (_colorIndex + 1) % PALETTE.Length;
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
        Progress = progress;

        if (progress >= 1f)
        {
            _timer     = 0f;
            IsSweeping = false;
            Intensity  = 0f;
            Progress   = 0f;
            ApplyAlpha(0f);
            return;
        }

        // Position
        float canvasW  = _canvasRT.rect.width;
        float canvasH  = _canvasRT.rect.height;
        float diagonal = Mathf.Sqrt(canvasW * canvasW + canvasH * canvasH);
        float travel   = diagonal * 2.2f;
        float posX     = Mathf.Lerp(-travel * 0.5f, travel * 0.5f, progress);
        _bandRT.anchoredPosition = new Vector2(posX, 0f);

        // Alpha: smooth sine with smoothstep for organic swell
        float bell  = Mathf.Sin(progress * Mathf.PI);
        bell = bell * bell * (3f - 2f * bell); // smoothstep the bell curve
        float alpha = bell * MAX_ALPHA;
        ApplyAlpha(alpha);
        Intensity = bell;
    }

    void OnDestroy()
    {
        if (_gradientTex != null) Destroy(_gradientTex);
        Intensity  = 0f;
        IsSweeping = false;
        Progress   = 0f;
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

        _bandImage = go.AddComponent<RawImage>();
        _bandImage.raycastTarget = false;
        _bandImage.texture       = _gradientTex;
        _bandImage.color         = new Color(1f, 1f, 1f, 0f);

        CanvasGroup cg    = go.AddComponent<CanvasGroup>();
        cg.interactable   = false;
        cg.blocksRaycasts = false;
    }

    /// <summary>
    /// Very wide, soft gradient. Gaussian multiplied by smoothstep squared
    /// to ensure true zero at edges with no visible seams.
    /// </summary>
    private Texture2D CreateGradientTexture()
    {
        const int width = 512;
        var tex = new Texture2D(width, 1, TextureFormat.RGBA32, false)
        {
            wrapMode   = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        for (int x = 0; x < width; x++)
        {
            float t = x / (float)(width - 1);
            float d = (t - 0.5f) * 2f;
            float bell = Mathf.Exp(-4f * d * d);
            float edge = Mathf.SmoothStep(0f, 1f, 1f - Mathf.Abs(d));
            float a    = bell * edge * edge;
            tex.SetPixel(x, 0, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        return tex;
    }

    private void ApplyColor(Color c)
    {
        if (_bandImage == null) return;
        _bandImage.color = new Color(c.r, c.g, c.b, _bandImage.color.a);
    }

    private void ApplyAlpha(float a)
    {
        if (_bandImage == null) return;
        Color c = _bandImage.color;
        c.a = a;
        _bandImage.color = c;
    }
}
