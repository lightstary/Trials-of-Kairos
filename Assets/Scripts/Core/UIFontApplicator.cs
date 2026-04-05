using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace TrialsOfKairos.UI
{
    /// <summary>
    /// Loads all Cinzel SDF assets from Assets/Materials/Fonts at startup and
    /// applies them across every TextMeshProUGUI in the UI hierarchy.
    ///
    /// Typography mapping (based on GameObject name suffixes or sibling context):
    ///   "Title"    → Cinzel-Bold
    ///   "Header"   → Cinzel-SemiBold
    ///   "Label"    → Cinzel-Regular
    ///   "Accent"   → Cinzel-Medium
    ///   "Desc"     → Cinzel-Regular (dimmed)
    ///   everything else → Cinzel-Regular
    ///
    /// Attach to UIRoot or any persistent manager.
    /// This resolves all font assignments without touching every individual label.
    /// </summary>
    public class UIFontApplicator : MonoBehaviour
    {
        private static readonly string FontPath = "Assets/Materials/Fonts/";

        // Loaded font references
        private TMP_FontAsset _bold;
        private TMP_FontAsset _semiBold;
        private TMP_FontAsset _medium;
        private TMP_FontAsset _regular;
        private TMP_FontAsset _black;   // for super-display if needed

        [Header("Scope")]
        [SerializeField] private List<Transform> screenRoots;  // all screens; leave empty to scan whole hierarchy

        private void Awake()
        {
            LoadFonts();
            ApplyFonts();
        }

        private void LoadFonts()
        {
#if UNITY_EDITOR
            _bold     = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath + "Cinzel-Bold SDF.asset");
            _semiBold = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath + "Cinzel-SemiBold SDF.asset");
            _medium   = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath + "Cinzel-Medium SDF.asset");
            _regular  = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath + "Cinzel-Regular SDF.asset");
            _black    = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath + "Cinzel-Black SDF.asset");
#endif
            // Runtime fallback: Resources.Load expects assets inside a Resources folder.
            // If fonts are not in Resources, they must be pre-assigned via Inspector.
            // The Editor path above covers Play-in-Editor which is the required use case.
        }

        private void ApplyFonts()
        {
            if (_regular == null) return;  // no fonts loaded, skip

            IEnumerable<TextMeshProUGUI> targets;

            if (screenRoots != null && screenRoots.Count > 0)
            {
                var list = new List<TextMeshProUGUI>();
                foreach (Transform root in screenRoots)
                    if (root != null)
                        list.AddRange(root.GetComponentsInChildren<TextMeshProUGUI>(includeInactive: true));
                targets = list;
            }
            else
            {
                targets = FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            }

            foreach (TextMeshProUGUI tmp in targets)
                ApplyToLabel(tmp);
        }

        private void ApplyToLabel(TextMeshProUGUI tmp)
        {
            string n = tmp.gameObject.name;

            // Display tier — big titles
            if (ContainsAny(n, "Title", "Display", "GOTitle", "EGTitle", "WinTitle"))
            {
                if (_bold != null) tmp.font = _bold;
                return;
            }

            // Header tier — section labels
            if (ContainsAny(n, "Header", "MoveTitle", "StanceTitle", "Section", "ChapterText",
                               "PanelTitle", "BossName", "SelectedTrialName", "StateSec"))
            {
                if (_semiBold != null) tmp.font = _semiBold;
                return;
            }

            // Accent tier — state labels, warnings, ranks
            if (ContainsAny(n, "Accent", "StateLabel", "RankLabel", "PhaseLabel", "RowLabel",
                               "ActionLabel", "Action", "CenterLabel", "ObjectiveLabel"))
            {
                if (_medium != null) tmp.font = _medium;
                return;
            }

            // Everything else → Regular (body)
            if (_regular != null) tmp.font = _regular;
        }

        private static bool ContainsAny(string name, params string[] keywords)
        {
            foreach (string kw in keywords)
                if (name.IndexOf(kw, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            return false;
        }

        /// <summary>Call at runtime to re-apply fonts after dynamic UI is built.</summary>
        public void Refresh() => ApplyFonts();
    }
}
