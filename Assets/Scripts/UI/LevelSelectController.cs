using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Level select / hub star-map screen.
/// </summary>
public class LevelSelectController : MonoBehaviour
{
    [Serializable]
    public class TrialNodeData
    {
        public string trialName;
        public string sceneName;
        public string timeMechanic;
        public string hazards;
        public bool   isUnlocked;
        public bool   isCompleted;
        public float  bestTime;
        public int    completionStars; // 0-3
    }

    [Header("Trial Data")]
    [SerializeField] private TrialNodeData[] trials;

    [Header("Node Visuals")]
    [SerializeField] private Button[] nodeButtons;
    [SerializeField] private Image[]  nodeIcons;

    [Header("Selection Panel")]
    [SerializeField] private GameObject      selectionPanel;
    [SerializeField] private CanvasGroup     selectionCanvasGroup;
    [SerializeField] private TextMeshProUGUI selectedTrialName;
    [SerializeField] private TextMeshProUGUI selectedMechanic;
    [SerializeField] private TextMeshProUGUI selectedHazards;
    [SerializeField] private TextMeshProUGUI selectedBestTime;
    [SerializeField] private Image[]         selectedDiamonds;
    [SerializeField] private Button          enterTrialButton;

    [Header("Chapter Navigation")]
    [SerializeField] private TextMeshProUGUI chapterTitle;
    [SerializeField] private Button          prevChapterButton;
    [SerializeField] private Button          nextChapterButton;

    [Header("Colors")]
    [SerializeField] private Color completedColor = new Color(1f, 0.843f, 0f);
    [SerializeField] private Color currentColor   = new Color(0.961f, 0.784f, 0.259f);
    [SerializeField] private Color lockedColor    = new Color(0.3f, 0.3f, 0.3f, 0.4f);

    private const float PULSE_SPEED    = 3f;
    private const float PULSE_MIN      = 0.5f;
    private const float SLIDE_DISTANCE = 300f;
    private const float SLIDE_DUR      = 0.3f;

    private int selectedIndex  = -1;
    private int currentChapter = 1;

    void Start()
    {
        for (int i = 0; i < nodeButtons.Length; i++)
        {
            int idx = i;
            if (nodeButtons[i] != null)
                nodeButtons[i].onClick.AddListener(() => SelectNode(idx));
        }

        if (enterTrialButton  != null) enterTrialButton.onClick.AddListener(EnterSelectedTrial);
        if (prevChapterButton != null) prevChapterButton.onClick.AddListener(PrevChapter);
        if (nextChapterButton != null) nextChapterButton.onClick.AddListener(NextChapter);

        UpdateNodeVisuals();
        HideSelectionPanel();
        UpdateChapterTitle();
    }

    void Update() => PulseCurrentNodes();

    /// <summary>Selects a trial node and shows its details.</summary>
    public void SelectNode(int index)
    {
        if (trials == null || index < 0 || index >= trials.Length) return;
        if (!trials[index].isUnlocked) return;
        selectedIndex = index;
        ShowSelectionPanel(trials[index]);
    }

    private void UpdateNodeVisuals()
    {
        if (trials == null || nodeIcons == null) return;
        for (int i = 0; i < nodeIcons.Length && i < trials.Length; i++)
        {
            if (nodeIcons[i] == null) continue;
            nodeIcons[i].color = !trials[i].isUnlocked ? lockedColor
                               : trials[i].isCompleted  ? completedColor
                               : currentColor;
            if (nodeButtons != null && i < nodeButtons.Length && nodeButtons[i] != null)
                nodeButtons[i].interactable = trials[i].isUnlocked;
        }
    }

    private void PulseCurrentNodes()
    {
        if (trials == null || nodeIcons == null) return;
        float pulse = Mathf.Lerp(PULSE_MIN, 1f, (Mathf.Sin(Time.time * PULSE_SPEED) + 1f) * 0.5f);
        for (int i = 0; i < nodeIcons.Length && i < trials.Length; i++)
        {
            if (nodeIcons[i] != null && trials[i].isUnlocked && !trials[i].isCompleted)
            {
                Color c = currentColor; c.a = pulse; nodeIcons[i].color = c;
            }
        }
    }

    private void ShowSelectionPanel(TrialNodeData t)
    {
        if (selectionPanel != null) selectionPanel.SetActive(true);
        if (selectedTrialName != null) selectedTrialName.text = t.trialName.ToUpper();
        if (selectedMechanic  != null) selectedMechanic.text  = t.timeMechanic;
        if (selectedHazards   != null) selectedHazards.text   = t.hazards;
        if (selectedBestTime  != null)
            selectedBestTime.text = t.bestTime > 0f
                ? $"{Mathf.FloorToInt(t.bestTime / 60f)}:{Mathf.FloorToInt(t.bestTime % 60f):D2}"
                : "--:--";

        Color gold = TimeStateUIManager.Instance != null
            ? TimeStateUIManager.Instance.goldColor : completedColor;
        if (selectedDiamonds != null)
            for (int i = 0; i < selectedDiamonds.Length; i++)
                if (selectedDiamonds[i] != null)
                    selectedDiamonds[i].color = i < t.completionStars ? gold : new Color(0.3f, 0.3f, 0.3f, 0.5f);

        if (enterTrialButton != null) enterTrialButton.interactable = t.isUnlocked;

        StopAllCoroutines();
        StartCoroutine(AnimatePanelIn());
    }

    private void HideSelectionPanel()
    {
        if (selectionPanel != null) selectionPanel.SetActive(false);
        selectedIndex = -1;
    }

    private void EnterSelectedTrial()
    {
        if (selectedIndex < 0 || selectedIndex >= trials.Length) return;
        string scene = trials[selectedIndex].sceneName;
        if (string.IsNullOrEmpty(scene)) return;
        if (ScreenTransitionManager.Instance != null)
            ScreenTransitionManager.Instance.FadeToScene(scene);
        else
            SceneManager.LoadScene(scene);
    }

    private void PrevChapter() { currentChapter = Mathf.Max(1, currentChapter - 1); UpdateChapterTitle(); }
    private void NextChapter() { currentChapter++;                                    UpdateChapterTitle(); }

    private void UpdateChapterTitle()
    {
        if (chapterTitle != null)
            chapterTitle.text = $"CHAPTER {ToRoman(currentChapter)}";
    }

    private IEnumerator AnimatePanelIn()
    {
        if (selectionCanvasGroup == null) yield break;
        RectTransform rt = selectionPanel.GetComponent<RectTransform>();
        Vector2 end   = rt != null ? rt.anchoredPosition : Vector2.zero;
        Vector2 start = end + Vector2.right * SLIDE_DISTANCE;
        float elapsed = 0f;
        selectionCanvasGroup.alpha = 0f;
        while (elapsed < SLIDE_DUR)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / SLIDE_DUR);
            selectionCanvasGroup.alpha = t;
            if (rt != null) rt.anchoredPosition = Vector2.Lerp(start, end, EaseOut(t));
            yield return null;
        }
        selectionCanvasGroup.alpha = 1f;
        if (rt != null) rt.anchoredPosition = end;
    }

    private static string ToRoman(int n)
    {
        if (n <= 0) return n.ToString();
        string[] t = { "", "M","MM","MMM" };
        string[] h = { "","C","CC","CCC","CD","D","DC","DCC","DCCC","CM" };
        string[] te = { "","X","XX","XXX","XL","L","LX","LXX","LXXX","XC" };
        string[] o = { "","I","II","III","IV","V","VI","VII","VIII","IX" };
        return t[Math.Min(n/1000,3)] + h[Math.Min((n%1000)/100,9)]
             + te[Math.Min((n%100)/10,9)] + o[Math.Min(n%10,9)];
    }

    private static float EaseOut(float t) => 1f - Mathf.Pow(1f - t, 3f);
}
