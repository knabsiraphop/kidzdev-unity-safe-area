using System.Collections.Generic;
using UnityEngine;

namespace KidzDev.Unity.SafeArea
{
    /// <summary>
    /// Single per-frame ticker for every <see cref="SafeAreaTracker"/>. Unity raises no "safe area changed"
    /// event, so the safe area must be polled — this collapses that poll into ONE place instead of an Update
    /// on each component. Trackers register while enabled and are notified only when the resolved safe area,
    /// screen size or orientation changes (or when an individual tracker is dirtied).
    ///
    /// The tick runs on <c>EditorApplication.update</c> in the editor (covering both edit and play mode) and
    /// on a single hidden pump object in a player build, so no per-component Update is needed.
    /// </summary>
    static class SafeAreaDriver
    {
        static readonly List<SafeAreaTracker> _trackers = new List<SafeAreaTracker>(8);

        static Rect _lastSafeArea;
        static Vector2Int _lastScreen;
        static ScreenOrientation _lastOrientation;
        static bool _hasState;
        static bool _editorHooked;

        public static void Register(SafeAreaTracker tracker)
        {
            if (tracker == null || _trackers.Contains(tracker))
                return;

            _trackers.Add(tracker);

#if UNITY_EDITOR
            if (!_editorHooked)
            {
                _editorHooked = true;
                // Fires in both edit mode and play mode while in the editor.
                UnityEditor.EditorApplication.update += Tick;
            }
#endif
        }

        public static void Unregister(SafeAreaTracker tracker)
        {
            _trackers.Remove(tracker);
        }

#if !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void CreatePlayerPump()
        {
            var go = new GameObject("[SafeAreaDriver]") { hideFlags = HideFlags.HideAndDontSave };
            Object.DontDestroyOnLoad(go);
            go.AddComponent<Pump>();
        }

        // The lone Update in the whole system: one call per frame for all trackers.
        sealed class Pump : MonoBehaviour
        {
            void Update() => Tick();
        }
#endif

        static void Tick()
        {
            if (_trackers.Count == 0)
                return;

            Rect safeArea = SafeAreaSimulator.Resolve();

            bool changed = !_hasState
                || safeArea != _lastSafeArea
                || Screen.width != _lastScreen.x
                || Screen.height != _lastScreen.y
                || Screen.orientation != _lastOrientation;

            if (changed)
            {
                _lastSafeArea = safeArea;
                _lastScreen.x = Screen.width;
                _lastScreen.y = Screen.height;
                _lastOrientation = Screen.orientation;
                _hasState = true;
            }

            // Iterate backwards so a tracker destroyed without OnDisable (e.g. domain reload) can be pruned.
            for (int i = _trackers.Count - 1; i >= 0; i--)
            {
                SafeAreaTracker tracker = _trackers[i];
                if (tracker == null)
                {
                    _trackers.RemoveAt(i);
                    continue;
                }

                tracker.DriverTick(safeArea, changed);
            }
        }
    }
}
