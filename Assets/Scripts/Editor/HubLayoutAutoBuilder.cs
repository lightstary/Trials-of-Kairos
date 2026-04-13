using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Automatically builds the Hub layout tiles whenever HubScene is loaded
/// in the editor and tiles don't exist yet. Runs after every domain reload
/// (script recompilation) so tiles are always visible in the Scene view.
/// </summary>
[InitializeOnLoad]
public static class HubLayoutAutoBuilder
{
    private static int _framesToWait = 5;

    static HubLayoutAutoBuilder()
    {
        _framesToWait = 5;
        EditorApplication.update += WaitAndBuild;
    }

    private static void WaitAndBuild()
    {
        if (_framesToWait-- > 0) return;

        EditorApplication.update -= WaitAndBuild;

        if (Application.isPlaying) return;

        HubLevelManager hlm = Object.FindObjectOfType<HubLevelManager>();
        if (hlm == null) return;

        Transform existing = hlm.transform.Find("HubTiles");
        if (existing != null && existing.childCount > 0) return;

        Debug.Log("[HubLayoutAutoBuilder] Building hub tiles...");

        // Use reflection to call the private EditorBuildLayout method
        var method = typeof(HubLevelManager).GetMethod("EditorBuildLayout",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (method != null)
        {
            method.Invoke(hlm, null);
            EditorSceneManager.MarkSceneDirty(hlm.gameObject.scene);
        }
        else
        {
            Debug.LogWarning("[HubLayoutAutoBuilder] EditorBuildLayout method not found.");
        }
    }
}
