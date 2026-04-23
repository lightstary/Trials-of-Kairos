using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

/// <summary>
/// Ensures the Input System package is installed and Active Input Handling is set to "Both".
/// Phase 1: If the package is not installed, reverts to Old input (to avoid compile errors)
///          and starts installing com.unity.inputsystem.
/// Phase 2: After the package is installed, sets Active Input Handling to "Both" so
///          Gamepad.current works at runtime alongside legacy Input.
/// </summary>
[InitializeOnLoad]
public static class RightStickAxesSetup
{
    private const int INPUT_HANDLER_OLD = 0;
    private const int INPUT_HANDLER_BOTH = 2;
    private const string INPUT_SYSTEM_PACKAGE = "com.unity.inputsystem@1.7.0";

    private static AddRequest _addRequest;

    static RightStickAxesSetup()
    {
        bool packageInstalled = IsInputSystemAssemblyLoaded();

        var playerSettings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0];
        var so = new SerializedObject(playerSettings);
        SerializedProperty handler = so.FindProperty("activeInputHandler");

        if (handler == null) return;

        if (!packageInstalled)
        {
            // Phase 1: Package not installed — revert to Old to fix compile errors
            if (handler.intValue != INPUT_HANDLER_OLD)
            {
                handler.intValue = INPUT_HANDLER_OLD;
                so.ApplyModifiedProperties();
                Debug.Log("[RightStickAxesSetup] Reverted Active Input Handling to Old (0) — Input System package not yet installed.");
            }

            // Start installing the package
            Debug.Log("[RightStickAxesSetup] Installing com.unity.inputsystem package...");
            _addRequest = Client.Add(INPUT_SYSTEM_PACKAGE);
            EditorApplication.update += OnPackageInstallProgress;
        }
        else
        {
            // Phase 2: Package is installed — enable Both
            if (handler.intValue != INPUT_HANDLER_BOTH)
            {
                handler.intValue = INPUT_HANDLER_BOTH;
                so.ApplyModifiedProperties();
                Debug.Log("[RightStickAxesSetup] Input System package detected. Set Active Input Handling to Both (2). Restart may be required.");
            }
        }

        // Remove Space from "Submit" axis — prevents Spacebar from acting as UI confirm
        RemoveSpaceFromSubmitAxis();
    }

    private static void OnPackageInstallProgress()
    {
        if (_addRequest == null || !_addRequest.IsCompleted) return;

        EditorApplication.update -= OnPackageInstallProgress;

        if (_addRequest.Status == StatusCode.Success)
        {
            Debug.Log($"[RightStickAxesSetup] Successfully installed {_addRequest.Result.packageId}. Unity will recompile — the editor script will then enable Both input handling.");
        }
        else
        {
            Debug.LogError($"[RightStickAxesSetup] Failed to install Input System package: {_addRequest.Error?.message}");
        }

        _addRequest = null;
    }

    private static bool IsInputSystemAssemblyLoaded()
    {
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.GetName().Name == "Unity.InputSystem")
                return true;
        }
        return false;
    }

    /// <summary>
    /// Removes any "Submit" Input Manager axis entry that uses "space" as its
    /// positive button. Keeps Return/Enter and JoystickButton0 bindings intact.
    /// This prevents Spacebar from acting as UI submit / left-click.
    /// </summary>
    private static void RemoveSpaceFromSubmitAxis()
    {
        var inputManager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset")[0];
        var im = new SerializedObject(inputManager);
        SerializedProperty axes = im.FindProperty("m_Axes");

        if (axes == null || !axes.isArray) return;

        bool changed = false;
        for (int i = axes.arraySize - 1; i >= 0; i--)
        {
            SerializedProperty axis = axes.GetArrayElementAtIndex(i);
            string axisName = axis.FindPropertyRelative("m_Name").stringValue;

            if (axisName != "Submit") continue;

            SerializedProperty posBtn = axis.FindPropertyRelative("positiveButton");
            SerializedProperty altPosBtn = axis.FindPropertyRelative("altPositiveButton");

            // If this Submit entry's primary button is "space", remove the entire entry
            if (posBtn != null && posBtn.stringValue == "space")
            {
                axes.DeleteArrayElementAtIndex(i);
                changed = true;
                Debug.Log("[RightStickAxesSetup] Removed Submit axis entry bound to Space.");
                continue;
            }

            // If space is the alt button, clear it
            if (altPosBtn != null && altPosBtn.stringValue == "space")
            {
                altPosBtn.stringValue = "";
                changed = true;
                Debug.Log("[RightStickAxesSetup] Cleared Space from Submit alt-positive button.");
            }
        }

        if (changed)
            im.ApplyModifiedProperties();
    }
}
