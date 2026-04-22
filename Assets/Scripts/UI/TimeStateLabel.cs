using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Displays the current time state (FORWARD / FROZEN / REVERSE)
/// as a large, color-coded label directly underneath the Time Scale meter.
/// Present in every gameplay scene (Citadel, Garden, Clock, Hub).
/// </summary>
public class TimeStateLabel : MonoBehaviour
{
    private static readonly Color FORWARD_COLOR = new Color(0.961f, 0.784f, 0.259f, 1f);
    private static readonly Color FROZEN_COLOR  = new Color(0.353f, 0.706f, 0.941f, 1f);
    private static readonly Color REVERSE_COLOR = new Color(0.608f, 0.361f, 0.898f, 1f);

    private const float FONT_SIZE    = 32f;
    private const float CHAR_SPACING = 14f;

    private TextMeshProUGUI _label;
    private bool _subscribed;

    /// <summary>
    /// Call this to create the TimeStateLabel. Safe to call multiple times —
    /// it cleans up any existing instances first.
    /// </summary>
    public static void EnsureExists()
    {
        // Destroy any stale instances
        TimeStateLabel[] existing = Object.FindObjectsOfType<TimeStateLabel>();
        for (int i = 0; i < existing.Length; i++)
            Destroy(existing[i].gameObject);

        GameObject go = new GameObject("[TimeStateLabel]");
        go.AddComponent<TimeStateLabel>();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneCallback()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string sceneName = scene.name;

        // MainScene hosts both Trial Selection and the Citadel.
        // The label is spawned from MainMenuController.SkipToGameplay() instead,
        // so we skip it here to avoid showing it during menus.
        if (sceneName == "MainScene") return;

        // All other gameplay scenes (Garden, Clock, Hub) spawn immediately.
        EnsureExists();
    }

    void Start()
    {
        StartCoroutine(InitRoutine());
    }

    private IEnumerator InitRoutine()
    {
        // Wait multiple frames to ensure all canvas/UI elements are built.
        yield return null;
        yield return null;
        yield return null;

        if (!BuildLabel())
        {
            Debug.LogWarning("[TimeStateLabel] BuildLabel failed on first attempt — retrying.");
            yield return null;
            yield return null;
            if (!BuildLabel())
            {
                Debug.LogError("[TimeStateLabel] BuildLabel failed after retry — destroying self.");
                Destroy(gameObject);
                yield break;
            }
        }

        SubscribeToStateChanges();
    }

    /// <summary>Subscribes to state change events and syncs with current state.</summary>
    private void SubscribeToStateChanges()
    {
        if (_subscribed) return;
        _subscribed = true;

        if (TimeStateUIManager.Instance != null)
            TimeStateUIManager.Instance.OnTimeStateChanged += OnStateChanged;

        if (TimeState.Instance != null)
            OnStateChanged(TimeState.Instance.currentState);

        Debug.Log("[TimeStateLabel] Subscribed to state changes.");
    }

    void OnDestroy()
    {
        if (TimeStateUIManager.Instance != null)
            TimeStateUIManager.Instance.OnTimeStateChanged -= OnStateChanged;
    }

    private void OnStateChanged(TimeState.State state)
    {
        if (_label == null) return;

        switch (state)
        {
            case TimeState.State.Forward:
                _label.text  = "FORWARD";
                _label.color = FORWARD_COLOR;
                break;
            case TimeState.State.Frozen:
                _label.text  = "FROZEN";
                _label.color = FROZEN_COLOR;
                break;
            case TimeState.State.Reverse:
                _label.text  = "REVERSE";
                _label.color = REVERSE_COLOR;
                break;
        }
    }

    /// <summary>Creates the label on the HUD canvas, positioned below the meter.</summary>
    private bool BuildLabel()
    {
        // Find ANY canvas in the scene. Prefer the GameCanvas / HUD canvas.
        Canvas canvas = null;

        // Strategy 1: HUDController's canvas
        if (HUDController.Instance != null)
            canvas = HUDController.Instance.GetComponentInParent<Canvas>();

        // Strategy 2: Find a canvas that contains a TimeScaleMeter
        if (canvas == null)
        {
            TimeScaleMeter meter = Object.FindObjectOfType<TimeScaleMeter>(true);
            if (meter != null)
                canvas = meter.GetComponentInParent<Canvas>();
        }

        // Strategy 3: Any canvas at all
        if (canvas == null)
        {
            Canvas[] allCanvases = Object.FindObjectsOfType<Canvas>(true);
            foreach (Canvas c in allCanvases)
            {
                if (c.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    canvas = c;
                    break;
                }
            }
        }

        if (canvas == null)
        {
            Debug.LogWarning("[TimeStateLabel] No Canvas found in scene.");
            return false;
        }

        // Parent to the HUD panel if it exists — this ensures the label
        // hides automatically when overlay screens (Controls, Pause) are shown.
        // Otherwise, parent to the canvas at sibling index 0 so overlays render on top.
        Transform labelParent = canvas.transform;
        if (HUDController.Instance != null)
        {
            labelParent = HUDController.Instance.transform;
        }

        GameObject go = new GameObject("TimeStateLabel_Text");
        go.transform.SetParent(labelParent, false);

        if (labelParent == canvas.transform)
        {
            // No HUD — place at sibling index 0 so overlays render on top
            go.transform.SetAsFirstSibling();
        }

        RectTransform rt = go.AddComponent<RectTransform>();

        // The TimeScaleMeter is anchored at top-center (0.5, 1.0),
        // positioned at y=-8, height=60. Bottom edge at y=-68.
        // Place the label 8px below that = y=-76.
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -76f);
        rt.sizeDelta = new Vector2(500f, 48f);

        _label = go.AddComponent<TextMeshProUGUI>();
        _label.fontSize = FONT_SIZE;
        _label.characterSpacing = CHAR_SPACING;
        _label.alignment = TextAlignmentOptions.Center;
        _label.raycastTarget = false;
        _label.text = "FORWARD";
        _label.color = FORWARD_COLOR;
        CinzelFontHelper.Apply(_label, true);

        // Dark drop shadow via TMP's material underlay for legibility
        // against any background (sky, bright geometry, etc.)
        Material mat = _label.fontMaterial;
        mat.EnableKeyword("UNDERLAY_ON");
        mat.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, 0.85f));
        mat.SetFloat("_UnderlayOffsetX", 0.4f);
        mat.SetFloat("_UnderlayOffsetY", -0.4f);
        mat.SetFloat("_UnderlayDilate", 0.3f);
        mat.SetFloat("_UnderlaySoftness", 0.25f);

        Debug.Log($"[TimeStateLabel] Label created on canvas '{canvas.name}' at y=-76.");
        return true;
    }
}
