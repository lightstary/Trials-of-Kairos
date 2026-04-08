using UnityEngine;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Applies Cinzel font variants to every TextMeshProUGUI in the canvas.
/// In the Editor, fonts are discovered automatically via asset search — resilient to file renames.
/// </summary>
public class UIFontApplier : MonoBehaviour
{
    [Header("Cinzel Font Variants (auto-discovered in Editor)")]
    [SerializeField] private TMP_FontAsset cinzelBlack;
    [SerializeField] private TMP_FontAsset cinzelBold;
    [SerializeField] private TMP_FontAsset cinzelRegular;

    void Awake()
    {
        LoadFonts();
        ApplyFonts();
    }

    // Second pass: TMP components initialised after Awake on some Unity versions
    void Start() => ApplyFonts();

    private void LoadFonts()
    {
#if UNITY_EDITOR
        string[] guids = AssetDatabase.FindAssets("Cinzel t:TMP_FontAsset");
        foreach (string guid in guids)
        {
            string path       = AssetDatabase.GUIDToAssetPath(guid);
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font == null) continue;

            string lower = path.ToLowerInvariant();
            // Check black before bold, regular before bold to avoid substring collisions
            if      (lower.Contains("black")   && cinzelBlack   == null) cinzelBlack   = font;
            else if (lower.Contains("regular") && cinzelRegular == null) cinzelRegular = font;
            else if (lower.Contains("bold")    && cinzelBold    == null) cinzelBold    = font;
        }
#endif
    }

    /// <summary>Applies Cinzel fonts to all TMP labels under this GameObject.</summary>
    public void ApplyFonts()
    {
        TMP_FontAsset fallback = cinzelBold != null ? cinzelBold : cinzelBlack;
        if (fallback == null) return;

        foreach (TextMeshProUGUI label in GetComponentsInChildren<TextMeshProUGUI>(includeInactive: true))
            label.font = SelectFont(label.gameObject.name, fallback);
    }

    private TMP_FontAsset SelectFont(string goName, TMP_FontAsset fallback)
    {
        string n = goName.ToLowerInvariant();

        // Large display titles → Black
        if (cinzelBlack != null && n == "title")
            return cinzelBlack;

        // Section headings → Bold
        if (cinzelBold != null && (n.Contains("controlstitle") || n == "trialselecttitle"))
            return cinzelBold;

        // Small captions, version, chapter numbers → Regular
        if (cinzelRegular != null && (n == "versionlabel" || n == "chapterlabel" || n == "trialnum"))
            return cinzelRegular;

        return fallback;
    }
}
