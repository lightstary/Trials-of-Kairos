using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TrialsOfKairos.UI
{
    /// <summary>
    /// Pause menu: layered panel with time-distortion wave background.
    /// Includes an integrated level-travel display — a compact map that shows
    /// current, completed, locked, and selectable levels so players can
    /// fast-travel without leaving the pause flow.
    /// </summary>
    public class PauseMenuScreen : UIScreenBase
    {
        [Header("Panel")]
        [SerializeField] private RectTransform panelRect;

        [Header("Wave Overlay")]
        [SerializeField] private CanvasGroup waveOverlay;

        [Header("Buttons")]
        [SerializeField] private KairosButton resumeButton;
        [SerializeField] private KairosButton restartButton;
        [SerializeField] private KairosButton controlsButton;
        [SerializeField] private KairosButton trialSelectionButton;
        [SerializeField] private KairosButton mainMenuButton;

        // ── Level Travel Display ──────────────────────────────────────────────────────
        [Header("Level Travel Display")]
        [SerializeField] private RectTransform   travelDisplayRoot;  // container
        [SerializeField] private CanvasGroup     travelDisplayGroup;
        [SerializeField] private TextMeshProUGUI travelCurrentLabel; // "TRIAL 4"
        [SerializeField] private TextMeshProUGUI travelChapterLabel; // "CHAPTER I"
        [SerializeField] private Transform       travelNodeContainer;
        [SerializeField] private GameObject      travelNodePrefab;   // mini node prefab
        [SerializeField] private LevelData[]     levelDataList;      // same data as LevelSelect

        // Node color contract (matches LevelNodeUI)
        private static readonly Color NodeCompleted = new Color(1f,    0.843f, 0f,    1f);
        private static readonly Color NodeCurrent   = new Color(0.961f,0.784f, 0.259f,0.9f);
        private static readonly Color NodeLocked    = new Color(0.3f,  0.3f,  0.4f,  0.35f);
        private static readonly Color NodeBoss      = new Color(0.898f,0.196f, 0.106f,1f);
        private static readonly Color NodeSelectable= new Color(0.353f,0.706f, 0.941f,0.9f);

        private int _currentLevelIndex = 1;

        // ── Lifecycle ─────────────────────────────────────────────────────────────────
        private void Start()
        {
            if (resumeButton != null)
            {
                resumeButton.OnClicked.AddListener(() => UIManager.Instance.ClosePause());
                SetButtonLabel(resumeButton, "RESUME TRIAL");
            }
            if (restartButton != null)
            {
                restartButton.OnClicked.AddListener(() =>
                    ScreenTransitionManager.Instance?.CrossFade(0.25f, 0.05f, 0.3f,
                        () => UIManager.Instance.ShowScreen(UIScreenType.HUD)));
                SetButtonLabel(restartButton, "RESTART TRIAL");
            }
            if (controlsButton != null)
            {
                controlsButton.OnClicked.AddListener(() =>
                    UIManager.Instance.ShowScreen(UIScreenType.Controls));
                SetButtonLabel(controlsButton, "CONTROLS");
            }
            if (trialSelectionButton != null)
            {
                trialSelectionButton.OnClicked.AddListener(() =>
                    UIManager.Instance.ShowScreen(UIScreenType.LevelSelect));
                SetButtonLabel(trialSelectionButton, "TRIAL SELECTION");
            }
            if (mainMenuButton != null)
            {
                mainMenuButton.OnClicked.AddListener(() =>
                    ScreenTransitionManager.Instance?.CrossFade(0.3f, 0.1f, 0.4f,
                        () => UIManager.Instance.ShowScreen(UIScreenType.MainMenu)));
                SetButtonLabel(mainMenuButton, "MAIN MENU");
            }

            BuildTravelNodes();
        }

        protected override void OnBeforeShow()
        {
            if (panelRect          != null) panelRect.localScale          = Vector3.one * 0.92f;
            if (waveOverlay        != null) waveOverlay.alpha             = 0f;
            if (travelDisplayGroup != null) travelDisplayGroup.alpha      = 0f;
        }

        /// <summary>Xbox B button resumes from Pause.</summary>
        protected override void OnCancelPressed()
        {
            UIManager.Instance.ClosePause();
        }

        protected override void OnShown()
        {
            StartCoroutine(EntranceAnimation());
        }

        private IEnumerator EntranceAnimation()
        {
            if (panelRect != null)
                yield return UIAnimationUtils.ScaleRect(panelRect,
                    Vector3.one * 0.92f, Vector3.one, 0.22f, UIAnimationUtils.Overshoot);

            if (waveOverlay != null)
                StartCoroutine(UIAnimationUtils.FadeCanvasGroup(waveOverlay, 1f, 0.4f));

            if (travelDisplayGroup != null)
                yield return UIAnimationUtils.FadeCanvasGroup(travelDisplayGroup, 1f, 0.35f);
        }

        // ── Level Travel ─────────────────────────────────────────────────────────────
        /// <summary>Call from game logic to tell the pause menu which level is active.</summary>
        public void SetCurrentLevel(int levelIndex)
        {
            _currentLevelIndex = levelIndex;
            RefreshTravelDisplay();
        }

        private void BuildTravelNodes()
        {
            if (travelNodeContainer == null || levelDataList == null || levelDataList.Length == 0) return;

            foreach (Transform child in travelNodeContainer)
                Destroy(child.gameObject);

            foreach (LevelData data in levelDataList)
            {
                // Build node in code (prefab optional)
                GameObject go = travelNodePrefab != null
                    ? Instantiate(travelNodePrefab, travelNodeContainer)
                    : BuildCodeTravelNode(data);

                go.transform.SetParent(travelNodeContainer, false);

                Image nodeImg  = go.GetComponentInChildren<Image>();
                TextMeshProUGUI nodeLabel = go.GetComponentInChildren<TextMeshProUGUI>();

                Color nodeColor = GetNodeColor(data);
                if (nodeImg   != null) nodeImg.color = nodeColor;
                if (nodeLabel != null)
                {
                    nodeLabel.text  = data.isBossLevel ? "B" : data.levelIndex.ToString();
                    nodeLabel.color = new Color(0.031f, 0.043f, 0.078f, 1f);
                }

                if (data.isUnlocked && data.levelIndex != _currentLevelIndex)
                {
                    int capturedIndex = data.levelIndex;
                    UnityEngine.UI.Button btn = go.GetComponent<UnityEngine.UI.Button>()
                                             ?? go.AddComponent<UnityEngine.UI.Button>();
                    btn.onClick.AddListener(() => TravelToLevel(capturedIndex));
                }

                go.name = $"TravelNode_{data.levelIndex}";
            }

            RefreshTravelDisplay();
        }

        private static GameObject BuildCodeTravelNode(LevelData data)
        {
            GameObject go = new GameObject($"TravelNode_{data.levelIndex}");
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(30f, 30f);

            Image bg = go.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.17f, 0.25f, 0.5f); // overwritten by caller

            GameObject lgo = new GameObject("Label");
            lgo.transform.SetParent(go.transform, false);
            TextMeshProUGUI lbl = lgo.AddComponent<TextMeshProUGUI>();
            lbl.fontSize  = data.isBossLevel ? 11f : 9f;
            lbl.alignment = TextAlignmentOptions.Center;
            lbl.color     = new Color(0.031f, 0.043f, 0.078f, 1f);
            RectTransform lrt = lgo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;

            return go;
        }

        private void RefreshTravelDisplay()
        {
            if (levelDataList == null) return;

            // Update header labels
            foreach (LevelData data in levelDataList)
            {
                if (data.levelIndex != _currentLevelIndex) continue;
                if (travelCurrentLabel != null)
                    travelCurrentLabel.text = $"TRIAL  {data.levelIndex}  -  {data.trialName?.ToUpper() ?? ""}";
                if (travelChapterLabel != null)
                    travelChapterLabel.text = data.chapterName?.ToUpper() ?? "";
                break;
            }

            // Re-tint nodes
            if (travelNodeContainer == null) return;
            foreach (Transform child in travelNodeContainer)
            {
                // Match node to data by name convention
                string name  = child.name; // "TravelNode_X"
                if (!int.TryParse(name.Replace("TravelNode_", ""), out int idx)) continue;

                LevelData match = System.Array.Find(levelDataList, d => d.levelIndex == idx);
                if (match == null) continue;

                Image img = child.GetComponentInChildren<Image>();
                if (img != null) img.color = GetNodeColor(match);
            }
        }

        private Color GetNodeColor(LevelData data)
        {
            if (data.isBossLevel)  return NodeBoss;
            if (data.levelIndex == _currentLevelIndex) return NodeCurrent;
            if (data.isCompleted)  return NodeCompleted;
            if (data.isUnlocked)   return NodeSelectable;
            return NodeLocked;
        }

        private void TravelToLevel(int levelIndex)
        {
            // Close pause and load the selected level
            UIManager.Instance.ClosePause();
            ScreenTransitionManager.Instance?.CrossFade(0.3f, 0.1f, 0.4f,
                () => UIManager.Instance.ShowScreen(UIScreenType.HUD));
            // Actual level-loading is handled by game logic subscribing to an event or polling.
        }

        private void QuitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static void SetButtonLabel(KairosButton button, string text)
        {
            if (button == null) return;
            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = text;
        }
    }
}
