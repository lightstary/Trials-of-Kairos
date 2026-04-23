using UnityEngine;
using UnityEngine.UI;
using System;
using System.Reflection;

/// <summary>
/// Over-the-shoulder third-person orbit camera.
/// Mouse controls yaw/pitch via legacy Input.
/// Xbox right stick controls yaw/pitch via the new Input System (accessed through
/// reflection to avoid compile-time dependency on the package).
/// Camera look is disabled only when menus or pause are open.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Orbit")]
    [SerializeField] private float distance = 8f;
    [SerializeField] private float followSpeed = 10f;
    [SerializeField] private float rotationSmoothSpeed = 12f;

    [Header("Look Target")]
    public Vector3 lookOffset = new Vector3(0f, 1f, 0f);

    [Header("Sensitivity")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float stickSensitivity = 120f;

    [Header("Pitch Limits")]
    [SerializeField] private float pitchMin = 5f;
    [SerializeField] private float pitchMax = 60f;

    [Header("Starting Angle")]
    [SerializeField] private float startYaw = 0f;
    [SerializeField] private float startPitch = 25f;

    private float _yaw;
    private float _pitch;
    private PauseMenuController _cachedPauseMenu;
    private bool _pauseMenuSearched;

    private const float MOUSE_DEAD_ZONE = 0.02f;
    private const float STICK_DEAD_ZONE = 0.15f;

    // ── Reflection cache for Input System Gamepad.current.rightStick ──
    private bool _inputSystemAvailable;
    private PropertyInfo _gamepadCurrentProp;   // Gamepad.current (static)
    private PropertyInfo _rightStickProp;       // gamepad.rightStick
    private MethodInfo _readValueMethod;        // stickControl.ReadValue()

    void Start()
    {
        _yaw = startYaw;
        _pitch = startPitch;

        InitInputSystemReflection();

        if (player != null)
        {
            transform.position = ComputeOrbitPosition();
            transform.LookAt(player.position + lookOffset);
        }
    }

    /// <summary>
    /// Attempts to locate InputSystem types via reflection.
    /// If the package is installed and the backend is active, Gamepad.current
    /// will return a live device. If not, controller look is gracefully skipped.
    /// </summary>
    private void InitInputSystemReflection()
    {
        try
        {
            Type gamepadType = null;

            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetName().Name == "Unity.InputSystem")
                {
                    gamepadType = asm.GetType("UnityEngine.InputSystem.Gamepad");
                    break;
                }
            }

            if (gamepadType == null)
            {
                Debug.LogWarning("[CameraFollow] Input System package not found. Xbox right stick camera disabled. Install com.unity.inputsystem to enable.");
                return;
            }

            _gamepadCurrentProp = gamepadType.GetProperty("current", BindingFlags.Public | BindingFlags.Static);
            _rightStickProp = gamepadType.GetProperty("rightStick", BindingFlags.Public | BindingFlags.Instance);

            if (_gamepadCurrentProp == null || _rightStickProp == null)
            {
                Debug.LogWarning("[CameraFollow] Could not find Gamepad.current or rightStick properties.");
                return;
            }

            Type stickType = _rightStickProp.PropertyType;
            _readValueMethod = stickType.GetMethod("ReadValue", Type.EmptyTypes);

            if (_readValueMethod == null)
            {
                Debug.LogWarning("[CameraFollow] Could not find ReadValue() on stick control.");
                return;
            }

            _inputSystemAvailable = true;
            Debug.Log("[CameraFollow] Input System detected via reflection. Xbox right stick camera enabled.");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[CameraFollow] Failed to initialize Input System reflection: {e.Message}");
        }
    }

    /// <summary>
    /// Reads the right stick value via cached reflection. Returns Vector2.zero on failure.
    /// </summary>
    private Vector2 ReadRightStick()
    {
        if (!_inputSystemAvailable) return Vector2.zero;

        try
        {
            object gamepad = _gamepadCurrentProp.GetValue(null);
            if (gamepad == null) return Vector2.zero;

            object stick = _rightStickProp.GetValue(gamepad);
            if (stick == null) return Vector2.zero;

            object result = _readValueMethod.Invoke(stick, null);
            return (Vector2)result;
        }
        catch
        {
            return Vector2.zero;
        }
    }

    void Update()
    {
        bool canLook = IsLookEnabled();

        Cursor.lockState = canLook ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = false;

        if (!canLook) return;

        // ── Mouse look (legacy Input) ────────────────────────────────────
        float mx = Input.GetAxisRaw("Mouse X");
        float my = Input.GetAxisRaw("Mouse Y");
        if (Mathf.Abs(mx) > MOUSE_DEAD_ZONE || Mathf.Abs(my) > MOUSE_DEAD_ZONE)
        {
            _yaw   += mx * mouseSensitivity;
            _pitch -= my * mouseSensitivity;
        }

        // ── Controller right stick (Input System via reflection) ─────────
        Vector2 rs = ReadRightStick();
        if (rs.sqrMagnitude > STICK_DEAD_ZONE * STICK_DEAD_ZONE)
        {
            _yaw   += rs.x * stickSensitivity * Time.unscaledDeltaTime;
            _pitch -= rs.y * stickSensitivity * Time.unscaledDeltaTime;
        }

        _pitch = Mathf.Clamp(_pitch, pitchMin, pitchMax);
    }

    void LateUpdate()
    {
        if (player == null) return;

        Vector3 targetPos = ComputeOrbitPosition();

        transform.position = Vector3.Lerp(
            transform.position,
            targetPos,
            followSpeed * Time.unscaledDeltaTime
        );

        Vector3 lookTarget = player.position + lookOffset;
        Vector3 lookDir = lookTarget - transform.position;
        if (lookDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                rotationSmoothSpeed * Time.unscaledDeltaTime
            );
        }
    }

    private Vector3 ComputeOrbitPosition()
    {
        Quaternion orbitRot = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3 offset = orbitRot * new Vector3(0f, 0f, -distance);
        return player.position + lookOffset + offset;
    }

    /// <summary>
    /// Camera look is blocked by any modal overlay: main menu, pause, tutorials,
    /// boss intros, fail screens, how-to-play, goal popups, or time-scale intro.
    /// NOT blocked by Time.timeScale — the game's time-freeze mechanic
    /// sets timeScale to 0 during normal gameplay.
    /// </summary>
    private bool IsLookEnabled()
    {
        // Main menu
        if (MainMenuController.Instance != null
            && MainMenuController.Instance.menuPanel != null
            && MainMenuController.Instance.menuPanel.activeSelf)
            return false;

        // Pause menu
        PauseMenuController pmc = GetPauseMenu();
        if (pmc != null && pmc.IsPaused) return false;

        // Modal popups and tutorials
        if (BossIntroModal.IsOpen) return false;
        if (BossFailUI.IsOpen) return false;
        if (GoalTile.IsOpen) return false;
        if (HowToPlayController.IsAnyOpen) return false;

        // Block look when interactive UI is present (win/gameover/completion screens).
        // Uses the same Selectable scan as UIStickCursor so behavior is consistent.
        if (HasActiveNonHUDButton()) return false;

        return true;
    }

    /// <summary>
    /// Returns true if any active, interactable Button exists outside the HUD.
    /// Catches win screens, game over screens, and any other interactive overlay
    /// without needing a dedicated static flag for each one.
    /// </summary>
    private static bool HasActiveNonHUDButton()
    {
        var selectables = UnityEngine.UI.Selectable.allSelectablesArray;
        int count = UnityEngine.UI.Selectable.allSelectableCount;

        for (int i = 0; i < count; i++)
        {
            var s = selectables[i];
            if (s == null) continue;
            if (!(s is UnityEngine.UI.Button)) continue;
            if (!s.interactable) continue;
            if (!s.gameObject.activeInHierarchy) continue;

            // Exclude HUD buttons
            Transform t = s.transform;
            bool isHud = false;
            while (t != null)
            {
                if (t.name == "HUD") { isHud = true; break; }
                t = t.parent;
            }
            if (isHud) continue;

            CanvasGroup cg = s.GetComponentInParent<CanvasGroup>();
            if (cg != null && (!cg.interactable || cg.alpha < 0.01f)) continue;

            return true;
        }
        return false;
    }

    private PauseMenuController GetPauseMenu()
    {
        if (!_pauseMenuSearched)
        {
            _cachedPauseMenu = FindObjectOfType<PauseMenuController>(true);
            _pauseMenuSearched = true;
        }
        return _cachedPauseMenu;
    }
}
