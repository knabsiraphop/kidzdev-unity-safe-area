using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace KidzDev.Unity.SafeArea.Tests.Runtime
{
    /// <summary>
    /// PlayMode lifecycle coverage for the driver-backed trackers: apply-on-enable, re-apply when a tracker
    /// is dirtied, the ZeroOffsets option, mask bar generation, and that destroying a tracker leaves the
    /// driver healthy. These assert behaviour that is independent of the host's actual safe area, so they are
    /// deterministic on any device / editor Game-view mode.
    /// </summary>
    public class SafeAreaPlayTests
    {
        readonly List<GameObject> _spawned = new List<GameObject>();

        const float Tol = 0.01f;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (int i = 0; i < _spawned.Count; i++)
                if (_spawned[i] != null)
                    Object.Destroy(_spawned[i]);
            _spawned.Clear();
            SafeAreaSimulator.Sim = SimDevice.None;
            yield return null;
        }

        RectTransform NewRect(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            _spawned.Add(go);
            return go.GetComponent<RectTransform>();
        }

        static IEnumerator SettleFrames()
        {
            // Give the driver a couple of ticks to run.
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator SafeArea_AppliesResolvedSafeArea_OnEnable()
        {
            RectTransform rt = NewRect("safe");
            rt.anchorMin = new Vector2(0.33f, 0.33f);   // sentinel that must be overwritten
            rt.anchorMax = new Vector2(0.33f, 0.33f);

            rt.gameObject.AddComponent<SafeArea>();      // OnEnable -> registers with the driver
            yield return SettleFrames();

            Rect r = SafeAreaSimulator.Resolve();
            Vector2 expMin = new Vector2(r.x / Screen.width, r.y / Screen.height);
            Vector2 expMax = new Vector2((r.x + r.width) / Screen.width, (r.y + r.height) / Screen.height);

            Assert.AreEqual(expMin.x, rt.anchorMin.x, Tol, "anchorMin.x");
            Assert.AreEqual(expMin.y, rt.anchorMin.y, Tol, "anchorMin.y");
            Assert.AreEqual(expMax.x, rt.anchorMax.x, Tol, "anchorMax.x");
            Assert.AreEqual(expMax.y, rt.anchorMax.y, Tol, "anchorMax.y");
        }

        [UnityTest]
        public IEnumerator SafeArea_ReAppliesWhenConformChanges()
        {
            RectTransform rt = NewRect("safe");
            var sa = rt.gameObject.AddComponent<SafeArea>();
            yield return SettleFrames();

            // Disabling the X axis must force the horizontal anchors to full screen, whatever the safe area is.
            sa.ConformX = false;
            yield return SettleFrames();

            Assert.AreEqual(0f, rt.anchorMin.x, Tol, "anchorMin.x should be full-left when ConformX is off");
            Assert.AreEqual(1f, rt.anchorMax.x, Tol, "anchorMax.x should be full-right when ConformX is off");
        }

        [UnityTest]
        public IEnumerator SafeArea_ZeroOffsets_ClearsOffsets()
        {
            RectTransform rt = NewRect("safe");
            var sa = rt.gameObject.AddComponent<SafeArea>();
            yield return SettleFrames();

            rt.offsetMin = new Vector2(25f, 25f);
            rt.offsetMax = new Vector2(-25f, -25f);
            sa.ZeroOffsets = true;                        // dirties -> re-apply zeroes the offsets
            yield return SettleFrames();

            Assert.AreEqual(Vector2.zero, rt.offsetMin, "offsetMin should be zeroed");
            Assert.AreEqual(Vector2.zero, rt.offsetMax, "offsetMax should be zeroed");
        }

        [UnityTest]
        public IEnumerator OutsideMask_GeneratesFourBars_WhenBothAxesConform()
        {
            RectTransform rt = NewRect("mask");
            rt.gameObject.AddComponent<SafeAreaOutsideMask>();
            yield return SettleFrames();

            Assert.AreEqual(4, rt.childCount, "expected one bar per edge (Left/Right/Bottom/Top)");
        }

        [UnityTest]
        public IEnumerator Driver_SurvivesDestroyedTracker()
        {
            // A tracker that gets destroyed must not break the shared driver for the others.
            RectTransform a = NewRect("a");
            GameObject aGo = a.gameObject;
            aGo.AddComponent<SafeArea>();
            yield return SettleFrames();

            _spawned.Remove(aGo);
            Object.DestroyImmediate(aGo);
            yield return SettleFrames();                  // driver ticks and must prune the null entry

            RectTransform b = NewRect("b");
            b.anchorMin = new Vector2(0.33f, 0.33f);
            b.anchorMax = new Vector2(0.33f, 0.33f);
            b.gameObject.AddComponent<SafeArea>();
            yield return SettleFrames();

            Assert.AreNotEqual(0.33f, b.anchorMin.x, "a new tracker still gets driven after one was destroyed");
        }
    }
}
