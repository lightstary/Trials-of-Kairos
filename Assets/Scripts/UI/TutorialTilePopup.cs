using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Shows an in-game tutorial message when the player steps on a color-coded tile.
/// Only one popup is visible at a time — a new trigger dismisses the previous one cleanly.
/// </summary>
public class TutorialTilePopup : MonoBehaviour
{
    public enum TileType { Forward, Frozen, Reverse }

    [Header("Configuration")]
    [SerializeField] private TileType tileType = TileType.Forward;

    [Header("Display Settings")]
    [SerializeField] private float displayDuration = 5f;
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    private const float DETECT_RANGE = 2.5f;
    private const float POPUP_WIDTH  = 520f;
    private const float POPUP_HEIGHT = 140f;

    private bool _triggered;

    // ── Shared single-popup system ──────────────────────────────────────
    private static GameObject      _sharedPopupGO;
    private static CanvasGroup     _sharedPopupCG;
    private static TextMeshProUGUI _sharedTitle;
    private static TextMeshProUGUI _sharedDesc;
    private static Image           _sharedAccent;
    private static Coroutine       _sharedRoutine;
    private static MonoBehaviour   _sharedOwner;

    private static readonly Color GOLD_COL   = new Color(0.961f, 0.784f, 0.259f, 1f);
    private static readonly Color BLUE_COL   = new Color(0.353f, 0.706f, 0.941f, 1f);
    private static readonly Color PURPLE_COL = new Color(0.608f, 0.365f, 0.898f, 1f);
    private static readonly Color BG_COL     = new Color(0.020f, 0.025f, 0.050f, 0.90f);
    private static readonly Color TEXT_COL   = new Color(0.910f, 0.918f, 0.965f, 0.95f);

    void Update()
    {
        if (_triggered) return;
        CheckPlayerAbove();
    }

    void OnDestroy()
    {
        // Clear static references when the scene unloads to prevent stale pointers
        // on scene reload (e.g. Retry Hub). The next TutorialTilePopup will recreate them.
        if (_sharedOwner == this)
        {
            _sharedPopupGO = null;
            _sharedPopupCG = null;
            _sharedTitle   = null;
            _sharedDesc    = null;
            _sharedAccent  = null;
            _sharedRoutine = null;
            _sharedOwner   = null;
        }
    }

    /// <summary>Resets the trigger so the popup can fire again.</summary>
    public void ResetTrigger() { _triggered = false; }

    private void CheckPlayerAbove()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 0.15f;
        if (Physics.Raycast(rayOrigin, Vector3.up, out RaycastHit hit, DETECT_RANGE))
        {
            if (hit.collider.CompareTag("Player"))
            {
                _triggered = true;
                ShowPopup();
            }
        }
    }

    private void ShowPopup()
    {
        EnsureSharedPopup();
        if (_sharedPopupGO == null) return;

        Color accent = GetAccentColor();
        if (_sharedAccent != null) _sharedAccent.color = accent;
        if (_sharedTitle != null) { _sharedTitle.text = GetTitle(); _sharedTitle.color = accent; }
        if (_sharedDesc != null)  _sharedDesc.text = GetDescription();

        // Cancel any running routine and start fresh
        if (_sharedRoutine != null && _sharedOwner != null)
            _sharedOwner.StopCoroutine(_sharedRoutine);

        _sharedOwner = this;
        _sharedRoutine = StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        _sharedPopupGO.SetActive(true);

        float elapsed = 0f;
        float startAlpha = _sharedPopupCG != null ? _sharedPopupCG.alpha : 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            if (_sharedPopupCG != null) _sharedPopupCG.alpha = Mathf.Lerp(startAlpha, 1f, Mathf.Clamp01(elapsed / fadeInDuration));
            yield return null;
        }
        if (_sharedPopupCG != null) _sharedPopupCG.alpha = 1f;

        yield return new WaitForSeconds(displayDuration);

        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            if (_sharedPopupCG != null) _sharedPopupCG.alpha = 1f - Mathf.Clamp01(elapsed / fadeOutDuration);
            yield return null;
        }

        _sharedPopupGO.SetActive(false);
        _sharedRoutine = null;
    }

    private static void EnsureSharedPopup()
    {
        if (_sharedPopupGO != null) return;

        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null) return;

        _sharedPopupGO = new GameObject("TutorialPopup_Shared");
        _sharedPopupGO.transform.SetParent(canvas.transform, false);

        RectTransform rt = _sharedPopupGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(POPUP_WIDTH, POPUP_HEIGHT);

        Image bg = _sharedPopupGO.AddComponent<Image>();
        bg.color = BG_COL; bg.raycastTarget = false;

        _sharedPopupCG = _sharedPopupGO.AddComponent<CanvasGroup>();
        _sharedPopupCG.alpha = 0f; _sharedPopupCG.interactable = false; _sharedPopupCG.blocksRaycasts = false;

        // Accent bar
        GameObject accentGO = new GameObject("Accent");
        accentGO.transform.SetParent(_sharedPopupGO.transform, false);
        RectTransform abRT = accentGO.AddComponent<RectTransform>();
        abRT.anchorMin = Vector2.zero; abRT.anchorMax = new Vector2(0f, 1f);
        abRT.pivot = new Vector2(0f, 0.5f); abRT.sizeDelta = new Vector2(4f, 0f); abRT.anchoredPosition = Vector2.zero;
        _sharedAccent = accentGO.AddComponent<Image>(); _sharedAccent.raycastTarget = false;

        // Title
        _sharedTitle = MakeLabel("Title", _sharedPopupGO.transform,
            new Vector2(0f, 0.55f), new Vector2(1f, 1f), new Vector2(20f, 0f), new Vector2(-16f, -8f), 20f, true);
        _sharedTitle.characterSpacing = 6f;

        // Description
        _sharedDesc = MakeLabel("Desc", _sharedPopupGO.transform,
            new Vector2(0f, 0f), new Vector2(1f, 0.55f), new Vector2(20f, 10f), new Vector2(-16f, 0f), 15f, false);
        _sharedDesc.color = TEXT_COL;

        _sharedPopupGO.SetActive(false);
    }

    private static TextMeshProUGUI MakeLabel(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offMin, Vector2 offMax, float size, bool bold)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.offsetMin = offMin; rt.offsetMax = offMax;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = size; tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        tmp.alignment = TextAlignmentOptions.Left; tmp.raycastTarget = false;
        AssignFont(tmp);
        return tmp;
    }

    private Color GetAccentColor()
    {
        switch (tileType)
        {
            case TileType.Forward: return GOLD_COL;
            case TileType.Frozen:  return BLUE_COL;
            case TileType.Reverse: return PURPLE_COL;
            default: return GOLD_COL;
        }
    }

    private string GetTitle()
    {
        switch (tileType)
        {
            case TileType.Forward: return "\u25B6  TIME FORWARD";
            case TileType.Frozen:  return "\u25A0  TIME FROZEN";
            case TileType.Reverse: return "\u25C0  TIME REVERSE";
            default: return "TIME";
        }
    }

    private string GetDescription()
    {
        switch (tileType)
        {
            case TileType.Forward: return "Stand upright to move time forward. Objects advance through their timeline.";
            case TileType.Frozen:  return "Lay flat to freeze time. Everything holds perfectly still at its current moment.";
            case TileType.Reverse: return "Flip upside down to reverse time. Objects rewind through their timeline.";
            default: return "";
        }
    }

    private static void AssignFont(TextMeshProUGUI tmp)
    {
        CinzelFontHelper.Apply(tmp, tmp.fontStyle == FontStyles.Bold);
    }
}
