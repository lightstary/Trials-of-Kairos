using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Game Over screen — mirrors the WinScreen layout with a red/danger theme.
/// Builds its own UI at runtime under this RectTransform for consistent layout.
/// </summary>
public class GameOverScreenController : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private float titleShakeAmount   = 8f;
    [SerializeField] private float titleShakeDuration = 0.12f;
    [SerializeField] private float quoteDelay         = 0.6f;

    // ── Quotes ──────────────────────────────────────────────────────
    private static readonly string[] QUOTES = {
        "There was a version of you that succeeded.\nYou are not it.",
        "This timeline was permitted.\nIt is now erased.",
        "You reached an ending that should not exist.",
        "I allowed this outcome once.\nI will not allow it again.",
        "You did not run out of time.\nYou misused it.",
        "This path collapses with you.",
        "You mistake repetition for progress.",
        "Every attempt leaves less of you behind.",
        "I do not punish failure. I remove it.",
        "You were close. That is irrelevant.",
        "What you call effort, I call delay.",
        "You are not fighting me.\nYou are failing yourself.",
        "I have seen you succeed.\nThis is not that version.",
        "You insist on becoming\nthe outcome that ends here.",
        "I do not wait. I conclude.",
        "This was avoidable.\nYou proved it inevitable.",
        "You are arriving later each time.",
        "The future is narrowing around you.",
        "There are fewer versions of you left.",
        "You are being edited.",
        "This moment will not remember you."
    };

    private const string TITLE = "TEMPORAL  FAILURE";

    // ── Theme ───────────────────────────────────────────────────────
    private static readonly Color PANEL_BG     = new Color(0.059f, 0.102f, 0.188f, 0.97f);
    private static readonly Color TITLE_COL    = new Color(0.85f, 0.20f, 0.15f, 1f);
    private static readonly Color SUBTITLE_COL = new Color(0.91f, 0.918f, 0.965f, 0.7f);
    // Quote + attrib: golden amber (matching mockup)
    private static readonly Color QUOTE_COL    = new Color(0.92f, 0.75f, 0.35f, 0.9f);
    private static readonly Color ATTRIB_COL   = new Color(0.85f, 0.65f, 0.30f, 0.85f);
    // Buttons
    private static readonly Color RETRY_BG     = new Color(0.85f, 0.20f, 0.15f, 1f);
    private static readonly Color RETRY_HOVER  = new Color(1f, 0.35f, 0.25f, 1f);
    private static readonly Color RETRY_PRESS  = new Color(0.6f, 0.12f, 0.08f, 1f);
    private static readonly Color RETURN_BG    = new Color(0.08f, 0.12f, 0.22f, 1f);
    private static readonly Color BTN_TEXT     = new Color(0.91f, 0.918f, 0.965f, 1f);
    // Overlay
    private static readonly Color OVERLAY_COL  = new Color(0.7f, 0.05f, 0.05f, 0f);

    // ── Layout ──────────────────────────────────────────────────────
    private const float PANEL_W = 700f;
    private const float PANEL_H = 500f;
    private const float BTN_W   = 190f;
    private const float BTN_H   = 50f;
    private const float BTN_Y   = 50f;
    private const float BTN_GAP = 30f;

    // ── Runtime refs ────────────────────────────────────────────────
    private GameObject      _panel;
    private CanvasGroup     _panelCG;
    private RectTransform   _titleRT;
    private TextMeshProUGUI _titleTMP;
    private TextMeshProUGUI _subtitleTMP;
    private TextMeshProUGUI _quoteTMP;
    private TextMeshProUGUI _attribTMP;
    private Image           _overlay;
    private bool            _built;

    /// <summary>True while the game over screen is visible.</summary>
    public static bool IsOpen { get; private set; }

    /// <summary>Shows the game over screen.</summary>
    public void Show(string customSubtitle = null)
    {
        gameObject.SetActive(true);
        IsOpen = true;
        EnsureUI();
        HideGameplayUI();

        _panel.SetActive(true);
        StartCoroutine(Animate(customSubtitle));
    }

    // ================================================================
    //  UI CONSTRUCTION
    // ================================================================

    private void EnsureUI()
    {
        if (_built) return;
        _built = true;

        // Deactivate old scene-placed children
        foreach (Transform child in transform)
            child.gameObject.SetActive(false);

        // Ensure raycasts pass through to buttons
        CanvasGroup oldCG = GetComponent<CanvasGroup>();
        if (oldCG != null) { oldCG.alpha = 1f; oldCG.blocksRaycasts = true; oldCG.interactable = true; }

        Transform root = transform;

        // Full-screen red glow overlay
        GameObject overlayGO = MakeRect("LossOverlay", root,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        _overlay = overlayGO.AddComponent<Image>();
        _overlay.color = OVERLAY_COL;
        _overlay.raycastTarget = false;

        // Panel — 700x500 centered
        _panel = MakeRect("LossPanel", root,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);
        _panel.GetComponent<RectTransform>().sizeDelta = new Vector2(PANEL_W, PANEL_H);

        Image bg = _panel.AddComponent<Image>();
        bg.color = PANEL_BG;
        bg.raycastTarget = true;

        _panelCG = _panel.AddComponent<CanvasGroup>();
        _panelCG.alpha = 0f;

        // Title — single line, 42pt
        _titleTMP = MakeLabel("LossTitle", _panel.transform,
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(20f, -80f), new Vector2(-20f, -20f),
            42f, true, TITLE_COL, TextAlignmentOptions.Center);
        _titleTMP.characterSpacing = 6f;
        _titleTMP.enableWordWrapping = false;
        _titleTMP.overflowMode = TextOverflowModes.Overflow;
        _titleRT = _titleTMP.GetComponent<RectTransform>();

        // Subtitle — below title
        _subtitleTMP = MakeLabel("LossSubtitle", _panel.transform,
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(20f, -120f), new Vector2(-20f, -88f),
            20f, false, SUBTITLE_COL, TextAlignmentOptions.Center);
        _subtitleTMP.characterSpacing = 3f;

        // ── Quote block container ──
        // Raised up snug under the subtitle, centered in the space between
        // subtitle and buttons so there's no awkward gap.
        GameObject quoteBlock = MakeRect("QuoteBlock", _panel.transform,
            new Vector2(0.1f, 0.24f), new Vector2(0.9f, 0.72f),
            Vector2.zero, Vector2.zero);

        // Quote text — golden amber, bold italic, centered vertically
        _quoteTMP = MakeLabel("LossQuote", quoteBlock.transform,
            new Vector2(0f, 0.35f), new Vector2(1f, 1f),
            new Vector2(10f, 0f), new Vector2(-10f, 0f),
            19f, true, QUOTE_COL, TextAlignmentOptions.Center);
        _quoteTMP.fontStyle = FontStyles.Bold | FontStyles.Italic;

        // Attribution — golden, right-aligned, tight under quote
        _attribTMP = MakeLabel("LossAttrib", quoteBlock.transform,
            new Vector2(0f, 0.15f), new Vector2(1f, 0.35f),
            new Vector2(10f, 0f), new Vector2(-10f, 0f),
            18f, true, ATTRIB_COL, TextAlignmentOptions.TopRight);

        // Buttons
        float totalW = BTN_W * 2f + BTN_GAP;
        float retryX = -totalW * 0.5f + BTN_W * 0.5f;
        float returnX = retryX + BTN_W + BTN_GAP;

        Button retryBtn = MakeButton("RetryBtn", _panel.transform,
            retryX, BTN_Y, "RESTART LEVEL", RETRY_BG, BTN_TEXT,
            RETRY_HOVER, RETRY_PRESS);
        retryBtn.onClick.AddListener(RetryTrial);

        Button returnBtn = MakeButton("ReturnBtn", _panel.transform,
            returnX, BTN_Y, "TRIAL SELECTION", RETURN_BG, BTN_TEXT,
            RETRY_HOVER, RETRY_PRESS);
        returnBtn.onClick.AddListener(ReturnToHub);

        _panel.SetActive(false);
    }

    private Button MakeButton(string name, Transform parent, float x, float y,
        string label, Color bgColor, Color textColor,
        Color hoverColor, Color pressColor)
    {
        GameObject go = MakeRect(name, parent,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            Vector2.zero, Vector2.zero);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta        = new Vector2(BTN_W, BTN_H);
        rt.pivot            = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(x, y);

        Image img = go.AddComponent<Image>();
        img.color = bgColor;

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        ColorBlock cb = ColorBlock.defaultColorBlock;
        cb.normalColor      = Color.white;
        cb.highlightedColor = hoverColor;
        cb.selectedColor    = hoverColor;
        cb.pressedColor     = pressColor;
        cb.fadeDuration     = 0.05f;
        btn.colors = cb;

        TextMeshProUGUI tmp = MakeLabel("Label", go.transform,
            Vector2.zero, Vector2.one,
            new Vector2(4f, 2f), new Vector2(-4f, -2f),
            18f, true, textColor, TextAlignmentOptions.Center);
        tmp.text = label;

        return btn;
    }

    // ================================================================
    //  ANIMATION
    // ================================================================

    // ── Animation timing ───────────────────────────────────────────
    private const float PANEL_SCALE_FROM  = 0.90f;
    private const float PANEL_SLIDE_Y     = -30f;
    private const float PANEL_ENTRY_DUR   = 0.6f;
    private const float OVERLAY_ENTRY_DUR = 0.5f;

    private IEnumerator Animate(string subtitle)
    {
        _panelCG.alpha   = 0f;
        _quoteTMP.alpha  = 0f;
        _attribTMP.alpha = 0f;

        _titleTMP.text    = TITLE;
        _subtitleTMP.text = subtitle ?? "THE TIMELINE HAS COLLAPSED";

        string q = QUOTES[Random.Range(0, QUOTES.Length)];
        _quoteTMP.text  = $"\u201C{q}\u201D";
        _attribTMP.text = "\u2014CHRONOS";

        // Red fracture flash (layered under panel)
        if (ScreenTransitionManager.Instance != null)
            ScreenTransitionManager.Instance.RedFracture();

        StartCoroutine(AnimateOverlay());

        // Panel entry: scale from 0.90 + slide up from -30 + alpha fade — eased
        RectTransform panelRT = _panel.GetComponent<RectTransform>();
        Vector2 targetPos = panelRT.anchoredPosition;
        Vector2 startPos  = targetPos + new Vector2(0f, PANEL_SLIDE_Y);

        float elapsed = 0f;
        while (elapsed < PANEL_ENTRY_DUR)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / PANEL_ENTRY_DUR);
            float eased = EaseOutCubic(t);

            _panelCG.alpha = eased;
            panelRT.localScale = Vector3.one * Mathf.Lerp(PANEL_SCALE_FROM, 1f, eased);
            panelRT.anchoredPosition = Vector2.Lerp(startPos, targetPos, eased);
            yield return null;
        }
        _panelCG.alpha = 1f;
        panelRT.localScale = Vector3.one;
        panelRT.anchoredPosition = targetPos;

        // Title shake — subtle, sells the impact
        if (_titleRT != null)
        {
            Vector2 orig = _titleRT.anchoredPosition;
            elapsed = 0f;
            while (elapsed < titleShakeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float decay = 1f - (elapsed / titleShakeDuration);
                _titleRT.anchoredPosition = orig + new Vector2(
                    Random.Range(-titleShakeAmount, titleShakeAmount) * decay,
                    Random.Range(-titleShakeAmount * 0.5f, titleShakeAmount * 0.5f) * decay);
                yield return null;
            }
            _titleRT.anchoredPosition = orig;
        }

        // Staggered quote reveal
        yield return new WaitForSecondsRealtime(quoteDelay);

        elapsed = 0f;
        float quoteDur = 0.8f;
        while (elapsed < quoteDur)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseOutCubic(Mathf.Clamp01(elapsed / quoteDur));
            _quoteTMP.alpha  = t;
            _attribTMP.alpha = t * 0.85f; // Attribution trails slightly
            yield return null;
        }
        _quoteTMP.alpha  = 1f;
        _attribTMP.alpha = 1f;

        // Select retry button for controller navigation
        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            Button retryBtn = _panel.GetComponentInChildren<Button>();
            if (retryBtn != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(retryBtn.gameObject);
        }
    }

    private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

    /// <summary>Red overlay: smooth ease-in, then gentle breathing pulse.</summary>
    private IEnumerator AnimateOverlay()
    {
        if (_overlay == null) yield break;

        Color c = OVERLAY_COL;

        // Ease-in ramp — matches panel entry timing
        float elapsed = 0f;
        while (elapsed < OVERLAY_ENTRY_DUR)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseOutCubic(Mathf.Clamp01(elapsed / OVERLAY_ENTRY_DUR));
            c.a = t * 0.22f;
            _overlay.color = c;
            yield return null;
        }

        // Gentle breathing pulse
        while (_overlay != null && _overlay.gameObject.activeInHierarchy)
        {
            float pulse = Mathf.Sin(Time.unscaledTime * 2f) * 0.04f + 0.18f;
            c.a = pulse;
            _overlay.color = c;
            yield return null;
        }
    }

    // ================================================================
    //  HIDE GAMEPLAY UI
    // ================================================================

    /// <summary>Forcefully hides ALL gameplay UI so nothing bleeds through.</summary>
    private void HideGameplayUI()
    {
        HUDController hud = HUDController.Instance;
        if (hud != null)
        {
            hud.DismissCenterFlash();

            TextMeshProUGUI flashLabel = null;
            var field = typeof(HUDController).GetField("_centerFlashLabel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
                flashLabel = field.GetValue(hud) as TextMeshProUGUI;
            if (flashLabel != null)
                Destroy(flashLabel.gameObject);

            hud.StopAllCoroutines();
            hud.gameObject.SetActive(false);
        }

        BossFailUI failUI = FindObjectOfType<BossFailUI>(true);
        if (failUI != null) failUI.gameObject.SetActive(false);

        TimeScaleMeter meter = FindObjectOfType<TimeScaleMeter>(true);
        if (meter != null) meter.gameObject.SetActive(false);

        BossPopup popup = FindObjectOfType<BossPopup>(true);
        if (popup != null) popup.gameObject.SetActive(false);
    }

    // ================================================================
    //  BUTTONS
    // ================================================================

    /// <summary>Restarts the current level with a quick transition.</summary>
    private void RetryTrial()
    {
        IsOpen = false;
        MainMenuController.RequestRestartTrialOnLoad();
        string scene = SceneManager.GetActiveScene().name;
        StartCoroutine(ExitThenTransition(() =>
        {
            Time.timeScale = 1f;
            if (ScreenTransitionManager.Instance != null)
                ScreenTransitionManager.Instance.QuickReloadScene(scene);
            else
                SceneManager.LoadScene(scene);
        }));
    }

    /// <summary>Returns to trial selection with dissolve + shimmer transition.</summary>
    private void ReturnToHub()
    {
        IsOpen = false;
        MainMenuController.RequestTrialSelectOnLoad();
        StartCoroutine(ExitThenTransition(() =>
        {
            Time.timeScale = 1f;
            if (ScreenTransitionManager.Instance != null)
                ScreenTransitionManager.Instance.FadeToScene("MainScene");
            else
                SceneManager.LoadScene("MainScene");
        }));
    }

    private const float EXIT_PANEL_DUR = 0.25f;

    /// <summary>Shrinks + fades the panel, then runs a cosmic fade before executing the callback.</summary>
    private IEnumerator ExitThenTransition(System.Action callback)
    {
        foreach (Button btn in _panel.GetComponentsInChildren<Button>())
            btn.interactable = false;

        RectTransform panelRT = _panel.GetComponent<RectTransform>();
        Vector2 startPos = panelRT.anchoredPosition;

        float elapsed = 0f;
        while (elapsed < EXIT_PANEL_DUR)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / EXIT_PANEL_DUR);
            float eased = t * t * t;

            _panelCG.alpha = 1f - eased;
            panelRT.localScale = Vector3.one * Mathf.Lerp(1f, 0.92f, eased);
            panelRT.anchoredPosition = startPos + new Vector2(0f, -20f * eased);

            if (_overlay != null)
            {
                Color c = _overlay.color;
                c.a *= (1f - eased * 0.5f);
                _overlay.color = c;
            }

            yield return null;
        }

        // Hide the game over UI so the dissolve sees the level underneath
        if (_panel != null) _panel.SetActive(false);
        if (_overlay != null) _overlay.gameObject.SetActive(false);
        gameObject.SetActive(false);

        callback?.Invoke();
    }

    // ================================================================
    //  HELPERS
    // ================================================================

    private static GameObject MakeRect(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offMin, Vector2 offMax)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offMin;
        rt.offsetMax = offMax;
        return go;
    }

    private static TextMeshProUGUI MakeLabel(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offMin, Vector2 offMax,
        float fontSize, bool bold, Color color, TextAlignmentOptions align)
    {
        GameObject go = MakeRect(name, parent, anchorMin, anchorMax, offMin, offMax);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize          = fontSize;
        tmp.fontStyle         = bold ? FontStyles.Bold : FontStyles.Normal;
        tmp.color             = color;
        tmp.alignment         = align;
        tmp.raycastTarget     = false;
        tmp.enableWordWrapping = true;
        CinzelFontHelper.Apply(tmp, bold);
        return tmp;
    }
}
