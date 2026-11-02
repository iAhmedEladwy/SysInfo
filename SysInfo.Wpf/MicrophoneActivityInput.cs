using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace CNSysInfo
{
    internal sealed class MicrophoneDevice
    {
        public uint Id { get; set; }
        public string Name { get; set; }
    }
    internal sealed class MicrophoneActivityInput : IDisposable
    {
        private sealed class Buffer { public IntPtr Data, Header; public bool Prepared; public int Length; }
        private readonly List<Buffer> buffers = new List<Buffer>();
        private readonly Queue<Buffer> pending = new Queue<Buffer>();
        private IntPtr handle;
        private bool stopping;
        public bool ReleaseFailed { get; private set; }
        public int Peak { get; private set; }
        public static List<MicrophoneDevice> Devices()
        {
            var result = new List<MicrophoneDevice>();
            uint count = waveInGetNumDevs();
            if (count > 0) result.Add(new MicrophoneDevice { Id = uint.MaxValue, Name = "Default Windows microphone" });
            for (uint i = 0; i < count; i++)
                if (waveInGetDevCaps(new UIntPtr(i), out var caps, (uint)Marshal.SizeOf(typeof(WaveCaps))) == 0)
                    result.Add(new MicrophoneDevice { Id = i, Name = caps.Name });
            return result;
        }
        public MicrophoneActivityInput(uint device)
        {
            try
            {
                var format = new WaveFormat { Tag = 1, Channels = 1, SampleRate = MicrophoneLevels.SampleRate, BytesPerSecond = MicrophoneLevels.SampleRate * 2, BlockAlign = 2, Bits = 16 };
                Check(waveInOpen(out handle, device, ref format, IntPtr.Zero, IntPtr.Zero, 0));
                int length = MicrophoneLevels.SampleRate / 10 * 2;
                for (int i = 0; i < 4; i++)
                {
                    var buffer = new Buffer { Data = Marshal.AllocHGlobal(length), Header = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(WaveHeader))), Length = length };
                    buffers.Add(buffer);
                    Marshal.StructureToPtr(new WaveHeader { Data = buffer.Data, Length = (uint)length }, buffer.Header, false);
                    Check(waveInPrepareHeader(handle, buffer.Header, HeaderSize)); buffer.Prepared = true;
                    Check(waveInAddBuffer(handle, buffer.Header, HeaderSize)); pending.Enqueue(buffer);
                }
                Check(waveInStart(handle));
            }
            catch { Dispose(); throw; }
        }
        private static uint HeaderSize => (uint)Marshal.SizeOf(typeof(WaveHeader));
        public void Poll()
        {
            Peak = 0;
            // Poll completed headers in submission order. No native callbacks or global audio hooks.
            int remaining = pending.Count;
            while (remaining-- > 0 && pending.Count > 0 && !stopping)
            {
                var buffer = pending.Peek();
                var header = (WaveHeader)Marshal.PtrToStructure(buffer.Header, typeof(WaveHeader));
                if ((header.Flags & 1) == 0) break; // WHDR_DONE
                header = (WaveHeader)Marshal.PtrToStructure(buffer.Header, typeof(WaveHeader));
                pending.Dequeue();
                int length = (int)Math.Min(header.Recorded, header.Length);
                length -= length % 2;
                var data = new byte[length];
                if (length > 0)
                {
                    Marshal.Copy(header.Data, data, 0, length); Peak = Math.Max(Peak, MicrophoneLevels.Peak(data));
                    Array.Clear(data, 0, data.Length);
                }
                Marshal.Copy(new byte[buffer.Length], 0, buffer.Data, buffer.Length);
                Check(waveInAddBuffer(handle, buffer.Header, HeaderSize)); pending.Enqueue(buffer);
            }
        }

        private void StopNative()
        {
            if (handle == IntPtr.Zero) return;
            stopping = true;
            waveInStop(handle); waveInReset(handle);
            foreach (var buffer in buffers)
            {
                uint released = buffer.Prepared ? waveInUnprepareHeader(handle, buffer.Header, HeaderSize) : 0;
                if (released != 0) { waveInReset(handle); released = waveInUnprepareHeader(handle, buffer.Header, HeaderSize); }
                if (released == 0)
                {
                    Marshal.Copy(new byte[buffer.Length], 0, buffer.Data, buffer.Length);
                    Marshal.FreeHGlobal(buffer.Header); Marshal.FreeHGlobal(buffer.Data);
                }
                else ReleaseFailed = true; // Never free a buffer still owned by a driver.
            }
            buffers.Clear(); pending.Clear();
            if (waveInClose(handle) != 0) ReleaseFailed = true;
            handle = IntPtr.Zero;
        }
        public void Dispose()
        {
            StopNative();

        }
        private static void Check(uint result) { if (result != 0) throw new InvalidOperationException("Windows microphone error " + result + "."); }
        [StructLayout(LayoutKind.Sequential, Pack = 2)] private struct WaveFormat { public ushort Tag, Channels; public uint SampleRate, BytesPerSecond; public ushort BlockAlign, Bits, Extra; }
        [StructLayout(LayoutKind.Sequential)] private struct WaveHeader { public IntPtr Data; public uint Length, Recorded; public UIntPtr User; public uint Flags, Loops; public IntPtr Next; public UIntPtr Reserved; }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] private struct WaveCaps { public ushort Manufacturer, Product; public uint Version; [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string Name; public uint Formats; public ushort Channels, Reserved; }
        [DllImport("winmm.dll")] private static extern uint waveInGetNumDevs();
        [DllImport("winmm.dll", CharSet = CharSet.Unicode, EntryPoint = "waveInGetDevCapsW")] private static extern uint waveInGetDevCaps(UIntPtr device, out WaveCaps caps, uint size);
        [DllImport("winmm.dll")] private static extern uint waveInOpen(out IntPtr input, uint device, ref WaveFormat format, IntPtr callback, IntPtr instance, uint flags);
        [DllImport("winmm.dll")] private static extern uint waveInPrepareHeader(IntPtr input, IntPtr header, uint size);
        [DllImport("winmm.dll")] private static extern uint waveInAddBuffer(IntPtr input, IntPtr header, uint size);
        [DllImport("winmm.dll")] private static extern uint waveInStart(IntPtr input);
        [DllImport("winmm.dll")] private static extern uint waveInStop(IntPtr input);
        [DllImport("winmm.dll")] private static extern uint waveInReset(IntPtr input);
        [DllImport("winmm.dll")] private static extern uint waveInUnprepareHeader(IntPtr input, IntPtr header, uint size);
        [DllImport("winmm.dll")] private static extern uint waveInClose(IntPtr input);
    }
}
