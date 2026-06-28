using UnityEngine;

namespace KidzDev.Unity.SafeArea
{
    /// <summary>
    /// Shared base for safe-area components. Rather than each component polling in its own <c>Update</c>,
    /// every tracker registers with <see cref="SafeAreaDriver"/> while enabled; the driver polls once per
    /// frame for all trackers and calls <see cref="Apply"/> only when the resolved safe area, screen size or
    /// orientation changes — or when this tracker has a pending apply flagged via <see cref="RequestApply"/>
    /// (an inspector edit, or a ConformX/ConformY change). Subclasses decide what to do with the safe area.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public abstract class SafeAreaTracker : MonoBehaviour
    {
        /// <summary>The RectTransform this component is attached to. Cached on first use.</summary>
        protected RectTransform Panel { get; private set; }

        bool _pendingApply = true;

        protected virtual void Awake()
        {
            CachePanel();
        }

        protected virtual void OnEnable()
        {
            _pendingApply = true;
            SafeAreaDriver.Register(this);
        }

        protected virtual void OnDisable()
        {
            SafeAreaDriver.Unregister(this);
        }

        protected virtual void OnValidate()
        {
            // Inspector edits can fire in a phase where creating/destroying objects is illegal, so we only
            // raise the flag here and let the next poll cycle do the real work.
            _pendingApply = true;
        }

        /// <summary>Requests <see cref="Apply"/> to run on the next poll cycle regardless of screen changes.</summary>
        protected void RequestApply()
        {
            _pendingApply = true;
        }

        /// <summary>
        /// Called by <see cref="SafeAreaDriver"/> on each poll cycle. <paramref name="screenChanged"/> is true
        /// when the resolved safe area, screen size or orientation changed since the previous poll.
        /// </summary>
        internal void OnPoll(Rect safeArea, bool screenChanged)
        {
            if (!_pendingApply && !screenChanged)
                return;

            if (Panel == null)
                CachePanel();

            Apply(safeArea);
            _pendingApply = false;
        }

        void CachePanel()
        {
            Panel = GetComponent<RectTransform>();
        }

        /// <summary>Applies the given pixel-space safe area. Called only when something relevant changed.</summary>
        protected abstract void Apply(Rect safeArea);
    }
}
