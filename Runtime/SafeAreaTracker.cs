using UnityEngine;

namespace KidzDev.Unity.SafeArea
{
    /// <summary>
    /// Shared base for safe-area components. Rather than each component polling in its own <c>Update</c>,
    /// every tracker registers with <see cref="SafeAreaDriver"/> while enabled; the driver polls once per
    /// frame for all trackers and calls <see cref="Apply"/> only when the resolved safe area, screen size or
    /// orientation changes — or when this tracker is individually flagged via <see cref="SetDirty"/> (an
    /// inspector edit, or a ConformX/ConformY change). Subclasses decide what to do with the safe area.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public abstract class SafeAreaTracker : MonoBehaviour
    {
        /// <summary>The RectTransform this component is attached to. Cached on first use.</summary>
        protected RectTransform Panel { get; private set; }

        bool _dirty = true;

        protected virtual void Awake()
        {
            CachePanel();
        }

        protected virtual void OnEnable()
        {
            _dirty = true;                  // apply on the first driver tick after enable
            SafeAreaDriver.Register(this);
        }

        protected virtual void OnDisable()
        {
            SafeAreaDriver.Unregister(this);
        }

        protected virtual void OnValidate()
        {
            // Inspector edits can fire in a phase where creating/destroying objects is illegal, so we only
            // raise the flag here and let the next driver tick do the real work.
            _dirty = true;
        }

        /// <summary>Forces <see cref="Apply"/> to run on the next driver tick regardless of change detection.</summary>
        protected void SetDirty()
        {
            _dirty = true;
        }

        /// <summary>
        /// Called by <see cref="SafeAreaDriver"/> once per frame. <paramref name="globalChanged"/> is true
        /// when the resolved safe area, screen size or orientation changed since the previous tick.
        /// </summary>
        internal void DriverTick(Rect safeArea, bool globalChanged)
        {
            if (!_dirty && !globalChanged)
                return;

            if (Panel == null)
                CachePanel();

            Apply(safeArea);
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
