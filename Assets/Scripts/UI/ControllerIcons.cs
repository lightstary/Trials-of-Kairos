using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Centralized lookup for input prompt sprites and labels.
/// Loads icon sprites from Assets/Daniel's Assets/UI Images/ by filename.
/// Provides mode-aware properties that return the correct icon/text
/// based on the current InputPromptManager mode.
/// </summary>
public static class ControllerIcons
{
    private static bool _loaded;

    // ── Cached sprites ───────────────────────────────────────────────────
    private static Sprite _ctrlA, _ctrlB, _ctrlL, _ctrlR, _ctrlPause;
    private static Sprite _kbW, _kbA, _kbS, _kbD, _kbEsc;
    private static Sprite _kbMouse, _kbMouseLeft;

    // ── Explicit accessors (always same sprite regardless of mode) ────────
    public static Sprite CtrlA         { get { EnsureLoaded(); return _ctrlA; } }
    public static Sprite CtrlB         { get { EnsureLoaded(); return _ctrlB; } }
    public static Sprite CtrlLeftStick { get { EnsureLoaded(); return _ctrlL; } }
    public static Sprite CtrlRightStick{ get { EnsureLoaded(); return _ctrlR; } }
    public static Sprite CtrlPause     { get { EnsureLoaded(); return _ctrlPause; } }
    public static Sprite KeyW          { get { EnsureLoaded(); return _kbW; } }
    public static Sprite KeyA          { get { EnsureLoaded(); return _kbA; } }
    public static Sprite KeyS          { get { EnsureLoaded(); return _kbS; } }
    public static Sprite KeyD          { get { EnsureLoaded(); return _kbD; } }
    public static Sprite KeyEsc        { get { EnsureLoaded(); return _kbEsc; } }
    public static Sprite MouseIcon     { get { EnsureLoaded(); return _kbMouse; } }
    public static Sprite MouseLeft     { get { EnsureLoaded(); return _kbMouseLeft; } }

    // ── Mode-aware accessors ─────────────────────────────────────────────

    /// <summary>Confirm icon: Mouse Left for KB/M, A button for controller.</summary>
    public static Sprite ConfirmIcon => InputPromptManager.IsKeyboardMouse ? MouseLeft : CtrlA;

    /// <summary>Back icon: ESC for KB/M, B button for controller.</summary>
    public static Sprite BackIcon => InputPromptManager.IsKeyboardMouse ? KeyEsc : CtrlB;

    /// <summary>Move icon: W key for KB/M, Left Stick for controller.</summary>
    public static Sprite MoveIcon => InputPromptManager.IsKeyboardMouse ? KeyW : CtrlLeftStick;

    /// <summary>Look icon: Mouse for KB/M, Right Stick for controller.</summary>
    public static Sprite LookIcon => InputPromptManager.IsKeyboardMouse ? MouseIcon : CtrlRightStick;

    /// <summary>Pause icon: ESC for KB/M, Pause Xbox for controller.</summary>
    public static Sprite PauseIcon => InputPromptManager.IsKeyboardMouse ? KeyEsc : CtrlPause;

    // ── Mode-aware labels ────────────────────────────────────────────────

    public static string ConfirmLabel => InputPromptManager.IsKeyboardMouse ? "LEFT CLICK" : "A";
    public static string BackLabel    => InputPromptManager.IsKeyboardMouse ? "ESC" : "B";
    public static string PauseLabel   => InputPromptManager.IsKeyboardMouse ? "ESC" : "PAUSE";

    // ── Mode-aware badge text (for ControlsScreen) ───────────────────────
    public static string MoveBadge    => InputPromptManager.IsKeyboardMouse ? "WASD" : "LS";
    public static string LookBadge    => InputPromptManager.IsKeyboardMouse ? "MOUSE" : "RS";
    public static string ConfirmBadge => InputPromptManager.IsKeyboardMouse ? "CLICK" : "A";
    public static string CancelBadge  => InputPromptManager.IsKeyboardMouse ? "ESC"   : "B";
    public static string PauseBadge   => InputPromptManager.IsKeyboardMouse ? "ESC"   : "PAUSE";

    // ── Mode-aware descriptions ──────────────────────────────────────────
    public static string MoveDesc    => InputPromptManager.IsKeyboardMouse
        ? "W A S D \u2014 roll in all directions"
        : "Left stick \u2014 roll in all directions";
    public static string LookDesc    => InputPromptManager.IsKeyboardMouse
        ? "Mouse \u2014 rotate camera"
        : "Right stick \u2014 rotate camera";
    public static string ConfirmDesc => InputPromptManager.IsKeyboardMouse
        ? "Left click \u2014 confirm selection"
        : "A button \u2014 confirm selection";
    public static string CancelDesc  => InputPromptManager.IsKeyboardMouse
        ? "Escape \u2014 cancel or go back"
        : "B button \u2014 cancel or go back";
    public static string PauseDesc   => InputPromptManager.IsKeyboardMouse
        ? "Escape \u2014 open pause screen"
        : "Menu button \u2014 open pause screen";

    // ── Sprite loading ───────────────────────────────────────────────────

    private static void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;

        _ctrlA     = FindSprite("A Xbox");
        _ctrlB     = FindSprite("B Xbox");
        _ctrlL     = FindSprite("L Xbox");
        _ctrlR     = FindSprite("R Xbox");
        _ctrlPause = FindSprite("Pause Xbox");

        _kbW          = FindSprite("W Keyboard");
        _kbA          = FindSprite("A Keyboard");
        _kbS          = FindSprite("S Keyboard");
        _kbD          = FindSprite("D Keyboard");
        _kbEsc        = FindSprite("ESC Keyboard");
        _kbMouse      = FindSprite("Mouse");
        _kbMouseLeft  = FindSprite("Mouse Left");

        LogMissing("A Xbox", _ctrlA);
        LogMissing("B Xbox", _ctrlB);
        LogMissing("Mouse Left", _kbMouseLeft);
        LogMissing("ESC Keyboard", _kbEsc);
    }

    private static Sprite FindSprite(string fileName)
    {
#if UNITY_EDITOR
        string filter = fileName + " t:Sprite";
        string[] guids = UnityEditor.AssetDatabase.FindAssets(filter, new[] { "Assets/Daniel's Assets/UI Images" });
        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            string assetName = System.IO.Path.GetFileNameWithoutExtension(path);
            if (assetName == fileName)
            {
                Sprite spr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (spr != null) return spr;
            }
        }
#endif
        // Runtime fallback: search loaded sprites
        foreach (Sprite spr in Resources.FindObjectsOfTypeAll<Sprite>())
        {
            if (spr.name == fileName) return spr;
        }
        return null;
    }

    private static void LogMissing(string name, Sprite spr)
    {
        if (spr == null)
            Debug.LogWarning($"[ControllerIcons] Missing sprite: {name}.png");
    }

    // ── UI helper: create icon Image ─────────────────────────────────────

    /// <summary>Creates an Image child with the given sprite and size.</summary>
    public static Image CreateIcon(Transform parent, Sprite sprite, float size)
    {
        if (sprite == null) return null;
        GameObject go = new GameObject("Icon");
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(size, size);
        return img;
    }

    /// <summary>Creates a horizontal icon + label pair.</summary>
    public static GameObject CreateIconLabel(Transform parent, Sprite icon, string label,
        float iconSize = 28f, float fontSize = 14f, Color? labelColor = null, float spacing = 6f)
    {
        Color lc = labelColor ?? Color.white;

        GameObject container = new GameObject("IconLabel");
        container.transform.SetParent(parent, false);
        container.AddComponent<RectTransform>();

        HorizontalLayoutGroup hlg = container.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = spacing;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        ContentSizeFitter csf = container.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Icon
        if (icon != null)
        {
            Image iconImg = CreateIcon(container.transform, icon, iconSize);
            if (iconImg != null)
            {
                LayoutElement ile = iconImg.gameObject.AddComponent<LayoutElement>();
                ile.preferredWidth = iconSize;
                ile.preferredHeight = iconSize;
            }
        }

        // Label
        GameObject lblGO = new GameObject("Label");
        lblGO.transform.SetParent(container.transform, false);
        TextMeshProUGUI tmp = lblGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = fontSize;
        tmp.color = lc;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.raycastTarget = false;
        CinzelFontHelper.Apply(tmp);
        LayoutElement lle = lblGO.AddComponent<LayoutElement>();
        lle.preferredWidth = tmp.preferredWidth + 8f;
        lle.preferredHeight = iconSize;

        return container;
    }
}
