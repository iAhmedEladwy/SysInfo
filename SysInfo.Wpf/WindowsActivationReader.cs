using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace CNSysInfo
{
    // Read-only Windows Software Licensing Client API, independent of the WMI provider.
    // https://learn.microsoft.com/windows/win32/api/slpublic/nf-slpublic-slgetlicensingstatusinformation
    internal static class WindowsActivationReader
    {
        public static string Edition()
        {
            try
            {
                using (var machine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Default))
                using (var version = machine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion")) return Convert.ToString(version?.GetValue("EditionID"));
            }
            catch (Exception) { return string.Empty; }
        }
        public static ActivationResult Read(int timeoutMs)
        {
            IntPtr context = IntPtr.Zero, data = IntPtr.Zero;
            var watch = Stopwatch.StartNew();
            try
            {
                string edition = Edition();
                if (string.IsNullOrWhiteSpace(edition)) return ActivationResult.Unavailable(ActivationUnavailableReason.NoInstalledRows);
                if (SLOpen(out context) < 0) return ActivationResult.Unavailable(ActivationUnavailableReason.Error);
                var application = new Guid(ActivationLogic.WindowsApplicationId);
                if (SLGetLicensingStatusInformation(context, ref application, IntPtr.Zero, IntPtr.Zero, out uint count, out data) < 0)
                    return ActivationResult.Unavailable(ActivationUnavailableReason.Error);
                if (count == 0 || count > 4096 || data == IntPtr.Zero) return ActivationResult.Unavailable(ActivationUnavailableReason.NoInstalledRows);
                int stride = Marshal.SizeOf(typeof(LicenseStatus)); WindowsLicenseStatus? state = null;
                for (int i = 0; i < count; i++)
                {
                    if (watch.ElapsedMilliseconds >= timeoutMs) return ActivationResult.Unavailable(ActivationUnavailableReason.Timeout);
                    var row = (LicenseStatus)Marshal.PtrToStructure(IntPtr.Add(data, i * stride), typeof(LicenseStatus));
                    if (row.State <= 0 || row.State > 3) continue; // Native catalog entries at state 0 are not necessarily installed.
                    string name = Name(context, row.Sku);
                    if (!ActivationLogic.MatchesEdition(name, edition)) continue; // Never use Office, add-ons or another Windows edition.
                    if (row.State == 1) return new ActivationResult(WindowsLicenseStatus.Licensed, ActivationUnavailableReason.None, DateTime.Now);
                    state = row.State == 2 ? WindowsLicenseStatus.GracePeriod : WindowsLicenseStatus.Notification;
                }
                return state.HasValue ? new ActivationResult(state, ActivationUnavailableReason.None, DateTime.Now) : ActivationResult.Unavailable(ActivationUnavailableReason.NoInstalledRows);
            }
            catch (Exception ex) { Debug.WriteLine("Windows licensing API unavailable: " + ex.GetType().Name); return ActivationResult.Unavailable(ActivationUnavailableReason.Error); }
            finally { if (data != IntPtr.Zero) LocalFree(data); if (context != IntPtr.Zero) SLClose(context); }
        }
        private static string Name(IntPtr context, Guid sku)
        {
            IntPtr value = IntPtr.Zero;
            try
            {
                int result = SLGetProductSkuInformation(context, ref sku, "Name", out uint type, out uint length, out value);
                return result >= 0 && type == 1 && length > 0 && length <= 8192 && value != IntPtr.Zero ? Marshal.PtrToStringUni(value, (int)length / 2).TrimEnd('\0') : string.Empty;
            }
            finally { if (value != IntPtr.Zero) LocalFree(value); }
        }
        [StructLayout(LayoutKind.Sequential)] private struct LicenseStatus { public Guid Sku; public int State; public uint GraceMinutes, TotalGraceDays; public int Reason; public ulong Expiration; }
        [DllImport("slc.dll")] private static extern int SLOpen(out IntPtr context);
        [DllImport("slc.dll")] private static extern int SLClose(IntPtr context);
        [DllImport("slc.dll")] private static extern int SLGetLicensingStatusInformation(IntPtr context, ref Guid app, IntPtr sku, IntPtr right, out uint count, out IntPtr status);
        [DllImport("slc.dll", CharSet = CharSet.Unicode)] private static extern int SLGetProductSkuInformation(IntPtr context, ref Guid sku, string key, out uint type, out uint length, out IntPtr value);
        [DllImport("kernel32.dll")] private static extern IntPtr LocalFree(IntPtr value);
    }
}
