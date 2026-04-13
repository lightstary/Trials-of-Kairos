using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// A multi-page modal that briefly explains the Time Scale mechanic.
/// Triggers once when the player walks near its position in the Hub.
/// Built entirely at runtime in the existing UI style.
/// Uses proximity detection (like TutorialTilePopup) since the player has no Rigidbody.
/// </summary>
public class TimeScaleIntroModal : MonoBehaviour
{
    private static bool _hasShown = false;

    private const float DETECT_RANGE = 2.0f;

    private static readonly Color PANEL_BG    = new Color(0.04f, 0.06f, 0.12f, 0.95f);
    private static readonly Color ACCENT_GOLD = new Color(0.961f, 0.784f, 0.259f, 1f);
    private static readonly Color TEXT_WHITE   = new Color(0.91f, 0.918f, 0.965f, 1f);
    private static readonly Color BTN_BG      = new Color(0.08f, 0.12f, 0.22f, 1f);

    private static readonly string[] PAGES = new string[]
    {
        "<color=#F5C842><size=24>TIME SCALE</size></color>\n\n" +
        "You control a <b>global time value</b>.\n\n" +
        "It starts at <b>zero</b> when each level begins.\n\n" +
        "<b>Stand upright</b> to push time forward.\n" +
        "<b>Flip upside down</b> to pull it back.\n" +
        "<b>Lay flat</b> to freeze it exactly where it is.",

        "<color=#F5C842><size=24>OBJECT LIMITS</size></color>\n\n" +
        "Each object has its own <b>time range</b>.\n\n" +
        "A platform might only move between <b>-2</b> and <b>+2</b>,\n" +
        "even if you push global time well past that.\n\n" +
        "When an object hits its limit, it <b>stops</b> — \n" +
        "but everything else keeps going.",

        "<color=#F5C842><size=24>USE THIS</size></color>\n\n" +
        "This is the core of every puzzle.\n\n" +
        "Different objects reach their limits at\n" +
        "<b>different times</b>. Use that to your advantage.\n\n" +
        "Freeze to lock objects in place.\n" +
        "Reverse to undo. Experiment freely."
    };

    private GameObject _modalGO;
    private TextMeshProUGUI _bodyTMP;
    private TextMeshProUGUI _pageIndicator;
    private TextMeshProUGUI _btnLabelTMP;
    private int _currentPage;
    private bool _isOpen;

    void Update()
    {
        if (_hasShown) return;
        if (_isOpen) return;

        // Proximity check — same approach as TutorialTilePopup
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.transform.position);
        if (dist < DETECT_RANGE)
        {
            _hasShown = true;
            Show();
        }
    }

    /// <summary>Shows the modal and pauses gameplay.</summary>
    private void Show()
    {
        _isOpen = true;
        _currentPage = 0;
        Time.timeScale = 0f;
        BuildUI();
        UpdatePage();
    }

    private void BuildUI()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        // Full-screen overlay
        _modalGO = new GameObject("TimeScaleIntroModal");
        _modalGO.transform.SetParent(canvas.transform, false);
        RectTransform overlayRT = _modalGO.AddComponent<RectTransform>();
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.sizeDelta = Vector2.zero;
        Image overlayImg = _modalGO.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.6f);
        overlayImg.raycastTarget = true;

        // Center panel
        GameObject panel = MakeRect("Panel", _modalGO.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        RectTransform panelRT = panel.GetComponent<RectTransform>();
        panelRT.sizeDelta = new Vector2(480f, 340f);
        Image panelImg = panel.AddComponent<Image>();
        panelImg.color = PANEL_BG;

        // Body text
        GameObject bodyGO = MakeRect("Body", panel.transform, new Vector2(0f, 1f), new Vector2(1f, 1f));
        RectTransform bodyRT = bodyGO.GetComponent<RectTransform>();
        bodyRT.pivot = new Vector2(0.5f, 1f);
        bodyRT.sizeDelta = new Vector2(-40f, 240f);
        bodyRT.anchoredPosition = new Vector2(0f, -20f);
        _bodyTMP = bodyGO.AddComponent<TextMeshProUGUI>();
        _bodyTMP.fontSize = 16f;
        _bodyTMP.color = TEXT_WHITE;
        _bodyTMP.alignment = TextAlignmentOptions.TopLeft;
        _bodyTMP.richText = true;
        _bodyTMP.raycastTarget = false;
        AssignFont(_bodyTMP);

        // Page indicator (e.g., "1 / 3")
        GameObject indicatorGO = MakeRect("PageIndicator", panel.transform, new Vector2(0f, 0f), new Vector2(0.5f, 0f));
        RectTransform indRT = indicatorGO.GetComponent<RectTransform>();
        indRT.pivot = new Vector2(0f, 0f);
        indRT.sizeDelta = new Vector2(0f, 36f);
        indRT.anchoredPosition = new Vector2(20f, 16f);
        _pageIndicator = indicatorGO.AddComponent<TextMeshProUGUI>();
        _pageIndicator.fontSize = 14f;
        _pageIndicator.color = new Color(TEXT_WHITE.r, TEXT_WHITE.g, TEXT_WHITE.b, 0.4f);
        _pageIndicator.alignment = TextAlignmentOptions.BottomLeft;
        _pageIndicator.raycastTarget = false;
        AssignFont(_pageIndicator);

        // "Continue" / "Got it" button
        GameObject btnGO = MakeRect("ContinueBtn", panel.transform, new Vector2(1f, 0f), new Vector2(1f, 0f));
        RectTransform btnRT = btnGO.GetComponent<RectTransform>();
        btnRT.pivot = new Vector2(1f, 0f);
        btnRT.sizeDelta = new Vector2(140f, 40f);
        btnRT.anchoredPosition = new Vector2(-20f, 16f);
        Image btnImg = btnGO.AddComponent<Image>();
        btnImg.color = BTN_BG;
        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = ACCENT_GOLD;
        cb.selectedColor = ACCENT_GOLD;
        cb.pressedColor = new Color(0.7f, 0.5f, 0.1f, 1f);
        cb.fadeDuration = 0.05f;
        btn.colors = cb;

        GameObject btnLabelGO = MakeRect("Label", btnGO.transform, Vector2.zero, Vector2.one);
        _btnLabelTMP = btnLabelGO.AddComponent<TextMeshProUGUI>();
        _btnLabelTMP.fontSize = 15f;
        _btnLabelTMP.color = TEXT_WHITE;
        _btnLabelTMP.alignment = TextAlignmentOptions.Center;
        _btnLabelTMP.raycastTarget = false;
        AssignFont(_btnLabelTMP);

        btn.onClick.AddListener(NextPage);

        // Also allow keyboard/gamepad advance (A / Space) since time is paused
        // and Update still runs — handled in a dedicated input check
        StartCoroutine(InputListenRoutine());
    }

    /// <summary>
    /// Listens for A button / Space while the modal is open.
    /// Uses unscaledDeltaTime since Time.timeScale is 0.
    /// </summary>
    private IEnumerator InputListenRoutine()
    {
        // Skip one frame so the button press that walked onto the trigger doesn't immediately advance
        yield return null;

        while (_isOpen)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.JoystickButton0)
                || Input.GetKeyDown(KeyCode.Return))
            {
                NextPage();
            }
            yield return null;
        }
    }

    private void UpdatePage()
    {
        if (_bodyTMP == null || _pageIndicator == null) return;

        _bodyTMP.text = PAGES[_currentPage];
        _pageIndicator.text = $"{_currentPage + 1} / {PAGES.Length}";

        if (_btnLabelTMP != null)
            _btnLabelTMP.text = _currentPage < PAGES.Length - 1 ? "CONTINUE  \u25B6" : "GOT IT  \u2714";
    }

    private void NextPage()
    {
        _currentPage++;
        if (_currentPage >= PAGES.Length)
        {
            Dismiss();
            return;
        }
        UpdatePage();
    }

    private void Dismiss()
    {
        _isOpen = false;
        Time.timeScale = 1f;
        if (_modalGO != null)
            Destroy(_modalGO);
    }

    private static GameObject MakeRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        return go;
    }

    private static void AssignFont(TextMeshProUGUI tmp)
    {
        foreach (TMP_FontAsset f in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
        {
            if (f.name.Contains("Cinzel"))
            {
                tmp.font = f;
                if (f.name.Contains("Bold") || f.name.Contains("SemiBold")) break;
            }
        }
    }
}
