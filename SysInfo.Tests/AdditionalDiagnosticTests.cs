using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CNSysInfo.Tests
{
    [TestClass]
    public class AdditionalDiagnosticTests
    {
        [TestMethod] public void MouseEventsKeepCountsAndReleaseHeldState()
        {
            var state = new MouseDiagnosticState();
            state.Press(0, false); state.Press(0, true); state.Press(2, false); state.Scroll(120); state.Scroll(-60);
            Assert.AreEqual(2, state.Clicks(0)); Assert.AreEqual(1, state.Clicks(2)); Assert.AreEqual(1, state.DoubleClicks);
            Assert.AreEqual(120, state.WheelUp); Assert.AreEqual(60, state.WheelDown); Assert.IsTrue(state.AnyHeld);
            state.LoseFocus(); Assert.IsFalse(state.AnyHeld); Assert.AreEqual(2, state.Clicks(0));
            state.Reset(); Assert.AreEqual(0, state.Clicks(0)); Assert.AreEqual(0, state.DoubleClicks); Assert.AreEqual(0, state.WheelUp);
        }
        [TestMethod] public void UsbDiffTreatsCaseAsSameAndReportsOnlyActualChanges()
        {
            string[] before = { "USB\\A", "USB\\B" }, after = { "usb\\a", "USB\\C", "USB\\C" };
            CollectionAssert.AreEqual(new[] { "USB\\C" }, UsbDiagnosticLogic.Added(before, after));
            CollectionAssert.AreEqual(new[] { "USB\\B" }, UsbDiagnosticLogic.Added(after, before));
            Assert.AreEqual(0, UsbDiagnosticLogic.Added(before, before).Length);
        }
        [TestMethod] public void MicLevelsHandleSilenceAndSignedPeaksWithoutRecording()
        {
            Assert.AreEqual(0, MicrophoneLevels.Peak(new byte[20]));
            Assert.AreEqual(100, MicrophoneLevels.Peak(new byte[] { 0, 128 }));
            Assert.AreEqual(50, MicrophoneLevels.Peak(new byte[] { 0, 64 }));
            Assert.AreEqual(0, MicrophoneLevels.Peak(new byte[0]));
        }
        [TestMethod] public void UsbLinkRateDoesNotConfuseCapabilityWithOperatingSpeed()
        {
            Assert.AreEqual("480 Mbps · High speed", UsbDiagnosticLogic.LinkSpeed(2, 2)); // SuperSpeed-capable, not operating at it.
            Assert.AreEqual("5 Gbps · SuperSpeed", UsbDiagnosticLogic.LinkSpeed(2, 1)); // v2 overrides legacy speed reporting.
            Assert.AreEqual("10 Gbps or higher · SuperSpeedPlus", UsbDiagnosticLogic.LinkSpeed(3, 4));
            Assert.AreEqual("Unavailable", UsbDiagnosticLogic.LinkSpeed(255, 0));
            Assert.AreEqual("USB 3.2", UsbDiagnosticLogic.UsbVersion(0x0320));
            Assert.AreEqual("USB 2.0", UsbDiagnosticLogic.UsbVersion(0x0200));
            Assert.AreEqual("Unavailable", UsbDiagnosticLogic.UsbVersion(0x03ff));
            Assert.AreEqual("USB 1.x / USB 2.0 / USB 3.x", UsbDiagnosticLogic.PortProtocols(7));
            Assert.AreEqual("Unavailable", UsbDiagnosticLogic.PortProtocols(0));
        }
    }
}