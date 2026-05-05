using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Tile tooltip — shows while the player stands on a colored tile. One popup shared across all tiles.
public class TutorialTilePopup : MonoBehaviour
{
    public enum TileType { Forward, Frozen, Reverse }

    [Header("Configuration")]
    [SerializeField] private TileType tileType = TileType.Forward;

    [Header("Display Settings")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    private const float DETECT_RANGE = 2.5f;
    private const float POPUP_WIDTH  = 520f;
    private const float POPUP_HEIGHT = 140f;
    private const float FORWARD_DISMISS_OFFSET = 1.5f;

    // Per-tile: tracks whether the player is currently on this tile
    private bool _playerAbove;
    private Transform _playerTransform;

    // Shared popup — only one visible at a time, content swaps per tile
    private static GameObject      _sharedPopupGO;
    private static CanvasGroup     _sharedPopupCG;
    private static TextMeshProUGUI _sharedTitle;
    private static TextMeshProUGUI _sharedDesc;
    private static Image           _sharedAccent;
    private static Coroutine       _sharedRoutine;
    private static MonoBehaviour   _sharedOwner;

    public static bool IsAnyVisible { get; private set; }

    private static readonly Color GOLD_COL   = new Color(0.961f, 0.784f, 0.259f, 1f);
    private static readonly Color BLUE_COL   = new Color(0.353f, 0.706f, 0.941f, 1f);
    private static readonly Color PURPLE_COL = new Color(0.608f, 0.365f, 0.898f, 1f);
    private static readonly Color BG_COL     = new Color(0.020f, 0.025f, 0.050f, 0.90f);
    private static readonly Color TEXT_COL   = new Color(0.910f, 0.918f, 0.965f, 0.95f);

    void Update()
    {
        // Block while full-screen modals are up
        if (HowToPlayController.IsAnyOpen || TimeScaleIntroModal.IsModalOpen)
        {
            if (_playerAbove)
            {
                _playerAbove = false;
                _playerTransform = null;
                RequestHide();
            }
            return;
        }

        if (!_playerAbove)
        {
            // Not showing yet — check if the player just stepped on this tile
            if (IsPlayerDetected())
            {
                _playerAbove = true;
                ShowPopup();
            }
        }
        else
        {
            // Already showing — only dismiss when the player walks forward past this tile
            if (HasPlayerMovedForward())
            {
                _playerAbove = false;
                _playerTransform = null;
                RequestHide();
            }
        }
    }

    void OnDestroy()
    {
        if (_sharedOwner == this)
        {
            _sharedPopupGO = null;
            _sharedPopupCG = null;
            _sharedTitle   = null;
            _sharedDesc    = null;
            _sharedAccent  = null;
            _sharedRoutine = null;
            _sharedOwner   = null;
            IsAnyVisible   = false;
        }
    }

    private bool IsPlayerDetected()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 0.15f;
        if (Physics.Raycast(rayOrigin, Vector3.up, out RaycastHit hit, DETECT_RANGE))
        {
            if (hit.collider.CompareTag("Player"))
            {
                _playerTransform = hit.collider.transform;
                return true;
            }
        }
        return false;
    }

    // Tooltip stays until the player walks forward (positive z) past this tile
    private bool HasPlayerMovedForward()
    {
        if (_playerTransform == null) return true;
        return _playerTransform.position.z > transform.position.z + FORWARD_DISMISS_OFFSET;
    }

    private void ShowPopup()
    {
        EnsureSharedPopup();
        if (_sharedPopupGO == null) return;

        // Overwrite content for this tile
        Color accent = GetAccentColor();
        if (_sharedAccent != null) _sharedAccent.color = accent;
        if (_sharedTitle != null) { _sharedTitle.text = GetTitle(); _sharedTitle.color = accent; }
        if (_sharedDesc != null) _sharedDesc.text = GetDescription();

        // Cancel whatever the previous owner was doing
        if (_sharedRoutine != null && _sharedOwner != null)
            _sharedOwner.StopCoroutine(_sharedRoutine);

        _sharedOwner = this;
        _sharedRoutine = StartCoroutine(FadeInRoutine());
    }

    // Only hides if this tile still owns the popup
    private void RequestHide()
    {
        if (_sharedOwner != this) return;

        if (_sharedRoutine != null)
            StopCoroutine(_sharedRoutine);

        _sharedRoutine = StartCoroutine(FadeOutRoutine());
    }

    private IEnumerator FadeInRoutine()
    {
        _sharedPopupGO.SetActive(true);
        IsAnyVisible = true;
        FadeOutAreaTitle();

        float elapsed = 0f;
        float startAlpha = _sharedPopupCG != null ? _sharedPopupCG.alpha : 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (_sharedPopupCG != null)
                _sharedPopupCG.alpha = Mathf.Lerp(startAlpha, 1f, Mathf.Clamp01(elapsed / fadeInDuration));
            yield return null;
        }
        if (_sharedPopupCG != null) _sharedPopupCG.alpha = 1f;
        _sharedRoutine = null;
    }

    private IEnumerator FadeOutRoutine()
    {
        float elapsed = 0f;
        float startAlpha = _sharedPopupCG != null ? _sharedPopupCG.alpha : 1f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (_sharedPopupCG != null)
                _sharedPopupCG.alpha = Mathf.Lerp(startAlpha, 0f, Mathf.Clamp01(elapsed / fadeOutDuration));
            yield return null;
        }

        if (_sharedPopupGO != null) _sharedPopupGO.SetActive(false);
        _sharedRoutine = null;
        IsAnyVisible = false;
    }

    // Fades out the AreaTitleIntro strip if it's showing.
    private static void FadeOutAreaTitle()
    {
        AreaTitleIntro areaTitleIntro = Object.FindObjectOfType<AreaTitleIntro>();
        if (areaTitleIntro != null)
        {
            areaTitleIntro.FadeOutNow();
        }
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
            case TileType.Forward: return ">>  TIME FORWARD";
            case TileType.Frozen:  return "||  TIME FROZEN";
            case TileType.Reverse: return "<<  TIME REVERSE";
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
