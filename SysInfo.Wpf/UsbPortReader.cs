using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace CNSysInfo
{
    internal sealed class UsbPortInfo
    {
        public string Port { get; set; }
        public string State { get; set; }
        public string DeviceName { get; set; } = "";
        public string UsbVersion { get; set; } = "Unavailable";
        public string LinkSpeed { get; set; } = "Unavailable";
        public string Protocols { get; set; } = "Unavailable";
        internal string DriverKey;
    }
    internal sealed class UsbPortScan
    {
        public readonly List<UsbPortInfo> Ports = new List<UsbPortInfo>();
        public readonly Dictionary<string, UsbPortInfo> ByDevice = new Dictionary<string, UsbPortInfo>(StringComparer.OrdinalIgnoreCase);
        public string Note = "";
    }
    internal static class UsbPortReader
    {
        // Read-only IOCTL definitions from the Windows SDK (usbioctl.h/usbiodef.h).
        // https://github.com/microsoft/Windows-driver-samples/tree/main/usb/usbview
        private const uint NodeInfo = (0x22u << 16) | (258u << 2), DriverName = (0x22u << 16) | (264u << 2), ConnectionEx = (0x22u << 16) | (274u << 2), ConnectionV2 = (0x22u << 16) | (279u << 2);
        private static readonly Guid HubInterface = new Guid("f18a0e88-c30c-11d0-8815-00a0c906bed8");
        private sealed class Node { public string Id, Parent, Driver, Name; }
        public static UsbPortScan Read()
        {
            var result = new UsbPortScan();
            try
            {
                var nodes = Nodes(); var byDriver = new Dictionary<string, Node>(StringComparer.OrdinalIgnoreCase);
                foreach (var node in nodes.Values) if (!string.IsNullOrEmpty(node.Driver)) byDriver[node.Driver] = node;
                var paths = HubPaths(); paths.Sort(StringComparer.OrdinalIgnoreCase); int hubIndex = 0, inaccessible = 0;
                foreach (string path in paths)
                {
                    hubIndex++;
                    // No elevation and no write access: ask only for metadata exposed to this user.
                    using (var hub = CreateFile(path, 0, 3, IntPtr.Zero, 3, 0, IntPtr.Zero))
                    {
                        if (hub.IsInvalid) { inaccessible++; continue; }
                        var info = new byte[256];
                        if (!Ioctl(hub, NodeInfo, info, out uint received) || received < 7) { inaccessible++; continue; }
                        int ports = info[6];
                        for (int port = 1; port <= ports; port++)
                        {
                            var link = new byte[4096]; Put(link, 0, (uint)port);
                            bool hasLink = Ioctl(hub, ConnectionEx, link, out uint size) && size >= 35;
                            int state = hasLink ? BitConverter.ToInt32(link, 31) : -1;
                            var v2 = new byte[16]; Put(v2, 0, (uint)port); Put(v2, 4, 16); Put(v2, 8, 7);
                            bool hasV2 = Ioctl(hub, ConnectionV2, v2, out uint sizeV2) && sizeV2 >= 16;
                            uint protocols = hasV2 ? BitConverter.ToUInt32(v2, 8) : 0, flags = hasV2 ? BitConverter.ToUInt32(v2, 12) : 0;
                            var row = new UsbPortInfo { Port = "Hub " + hubIndex + " · Port " + port, State = state == 1 ? "Connected" : state == 0 ? "Empty" : state < 0 ? "Unavailable" : "Windows error " + state,
                                Protocols = UsbDiagnosticLogic.PortProtocols(protocols), LinkSpeed = state == 1 ? UsbDiagnosticLogic.LinkSpeed(link[23], flags) : "—",
                                UsbVersion = state == 1 ? UsbDiagnosticLogic.UsbVersion(BitConverter.ToUInt16(link, 6)) : "—" };
                            if (state == 1)
                            {
                                row.DriverKey = Driver(hub, port);
                                if (!string.IsNullOrEmpty(row.DriverKey) && byDriver.TryGetValue(row.DriverKey, out var node))
                                { row.DeviceName = node.Name; result.ByDevice[node.Id] = row; }
                            }
                            result.Ports.Add(row);
                        }
                    }
                }
                foreach (var node in nodes.Values)
                {
                    if (!node.Id.StartsWith("USB", StringComparison.OrdinalIgnoreCase) || result.ByDevice.ContainsKey(node.Id)) continue;
                    string parent = node.Parent;
                    for (int depth = 0; depth < 8 && !string.IsNullOrEmpty(parent); depth++)
                    {
                        if (result.ByDevice.TryGetValue(parent, out var row)) { result.ByDevice[node.Id] = row; break; }
                        parent = nodes.TryGetValue(parent, out var ancestor) ? ancestor.Parent : null;
                    }
                }
                result.Note = inaccessible > 0 ? "Some hubs do not expose port metadata under current Windows permissions. Unavailable readings are not inferred." : "Port protocol support and negotiated device link speed are separate readings. Rates are signaling rates, not measured file-transfer speed.";
                if (paths.Count == 0) result.Note = "Windows did not expose USB hub interfaces. Device detection remains available; port readings are unavailable.";
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("USB port metadata unavailable: " + ex.GetType().Name); result.Note = "Port speed/generation metadata is unavailable under current Windows permissions or driver support."; }
            return result;
        }
        private static Dictionary<string, Node> Nodes()
        {
            var nodes = new Dictionary<string, Node>(StringComparer.OrdinalIgnoreCase);
            IntPtr set = SetupDiGetClassDevs(IntPtr.Zero, null, IntPtr.Zero, 6);
            if (set == new IntPtr(-1)) return nodes;
            try
            {
                for (uint i = 0; ; i++)
                {
                    var data = new DeviceData { Size = (uint)Marshal.SizeOf(typeof(DeviceData)) };
                    if (!SetupDiEnumDeviceInfo(set, i, ref data)) break;
                    var id = new StringBuilder(1024);
                    if (!SetupDiGetDeviceInstanceId(set, ref data, id, id.Capacity, out _)) continue;
                    var node = new Node { Id = id.ToString(), Driver = Property(set, ref data, 9), Name = Property(set, ref data, 12) };
                    if (string.IsNullOrWhiteSpace(node.Name)) node.Name = Property(set, ref data, 0);
                    if (CM_Get_Parent(out uint parent, data.DevInst, 0) == 0)
                    { var parentId = new StringBuilder(1024); if (CM_Get_Device_ID(parent, parentId, parentId.Capacity, 0) == 0) node.Parent = parentId.ToString(); }
                    nodes[node.Id] = node;
                }
            }
            finally { SetupDiDestroyDeviceInfoList(set); }
            return nodes;
        }
        private static string Property(IntPtr set, ref DeviceData data, uint property)
        {
            var bytes = new byte[4096];
            return SetupDiGetDeviceRegistryProperty(set, ref data, property, out _, bytes, bytes.Length, out uint required) && required > 1 && required <= bytes.Length
                ? Encoding.Unicode.GetString(bytes, 0, (int)required).TrimEnd('\0') : string.Empty;
        }
        private static List<string> HubPaths()
        {
            var paths = new List<string>(); Guid guid = HubInterface;
            IntPtr set = SetupDiGetClassDevs(ref guid, null, IntPtr.Zero, 18);
            if (set == new IntPtr(-1)) return paths;
            try
            {
                for (uint i = 0; ; i++)
                {
                    var data = new InterfaceData { Size = (uint)Marshal.SizeOf(typeof(InterfaceData)) };
                    if (!SetupDiEnumDeviceInterfaces(set, IntPtr.Zero, ref guid, i, ref data)) break;
                    SetupDiGetDeviceInterfaceDetail(set, ref data, IntPtr.Zero, 0, out uint size, IntPtr.Zero);
                    if (size < 6 || size > 65536) continue;
                    IntPtr detail = Marshal.AllocHGlobal((int)size);
                    try { Marshal.WriteInt32(detail, IntPtr.Size == 8 ? 8 : 6); if (SetupDiGetDeviceInterfaceDetail(set, ref data, detail, size, out _, IntPtr.Zero)) paths.Add(Marshal.PtrToStringUni(IntPtr.Add(detail, 4))); }
                    finally { Marshal.FreeHGlobal(detail); }
                }
            }
            finally { SetupDiDestroyDeviceInfoList(set); }
            return paths;
        }
        private static string Driver(SafeFileHandle hub, int port)
        {
            var data = new byte[4096]; Put(data, 0, (uint)port);
            if (!Ioctl(hub, DriverName, data, out uint received) || received < 10) return string.Empty;
            uint actual = BitConverter.ToUInt32(data, 4);
            return actual >= 10 && actual <= data.Length ? Encoding.Unicode.GetString(data, 8, (int)actual - 8).TrimEnd('\0') : string.Empty;
        }
        private static void Put(byte[] data, int offset, uint value) => Buffer.BlockCopy(BitConverter.GetBytes(value), 0, data, offset, 4);
        private static bool Ioctl(SafeFileHandle hub, uint code, byte[] data, out uint received) => DeviceIoControl(hub, code, data, data.Length, data, data.Length, out received, IntPtr.Zero);
        [StructLayout(LayoutKind.Sequential)] private struct DeviceData { public uint Size; public Guid Class; public uint DevInst; public UIntPtr Reserved; }
        [StructLayout(LayoutKind.Sequential)] private struct InterfaceData { public uint Size; public Guid Class; public uint Flags; public UIntPtr Reserved; }
        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern IntPtr SetupDiGetClassDevs(ref Guid guid, string enumerator, IntPtr parent, uint flags);
        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern IntPtr SetupDiGetClassDevs(IntPtr guid, string enumerator, IntPtr parent, uint flags);
        [DllImport("setupapi.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetupDiEnumDeviceInfo(IntPtr set, uint index, ref DeviceData data);
        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetupDiGetDeviceInstanceId(IntPtr set, ref DeviceData data, StringBuilder id, int length, out uint required);
        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetupDiGetDeviceRegistryProperty(IntPtr set, ref DeviceData data, uint property, out uint type, byte[] buffer, int size, out uint required);
        [DllImport("setupapi.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetupDiEnumDeviceInterfaces(IntPtr set, IntPtr device, ref Guid guid, uint index, ref InterfaceData data);
        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr set, ref InterfaceData data, IntPtr detail, uint size, out uint required, IntPtr device);
        [DllImport("setupapi.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetupDiDestroyDeviceInfoList(IntPtr set);
        [DllImport("cfgmgr32.dll")] private static extern uint CM_Get_Parent(out uint parent, uint instance, uint flags);
        [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode, EntryPoint = "CM_Get_Device_IDW")] private static extern uint CM_Get_Device_ID(uint instance, StringBuilder id, int length, uint flags);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern SafeFileHandle CreateFile(string path, uint access, uint share, IntPtr security, uint creation, uint flags, IntPtr template);
        [DllImport("kernel32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool DeviceIoControl(SafeFileHandle device, uint code, [In] byte[] input, int inputSize, [Out] byte[] output, int outputSize, out uint received, IntPtr overlapped);
    }
}
