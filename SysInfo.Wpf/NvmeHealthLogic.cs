using System;

namespace CNSysInfo
{
    internal static class NvmeHealthLogic
    {
        // STORAGE_PROTOCOL_DATA_DESCRIPTOR wraps the standard 512-byte NVMe
        // health log. Validate offsets before trusting any firmware-supplied data.
        public static StorageHealthInfo ParseResponse(byte[] data, int returned, string diskIndex)
        {
            if (data == null || returned < 48 || returned > data.Length) return null;
            if (BitConverter.ToUInt32(data, 0) != 48 || BitConverter.ToUInt32(data, 4) != 48 ||
                BitConverter.ToUInt32(data, 8) != 3 || BitConverter.ToUInt32(data, 12) != 2) return null;
            uint offset = BitConverter.ToUInt32(data, 24), length = BitConverter.ToUInt32(data, 28);
            if (offset < 40 || length < 512 || (ulong)offset + 8 + length > (ulong)returned) return null;
            int start = (int)offset + 8;
            bool anyData = false;
            for (int i = 0; i < 512; i++) anyData |= data[start + i] != 0;
            if (!anyData || data[start + 3] > 100 || data[start + 4] > 100) return null;

            int warnings = data[start];
            int spare = data[start + 3], threshold = data[start + 4];
            int remaining = Math.Max(0, 100 - data[start + 5]);
            int kelvin = data[start + 1] | (data[start + 2] << 8);
            ulong hours = BitConverter.ToUInt64(data, start + 128);
            bool hoursFit = true, mediaErrors = false;
            for (int i = 8; i < 16; i++) hoursFit &= data[start + 128 + i] == 0;
            for (int i = 0; i < 16; i++) mediaErrors |= data[start + 160 + i] != 0;
            var info = new StorageHealthInfo
            {
                DiskIndex = diskIndex,
                Source = "NVMe SMART",
                IsSsd = true,
                LifeRemainingPercent = remaining,
                TemperatureCelsius = kelvin >= 233 && kelvin <= 473 ? (int?)(kelvin - 273) : null,
                PowerOnHours = hoursFit && hours <= int.MaxValue ? (int?)hours : null,
                State = StorageHealthState.Good,
                StatusText = "No problems reported by NVMe SMART"
            };
            if ((warnings & 0x2C) != 0)
            {
                info.State = StorageHealthState.Critical;
                info.StatusText = "NVMe reports degraded reliability or read-only media";
            }
            else if (remaining <= StorageHealthLogic.CriticalAtOrBelowPercent)
            {
                info.State = StorageHealthState.Critical;
                info.StatusText = "SSD endurance nearly used up";
            }
            else if (warnings != 0 || spare < threshold || mediaErrors)
            {
                info.State = StorageHealthState.Warning;
                info.StatusText = mediaErrors ? "NVMe media or data integrity errors reported"
                    : spare < threshold ? "NVMe available spare is below its threshold"
                    : (warnings & 2) != 0 ? "NVMe temperature warning"
                    : "NVMe health warning (0x" + warnings.ToString("X2") + ")";
            }
            else if (remaining < StorageHealthLogic.WarnBelowPercent)
            {
                info.State = StorageHealthState.Warning;
                info.StatusText = "SSD wear is high";
            }
            return info;
        }
    }
}
