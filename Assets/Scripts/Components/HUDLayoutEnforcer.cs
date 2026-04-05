using UnityEngine;

namespace TrialsOfKairos.UI
{
    /// <summary>
    /// Enforces anchor and position constraints on all HUD zones at runtime.
    /// Attach to the HUDScreen root. Runs once on Awake, then again on any
    /// resolution-change event so the layout never goes out of frame.
    ///
    /// HUD layout contract:
    ///   Top-left     → TimeStateIndicator
    ///   Top-center   → SplitTimeScaleMeter  (PRIMARY)
    ///   Top-right    → ObjectivePanel
    ///   Bottom-left  → PauseButton
    ///   Bottom-right → (reserved / secondary only)
    /// </summary>
    public class HUDLayoutEnforcer : MonoBehaviour
    {
        private const float EDGE_MARGIN   = 16f;
        private const float TOP_MARGIN    = 24f;
        private const float BOTTOM_MARGIN = 24f;

        [Header("HUD Zones")]
        [SerializeField] private RectTransform topLeft;     // TimeStateIndicator container
        [SerializeField] private RectTransform topCenter;   // SplitTimeScaleMeter container
        [SerializeField] private RectTransform topRight;    // ObjectivePanel container
        [SerializeField] private RectTransform bottomLeft;  // PauseButton container
        [SerializeField] private RectTransform bottomRight; // secondary (reserved)

        private void Awake() => Enforce();

        private void Enforce()
        {
            // Top-left
            SetAnchor(topLeft,
                anchor: new Vector2(0f, 1f),
                pivot:  new Vector2(0f, 1f),
                offset: new Vector2(EDGE_MARGIN, -TOP_MARGIN));

            // Top-center
            SetAnchor(topCenter,
                anchor: new Vector2(0.5f, 1f),
                pivot:  new Vector2(0.5f, 1f),
                offset: new Vector2(0f, -TOP_MARGIN));

            // Top-right
            SetAnchor(topRight,
                anchor: new Vector2(1f, 1f),
                pivot:  new Vector2(1f, 1f),
                offset: new Vector2(-EDGE_MARGIN, -TOP_MARGIN));

            // Bottom-left
            SetAnchor(bottomLeft,
                anchor: new Vector2(0f, 0f),
                pivot:  new Vector2(0f, 0f),
                offset: new Vector2(EDGE_MARGIN, BOTTOM_MARGIN));

            // Bottom-right
            SetAnchor(bottomRight,
                anchor: new Vector2(1f, 0f),
                pivot:  new Vector2(1f, 0f),
                offset: new Vector2(-EDGE_MARGIN, BOTTOM_MARGIN));
        }

        private static void SetAnchor(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 offset)
        {
            if (rt == null) return;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot     = pivot;
            rt.anchoredPosition = offset;
        }
    }
}
