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
        [TestMethod] public void MicrophoneWaveContainsOnlyBoundedMonoPcm()
        {
            var pcm = new byte[] { 0, 0, 255, 127, 0, 128, 0, 0 };
            var wave = MicrophoneWave.Create(pcm);
            Assert.AreEqual("RIFF", System.Text.Encoding.ASCII.GetString(wave, 0, 4));
            Assert.AreEqual(1, BitConverter.ToInt16(wave, 22)); Assert.AreEqual(22050, BitConverter.ToInt32(wave, 24));
            Assert.AreEqual(16, BitConverter.ToInt16(wave, 34)); Assert.AreEqual(wave.Length - 8, BitConverter.ToInt32(wave, 4));
            Assert.AreEqual(pcm.Length, BitConverter.ToInt32(wave, 40)); CollectionAssert.AreEqual(pcm, wave.Skip(44).ToArray());
            Assert.AreEqual(100, MicrophoneWave.Peak(pcm)); Assert.AreEqual(0, MicrophoneWave.Peak(new byte[20]));
            Assert.ThrowsException<ArgumentException>(() => MicrophoneWave.Create(null));
            Assert.ThrowsException<ArgumentException>(() => MicrophoneWave.Create(new byte[1]));
            Assert.ThrowsException<ArgumentException>(() => MicrophoneWave.Create(new byte[MicrophoneWave.MaximumBytes + 2]));
        }
    }
}
