using System;
using CNSysInfo;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SysInfo.Tests
{
    [TestClass]
    public class RatingTests
    {
        private const CpuSupportLevel Ok = CpuSupportLevel.Supported;
        private const CpuSupportLevel Old = CpuSupportLevel.Unsupported;

        [TestMethod]
        public void EveryScoreMapsToExactlyOneTier_AndTiersAreMonotonic()
        {
            var previous = (int)SpecRatingLevel.Obsolete + 1;
            for (var score = 0; score <= SpecRatingEngine.MaxScore; score++)
            {
                var level = SpecRatingEngine.LevelForScore(score);
                Assert.AreNotEqual(SpecRatingLevel.Unknown, level);
                // higher score => better (smaller enum value after Unknown)
                Assert.IsTrue((int)level <= previous, "score " + score);
                previous = (int)level;
            }
            Assert.AreEqual(SpecRatingLevel.Obsolete, SpecRatingEngine.LevelForScore(0));
            Assert.AreEqual(SpecRatingLevel.Flagship, SpecRatingEngine.LevelForScore(13));
        }

        [TestMethod]
        public void RamBoundaries_AreHalfOpen()
        {
            Assert.AreEqual(6, SpecRatingEngine.RamPoints(32));
            Assert.AreEqual(5, SpecRatingEngine.RamPoints(31.99));
            Assert.AreEqual(5, SpecRatingEngine.RamPoints(24));
            Assert.AreEqual(4, SpecRatingEngine.RamPoints(23.99));
            Assert.AreEqual(4, SpecRatingEngine.RamPoints(16));
            Assert.AreEqual(3, SpecRatingEngine.RamPoints(15.99));
            Assert.AreEqual(3, SpecRatingEngine.RamPoints(12));
            Assert.AreEqual(2, SpecRatingEngine.RamPoints(11.99));
            Assert.AreEqual(2, SpecRatingEngine.RamPoints(8));
            Assert.AreEqual(1, SpecRatingEngine.RamPoints(7.99));
            Assert.AreEqual(1, SpecRatingEngine.RamPoints(4));
            Assert.AreEqual(0, SpecRatingEngine.RamPoints(3.99));
        }

        [TestMethod]
        public void KnownCombinations()
        {
            Assert.AreEqual(SpecRatingLevel.Flagship, SpecRatingEngine.Evaluate(Ok, 32, StorageKind.Nvme));
            Assert.AreEqual(SpecRatingLevel.Enthusiast, SpecRatingEngine.Evaluate(Ok, 24, StorageKind.Nvme));
            Assert.AreEqual(SpecRatingLevel.Enthusiast, SpecRatingEngine.Evaluate(Ok, 32, StorageKind.SataSsd));
            Assert.AreEqual(SpecRatingLevel.PremiumPerformance, SpecRatingEngine.Evaluate(Ok, 16, StorageKind.Nvme));
            Assert.AreEqual(SpecRatingLevel.HighPerformance, SpecRatingEngine.Evaluate(Ok, 16, StorageKind.SataSsd));
            Assert.AreEqual(SpecRatingLevel.ModernStandard, SpecRatingEngine.Evaluate(Ok, 12, StorageKind.SataSsd));
            Assert.AreEqual(SpecRatingLevel.AdequatePerformance, SpecRatingEngine.Evaluate(Ok, 8, StorageKind.SataSsd));
            Assert.AreEqual(SpecRatingLevel.BelowStandard, SpecRatingEngine.Evaluate(Ok, 8, StorageKind.Hdd));
            Assert.AreEqual(SpecRatingLevel.AdequatePerformance, SpecRatingEngine.Evaluate(Old, 16, StorageKind.Nvme));
            Assert.AreEqual(SpecRatingLevel.Obsolete, SpecRatingEngine.Evaluate(Old, 2, StorageKind.Hdd));
        }

        [TestMethod]
        public void MissingDataIsUnknown_NeverAdequate()
        {
            Assert.AreEqual(SpecRatingLevel.Unknown, SpecRatingEngine.Evaluate(CpuSupportLevel.Unknown, 16, StorageKind.Nvme));
            Assert.AreEqual(SpecRatingLevel.Unknown, SpecRatingEngine.Evaluate(Ok, 0, StorageKind.Nvme));
            Assert.AreEqual(SpecRatingLevel.Unknown, SpecRatingEngine.Evaluate(Ok, -1, StorageKind.Nvme));
            Assert.AreEqual(SpecRatingLevel.Unknown, SpecRatingEngine.Evaluate(Ok, 16, StorageKind.Unknown));
        }

        [TestMethod]
        public void StorageClassification()
        {
            Assert.AreEqual(StorageKind.Nvme, SpecRatingEngine.ClassifyStorage("SSD", "Samsung 970 EVO NVMe", "SCSI"));
            Assert.AreEqual(StorageKind.SataSsd, SpecRatingEngine.ClassifyStorage("SSD", "CT500MX500SSD1", "IDE"));
            Assert.AreEqual(StorageKind.SataSsd, SpecRatingEngine.ClassifyStorage("Fixed hard disk media", "Kingston SSD", "SATA"));
            Assert.AreEqual(StorageKind.Hdd, SpecRatingEngine.ClassifyStorage("HDD", "WDC WD10", "SATA"));
            Assert.AreEqual(StorageKind.Unknown, SpecRatingEngine.ClassifyStorage("", "", ""));
            Assert.AreEqual(StorageKind.Unknown, SpecRatingEngine.ClassifyStorage("Unspecified", "Generic Disk", "SCSI"));
        }

        [TestMethod]
        public void ParseGigabytes()
        {
            Assert.AreEqual(16, SpecRatingEngine.ParseGigabytes("16 GB"), 0.001);
            Assert.AreEqual(1024, SpecRatingEngine.ParseGigabytes("1 TB"), 0.001);
            Assert.AreEqual(-1, SpecRatingEngine.ParseGigabytes("Unknown"), 0.001);
            Assert.AreEqual(-1, SpecRatingEngine.ParseGigabytes(null), 0.001);
        }
    }

    [TestClass]
    public class WindowsVersionTests
    {
        [TestMethod]
        public void Win10Vs11ByBuild()
        {
            Assert.AreEqual("Windows 10", WindowsVersionInfo.GetProductName(10, 19045));
            Assert.AreEqual("Windows 10", WindowsVersionInfo.GetProductName(10, 21999));
            Assert.AreEqual("Windows 11", WindowsVersionInfo.GetProductName(10, 22000));
            Assert.AreEqual("Windows 11", WindowsVersionInfo.GetProductName(10, 26100));
        }
    }

    [TestClass]
    public class MergeIntegrationTests
    {
        [TestMethod]
        public void MemoryGbPrefersExactBytes()
        {
            Assert.AreEqual(16.0, SpecRatingEngine.MemoryGb(16UL * 1024 * 1024 * 1024, "garbage"), 0.0001);
            Assert.AreEqual(8.0, SpecRatingEngine.MemoryGb(0, "8 GB"), 0.0001);
            Assert.AreEqual(-1, SpecRatingEngine.MemoryGb(0, "Unknown"), 0.0001);
        }

        [TestMethod]
        public void UnknownCpuFromDataEvaluatorGivesUnknownRating()
        {
            var level = CpuSupportEvaluator.Evaluate("Totally Unknown Chip 9000", "", 0, 0);
            Assert.AreEqual(CpuSupportLevel.Unknown, level);
            Assert.AreEqual(SpecRatingLevel.Unknown, SpecRatingEngine.Evaluate(level, 16, StorageKind.Nvme));
        }
    }

    [TestClass]
    public class ShutdownTests
    {
        [TestMethod]
        public void RebootIsNeverImmediateAndHasUpperBound()
        {
            StringAssert.Contains(ShutdownPlan.RebootArguments(0, "x"), "/t 5");
            StringAssert.Contains(ShutdownPlan.RebootArguments(60, "x"), "/t 60");
            StringAssert.Contains(ShutdownPlan.RebootArguments(99999, "x"), "/t 600");
            Assert.AreEqual("/a", ShutdownPlan.AbortArguments);
            Assert.IsFalse(ShutdownPlan.RebootArguments(60, "say \"hi\"").Contains("\"hi\""));
        }
    }

    [TestClass]
    public class ReportTests
    {
        private static ReportModel Sample()
        {
            var s = new ReportModel
            {
                CapturedAt = new DateTime(2026, 10, 13, 12, 0, 0),
                MachineName = "ELADWY-PC",
                UserName = "ahmed",
                SerialNumber = "SN123456",
                AssetTag = "AT-998",
                SystemModel = "ThinkPad T14",
                Manufacturer = "LENOVO",
                OsDisplay = "Microsoft Windows 11 Pro 64-bit Version 23H2 (Build 22631)",
                InstalledPhysicalMemory = "16 GB",
                UsablePhysicalMemory = "15.8 GB",
                Cpu = "Intel Core i7-1185G7",
                CpuSupport = CpuSupportLevel.Supported
            };
            s.Disks = new System.Collections.Generic.List<ReportDisk> { new ReportDisk { Index = "0", Model = "Samsung NVMe", Size = "512 GB", MediaType = "SSD", InterfaceType = "SCSI" } };
            return s;
        }

        [TestMethod]
        public void RedactedReportHasNoIdentifiers()
        {
            var s = Sample();
            s.OsDisplay += " user ahmed on ELADWY-PC";
            var text = ReportBuilder.Build(s, true, "1.0");
            foreach (var secret in new[] { "ELADWY-PC", "ahmed", "SN123456", "AT-998" })
                Assert.IsFalse(text.IndexOf(secret, StringComparison.OrdinalIgnoreCase) >= 0, secret);
            StringAssert.Contains(text, "[redacted]");
            StringAssert.Contains(text, "ThinkPad T14");
            Assert.IsFalse(ReportBuilder.SuggestedFileName(s, true).Contains("SN123456"));
        }

        [TestMethod]
        public void UnredactedReportKeepsIdentifiers()
        {
            var text = ReportBuilder.Build(Sample(), false, "1.0");
            StringAssert.Contains(text, "ELADWY-PC");
            StringAssert.Contains(text, "SN123456");
        }

        [TestMethod]
        public void ReportRatingMatchesEngine_AndUnknownIsReportedAsUnknown()
        {
            var s = Sample();
            var level = ReportBuilder.ComputeRating(s);
            StringAssert.Contains(ReportBuilder.Build(s, false, "1.0"), "Rating: " + SpecRatingEngine.Label(level));

            s.InstalledPhysicalMemory = "Unknown";
            Assert.AreEqual(SpecRatingLevel.Unknown, ReportBuilder.ComputeRating(s));
            StringAssert.Contains(ReportBuilder.Build(s, false, "1.0"), "Rating: Unknown");
        }

        [TestMethod]
        public void EmptyInstallDateIsReportedUnknown()
        {
            StringAssert.Contains(ReportBuilder.Build(Sample(), false, "1.0"), "OS Installed On: Unknown");
        }
    }
}
