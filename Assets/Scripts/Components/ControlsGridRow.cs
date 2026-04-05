using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TrialsOfKairos.UI
{
    /// <summary>
    /// A single row in the Controls Screen grid.
    /// Layout:  [BadgeImage]  [ActionLabel]
    ///                        [DescLabel]
    ///
    /// The badge image is sized square and tinted to the button color.
    /// Built and populated by ControlsScreen.BuildGrid().
    /// </summary>
    public class ControlsGridRow : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private Image           badgeImage;
        [SerializeField] private TextMeshProUGUI badgeLabel;
        [SerializeField] private TextMeshProUGUI actionLabel;
        [SerializeField] private TextMeshProUGUI descLabel;

        /// <summary>Populate this row with icon, action name, and description.</summary>
        public void Populate(string badgeText, Color badgeColor,
                             string action, string description)
        {
            if (badgeImage  != null) badgeImage.color  = badgeColor;
            if (badgeLabel  != null)
            {
                badgeLabel.text  = badgeText;
                badgeLabel.color = IsDark(badgeColor)
                    ? new Color(0.910f, 0.918f, 0.965f, 1f)
                    : new Color(0.031f, 0.043f, 0.078f, 1f);
            }
            if (actionLabel != null)
            {
                actionLabel.text       = action.ToUpper();
                actionLabel.fontSize   = 14f;
                actionLabel.characterSpacing = 8f;
                actionLabel.color      = new Color(0.910f, 0.918f, 0.965f, 0.90f);
            }
            if (descLabel != null)
            {
                descLabel.text       = description;
                descLabel.fontSize   = 11f;
                descLabel.color      = new Color(0.910f, 0.918f, 0.965f, 0.40f);
            }
        }

        private static bool IsDark(Color c) =>
            (c.r * 0.299f + c.g * 0.587f + c.b * 0.114f) < 0.35f;
    }
}
