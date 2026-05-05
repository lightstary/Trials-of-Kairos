using UnityEngine;
using TMPro;

// Cinzel font lookup. Loads from GameAssets in Resources/.
public static class CinzelFontHelper
{
    private static TMP_FontAsset _regular;
    private static TMP_FontAsset _bold;
    private static TMP_FontAsset _black;
    private static bool _searched;

    public static TMP_FontAsset Regular
    {
        get
        {
            if (!_searched) FindFonts();
            return _regular;
        }
    }

    public static TMP_FontAsset Bold
    {
        get
        {
            if (!_searched) FindFonts();
            return _bold != null ? _bold : _regular;
        }
    }

    public static TMP_FontAsset Black
    {
        get
        {
            if (!_searched) FindFonts();
            return _black != null ? _black : Bold;
        }
    }

    public static void Apply(TextMeshProUGUI tmp, bool bold = false)
    {
        if (tmp == null) return;
        TMP_FontAsset font = bold ? Bold : Regular;
        if (font != null) tmp.font = font;
    }

    public static void Apply(TextMeshPro tmp, bool bold = false)
    {
        if (tmp == null) return;
        TMP_FontAsset font = bold ? Bold : Regular;
        if (font != null) tmp.font = font;
    }

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
