using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Modal popup shown when the player falls off the map outside of a boss fight.
/// Offers "Restart from Checkpoint" or "Restart Level" options.
/// Builds its own UI at runtime on the first Canvas it finds.
/// </summary>
public class FallModal : MonoBehaviour
{
    // ── Theme (matches GameOverScreen / WinScreen style) ────────────
    private static readonly Color PANEL_BG     = new Color(0.059f, 0.102f, 0.188f, 0.95f);
    private static readonly Color DIMMER_COL   = new Color(0f, 0f, 0f, 0.5f);
    private static readonly Color TITLE_COL    = new Color(0.85f, 0.20f, 0.15f, 1f);
    private static readonly Color DESC_COL     = new Color(0.82f, 0.84f, 0.90f, 0.85f);
    private static readonly Color BTN_PRIMARY  = new Color(0.85f, 0.20f, 0.15f, 1f);
    private static readonly Color BTN_SECONDARY = new Color(0.08f, 0.12f, 0.22f, 1f);
    private static readonly Color BTN_TEXT     = new Color(0.91f, 0.918f, 0.965f, 1f);
    private static readonly Color BTN_HOVER    = new Color(1f, 0.35f, 0.25f, 1f);
    private static readonly Color BTN_PRESS    = new Color(0.6f, 0.12f, 0.08f, 1f);
    private static readonly Color ACCENT_COL   = new Color(0.85f, 0.20f, 0.15f, 1f);

    private const float PANEL_W = 560f;
    private const float PANEL_H = 300f;
    private const float BTN_W   = 220f;
    private const float BTN_H   = 50f;
    private const float BTN_GAP = 24f;

    private static readonly string[] FALL_MESSAGES = {
        "You slipped between the seconds.",
        "The path crumbled beneath you.",
        "Time does not catch the fallen.",
        "You stepped beyond the timeline.",
        "The void between moments claimed you.",
        "You fell outside of time itself."
    };

    private const string TITLE_TEXT = "LOST IN TIME";

    private static GameObject _root;

    /// <summary>True while the fall modal is visible.</summary>
    public static bool IsOpen => _root != null;

    /// <summary>
    /// Shows the fall modal with options based on whether a checkpoint exists.
    /// </summary>
    /// <param name="hasCheckpoint">If true, shows both "Restart from Checkpoint" and "Restart Level". If false, only "Restart Level".</param>
    /// <param name="onCheckpoint">Called when the player chooses to restart from checkpoint.</param>
    /// <param name="onRestartLevel">Called when the player chooses to restart the level.</param>
    public static void Show(bool hasCheckpoint, Action onCheckpoint, Action onRestartLevel)
    {
        // Destroy any existing modal
        if (_root != null) Destroy(_root);

        Canvas canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[FallModal] No Canvas found. Falling back to checkpoint.");
            onCheckpoint?.Invoke();
            return;
        }

        // Root container (full-screen dimmer)
        _root = new GameObject("FallModal");
        _root.transform.SetParent(canvas.transform, false);
        RectTransform rootRT = _root.AddComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = Vector2.zero;
        rootRT.offsetMax = Vector2.zero;

        Image dimmer = _root.AddComponent<Image>();
        dimmer.color = DIMMER_COL;
        dimmer.raycastTarget = true; // Block clicks behind modal

        CanvasGroup cg = _root.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.interactable = true;
        cg.blocksRaycasts = true;

        // Panel
        GameObject panel = MakeRect("Panel", _root.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);
        panel.GetComponent<RectTransform>().sizeDelta = new Vector2(PANEL_W, PANEL_H);
        Image panelBg = panel.AddComponent<Image>();
        panelBg.color = PANEL_BG;
        panelBg.raycastTarget = true;

        // Accent bar (top)
        GameObject accent = MakeRect("Accent", panel.transform,
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -3f), Vector2.zero);
        accent.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 3f);
        Image acImg = accent.AddComponent<Image>();
        acImg.color = ACCENT_COL;
        acImg.raycastTarget = false;

        // Title
        TextMeshProUGUI titleTMP = MakeLabel("Title", panel.transform,
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(30f, -75f), new Vector2(-30f, -20f),
            36f, true, TITLE_COL, TextAlignmentOptions.Center);
        titleTMP.text = TITLE_TEXT;
        titleTMP.characterSpacing = 6f;
        titleTMP.enableWordWrapping = false;

        // Description
        string msg = FALL_MESSAGES[UnityEngine.Random.Range(0, FALL_MESSAGES.Length)];
        TextMeshProUGUI descTMP = MakeLabel("Desc", panel.transform,
            new Vector2(0f, 0.40f), new Vector2(1f, 0.65f),
            new Vector2(40f, 0f), new Vector2(-40f, 0f),
            18f, false, DESC_COL, TextAlignmentOptions.Center);
        descTMP.text = msg;
        descTMP.fontStyle = FontStyles.Italic;

        // Buttons
        if (hasCheckpoint)
        {
            float totalW = BTN_W * 2f + BTN_GAP;
            float leftX = -totalW * 0.5f + BTN_W * 0.5f;
            float rightX = leftX + BTN_W + BTN_GAP;
            float btnY = 40f;

            Button checkpointBtn = MakeButton("CheckpointBtn", panel.transform,
                leftX, btnY, "RESTART FROM CHECKPOINT", BTN_PRIMARY, BTN_TEXT);
            checkpointBtn.onClick.AddListener(() =>
            {
                Dismiss();
                FadeAndExecute(onCheckpoint);
            });

            Button restartBtn = MakeButton("RestartBtn", panel.transform,
                rightX, btnY, "RESTART LEVEL", BTN_SECONDARY, BTN_TEXT);
            restartBtn.onClick.AddListener(() =>
            {
                Dismiss();
                FadeAndExecute(onRestartLevel);
            });
        }
        else
        {
            float btnY = 40f;

            Button restartBtn = MakeButton("RestartBtn", panel.transform,
                0f, btnY, "RESTART LEVEL", BTN_PRIMARY, BTN_TEXT);
            restartBtn.onClick.AddListener(() =>
            {
                Dismiss();
                FadeAndExecute(onRestartLevel);
            });
        }

        // Fade in
        FallModal fm = _root.AddComponent<FallModal>();
        fm.StartCoroutine(fm.FadeIn(cg));
    }

    private IEnumerator FadeIn(CanvasGroup cg)
    {
        float elapsed = 0f;
        float dur = 0.35f;
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Clamp01(elapsed / dur);
            yield return null;
        }
        cg.alpha = 1f;
    }

    private static void Dismiss()
    {
        if (_root != null) Destroy(_root);
        _root = null;
    }

    /// <summary>Fades through a cosmic wash, then executes the callback.</summary>
    private static void FadeAndExecute(Action callback)
    {
        if (ScreenTransitionManager.Instance != null)
        {
            ScreenTransitionManager.Instance.CosmicFadeOut(0.5f, () =>
            {
                callback?.Invoke();
            });
        }
        else
        {
            callback?.Invoke();
        }
    }

    private static Button MakeButton(string name, Transform parent,
        float x, float y, string label, Color bgColor, Color textColor)
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
        cb.highlightedColor = BTN_HOVER;
        cb.selectedColor    = BTN_HOVER;
        cb.pressedColor     = BTN_PRESS;
        cb.fadeDuration     = 0.05f;
        btn.colors = cb;

        TextMeshProUGUI tmp = MakeLabel("Label", go.transform,
            Vector2.zero, Vector2.one,
            new Vector2(4f, 2f), new Vector2(-4f, -2f),
            17f, true, textColor, TextAlignmentOptions.Center);
        tmp.text = label;

        return btn;
    }

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
