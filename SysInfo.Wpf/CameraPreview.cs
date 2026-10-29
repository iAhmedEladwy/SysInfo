using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace CNSysInfo
{
    internal sealed class CameraDevice : IDisposable
    {
        public string Name { get; set; }
        internal IMoniker Moniker;
        public void Dispose() { CameraPreview.Release(Moniker); Moniker = null; }
    }

    internal sealed class CameraPreview : IDisposable
    {
        private object graphObject, builderObject;
        private ICameraFilter filter;
        private ICameraVideoWindow video;
        private ICameraMediaControl control;
        private ICameraMediaEvent events;
        private static readonly Guid VideoDevices = new Guid("860BB310-5D01-11D0-BD3B-00A0C911CE86");
        private static readonly Guid PreviewPin = new Guid("FB6C4282-0353-11D1-905F-0000C0CC16BA");
        private static readonly Guid VideoMedia = new Guid("73646976-0000-0010-8000-00AA00389B71");

        public static List<CameraDevice> Enumerate()
        {
            var result = new List<CameraDevice>();
            object enumeratorObject = null;
            IEnumMoniker enumerator = null;
            try
            {
                enumeratorObject = Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("62BE5D10-60EB-11D0-BD3B-00A0C911CE86")));
                var category = VideoDevices;
                int hr = ((ICameraCreateDevEnum)enumeratorObject).CreateClassEnumerator(ref category, out enumerator, 0);
                Check(hr);
                if (hr != 0 || enumerator == null) return result;
                var one = new IMoniker[1];
                while (enumerator.Next(1, one, IntPtr.Zero) == 0)
                {
                    IMoniker moniker = one[0]; one[0] = null;
                    object bagObject = null;
                    try
                    {
                        Guid bagId = typeof(ICameraPropertyBag).GUID;
                        moniker.BindToStorage(null, null, ref bagId, out bagObject);
                        object name;
                        Check(((ICameraPropertyBag)bagObject).Read("FriendlyName", out name, IntPtr.Zero));
                        result.Add(new CameraDevice { Name = Convert.ToString(name), Moniker = moniker });
                        moniker = null; // Device owns the reference until dialog close or refresh.
                    }
                    finally { Release(bagObject); Release(moniker); }
                }
                return result;
            }
            catch { foreach (var device in result) device.Dispose(); throw; }
            finally { Release(enumerator); Release(enumeratorObject); }
        }

        public void Start(CameraDevice device, IntPtr owner, int width, int height)
        {
            if (owner == IntPtr.Zero) throw new InvalidOperationException("Preview surface is unavailable.");
            try
            {
                graphObject = Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("E436EBB3-524F-11CE-9F53-0020AF0BA770")));
                builderObject = Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("BF87B6E1-8C27-11D0-B3F0-00AA003761C5")));
                var graph = (ICameraGraphBuilder)graphObject;
                var builder = (ICameraCaptureGraphBuilder)builderObject;
                Check(builder.SetFiltergraph(graph));
                object source; Guid filterId = typeof(ICameraFilter).GUID;
                device.Moniker.BindToObject(null, null, ref filterId, out source);
                filter = (ICameraFilter)source;
                Check(graph.AddFilter(filter, device.Name));
                Guid category = PreviewPin, media = VideoMedia;
                Check(builder.RenderStream(ref category, ref media, filter, null, null));
                video = (ICameraVideoWindow)graphObject;
                control = (ICameraMediaControl)graphObject;
                events = (ICameraMediaEvent)graphObject;
                Check(video.put_Owner(owner));
                Check(video.put_WindowStyle(0x40000000 | 0x02000000 | 0x04000000));
                Resize(width, height);
                Check(video.put_Visible(-1));
                Check(control.Run());
            }
            catch { Dispose(); throw; }
        }

        public bool HasDeviceError()
        {
            if (events == null) return false;
            for (int i = 0; i < 32; i++)
            {
                int code; IntPtr first, second;
                if (events.GetEvent(out code, out first, out second, 0) < 0) return false;
                try { if (code == 3 || code == 6 || code == 0x1f) return true; }
                finally { events.FreeEventParams(code, first, second); }
            }
            return false;
        }

        public void Resize(int width, int height)
        {
            if (video != null) Check(video.SetWindowPosition(0, 0, Math.Max(1, width), Math.Max(1, height)));
        }
        public void Dispose()
        {
            // Interface aliases share the graph RCW. Release the graph once, after detaching its window.
            try { if (control != null) control.Stop(); } catch (COMException) { }
            try { if (video != null) { video.put_Visible(0); video.put_Owner(IntPtr.Zero); } } catch (COMException) { }
            video = null; control = null; events = null;
            Release(builderObject); builderObject = null;
            Release(filter); filter = null;
            Release(graphObject); graphObject = null;
        }
        internal static void Release(object value) { if (value != null && Marshal.IsComObject(value)) Marshal.ReleaseComObject(value); }
        private static void Check(int hr) { if (hr < 0) Marshal.ThrowExceptionForHR(hr); }
    }

    [ComImport, Guid("29840822-5B84-11D0-BD3B-00A0C911CE86"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ICameraCreateDevEnum { [PreserveSig] int CreateClassEnumerator(ref Guid category, out IEnumMoniker enumerator, int flags); }
    [ComImport, Guid("55272A00-42CB-11CE-8135-00AA004BB851"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ICameraPropertyBag
    {
        [PreserveSig] int Read([MarshalAs(UnmanagedType.LPWStr)] string name, [MarshalAs(UnmanagedType.Struct)] out object value, IntPtr errorLog);
        [PreserveSig] int Write([MarshalAs(UnmanagedType.LPWStr)] string name, [MarshalAs(UnmanagedType.Struct)] ref object value);
    }
    [ComImport, Guid("56A86895-0AD4-11CE-B03A-0020AF0BA770"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ICameraFilter { }
    [ComImport, Guid("56A868A9-0AD4-11CE-B03A-0020AF0BA770"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ICameraGraphBuilder
    {
        [PreserveSig] int AddFilter(ICameraFilter filter, [MarshalAs(UnmanagedType.LPWStr)] string name);
    }
    [ComImport, Guid("93E5A4E0-2D50-11D2-ABFA-00A0C9C6E38D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ICameraCaptureGraphBuilder
    {
        [PreserveSig] int SetFiltergraph(ICameraGraphBuilder graph);
        [PreserveSig] int GetFiltergraph(out ICameraGraphBuilder graph);
        [PreserveSig] int SetOutputFileName(ref Guid type, [MarshalAs(UnmanagedType.LPWStr)] string file, out ICameraFilter filter, out IntPtr sink);
        [PreserveSig] int FindInterface(ref Guid category, ref Guid type, ICameraFilter source, ref Guid iid, out IntPtr value);
        [PreserveSig] int RenderStream(ref Guid category, ref Guid type, [MarshalAs(UnmanagedType.IUnknown)] object source, ICameraFilter compressor, ICameraFilter renderer);
    }
    [ComImport, Guid("56A868B1-0AD4-11CE-B03A-0020AF0BA770"), InterfaceType(ComInterfaceType.InterfaceIsDual)]
    internal interface ICameraMediaControl
    {
        [PreserveSig] int Run();
        [PreserveSig] int Pause();
        [PreserveSig] int Stop();
    }
    [ComImport, Guid("56A868B6-0AD4-11CE-B03A-0020AF0BA770"), InterfaceType(ComInterfaceType.InterfaceIsDual)]
    internal interface ICameraMediaEvent
    {
        [PreserveSig] int GetEventHandle(out IntPtr handle);
        [PreserveSig] int GetEvent(out int code, out IntPtr first, out IntPtr second, int milliseconds);
        [PreserveSig] int WaitForCompletion(int milliseconds, out int code);
        [PreserveSig] int CancelDefaultHandling(int code);
        [PreserveSig] int RestoreDefaultHandling(int code);
        [PreserveSig] int FreeEventParams(int code, IntPtr first, IntPtr second);
    }
    [ComImport, Guid("56A868B4-0AD4-11CE-B03A-0020AF0BA770"), InterfaceType(ComInterfaceType.InterfaceIsDual)]
    internal interface ICameraVideoWindow
    {
        [PreserveSig] int put_Caption([MarshalAs(UnmanagedType.BStr)] string caption);
        [PreserveSig] int get_Caption([MarshalAs(UnmanagedType.BStr)] out string caption);
        [PreserveSig] int put_WindowStyle(int value);
        [PreserveSig] int get_WindowStyle(out int value);
        [PreserveSig] int put_WindowStyleEx(int value);
        [PreserveSig] int get_WindowStyleEx(out int value);
        [PreserveSig] int put_AutoShow(int value);
        [PreserveSig] int get_AutoShow(out int value);
        [PreserveSig] int put_WindowState(int value);
        [PreserveSig] int get_WindowState(out int value);
        [PreserveSig] int put_BackgroundPalette(int value);
        [PreserveSig] int get_BackgroundPalette(out int value);
        [PreserveSig] int put_Visible(int value);
        [PreserveSig] int get_Visible(out int value);
        [PreserveSig] int put_Left(int value);
        [PreserveSig] int get_Left(out int value);
        [PreserveSig] int put_Width(int value);
        [PreserveSig] int get_Width(out int value);
        [PreserveSig] int put_Top(int value);
        [PreserveSig] int get_Top(out int value);
        [PreserveSig] int put_Height(int value);
        [PreserveSig] int get_Height(out int value);
        [PreserveSig] int put_Owner(IntPtr owner);
        [PreserveSig] int get_Owner(out IntPtr owner);
        [PreserveSig] int put_MessageDrain(IntPtr drain);
        [PreserveSig] int get_MessageDrain(out IntPtr drain);
        [PreserveSig] int get_BorderColor(out int color);
        [PreserveSig] int put_BorderColor(int color);
        [PreserveSig] int get_FullScreenMode(out int mode);
        [PreserveSig] int put_FullScreenMode(int mode);
        [PreserveSig] int SetWindowForeground(int focus);
        [PreserveSig] int NotifyOwnerMessage(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam);
        [PreserveSig] int SetWindowPosition(int left, int top, int width, int height);
    }
}
