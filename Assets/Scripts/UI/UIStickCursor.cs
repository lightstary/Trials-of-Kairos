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

    // ── Private ──────────────────────────────────────────────────────────
    private RectTransform   _root;
    private Image           _coreImg;
    private Image           _innerGlowImg;
    private Image           _outerGlowImg;
    private RectTransform[] _trailRTs;
    private Image[]         _trailImgs;

    private RectTransform   _canvasRT;
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

    /// <summary>True when the mouse is the active cursor input.</summary>
    public static bool IsMouseMode { get; private set; }

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

    void Update()
    {
        if (_canvasRT == null) return;
        if (!_built) Build();
        if (_root == null) return;

        // ── Gameplay detection: hide cursor if in gameplay ───────────────
        bool inMenu = IsAnyMenuActive();
        if (!inMenu)
        {
            SetCursorVisible(false);
            IsMouseMode = false;
            return;
        }

        // ── Read left stick ──────────────────────────────────────────────
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector2 rawStick = new Vector2(h, v);

        // Apply dead zone with smooth ramp
        float mag = rawStick.magnitude;
        Vector2 stick = Vector2.zero;
        if (mag > DEAD_ZONE)
        {
            float remapped = (mag - DEAD_ZONE) / (1f - DEAD_ZONE);
            stick = rawStick.normalized * remapped;
        }

        bool stickMoved = stick.sqrMagnitude > 0.01f;

        // ── Read mouse ───────────────────────────────────────────────────
        Vector2 mouseScreen = (Vector2)Input.mousePosition;
        bool mouseMoved = (mouseScreen - _lastMouseScreenPos).sqrMagnitude > 4f;
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

        // Keep cursor + trail on top of everything (modals/popups created after cursor)
        for (int i = 0; i < TRAIL_COUNT; i++)
            if (_trailRTs != null && _trailRTs[i] != null) _trailRTs[i].transform.SetAsLastSibling();
        _root.transform.SetAsLastSibling();

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
        if (!UIGamepadNavigator.AConsumedThisFrame
            && !UIGamepadNavigator.SuppressInput
            && _hoveredSel != null)
        {
            bool aPressed    = Input.GetKeyDown(KeyCode.JoystickButton0);
            bool mouseClick  = mouseActive && Input.GetMouseButtonDown(0);

            if (aPressed || mouseClick)
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

    /// <summary>True when any menu overlay is visible (main menu, pause, sub-screens, popups).</summary>
    private bool IsAnyMenuActive()
    {
        MainMenuController mmc = MainMenuController.Instance;
        if (mmc != null && mmc.menuPanel != null && mmc.menuPanel.activeSelf) return true;

        // Trial select screen
        if (mmc != null)
        {
            Transform ts = mmc.transform.parent != null ? mmc.transform.parent.Find("TrialSelectScreen") : null;
            if (ts == null) ts = mmc.transform.Find("TrialSelectScreen");
            if (ts != null && ts.gameObject.activeSelf) return true;
        }

        // Pause menu
        PauseMenuController pmc = FindObjectOfType<PauseMenuController>();
        if (pmc != null && pmc.IsPaused) return true;

        // Controls screen (opened from pause)
        ControlsScreenController ctrl = FindObjectOfType<ControlsScreenController>();
        if (ctrl != null && ctrl.gameObject.activeInHierarchy) return true;

        // Boss fail UI
        BossFailUI failUI = FindObjectOfType<BossFailUI>();
        if (failUI != null && failUI.gameObject.activeInHierarchy) return true;

        // Hub completion popup
        GameObject overlay = GameObject.Find("CompletionOverlay");
        if (overlay != null && overlay.activeInHierarchy) return true;

        // TimeScale intro modal (pauses game, needs cursor for button navigation)
        GameObject modal = GameObject.Find("TimeScaleIntroModal");
        if (modal != null && modal.activeInHierarchy) return true;

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

        Canvas parentCanvas = _canvasRT != null ? _canvasRT.GetComponent<Canvas>() : null;
        // For ScreenSpaceOverlay, worldCamera must be null even if one is assigned
        // in the inspector. Using the 3D camera gives completely wrong coordinates.
        Camera uiCam = null;
        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCam = parentCanvas.worldCamera;
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
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    // ── Build ────────────────────────────────────────────────────────────

    private void Build()
    {
        _built = true;
        if (_canvasRT == null) return;

        _targetPos = Vector2.zero;
        _smoothPos = Vector2.zero;

        // Initialize dynamic colors to defaults
        _currentHue = new Color(0.96f, 0.82f, 0.40f); // gold
        _targetHue  = _currentHue;
        _coreColor  = DEFAULT_CORE_COLOR;
        _innerColor = DEFAULT_INNER_COLOR;
        _outerColor = DEFAULT_OUTER_COLOR;
        _trailColor = DEFAULT_TRAIL_COLOR;

        // Generate soft radial gradient sprite (circle, not a square)
        _orbSprite = CreateOrbSprite(64);

        // ── Trail segments (rendered behind cursor, parented to canvas) ──
        _trailRTs  = new RectTransform[TRAIL_COUNT];
        _trailImgs = new Image[TRAIL_COUNT];

        for (int i = TRAIL_COUNT - 1; i >= 0; i--)
        {
            float t = (float)i / TRAIL_COUNT;
            float size = Mathf.Lerp(TRAIL_START_R, 2f, t) * 2f;

            GameObject tGO = new GameObject($"CursorTrail_{i}");
            tGO.transform.SetParent(_canvasRT, false);
            RectTransform tRT = tGO.AddComponent<RectTransform>();
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
        rootGO.transform.SetParent(_canvasRT, false);
        rootGO.transform.SetAsLastSibling();
        _root = rootGO.AddComponent<RectTransform>();
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
