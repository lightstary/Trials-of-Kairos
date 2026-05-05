using UnityEngine;
using UnityEngine.UI;
using System;
using System.Reflection;

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

    private bool _inputSystemAvailable;
    private PropertyInfo _gamepadCurrentProp;
    private PropertyInfo _rightStickProp;
    private MethodInfo _readValueMethod;

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

        float mx = Input.GetAxisRaw("Mouse X");
        float my = Input.GetAxisRaw("Mouse Y");
        float currentMouseSens = GameSettings.MouseSensitivity;
        float invertMul = GameSettings.InvertYAxis ? 1f : -1f;
        if (Mathf.Abs(mx) > MOUSE_DEAD_ZONE || Mathf.Abs(my) > MOUSE_DEAD_ZONE)
        {
            _yaw   += mx * currentMouseSens;
            _pitch += my * invertMul * currentMouseSens;
        }

        float stickDead = GameSettings.StickDeadzone;
        float currentStickSens = GameSettings.StickSensitivity;
        Vector2 rs = ReadRightStick();
        if (rs.sqrMagnitude > stickDead * stickDead)
        {
            _yaw   += rs.x * currentStickSens * Time.unscaledDeltaTime;
            _pitch += rs.y * invertMul * currentStickSens * Time.unscaledDeltaTime;
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

    private bool IsLookEnabled()
    {
        if (MainMenuController.Instance != null
            && MainMenuController.Instance.menuPanel != null
            && MainMenuController.Instance.menuPanel.activeSelf)
            return false;

        PauseMenuController pmc = GetPauseMenu();
        if (pmc != null && pmc.IsPaused) return false;

        if (BossIntroModal.IsOpen) return false;
        if (BossFailUI.IsOpen) return false;
        if (GoalTile.IsOpen) return false;
        if (HowToPlayController.IsAnyOpen) return false;

        if (HasActiveNonHUDButton()) return false;

        return true;
    }

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