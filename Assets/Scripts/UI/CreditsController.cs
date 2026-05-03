using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Displays the game credits with a staggered sand-grain entrance and
/// sand-dissolve exit. Builds the entire UI programmatically.
///
/// When opened from the main menu: destroys itself and returns to menu.
/// When opened after the ending cutscene: loads MainScene cleanly.
/// </summary>
public class CreditsController : MonoBehaviour
{
    /// <summary>
    /// When true, the credits were opened from the main menu (overlay mode).
    /// When false, the credits are a standalone screen after the ending cutscene.
    /// </summary>
    public bool IsOverlay { get; set; } = false;

    private CanvasGroup _canvasGroup;
    private Button _returnButton;
    private GameObject _canvasGO;
    private bool _exiting;

    private const float ENTRY_STAGGER = 0.08f;
    private const float ENTRY_FADE_DURATION = 0.4f;
    private const string MAIN_MENU_SCENE = "MainScene";

    // Sand grain constants (matching SandTransitionOrchestrator palette)
    private const int   GRAIN_COUNT_MIN       = 8;
    private const int   GRAIN_COUNT_MAX       = 40;
    private const float GRAIN_SIZE_MIN        = 2f;
    private const float GRAIN_SIZE_MAX        = 5f;
    private const float GRAIN_DRIFT_Y_MIN     = 30f;
    private const float GRAIN_DRIFT_Y_MAX     = 70f;
    private const float GRAIN_DRIFT_X_RANGE   = 20f;
    private const float GRAIN_LIFETIME_MIN    = 0.4f;
    private const float GRAIN_LIFETIME_MAX    = 0.8f;
    private const float EXIT_ELEMENT_FADE     = 0.35f;
    private const float EXIT_STAGGER          = 0.06f;

    private static readonly Color BG_COLOR = new Color(0.02f, 0.025f, 0.05f, 1f);
    private static readonly Color GOLD_COLOR = new Color(0.961f, 0.784f, 0.259f, 1f);
    private static readonly Color TEXT_COLOR = new Color(0.91f, 0.918f, 0.965f, 0.95f);
    private static readonly Color ROLE_COLOR = new Color(0.91f, 0.918f, 0.965f, 0.55f);
    private static readonly Color BTN_BG = new Color(0.059f, 0.102f, 0.188f, 0.85f);

    // Sand grain colors
    private static readonly Color SAND_DARK  = new Color(0.76f, 0.60f, 0.32f, 1f);
    private static readonly Color SAND_LIGHT = new Color(0.95f, 0.85f, 0.55f, 1f);

    private static readonly string[] CREDITS = new string[]
    {
        "ROLE:Programmer",
        "NAME:Alexandra Arizmendi Cortes",
        "",
        "ROLE:Asset Artist, Texture Artist",
        "NAME:Zutzuy Ayala-Zeferino",
        "",
        "ROLE:User Interface, Programmer",
        "NAME:Daniel Chaviano",
        "",
        "ROLE:Audio/SFX, Level Designer",
        "NAME:Fredrick Clay",
        "",
        "ROLE:Environment Design, Asset Integration, Animation",
        "NAME:Reagan Jewett",
        "",
        "ROLE:Production Lead, Level Designer, Marketing Videos",
        "NAME:Lily Miska",
        "",
        "ROLE:Audio Designer",
        "NAME:Samantha Perry",
        "",
        "ROLE:Character Designer, Animation",
        "NAME:Chanai Rhodes",
        "",
        "ROLE:Asset Artist, Marketing Materials",
        "NAME:Kayla Stromp",
    };

    // All fade-in elements for stagger entrance and dissolve exit
    private readonly List<CanvasGroup> _entryElements = new List<CanvasGroup>();

    void Start()
    {
        Time.timeScale = 0f;
        BuildUI();
        StartCoroutine(StaggeredEntrance());
    }

    private void BuildUI()
    {
        // Canvas
        _canvasGO = new GameObject("CreditsCanvas");
        _canvasGO.transform.SetParent(transform, false);

        Canvas canvas = _canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = _canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        _canvasGO.AddComponent<GraphicRaycaster>();
        _canvasGroup = _canvasGO.AddComponent<CanvasGroup>();
        _canvasGroup.alpha = 1f;

        // Background (always visible, no stagger)
        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(_canvasGO.transform, false);
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = BG_COLOR;
        bgImg.raycastTarget = true;
        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        // Title: "TRIALS OF KAIROS"
        CanvasGroup titleCG = MakeFadeLabel(_canvasGO.transform, "TRIALS OF KAIROS", 48f, GOLD_COLOR, true,
            new Vector2(0.1f, 0.85f), new Vector2(0.9f, 0.96f), 12f);
        _entryElements.Add(titleCG);

        // Subtitle: "CREDITS"
        CanvasGroup subtitleCG = MakeFadeLabel(_canvasGO.transform, "CREDITS", 24f, TEXT_COLOR, true,
            new Vector2(0.1f, 0.79f), new Vector2(0.9f, 0.85f), 8f);
        _entryElements.Add(subtitleCG);

        // Gold accent line
        GameObject accentGO = new GameObject("Accent");
        accentGO.transform.SetParent(_canvasGO.transform, false);
        Image accentImg = accentGO.AddComponent<Image>();
        accentImg.color = GOLD_COLOR;
        accentImg.raycastTarget = false;
        RectTransform accentRT = accentGO.GetComponent<RectTransform>();
        accentRT.anchorMin = new Vector2(0.35f, 0.785f);
        accentRT.anchorMax = new Vector2(0.65f, 0.785f);
        accentRT.offsetMin = Vector2.zero;
        accentRT.offsetMax = Vector2.zero;
        accentRT.sizeDelta = new Vector2(0f, 2f);
        CanvasGroup accentCG = accentGO.AddComponent<CanvasGroup>();
        accentCG.alpha = 0f;
        _entryElements.Add(accentCG);

        // Credits scroll area - bottom at 0.10 leaves room for the button area
        GameObject scrollAreaGO = new GameObject("ScrollArea");
        scrollAreaGO.transform.SetParent(_canvasGO.transform, false);
        RectTransform scrollRT = scrollAreaGO.AddComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0.15f, 0.10f);
        scrollRT.anchorMax = new Vector2(0.85f, 0.77f);
        scrollRT.offsetMin = Vector2.zero;
        scrollRT.offsetMax = Vector2.zero;

        // Scroll rect for overflow
        ScrollRect scrollRect = scrollAreaGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 30f;

        // Mask so content doesn't bleed outside scroll area
        Image scrollMaskImg = scrollAreaGO.AddComponent<Image>();
        scrollMaskImg.color = new Color(0f, 0f, 0f, 0.01f); // Nearly invisible
        scrollMaskImg.raycastTarget = true;
        Mask scrollMask = scrollAreaGO.AddComponent<Mask>();
        scrollMask.showMaskGraphic = false;

        // Content container for credits entries
        GameObject contentGO = new GameObject("Content");
        contentGO.transform.SetParent(scrollAreaGO.transform, false);
        RectTransform contentRT = contentGO.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = new Vector2(0f, 0f);

        scrollRect.content = contentRT;
        scrollRect.viewport = scrollRT;

        VerticalLayoutGroup vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 2f;
        vlg.padding = new RectOffset(20, 20, 10, 30);

        ContentSizeFitter csf = contentGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Populate credits - each entry gets its own CanvasGroup for staggered fade
        foreach (string entry in CREDITS)
        {
            if (string.IsNullOrEmpty(entry))
            {
                // Spacer
                GameObject spacer = new GameObject("Spacer");
                spacer.transform.SetParent(contentGO.transform, false);
                LayoutElement le = spacer.AddComponent<LayoutElement>();
                le.preferredHeight = 16f;
                continue;
            }

            if (entry.StartsWith("ROLE:"))
            {
                string role = entry.Substring(5);
                CanvasGroup cg = MakeLayoutFadeLabel(contentGO.transform, role, 14f, ROLE_COLOR, false, 4f);
                _entryElements.Add(cg);
            }
            else if (entry.StartsWith("NAME:"))
            {
                string personName = entry.Substring(5);
                CanvasGroup cg = MakeLayoutFadeLabel(contentGO.transform, personName, 20f, TEXT_COLOR, true, 2f);
                _entryElements.Add(cg);
            }
        }

        // Return to Main Menu button - fixed at bottom, clear of scroll content
        GameObject btnGO = new GameObject("ReturnButton");
        btnGO.transform.SetParent(_canvasGO.transform, false);
        RectTransform btnRT = btnGO.AddComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.5f, 0f);
        btnRT.anchorMax = new Vector2(0.5f, 0f);
        btnRT.pivot = new Vector2(0.5f, 0.5f);
        btnRT.sizeDelta = new Vector2(250f, 46f);
        btnRT.anchoredPosition = new Vector2(0f, 38f);

        Image btnImg = btnGO.AddComponent<Image>();
        btnImg.color = Color.white;
        btnImg.raycastTarget = true;

        _returnButton = btnGO.AddComponent<Button>();
        _returnButton.targetGraphic = btnImg;
        ColorBlock cb = _returnButton.colors;
        cb.normalColor = BTN_BG;
        cb.highlightedColor = GOLD_COLOR;
        cb.selectedColor = GOLD_COLOR;
        cb.pressedColor = new Color(0.7f, 0.55f, 0.1f, 1f);
        cb.fadeDuration = 0.05f;
        _returnButton.colors = cb;
        _returnButton.onClick.AddListener(OnReturnClicked);

        MakeLabel(btnGO.transform, "MAIN MENU", 16f, TEXT_COLOR, true,
            Vector2.zero, Vector2.one, 6f);

        CanvasGroup btnCG = btnGO.AddComponent<CanvasGroup>();
        btnCG.alpha = 0f;
        _entryElements.Add(btnCG);
    }

    // ════════════════════════════════════════════════════════════════════
    //  STAGGERED SAND ENTRANCE
    // ════════════════════════════════════════════════════════════════════

    /// <summary>Reveals each credit entry one by one with a fade + sand grain burst.</summary>
    private IEnumerator StaggeredEntrance()
    {
        // Small initial delay
        yield return new WaitForSecondsRealtime(0.3f);

        foreach (CanvasGroup cg in _entryElements)
        {
            if (cg == null) continue;
            StartCoroutine(FadeInElement(cg));
            SpawnEntryGrains(cg.GetComponent<RectTransform>());
            yield return new WaitForSecondsRealtime(ENTRY_STAGGER);
        }

        // Wait for last element to finish fading
        yield return new WaitForSecondsRealtime(ENTRY_FADE_DURATION);

        // Select the return button for controller navigation
        if (_returnButton != null && UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(_returnButton.gameObject);
    }

    /// <summary>Fades a single element from 0 to 1 over ENTRY_FADE_DURATION.</summary>
    private IEnumerator FadeInElement(CanvasGroup cg)
    {
        float elapsed = 0f;
        while (elapsed < ENTRY_FADE_DURATION)
        {
            elapsed += Time.unscaledDeltaTime;
            if (cg != null) cg.alpha = Mathf.Clamp01(elapsed / ENTRY_FADE_DURATION);
            yield return null;
        }
        if (cg != null) cg.alpha = 1f;
    }

    /// <summary>Spawns a small burst of sand grains from the element's position.</summary>
    private void SpawnEntryGrains(RectTransform element)
    {
        if (element == null || _canvasGO == null) return;

        int count = Random.Range(GRAIN_COUNT_MIN, GRAIN_COUNT_MAX / 2);
        for (int i = 0; i < count; i++)
        {
            StartCoroutine(AnimateGrain(element, _canvasGO.transform, true));
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  SAND DISSOLVE EXIT
    // ════════════════════════════════════════════════════════════════════

    /// <summary>Dissolves all credit entries into sand, then performs the exit action.</summary>
    private IEnumerator DissolveExit()
    {
        _exiting = true;

        // Block the button
        if (_returnButton != null) _returnButton.interactable = false;

        // Dissolve entries in reverse order (button first, names next, title last)
        for (int i = _entryElements.Count - 1; i >= 0; i--)
        {
            CanvasGroup cg = _entryElements[i];
            if (cg == null) continue;
            StartCoroutine(FadeOutElement(cg));
            SpawnExitGrains(cg.GetComponent<RectTransform>());
            yield return new WaitForSecondsRealtime(EXIT_STAGGER);
        }

        // Wait for grains to finish
        yield return new WaitForSecondsRealtime(EXIT_ELEMENT_FADE + GRAIN_LIFETIME_MAX + 0.1f);

        // Perform the actual navigation
        PerformExit();
    }

    /// <summary>Fades a single element from current alpha to 0.</summary>
    private IEnumerator FadeOutElement(CanvasGroup cg)
    {
        float startAlpha = cg != null ? cg.alpha : 1f;
        float elapsed = 0f;
        while (elapsed < EXIT_ELEMENT_FADE)
        {
            elapsed += Time.unscaledDeltaTime;
            if (cg != null)
                cg.alpha = Mathf.Lerp(startAlpha, 0f, Mathf.Clamp01(elapsed / EXIT_ELEMENT_FADE));
            yield return null;
        }
        if (cg != null) cg.alpha = 0f;
    }

    /// <summary>Spawns sand grains drifting upward from an element during exit dissolve.</summary>
    private void SpawnExitGrains(RectTransform element)
    {
        if (element == null || _canvasGO == null) return;

        int count = Random.Range(GRAIN_COUNT_MIN, GRAIN_COUNT_MAX);
        for (int i = 0; i < count; i++)
        {
            StartCoroutine(AnimateGrain(element, _canvasGO.transform, false));
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  SAND GRAIN ANIMATION
    // ════════════════════════════════════════════════════════════════════

    /// <summary>Creates and animates a single sand grain from a UI element.</summary>
    private IEnumerator AnimateGrain(RectTransform source, Transform canvasRoot, bool isEntry)
    {
        if (source == null) yield break;

        // Create grain
        GameObject grainGO = new GameObject("~grain");
        grainGO.transform.SetParent(canvasRoot, false);
        grainGO.transform.SetAsLastSibling();

        Image grainImg = grainGO.AddComponent<Image>();
        grainImg.color = Color.Lerp(SAND_DARK, SAND_LIGHT, Random.value);
        grainImg.raycastTarget = false;

        RectTransform grainRT = grainGO.GetComponent<RectTransform>();
        float size = Random.Range(GRAIN_SIZE_MIN, GRAIN_SIZE_MAX);
        grainRT.sizeDelta = new Vector2(size, size);

        // Start position: random point within the source element bounds
        Vector3 sourcePos = source.position;
        float halfW = source.rect.width * source.lossyScale.x * 0.5f;
        float halfH = source.rect.height * source.lossyScale.y * 0.5f;
        float startX = sourcePos.x + Random.Range(-halfW, halfW);
        float startY = sourcePos.y + Random.Range(-halfH, halfH);
        grainRT.position = new Vector3(startX, startY, 0f);

        // Drift direction
        float driftY = Random.Range(GRAIN_DRIFT_Y_MIN, GRAIN_DRIFT_Y_MAX) * (isEntry ? -1f : 1f);
        float driftX = Random.Range(-GRAIN_DRIFT_X_RANGE, GRAIN_DRIFT_X_RANGE);
        float lifetime = Random.Range(GRAIN_LIFETIME_MIN, GRAIN_LIFETIME_MAX);

        // Delay before appearing (spread out the burst)
        float delay = Random.Range(0f, 0.15f);
        Color baseColor = grainImg.color;
        grainImg.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
        float delayElapsed = 0f;
        while (delayElapsed < delay)
        {
            delayElapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // Animate
        Vector3 startPos = grainRT.position;
        float elapsed = 0f;
        while (elapsed < lifetime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / lifetime);

            grainRT.position = startPos + new Vector3(driftX * t, driftY * t, 0f);

            // Fade: quick in, slow out
            float alpha;
            if (t < 0.2f)
                alpha = t / 0.2f;
            else
                alpha = 1f - ((t - 0.2f) / 0.8f);
            grainImg.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha * 0.7f);

            yield return null;
        }

        Destroy(grainGO);
    }

    // ════════════════════════════════════════════════════════════════════
    //  NAVIGATION
    // ════════════════════════════════════════════════════════════════════

    /// <summary>Called when the return button is clicked.</summary>
    private void OnReturnClicked()
    {
        if (_exiting) return;
        StartCoroutine(DissolveExit());
    }

    /// <summary>Performs the actual exit after dissolve completes.</summary>
    private void PerformExit()
    {
        if (IsOverlay)
        {
            // Opened from main menu — just destroy this overlay, menu is still behind it
            Destroy(gameObject);
        }
        else
        {
            // Opened after ending cutscene from a trial scene — fade to main menu
            Time.timeScale = 1f;
            if (ScreenTransitionManager.Instance != null)
            {
                ScreenTransitionManager.Instance.FadeToScene(MAIN_MENU_SCENE);
            }
            else
            {
                SceneManager.LoadScene(MAIN_MENU_SCENE);
            }
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  UI HELPERS
    // ════════════════════════════════════════════════════════════════════

    /// <summary>Creates an anchored label with its own CanvasGroup (starts invisible).</summary>
    private CanvasGroup MakeFadeLabel(Transform parent, string text, float fontSize, Color color,
        bool bold, Vector2 anchorMin, Vector2 anchorMax, float charSpacing)
    {
        GameObject go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.characterSpacing = charSpacing;
        tmp.raycastTarget = false;
        CinzelFontHelper.Apply(tmp, bold);
        CanvasGroup cg = go.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        return cg;
    }

    /// <summary>Creates a layout label with CanvasGroup for staggered entrance (starts invisible).</summary>
    private CanvasGroup MakeLayoutFadeLabel(Transform parent, string text, float fontSize,
        Color color, bool bold, float charSpacing)
    {
        GameObject go = new GameObject("CreditEntry");
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.characterSpacing = charSpacing;
        tmp.raycastTarget = false;
        CinzelFontHelper.Apply(tmp, bold);

        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredHeight = fontSize + 8f;

        CanvasGroup cg = go.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        return cg;
    }

    /// <summary>Simple label helper (no CanvasGroup, always visible).</summary>
    private TextMeshProUGUI MakeLabel(Transform parent, string text, float fontSize, Color color,
        bool bold, Vector2 anchorMin, Vector2 anchorMax, float charSpacing)
    {
        GameObject go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.characterSpacing = charSpacing;
        tmp.raycastTarget = false;
        CinzelFontHelper.Apply(tmp, bold);
        return tmp;
    }
}
