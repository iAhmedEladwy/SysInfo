using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CNSysInfo.Tests
{
    [TestClass]
    public class DiagnosticTests
    {
        [TestMethod]
        public void FocusLossClearsHeldButKeepsTestedAndResetClearsBoth()
        {
            var state = new DiagnosticKeyState();
            state.Press(1); state.Press(2); state.Press(1); state.Release(2);
            Assert.IsTrue(state.IsHeld(1)); Assert.IsFalse(state.IsHeld(2)); Assert.IsTrue(state.IsTested(2));
            state.LoseFocus(); Assert.IsFalse(state.IsHeld(1)); Assert.IsTrue(state.IsTested(1));
            state.Reset(); Assert.IsFalse(state.IsTested(1)); Assert.IsFalse(state.IsTested(2));
        }
        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void ToneIsBoundedStereoPcmWithSilentOppositeChannelAndFadedEdges(bool left)
        {
            byte[] wave = StereoTestTone.CreateWave(left);
            Assert.AreEqual("RIFF", System.Text.Encoding.ASCII.GetString(wave, 0, 4));
            Assert.AreEqual("WAVE", System.Text.Encoding.ASCII.GetString(wave, 8, 4));
            Assert.AreEqual(2, BitConverter.ToInt16(wave, 22));
            Assert.AreEqual(16, BitConverter.ToInt16(wave, 34));
            Assert.AreEqual(StereoTestTone.SampleRate, BitConverter.ToInt32(wave, 24));
            Assert.AreEqual(wave.Length - 8, BitConverter.ToInt32(wave, 4));
            Assert.AreEqual(wave.Length - 44, BitConverter.ToInt32(wave, 40));
            Assert.AreEqual((int)(StereoTestTone.SampleRate * StereoTestTone.DurationSeconds) * 4 + 44, wave.Length);
            int peak = 0;
            for (int offset = 44; offset < wave.Length; offset += 4)
            {
                int active = BitConverter.ToInt16(wave, offset + (left ? 0 : 2));
                Assert.AreEqual(0, BitConverter.ToInt16(wave, offset + (left ? 2 : 0)));
                peak = Math.Max(peak, Math.Abs(active));
                Assert.IsTrue(Math.Abs(active) <= short.MaxValue * StereoTestTone.Amplitude);
            }
            Assert.IsTrue(peak > 1000);
            Assert.AreEqual(0, BitConverter.ToInt16(wave, 44 + (left ? 0 : 2)));
            Assert.AreEqual(0, BitConverter.ToInt16(wave, wave.Length - 4 + (left ? 0 : 2)));
        }
    }
}
