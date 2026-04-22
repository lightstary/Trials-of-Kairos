using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Goal tile that detects when the player reaches it.
/// In HubScene: shows a themed completion popup to proceed to the Citadel.
/// In other scenes: triggers standard level completion.
/// </summary>
public class GoalTile : MonoBehaviour
{
    public Color goalColor = new Color(1f, 0.8f, 0f);

    private bool _completed;
    private Renderer _renderer;

    private static readonly Color BG_COL       = new Color(0.020f, 0.025f, 0.050f, 0.92f);
    private static readonly Color OVERLAY_COL  = new Color(0f, 0f, 0f, 0.55f);
    private static readonly Color GOLD_COL     = new Color(0.961f, 0.784f, 0.259f, 1f);
    private static readonly Color TEXT_COL     = new Color(0.910f, 0.918f, 0.965f, 0.95f);
    private static readonly Color BTN_BG       = new Color(0.059f, 0.102f, 0.188f, 0.85f);
    private static readonly Color BTN_HOVER    = new Color(0.12f, 0.15f, 0.25f, 0.95f);
    private static readonly Color BTN_PRESSED  = new Color(0.18f, 0.22f, 0.35f, 1f);

    private const string CITADEL_COMPLETE_KEY = "Level_Citadel_Complete";

    void Start()
    {
        _renderer = GetComponent<Renderer>();
        if (_renderer != null)
            _renderer.material.color = goalColor;
    }

    void Update()
    {
        if (_completed) return;
        CheckPlayerAbove();
    }

    private void CheckPlayerAbove()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 0.2f;
        if (Physics.Raycast(rayOrigin, Vector3.up, out RaycastHit hit, 2f))
        {
            if (hit.collider.CompareTag("Player"))
            {
                _completed = true;
                OnLevelComplete();
            }
        }
    }

    private void OnLevelComplete()
    {
        bool isHub = SceneManager.GetActiveScene().name == "HubScene";

        if (isHub)
        {
            Time.timeScale = 0f;
            ShowHubCompletionPopup();
        }
        else
        {
            // Mark Citadel as complete for level progression
            PlayerPrefs.SetInt(CITADEL_COMPLETE_KEY, 1);
            PlayerPrefs.Save();
            Debug.Log("Level Complete! Citadel marked as finished.");
        }
    }

    /// <summary>Builds and shows a themed completion popup for the Hub tutorial.</summary>
    private void ShowHubCompletionPopup()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        // Dark overlay
        GameObject overlayGO = new GameObject("CompletionOverlay");
        overlayGO.transform.SetParent(canvas.transform, false);
        RectTransform ovRT = overlayGO.AddComponent<RectTransform>();
        ovRT.anchorMin = Vector2.zero; ovRT.anchorMax = Vector2.one;
        ovRT.offsetMin = Vector2.zero; ovRT.offsetMax = Vector2.zero;
        Image ovImg = overlayGO.AddComponent<Image>();
        ovImg.color = OVERLAY_COL; ovImg.raycastTarget = true;

        // Panel
        GameObject panelGO = new GameObject("CompletionPanel");
        panelGO.transform.SetParent(overlayGO.transform, false);
        RectTransform pRT = panelGO.AddComponent<RectTransform>();
        pRT.anchorMin = pRT.anchorMax = new Vector2(0.5f, 0.5f);
        pRT.sizeDelta = new Vector2(700f, 500f);
        Image pBg = panelGO.AddComponent<Image>();
        pBg.color = BG_COL; pBg.raycastTarget = true;
        CanvasGroup pCG = panelGO.AddComponent<CanvasGroup>();

        // Gold accent bar at top
        MakeAccent(panelGO.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -3f), 3f);

        // Title
        MakeText(panelGO.transform, "TRIAL COMPLETE", 36f, GOLD_COL, true,
            new Vector2(0.05f, 0.72f), new Vector2(0.95f, 0.93f), 8f);

        // Subtitle
        MakeText(panelGO.transform, "You have proven yourself worthy.\nThree trials now await — each more unforgiving than the last.", 18f, TEXT_COL, false,
            new Vector2(0.08f, 0.45f), new Vector2(0.92f, 0.72f), 0f);

        // ── TRIAL SELECTION button ──
        Button continueBtn = MakePopupButton(panelGO.transform, "TRIAL SELECTION", new Vector2(0f, 120f));
        continueBtn.onClick.AddListener(GoToTrialSelection);

        // ── RETRY HUB button ──
        Button retryBtn = MakePopupButton(panelGO.transform, "RETRY HUB", new Vector2(0f, 55f));
        retryBtn.onClick.AddListener(RetryHub);

        // Wire navigation between the two buttons
        Navigation contNav = new Navigation();
        contNav.mode = Navigation.Mode.Explicit;
        contNav.selectOnDown = retryBtn;
        contNav.selectOnUp = retryBtn;
        continueBtn.navigation = contNav;

        Navigation retryNav = new Navigation();
        retryNav.mode = Navigation.Mode.Explicit;
        retryNav.selectOnUp = continueBtn;
        retryNav.selectOnDown = continueBtn;
        retryBtn.navigation = retryNav;

        // Select Continue button for controller
        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(continueBtn.gameObject);

        // Fade in
        StartCoroutine(FadeInPopup(pCG));
    }

    private Button MakePopupButton(Transform parent, string label, Vector2 pos)
    {
        GameObject btnGO = new GameObject(label.Replace(" ", ""));
        btnGO.transform.SetParent(parent, false);
        RectTransform bRT = btnGO.AddComponent<RectTransform>();
        bRT.anchorMin = new Vector2(0.5f, 0f); bRT.anchorMax = new Vector2(0.5f, 0f);
        bRT.pivot = new Vector2(0.5f, 0f);
        bRT.sizeDelta = new Vector2(340f, 54f);
        bRT.anchoredPosition = pos;
        Image bImg = btnGO.AddComponent<Image>();
        bImg.color = Color.white; bImg.raycastTarget = true;

        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = bImg;
        ColorBlock cb = btn.colors;
        cb.normalColor = BTN_BG;
        cb.highlightedColor = BTN_HOVER;
        cb.selectedColor = BTN_HOVER;
        cb.pressedColor = BTN_PRESSED;
        cb.fadeDuration = 0.05f;
        btn.colors = cb;

        // Gold accent bar on left edge
        GameObject accentGO = new GameObject("Accent");
        accentGO.transform.SetParent(btnGO.transform, false);
        RectTransform accRT = accentGO.AddComponent<RectTransform>();
        accRT.anchorMin = Vector2.zero; accRT.anchorMax = new Vector2(0f, 1f);
        accRT.pivot = new Vector2(0f, 0.5f);
        accRT.sizeDelta = new Vector2(3f, 0f); accRT.anchoredPosition = Vector2.zero;
        Image accImg = accentGO.AddComponent<Image>();
        accImg.color = GOLD_COL; accImg.raycastTarget = false;

        // Gold text on dark background = readable contrast
        MakeText(btnGO.transform, label, 20f, GOLD_COL, true,
            Vector2.zero, Vector2.one, 5f);

        return btn;
    }

    private IEnumerator FadeInPopup(CanvasGroup cg)
    {
        cg.alpha = 0f;
        float elapsed = 0f;
        while (elapsed < 0.4f)
        {
            elapsed += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Clamp01(elapsed / 0.4f);
            yield return null;
        }
        cg.alpha = 1f;
    }

    private void GoToTrialSelection()
    {
        Time.timeScale = 1f;
        MainMenuController.RequestTrialSelectOnLoad();
        if (ScreenTransitionManager.Instance != null)
            ScreenTransitionManager.Instance.FadeToScene("MainScene");
        else
            SceneManager.LoadScene("MainScene");
    }

    private void RetryHub()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("HubScene");
    }

    private void MakeAccent(Transform parent, Vector2 aMin, Vector2 aMax, Vector2 pos, float h)
    {
        GameObject go = new GameObject("TopAccent");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, h); rt.anchoredPosition = pos;
        Image img = go.AddComponent<Image>();
        img.color = GOLD_COL; img.raycastTarget = false;
    }

    private TextMeshProUGUI MakeText(Transform parent, string text, float size, Color col, bool bold,
        Vector2 aMin, Vector2 aMax, float charSpacing)
    {
        GameObject go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.color = col;
        tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.characterSpacing = charSpacing; tmp.raycastTarget = false;
        foreach (TMP_FontAsset f in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
        {
            if (f.name.Contains("Cinzel")) { tmp.font = f; if (f.name.Contains("Bold")) break; }
        }
        return tmp;
    }
}