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
        [DataTestMethod]
        [DataRow(0, 0)]
        [DataRow(0, 1)]
        [DataRow(0, 2)]
        [DataRow(1, 0)]
        [DataRow(1, 1)]
        [DataRow(1, 2)]
        [DataRow(2, 0)]
        [DataRow(2, 1)]
        [DataRow(2, 2)]
        public void QualitySamplesAreBoundedFadedAndChannelIsolated(int sampleValue, int channelValue)
        {
            var sample = (SpeakerSample)sampleValue; var channel = (SpeakerChannel)channelValue;
            var wave = StereoTestTone.CreateWave(sample, channel);
            Assert.AreEqual(44 + (int)(44100 * StereoTestTone.Duration(sample)) * 4, wave.Length);
            Assert.AreEqual(44100, BitConverter.ToInt32(wave, 24));
            Assert.AreEqual(2, BitConverter.ToInt16(wave, 22));
            int peak = 0;
            for (int offset = 44; offset < wave.Length; offset += 4)
            {
                int left = BitConverter.ToInt16(wave, offset), right = BitConverter.ToInt16(wave, offset + 2);
                if (channel == SpeakerChannel.Right) Assert.AreEqual(0, left);
                if (channel == SpeakerChannel.Left) Assert.AreEqual(0, right);
                if (channel == SpeakerChannel.Both) Assert.AreEqual(left, right);
                peak = Math.Max(peak, Math.Max(Math.Abs(left), Math.Abs(right)));
            }
            Assert.IsTrue(peak > 1000 && peak <= short.MaxValue * 0.12);
            Assert.AreEqual(0, BitConverter.ToInt32(wave, 44));
            Assert.AreEqual(0, BitConverter.ToInt32(wave, wave.Length - 4));
        }

        [TestMethod]
        public void SweepActuallyMovesFromLowToHighPitch()
        {
            var wave = StereoTestTone.CreateWave(SpeakerSample.FrequencySweep, SpeakerChannel.Left);
            int low = RisingCrossings(wave, 0.1, 0.2), high = RisingCrossings(wave, 4.7, 4.8);
            Assert.IsTrue(low >= 15 && low <= 30);
            Assert.IsTrue(high > low * 10 && high < 650);
        }

        private static int RisingCrossings(byte[] wave, double from, double to)
        {
            int count = 0;
            for (int i = (int)(from * 44100) + 1; i < (int)(to * 44100); i++)
                if (BitConverter.ToInt16(wave, 44 + 4 * (i - 1)) <= 0 && BitConverter.ToInt16(wave, 44 + 4 * i) > 0) count++;
            return count;
        }    }
}
