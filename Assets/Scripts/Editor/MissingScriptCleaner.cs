using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Automatically runs on every script compile:
///   1. Removes missing-script components from all loaded scenes.
///   2. Installs D-pad input axes (DPadHorizontal / DPadVertical) if absent.
/// Also available manually via the Tools menu.
/// </summary>
[InitializeOnLoad]
public static class MissingScriptCleaner
{
    static MissingScriptCleaner()
    {
        // Defer so the Editor has fully loaded the scene before we touch it.
        EditorApplication.delayCall += RunAll;
    }

    private static void RunAll()
    {
        CleanAllScenes();
        AddDPadAxes();
    }

    // ── Missing-script cleaner ─────────────────────────────────────────────────

    [MenuItem("Tools/Clean Missing Scripts (All Scenes)")]
    public static void CleanAllScenes()
    {
        int totalRemoved = 0;

        for (int s = 0; s < SceneManager.sceneCount; s++)
        {
            Scene scene = SceneManager.GetSceneAt(s);
            if (!scene.isLoaded) continue;

            foreach (GameObject root in scene.GetRootGameObjects())
                totalRemoved += CleanRecursive(root);

            if (totalRemoved > 0)
                EditorSceneManager.MarkSceneDirty(scene);
        }

        if (totalRemoved > 0)
        {
            EditorSceneManager.SaveOpenScenes();
            Debug.Log($"[MissingScriptCleaner] Removed {totalRemoved} missing-script components and saved.");
        }
        else
        {
            Debug.Log("[MissingScriptCleaner] No missing-script components found.");
        }
    }

    private static int CleanRecursive(GameObject go)
    {
        int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
        for (int i = 0; i < go.transform.childCount; i++)
            removed += CleanRecursive(go.transform.GetChild(i).gameObject);
        return removed;
    }

    // ── D-pad axis installer ───────────────────────────────────────────────────

    [MenuItem("Tools/Add D-Pad Input Axes")]
    public static void AddDPadAxes()
    {
        SerializedObject inputManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset")[0]);

        SerializedProperty axes = inputManager.FindProperty("m_Axes");

        // Always remove existing D-pad axes so we can reinstall with correct settings.
        for (int i = axes.arraySize - 1; i >= 0; i--)
        {
            string axisName = axes.GetArrayElementAtIndex(i)
                                  .FindPropertyRelative("m_Name").stringValue;
            if (axisName == "DPadHorizontal" || axisName == "DPadVertical")
                axes.DeleteArrayElementAtIndex(i);
        }

        // DPadVertical intentionally NOT inverted — on Xbox, D-pad Up = +1 on axis 7.
        AddAxis(axes, "DPadHorizontal", axisIndex: 6, invert: false);
        AddAxis(axes, "DPadVertical",   axisIndex: 7, invert: false);

        inputManager.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        Debug.Log("[DPad] D-pad axes (re)installed. DPadVertical invert = false.");
    }

    private static bool AxisExists(SerializedProperty axes, string name)
    {
        for (int i = 0; i < axes.arraySize; i++)
        {
            if (axes.GetArrayElementAtIndex(i).FindPropertyRelative("m_Name").stringValue == name)
                return true;
        }
        return false;
    }

    private static void AddAxis(SerializedProperty axes, string name, int axisIndex, bool invert)
    {
        axes.InsertArrayElementAtIndex(axes.arraySize);
        SerializedProperty axis = axes.GetArrayElementAtIndex(axes.arraySize - 1);

        axis.FindPropertyRelative("m_Name").stringValue             = name;
        axis.FindPropertyRelative("descriptiveName").stringValue    = "";
        axis.FindPropertyRelative("descriptiveNegativeName").stringValue = "";
        axis.FindPropertyRelative("negativeButton").stringValue     = "";
        axis.FindPropertyRelative("positiveButton").stringValue     = "";
        axis.FindPropertyRelative("altNegativeButton").stringValue  = "";
        axis.FindPropertyRelative("altPositiveButton").stringValue  = "";
        axis.FindPropertyRelative("gravity").floatValue             = 0f;
        axis.FindPropertyRelative("dead").floatValue                = 0.19f;
        axis.FindPropertyRelative("sensitivity").floatValue         = 1f;
        axis.FindPropertyRelative("snap").boolValue                 = false;
        axis.FindPropertyRelative("invert").boolValue               = invert;
        axis.FindPropertyRelative("type").intValue                  = 2;    // Joystick Axis
        axis.FindPropertyRelative("axis").intValue                  = axisIndex - 1; // 0-based
        axis.FindPropertyRelative("joyNum").intValue                = 0;    // All joysticks
    }
}

