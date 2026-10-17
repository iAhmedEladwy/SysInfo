using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CNSysInfo.Tests
{
    internal static class Fixture
    {
        public static string Path(string name)
        {
            return System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures", name);
        }

        public static byte[] LoadSmartHex(string name)
        {
            var bytes = new List<byte>();
            foreach (var line in File.ReadAllLines(Path(name)))
            {
                var text = line.Trim();
                if (text.Length == 0 || text.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (var token in text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (token.Length == 4)
                    {
                        // revision word as two bytes, e.g. "1000" -> 0x10 0x00
                        bytes.Add(byte.Parse(token.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                        bytes.Add(byte.Parse(token.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        bytes.Add(byte.Parse(token, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                    }
                }
            }

            // Pad to the real 512 bytes block size so the parser sees a realistic buffer.
            while (bytes.Count < 512)
            {
                bytes.Add(0);
            }

            return bytes.ToArray();
        }
    }

    [TestClass]
    public class CpuSupportTests
    {
        [TestMethod]
        public void CpuNameFixtures_AllMatchExpectedLevel()
        {
            var failures = new List<string>();
            var count = 0;
            foreach (var line in File.ReadAllLines(Fixture.Path("cpu-names.csv")))
            {
                if (line.Trim().Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                var parts = line.Split('|');
                var expected = (CpuSupportLevel)Enum.Parse(typeof(CpuSupportLevel), parts[2].Trim());
                var actual = CpuSupportEvaluator.Evaluate(parts[0], parts[1], 0, 0);
                count++;
                if (actual != expected)
                {
                    failures.Add(parts[0] + " expected " + expected + " got " + actual);
                }
            }

            Assert.IsTrue(count > 40, "fixture file was not read");
            Assert.AreEqual(0, failures.Count, string.Join(Environment.NewLine, failures));
        }

        [TestMethod]
        public void EmptyInput_IsUnknown()
        {
            Assert.AreEqual(CpuSupportLevel.Unknown, CpuSupportEvaluator.Evaluate("", "", 0, 0));
            Assert.AreEqual(CpuSupportLevel.Unknown, CpuSupportEvaluator.Evaluate(null, null, 0, 0));
        }

        [TestMethod]
        public void NameWins_OverCpuidModel()
        {
            // 7th gen name with a model number that would otherwise look supported.
            Assert.AreEqual(CpuSupportLevel.Unsupported, CpuSupportEvaluator.Evaluate("Intel(R) Core(TM) i7-7700 CPU @ 3.60GHz", "GenuineIntel", 6, 0xA5));
        }

        [TestMethod]
        public void NoName_AmbiguousCpuidModel_IsUnknown()
        {
            // 0x8E is Kaby Lake R (7th/8th gen) and 0x9E is Kaby Lake/Coffee Lake: cannot decide from the model alone.
            Assert.AreEqual(CpuSupportLevel.Unknown, CpuSupportEvaluator.Evaluate("", "GenuineIntel", 6, 0x8E));
            Assert.AreEqual(CpuSupportLevel.Unknown, CpuSupportEvaluator.Evaluate("", "GenuineIntel", 6, 0x9E));
        }

        [TestMethod]
        public void NoName_UnambiguousCpuidModel_IsDecided()
        {
            Assert.AreEqual(CpuSupportLevel.Supported, CpuSupportEvaluator.Evaluate("Intel Processor", "GenuineIntel", 6, 0x8C)); // Tiger Lake
            Assert.AreEqual(CpuSupportLevel.Unsupported, CpuSupportEvaluator.Evaluate("Intel Processor", "GenuineIntel", 6, 0x3C)); // Haswell
        }

        [TestMethod]
        public void UnknownVendor_IsUnknown()
        {
            Assert.AreEqual(CpuSupportLevel.Unknown, CpuSupportEvaluator.Evaluate("Some Other CPU", "Acme", 6, 0x8C));
        }

        [TestMethod]
        public void CpuId_DecodesProcessorIdFromWin32Processor()
        {
            // i7-8550U: EAX 000806EA -> family 6, model 0x8E, stepping 0xA
            var sig = CpuId.Parse("BFEBFBFF000806EA");
            Assert.IsTrue(sig.IsValid);
            Assert.AreEqual(6, sig.Family);
            Assert.AreEqual(0x8E, sig.Model);
            Assert.AreEqual(0xA, sig.Stepping);

            // i7-7820HQ: 000906E9 -> model 0x9E
            Assert.AreEqual(0x9E, CpuId.Parse("BFEBFBFF000906E9").Model);

            // Ryzen 5 3600: EAX 00870F10 -> family 0xF + 0x8 = 0x17, model 0x71
            var amd = CpuId.Parse("178BFBFF00870F10");
            Assert.AreEqual(0x17, amd.Family);
            Assert.AreEqual(0x71, amd.Model);
        }

        [TestMethod]
        public void CpuId_RejectsBadInput()
        {
            Assert.IsFalse(CpuId.Parse(null).IsValid);
            Assert.IsFalse(CpuId.Parse("").IsValid);
            Assert.IsFalse(CpuId.Parse("XYZ").IsValid);
            Assert.IsFalse(CpuId.Parse("ZZZZZZZZZZZZZZZZ").IsValid);
        }
    }

    [TestClass]
    public class WindowsVersionTests
    {
        [DataTestMethod]
        [DataRow(10, 19045, "Windows 10")]
        [DataRow(10, 21999, "Windows 10")]
        [DataRow(10, 22000, "Windows 11")]
        [DataRow(10, 22631, "Windows 11")]
        [DataRow(10, 26100, "Windows 11")]
        [DataRow(10, 0, "Windows")]
        [DataRow(6, 9600, "Windows")]
        public void ProductName_UsesBuildNumber(int major, int build, string expected)
        {
            Assert.AreEqual(expected, WindowsVersionInfo.GetProductName(major, build));
        }

        [TestMethod]
        public void Display_Win11BuildWithWindows10Caption_IsWindows11()
        {
            // Early Windows 11 WMI builds still return the caption "Microsoft Windows 10 Pro".
            var text = WindowsVersionInfo.BuildDisplay("Microsoft Windows 10 Pro", 10, 22621, "64-bit", "22H2");
            Assert.AreEqual("Windows 11 Pro 64-bit Version 22H2 (Build 22621)", text);
        }

        [TestMethod]
        public void Display_Windows10_StaysWindows10()
        {
            var text = WindowsVersionInfo.BuildDisplay("Microsoft Windows 10 Home", 10, 19045, "64-bit", "22H2");
            Assert.AreEqual("Windows 10 Home 64-bit Version 22H2 (Build 19045)", text);
        }

        [TestMethod]
        public void Display_MissingParts_AreSkippedNotUnknown()
        {
            Assert.AreEqual("Windows 11 (Build 22000)", WindowsVersionInfo.BuildDisplay("", 10, 22000, "", ""));
        }

        [TestMethod]
        public void TryParseBuild_Variants()
        {
            Assert.IsTrue(WindowsVersionInfo.TryParseBuild("22631", out var a));
            Assert.AreEqual(22631, a);
            Assert.IsTrue(WindowsVersionInfo.TryParseBuild("10.0.19045", out var b));
            Assert.AreEqual(19045, b);
            Assert.IsFalse(WindowsVersionInfo.TryParseBuild("", out _));
            Assert.IsFalse(WindowsVersionInfo.TryParseBuild("abc", out _));
        }
    }

    [TestClass]
    public class BatteryTests
    {
        [DataTestMethod]
        [DataRow(1, "Discharging")]
        [DataRow(2, "On AC power")]
        [DataRow(3, "Fully charged")]
        [DataRow(4, "Low")]
        [DataRow(5, "Critical")]
        [DataRow(6, "Charging")]
        [DataRow(7, "Charging (high)")]
        [DataRow(8, "Charging (low)")]
        [DataRow(9, "Charging (critical)")]
        [DataRow(10, "Unknown")]
        [DataRow(11, "Partially charged")]
        [DataRow(99, "Unknown")]
        public void StatusText_FollowsWin32BatteryDocs(int code, string expected)
        {
            Assert.AreEqual(expected, BatteryMapper.GetStatusText(code));
        }

        [TestMethod]
        public void Status_NullIsUnknown()
        {
            Assert.AreEqual("Unknown", BatteryMapper.GetStatusText(null));
            Assert.IsNull(BatteryMapper.IsCharging(null));
            Assert.IsNull(BatteryMapper.IsOnAcPower(null));
            Assert.AreEqual("Unknown", BatteryMapper.GetPowerText(null));
        }

        [TestMethod]
        public void OldMappingBug_Code2IsNotCharging_Code6IsCharging()
        {
            Assert.AreEqual(false, BatteryMapper.IsCharging(2));
            Assert.AreEqual(true, BatteryMapper.IsOnAcPower(2));
            Assert.AreEqual(true, BatteryMapper.IsCharging(6));
            Assert.AreEqual(false, BatteryMapper.IsCharging(1));
            Assert.AreEqual("On battery", BatteryMapper.GetPowerText(1));
            Assert.AreEqual("Plugged in", BatteryMapper.GetPowerText(3));
        }

        [TestMethod]
        public void Capacity_ZeroOrMissing_IsNull()
        {
            Assert.IsNull(BatteryMapper.NormalizeCapacity(null));
            Assert.IsNull(BatteryMapper.NormalizeCapacity(0));
            Assert.IsNull(BatteryMapper.NormalizeCapacity(0xFFFFFFFFUL));
            Assert.AreEqual(50000, BatteryMapper.NormalizeCapacity(50000));
        }

        [TestMethod]
        public void Charge_OutOfRangeIsNull_ZeroIsRealWhenReported()
        {
            Assert.IsNull(BatteryMapper.NormalizeChargePercent(null));
            Assert.IsNull(BatteryMapper.NormalizeChargePercent(255));
            Assert.AreEqual(0, BatteryMapper.NormalizeChargePercent(0));
            Assert.AreEqual(87, BatteryMapper.NormalizeChargePercent(87));
        }

        [TestMethod]
        public void Health_NullWhenAnyCapacityUnknown()
        {
            Assert.IsNull(BatteryMapper.CalculateHealthPercent(null, 40000));
            Assert.IsNull(BatteryMapper.CalculateHealthPercent(50000, null));
            Assert.IsNull(BatteryMapper.CalculateHealthPercent(0, 40000));
        }

        [TestMethod]
        public void Health_ComputedAndCapped()
        {
            Assert.AreEqual(80, BatteryMapper.CalculateHealthPercent(50000, 40000));
            Assert.AreEqual(100, BatteryMapper.CalculateHealthPercent(50000, 52000));
            Assert.AreEqual(2, BatteryMapper.CalculateHealthPercent(50000, 1000));
        }
    }

    [TestClass]
    public class InstallDateTests
    {
        [TestMethod]
        public void ParseCim_WithOffset_ConvertsToLocal()
        {
            var parsed = WmiValueParser.ParseCimDateTime("20230415093012.000000+120");
            Assert.IsTrue(parsed.HasValue);
            var expected = new DateTime(2023, 4, 15, 7, 30, 12, DateTimeKind.Utc).ToLocalTime();
            Assert.AreEqual(expected, parsed.Value);
        }

        [TestMethod]
        public void ParseCim_NegativeOffset()
        {
            var parsed = WmiValueParser.ParseCimDateTime("20230415093012.000000-300");
            var expected = new DateTime(2023, 4, 15, 14, 30, 12, DateTimeKind.Utc).ToLocalTime();
            Assert.AreEqual(expected, parsed.Value);
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("garbage")]
        [DataRow("00000000000000.000000+000")]
        [DataRow("20231345250000.000000+000")]
        public void ParseCim_BadValues_AreNull(string value)
        {
            Assert.IsNull(WmiValueParser.ParseCimDateTime(value));
        }

        [TestMethod]
        public void UnixSeconds_Fallback()
        {
            var parsed = WmiValueParser.FromUnixSeconds(1700000000);
            Assert.AreEqual(DateTimeOffset.FromUnixTimeSeconds(1700000000).LocalDateTime, parsed.Value);
            Assert.IsNull(WmiValueParser.FromUnixSeconds(0));
            Assert.IsNull(WmiValueParser.FromUnixSeconds(-5));
        }

        [TestMethod]
        public void Format_NullIsUnknown()
        {
            Assert.AreEqual("Unknown", WmiValueParser.FormatInstallDate(null));
            Assert.AreEqual("2023-04-15 12:00", WmiValueParser.FormatInstallDate(new DateTime(2023, 4, 15, 12, 0, 0)));
        }
    }

    [TestClass]
    public class NumericValueTests
    {
        [TestMethod]
        public void ToUInt64_HandlesWmiTypes()
        {
            Assert.AreEqual(8589934592UL, WmiValueParser.ToUInt64(8589934592UL));
            Assert.AreEqual(4096UL, WmiValueParser.ToUInt64((uint)4096));
            Assert.AreEqual(7UL, WmiValueParser.ToUInt64((ushort)7));
            Assert.AreEqual(512110190592UL, WmiValueParser.ToUInt64("512110190592"));
            Assert.IsNull(WmiValueParser.ToUInt64(null));
            Assert.IsNull(WmiValueParser.ToUInt64(-1));
            Assert.IsNull(WmiValueParser.ToUInt64("n/a"));
        }

        [TestMethod]
        public void FormatBytes_BinaryUnits()
        {
            Assert.AreEqual("0 B", WmiValueParser.FormatBytes(0));
            Assert.AreEqual("16 GB", WmiValueParser.FormatBytes(17179869184UL));
            Assert.AreEqual("476.94 GB", WmiValueParser.FormatBytes(512110190592UL));
            Assert.AreEqual("1.5 TB", WmiValueParser.FormatBytes(1649267441664UL));
        }

        [TestMethod]
        public void FormatBytes_IsCultureIndependent()
        {
            var old = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("ar-EG");
                Assert.AreEqual("476.94 GB", WmiValueParser.FormatBytes(512110190592UL));
            }
            finally
            {
                CultureInfo.CurrentCulture = old;
            }
        }

        [TestMethod]
        public void Percent_ClampsAndHandlesZero()
        {
            Assert.IsNull(WmiValueParser.Percent(1, 0));
            Assert.AreEqual(50, WmiValueParser.Percent(50, 100));
            Assert.AreEqual(100, WmiValueParser.Percent(500, 100));
            Assert.AreEqual(0, WmiValueParser.Percent(0, 100));
        }

        [TestMethod]
        public void DiskUsage_FromTotals()
        {
            var usage = DiskUsageInfo.FromTotals(500UL * 1024 * 1024 * 1024, 125UL * 1024 * 1024 * 1024);
            Assert.IsTrue(usage.HasData);
            Assert.AreEqual(375UL * 1024 * 1024 * 1024, usage.UsedBytes);
            Assert.AreEqual(75, usage.UsagePercentage);
        }

        [TestMethod]
        public void DiskUsage_FreeLargerThanTotal_IsClamped_AndZeroTotalHasNoData()
        {
            var usage = DiskUsageInfo.FromTotals(100, 200);
            Assert.AreEqual(0UL, usage.UsedBytes);
            Assert.AreEqual(0, usage.UsagePercentage);
            Assert.IsFalse(DiskUsageInfo.FromTotals(0, 0).HasData);
        }
    }

    [TestClass]
    public class StorageHealthTests
    {
        [TestMethod]
        public void NoData_IsUnknown_NotHealthy()
        {
            var info = StorageHealthLogic.Evaluate("0", null, null, null);
            Assert.AreEqual(StorageHealthState.Unknown, info.State);
            Assert.IsFalse(info.IsHealthy);
            Assert.IsNull(info.LifeRemainingPercent);
        }

        [TestMethod]
        public void WindowsStatusOk_AloneIsStillUnknown()
        {
            var info = StorageHealthLogic.Evaluate("0", null, null, "OK");
            Assert.AreEqual(StorageHealthState.Unknown, info.State);
            Assert.IsNull(info.LifeRemainingPercent);
            StringAssert.Contains(info.StatusText, "Unknown");
        }

        [TestMethod]
        public void PredictFailure_IsCritical_EvenWithGoodAttributes()
        {
            var attrs = StorageHealthLogic.ParseSmartAttributes(Fixture.LoadSmartHex("smart-ssd-ok.hex"));
            var info = StorageHealthLogic.Evaluate("0", attrs, true, "OK");
            Assert.AreEqual(StorageHealthState.Critical, info.State);
        }

        [TestMethod]
        public void WindowsPredFail_IsCritical()
        {
            var info = StorageHealthLogic.Evaluate("0", null, null, "Pred Fail");
            Assert.AreEqual(StorageHealthState.Critical, info.State);
        }

        [TestMethod]
        public void PredictFailureFalse_NoSmart_IsGoodWithoutInventedPercent()
        {
            var info = StorageHealthLogic.Evaluate("0", null, false, "OK");
            Assert.AreEqual(StorageHealthState.Good, info.State);
            Assert.IsNull(info.LifeRemainingPercent);
        }

        [TestMethod]
        public void SsdFixture_ParsedValues()
        {
            var attrs = StorageHealthLogic.ParseSmartAttributes(Fixture.LoadSmartHex("smart-ssd-ok.hex"));
            var info = StorageHealthLogic.Evaluate("0", attrs, false, "OK", knownSsdLifeEncoding: true);
            Assert.AreEqual(StorageHealthState.Good, info.State);
            Assert.AreEqual(97, info.LifeRemainingPercent);
            Assert.AreEqual(35, info.TemperatureCelsius);
            Assert.AreEqual(10000, info.PowerOnHours);
            Assert.AreEqual(true, info.IsSsd);
        }

        [TestMethod]
        public void WornSsd_IsCritical()
        {
            var attrs = StorageHealthLogic.ParseSmartAttributes(Fixture.LoadSmartHex("smart-ssd-worn.hex"));
            var info = StorageHealthLogic.Evaluate("0", attrs, false, "OK", knownSsdLifeEncoding: true);
            Assert.AreEqual(StorageHealthState.Critical, info.State);
            Assert.AreEqual(8, info.LifeRemainingPercent);
        }

        [TestMethod]
        public void HddWithReallocatedSectors_IsWarning_NoPercent()
        {
            var attrs = StorageHealthLogic.ParseSmartAttributes(Fixture.LoadSmartHex("smart-hdd-bad-sectors.hex"));
            var info = StorageHealthLogic.Evaluate("1", attrs, false, "OK");
            Assert.AreEqual(StorageHealthState.Warning, info.State);
            Assert.IsNull(info.LifeRemainingPercent);
            Assert.AreEqual(41, info.TemperatureCelsius);
            Assert.AreEqual(20000, info.PowerOnHours);
        }

        [TestMethod]
        public void SectorCountersWithoutOverallStatus_AreUnknown_NoPercent()
        {
            var attrs = StorageHealthLogic.ParseSmartAttributes(Fixture.LoadSmartHex("smart-hdd-clean.hex"));
            var info = StorageHealthLogic.Evaluate("1", attrs, null, "OK");
            Assert.AreEqual(StorageHealthState.Unknown, info.State);
            Assert.IsNull(info.LifeRemainingPercent);
            Assert.AreEqual(1000, info.PowerOnHours);
        }

        [TestMethod]
        public void HealthIsSeparateFromUsage_FullDriveIsNotUnhealthy()
        {
            var usage = DiskUsageInfo.FromTotals(1000, 10); // 99% full
            var attrs = StorageHealthLogic.ParseSmartAttributes(Fixture.LoadSmartHex("smart-hdd-clean.hex"));
            var health = StorageHealthLogic.Evaluate("0", attrs, false, "OK");
            Assert.AreEqual(99, usage.UsagePercentage);
            Assert.AreEqual(StorageHealthState.Good, health.State);
        }

        [TestMethod]
        public void InstanceMatching_UsesPnpId_NotPhysicalDriveNumber()
        {
            const string pnp = @"SCSI\DISK&VEN_SAMSUNG&PROD_SSD_850\4&1A2B3C&0&000000";
            Assert.IsTrue(StorageHealthLogic.InstanceMatchesPnpId(pnp + "_0", pnp));
            Assert.IsTrue(StorageHealthLogic.InstanceMatchesPnpId(pnp.ToLowerInvariant() + "_0", pnp));
            Assert.IsTrue(StorageHealthLogic.InstanceMatchesPnpId(pnp, pnp));
            // another disk whose id merely starts the same
            Assert.IsFalse(StorageHealthLogic.InstanceMatchesPnpId(pnp + "1_0", pnp));
            Assert.IsFalse(StorageHealthLogic.InstanceMatchesPnpId(@"SCSI\DISK&VEN_OTHER\4&99&0&000000_0", pnp));
            Assert.IsFalse(StorageHealthLogic.InstanceMatchesPnpId(null, pnp));
            Assert.IsFalse(StorageHealthLogic.InstanceMatchesPnpId(pnp, ""));
        }

        [TestMethod]
        public void Parser_ShortOrNullBuffers_AreEmpty()
        {
            Assert.AreEqual(0, StorageHealthLogic.ParseSmartAttributes(null).Count);
            Assert.AreEqual(0, StorageHealthLogic.ParseSmartAttributes(new byte[5]).Count);
        }

        [TestMethod]
        public void VendorSpecificAttributes_DoNotInventSsdLife()
        {
            var attrs = StorageHealthLogic.ParseSmartAttributes(Fixture.LoadSmartHex("smart-ssd-worn.hex"));
            var info = StorageHealthLogic.Evaluate("0", attrs, false, "OK");
            Assert.IsNull(info.LifeRemainingPercent);
            Assert.IsNull(info.IsSsd);
        }

        [TestMethod]
        public void TemperatureAlone_DoesNotEstablishHealth()
        {
            var attrs = new Dictionary<int, SmartAttribute> { {194, new SmartAttribute(194, 100, 35)} };
            Assert.AreEqual(StorageHealthState.Unknown, StorageHealthLogic.Evaluate("0", attrs, null, "OK").State);
        }

        [DataTestMethod]
        [DataRow(0, 100, (int)StorageHealthState.Good)]
        [DataRow(70, 30, (int)StorageHealthState.Warning)]
        [DataRow(90, 10, (int)StorageHealthState.Critical)]
        [DataRow(100, 0, (int)StorageHealthState.Critical)]
        [DataRow(105, 0, (int)StorageHealthState.Critical)]
        public void WindowsWear_IsConsumedEndurance(int wear, int remaining, int state)
        {
            var info = new StorageHealthInfo();
            StorageHealthLogic.MergeWindowsHealth(info, 0, wear, 38, 1000, null, null);
            Assert.AreEqual(remaining, info.LifeRemainingPercent);
            Assert.AreEqual((StorageHealthState)state, info.State);
            Assert.AreEqual(38, info.TemperatureCelsius);
            Assert.AreEqual(1000, info.PowerOnHours);
        }

        [TestMethod]
        public void WindowsHealthy_DoesNotHideFailurePrediction()
        {
            var info = StorageHealthLogic.Evaluate("0", null, true, "OK");
            StorageHealthLogic.MergeWindowsHealth(info, 0, 1, null, null, 0, 0);
            Assert.AreEqual(StorageHealthState.Critical, info.State);
            StringAssert.Contains(info.StatusText, "Failure predicted");
        }

        [TestMethod]
        public void WindowsErrors_OverrideHealthy_AndNeverDowngrade()
        {
            var info = new StorageHealthInfo();
            StorageHealthLogic.MergeWindowsHealth(info, 0, null, null, null, 1, 0);
            Assert.AreEqual(StorageHealthState.Warning, info.State);
            StorageHealthLogic.MergeWindowsHealth(info, 0, null, null, null, 0, 0);
            Assert.AreEqual(StorageHealthState.Warning, info.State);
            StorageHealthLogic.MergeWindowsHealth(info, 2, null, null, null, null, null);
            Assert.AreEqual(StorageHealthState.Critical, info.State);
        }

        [TestMethod]
        public void MissingAndInvalidCounters_RemainUnknown()
        {
            var info = new StorageHealthInfo();
            StorageHealthLogic.MergeWindowsHealth(info, 5, -1, 255, -1, null, null);
            Assert.AreEqual(StorageHealthState.Unknown, info.State);
            Assert.IsNull(info.LifeRemainingPercent);
            Assert.IsNull(info.TemperatureCelsius);
            Assert.IsNull(info.PowerOnHours);
        }
    }
}
