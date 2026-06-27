using UnityEngine;

namespace KidzDev.Unity.SafeArea
{
    /// <summary>
    /// Device whose safe area can be simulated in the editor. See <see cref="SafeAreaSimulator"/>.
    /// </summary>
    public enum SimDevice
    {
        /// <summary>No simulation — use the real device safe area (full screen in the editor).</summary>
        None,

        /// <summary>iPhone X / Xs / 11 Pro (identical safe areas).</summary>
        iPhoneX,

        /// <summary>iPhone Xs Max / XR / 11 / 11 Pro Max (identical safe areas).</summary>
        iPhoneXsMax,

        /// <summary>Google Pixel 3 XL, landscape-left.</summary>
        Pixel3XL_LandscapeLeft,

        /// <summary>Google Pixel 3 XL, landscape-right.</summary>
        Pixel3XL_LandscapeRight
    }

    /// <summary>
    /// Editor-only safe-area simulation. Lets you preview notch / home-bar layouts in the Game view
    /// without deploying to a device: set <see cref="Sim"/> and every <see cref="SafeArea"/> and
    /// <see cref="SafeAreaOutsideMask"/> in the scene reacts on the next frame. In a player build the
    /// simulation is ignored and the real <see cref="Screen.safeArea"/> is always used.
    /// </summary>
    public static class SafeAreaSimulator
    {
        /// <summary>
        /// Device to simulate in the editor. Defaults to <see cref="SimDevice.None"/> (use the real safe
        /// area). Can be changed at runtime — e.g. from an editor tool — to flip between devices live.
        /// </summary>
        public static SimDevice Sim = SimDevice.None;

        // Normalised [0..1] safe areas, indexed [0] = portrait, [1] = landscape. Values transcribed from
        // the canonical Crystal SafeArea reference implementation.

        static readonly Rect[] _iPhoneX =
        {
            new Rect(0f, 102f / 2436f, 1f, 2202f / 2436f),
            new Rect(132f / 2436f, 63f / 1125f, 2172f / 2436f, 1062f / 1125f)
        };

        static readonly Rect[] _iPhoneXsMax =
        {
            new Rect(0f, 102f / 2688f, 1f, 2454f / 2688f),
            new Rect(132f / 2688f, 63f / 1242f, 2424f / 2688f, 1179f / 1242f)
        };

        static readonly Rect[] _pixel3XlLeft =
        {
            new Rect(0f, 0f, 1f, 2789f / 2960f),
            new Rect(0f, 0f, 2789f / 2960f, 1f)
        };

        static readonly Rect[] _pixel3XlRight =
        {
            new Rect(0f, 0f, 1f, 2789f / 2960f),
            new Rect(171f / 2960f, 0f, 2789f / 2960f, 1f)
        };

        /// <summary>
        /// The safe area to use right now: the simulated area when running in the editor with a device
        /// selected, otherwise the device's real <see cref="Screen.safeArea"/>.
        /// </summary>
        public static Rect Resolve()
        {
            return Resolve(Screen.safeArea, Screen.width, Screen.height);
        }

        /// <summary>
        /// Pure resolver behind <see cref="Resolve()"/>, also used by tests. Returns
        /// <paramref name="deviceSafeArea"/> unless a simulation is active in the editor, in which case it
        /// returns the simulated pixel rect for the given screen size.
        /// </summary>
        public static Rect Resolve(Rect deviceSafeArea, int screenWidth, int screenHeight)
        {
            if (Application.isEditor && Sim != SimDevice.None)
                return Simulated(Sim, screenWidth, screenHeight);

            return deviceSafeArea;
        }

        /// <summary>
        /// The pixel-space safe area a device would report at the given screen size. Pure — no engine state.
        /// </summary>
        public static Rect Simulated(SimDevice device, int screenWidth, int screenHeight)
        {
            bool portrait = screenHeight > screenWidth;
            Rect n = Normalized(device, portrait);
            return new Rect(screenWidth * n.x, screenHeight * n.y, screenWidth * n.width, screenHeight * n.height);
        }

        /// <summary>
        /// The normalised [0..1] safe area for a device in the requested orientation. Pure — no engine state.
        /// </summary>
        public static Rect Normalized(SimDevice device, bool portrait)
        {
            int o = portrait ? 0 : 1;
            switch (device)
            {
                case SimDevice.iPhoneX: return _iPhoneX[o];
                case SimDevice.iPhoneXsMax: return _iPhoneXsMax[o];
                case SimDevice.Pixel3XL_LandscapeLeft: return _pixel3XlLeft[o];
                case SimDevice.Pixel3XL_LandscapeRight: return _pixel3XlRight[o];
                default: return new Rect(0f, 0f, 1f, 1f);
            }
        }
    }
}
