using UnityEngine;
using TMPro;

namespace TrialsOfKairos.UI
{
    /// <summary>
    /// Enforces Cinzel typography hierarchy across any GameObject subtree.
    /// Attach to a screen root and call Apply() or let it run on Awake.
    ///
    /// Tier tagging: add the component alongside TextMeshProUGUI and set the Tier
    /// field. The applicator then sets font, size, spacing and color automatically.
    ///
    /// Font asset must be placed at:
    ///   Assets/Materials/Fonts/Cinzel-Regular SDF.asset   (BODY)
    ///   Assets/Materials/Fonts/Cinzel-Bold SDF.asset      (DISPLAY)
    ///   Assets/Materials/Fonts/Cinzel-SemiBold SDF.asset  (HEADER)   — optional
    ///
    /// If only one Cinzel SDF asset exists, assign it to all three slots.
    /// All tiers remain visually distinct through size / spacing / color alone.
    /// </summary>
    public class CinzelTypographyApplicator : MonoBehaviour
    {
        public enum TypographyTier
        {
            Display,   // Cinzel Bold  — big titles
            Header,    // Cinzel SemiBold — section titles
            Body,      // Cinzel Regular  — labels / stats
            Accent,    // Cinzel Medium   — color-coded state text
        }

        [Header("Font Assets (assign same SDF if only one available)")]
        [SerializeField] private TMP_FontAsset displayFont;
        [SerializeField] private TMP_FontAsset headerFont;
        [SerializeField] private TMP_FontAsset bodyFont;
        [SerializeField] private TMP_FontAsset accentFont;

        [Header("Sizes")]
        [SerializeField] private float displaySize  = 52f;
        [SerializeField] private float headerSize   = 18f;
        [SerializeField] private float bodySize     = 13f;
        [SerializeField] private float accentSize   = 14f;

        [Header("Character Spacing")]
        [SerializeField] private float displaySpacing = 14f;
        [SerializeField] private float headerSpacing  = 10f;
        [SerializeField] private float bodySpacing    = 6f;
        [SerializeField] private float accentSpacing  = 8f;

        [Header("Colors")]
        [SerializeField] private Color displayColor = new Color(1f, 0.843f, 0f, 1f);
        [SerializeField] private Color headerColor  = new Color(0.910f, 0.918f, 0.965f, 0.9f);
        [SerializeField] private Color bodyColor    = new Color(0.910f, 0.918f, 0.965f, 0.6f);
        [SerializeField] private Color accentColor  = new Color(0.961f, 0.784f, 0.259f, 1f);

        [Header("Scope")]
        [SerializeField] private bool applyOnAwake = true;
        [SerializeField] private bool applyToChildren = true;

        private void Awake()
        {
            if (applyOnAwake) Apply();
        }

        /// <summary>Apply Cinzel typography to all tagged TypographyTag components in scope.</summary>
        public void Apply()
        {
            TypographyTag[] tags = applyToChildren
                ? GetComponentsInChildren<TypographyTag>(includeInactive: true)
                : GetComponents<TypographyTag>();

            foreach (TypographyTag tag in tags)
            {
                TextMeshProUGUI tmp = tag.GetComponent<TextMeshProUGUI>();
                if (tmp == null) continue;
                ApplyTier(tmp, tag.Tier);
            }
        }

        private void ApplyTier(TextMeshProUGUI tmp, TypographyTier tier)
        {
            switch (tier)
            {
                case TypographyTier.Display:
                    if (displayFont   != null) tmp.font = displayFont;
                    tmp.fontSize         = displaySize;
                    tmp.characterSpacing = displaySpacing;
                    tmp.color            = displayColor;
                    tmp.fontStyle        = FontStyles.Bold;
                    break;

                case TypographyTier.Header:
                    if (headerFont    != null) tmp.font = headerFont;
                    tmp.fontSize         = headerSize;
                    tmp.characterSpacing = headerSpacing;
                    tmp.color            = headerColor;
                    tmp.fontStyle        = FontStyles.Normal;
                    break;

                case TypographyTier.Body:
                    if (bodyFont      != null) tmp.font = bodyFont;
                    tmp.fontSize         = bodySize;
                    tmp.characterSpacing = bodySpacing;
                    tmp.color            = bodyColor;
                    tmp.fontStyle        = FontStyles.Normal;
                    break;

                case TypographyTier.Accent:
                    if (accentFont    != null) tmp.font = accentFont;
                    tmp.fontSize         = accentSize;
                    tmp.characterSpacing = accentSpacing;
                    tmp.color            = accentColor;
                    tmp.fontStyle        = FontStyles.Normal;
                    break;
            }
        }
    }

    /// <summary>
    /// Lightweight marker placed alongside a TextMeshProUGUI to declare its
    /// typography tier. The CinzelTypographyApplicator reads these to apply styles.
    /// </summary>
    [DisallowMultipleComponent]
    public class TypographyTag : MonoBehaviour
    {
        [SerializeField] private CinzelTypographyApplicator.TypographyTier tier = CinzelTypographyApplicator.TypographyTier.Body;
        public CinzelTypographyApplicator.TypographyTier Tier => tier;
    }
}
