using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Left-stick free cursor: a soft glowing orb that moves freely over menus.
/// Hovers UI Selectables, highlights them, and A-button activates.
/// Automatically hidden during gameplay (HUD active, no menu overlay).
/// Yields to D-pad when D-pad is used; reactivates when stick moves.
/// </summary>
public class UIStickCursor : MonoBehaviour
{
    // ── Tuning ───────────────────────────────────────────────────────────
    private const float CURSOR_SPEED    = 1100f;
    private const float DEAD_ZONE       = 0.15f;
    private const float SMOOTHING       = 14f;
    private const float PULSE_SPEED     = 2.8f;
    private const int   TRAIL_COUNT     = 6;
    private const float TRAIL_LERP      = 8f;
    private const float HIDE_DELAY      = 4f;

    // Sizes
    private const float CORE_RADIUS     = 10f;
    private const float INNER_GLOW_R    = 28f;
    private const float OUTER_GLOW_R    = 56f;
    private const float TRAIL_START_R   = 8f;

    // Default colors — warm gold/white orb with soft bloom (dynamically tinted by shimmer)
    private static readonly Color DEFAULT_CORE_COLOR   = new Color(1.00f, 0.95f, 0.82f, 0.95f);
    private static readonly Color DEFAULT_INNER_COLOR  = new Color(0.96f, 0.82f, 0.40f, 0.30f);
    private static readonly Color DEFAULT_OUTER_COLOR  = new Color(0.96f, 0.78f, 0.26f, 0.10f);
    private static readonly Color DEFAULT_TRAIL_COLOR  = new Color(0.96f, 0.82f, 0.40f, 0.18f);

    // ── Shared state ────────────────────────────────────────────────────
    /// <summary>True when the left stick is the active input mode.</summary>
    public static bool IsStickMode { get; set; }

    /// <summary>True when the cursor orb is currently visible on screen.</summary>
    public static bool IsCursorVisible { get; private set; }

    /// <summary>The Selectable currently under the cursor, or null.</summary>
    public static Selectable HoveredSelectable { get; private set; }

    /// <summary>The cursor's current world position (screen-space for Overlay canvases).</summary>
    public static Vector3 CursorWorldPosition { get; private set; }

    /// <summary>When true, the cursor will not clear the EventSystem selection.
    /// Used by menus that manage their own D-pad navigation.</summary>
    public static bool PreserveSelection { get; set; }

    /// <summary>True when the mouse is the active input mode.</summary>
    public static bool IsMouseMode { get; set; }

    /// <summary>Frames remaining to ignore mouse position changes (e.g. after resolution switch).</summary>
    private static int _ignoreMouseFrames;

    /// <summary>Call after a resolution or display-mode change to prevent the cursor
    /// from snapping to center when Unity resets Input.mousePosition.</summary>
    public static void SuppressMouseSwitch(int frames = 3)
    {
        _ignoreMouseFrames = frames;
    }

    // ── Private ──────────────────────────────────────────────────────────
    private RectTransform   _root;
    private Image           _coreImg;
    private Image           _innerGlowImg;
    private Image           _outerGlowImg;
    private RectTransform[] _trailRTs;
    private Image[]         _trailImgs;

    private RectTransform   _canvasRT;
    // No dedicated cursor canvas — cursor elements live inside the scene's
    // main GameCanvas and are reparented to the topmost active canvas each
    // frame via LateUpdate so they always render last.
    private Vector2         _targetPos;
    private Vector2         _smoothPos;
    private float           _lastStickTime = -100f;
    private bool            _built;
    private Selectable      _hoveredSel;
    private Sprite          _orbSprite;

    // Dynamic shimmer color tracking
    private Color           _currentHue;
    private Color           _targetHue;
    private Color           _coreColor;
    private Color           _innerColor;
    private Color           _outerColor;
    private Color           _trailColor;

    // Mouse tracking
    private Vector2         _lastMouseScreenPos;
    private float           _lastMouseMoveTime = -100f;

    // ── Lifecycle ────────────────────────────────────────────────────────

    void Awake()
    {
        // Reset static state on scene load — prevents stale cursor visibility
        // after Retry/Restart reloads the scene (statics survive scene loads).
        IsStickMode = false;
        IsMouseMode = false;
        IsCursorVisible = false;
        HoveredSelectable = null;
    }

    void Start()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas != null) _canvasRT = canvas.GetComponent<RectTransform>();
    }

    /// <summary>
    /// Every frame after all Updates, reparent the cursor elements to
    /// whichever root canvas currently has the highest sortingOrder.
    /// This guarantees the cursor renders last regardless of what
    /// dynamic canvases modals create at runtime.
    /// </summary>
    void LateUpdate()
    {
        if (_root == null || !IsCursorVisible) return;

        // Find the topmost root canvas
        Canvas topCanvas = null;
        int topSort = int.MinValue;
        Canvas[] allCanvases = FindObjectsOfType<Canvas>();
        foreach (Canvas c in allCanvases)
        {
            if (!c.isRootCanvas) continue;
            if (c.sortingOrder > topSort)
            {
                topSort = c.sortingOrder;
                topCanvas = c;
            }
        }

        if (topCanvas == null) return;
        RectTransform targetRT = topCanvas.GetComponent<RectTransform>();
        if (targetRT == null) return;

        // Reparent cursor elements to the topmost canvas if needed
        if (_root.parent != targetRT)
        {
            // Convert current screen position to new canvas space
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, _root.position);

            _root.SetParent(targetRT, false);
            for (int i = 0; i < TRAIL_COUNT; i++)
            {
                if (_trailRTs != null && _trailRTs[i] != null)
                    _trailRTs[i].SetParent(targetRT, false);
            }

            // Recalculate anchored position in new canvas space
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                targetRT, screenPos, null, out localPoint);
            _smoothPos = localPoint;
            _targetPos = localPoint;
            _root.anchoredPosition = _smoothPos;
        }

        // Always keep cursor elements as last siblings so they draw last
        for (int i = 0; i < TRAIL_COUNT; i++)
        {
            if (_trailRTs != null && _trailRTs[i] != null)
                _trailRTs[i].SetAsLastSibling();
        }
        _root.SetAsLastSibling();
    }

    /// <summary>Clean up cursor visuals when this component is destroyed.</summary>
    void OnDestroy()
    {
        if (_root != null) Destroy(_root.gameObject);
        if (_trailRTs != null)
        {
            foreach (var t in _trailRTs)
                if (t != null) Destroy(t.gameObject);
        }
    }

    void Update()
    {
        // ── Rebuild safety: if container was destroyed (scene transition), rebuild ──
        if (_canvasRT == null)
        {
            Canvas c = GetComponentInParent<Canvas>();
            if (c == null) c = FindObjectOfType<Canvas>();
            if (c != null) _canvasRT = c.GetComponent<RectTransform>();
        }
        if (_canvasRT == null) return;

        if (_built && (_root == null))
        {
            _built = false;
        }
        if (!_built) Build();
        if (_canvasRT == null || _root == null) return;

        // ── Gameplay detection: hide cursor unless interactive UI is present ──
        bool inMenu = HasInteractableUI();
        if (!inMenu)
        {
            SetCursorVisible(false);
            SetTrailVisible(false);
            IsMouseMode = false;
            // Ensure system cursor is hidden during gameplay even in scenes
            // without CameraFollow (e.g., Hub).
            Cursor.visible = false;
            return;
        }

        // ── Interactive UI is present — unlock cursor so mouse clicks work ──
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;

        // Clear stale EventSystem selection to prevent Spacebar/Enter from
        // auto-submitting a button the player didn't explicitly hover.
        // Controller confirm (A button) is handled manually in CheckHover,
        // so this does not break controller flow.
        // When PreserveSelection is true, menus manage their own selection.
        if (!PreserveSelection
            && EventSystem.current != null
            && EventSystem.current.currentSelectedGameObject != null)
            EventSystem.current.SetSelectedGameObject(null);

        // ── Read left stick (CONTROLLER ONLY — exclude keyboard WASD) ────
        // When SuppressInput is active, skip stick reading to prevent cursor
        // interference during hold-A slider drag in options menus.
        Vector2 stick = Vector2.zero;
        bool stickMoved = false;

        bool keyboardMoving = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) ||
                              Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D) ||
                              Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow) ||
                              Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow);

        if (!keyboardMoving)
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector2 rawStick = new Vector2(h, v);

            float mag = rawStick.magnitude;
            if (mag > DEAD_ZONE)
            {
                float remapped = (mag - DEAD_ZONE) / (1f - DEAD_ZONE);
                stick = rawStick.normalized * remapped;
            }

            stickMoved = stick.sqrMagnitude > 0.01f;
        }

        // ── Read mouse ───────────────────────────────────────────────────
        Vector2 mouseScreen = (Vector2)Input.mousePosition;
        bool mouseMoved;
        if (_ignoreMouseFrames > 0)
        {
            // After a resolution change, Input.mousePosition jumps — ignore it
            _ignoreMouseFrames--;
            mouseMoved = false;
            _lastMouseScreenPos = mouseScreen;
        }
        else
        {
            mouseMoved = (mouseScreen - _lastMouseScreenPos).sqrMagnitude > 4f;
        }
        _lastMouseScreenPos = mouseScreen;

        // ── Mode switching: stick vs mouse vs D-pad ──────────────────────
        if (stickMoved)
        {
            IsStickMode = true;
            IsMouseMode = false;
            _lastStickTime = Time.unscaledTime;
        }
        else if (mouseMoved)
        {
            IsMouseMode = true;
            IsStickMode = false;
            _lastMouseMoveTime = Time.unscaledTime;
        }

        bool stickActive = IsStickMode && (Time.unscaledTime - _lastStickTime) < HIDE_DELAY;
        bool mouseActive = IsMouseMode && (Time.unscaledTime - _lastMouseMoveTime) < HIDE_DELAY;
        bool shouldShow  = stickActive || mouseActive;

        SetCursorVisible(shouldShow);
        SetTrailVisible(shouldShow);

        if (!shouldShow) return;

        // ── Movement ─────────────────────────────────────────────────────
        if (mouseActive)
        {
            // Convert mouse screen position to canvas anchored position
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRT, mouseScreen, null, out localPoint);
            _targetPos = localPoint;
            // Snap smooth position closer for responsive mouse feel
            _smoothPos = Vector2.Lerp(_smoothPos, _targetPos, SMOOTHING * 2f * Time.unscaledDeltaTime);
        }
        else if (stickActive && stickMoved)
        {
            _targetPos += stick * CURSOR_SPEED * Time.unscaledDeltaTime;

            Vector2 half = _canvasRT.rect.size * 0.5f;
            _targetPos.x = Mathf.Clamp(_targetPos.x, -half.x + 10f, half.x - 10f);
            _targetPos.y = Mathf.Clamp(_targetPos.y, -half.y + 10f, half.y - 10f);
        }

        if (!mouseActive)
        {
            // Smooth interpolation for fluid feel (stick mode)
            _smoothPos = Vector2.Lerp(_smoothPos, _targetPos, SMOOTHING * Time.unscaledDeltaTime);
        }

        _root.anchoredPosition = _smoothPos;
        CursorWorldPosition = _root.position;

        // ── Update trail ─────────────────────────────────────────────────
        UpdateTrail();

        // ── Animate pulse ────────────────────────────────────────────────
        AnimatePulse();

        // ── Hover detection ──────────────────────────────────────────────
        CheckHover(stickMoved || mouseMoved);

        // ── A button / left-click confirms hovered element ───────────────
        // Check AConsumedThisFrame so another handler (e.g., TrialSelectController)
        // that already processed this press doesn't cause a double-fire.
        // Also skip when SuppressInput is active — another controller fully owns input.
        // NOTE: Mouse clicks are handled natively by Unity's EventSystem via
        // GraphicRaycaster — do NOT manually invoke onClick for mouse, only for
        // controller A button which bypasses EventSystem pointer input.
        if (!UIGamepadNavigator.AConsumedThisFrame
            && !UIGamepadNavigator.SuppressInput
            && _hoveredSel != null)
        {
            bool aPressed = Input.GetKeyDown(KeyCode.JoystickButton0);

            if (aPressed)
            {
                UIGamepadNavigator.AConsumedThisFrame = true;

                Button btn = _hoveredSel.GetComponent<Button>();
                if (btn != null)
                    btn.onClick.Invoke();
                else
                    ExecuteEvents.Execute(_hoveredSel.gameObject,
                        new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
            }
        }
    }

    // ── Visibility ───────────────────────────────────────────────────────

    private void SetCursorVisible(bool visible)
    {
        IsCursorVisible = visible;
        if (_root != null && _root.gameObject.activeSelf != visible)
            _root.gameObject.SetActive(visible);

        // Always hide the hardware cursor — the orb replaces it entirely.
        // During menus the orb is visible; during gameplay both are hidden.
        Cursor.visible = false;
    }

    private void SetTrailVisible(bool visible)
    {
        if (_trailRTs == null) return;
        for (int i = 0; i < TRAIL_COUNT; i++)
        {
            if (_trailRTs[i] != null && _trailRTs[i].gameObject.activeSelf != visible)
                _trailRTs[i].gameObject.SetActive(visible);
        }
    }

    /// <summary>
    /// Returns true when the player needs a free cursor — either because
    /// interactive Buttons are on screen, or a known modal that handles
    /// its own input (no Button components) is open.
    /// </summary>
    private bool HasInteractableUI()
    {
        // ── Check for modals that don't use Button components ────────────
        // HowToPlayController navigates via direct Input.GetKeyDown.
        // It still needs the cursor so the player can see and move it.
        if (HowToPlayController.IsAnyOpen) return true;

        // ── Check for active, interactable Buttons (non-HUD) ────────────
        var selectables = Selectable.allSelectablesArray;
        int count = Selectable.allSelectableCount;

        for (int i = 0; i < count; i++)
        {
            Selectable s = selectables[i];
            if (s == null) continue;
            if (!(s is Button)) continue;
            if (!s.interactable) continue;
            if (!s.gameObject.activeInHierarchy) continue;

            // Gameplay HUD buttons (e.g., PauseButton) must not trigger the cursor
            if (IsInsideHUD(s.transform)) continue;

            // Skip buttons inside invisible or non-interactable CanvasGroups
            CanvasGroup cg = s.GetComponentInParent<CanvasGroup>();
            if (cg != null && (!cg.interactable || cg.alpha < 0.01f)) continue;

            return true;
        }

        return false;
    }

    /// <summary>Returns true if the transform is a descendant of a GameObject named "HUD".</summary>
    private static bool IsInsideHUD(Transform t)
    {
        while (t != null)
        {
            if (t.name == "HUD") return true;
            t = t.parent;
        }
        return false;
    }

    // ── Pulse animation ──────────────────────────────────────────────────

    private void AnimatePulse()
    {
        float t = Time.unscaledTime;
        float dt = Time.unscaledDeltaTime;

        // ── Absorb shimmer color like the rotating squares do ────────────
        if (MenuShimmerController.IsSweeping)
        {
            float p = MenuShimmerController.Progress;
            float absorb = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.25f, 0.60f, p));
            if (absorb > 0.01f)
                _targetHue = Color.Lerp(_currentHue, MenuShimmerController.CurrentColor, absorb);
        }

        _currentHue = Color.Lerp(_currentHue, _targetHue, 1.5f * dt);

        // Blend the dynamic hue into the orb colors
        _coreColor  = TintColor(DEFAULT_CORE_COLOR,  _currentHue, 0.45f);
        _innerColor = TintColor(DEFAULT_INNER_COLOR, _currentHue, 0.60f);
        _outerColor = TintColor(DEFAULT_OUTER_COLOR, _currentHue, 0.65f);
        _trailColor = TintColor(DEFAULT_TRAIL_COLOR, _currentHue, 0.55f);

        // ── Layered sine pulse ───────────────────────────────────────────
        float wave = Mathf.Sin(t * PULSE_SPEED) * 0.4f
                   + Mathf.Sin(t * PULSE_SPEED * 1.618f + 0.9f) * 0.35f
                   + Mathf.Sin(t * PULSE_SPEED * 0.618f + 2.1f) * 0.25f;
        float n = (wave + 1f) * 0.5f;
        n = n * n * (3f - 2f * n);

        if (_coreImg != null)
        {
            Color c = _coreColor;
            c.a = Mathf.Lerp(0.80f, 1.00f, n);
            _coreImg.color = c;
        }

        if (_innerGlowImg != null)
        {
            Color c = _innerColor;
            c.a = Mathf.Lerp(0.20f, 0.40f, n);
            _innerGlowImg.color = c;
        }

        if (_outerGlowImg != null)
        {
            Color c = _outerColor;
            c.a = Mathf.Lerp(0.05f, 0.16f, n);
            _outerGlowImg.color = c;
            _outerGlowImg.rectTransform.sizeDelta =
                Vector2.one * Mathf.Lerp(OUTER_GLOW_R * 1.84f, OUTER_GLOW_R * 2.16f, n);
        }
    }

    /// <summary>Tints a base color toward a hue by the given amount, preserving original alpha.</summary>
    private static Color TintColor(Color original, Color hue, float amount)
    {
        return new Color(
            Mathf.Lerp(original.r, hue.r, amount),
            Mathf.Lerp(original.g, hue.g, amount),
            Mathf.Lerp(original.b, hue.b, amount),
            original.a
        );
    }

    // ── Trail ────────────────────────────────────────────────────────────

    private void UpdateTrail()
    {
        if (_trailRTs == null) return;

        for (int i = 0; i < TRAIL_COUNT; i++)
        {
            if (_trailRTs[i] == null) continue;

            Vector2 target = i == 0 ? _smoothPos : _trailRTs[i - 1].anchoredPosition;
            float lerpSpeed = TRAIL_LERP * (1f - (float)i / TRAIL_COUNT * 0.5f);
            _trailRTs[i].anchoredPosition = Vector2.Lerp(
                _trailRTs[i].anchoredPosition, target, lerpSpeed * Time.unscaledDeltaTime);

            if (_trailImgs[i] != null)
            {
                float dist = Vector2.Distance(_trailRTs[i].anchoredPosition, _smoothPos);
                float distFade = Mathf.Clamp01(dist / 60f);
                float indexFade = 1f - (float)i / TRAIL_COUNT;
                Color c = _trailColor;
                c.a = _trailColor.a * indexFade * distFade;
                _trailImgs[i].color = c;
            }
        }
    }

    // ── Hover detection ──────────────────────────────────────────────────

    private void CheckHover(bool stickMoved)
    {
        if (_root == null) return;

        Selectable closest = null;
        float closestDist = float.MaxValue;

        // Cursor canvas is always ScreenSpaceOverlay — no camera needed.
        Camera uiCam = null;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCam, _root.position);

        foreach (Selectable s in Selectable.allSelectablesArray)
        {
            if (s == null || !s.gameObject.activeInHierarchy || !s.interactable) continue;

            // Skip elements inside disabled CanvasGroups
            CanvasGroup cg = s.GetComponentInParent<CanvasGroup>();
            if (cg != null && (!cg.interactable || cg.alpha < 0.01f)) continue;

            RectTransform srt = s.GetComponent<RectTransform>();
            if (srt == null) continue;

            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    srt, screenPoint, uiCam, out localPoint))
            {
                if (srt.rect.Contains(localPoint))
                {
                    float dist = localPoint.sqrMagnitude;
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closest = s;
                    }
                }
            }
        }

        // Update hover state — do NOT touch EventSystem.selectedGameObject.
        // The cursor tracks hover independently to avoid ghost selection.
        if (closest != _hoveredSel)
        {
            // Unhighlight previous
            if (_hoveredSel != null)
                _hoveredSel.OnPointerExit(new UnityEngine.EventSystems.PointerEventData(EventSystem.current));

            _hoveredSel = closest;
            HoveredSelectable = closest;

            // Highlight new
            if (_hoveredSel != null)
                _hoveredSel.OnPointerEnter(new UnityEngine.EventSystems.PointerEventData(EventSystem.current));
        }

        // When cursor is active, clear the EventSystem selection so D-pad mode
        // doesn't have a stale selected object when it resumes.
        // (Selection is also cleared at the top of Update when inMenu is true.)
    }

    // ── Build ────────────────────────────────────────────────────────────

    private void Build()
    {
        _built = true;
        if (_canvasRT == null) return;

        _targetPos = Vector2.zero;
        _smoothPos = Vector2.zero;

        _currentHue = new Color(0.96f, 0.82f, 0.40f);
        _targetHue  = _currentHue;
        _coreColor  = DEFAULT_CORE_COLOR;
        _innerColor = DEFAULT_INNER_COLOR;
        _outerColor = DEFAULT_OUTER_COLOR;
        _trailColor = DEFAULT_TRAIL_COLOR;

        _orbSprite = CreateOrbSprite(64);

        // ── Trail segments ───────────────────────────────────────────────
        _trailRTs  = new RectTransform[TRAIL_COUNT];
        _trailImgs = new Image[TRAIL_COUNT];

        for (int i = TRAIL_COUNT - 1; i >= 0; i--)
        {
            float t = (float)i / TRAIL_COUNT;
            float size = Mathf.Lerp(TRAIL_START_R, 2f, t) * 2f;

            GameObject tGO = new GameObject($"CursorTrail_{i}");
            RectTransform tRT = tGO.AddComponent<RectTransform>();
            tGO.transform.SetParent(_canvasRT, false);
            tRT.anchorMin = tRT.anchorMax = new Vector2(0.5f, 0.5f);
            tRT.pivot = new Vector2(0.5f, 0.5f);
            tRT.sizeDelta = new Vector2(size, size);
            _trailRTs[i] = tRT;

            Image tImg = tGO.AddComponent<Image>();
            tImg.sprite = _orbSprite;
            tImg.color = new Color(DEFAULT_TRAIL_COLOR.r, DEFAULT_TRAIL_COLOR.g, DEFAULT_TRAIL_COLOR.b, 0f);
            tImg.raycastTarget = false;
            _trailImgs[i] = tImg;
        }

        // ── Root container (all orb layers are children) ─────────────────
        GameObject rootGO = new GameObject("StickCursor");
        _root = rootGO.AddComponent<RectTransform>();
        rootGO.transform.SetParent(_canvasRT, false);
        _root.anchorMin = _root.anchorMax = new Vector2(0.5f, 0.5f);
        _root.pivot = new Vector2(0.5f, 0.5f);
        _root.sizeDelta = Vector2.zero;

        // ── Outer glow (big, soft, faint) ────────────────────────────────
        _outerGlowImg = CreateOrbLayer(_root, "OuterGlow", OUTER_GLOW_R * 2f, DEFAULT_OUTER_COLOR);

        // ── Inner glow (medium, warmer) ──────────────────────────────────
        _innerGlowImg = CreateOrbLayer(_root, "InnerGlow", INNER_GLOW_R * 2f, DEFAULT_INNER_COLOR);

        // ── Core (small, bright, almost white) ───────────────────────────
        _coreImg = CreateOrbLayer(_root, "Core", CORE_RADIUS * 2f, DEFAULT_CORE_COLOR);

        rootGO.SetActive(false);
    }

    /// <summary>Creates a child Image with a radial-gradient sprite.</summary>
    private Image CreateOrbLayer(RectTransform parent, string name, float diameter, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(diameter, diameter);
        rt.anchoredPosition = Vector2.zero;

        Image img = go.AddComponent<Image>();
        img.sprite = _orbSprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    /// <summary>Generates a soft radial gradient texture (glowing circle).</summary>
    private Sprite CreateOrbSprite(int resolution)
    {
        Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float center = (resolution - 1) * 0.5f;
        float maxR = center;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dx = (x - center) / maxR;
                float dy = (y - center) / maxR;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                // Soft quadratic falloff with smoothstep — bright center, feathered edge
                float alpha = 1f - Mathf.Clamp01(dist);
                alpha = alpha * alpha;
                alpha *= Mathf.SmoothStep(0f, 1f, 1f - dist);

                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, resolution, resolution),
            new Vector2(0.5f, 0.5f), 100f);
    }
}
