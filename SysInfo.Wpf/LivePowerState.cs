using System;

namespace CNSysInfo
{
    // OS power state is separate from firmware capacity/health and the captured hardware snapshot.
    internal sealed class LivePowerState
    {
        public bool? BatteryPresent { get; private set; }
        public bool? AcConnected { get; private set; }
        public int? Charge { get; private set; }
        public bool? Charging { get; private set; }
        public string Text { get; private set; }

        public static LivePowerState FromNative(bool success, byte ac, byte flags, byte percentage)
        {
            var result = new LivePowerState();
            if (!success) { result.Text = "Live power unavailable"; return result; }
            result.AcConnected = ac == 0 ? false : ac == 1 ? true : (bool?)null;
            result.BatteryPresent = flags == 255 ? (bool?)null : (flags & 128) != 0 ? false : true;
            if (result.BatteryPresent == false)
            {
                result.Text = "No battery detected · " + (result.AcConnected == true ? "AC power" : "Power source unknown");
                return result;
            }
            result.Charge = percentage <= 100 ? (int?)percentage : null;
            result.Charging = flags == 255 ? (bool?)null : (flags & 8) != 0;
            string status;
            if (result.BatteryPresent == null) status = result.AcConnected == true ? "AC power · Battery status unknown" : "Power status unknown";
            else if (result.Charging == true) status = result.AcConnected == false ? "On battery · Charging status uncertain" : "Charging";
            else if (result.AcConnected == true) status = result.Charge == 100 ? "Fully charged · Plugged in" : "Plugged in · Not charging";
            else status = result.AcConnected == false ? "On battery" : "Power source unknown";
            result.Text = BatteryMapper.ChargeAndPowerText(result.Charge, status);
            return result;
        }
    }
}
