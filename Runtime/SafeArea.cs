using UnityEngine;

namespace KidzDev.Unity.SafeArea
{
    /// <summary>
    /// Shrinks this RectTransform so its content stays inside the device safe area (notch, status bar,
    /// home-bar indicator, rounded corners).
    ///
    /// Usage:
    ///  (1) Put this on the top-level RectTransform of a GUI panel (anchors stretched, offsets 0).
    ///  (2) If the panel has a full-screen background you want to keep behind the notch, add this to an
    ///      immediate child that holds the foreground content instead, leaving the background full-bleed.
    ///  (3) Use <see cref="ConformX"/> / <see cref="ConformY"/> to constrain only one axis when mixing
    ///      full-width and full-height background stripes.
    ///
    /// Only the anchors are driven, so keep this rect's offsets at zero — it is expected to track its parent.
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("Layout/Safe Area")]
    public sealed class SafeArea : SafeAreaTracker
    {
        [Tooltip("Constrain the horizontal axis (left/right insets). Disable to ignore side cutouts.")]
        [SerializeField] bool conformX = true;

        [Tooltip("Constrain the vertical axis (top/bottom insets). Disable to ignore the notch / home bar.")]
        [SerializeField] bool conformY = true;

        [Tooltip("Also reset offsetMin/offsetMax to zero so the panel exactly fills the safe area even when " +
                 "its offsets were non-zero. Leave off to drive anchors only (classic behaviour).")]
        [SerializeField] bool zeroOffsets = false;

        /// <summary>Constrain the horizontal axis. Assigning re-applies on the next tick.</summary>
        public bool ConformX
        {
            get => conformX;
            set { conformX = value; SetDirty(); }
        }

        /// <summary>Constrain the vertical axis. Assigning re-applies on the next tick.</summary>
        public bool ConformY
        {
            get => conformY;
            set { conformY = value; SetDirty(); }
        }

        /// <summary>
        /// When true, also zeroes <c>offsetMin</c>/<c>offsetMax</c> so the panel fills the safe area exactly
        /// regardless of any prior offsets. Assigning re-applies on the next tick.
        /// </summary>
        public bool ZeroOffsets
        {
            get => zeroOffsets;
            set { zeroOffsets = value; SetDirty(); }
        }

        protected override void Apply(Rect safeArea)
        {
            if (Screen.width <= 0 || Screen.height <= 0)
                return;

            if (!conformX)
            {
                safeArea.x = 0f;
                safeArea.width = Screen.width;
            }

            if (!conformY)
            {
                safeArea.y = 0f;
                safeArea.height = Screen.height;
            }

            Vector2 anchorMin = new Vector2(safeArea.x / Screen.width, safeArea.y / Screen.height);
            Vector2 anchorMax = new Vector2(
                (safeArea.x + safeArea.width) / Screen.width,
                (safeArea.y + safeArea.height) / Screen.height);

            // Some Samsung devices (Note 10+, A71, S20) report NaN on the first frame — skip until valid.
            if (float.IsNaN(anchorMin.x) || float.IsNaN(anchorMin.y) ||
                float.IsNaN(anchorMax.x) || float.IsNaN(anchorMax.y))
                return;

            if (anchorMin.x < 0f || anchorMin.y < 0f || anchorMax.x < 0f || anchorMax.y < 0f)
                return;

            Panel.anchorMin = anchorMin;
            Panel.anchorMax = anchorMax;

            if (zeroOffsets)
            {
                Panel.offsetMin = Vector2.zero;
                Panel.offsetMax = Vector2.zero;
            }
        }
    }
}
