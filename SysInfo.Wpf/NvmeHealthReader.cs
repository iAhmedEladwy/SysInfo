using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace CNSysInfo
{
    internal static class NvmeHealthReader
    {
        // Read-only STORAGE_QUERY_PROPERTY; no firmware, write, or self-test commands.
        public static StorageHealthInfo Read(int diskIndex)
        {
            if (diskIndex < 0) return null;
            using (var handle = CreateFile(@"\\.\PhysicalDrive" + diskIndex, 0, 3,
                IntPtr.Zero, 3, 0, IntPtr.Zero))
            {
                if (handle.IsInvalid) return null;
                var buffer = new byte[560]; // query header + protocol data + health log
                WriteUInt32(buffer, 0, 50); // StorageDeviceProtocolSpecificProperty
                WriteUInt32(buffer, 8, 3);  // ProtocolTypeNvme
                WriteUInt32(buffer, 12, 2); // NVMeDataTypeLogPage
                WriteUInt32(buffer, 16, 2); // NVME_LOG_PAGE_HEALTH_INFO
                WriteUInt32(buffer, 24, 40);
                WriteUInt32(buffer, 28, 512);
                if (!DeviceIoControl(handle, 0x002D1400, buffer, buffer.Length,
                    buffer, buffer.Length, out var returned, IntPtr.Zero)) return null;
                return NvmeHealthLogic.ParseResponse(buffer, returned, diskIndex.ToString());
            }
        }

        private static void WriteUInt32(byte[] buffer, int offset, uint value)
        {
            for (int i = 0; i < 4; i++) buffer[offset + i] = (byte)(value >> (8 * i));
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern SafeFileHandle CreateFile(string name, uint access, uint share,
            IntPtr security, uint creation, uint flags, IntPtr template);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeviceIoControl(SafeFileHandle handle, uint code,
            [In, Out] byte[] input, int inputSize, [In, Out] byte[] output, int outputSize,
            out int returned, IntPtr overlapped);
    }
}
