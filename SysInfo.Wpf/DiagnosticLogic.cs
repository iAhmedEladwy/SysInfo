using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CNSysInfo
{
    internal sealed class DiagnosticKeyState
    {
        private readonly HashSet<int> held = new HashSet<int>();
        private readonly HashSet<int> tested = new HashSet<int>();
        public void Press(int key) { held.Add(key); tested.Add(key); }
        public void Release(int key) { held.Remove(key); }
        public bool IsHeld(int key) => held.Contains(key);
        public bool IsTested(int key) => tested.Contains(key);
        public void LoseFocus() { held.Clear(); }
        public void Reset() { held.Clear(); tested.Clear(); }
    }

    internal enum SpeakerSample { ChannelTone, FrequencySweep, Music }
    internal enum SpeakerChannel { Left, Right, Both }

    internal static class StereoTestTone
    {
        public const int SampleRate = 44100;
        public const double DurationSeconds = 1.2;
        public const double Amplitude = 0.12;
        public static double Duration(SpeakerSample sample) => sample == SpeakerSample.ChannelTone ? DurationSeconds : sample == SpeakerSample.FrequencySweep ? 5 : 6;
        public static byte[] CreateWave(bool left) => CreateWave(SpeakerSample.ChannelTone, left ? SpeakerChannel.Left : SpeakerChannel.Right);

        public static byte[] CreateWave(SpeakerSample sample, SpeakerChannel channel)
        {
            if (!Enum.IsDefined(typeof(SpeakerSample), sample) || !Enum.IsDefined(typeof(SpeakerChannel), channel))
                throw new ArgumentOutOfRangeException();
            double seconds = Duration(sample);
            int frames = (int)(SampleRate * seconds);
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.ASCII))
            {
                writer.Write(Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + frames * 4);
                writer.Write(Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
                writer.Write((short)1); writer.Write((short)2); writer.Write(SampleRate);
                writer.Write(SampleRate * 4); writer.Write((short)4); writer.Write((short)16);
                writer.Write(Encoding.ASCII.GetBytes("data")); writer.Write(frames * 4);
                int fade = SampleRate / 50;
                double sweepRate = Math.Log(6000.0 / 180) / seconds;
                double[] notes = { 261.6256, 329.6276, 391.9954, 523.2511, 493.8833, 391.9954, 329.6276, 293.6648 };
                for (int i = 0; i < frames; i++)
                {
                    double t = (double)i / SampleRate;
                    double signal;
                    if (sample == SpeakerSample.FrequencySweep)
                        signal = Math.Sin(2 * Math.PI * 180 * (Math.Exp(sweepRate * t) - 1) / sweepRate);
                    else if (sample == SpeakerSample.Music)
                    {
                        const double noteDuration = 0.375;
                        int note = (int)(t / noteDuration);
                        double local = t - note * noteDuration;
                        double phase = 2 * Math.PI * notes[note % notes.Length] * local;
                        double envelope = Math.Min(1, local / 0.015) * Math.Pow(Math.Max(0, 1 - local / noteDuration), 0.7);
                        signal = 0.8 * envelope * (0.58 * Math.Sin(phase) + 0.24 * Math.Sin(2 * phase) + 0.12 * Math.Sin(3 * phase) + 0.06 * Math.Sin(4 * phase))
                            + 0.2 * Math.Sin(2 * Math.PI * 196 * t);
                    }
                    else signal = Math.Sin(2 * Math.PI * 440 * t);
                    double fadeEnvelope = Math.Min(1.0, Math.Min((double)i / fade, (double)(frames - 1 - i) / fade));
                    short value = (short)(short.MaxValue * Amplitude * fadeEnvelope * Math.Max(-1, Math.Min(1, signal)));
                    writer.Write(channel == SpeakerChannel.Right ? (short)0 : value);
                    writer.Write(channel == SpeakerChannel.Left ? (short)0 : value);
                }
                writer.Flush(); return stream.ToArray();
            }
        }
    }
}