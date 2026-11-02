using System;
using System.Collections.Generic;
using System.Linq;

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
        public static string LinkSpeed(byte speed, uint flags)
        {
            if ((flags & 4) != 0) return "10 Gbps or higher · SuperSpeedPlus";
            if ((flags & 1) != 0 || speed == 3) return "5 Gbps · SuperSpeed";
            switch (speed) { case 0: return "1.5 Mbps · Low speed"; case 1: return "12 Mbps · Full speed"; case 2: return "480 Mbps · High speed"; default: return "Unavailable"; }
        }
        public static string UsbVersion(ushort bcd)
        {
            int a = (bcd >> 12) & 15, b = (bcd >> 8) & 15, c = (bcd >> 4) & 15, d = bcd & 15;
            if (bcd == 0 || a > 9 || b > 9 || c > 9 || d > 9) return "Unavailable";
            return "USB " + (a * 10 + b) + "." + c + (d == 0 ? "" : d.ToString());
        }
        public static string PortProtocols(uint flags)
        {
            var protocols = new List<string>(); if ((flags & 1) != 0) protocols.Add("USB 1.x"); if ((flags & 2) != 0) protocols.Add("USB 2.0"); if ((flags & 4) != 0) protocols.Add("USB 3.x");
            return protocols.Count == 0 ? "Unavailable" : string.Join(" / ", protocols);
        }
        public static string[] Added(IEnumerable<string> before, IEnumerable<string> after) => after.Except(before, StringComparer.OrdinalIgnoreCase).ToArray();
    }
    internal static class MicrophoneLevels
    {
        public const int SampleRate = 22050;
        public static int Peak(byte[] pcm)
        {
            int peak = 0; for (int i = 0; i + 1 < pcm.Length; i += 2) peak = Math.Max(peak, Math.Abs((int)BitConverter.ToInt16(pcm, i)));
            return (int)Math.Round(peak * 100d / 32768);
        }
    }
}
