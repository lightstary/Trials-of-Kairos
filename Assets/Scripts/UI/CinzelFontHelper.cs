using UnityEngine;
using TMPro;

/// <summary>
/// Centralized Cinzel font lookup. Caches the SDF font assets so every
/// runtime-built UI element uses the same font without repeated searches.
/// </summary>
public static class CinzelFontHelper
{
    private static TMP_FontAsset _regular;
    private static TMP_FontAsset _bold;
    private static bool _searched;

    /// <summary>Returns the Cinzel Regular SDF font, or the best available Cinzel variant.</summary>
    public static TMP_FontAsset Regular
    {
        get
        {
            if (!_searched) FindFonts();
            return _regular;
        }
    }

    /// <summary>Returns the Cinzel Bold (or SemiBold) SDF font, falling back to Regular.</summary>
    public static TMP_FontAsset Bold
    {
        get
        {
            if (!_searched) FindFonts();
            return _bold != null ? _bold : _regular;
        }
    }

    /// <summary>Assigns the appropriate Cinzel font to a TMP label.</summary>
    public static void Apply(TextMeshProUGUI tmp, bool bold = false)
    {
        if (tmp == null) return;
        TMP_FontAsset font = bold ? Bold : Regular;
        if (font != null) tmp.font = font;
    }

    /// <summary>Assigns the appropriate Cinzel font to a TMP label (3D).</summary>
    public static void Apply(TextMeshPro tmp, bool bold = false)
    {
        if (tmp == null) return;
        TMP_FontAsset font = bold ? Bold : Regular;
        if (font != null) tmp.font = font;
    }

    private static void FindFonts()
    {
        _searched = true;

        // Try loading by known path first (fastest, works in Editor and builds
        // if the asset is in a Resources folder or addressable).
#if UNITY_EDITOR
        _regular = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/Daniel's Assets/Fonts/Cinzel-Regular SDF.asset");
        _bold = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/Daniel's Assets/Fonts/Cinzel-Bold SDF.asset");
        if (_bold == null)
            _bold = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/Daniel's Assets/Fonts/Cinzel-SemiBold SDF.asset");
#endif

        // Fallback: scan all loaded TMP_FontAssets
        if (_regular == null || _bold == null)
        {
            foreach (TMP_FontAsset f in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
            {
                if (!f.name.Contains("Cinzel")) continue;

                if (_regular == null && f.name.Contains("Regular"))
                    _regular = f;

                if (_bold == null && (f.name.Contains("Bold") || f.name.Contains("SemiBold")))
                    _bold = f;

                // Accept any Cinzel as fallback regular
                if (_regular == null)
                    _regular = f;
            }
        }

        if (_bold == null) _bold = _regular;
    }
}
