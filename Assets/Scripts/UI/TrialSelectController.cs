using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the trial selection screen with controller-first navigation:
///   - LB/RB (bumpers) or Left/Right arrows to browse trial cards
///   - D-pad horizontal also navigates with repeat delay
///   - A / Space to confirm the currently browsed node
///   - Two-phase selection: browse -> A confirm -> Enter Trial activates
///   - LB/RB hint pills flanking the card row, gold-flash on press
///   - Sand/dust particle background via MenuParticlesController
///   - MainMenuAtmosphere for rotating squares + grain
///
/// Attach to the TrialSelectScreen GameObject. Integrates with the
/// existing hierarchy (TrialGrid, BackButton) and MainMenuController.
/// </summary>
public class TrialSelectController : MonoBehaviour
{
    [Header("References (auto-discovered if null)")]
    [SerializeField] private Transform trialGrid;
    [SerializeField] private Button    backButton;

    // ── State ────────────────────────────────────────────────────────────────
    private Button[]    _cards;
    private Image[]     _cardBgs;
    private Color[]     _cardOrigColors;
    private int         _selectedIndex;
    private bool        _confirmed;
    private bool        _confirmBlocked;
    private float       _navCooldown;
    private Image       _lbHintBg;
    private Image       _rbHintBg;
    private bool        _hintsBuilt;

    // Enter Trial button (created in code)
    private Button      _enterButton;
    private CanvasGroup _enterGroup;
    private GameObject  _enterGO;

    private const float DPAD_REPEAT_DELAY = 0.25f;

    // ── Colors ────────────────────────────────────────────────────────────────
    private static readonly Color SelectedGold = new Color(1f, 0.82f, 0.10f, 1f);
    private static readonly Color HintResting  = new Color(0.08f, 0.08f, 0.16f, 0.72f);
    private static readonly Color HintPressed  = new Color(0.95f, 0.78f, 0.10f, 0.95f);
    private static readonly Color EnterBtnBg   = new Color(0.059f, 0.102f, 0.188f, 0.85f);
    private static readonly Color EnterBtnGold = new Color(0.961f, 0.784f, 0.259f, 1f);
    private static readonly Color LabelWhite   = new Color(0.91f, 0.918f, 0.965f, 1f);

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void OnEnable()
    {
        GatherCards();
        EnsureEnterButton();
        UpdateBackButtonLabel();

        _confirmed      = false;
        _confirmBlocked = false;
        SetEnterButtonState(false);

        if (_cards != null && _cards.Length > 0)
            SelectCard(0);

        if (!_hintsBuilt)
            StartCoroutine(BuildHintsDeferred());
    }

    void Update()
    {
        if (_cards == null || _cards.Length == 0) return;

        // ── Navigation: LB/RB, D-pad, left stick, arrow keys ──────────────
        bool navLeft  = Input.GetKeyDown(KeyCode.LeftArrow)
                     || Input.GetKeyDown(KeyCode.JoystickButton4);   // LB
        bool navRight = Input.GetKeyDown(KeyCode.RightArrow)
                     || Input.GetKeyDown(KeyCode.JoystickButton5);   // RB

        if (navLeft)  { NavigateCard(-1); FlashHint(_lbHintBg); }
        if (navRight) { NavigateCard( 1); FlashHint(_rbHintBg); }

        // Left stick / D-pad with repeat delay
        float axisH = Input.GetAxisRaw("Horizontal");
        float dpadH = Input.GetAxisRaw("DPadHorizontal");
        float combinedH = Mathf.Abs(axisH) > Mathf.Abs(dpadH) ? axisH : dpadH;

        if (_navCooldown <= 0f)
        {
            if (combinedH < -0.4f) { NavigateCard(-1); FlashHint(_lbHintBg); _navCooldown = DPAD_REPEAT_DELAY; }
            if (combinedH >  0.4f) { NavigateCard( 1); FlashHint(_rbHintBg); _navCooldown = DPAD_REPEAT_DELAY; }
        }
        else
        {
            _navCooldown -= Time.unscaledDeltaTime;
            if (Mathf.Abs(combinedH) < 0.2f) _navCooldown = 0f;
        }

        // ── A button / Space = confirm selection ──────────────────────────
        if (!_confirmBlocked &&
            (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.JoystickButton0)))
        {
            ConfirmSelection();
        }

        // ── B button / Escape = go back ───────────────────────────────────
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton1))
        {
            MainMenuController mmc = FindObjectOfType<MainMenuController>();
            if (mmc != null) mmc.CloseTrialSelect();
        }
    }

    // ── Card navigation ───────────────────────────────────────────────────────

    /// <summary>Gathers all Button children of trialGrid.</summary>
    private void GatherCards()
    {
        if (trialGrid == null)
            trialGrid = transform.Find("TrialGrid");
        if (trialGrid == null) return;

        int count = trialGrid.childCount;
        _cards          = new Button[count];
        _cardBgs        = new Image[count];
        _cardOrigColors = new Color[count];

        for (int i = 0; i < count; i++)
        {
            Transform child = trialGrid.GetChild(i);
            _cards[i]   = child.GetComponent<Button>();
            _cardBgs[i] = child.GetComponent<Image>();
            _cardOrigColors[i] = _cardBgs[i] != null ? _cardBgs[i].color : Color.clear;

            // Remove direct onClick so cards don't bypass two-phase confirm
            if (_cards[i] != null)
                _cards[i].onClick.RemoveAllListeners();
        }
    }

    /// <summary>Selects a card visually — highlights it gold, dims others.</summary>
    private void SelectCard(int index)
    {
        if (_cards == null || _cards.Length == 0) return;
        _selectedIndex = Mathf.Clamp(index, 0, _cards.Length - 1);

        for (int i = 0; i < _cards.Length; i++)
        {
            if (_cardBgs[i] == null) continue;

            if (i == _selectedIndex)
            {
                Color gold = _cardOrigColors[i];
                gold = Color.Lerp(gold, SelectedGold, 0.25f);
                _cardBgs[i].color = gold;

                RectTransform rt = _cardBgs[i].GetComponent<RectTransform>();
                if (rt != null) StartCoroutine(AnimateScale(rt, 1.08f, 0.15f));
            }
            else
            {
                Color dim = _cardOrigColors[i];
                dim.a = 0.5f;
                _cardBgs[i].color = dim;

                RectTransform rt = _cardBgs[i].GetComponent<RectTransform>();
                if (rt != null) StartCoroutine(AnimateScale(rt, 1.0f, 0.15f));
            }
        }
    }

    /// <summary>Navigates to next/prev card, resetting confirmation.</summary>
    private void NavigateCard(int delta)
    {
        if (_cards == null || _cards.Length == 0) return;

        _confirmed = false;
        SetEnterButtonState(false);

        int next = (_selectedIndex + delta + _cards.Length) % _cards.Length;
        SelectCard(next);
    }

    /// <summary>
    /// Confirms the currently browsed card. Activates Enter Trial button.
    /// Blocks for two frames so the same A press doesn't also fire Enter Trial.
    /// </summary>
    private void ConfirmSelection()
    {
        if (_confirmed || _confirmBlocked) return;
        _confirmed = true;
        SetEnterButtonState(true);
        StartCoroutine(BlockConfirmFrames());

        // Visual pulse on selected card
        if (_selectedIndex >= 0 && _selectedIndex < _cards.Length && _cardBgs[_selectedIndex] != null)
        {
            RectTransform rt = _cardBgs[_selectedIndex].GetComponent<RectTransform>();
            if (rt != null) StartCoroutine(ConfirmPulse(rt));
        }
    }

    private IEnumerator BlockConfirmFrames()
    {
        _confirmBlocked = true;
        yield return null;
        yield return null;
        _confirmBlocked = false;
    }

    private IEnumerator ConfirmPulse(RectTransform rt)
    {
        yield return AnimateScale(rt, 1.25f, 0.10f);
        yield return AnimateScale(rt, 1.08f, 0.15f);
    }

    // ── Enter Trial button ────────────────────────────────────────────────────

    /// <summary>Creates an Enter Trial button below the trial grid.</summary>
    private void EnsureEnterButton()
    {
        if (_enterGO != null) return;

        _enterGO = new GameObject("EnterTrialButton");
        _enterGO.transform.SetParent(transform, false);

        RectTransform rt = _enterGO.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0f);
        rt.anchorMax        = new Vector2(0.5f, 0f);
        rt.pivot            = new Vector2(0.5f, 0f);
        rt.sizeDelta        = new Vector2(220f, 50f);
        rt.anchoredPosition = new Vector2(0f, 120f);

        Image bg       = _enterGO.AddComponent<Image>();
        bg.color       = Color.white;
        bg.raycastTarget = true;

        _enterButton = _enterGO.AddComponent<Button>();
        _enterButton.targetGraphic = bg;
        ColorBlock cb       = _enterButton.colors;
        cb.normalColor      = EnterBtnBg;
        cb.highlightedColor = EnterBtnGold;
        cb.pressedColor     = new Color(0.7f, 0.55f, 0.1f, 1f);
        cb.selectedColor    = EnterBtnBg;
        cb.fadeDuration     = 0.1f;
        _enterButton.colors = cb;
        _enterButton.onClick.AddListener(EnterTrial);

        _enterGroup = _enterGO.AddComponent<CanvasGroup>();

        // Label
        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(_enterGO.transform, false);
        TextMeshProUGUI label = labelGO.AddComponent<TextMeshProUGUI>();
        label.text             = "ENTER TRIAL";
        label.fontSize         = 16f;
        label.characterSpacing = 10f;
        label.alignment        = TextAlignmentOptions.Center;
        label.color            = LabelWhite;
        label.raycastTarget    = false;

        RectTransform lrt = labelGO.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;

        SetEnterButtonState(false);
    }

    /// <summary>Dims or activates the Enter Trial button.</summary>
    private void SetEnterButtonState(bool active)
    {
        if (_enterGroup == null) return;
        _enterGroup.alpha          = active ? 1f : 0.30f;
        _enterGroup.interactable   = active;
        _enterGroup.blocksRaycasts = active;
    }

    /// <summary>Calls MainMenuController.BeginTrial to start gameplay.</summary>
    private void EnterTrial()
    {
        MainMenuController mmc = FindObjectOfType<MainMenuController>();
        if (mmc != null) mmc.BeginTrial();
    }

    /// <summary>Updates the existing back button label to [ B ] BACK.</summary>
    private void UpdateBackButtonLabel()
    {
        if (backButton == null)
        {
            Transform bb = transform.Find("BackButton");
            if (bb != null) backButton = bb.GetComponent<Button>();
        }
        if (backButton == null) return;

        TextMeshProUGUI lbl = backButton.GetComponentInChildren<TextMeshProUGUI>();
        if (lbl != null) lbl.text = "[ B ]  BACK";
    }

    // ── LB / RB hint pills ───────────────────────────────────────────────────

    private IEnumerator BuildHintsDeferred()
    {
        yield return null; // wait for layout

        if (_cards == null || _cards.Length == 0) yield break;

        DestroyChild("LBHint");
        DestroyChild("RBHint");

        RectTransform firstRT = _cardBgs[0] != null ? _cardBgs[0].GetComponent<RectTransform>() : null;
        RectTransform lastRT  = _cardBgs[_cards.Length - 1] != null
            ? _cardBgs[_cards.Length - 1].GetComponent<RectTransform>() : null;
        RectTransform selfRT  = transform as RectTransform;
        if (firstRT == null || lastRT == null || selfRT == null) yield break;

        Vector3[] firstCorners = new Vector3[4];
        Vector3[] lastCorners  = new Vector3[4];
        firstRT.GetWorldCorners(firstCorners);
        lastRT.GetWorldCorners(lastCorners);

        Vector2 firstLeft  = selfRT.InverseTransformPoint(firstCorners[0]);
        Vector2 lastRight  = selfRT.InverseTransformPoint(lastCorners[3]);
        float nodeCenterY  = selfRT.InverseTransformPoint(
            (firstCorners[0] + firstCorners[1]) * 0.5f).y;

        const float HINT_W = 58f;
        const float HINT_H = 30f;
        const float GAP    = 20f;

        Vector2 lbPos = new Vector2(firstLeft.x - GAP - HINT_W * 0.5f, nodeCenterY);
        Vector2 rbPos = new Vector2(lastRight.x + GAP + HINT_W * 0.5f, nodeCenterY);

        _lbHintBg = SpawnHint("LBHint", "LB", lbPos, HINT_W, HINT_H, isLeft: true);
        _rbHintBg = SpawnHint("RBHint", "RB", rbPos, HINT_W, HINT_H, isLeft: false);

        _hintsBuilt = true;
    }

    private Image SpawnHint(string goName, string label, Vector2 pos, float w, float h, bool isLeft)
    {
        GameObject root = new GameObject(goName);
        root.transform.SetParent(transform, false);

        RectTransform rt = root.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(w, h);
        rt.anchoredPosition = pos;

        Image bg         = root.AddComponent<Image>();
        bg.color         = HintResting;
        bg.raycastTarget = false;

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(root.transform, false);
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.richText      = true;
        tmp.text          = isLeft
            ? "<color=#F5C842>\u2039</color> <color=#E8EAF0>" + label + "</color>"
            : "<color=#E8EAF0>" + label + "</color> <color=#F5C842>\u203A</color>";
        tmp.fontSize      = 14f;
        tmp.fontStyle     = FontStyles.Bold;
        tmp.alignment     = TextAlignmentOptions.Center;
        tmp.color         = Color.white;
        tmp.raycastTarget = false;

        RectTransform trt = textGO.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(3f, 0f); trt.offsetMax = new Vector2(-3f, 0f);

        return bg;
    }

    /// <summary>Briefly flashes a hint pill gold on LB/RB press.</summary>
    private void FlashHint(Image hint)
    {
        if (hint == null) return;
        StartCoroutine(HintFlashRoutine(hint));
    }

    private IEnumerator HintFlashRoutine(Image img)
    {
        img.color = HintPressed;
        yield return new WaitForSecondsRealtime(0.12f);
        float elapsed = 0f;
        while (elapsed < 0.20f)
        {
            elapsed += Time.unscaledDeltaTime;
            img.color = Color.Lerp(HintPressed, HintResting, elapsed / 0.20f);
            yield return null;
        }
        img.color = HintResting;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private IEnumerator AnimateScale(RectTransform rt, float target, float duration)
    {
        Vector3 start = rt.localScale;
        Vector3 end   = Vector3.one * target;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            rt.localScale = Vector3.Lerp(start, end, EaseOut(t));
            yield return null;
        }
        rt.localScale = end;
    }

    private void DestroyChild(string name)
    {
        Transform t = transform.Find(name);
        if (t != null) Destroy(t.gameObject);
    }

    private static float EaseOut(float t) => 1f - Mathf.Pow(1f - t, 3f);
}
