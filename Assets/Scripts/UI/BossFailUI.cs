using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Proper boss-fight fail screen with RETRY and RETURN TO MAIN MENU options.
/// Replaces the temporary BossPopup 3D text with a real UI overlay.
/// Supports both D-pad and free cursor interaction.
/// </summary>
public class BossFailUI : MonoBehaviour
{
    private static readonly Color OVERLAY_COL = new Color(0f, 0f, 0f, 0.70f);
    private static readonly Color BG_COL      = new Color(0.020f, 0.025f, 0.050f, 0.92f);
    private static readonly Color FAIL_RED    = new Color(0.898f, 0.196f, 0.106f, 1f);
    private static readonly Color TEXT_COL    = new Color(0.910f, 0.918f, 0.965f, 0.95f);
    private static readonly Color BTN_BG      = new Color(0.059f, 0.102f, 0.188f, 0.85f);
    private static readonly Color BTN_HOVER   = new Color(0.12f, 0.15f, 0.25f, 0.95f);
    private static readonly Color BTN_PRESSED = new Color(0.18f, 0.22f, 0.35f, 1f);

    private GameObject _overlayGO;
    private bool _shown;

    /// <summary>True when the boss fail screen is currently displayed.</summary>
    public static bool IsOpen { get; private set; }

    /// <summary>Shows the boss fail screen.</summary>
    public void ShowFail()
    {
        if (_shown) return;
        _shown = true;
        IsOpen = true;
        Time.timeScale = 0f;
        BuildUI();
    }

    private void BuildUI()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        // Dark overlay
        _overlayGO = new GameObject("BossFailOverlay");
        _overlayGO.transform.SetParent(canvas.transform, false);
        _overlayGO.transform.SetAsLastSibling();
        RectTransform ovRT = _overlayGO.AddComponent<RectTransform>();
        ovRT.anchorMin = Vector2.zero; ovRT.anchorMax = Vector2.one;
        ovRT.offsetMin = Vector2.zero; ovRT.offsetMax = Vector2.zero;
        Image ovImg = _overlayGO.AddComponent<Image>();
        ovImg.color = OVERLAY_COL; ovImg.raycastTarget = true;

        // Panel
        GameObject panelGO = new GameObject("FailPanel");
        panelGO.transform.SetParent(_overlayGO.transform, false);
        RectTransform pRT = panelGO.AddComponent<RectTransform>();
        pRT.anchorMin = pRT.anchorMax = new Vector2(0.5f, 0.5f);
        pRT.sizeDelta = new Vector2(420f, 280f);
        Image pBg = panelGO.AddComponent<Image>();
        pBg.color = BG_COL; pBg.raycastTarget = true;
        CanvasGroup pCG = panelGO.AddComponent<CanvasGroup>();

        // Red accent bar at top
        MakeAccent(panelGO.transform);

        // Title
        MakeText(panelGO.transform, "TEMPORAL FAILURE", 22f, FAIL_RED, true,
            new Vector2(0.05f, 0.72f), new Vector2(0.95f, 0.93f), 6f);

        // Subtitle
        MakeText(panelGO.transform, "The timeline has collapsed.\nThe temporal balance was lost.", 13f, TEXT_COL, false,
            new Vector2(0.08f, 0.45f), new Vector2(0.92f, 0.72f), 0f);

        // RETRY button
        Button retryBtn = MakeButton(panelGO.transform, "RETRY", new Vector2(0f, 85f));
        retryBtn.onClick.AddListener(Retry);

        // RETURN TO MAIN MENU button
        Button menuBtn = MakeButton(panelGO.transform, "RETURN TO MAIN MENU", new Vector2(0f, 30f));
        menuBtn.onClick.AddListener(ReturnToMainMenu);

        // Wire navigation
        Navigation retryNav = new Navigation();
        retryNav.mode = Navigation.Mode.Explicit;
        retryNav.selectOnDown = menuBtn;
        retryNav.selectOnUp = menuBtn;
        retryBtn.navigation = retryNav;

        Navigation menuNav = new Navigation();
        menuNav.mode = Navigation.Mode.Explicit;
        menuNav.selectOnUp = retryBtn;
        menuNav.selectOnDown = retryBtn;
        menuBtn.navigation = menuNav;

        // Select Retry for controller
        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(retryBtn.gameObject);

        // Fade in
        StartCoroutine(FadeIn(pCG));
    }

    private void Retry()
    {
        Time.timeScale = 1f;
        _shown = false;
        IsOpen = false;

        // Destroy the overlay UI
        if (_overlayGO != null)
            Destroy(_overlayGO);

        // Reset the time scale meter so the fail doesn't re-trigger
        if (TimeScaleLogic.Instance != null)
            TimeScaleLogic.Instance.ResetMeter();

        // Respawn at the latest checkpoint instead of reloading the entire scene
        FallDetection fd = FindObjectOfType<FallDetection>();
        if (fd != null)
            fd.Respawn();
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        _shown = false;
        IsOpen = false;
        SceneManager.LoadScene("MainScene");
    }

    private IEnumerator FadeIn(CanvasGroup cg)
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

    private void MakeAccent(Transform parent)
    {
        GameObject go = new GameObject("TopAccent");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, 3f); rt.anchoredPosition = new Vector2(0f, -3f);
        Image img = go.AddComponent<Image>();
        img.color = FAIL_RED; img.raycastTarget = false;
    }

    private Button MakeButton(Transform parent, string label, Vector2 pos)
    {
        GameObject btnGO = new GameObject(label.Replace(" ", ""));
        btnGO.transform.SetParent(parent, false);
        RectTransform bRT = btnGO.AddComponent<RectTransform>();
        bRT.anchorMin = new Vector2(0.5f, 0f); bRT.anchorMax = new Vector2(0.5f, 0f);
        bRT.pivot = new Vector2(0.5f, 0f);
        bRT.sizeDelta = new Vector2(300f, 46f);
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

        // Red accent bar on left edge
        GameObject accentGO = new GameObject("Accent");
        accentGO.transform.SetParent(btnGO.transform, false);
        RectTransform accRT = accentGO.AddComponent<RectTransform>();
        accRT.anchorMin = Vector2.zero; accRT.anchorMax = new Vector2(0f, 1f);
        accRT.pivot = new Vector2(0f, 0.5f);
        accRT.sizeDelta = new Vector2(3f, 0f); accRT.anchoredPosition = Vector2.zero;
        Image accImg = accentGO.AddComponent<Image>();
        accImg.color = FAIL_RED; accImg.raycastTarget = false;

        // Label
        MakeText(btnGO.transform, label, 14f, FAIL_RED, true,
            Vector2.zero, Vector2.one, 4f);

        return btn;
    }

    private void MakeText(Transform parent, string text, float size, Color col, bool bold,
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
    }
}
