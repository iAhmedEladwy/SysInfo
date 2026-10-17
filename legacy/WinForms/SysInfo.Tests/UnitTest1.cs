using CNSysInfo;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SysInfo.Tests
{
    [TestClass]
    public class CpuSupportEvaluatorTests
    {
        [TestMethod]
        public void IntelCoreI7_8565U_IsSupported()
        {
            var snapshot = new SystemInfoSnapshot
            {
                Cpu = "Intel(R) Core(TM) i7-8565U CPU @ 1.80GHz",
                CpuManufacturer = "GenuineIntel",
                CpuFamily = 6,
                CpuModel = 0x9E
            };

            var result = CpuSupportEvaluator.Evaluate(snapshot);

            Assert.AreEqual(CpuSupportLevel.Supported, result);
        }

        [TestMethod]
        public void AmdRyzen7_5800U_IsSupported()
        {
            var snapshot = new SystemInfoSnapshot
            {
                Cpu = "AMD Ryzen 7 5800U with Radeon Graphics",
                CpuManufacturer = "AuthenticAMD"
            };

            var result = CpuSupportEvaluator.Evaluate(snapshot);

            Assert.AreEqual(CpuSupportLevel.Supported, result);
        }

        [TestMethod]
        public void IntelCoreI7_1185G7_IsSupported()
        {
            var snapshot = new SystemInfoSnapshot
            {
                Cpu = "Intel(R) Core(TM) i7-1185G7 @ 3.00GHz",
                CpuManufacturer = "GenuineIntel",
                CpuFamily = 6,
                CpuModel = 0x8C
            };

            var result = CpuSupportEvaluator.Evaluate(snapshot);

            Assert.AreEqual(CpuSupportLevel.Supported, result);
        }

        [TestMethod]
        public void IntelCoreI7_6500U_IsUnsupported()
        {
            var snapshot = new SystemInfoSnapshot
            {
                Cpu = "Intel(R) Core(TM) i7-6500U CPU @ 2.50GHz",
                CpuManufacturer = "GenuineIntel",
                CpuFamily = 6,
                CpuModel = 0x5E
            };

            var result = CpuSupportEvaluator.Evaluate(snapshot);

            Assert.AreEqual(CpuSupportLevel.Unsupported, result);
        }

        [TestMethod]
        public void UnknownCpu_ReturnsUnknown()
        {
            var snapshot = new SystemInfoSnapshot
            {
                Cpu = "Some Custom CPU",
                CpuManufacturer = string.Empty
            };

            var result = CpuSupportEvaluator.Evaluate(snapshot);

            Assert.AreEqual(CpuSupportLevel.Unknown, result);
        }
    }
}
