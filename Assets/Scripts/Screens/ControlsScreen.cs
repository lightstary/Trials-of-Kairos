using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TrialsOfKairos.UI
{
    /// <summary>
    /// Controls reference screen — Xbox-controller only.
    /// Two-panel grid layout:
    ///   LEFT  panel: Movement (LS / RS / Menu button)
    ///   RIGHT panel: Time Mechanics (RT / LT / RT+LT) + Face Buttons (A / B / X / Y)
    ///
    /// Rows are built entirely in code — no prefab required.
    /// Each row: [Badge]  ACTION NAME / short description.
    /// Assign the Cinzel font assets in Inspector; falls back gracefully if null.
    /// </summary>
    public class ControlsScreen : UIScreenBase
    {
        [Header("Panel Roots")]
        [SerializeField] private RectTransform movementPanelRoot;
        [SerializeField] private RectTransform mechanicsPanelRoot;

        [Header("Navigation")]
        [SerializeField] private KairosButton backButton;

        [Header("Section Titles")]
        [SerializeField] private TextMeshProUGUI movementTitle;
        [SerializeField] private TextMeshProUGUI mechanicsTitle;

        [Header("Fonts (Cinzel)")]
        [SerializeField] private TMP_FontAsset cinzelSemiBold;   // Assets/Materials/Fonts/Cinzel-SemiBold SDF
        [SerializeField] private TMP_FontAsset cinzelRegular;    // Assets/Materials/Fonts/Cinzel-Regular SDF

        // ── Row data ─────────────────────────────────────────────────────────────────
        private static readonly Color Gold   = new Color(0.961f, 0.784f, 0.259f, 1f);
        private static readonly Color Blue   = new Color(0.353f, 0.706f, 0.941f, 1f);
        private static readonly Color Purple = new Color(0.608f, 0.365f, 0.898f, 1f);
        private static readonly Color Green  = new Color(0.271f, 0.761f, 0.275f, 1f);
        private static readonly Color Red    = new Color(0.898f, 0.196f, 0.106f, 1f);
        private static readonly Color XBlue  = new Color(0.224f, 0.478f, 0.918f, 1f);
        private static readonly Color Panel  = new Color(0.118f, 0.196f, 0.353f, 1f);

        private struct RowData { public string badge; public Color color; public string action; public string desc; }

        private static readonly RowData[] MovementRows =
        {
            new RowData { badge = "LS",   color = Panel, action = "Move",       desc = "Left stick — move in all directions"   },
            new RowData { badge = "RS",   color = Panel, action = "Look / Aim", desc = "Right stick — rotate camera"           },
            new RowData { badge = "MENU", color = Panel, action = "Pause",      desc = "Menu button — open pause screen"       },
        };

        private static readonly RowData[] MechanicsRows =
        {
            new RowData { badge = "RT",    color = Gold,   action = "Time Forward",    desc = "Hold RT — sand flows, world advances"     },
            new RowData { badge = "LT",    color = Blue,   action = "Time Frozen",     desc = "Hold LT — motion arrested, world holds"   },
            new RowData { badge = "RT+LT", color = Purple, action = "Time Reversed",   desc = "Hold both — time recedes, world unravels" },
            new RowData { badge = "A",     color = Green,  action = "Jump / Confirm",  desc = "Jump in world or confirm UI selection"    },
            new RowData { badge = "B",     color = Red,    action = "Dash / Cancel",   desc = "Quick dash or cancel a selection"         },
            new RowData { badge = "X",     color = XBlue,  action = "Interact / Grab", desc = "Interact with objects or grab surfaces"   },
            new RowData { badge = "Y",     color = Gold,   action = "Time Pulse",      desc = "Active ability — burst of time energy"    },
        };

        // ── Lifecycle ────────────────────────────────────────────────────────────────
        protected override void Awake()
        {
            base.Awake();
            TryAutoLoadFonts();

            // Shared sand/dust atmosphere background
            if (GetComponentInChildren<MainMenuAtmosphere>() == null)
            {
                MainMenuAtmosphere atm = gameObject.AddComponent<MainMenuAtmosphere>();
                atm.DisableSubtitleBreathing();
            }
        }

        private void Start()
        {
            EnsurePanelRoots();
            ApplySectionTitles();
            BuildGrid(movementPanelRoot,  MovementRows);
            BuildGrid(mechanicsPanelRoot, MechanicsRows);

            if (backButton != null)
                backButton.OnClicked.AddListener(() => UIManager.Instance.CloseControls());

            SetButtonLabel(backButton, "[ B ]  BACK");
        }

        /// <summary>Xbox B button navigates back to wherever Controls was opened from.</summary>
        protected override void OnCancelPressed()
        {
            UIManager.Instance.CloseControls();
        }

        /// <summary>
        /// Stacks both sections vertically in the center of the screen.
        ///
        /// Layout (normalized screen space, centered horizontally):
        ///   Title 1   — y 0.88 → 0.95
        ///   Movement  — y 0.57 → 0.88   (3 rows × 52px + padding)
        ///   Title 2   — y 0.50 → 0.57
        ///   Mechanics — y 0.08 → 0.50   (7 rows × 52px + padding)
        ///
        /// Both panels are anchored to the horizontal center third of the screen
        /// (anchorMin.x = 0.15, anchorMax.x = 0.85) so content is always centered.
        /// </summary>
        private void EnsurePanelRoots()
        {
            const float LEFT  = 0.15f;
            const float RIGHT = 0.85f;

            if (movementPanelRoot == null)
                movementPanelRoot = CreatePanelRoot("MovementPanel",
                    new Vector2(LEFT, 0.57f), new Vector2(RIGHT, 0.88f));

            if (mechanicsPanelRoot == null)
                mechanicsPanelRoot = CreatePanelRoot("MechanicsPanel",
                    new Vector2(LEFT, 0.08f), new Vector2(RIGHT, 0.50f));

            if (movementTitle == null)
                movementTitle = CreateSectionTitle("MovementTitle",
                    new Vector2(LEFT, 0.88f), new Vector2(RIGHT, 0.95f));

            if (mechanicsTitle == null)
                mechanicsTitle = CreateSectionTitle("MechanicsTitle",
                    new Vector2(LEFT, 0.50f), new Vector2(RIGHT, 0.57f));
        }

        private RectTransform CreatePanelRoot(string panelName, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject go = new GameObject(panelName, typeof(RectTransform));
            go.transform.SetParent(transform, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            VerticalLayoutGroup vlg     = go.AddComponent<VerticalLayoutGroup>();
            vlg.spacing                 = 2f;
            vlg.childAlignment          = TextAnchor.UpperCenter;
            vlg.childControlWidth       = true;
            vlg.childControlHeight      = false;
            vlg.childForceExpandWidth   = true;
            vlg.childForceExpandHeight  = false;
            vlg.padding                 = new RectOffset(8, 8, 8, 8);

            return rt;
        }

        private TextMeshProUGUI CreateSectionTitle(string titleName, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject go = new GameObject(titleName);
            go.transform.SetParent(transform, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = new Vector2(8f, 0f);
            rt.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.alignment      = TextAlignmentOptions.Bottom;
            tmp.fontSize       = 13f;
            tmp.raycastTarget  = false;
            return tmp;
        }

        // ── Grid builder (code-only, no prefab needed) ────────────────────────────────
        private void BuildGrid(RectTransform parent, RowData[] rows)
        {
            if (parent == null) return;

            // Clear existing placeholder children
            for (int i = parent.childCount - 1; i >= 0; i--)
                Destroy(parent.GetChild(i).gameObject);

            foreach (RowData row in rows)
                CreateRow(parent, row.badge, row.color, row.action, row.desc);
        }

        private void CreateRow(RectTransform parent, string badge, Color badgeColor,
                               string action, string desc)
        {
            // Row container — centered horizontally
            GameObject rowGO = new GameObject($"Row_{action}");
            rowGO.transform.SetParent(parent, false);
            HorizontalLayoutGroup hlg = rowGO.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing               = 12f;
            hlg.childAlignment        = TextAnchor.MiddleCenter;
            hlg.childControlWidth     = false;
            hlg.childControlHeight    = false;
            hlg.childForceExpandWidth = false;
            hlg.padding               = new RectOffset(0, 0, 4, 4);
            RectTransform rowRT = rowGO.GetComponent<RectTransform>();
            rowRT.sizeDelta = new Vector2(0f, 52f);

            // Badge
            GameObject badgeGO = new GameObject("Badge");
            badgeGO.transform.SetParent(rowGO.transform, false);
            Image badgeImg    = badgeGO.AddComponent<Image>();
            badgeImg.color    = badgeColor;
            RectTransform bRT = badgeGO.GetComponent<RectTransform>();
            bRT.sizeDelta     = new Vector2(44f, 44f);

            // Badge label
            GameObject badgeLblGO = new GameObject("BadgeTxt");
            badgeLblGO.transform.SetParent(badgeGO.transform, false);
            TextMeshProUGUI badgeTxt = badgeLblGO.AddComponent<TextMeshProUGUI>();
            badgeTxt.text      = badge;
            badgeTxt.fontSize  = badge.Length > 2 ? 8f : 14f;
            badgeTxt.alignment = TextAlignmentOptions.Center;
            badgeTxt.color     = IsLight(badgeColor)
                ? new Color(0.031f, 0.043f, 0.078f, 1f)
                : new Color(0.910f, 0.918f, 0.965f, 1f);
            if (cinzelSemiBold != null) badgeTxt.font = cinzelSemiBold;
            RectTransform blRT = badgeLblGO.GetComponent<RectTransform>();
            blRT.anchorMin = Vector2.zero; blRT.anchorMax = Vector2.one;
            blRT.offsetMin = Vector2.zero; blRT.offsetMax = Vector2.zero;

            // Text column
            GameObject textColGO = new GameObject("TextCol");
            textColGO.transform.SetParent(rowGO.transform, false);
            VerticalLayoutGroup vlg = textColGO.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment        = TextAnchor.MiddleCenter;
            vlg.childControlHeight    = false;
            vlg.childControlWidth     = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing               = 0f;
            RectTransform tcRT = textColGO.GetComponent<RectTransform>();
            tcRT.sizeDelta = new Vector2(320f, 52f);

            // Action label (HEADER tier)
            GameObject actionGO = new GameObject("Action");
            actionGO.transform.SetParent(textColGO.transform, false);
            TextMeshProUGUI actionTxt = actionGO.AddComponent<TextMeshProUGUI>();
            actionTxt.text             = action.ToUpper();
            actionTxt.fontSize         = 13f;
            actionTxt.characterSpacing = 8f;
            actionTxt.alignment        = TextAlignmentOptions.Center;
            actionTxt.color            = new Color(0.910f, 0.918f, 0.965f, 0.92f);
            if (cinzelSemiBold != null) actionTxt.font = cinzelSemiBold;
            RectTransform aRT = actionGO.GetComponent<RectTransform>();
            aRT.sizeDelta = new Vector2(320f, 26f);

            // Desc label (BODY tier)
            GameObject descGO = new GameObject("Desc");
            descGO.transform.SetParent(textColGO.transform, false);
            TextMeshProUGUI descTxt = descGO.AddComponent<TextMeshProUGUI>();
            descTxt.text             = desc;
            descTxt.fontSize         = 10f;
            descTxt.characterSpacing = 3f;
            descTxt.alignment        = TextAlignmentOptions.Center;
            descTxt.color            = new Color(0.910f, 0.918f, 0.965f, 0.38f);
            if (cinzelRegular != null) descTxt.font = cinzelRegular;
            RectTransform dRT = descGO.GetComponent<RectTransform>();
            dRT.sizeDelta = new Vector2(320f, 18f);
        }

        // ── Section titles ────────────────────────────────────────────────────────────
        private void ApplySectionTitles()
        {
            ApplyTitle(movementTitle,  "MOVEMENT");
            ApplyTitle(mechanicsTitle, "TIME MECHANICS AND ACTIONS");
        }

        private void ApplyTitle(TextMeshProUGUI label, string text)
        {
            if (label == null) return;
            label.text             = text;
            label.fontSize         = 13f;
            label.characterSpacing = 16f;
            label.color            = new Color(1f, 0.843f, 0f, 0.85f);
            if (cinzelSemiBold != null) label.font = cinzelSemiBold;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────────
        private static bool IsLight(Color c) =>
            (c.r * 0.299f + c.g * 0.587f + c.b * 0.114f) > 0.50f;

        private static void SetButtonLabel(KairosButton btn, string text)
        {
            if (btn == null) return;
            TextMeshProUGUI lbl = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (lbl != null) lbl.text = text;
        }

        private void TryAutoLoadFonts()
        {
#if UNITY_EDITOR
            if (cinzelSemiBold == null)
                cinzelSemiBold = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    "Assets/Materials/Fonts/Cinzel-SemiBold SDF.asset");
            if (cinzelRegular == null)
                cinzelRegular = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    "Assets/Materials/Fonts/Cinzel-Regular SDF.asset");
#endif
        }
    }
}
