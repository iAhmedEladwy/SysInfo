using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace CNSysInfo
{
    public partial class MainWindow
    {
        private DispatcherTimer _powerTimer;
        private HwndSource _powerSource;
        private LivePowerState _livePower;
        private bool _batteryNeedsRefresh;

        [StructLayout(LayoutKind.Sequential)]
        private struct NativePowerStatus
        {
            public byte ACLineStatus, BatteryFlag, BatteryLifePercent, SystemStatusFlag;
            public uint BatteryLifeTime, BatteryFullLifeTime;
        }
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetSystemPowerStatus(out NativePowerStatus status);

        private void StartPowerMonitoring()
        {
            // Fast OS status only: no repeated WMI/SMART scan, event subscriptions or saved history.
            SourceInitialized += (_, __) =>
            {
                _powerSource = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
                _powerSource?.AddHook(PowerMessage);
            };
            _powerTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _powerTimer.Tick += PowerTick;
            Loaded += (_, __) => { UpdateLivePower(); _powerTimer.Start(); };
            Closed += (_, __) =>
            {
                _powerTimer.Stop();
                _powerTimer.Tick -= PowerTick;
                _powerSource?.RemoveHook(PowerMessage);
            };
        }

        private void PowerTick(object sender, EventArgs args) => UpdateLivePower();

        private IntPtr PowerMessage(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (message == 0x0218) Dispatcher.BeginInvoke(new Action(UpdateLivePower)); // WM_POWERBROADCAST, including resume.
            return IntPtr.Zero;
        }

        private static string BatteryCapacityDetails(BatteryInfo battery)
        {
            var details = new List<string>();
            if (battery?.DesignCapacity != null) details.Add("Design capacity: " + Presentation.BatteryCapacity(battery.DesignCapacity.Value));
            if (battery?.FullChargeCapacity != null) details.Add("Full charge capacity: " + Presentation.BatteryCapacity(battery.FullChargeCapacity.Value));
            details.Add("Battery health is full-charge capacity compared with design capacity. It updates with Refresh.");
            return string.Join(Environment.NewLine, details);
        }

        private void UpdateLivePower()
        {
            NativePowerStatus status;
            bool success = GetSystemPowerStatus(out status);
            var next = LivePowerState.FromNative(success, status.ACLineStatus, status.BatteryFlag, status.BatteryLifePercent);
            if (_livePower?.BatteryPresent != null && next.BatteryPresent != null && _livePower.BatteryPresent != next.BatteryPresent)
                _batteryNeedsRefresh = true;
            _livePower = next;
            txtBattery.Text = _livePower.Text;
            txtBattery.ToolTip = "Live Windows charge and power status · checked " + DateTime.Now.ToString("HH:mm:ss") +
                Environment.NewLine + "Updates every 2 seconds and on power changes. The capture time applies to the hardware snapshot." +
                Environment.NewLine + BatteryCapacityDetails(_ui?.System.BatteryInfo);
            System.Windows.Automation.AutomationProperties.SetName(txtBattery, "Live charge and power: " + _livePower.Text);
            // No battery means no battery-health claim. Never derive health from charge.
            if (_livePower.BatteryPresent == false)
                Bar(pbBattery, lblBattery, null, Presentation.Unknown, "N/A");
            else if (_batteryNeedsRefresh)
                Bar(pbBattery, lblBattery, null, Presentation.Unknown, "Refresh");
            pbBattery.ToolTip = _batteryNeedsRefresh ? "Battery presence changed. Use Refresh to read battery health and capacity again." : BatteryCapacityDetails(_ui?.System.BatteryInfo);
        }

        private void ShowDiagnostic(Window test)
        {
            test.Owner = this;
            test.Icon = Icon;
            test.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            test.ShowDialog();
        }
    }
}
