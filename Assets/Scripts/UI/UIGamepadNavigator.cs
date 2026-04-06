using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Reads gamepad left stick and dpad via legacy Input API and drives
/// UI navigation manually. Attach to any always-active GameObject (e.g. EventSystem).
/// Handles Submit (A / joystick button 0), Cancel (B / joystick button 1),
/// and directional navigation via left stick + dpad.
/// </summary>
public class UIGamepadNavigator : MonoBehaviour
{
    private const float STICK_THRESHOLD = 0.45f;
    private const float REPEAT_DELAY   = 0.40f;
    private const float REPEAT_RATE    = 0.14f;

    private float _nextMoveTime;
    private Vector2 _lastDir;

    void Update()
    {
        // ── Read left stick via standard axes ────────────────────────────
        float stickH = Input.GetAxisRaw("Horizontal");
        float stickV = Input.GetAxisRaw("Vertical");
        Vector2 dir = new Vector2(stickH, stickV);

        // Apply dead zone
        if (Mathf.Abs(dir.x) < STICK_THRESHOLD) dir.x = 0f;
        if (Mathf.Abs(dir.y) < STICK_THRESHOLD) dir.y = 0f;

        // ── Also check dpad as discrete buttons ──────────────────────────
        if (dir.sqrMagnitude < 0.01f)
        {
            float dh = 0f, dv = 0f;
            if (Input.GetKey(KeyCode.JoystickButton13) || Input.GetKey("joystick button 13")) dv =  1f; // DPad Up (varies by platform)
            if (Input.GetKey(KeyCode.JoystickButton14) || Input.GetKey("joystick button 14")) dv = -1f; // DPad Down
            if (Input.GetKey(KeyCode.JoystickButton11) || Input.GetKey("joystick button 11")) dh = -1f; // DPad Left
            if (Input.GetKey(KeyCode.JoystickButton12) || Input.GetKey("joystick button 12")) dh =  1f; // DPad Right
            if (Mathf.Abs(dh) > 0.1f || Mathf.Abs(dv) > 0.1f)
                dir = new Vector2(dh, dv);
        }

        if (dir.sqrMagnitude < 0.01f)
        {
            _nextMoveTime = 0f;
            _lastDir = Vector2.zero;
        }
        else
        {
            bool isNewDir = _lastDir.sqrMagnitude < 0.01f || Vector2.Dot(dir.normalized, _lastDir.normalized) < 0.5f;
            if (isNewDir)
            {
                _nextMoveTime = Time.unscaledTime + REPEAT_DELAY;
                _lastDir = dir;
                Navigate(dir);
            }
            else if (Time.unscaledTime >= _nextMoveTime)
            {
                _nextMoveTime = Time.unscaledTime + REPEAT_RATE;
                Navigate(dir);
            }
        }

        // ── Submit: A button (joystick button 0) ─────────────────────────
        if (Input.GetKeyDown(KeyCode.JoystickButton0))
        {
            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (selected != null)
                ExecuteEvents.Execute(selected, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
        }

        // ── Cancel: B button (joystick button 1) ─────────────────────────
        if (Input.GetKeyDown(KeyCode.JoystickButton1))
        {
            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (selected != null)
                ExecuteEvents.Execute(selected, new BaseEventData(EventSystem.current), ExecuteEvents.cancelHandler);
        }
    }

    private void Navigate(Vector2 dir)
    {
        if (EventSystem.current == null) return;

        GameObject current = EventSystem.current.currentSelectedGameObject;
        if (current == null || !current.activeInHierarchy)
        {
            Selectable first = FindFirstActiveSelectable();
            if (first != null)
                EventSystem.current.SetSelectedGameObject(first.gameObject);
            return;
        }

        Selectable sel = current.GetComponent<Selectable>();
        if (sel == null) return;

        Selectable next = null;
        if (Mathf.Abs(dir.y) >= Mathf.Abs(dir.x))
            next = dir.y > 0 ? sel.FindSelectableOnUp() : sel.FindSelectableOnDown();
        else
            next = dir.x > 0 ? sel.FindSelectableOnRight() : sel.FindSelectableOnLeft();

        if (next != null && next.gameObject.activeInHierarchy)
            EventSystem.current.SetSelectedGameObject(next.gameObject);
    }

    /// <summary>Finds the first interactable, active Selectable in the scene.</summary>
    private Selectable FindFirstActiveSelectable()
    {
        foreach (Selectable s in Selectable.allSelectablesArray)
        {
            if (s != null && s.gameObject.activeInHierarchy && s.interactable)
                return s;
        }
        return null;
    }
}
