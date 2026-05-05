using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// Boss-fight fail screen: checkpoint, restart, or trial selection.
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
        pRT.sizeDelta = new Vector2(420f, 340f);
        Image pBg = panelGO.AddComponent<Image>();
        pBg.color = BG_COL; pBg.raycastTarget = true;
        CanvasGroup pCG = panelGO.AddComponent<CanvasGroup>();

        // Red accent bar at top
        MakeAccent(panelGO.transform);

        // Title
        MakeText(panelGO.transform, "TEMPORAL FAILURE", 22f, FAIL_RED, true,
            new Vector2(0.05f, 0.78f), new Vector2(0.95f, 0.93f), 6f);

        // Subtitle
        MakeText(panelGO.transform, "The timeline has collapsed.\nThe temporal balance was lost.", 13f, TEXT_COL, false,
            new Vector2(0.08f, 0.58f), new Vector2(0.92f, 0.78f), 0f);

        // RESPAWN AT CHECKPOINT button
        Button checkpointBtn = MakeButton(panelGO.transform, "RESPAWN AT CHECKPOINT", new Vector2(0f, 130f));
        checkpointBtn.onClick.AddListener(RespawnAtCheckpoint);

        // RESTART LEVEL button
        Button restartBtn = MakeButton(panelGO.transform, "RESTART LEVEL", new Vector2(0f, 75f));
        restartBtn.onClick.AddListener(RestartLevel);

        // TRIAL SELECTION button
        Button trialBtn = MakeButton(panelGO.transform, "TRIAL SELECTION", new Vector2(0f, 20f));
        trialBtn.onClick.AddListener(GoToTrialSelection);

        // Wire navigation for controller support
        Navigation checkpointNav = new Navigation { mode = Navigation.Mode.Explicit };
        checkpointNav.selectOnDown = restartBtn;
        checkpointNav.selectOnUp = trialBtn;
        checkpointBtn.navigation = checkpointNav;

        Navigation restartNav = new Navigation { mode = Navigation.Mode.Explicit };
        restartNav.selectOnUp = checkpointBtn;
        restartNav.selectOnDown = trialBtn;
        restartBtn.navigation = restartNav;

        Navigation trialNav = new Navigation { mode = Navigation.Mode.Explicit };
        trialNav.selectOnUp = restartBtn;
        trialNav.selectOnDown = checkpointBtn;
        trialBtn.navigation = trialNav;

        // Select checkpoint button for controller
        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(checkpointBtn.gameObject);

        StartCoroutine(FadeIn(pCG));
    }

    /// <summary>Respawns the player at the most recent checkpoint and resets the boss fight.</summary>
    private void RespawnAtCheckpoint()
    {
        Close();
        Time.timeScale = 1f;

        if (TimeScaleLogic.Instance != null)
            TimeScaleLogic.Instance.ResetMeter();

        FallDetection fd = FindObjectOfType<FallDetection>();
        if (fd != null)
        {
            // Force-clear fall state so checkpoint respawn works
            // (isFalling stays true after boss death, blocking Respawn())
            fd.ForceResetFallState();

            if (ScreenTransitionManager.Instance != null)
                ScreenTransitionManager.Instance.CosmicFadeOut(0.5f, () =>
                {
                    fd.DoCheckpointRespawnPublic();
                    RestartActiveBossFight();
                    if (ScreenTransitionManager.Instance != null)
                        ScreenTransitionManager.Instance.CosmicFadeIn(0.5f);
                });
            else
            {
                fd.DoCheckpointRespawnPublic();
                RestartActiveBossFight();
            }
        }
    }

    /// <summary>
    /// Restarts whichever boss fight exists in the current scene and plays boss music.
    /// Called after checkpoint respawn because OnTriggerEnter won't fire when the player
    /// is teleported directly into the trigger volume.
    /// </summary>
    private static void RestartActiveBossFight()
    {
        if (BossFight.Instance != null)
        {
            BossFight.Instance.StopBossFight();
            BossFight.Instance.StartBossFight();
            return;
        }
        if (BossBFight.Instance != null)
        {
            BossBFight.Instance.StopBossFight();
            BossBFight.Instance.StartBossFight();
            return;
        }
        if (BossCFight.Instance != null)
        {
            BossCFight.Instance.StopBossFight();
            BossCFight.Instance.StartBossFight();
            return;
        }

        // No boss found — at least ensure boss music plays if SoundManager has it
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayBossMusic();
    }

    /// <summary>Reloads the entire scene from the beginning.</summary>
    private void RestartLevel()
    {
        Close();
        Time.timeScale = 1f;

        MainMenuController.RequestRestartTrialOnLoad();

        if (ScreenTransitionManager.Instance != null)
            ScreenTransitionManager.Instance.QuickReloadScene(SceneManager.GetActiveScene().name);
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>Returns to the trial selection screen.</summary>
    private void GoToTrialSelection()
    {
        Close();
        Time.timeScale = 1f;

        MainMenuController.RequestTrialSelectOnLoad();

        if (ScreenTransitionManager.Instance != null)
            ScreenTransitionManager.Instance.FadeToScene("MainScene");
        else
            SceneManager.LoadScene("MainScene");
    }

    /// <summary>Cleans up the overlay and resets state.</summary>
    private void Close()
    {
        _shown = false;
        IsOpen = false;
        if (_overlayGO != null) Destroy(_overlayGO);
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
        CinzelFontHelper.Apply(tmp, bold);
    }
}
