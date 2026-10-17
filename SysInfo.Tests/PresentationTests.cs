using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CNSysInfo.Tests
{
    [TestClass]
    public class PresentationTests
    {
        [TestMethod] public void UsageTiers_AreInverse()
        {
            Assert.AreEqual("#4B0082", Presentation.ForUsage(10).Back);
            Assert.AreEqual("#0078D7", Presentation.ForUsage(50).Back);
            Assert.AreEqual("#FFA54F", Presentation.ForUsage(95).Back);
        }
        [TestMethod] public void HealthTiers_Boundaries()
        {
            Assert.AreEqual("#4B0082", Presentation.ForHealth(90).Back);
            Assert.AreEqual("#9CCC65", Presentation.ForHealth(89).Back == "#0078D7" ? "#9CCC65" : "x");
            Assert.AreEqual("#FFB6C1", Presentation.ForHealth(29).Back);
        }
        [TestMethod] public void RatingText_UnknownAndUpgrades()
        {
            StringAssert.Contains(Presentation.RatingText(SpecRatingLevel.Unknown, ""), "not enough data");
            StringAssert.Contains(Presentation.RatingText(SpecRatingLevel.BelowStandard, "RAM to 16GB"), "Upgrade recommended: RAM to 16GB");
            Assert.IsFalse(Presentation.RatingText(SpecRatingLevel.ModernStandard, "x").Contains("x"));
        }
        [TestMethod] public void Upgrades_HddSupportedCpu16Gb()
        {
            Assert.AreEqual("Storage to Gen4 NVMe SSD (5000MB/s+)", Presentation.UpgradeRecommendations(CpuSupportLevel.Supported, 16, StorageKind.Hdd));
        }
        [TestMethod] public void Upgrades_LowRam()
        {
            StringAssert.Contains(Presentation.UpgradeRecommendations(CpuSupportLevel.Supported, 4, StorageKind.Nvme), "RAM to 16GB (add 12GB)");
        }
        [TestMethod] public void FormatBytes_Works()
        {
            Assert.AreEqual("0 B", Presentation.FormatBytes(0));
            Assert.AreEqual("1.5 GB", Presentation.FormatBytes(1610612736UL));
        }
        [TestMethod] public void CpuText_Variants()
        {
            Assert.AreEqual("Unknown", Presentation.CpuText("", 0, 0));
            Assert.AreEqual("i7 | Cores: 4 | Threads: 8", Presentation.CpuText(" i7 ", 4, 8));
        }
        [TestMethod] public void MemoryText_ClampsSlots()
        {
            Assert.AreEqual("16 GB (15.8 GB usable) | Slots: 2/2 | Speed: 3200 MHz", Presentation.MemoryText("16 GB", "15.8 GB", 5, 2, 3200));
        }
    }
}
