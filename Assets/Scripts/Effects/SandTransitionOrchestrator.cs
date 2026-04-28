using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dissolves scene elements into sand grains before a scene transition.
/// Auto-detects whether the current view is a UI screen (trial select,
/// main menu) or a 3D level and dissolves elements accordingly.
///
/// For UI: spawns small amber-colored Image "grains" within the canvas
/// that drift upward and fade — matching the player death sand palette.
///
/// For 3D: uses <see cref="SandDisintegrationEffect"/> on world objects
/// (player, tiles, decorations) in a staggered sequence.
/// </summary>
public class SandTransitionOrchestrator : MonoBehaviour
{
    // Sand colors matching SandDisintegrationEffect
    private static readonly Color SAND_DARK  = new Color(0.76f, 0.60f, 0.32f, 1f);
    private static readonly Color SAND_LIGHT = new Color(0.95f, 0.85f, 0.55f, 1f);

    private const int   MIN_GRAINS            = 15;
    private const int   MAX_GRAINS            = 80;
    private const float GRAIN_SIZE_MIN        = 2f;
    private const float GRAIN_SIZE_MAX        = 5f;
    private const float GRAIN_DRIFT_Y_MIN     = 40f;
    private const float GRAIN_DRIFT_Y_MAX     = 90f;
    private const float GRAIN_DRIFT_X_RANGE   = 25f;
    private const float GRAIN_LIFETIME_MIN    = 0.5f;
    private const float GRAIN_LIFETIME_MAX    = 1.0f;
    private const float ELEMENT_FADE_DURATION = 0.45f;
    private const float UI_STAGGER            = 0.12f;
    private const float OBJECT_SHRINK_DUR     = 0.5f;

    /// <summary>
    /// Runs the full dissolve sequence. Auto-detects UI vs 3D context.
    /// Checks for active UI panels first (TrialSelectScreen, MainMenu).
    /// If none found, falls through to 3D scene object dissolve.
    /// </summary>
    public IEnumerator RunDissolve()
    {
        // Check for active UI panels first
        RectTransform uiPanel = FindActiveUIPanel();
        if (uiPanel != null)
        {
            yield return DissolveUIPanel(uiPanel);
            yield break;
        }

        // No active UI panel → dissolve 3D scene objects
        yield return DissolveSceneObjects();
    }

    // ════════════════════════════════════════════════════════════════════
    //  UI DISSOLVE
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Dissolves all visible elements within a UI panel into sand grains.
    /// Elements dissolve sorted smallest-first so peripheral UI (hints,
    /// dividers) goes before cards, and titles go last.
    /// </summary>
    private IEnumerator DissolveUIPanel(RectTransform panel)
    {
        // Create a container for grains that renders above all elements
        GameObject containerGO = new GameObject("~SandGrains");
        containerGO.transform.SetParent(panel, false);
        containerGO.transform.SetAsLastSibling();
        RectTransform grainContainer = containerGO.AddComponent<RectTransform>();
        grainContainer.anchorMin = Vector2.zero;
        grainContainer.anchorMax = Vector2.one;
        grainContainer.offsetMin = Vector2.zero;
        grainContainer.offsetMax = Vector2.zero;

        // Collect dissolvable elements (leaves with visible graphics)
        List<RectTransform> elements = CollectUIElements(panel, grainContainer.transform);

        // Sort smallest area first → hints/dividers first, cards mid, title last
        elements.Sort((a, b) => GetWorldArea(a).CompareTo(GetWorldArea(b)));

        // Dissolve in sequence with stagger
        float lastDuration = 0f;
        foreach (RectTransform element in elements)
        {
            lastDuration = ELEMENT_FADE_DURATION + GRAIN_LIFETIME_MAX;
            StartCoroutine(DissolveUIElement(element, grainContainer));
            yield return new WaitForSecondsRealtime(UI_STAGGER);
        }

        // Wait for the last element's grains to finish
        yield return new WaitForSecondsRealtime(lastDuration);

        // Cleanup grain container
        Destroy(containerGO);
    }

    /// <summary>
    /// Dissolves a single UI element: fades it out while spawning amber
    /// sand grains that drift upward from its position.
    /// </summary>
    private IEnumerator DissolveUIElement(RectTransform element, RectTransform grainContainer)
    {
        if (element == null) yield break;

        // Get element world-space bounds for grain placement
        Vector3 center = element.position;
        float halfW = element.rect.width  * element.lossyScale.x * 0.5f;
        float halfH = element.rect.height * element.lossyScale.y * 0.5f;
        float canvasScale = Mathf.Max(element.lossyScale.x, 0.001f);

        // Scale grain count to element area (in canvas units)
        float areaInPixels = (halfW * 2f) * (halfH * 2f) / (canvasScale * canvasScale);
        int grainCount = Mathf.Clamp((int)(areaInPixels / 800f), MIN_GRAINS, MAX_GRAINS);

        // Ensure we can fade this element
        CanvasGroup cg = element.GetComponent<CanvasGroup>();
        bool addedCG = false;
        if (cg == null)
        {
            cg = element.gameObject.AddComponent<CanvasGroup>();
            addedCG = true;
        }

        // Spawn grains
        GrainData[] grains = new GrainData[grainCount];
        for (int i = 0; i < grainCount; i++)
        {
            GameObject go = new GameObject("~g");
            go.transform.SetParent(grainContainer, false);

            RectTransform rt = go.AddComponent<RectTransform>();
            Image img = go.AddComponent<Image>();
            img.raycastTarget = false;

            // Random position within element bounds
            float px = center.x + Random.Range(-halfW, halfW);
            float py = center.y + Random.Range(-halfH, halfH);
            rt.position = new Vector3(px, py, center.z);

            float size = Random.Range(GRAIN_SIZE_MIN, GRAIN_SIZE_MAX) * canvasScale;
            rt.sizeDelta = new Vector2(size, size);

            Color sandColor = Color.Lerp(SAND_DARK, SAND_LIGHT, Random.value);
            sandColor.a = 0f; // Start invisible, appear as element fades
            img.color = sandColor;

            grains[i] = new GrainData
            {
                rt       = rt,
                img      = img,
                color    = sandColor,
                delay    = Random.Range(0f, ELEMENT_FADE_DURATION * 0.6f),
                driftX   = Random.Range(-GRAIN_DRIFT_X_RANGE, GRAIN_DRIFT_X_RANGE) * canvasScale,
                driftY   = Random.Range(GRAIN_DRIFT_Y_MIN, GRAIN_DRIFT_Y_MAX) * canvasScale,
                lifetime = Random.Range(GRAIN_LIFETIME_MIN, GRAIN_LIFETIME_MAX)
            };
        }

        // Animate: element fades out, grains drift and fade
        float totalDuration = ELEMENT_FADE_DURATION + GRAIN_LIFETIME_MAX;
        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            float dt = Time.unscaledDeltaTime;
            elapsed += dt;

            // Fade element out
            float fadeT = Mathf.Clamp01(elapsed / ELEMENT_FADE_DURATION);
            cg.alpha = 1f - fadeT;

            // Update grains
            for (int i = 0; i < grains.Length; i++)
            {
                GrainData g = grains[i];
                if (g.rt == null) continue;
                if (elapsed < g.delay) continue;

                float grainElapsed = elapsed - g.delay;
                float grainT = Mathf.Clamp01(grainElapsed / g.lifetime);

                // Drift upward with slight horizontal sway
                Vector3 pos = g.rt.position;
                pos.x += g.driftX * dt;
                pos.y += g.driftY * dt;
                g.rt.position = pos;

                // Fade: appear quickly, then fade out
                float alpha = grainT < 0.15f
                    ? Mathf.Clamp01(grainT / 0.15f)
                    : 1f - Mathf.Clamp01((grainT - 0.15f) / 0.85f);

                Color c = g.color;
                c.a = alpha;
                g.img.color = c;

                // Deactivate once done
                if (grainT >= 1f && g.rt.gameObject.activeSelf)
                    g.rt.gameObject.SetActive(false);
            }

            yield return null;
        }

        // Cleanup
        for (int i = 0; i < grains.Length; i++)
        {
            if (grains[i].rt != null)
                Destroy(grains[i].rt.gameObject);
        }

        element.gameObject.SetActive(false);
        if (addedCG) Destroy(cg);
    }

    // ════════════════════════════════════════════════════════════════════
    //  3D SCENE DISSOLVE
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Dissolves 3D scene objects: player first, then tiles in a wave
    /// outward from the player, then decorations and remaining geometry.
    /// Covers all level layouts (Hub, Garden, Citadel, Clock).
    /// </summary>
    private IEnumerator DissolveSceneObjects()
    {
        Vector3 waveOrigin = Vector3.zero;

        // ── Player ──
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            waveOrigin = player.transform.position;

            // Disable FallDetection and PlayerMovement before shrinking so they
            // don't trigger death sounds or game-over sequences.
            FallDetection fallDetection = player.GetComponent<FallDetection>();
            if (fallDetection != null) fallDetection.enabled = false;
            PlayerMovement movement = player.GetComponent<PlayerMovement>();
            if (movement != null) movement.enabled = false;

            Vector3 size = GetObjectBoundsSize(player);
            SandDisintegrationEffect.Spawn(player.transform.position, size);
            StartCoroutine(ShrinkAndHide(player.transform, OBJECT_SHRINK_DUR));
            yield return new WaitForSecondsRealtime(0.3f);
        }

        // ── Tiles (wave outward from player) ──
        // Standard levels use "Tiles"; HubScene uses "HubTiles" under HubManager
        yield return DissolveGroupWave("Tiles", waveOrigin, 0.02f);
        yield return DissolveGroupWave("HubTiles", waveOrigin, 0.01f);
        yield return new WaitForSecondsRealtime(0.15f);

        // ── Moving tiles ──
        yield return DissolveGroupWave("MovingTiles", waveOrigin, 0.02f);
        yield return new WaitForSecondsRealtime(0.1f);

        // ── Boss tiles ──
        yield return DissolveGroupWave("BossTiles", waveOrigin, 0.02f);

        // ── Checkpoints ──
        yield return DissolveGroupWave("CheckPoint", waveOrigin, 0.02f);

        // ── Kill objects / Obstacles ──
        DissolveGroupImmediate("KillObjects");
        DissolveGroupImmediate("Obstacles");
        yield return new WaitForSecondsRealtime(0.1f);

        // ── Boss geometry ──
        DissolveGroupImmediate("Boss");
        yield return new WaitForSecondsRealtime(0.1f);

        // ── Decorations ──
        yield return DissolveGroupWave("Decorations", waveOrigin, 0.03f);
        yield return new WaitForSecondsRealtime(0.3f);

        // ── HUD elements ──
        RectTransform hud = FindUIElement("HUD");
        if (hud != null)
            yield return DissolveUIPanel(hud);

        // ── Remaining root objects (goal tiles, managers, etc.) ──
        DissolveRemainingVisible();

        yield return new WaitForSecondsRealtime(0.4f);
    }

    /// <summary>
    /// Dissolves children of a named root GameObject in a wave pattern,
    /// sorted by distance from the origin. Each child shrinks to zero
    /// with a small sand burst.
    /// </summary>
    private IEnumerator DissolveGroupWave(string rootName, Vector3 origin, float stagger)
    {
        GameObject root = GameObject.Find(rootName);
        if (root == null) yield break;

        // Collect active children, sorted by distance from origin
        List<Transform> children = new List<Transform>();
        for (int i = 0; i < root.transform.childCount; i++)
        {
            Transform child = root.transform.GetChild(i);
            if (child.gameObject.activeSelf)
                children.Add(child);
        }

        if (children.Count == 0) yield break;

        children.Sort((a, b) =>
            Vector3.SqrMagnitude(a.position - origin)
            .CompareTo(Vector3.SqrMagnitude(b.position - origin)));

        // Dissolve in wave — batch to avoid thousands of particle systems
        int batchSize = Mathf.Max(1, children.Count / 8);
        for (int i = 0; i < children.Count; i++)
        {
            Transform child = children[i];
            if (child == null || !child.gameObject.activeSelf) continue;

            // Only spawn sand particles for every Nth object to stay performant
            if (i % 3 == 0)
            {
                Vector3 size = child.localScale;
                Renderer rend = child.GetComponentInChildren<Renderer>();
                if (rend != null) size = rend.bounds.size;
                SandDisintegrationEffect.Spawn(child.position, size);
            }

            StartCoroutine(ShrinkAndHide(child, OBJECT_SHRINK_DUR * 0.6f));

            // Stagger: wait after each batch
            if ((i + 1) % batchSize == 0)
                yield return new WaitForSecondsRealtime(stagger * batchSize);
        }
    }

    /// <summary>
    /// Immediately hides all children of a named root (no animation).
    /// Used for minor geometry that doesn't need a fancy dissolve.
    /// </summary>
    private void DissolveGroupImmediate(string rootName)
    {
        GameObject root = GameObject.Find(rootName);
        if (root == null) return;
        root.SetActive(false);
    }

    /// <summary>
    /// Hides any remaining visible renderers (goal tiles, checkpoints, etc.)
    /// that weren't covered by the specific group dissolves.
    /// </summary>
    private void DissolveRemainingVisible()
    {
        string[] skipNames =
        {
            "GameCanvas", "Main Camera", "Directional Light",
            "Managers", "EventSystem", "SoundManager",
            "TimeScaleLogic", "DecorationsManager", "HubManager",
            "BossBManager", "BossCManager", "DeathTileTutorialTrigger",
            "BossBTrigger", "BossCTrigger"
        };

        foreach (GameObject rootGO in gameObject.scene.GetRootGameObjects())
        {
            if (!rootGO.activeSelf) continue;

            bool skip = false;
            foreach (string s in skipNames)
            {
                if (rootGO.name == s) { skip = true; break; }
            }
            if (skip) continue;

            // Hide if it has renderers (don't touch cameras, lights, etc.)
            Renderer[] renderers = rootGO.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
                rootGO.SetActive(false);
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  HELPERS
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Finds the currently active UI panel (TrialSelectScreen, MainMenu).
    /// Returns null if no recognizable UI panel is active (3D level context).
    /// Excludes PauseMenu since it's always closed before a transition starts.
    /// Also verifies the panel has at least one active visible child so we
    /// don't accidentally match an empty panel.
    /// </summary>
    private RectTransform FindActiveUIPanel()
    {
        string[] panelNames = { "TrialSelectScreen", "MainMenu" };
        Canvas[] canvases = FindObjectsOfType<Canvas>();

        foreach (Canvas canvas in canvases)
        {
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;

            foreach (string panelName in panelNames)
            {
                Transform panel = canvas.transform.Find(panelName);
                if (panel == null || !panel.gameObject.activeSelf) continue;

                // Verify the panel has at least one active Graphic child
                Graphic[] graphics = panel.GetComponentsInChildren<Graphic>(false);
                if (graphics.Length > 0)
                    return panel as RectTransform;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds a UI element by name under any overlay canvas.
    /// Returns null if not found or inactive.
    /// </summary>
    private RectTransform FindUIElement(string elementName)
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        foreach (Canvas canvas in canvases)
        {
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
            Transform element = canvas.transform.Find(elementName);
            if (element != null && element.gameObject.activeSelf)
                return element as RectTransform;
        }
        return null;
    }

    /// <summary>
    /// Collects all dissolvable UI elements under a panel.
    /// Recurses into layout groups (TrialGrid) to get individual cards.
    /// Skips the grain container and any inactive elements.
    /// </summary>
    private List<RectTransform> CollectUIElements(RectTransform panel, Transform grainContainer)
    {
        List<RectTransform> result = new List<RectTransform>();
        CollectUIElementsRecursive(panel, grainContainer, result, 0);
        return result;
    }

    private void CollectUIElementsRecursive(RectTransform parent, Transform grainContainer,
        List<RectTransform> result, int depth)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (!child.gameObject.activeSelf) continue;
            if (child == grainContainer) continue;

            RectTransform rt = child as RectTransform;
            if (rt == null) continue;

            // If this is a pure layout container (has LayoutGroup but no Graphic
            // of its own, like TrialGrid), recurse into it to dissolve individual
            // items. If it has BOTH a LayoutGroup and a Graphic (like BackButton
            // which gets a HorizontalLayoutGroup at runtime for icon+label), treat
            // it as a single dissolvable element instead.
            LayoutGroup layout = rt.GetComponent<LayoutGroup>();
            if (layout != null && rt.GetComponent<Graphic>() == null)
            {
                CollectUIElementsRecursive(rt, grainContainer, result, depth + 1);
                continue;
            }

            // Only dissolve elements that have visible graphics
            Graphic graphic = rt.GetComponent<Graphic>();
            bool hasVisibleChild = rt.GetComponentInChildren<Graphic>() != null;
            if (graphic != null || hasVisibleChild)
                result.Add(rt);
        }
    }

    /// <summary>Returns the world-space area of a RectTransform.</summary>
    private float GetWorldArea(RectTransform rt)
    {
        float w = rt.rect.width  * rt.lossyScale.x;
        float h = rt.rect.height * rt.lossyScale.y;
        return w * h;
    }

    /// <summary>Returns the bounds size of a 3D object.</summary>
    private Vector3 GetObjectBoundsSize(GameObject go)
    {
        Renderer rend = go.GetComponentInChildren<Renderer>();
        if (rend != null) return rend.bounds.size;
        return go.transform.localScale;
    }

    /// <summary>Smoothly shrinks a transform to zero and deactivates it.</summary>
    private IEnumerator ShrinkAndHide(Transform target, float duration)
    {
        if (target == null) yield break;

        Vector3 startScale = target.localScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (target == null) yield break;
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // Ease-in for accelerating shrink
            target.localScale = startScale * (1f - t * t);
            yield return null;
        }

        if (target != null)
            target.gameObject.SetActive(false);
    }

    // ════════════════════════════════════════════════════════════════════
    //  DATA
    // ════════════════════════════════════════════════════════════════════

    private struct GrainData
    {
        public RectTransform rt;
        public Image         img;
        public Color         color;
        public float         delay;
        public float         driftX;
        public float         driftY;
        public float         lifetime;
    }
}
