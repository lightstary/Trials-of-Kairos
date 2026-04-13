using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// D-pad step navigation and A/B button handling for UI menus.
/// Left stick is NEVER used here — it belongs exclusively to UIStickCursor.
/// When cursor mode is active (IsStickMode), this script does NOT fire submit/cancel
/// and does NOT auto-select. The cursor handles its own hover+click.
/// </summary>
public class UIGamepadNavigator : MonoBehaviour
{
    private const float REPEAT_DELAY    = 0.40f;
    private const float REPEAT_RATE     = 0.14f;

    private float   _nextMoveTime;
    private Vector2 _lastDir;

    /// <summary>Set by UIStickCursor when it consumes the A press this frame.</summary>
    public static bool AConsumedThisFrame { get; set; }

    /// <summary>When true, this navigator yields ALL input to another handler (e.g., TrialSelectController).</summary>
    public static bool SuppressInput { get; set; }

    void Awake()
    {
        // Disable EventSystem navigation events to prevent StandaloneInputModule
        // from competing with this navigator for controller/keyboard input.
        EventSystem es = GetComponent<EventSystem>();
        if (es == null) es = FindObjectOfType<EventSystem>();
        if (es != null)
            es.sendNavigationEvents = false;
    }

    void LateUpdate()
    {
        AConsumedThisFrame = false;
    }

    void Update()
    {
        // When another controller (e.g., TrialSelectController) owns all input,
        // this navigator must not navigate, submit, cancel, or auto-select.
        if (SuppressInput)
            return;

        // ── D-PAD ONLY (never left stick) ────────────────────────────────
        Vector2 dir = Vector2.zero;

        // D-pad as buttons
        if (Input.GetKey(KeyCode.JoystickButton13)) dir.y =  1f;
        if (Input.GetKey(KeyCode.JoystickButton14)) dir.y = -1f;
        if (Input.GetKey(KeyCode.JoystickButton11)) dir.x = -1f;
        if (Input.GetKey(KeyCode.JoystickButton12)) dir.x =  1f;

        // D-pad as axes
        if (dir.sqrMagnitude < 0.01f)
        {
            float dh = 0f, dv = 0f;
            try { dh = Input.GetAxisRaw("DPadHorizontal"); } catch { }
            try { dv = Input.GetAxisRaw("DPadVertical"); } catch { }
            if (Mathf.Abs(dh) > 0.4f || Mathf.Abs(dv) > 0.4f)
                dir = new Vector2(dh, dv);
        }

        // Keyboard arrows
        if (dir.sqrMagnitude < 0.01f)
        {
            if (Input.GetKey(KeyCode.UpArrow))    dir.y =  1f;
            if (Input.GetKey(KeyCode.DownArrow))  dir.y = -1f;
            if (Input.GetKey(KeyCode.LeftArrow))  dir.x = -1f;
            if (Input.GetKey(KeyCode.RightArrow)) dir.x =  1f;
        }

        // Clean diagonals — prefer dominant axis
        if (dir.sqrMagnitude > 0.01f && Mathf.Abs(dir.x) > 0.01f && Mathf.Abs(dir.y) > 0.01f)
        {
            if (Mathf.Abs(dir.y) >= Mathf.Abs(dir.x)) dir.x = 0f;
            else dir.y = 0f;
        }

        // ── Navigation with repeat ──────────────────────────────────────
        if (dir.sqrMagnitude < 0.01f)
        {
            _nextMoveTime = 0f;
            _lastDir = Vector2.zero;
        }
        else
        {
            // D-pad used → switch to D-pad mode, hide cursor
            UIStickCursor.IsStickMode = false;

            bool isNewDir = _lastDir.sqrMagnitude < 0.01f
                         || Vector2.Dot(dir.normalized, _lastDir.normalized) < 0.5f;
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

        // ── When cursor mode is active, do NOT process submit/cancel/auto-select ─
        // The cursor handles its own A-click on hovered items exclusively.
        if (UIStickCursor.IsStickMode)
            return;

        // ── Submit: A button (D-pad mode only) ───────────────────────────
        if (!AConsumedThisFrame)
        {
            if (Input.GetKeyDown(KeyCode.JoystickButton0) || Input.GetKeyDown(KeyCode.Return))
            {
                GameObject selected = EventSystem.current != null
                    ? EventSystem.current.currentSelectedGameObject : null;
                if (selected != null && selected.activeInHierarchy)
                {
                    // Don't submit on objects inside hidden or non-interactable CanvasGroups.
                    // This prevents stale selections (e.g., main menu buttons) from firing
                    // when another screen (trial select) is active on top.
                    CanvasGroup cg = selected.GetComponentInParent<CanvasGroup>();
                    bool blocked = cg != null && (!cg.interactable || cg.alpha < 0.01f);
                    if (!blocked)
                        ExecuteEvents.Execute(selected,
                            new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
                }
            }
        }

        // ── Cancel: B button ─────────────────────────────────────────────
        if (Input.GetKeyDown(KeyCode.JoystickButton1))
        {
            GameObject selected = EventSystem.current != null
                ? EventSystem.current.currentSelectedGameObject : null;
            if (selected != null && selected.activeInHierarchy)
                ExecuteEvents.Execute(selected,
                    new BaseEventData(EventSystem.current), ExecuteEvents.cancelHandler);
        }

        // ── Auto-select if nothing selected (D-pad mode only) ────────────
        if (EventSystem.current != null
            && EventSystem.current.currentSelectedGameObject == null)
        {
            Selectable first = FindFirstActiveSelectable();
            if (first != null)
                EventSystem.current.SetSelectedGameObject(first.gameObject);
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

    /// <summary>Finds the first visible, interactable Selectable in the scene.</summary>
    private Selectable FindFirstActiveSelectable()
    {
        foreach (Selectable s in Selectable.allSelectablesArray)
        {
            if (s == null || !s.gameObject.activeInHierarchy || !s.interactable)
                continue;

            CanvasGroup cg = s.GetComponentInParent<CanvasGroup>();
            if (cg != null && (!cg.interactable || cg.alpha < 0.01f))
                continue;

            return s;
        }
        return null;
    }
}
