using UnityEngine;
using TMPro;

/// <summary>
/// Centralized Cinzel font lookup. Loads fonts from the GameAssets
/// ScriptableObject in Resources/ to ensure they work in builds.
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

        GameAssets assets = GameAssets.Instance;
        if (assets != null)
        {
            _regular = assets.cinzelRegular;
            _bold    = assets.cinzelBold;
            _black   = assets.cinzelBlack;
        }

        // Ensure fallbacks
        if (_bold == null) _bold = _regular;
        if (_black == null) _black = _bold;

        if (_regular == null)
            Debug.LogWarning("[CinzelFontHelper] No Cinzel fonts found. Check GameAssets in Resources/.");
    }
}
