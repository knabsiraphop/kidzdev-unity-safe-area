using UnityEngine;
using TMPro;

namespace KidzDev.Unity.SafeArea.Demo
{
    /// <summary>
    /// Drives the Safe Area demo: cycles the editor device simulator (auto-advancing on a timer, and on a
    /// button tap) and reports the resolved safe area, so you can watch the <see cref="SafeArea"/> content
    /// shrink and the <see cref="SafeAreaOutsideMask"/> bars appear live in the Game view.
    ///
    /// The simulation is editor-only — in a player build the real device safe area is always used.
    /// </summary>
    public class SafeAreaDemoController : MonoBehaviour
    {
        [SerializeField] TMP_Text statusLabel;
        [Tooltip("Seconds between automatic device changes. Set to 0 to advance only on tap.")]
        [SerializeField] float autoAdvanceSeconds = 2.5f;

        static readonly SimDevice[] Cycle =
        {
            SimDevice.None,
            SimDevice.iPhoneX,
            SimDevice.iPhoneXsMax,
            SimDevice.Pixel3XL_LandscapeLeft,
            SimDevice.Pixel3XL_LandscapeRight
        };

        int _index;
        float _timer;

        void OnEnable()
        {
            _index = 0;
            _timer = 0f;
            Apply();
        }

        void OnDisable()
        {
            // Don't leak the simulated device into the editor / other scenes.
            SafeAreaSimulator.Sim = SimDevice.None;
        }

        void Update()
        {
            if (autoAdvanceSeconds <= 0f)
                return;

            _timer += Time.unscaledDeltaTime;
            if (_timer >= autoAdvanceSeconds)
            {
                _timer = 0f;
                Next();
            }
        }

        /// <summary>Advance to the next simulated device (wired to the on-screen button).</summary>
        public void Next()
        {
            _index = (_index + 1) % Cycle.Length;
            _timer = 0f;
            Apply();
        }

        void Apply()
        {
            SafeAreaSimulator.Sim = Cycle[_index];

            if (statusLabel == null)
                return;

            Rect r = SafeAreaSimulator.Resolve();
            statusLabel.text =
                $"Simulated device\n<b>{Cycle[_index]}</b>\n\n" +
                $"Safe area:  x{r.x:0} y{r.y:0}  {r.width:0}×{r.height:0}\n" +
                $"Screen:  {Screen.width}×{Screen.height}\n\n" +
                "<size=70%>Auto-cycling — tap NEXT to advance</size>";
        }
    }
}
