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

    internal static class StereoTestTone
    {
        public const int SampleRate = 44100;
        public const double DurationSeconds = 1.2;
        public const double Amplitude = 0.12;
        public static byte[] CreateWave(bool left)
        {
            int frames = (int)(SampleRate * DurationSeconds);
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.ASCII))
            {
                writer.Write(Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + frames * 4);
                writer.Write(Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
                writer.Write((short)1); writer.Write((short)2); writer.Write(SampleRate);
                writer.Write(SampleRate * 4); writer.Write((short)4); writer.Write((short)16);
                writer.Write(Encoding.ASCII.GetBytes("data")); writer.Write(frames * 4);
                int fade = SampleRate / 50;
                for (int i = 0; i < frames; i++)
                {
                    double envelope = Math.Min(1.0, Math.Min((double)i / fade, (double)(frames - 1 - i) / fade));
                    short value = (short)(short.MaxValue * Amplitude * envelope * Math.Sin(2 * Math.PI * 440 * i / SampleRate));
                    writer.Write(left ? value : (short)0);
                    writer.Write(left ? (short)0 : value);
                }
                writer.Flush(); return stream.ToArray();
            }
        }
    }
}
