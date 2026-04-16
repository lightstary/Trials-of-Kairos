using UnityEngine;
using TMPro;

/// <summary>
/// Centralized Cinzel font lookup. Caches the SDF font assets so every
/// runtime-built UI element uses the same font without repeated searches.
/// Uses AssetDatabase.FindAssets for resilience to file renames.
/// </summary>
public static class CinzelFontHelper
{
    private static TMP_FontAsset _regular;
    private static TMP_FontAsset _bold;
    private static TMP_FontAsset _black;
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

    /// <summary>Returns the Cinzel Bold SDF font, falling back to Regular.</summary>
    public static TMP_FontAsset Bold
    {
        get
        {
            if (!_searched) FindFonts();
            return _bold != null ? _bold : _regular;
        }
    }

    /// <summary>Returns the Cinzel Black SDF font, falling back to Bold.</summary>
    public static TMP_FontAsset Black
    {
        get
        {
            if (!_searched) FindFonts();
            return _black != null ? _black : Bold;
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

    /// <summary>Applies Cinzel to every TMP component under a transform.</summary>
    public static void ApplyToAll(Transform root, bool bold = false)
    {
        if (root == null) return;
        foreach (TextMeshProUGUI tmp in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            Apply(tmp, bold);
    }

    private static void FindFonts()
    {
        _searched = true;

#if UNITY_EDITOR
        string[] guids = UnityEditor.AssetDatabase.FindAssets("Cinzel t:TMP_FontAsset");
        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            TMP_FontAsset font = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font == null) continue;

            string lower = path.ToLowerInvariant();
            if      (lower.Contains("black")   && _black   == null) _black   = font;
            else if (lower.Contains("regular") && _regular == null) _regular = font;
            else if (lower.Contains("bold")    && _bold    == null) _bold    = font;
        }
#endif

        // Fallback: scan all loaded TMP_FontAssets
        if (_regular == null || _bold == null)
        {
            foreach (TMP_FontAsset f in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
            {
                if (!f.name.Contains("Cinzel")) continue;
                if (_regular == null && f.name.Contains("Regular")) _regular = f;
                if (_bold    == null && f.name.Contains("Bold"))    _bold    = f;
                if (_black   == null && f.name.Contains("Black"))   _black   = f;
                if (_regular == null) _regular = f;
            }
        }

        if (_bold == null) _bold = _regular;
        if (_black == null) _black = _bold;
    }
}
