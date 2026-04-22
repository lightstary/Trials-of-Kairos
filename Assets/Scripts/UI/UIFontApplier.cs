using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Global Cinzel font enforcer. Auto-spawns on application start and applies
/// Cinzel to every TextMeshProUGUI and TextMeshPro in the scene — both
/// serialized (Inspector-assigned) and dynamically created components.
/// Runs on every scene load to catch late additions.
/// </summary>
public class UIFontApplier : MonoBehaviour
{
    private static UIFontApplier _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoSpawn()
    {
        if (_instance != null) return;

        GameObject go = new GameObject("[UIFontApplier]");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<UIFontApplier>();

        SceneManager.sceneLoaded += OnSceneLoaded;

        // Apply to the initial scene immediately
        ApplyToEntireScene();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyToEntireScene();
    }

    /// <summary>
    /// Applies Cinzel font to every TMP component currently in the scene.
    /// Uses CinzelFontHelper for consistent font lookup and caching.
    /// </summary>
    public static void ApplyToEntireScene()
    {
        TMP_FontAsset bold = CinzelFontHelper.Bold;
        TMP_FontAsset regular = CinzelFontHelper.Regular;

        if (bold == null && regular == null) return;

        // Apply to all UGUI TMP components (Canvas-based)
        foreach (TextMeshProUGUI tmp in Resources.FindObjectsOfTypeAll<TextMeshProUGUI>())
        {
            if (tmp == null) continue;
            // Skip if already using a Cinzel variant
            if (tmp.font != null && tmp.font.name.Contains("Cinzel")) continue;

            // Use bold for bold-styled text, regular for everything else
            bool isBold = (tmp.fontStyle & FontStyles.Bold) != 0;
            TMP_FontAsset target = isBold ? bold : regular;
            if (target == null) target = bold;
            if (target != null) tmp.font = target;
        }

        // Apply to all 3D TMP components (world-space)
        foreach (TextMeshPro tmp in Resources.FindObjectsOfTypeAll<TextMeshPro>())
        {
            if (tmp == null) continue;
            if (tmp.font != null && tmp.font.name.Contains("Cinzel")) continue;

            bool isBold = (tmp.fontStyle & FontStyles.Bold) != 0;
            TMP_FontAsset target = isBold ? bold : regular;
            if (target == null) target = bold;
            if (target != null) tmp.font = target;
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (_instance == this) _instance = null;
    }
}
