using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CNSysInfo
{
    internal sealed class MouseDiagnosticState
    {
        private readonly Dictionary<int, int> clicks = new Dictionary<int, int>();
        private readonly HashSet<int> held = new HashSet<int>();
        public int Moves, DoubleClicks, WheelUp, WheelDown;
        public bool AnyHeld => held.Count != 0;
        public int Clicks(int button) => clicks.TryGetValue(button, out var value) ? value : 0;
        public void Press(int button, bool twice) { clicks[button] = Clicks(button) + 1; held.Add(button); if (twice && button == 0) DoubleClicks++; }
        public void Release(int button) => held.Remove(button);
        public void LoseFocus() => held.Clear();
        public void Scroll(int delta) { if (delta > 0) WheelUp += delta; else WheelDown -= delta; }
        public void Reset() { clicks.Clear(); held.Clear(); Moves = DoubleClicks = WheelUp = WheelDown = 0; }
    }
    internal static class UsbDiagnosticLogic
    {
        public static string[] Added(IEnumerable<string> before, IEnumerable<string> after) => after.Except(before, StringComparer.OrdinalIgnoreCase).ToArray();
    }
    internal static class MicrophoneWave
    {
        public const int SampleRate = 22050;
        public const int MaximumBytes = SampleRate * 2 * 5;
        public static byte[] Create(byte[] pcm)
        {
            if (pcm == null || pcm.Length == 0 || pcm.Length % 2 != 0 || pcm.Length > MaximumBytes) throw new ArgumentException("Invalid microphone sample.");
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream, Encoding.ASCII))
            {
                writer.Write(Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + pcm.Length); writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
                writer.Write(16); writer.Write((short)1); writer.Write((short)1); writer.Write(SampleRate); writer.Write(SampleRate * 2); writer.Write((short)2); writer.Write((short)16);
                writer.Write(Encoding.ASCII.GetBytes("data")); writer.Write(pcm.Length); writer.Write(pcm); writer.Flush(); return stream.ToArray();
            }
        }
        public static int Peak(byte[] pcm)
        {
            int peak = 0; for (int i = 0; i + 1 < pcm.Length; i += 2) peak = Math.Max(peak, Math.Abs((int)BitConverter.ToInt16(pcm, i)));
            return (int)Math.Round(peak * 100d / 32768);
        }
    }
}
