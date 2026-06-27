using NUnit.Framework;
using UnityEngine;

namespace KidzDev.Unity.SafeArea.Tests
{
    public class SafeAreaSimulatorTests
    {
        [TearDown]
        public void ResetSim()
        {
            SafeAreaSimulator.Sim = SimDevice.None;
        }

        [Test]
        public void Normalized_None_IsFullScreen()
        {
            Assert.AreEqual(new Rect(0f, 0f, 1f, 1f), SafeAreaSimulator.Normalized(SimDevice.None, portrait: true));
            Assert.AreEqual(new Rect(0f, 0f, 1f, 1f), SafeAreaSimulator.Normalized(SimDevice.None, portrait: false));
        }

        [Test]
        public void Simulated_iPhoneX_Portrait_InsetsTopAndBottom()
        {
            Rect r = SafeAreaSimulator.Simulated(SimDevice.iPhoneX, 1125, 2436);

            Assert.AreEqual(0f, r.x, 0.5f);
            Assert.AreEqual(102f, r.y, 0.5f);
            Assert.AreEqual(1125f, r.width, 0.5f);
            Assert.AreEqual(2202f, r.height, 0.5f);
        }

        [Test]
        public void Simulated_iPhoneX_Landscape_InsetsLeftAndRight()
        {
            Rect r = SafeAreaSimulator.Simulated(SimDevice.iPhoneX, 2436, 1125);

            Assert.AreEqual(132f, r.x, 0.5f);
            Assert.AreEqual(63f, r.y, 0.5f);
            Assert.AreEqual(2172f, r.width, 0.5f);
            Assert.AreEqual(1062f, r.height, 0.5f);
        }

        [Test]
        public void Simulated_OrientationFollowsScreenAspect()
        {
            // Portrait when height > width, landscape otherwise.
            Rect portrait = SafeAreaSimulator.Simulated(SimDevice.iPhoneXsMax, 1242, 2688);
            Rect landscape = SafeAreaSimulator.Simulated(SimDevice.iPhoneXsMax, 2688, 1242);

            Assert.AreEqual(0f, portrait.x, 0.5f, "Portrait has no left inset on iPhone Xs Max.");
            Assert.Greater(landscape.x, 0f, "Landscape insets the left edge.");
        }

        [Test]
        public void Resolve_NoSim_ReturnsDeviceSafeArea()
        {
            SafeAreaSimulator.Sim = SimDevice.None;
            Rect device = new Rect(7f, 11f, 13f, 17f);

            Assert.AreEqual(device, SafeAreaSimulator.Resolve(device, 1080, 1920));
        }

        [Test]
        public void Resolve_WithSimInEditor_OverridesDeviceSafeArea()
        {
            // Tests run in the editor, so Application.isEditor is true and the simulation takes effect.
            SafeAreaSimulator.Sim = SimDevice.iPhoneX;
            Rect device = new Rect(0f, 0f, 1080f, 1920f);

            Rect resolved = SafeAreaSimulator.Resolve(device, 1125, 2436);

            Assert.AreNotEqual(device, resolved);
            Assert.AreEqual(102f, resolved.y, 0.5f);
        }
    }
}
