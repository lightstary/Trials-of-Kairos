using UnityEngine;
using System;

/// <summary>
/// Detects whether the player is using keyboard/mouse or a controller
/// and fires an event when the active input device changes.
/// Auto-creates itself via RuntimeInitializeOnLoadMethod.
/// </summary>
public class InputPromptManager : MonoBehaviour
{
    /// <summary>The two supported input modes.</summary>
    public enum InputMode { KeyboardMouse, Controller }

    /// <summary>Fires when the active input device changes.</summary>
    public static event Action<InputMode> OnInputModeChanged;

    /// <summary>The current active input mode.</summary>
    public static InputMode CurrentMode { get; private set; } = InputMode.Controller;

    /// <summary>True when the current input mode is keyboard + mouse.</summary>
    public static bool IsKeyboardMouse => CurrentMode == InputMode.KeyboardMouse;

    private static InputPromptManager _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (_instance != null) return;
        GameObject go = new GameObject("[InputPromptManager]");
        _instance = go.AddComponent<InputPromptManager>();
        DontDestroyOnLoad(go);
    }

    void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
    }

    void Update()
    {
        if (AnyKeyboardInput() || AnyMouseInput())
            SetMode(InputMode.KeyboardMouse);
        else if (AnyControllerInput())
            SetMode(InputMode.Controller);
    }

    private static void SetMode(InputMode mode)
    {
        if (CurrentMode == mode) return;
        CurrentMode = mode;
        OnInputModeChanged?.Invoke(mode);
    }

    /// <summary>Checks for any keyboard key press this frame.</summary>
    private static bool AnyKeyboardInput()
    {
        for (int i = (int)KeyCode.A; i <= (int)KeyCode.Z; i++)
            if (Input.GetKeyDown((KeyCode)i)) return true;
        for (int i = (int)KeyCode.Alpha0; i <= (int)KeyCode.Alpha9; i++)
            if (Input.GetKeyDown((KeyCode)i)) return true;

        return Input.GetKeyDown(KeyCode.Space)
            || Input.GetKeyDown(KeyCode.Return)
            || Input.GetKeyDown(KeyCode.Escape)
            || Input.GetKeyDown(KeyCode.Tab)
            || Input.GetKeyDown(KeyCode.LeftShift)
            || Input.GetKeyDown(KeyCode.RightShift)
            || Input.GetKeyDown(KeyCode.LeftControl)
            || Input.GetKeyDown(KeyCode.RightControl)
            || Input.GetKeyDown(KeyCode.UpArrow)
            || Input.GetKeyDown(KeyCode.DownArrow)
            || Input.GetKeyDown(KeyCode.LeftArrow)
            || Input.GetKeyDown(KeyCode.RightArrow)
            || Input.GetKeyDown(KeyCode.Backspace)
            || Input.GetKeyDown(KeyCode.Delete);
    }

    /// <summary>Checks for any mouse movement or button press this frame.</summary>
    private static bool AnyMouseInput()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
            return true;

        float mx = Input.GetAxisRaw("Mouse X");
        float my = Input.GetAxisRaw("Mouse Y");
        return (mx * mx + my * my) > 0.0001f;
    }

    /// <summary>Checks for any gamepad input this frame.</summary>
    private static bool AnyControllerInput()
    {
        // Joystick buttons 0-19
        for (int i = (int)KeyCode.JoystickButton0; i <= (int)KeyCode.JoystickButton19; i++)
            if (Input.GetKeyDown((KeyCode)i)) return true;

        // Joystick axes — but only when NO keyboard movement keys are held,
        // because "Horizontal"/"Vertical" respond to both WASD and joystick.
        bool keyboardMoving = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) ||
                              Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D) ||
                              Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow) ||
                              Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow);

        if (!keyboardMoving)
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            if (h * h + v * v > 0.04f) return true;
        }

        return false;
    }
}
