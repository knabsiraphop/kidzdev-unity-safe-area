using UnityEngine;

namespace KidzDev.Unity.SafeArea
{
    /// <summary>
    /// Shared base for safe-area components. Watches the resolved safe area, screen size and orientation,
    /// and invokes <see cref="Apply"/> only when one of them actually changes — or when a re-apply is
    /// forced via <see cref="SetDirty"/> (e.g. after an inspector edit). Subclasses decide what to do with
    /// the safe area in <see cref="Apply"/>.
    ///
    /// The single per-frame poll mirrors the proven Crystal SafeArea approach: the comparison is a handful
    /// of value checks and nothing is touched on an idle frame.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public abstract class SafeAreaTracker : MonoBehaviour
    {
        /// <summary>The RectTransform this component is attached to. Cached on Awake.</summary>
        protected RectTransform Panel { get; private set; }

        Rect _lastSafeArea = new Rect(0f, 0f, 0f, 0f);
        Vector2Int _lastScreenSize = new Vector2Int(0, 0);
        ScreenOrientation _lastOrientation = ScreenOrientation.AutoRotation;
        bool _dirty = true;

        protected virtual void Awake()
        {
            CachePanel();
            SetDirty();
        }

        protected virtual void OnEnable()
        {
            SetDirty();
        }

        protected virtual void OnValidate()
        {
            // Inspector edits can fire in a phase where creating/destroying objects is illegal, so we only
            // raise the flag here and let the next Update tick do the real work.
            SetDirty();
        }

        /// <summary>Forces <see cref="Apply"/> to run on the next Update tick regardless of change detection.</summary>
        protected void SetDirty()
        {
            _dirty = true;
        }

        void Update()
        {
            Rect safeArea = SafeAreaSimulator.Resolve();

            if (!_dirty
                && safeArea == _lastSafeArea
                && Screen.width == _lastScreenSize.x
                && Screen.height == _lastScreenSize.y
                && Screen.orientation == _lastOrientation)
            {
                return;
            }

            if (Panel == null)
                CachePanel();

            Apply(safeArea);

            _lastSafeArea = safeArea;
            _lastScreenSize.x = Screen.width;
            _lastScreenSize.y = Screen.height;
            _lastOrientation = Screen.orientation;
            _dirty = false;
        }

        void CachePanel()
        {
            Panel = GetComponent<RectTransform>();
        }

        /// <summary>Applies the given pixel-space safe area. Called only when something relevant changed.</summary>
        protected abstract void Apply(Rect safeArea);
    }
}
