using System.Collections.Generic;
using UnityEngine;

namespace KidzDev.Unity.SafeArea
{
    /// <summary>
    /// Single per-frame poller for every <see cref="SafeAreaTracker"/>. Unity raises no "safe area changed"
    /// event, so the safe area must be polled — this collapses that poll into ONE place instead of an Update
    /// on each component. Trackers register while enabled and are notified only when the resolved safe area,
    /// screen size or orientation changes (or when an individual tracker has a pending apply).
    ///
    /// The poll runs on <c>EditorApplication.update</c> in the editor (covering both edit and play mode) and
    /// on a single hidden pump object in a player build, so no per-component Update is needed.
    /// </summary>
    static class SafeAreaDriver
    {
        static readonly List<SafeAreaTracker> _trackers = new List<SafeAreaTracker>(8);

        static Rect _lastKnownSafeArea;
        static Vector2Int _lastKnownScreen;
        static ScreenOrientation _lastKnownOrientation;
        static bool _initialized;
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
                UnityEditor.EditorApplication.update += Poll;
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

        sealed class Pump : MonoBehaviour
        {
            void Update() => Poll();
        }
#endif

        static void Poll()
        {
            if (_trackers.Count == 0)
                return;

            Rect safeArea = SafeAreaSimulator.Resolve();

            bool screenChanged = !_initialized
                || safeArea != _lastKnownSafeArea
                || Screen.width  != _lastKnownScreen.x
                || Screen.height != _lastKnownScreen.y
                || Screen.orientation != _lastKnownOrientation;

            if (screenChanged)
            {
                _lastKnownSafeArea      = safeArea;
                _lastKnownScreen.x      = Screen.width;
                _lastKnownScreen.y      = Screen.height;
                _lastKnownOrientation   = Screen.orientation;
                _initialized            = true;
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

                tracker.OnPoll(safeArea, screenChanged);
            }
        }
    }
}
