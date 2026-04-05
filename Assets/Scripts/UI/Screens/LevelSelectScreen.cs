using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TrialsOfKairos.UI
{
    /// <summary>
    /// Level select screen: star-map node layout with gateway selection panel.
    /// </summary>
    public class LevelSelectScreen : UIScreenBase
    {
        [Header("Level Data")]
        [SerializeField] private LevelData[] levels;

        [Header("Node Container")]
        [SerializeField] private Transform nodeContainer;
        [SerializeField] private GameObject levelNodePrefab;

        [Header("Selection Panel")]
        [SerializeField] private CanvasGroup     selectionPanel;
        [SerializeField] private TextMeshProUGUI selectedTrialName;
        [SerializeField] private TextMeshProUGUI selectedMechanic;
        [SerializeField] private TextMeshProUGUI selectedHazards;
        [SerializeField] private TextMeshProUGUI selectedBestTime;
        [SerializeField] private Transform       starsContainer;
        [SerializeField] private KairosButton    enterButton;

        [Header("Navigation")]
        [SerializeField] private KairosButton backButton;

        private LevelData _selectedLevel;
        private int _selectedIndex;
        private float _dpadCooldown;
        private readonly List<LevelNodeUI> _nodes = new();
        private Coroutine _showPanelRoutine;

        // Two-phase selection: browse with LB/RB, confirm with A, then Enter Trial lights up
        private bool _nodeConfirmed  = false;
        private bool _confirmBlocked = false;  // prevents same A-press from both confirming & entering

        // References to hint Images for press-flash feedback
        private Image _lbHintBg;
        private Image _rbHintBg;

        private const float DPAD_REPEAT_DELAY = 0.25f;

        private void Start()
        {
            if (backButton  != null) backButton.OnClicked.AddListener(NavigateBack);
            if (enterButton != null) enterButton.OnClicked.AddListener(EnterSelectedTrial);

            SetButtonLabel(enterButton, "ENTER TRIAL");
            SetButtonLabel(backButton,  "[ B ]  BACK");

            // Enter Trial starts dimmed — must confirm a node with A first
            SetEnterButtonState(false);

            // Add atmosphere (sand/dust + rotating squares) as background, same as Main Menu
            if (GetComponentInChildren<MainMenuAtmosphere>() == null)
            {
                MainMenuAtmosphere atm = gameObject.AddComponent<MainMenuAtmosphere>();
                atm.DisableSubtitleBreathing();
            }

            BuildNodes();
        }

        /// <summary>
        /// Xbox B button: returns to PauseMenu if opened from there, otherwise to MainMenu.
        /// </summary>
        protected override void OnCancelPressed() => NavigateBack();

        private void NavigateBack()
        {
            UIScreenType returnTo = UIManager.Instance.PreviousScreenType == UIScreenType.Pause
                ? UIScreenType.Pause
                : UIScreenType.MainMenu;
            UIManager.Instance.ShowScreen(returnTo);
        }

        private void LateUpdate()
        {
            if (CanvasGroup == null || !CanvasGroup.interactable) return;

            // LB = JoystickButton4, RB = JoystickButton5
            bool lb = Input.GetKeyDown(KeyCode.JoystickButton4) || Input.GetKeyDown(KeyCode.LeftArrow);
            bool rb = Input.GetKeyDown(KeyCode.JoystickButton5) || Input.GetKeyDown(KeyCode.RightArrow);

            if (lb) { NavigateNode(-1); FlashHint(_lbHintBg); }
            if (rb) { NavigateNode( 1); FlashHint(_rbHintBg); }

            // D-pad horizontal
            float dpadH = Input.GetAxisRaw("Horizontal");
            if (_dpadCooldown <= 0f)
            {
                if (dpadH < -0.5f) { NavigateNode(-1); FlashHint(_lbHintBg); _dpadCooldown = DPAD_REPEAT_DELAY; }
                if (dpadH >  0.5f) { NavigateNode( 1); FlashHint(_rbHintBg); _dpadCooldown = DPAD_REPEAT_DELAY; }
            }
            else
            {
                _dpadCooldown -= Time.unscaledDeltaTime;
                if (Mathf.Abs(dpadH) < 0.2f) _dpadCooldown = 0f;
            }

            // A button (JoystickButton0) or Space = confirm current browsed node
            if (Input.GetKeyDown(KeyCode.JoystickButton0) || Input.GetKeyDown(KeyCode.Space))
                ConfirmNode();
        }

        /// <summary>
        /// Confirms the currently browsed node. Lights up Enter Trial if the level is unlocked.
        /// Blocks Enter Trial from also firing this same frame via _confirmBlocked.
        /// </summary>
        private void ConfirmNode()
        {
            if (_selectedLevel == null || _confirmBlocked) return;
            _nodeConfirmed = _selectedLevel.isUnlocked;
            SetEnterButtonState(_nodeConfirmed);

            if (_nodeConfirmed)
                StartCoroutine(BlockConfirmForOneFrame());

            // Visual: scale burst on the selected node
            if (_selectedIndex >= 0 && _selectedIndex < _nodes.Count)
                StartCoroutine(ConfirmNodePulse(_nodes[_selectedIndex]));
        }

        /// <summary>
        /// Enables Enter Trial but sets _confirmBlocked for two frames so the same
        /// A-press that confirmed doesn't also immediately fire the button's Submit.
        /// </summary>
        private IEnumerator BlockConfirmForOneFrame()
        {
            _confirmBlocked = true;
            yield return null;
            yield return null;
            _confirmBlocked = false;
        }

        private IEnumerator ConfirmNodePulse(LevelNodeUI node)
        {
            if (node == null) yield break;
            RectTransform rt = node.GetComponent<RectTransform>();
            if (rt == null) yield break;

            // Quick scale burst: 1.0 → 1.35 → 1.22 (overshoot then settle)
            float dur = 0.12f;
            float elapsed = 0f;
            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                rt.localScale = Vector3.one * Mathf.Lerp(1f, 1.35f, elapsed / dur);
                yield return null;
            }
            elapsed = 0f;
            while (elapsed < 0.18f)
            {
                elapsed += Time.unscaledDeltaTime;
                rt.localScale = Vector3.one * Mathf.Lerp(1.35f, 1.22f, elapsed / 0.18f);
                yield return null;
            }
        }

        /// <summary>Dims or lights up the Enter Trial button.</summary>
        private void SetEnterButtonState(bool active)
        {
            if (enterButton == null) return;
            CanvasGroup cg = enterButton.GetComponent<CanvasGroup>();
            if (cg == null) cg = enterButton.gameObject.AddComponent<CanvasGroup>();
            cg.alpha          = active ? 1f : 0.35f;
            cg.interactable   = active;
            cg.blocksRaycasts = active;
        }

        /// <summary>Briefly flashes a hint pill bright gold on LB/RB press.</summary>
        private void FlashHint(Image hintBg)
        {
            if (hintBg == null) return;
            StartCoroutine(HintFlash(hintBg));
        }

        private IEnumerator HintFlash(Image img)
        {
            Color pressed  = new Color(0.95f, 0.78f, 0.10f, 0.95f);
            Color resting  = new Color(0.08f, 0.08f, 0.16f, 0.72f);
            img.color = pressed;
            yield return new WaitForSecondsRealtime(0.12f);
            float elapsed = 0f;
            while (elapsed < 0.20f)
            {
                elapsed += Time.unscaledDeltaTime;
                img.color = Color.Lerp(pressed, resting, elapsed / 0.20f);
                yield return null;
            }
            img.color = resting;
        }

        /// <summary>
        /// Moves selection left or right by one, wrapping around.
        /// Navigates to ALL nodes (locked or not) so the user can browse info;
        /// entering a locked trial is blocked in EnterSelectedTrial.
        /// </summary>
        private void NavigateNode(int delta)
        {
            if (_nodes.Count == 0) return;

            // Browsing resets confirmation — player must re-confirm the new node
            _nodeConfirmed = false;
            SetEnterButtonState(false);

            int next = (_selectedIndex + delta + _nodes.Count) % _nodes.Count;

            foreach (LevelNodeUI n in _nodes) n.SetSelected(false);
            _nodes[next].SetSelected(true);
            _selectedIndex = next;
            _selectedLevel = _nodes[next].BoundData;

            if (_selectedLevel != null)
            {
                PopulatePanel(_selectedLevel);
                ShowPanelTracked();
            }
        }

        protected override void OnBeforeShow()
        {
            _nodeConfirmed = false;
            SetEnterButtonState(false);
            if (selectionPanel != null)
            {
                selectionPanel.alpha          = 0f;
                selectionPanel.interactable   = false;
                selectionPanel.blocksRaycasts = false;
            }
        }

        protected override void OnShown()
        {
            _nodeConfirmed = false;
            SetEnterButtonState(false);
            if (_nodes.Count == 0) { BuildNodes(); return; }

            int idx = (_selectedIndex >= 0 && _selectedIndex < _nodes.Count) ? _selectedIndex : 0;
            foreach (LevelNodeUI n in _nodes) n.SetSelected(false);
            _nodes[idx].SetSelected(true);
            _selectedIndex = idx;
            _selectedLevel = _nodes[idx].BoundData;

            if (_selectedLevel != null)
            {
                PopulatePanel(_selectedLevel);
                ShowPanelTracked();
            }
        }

        private void BuildNodes()
        {
            if (nodeContainer == null || levels == null) return;
            _nodes.Clear();

            List<Transform> existingNodes = new List<Transform>();
            foreach (Transform child in nodeContainer)
            {
                if (child.name == "NodeConnector") continue;
                existingNodes.Add(child);
            }

            bool reuseExisting = existingNodes.Count == levels.Length;

            if (reuseExisting)
            {
                for (int i = 0; i < levels.Length; i++)
                {
                    Transform  t    = existingNodes[i];
                    LevelData  data = levels[i];
                    LevelNodeUI existing = t.GetComponent<LevelNodeUI>();
                    existing?.StopAndClean();
                    LevelNodeUI node = existing != null ? existing : t.gameObject.AddComponent<LevelNodeUI>();
                    node.Initialise(data, OnNodeSelected);
                    _nodes.Add(node);
                }
            }
            else
            {
                for (int i = nodeContainer.childCount - 1; i >= 0; i--)
                {
                    Transform child = nodeContainer.GetChild(i);
                    if (child.name == "NodeConnector") continue;
                    LevelNodeUI existingNode = child.GetComponent<LevelNodeUI>();
                    existingNode?.StopAndClean();
                    Destroy(child.gameObject);
                }

                foreach (LevelData data in levels)
                {
                    GameObject  go   = BuildCodeNode(data);
                    LevelNodeUI node = go.GetComponent<LevelNodeUI>();
                    if (node != null)
                    {
                        node.Initialise(data, OnNodeSelected);
                        _nodes.Add(node);
                    }
                }
            }

            // Auto-select first unlocked level
            LevelNodeUI firstUnlocked = _nodes.Find(n => n.IsUnlocked) ?? (_nodes.Count > 0 ? _nodes[0] : null);
            if (firstUnlocked != null)
            {
                _selectedIndex = _nodes.IndexOf(firstUnlocked);
                firstUnlocked.SetSelected(true);
                _selectedLevel = firstUnlocked.BoundData;
                if (_selectedLevel != null)
                {
                    PopulatePanel(_selectedLevel);
                    ShowPanelTracked();
                }
            }

            // Defer hint placement one frame so the LayoutGroup finishes positioning nodes
            StartCoroutine(BuildNavHintsDeferred());
        }

        private IEnumerator BuildNavHintsDeferred()
        {
            yield return null;   // wait for layout to settle
            BuildNavHints();
        }

        /// <summary>
        /// Builds LB/RB hints parented to the screen, positioned in world space
        /// beside the actual first and last node. Runs after BuildNodes so the
        /// node RectTransforms are already laid out.
        /// </summary>
        private void BuildNavHints()
        {
            if (nodeContainer == null) return;

            // Clean up stale hints from the screen root
            foreach (string n in new[] { "LBHint", "RBHint" })
            {
                Transform old = transform.Find(n);
                if (old != null) Destroy(old.gameObject);
            }

            // Also clean from container (legacy)
            foreach (string n in new[] { "LBHint", "RBHint", "ConfirmHint" })
            {
                Transform old = nodeContainer.Find(n);
                if (old != null) Destroy(old.gameObject);
            }

            if (_nodes.Count == 0) return;

            // Get the RectTransform of the first and last nodes so we can sit right beside them
            RectTransform firstRT = _nodes[0].GetComponent<RectTransform>();
            RectTransform lastRT  = _nodes[_nodes.Count - 1].GetComponent<RectTransform>();
            RectTransform selfRT  = transform as RectTransform;

            if (firstRT == null || lastRT == null || selfRT == null) return;

            // Convert the node corners to the screen's local space
            Vector3[] firstCorners = new Vector3[4];
            Vector3[] lastCorners  = new Vector3[4];
            firstRT.GetWorldCorners(firstCorners);
            lastRT.GetWorldCorners(lastCorners);

            // firstCorners[0] = bottom-left, [1] = top-left, [2] = top-right, [3] = bottom-right
            Vector2 firstLeft  = selfRT.InverseTransformPoint(firstCorners[0]);   // left edge of node 1
            Vector2 lastRight  = selfRT.InverseTransformPoint(lastCorners[3]);    // right edge of last node
            float nodeHeight   = firstCorners[1].y - firstCorners[0].y;           // world height
            float nodeCenterY  = selfRT.InverseTransformPoint(
                (firstCorners[0] + firstCorners[1]) * 0.5f).y;                   // vertical center of nodes

            const float HINT_W  = 58f;
            const float HINT_H  = 30f;
            const float GAP     = 20f; // gap between hint pill edge and node edge

            // LB: right edge of pill aligns with left edge of node 1, minus GAP
            Vector2 lbPos = new Vector2(firstLeft.x - GAP - HINT_W * 0.5f, nodeCenterY);
            // RB: left edge of pill aligns with right edge of last node, plus GAP
            Vector2 rbPos = new Vector2(lastRight.x + GAP + HINT_W * 0.5f, nodeCenterY);

            _lbHintBg = SpawnHint("LBHint", "LB", transform, lbPos, HINT_W, HINT_H, isLeft: true);
            _rbHintBg = SpawnHint("RBHint", "RB", transform, rbPos, HINT_W, HINT_H, isLeft: false);
        }

        private Image SpawnHint(string goName, string label, Transform parent,
            Vector2 anchoredPos, float w, float h, bool isLeft)
        {
            GameObject root = new GameObject(goName);
            root.transform.SetParent(parent, false);
            RectTransform rootRt    = root.AddComponent<RectTransform>();
            rootRt.anchorMin        = new Vector2(0.5f, 0.5f);
            rootRt.anchorMax        = new Vector2(0.5f, 0.5f);
            rootRt.pivot            = new Vector2(0.5f, 0.5f);
            rootRt.sizeDelta        = new Vector2(w, h);
            rootRt.anchoredPosition = anchoredPos;

            Image bg         = root.AddComponent<Image>();
            bg.color         = new Color(0.08f, 0.08f, 0.16f, 0.72f);
            bg.raycastTarget = false;

            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(root.transform, false);
            TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.richText      = true;
            tmp.text          = isLeft
                ? $"<color=#F5C842>‹</color> <color=#E8EAF0>{label}</color>"
                : $"<color=#E8EAF0>{label}</color> <color=#F5C842>›</color>";
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

        /// <summary>Builds a level node entirely in code, wiring all visual references.</summary>
        private GameObject BuildCodeNode(LevelData data)
        {
            Color nodeColor = GetNodeColor(data);

            // ── Root ───────────────────────────────────────────────────────────────────
            GameObject go = new GameObject($"Node_{data.levelIndex}");
            go.transform.SetParent(nodeContainer, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(72f, 72f);

            // ── Selection ring (outermost — ice blue pulse, hidden until selected) ─────
            GameObject selGO = new GameObject("SelectionRing");
            selGO.transform.SetParent(go.transform, false);
            selGO.transform.SetAsFirstSibling();
            Image selImg = selGO.AddComponent<Image>();
            selImg.color = new Color(0.45f, 0.75f, 1.0f, 0f);
            RectTransform selRT = selGO.GetComponent<RectTransform>();
            selRT.anchorMin = new Vector2(-0.30f, -0.30f);
            selRT.anchorMax = new Vector2( 1.30f,  1.30f);
            selRT.offsetMin = Vector2.zero;
            selRT.offsetMax = Vector2.zero;
            selGO.SetActive(false);

            // ── Ambient pulse ring (slightly larger than node, visible while current) ──
            GameObject ringGO = new GameObject("Ring");
            ringGO.transform.SetParent(go.transform, false);
            ringGO.transform.SetAsFirstSibling();
            Image ringImg = ringGO.AddComponent<Image>();
            ringImg.color = nodeColor;
            RectTransform ringRT = ringGO.GetComponent<RectTransform>();
            ringRT.anchorMin = new Vector2(-0.18f, -0.18f);
            ringRT.anchorMax = new Vector2( 1.18f,  1.18f);
            ringRT.offsetMin = Vector2.zero;
            ringRT.offsetMax = Vector2.zero;
            ringGO.SetActive(data.isUnlocked && !data.isCompleted);

            // ── Node background ────────────────────────────────────────────────────────
            Image bg = go.AddComponent<Image>();
            bg.color = nodeColor;

            // ── Index label ────────────────────────────────────────────────────────────
            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            TextMeshProUGUI label = labelGO.AddComponent<TextMeshProUGUI>();
            label.text      = data.isBossLevel ? "BOSS" : data.levelIndex.ToString();
            label.fontSize  = 22f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color     = new Color(0.031f, 0.043f, 0.078f, 1f);
            RectTransform lrt = labelGO.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;

            // ── Lock overlay ───────────────────────────────────────────────────────────
            if (!data.isUnlocked)
            {
                GameObject lockGO = new GameObject("LockOverlay");
                lockGO.transform.SetParent(go.transform, false);
                Image lockImg = lockGO.AddComponent<Image>();
                lockImg.color = new Color(0f, 0f, 0f, 0.55f);
                RectTransform lkrt = lockGO.GetComponent<RectTransform>();
                lkrt.anchorMin = Vector2.zero; lkrt.anchorMax = Vector2.one;
                lkrt.offsetMin = Vector2.zero; lkrt.offsetMax = Vector2.zero;
            }

            // ── Wire all references to LevelNodeUI ────────────────────────────────────
            LevelNodeUI nodeUI = go.AddComponent<LevelNodeUI>();
            nodeUI.ConfigureReferences(bg, ringImg, selImg, label);

            return go;
        }

        private static Color GetNodeColor(LevelData data)
        {
            if (data.isBossLevel) return new Color(0.898f, 0.196f, 0.106f, 1f);
            if (data.isCompleted) return new Color(1f,     0.843f, 0f,    1f);
            if (data.isUnlocked)  return new Color(0.353f, 0.706f, 0.941f, 0.9f);
            return new Color(0.15f, 0.17f, 0.25f, 0.6f);
        }

        private void OnNodeSelected(LevelData data)
        {
            _selectedLevel = data;
            _selectedIndex = _nodes.FindIndex(n => n.BoundData == data);
            PopulatePanel(data);
            foreach (LevelNodeUI node in _nodes) node.SetSelected(false);
            LevelNodeUI chosen = _nodes.Find(n => n.BoundData == data);
            chosen?.SetSelected(true);
            ShowPanelTracked();
        }

        private void PopulatePanel(LevelData data)
        {
            if (selectedTrialName != null)
            {
                // Typewriter reveal for the trial name
                StopCoroutine("TypewriterTrialName");
                StartCoroutine(UIAnimationUtils.TypewriterReveal(
                    selectedTrialName, data.trialName.ToUpper(), 0.45f));
            }
            if (selectedMechanic != null)
            {
                selectedMechanic.text = data.timeMechanic;
                StartCoroutine(FadeInLabel(selectedMechanic, 0.30f));
            }
            if (selectedHazards != null)
            {
                selectedHazards.text = data.hazardDescription;
                StartCoroutine(FadeInLabel(selectedHazards, 0.45f));
            }
            if (selectedBestTime != null)
            {
                selectedBestTime.text = data.bestTimeSeconds > 0f
                    ? $"{Mathf.FloorToInt(data.bestTimeSeconds / 60f):D1}:{data.bestTimeSeconds % 60f:00.##}"
                    : "N/A";
                StartCoroutine(FadeInLabel(selectedBestTime, 0.20f));
            }
        }

        /// <summary>Snaps a label alpha to zero then fades it in over duration.</summary>
        private static IEnumerator FadeInLabel(TextMeshProUGUI label, float duration)
        {
            Color c = label.color; c.a = 0f; label.color = c;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                c.a = Mathf.Clamp01(elapsed / duration);
                label.color = c;
                yield return null;
            }
            c.a = 1f; label.color = c;
        }

        private IEnumerator ShowPanel()
        {
            if (selectionPanel == null) yield break;
            selectionPanel.interactable   = false;
            selectionPanel.blocksRaycasts = false;
            selectionPanel.alpha          = 0f;
            yield return UIAnimationUtils.FadeCanvasGroup(selectionPanel, 1f, 0.22f);
            selectionPanel.interactable   = true;
            selectionPanel.blocksRaycasts = true;
        }

        private void ShowPanelTracked()
        {
            if (_showPanelRoutine != null) StopCoroutine(_showPanelRoutine);
            _showPanelRoutine = StartCoroutine(ShowPanel());
        }

        private void EnterSelectedTrial()
        {
            if (_selectedLevel == null || !_selectedLevel.isUnlocked) return;
            ScreenTransitionManager.Instance?.CrossFade(0.3f, 0.1f, 0.4f,
                () => UIManager.Instance.ShowScreen(UIScreenType.HUD));
        }

        /// <summary>Sets the text label on a KairosButton's child TMP label.</summary>
        private static void SetButtonLabel(KairosButton button, string text)
        {
            if (button == null) return;
            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = text;
        }
    }
}
