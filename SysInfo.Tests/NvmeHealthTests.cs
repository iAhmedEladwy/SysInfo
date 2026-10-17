using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CNSysInfo.Tests
{
    [TestClass]
    public class NvmeHealthTests
    {
        private static byte[] Response()
        {
            var data = new byte[560];
            Put(data, 0, 48); Put(data, 4, 48);
            Put(data, 8, 3); Put(data, 12, 2);
            Put(data, 24, 40); Put(data, 28, 512);
            data[48 + 1] = 0x35; data[48 + 2] = 1; // 309 K, 36 C
            data[48 + 3] = 100; data[48 + 4] = 10;
            data[48 + 5] = 3;
            data[48 + 128] = 0x10; data[48 + 129] = 0x27; // 10000 h
            return data;
        }

        private static void Put(byte[] data, int offset, uint value)
        {
            Array.Copy(BitConverter.GetBytes(value), 0, data, offset, 4);
        }

        [TestMethod]
        public void StandardHealthLog_UsesConsumedLifeAndKelvinTemperature()
        {
            var data = Response();
            var result = NvmeHealthLogic.ParseResponse(data, data.Length, "2");
            Assert.AreEqual("2", result.DiskIndex);
            Assert.AreEqual(StorageHealthState.Good, result.State);
            Assert.AreEqual("NVMe SMART", result.Source);
            Assert.AreEqual(97, result.LifeRemainingPercent);
            Assert.AreEqual(36, result.TemperatureCelsius);
            Assert.AreEqual(10000, result.PowerOnHours);
            Assert.AreEqual(true, result.IsSsd);
        }

        [DataTestMethod]
        [DataRow(4)]
        [DataRow(8)]
        [DataRow(32)]
        public void ReliabilityAndReadOnlyWarnings_AreCritical(int warning)
        {
            var data = Response(); data[48] = (byte)warning;
            Assert.AreEqual(StorageHealthState.Critical, NvmeHealthLogic.ParseResponse(data, data.Length, "0").State);
        }

        [TestMethod]
        public void TemperatureSpareAndMediaErrors_AreWarnings()
        {
            var data = Response(); data[48] = 2;
            Assert.AreEqual(StorageHealthState.Warning, NvmeHealthLogic.ParseResponse(data, data.Length, "0").State);
            data[48] = 0; data[51] = 5;
            Assert.AreEqual(StorageHealthState.Warning, NvmeHealthLogic.ParseResponse(data, data.Length, "0").State);
            data[51] = 100; data[48 + 160 + 15] = 1; // include high bits of the 128-bit counter
            Assert.AreEqual(StorageHealthState.Warning, NvmeHealthLogic.ParseResponse(data, data.Length, "0").State);
        }

        [TestMethod]
        public void EnduranceOver100_IsExhausted_NotNegative()
        {
            var data = Response(); data[53] = 105;
            var result = NvmeHealthLogic.ParseResponse(data, data.Length, "0");
            Assert.AreEqual(0, result.LifeRemainingPercent);
            Assert.AreEqual(StorageHealthState.Critical, result.State);
        }

        [TestMethod]
        public void MalformedDescriptorAndTruncatedLog_AreRejected()
        {
            Assert.IsNull(NvmeHealthLogic.ParseResponse(null, 0, "0"));
            var data = Response();
            Assert.IsNull(NvmeHealthLogic.ParseResponse(data, 559, "0"));
            Assert.IsNull(NvmeHealthLogic.ParseResponse(data, 561, "0"));
            Put(data, 24, uint.MaxValue);
            Assert.IsNull(NvmeHealthLogic.ParseResponse(data, 560, "0"));
            data = Response(); Put(data, 24, 0);
            Assert.IsNull(NvmeHealthLogic.ParseResponse(data, 560, "0"));
            data = Response(); Put(data, 8, 1);
            Assert.IsNull(NvmeHealthLogic.ParseResponse(data, 560, "0"));
            data = Response(); Put(data, 0, 0);
            Assert.IsNull(NvmeHealthLogic.ParseResponse(data, 560, "0"));
            data = Response(); Array.Clear(data, 48, 512);
            Assert.IsNull(NvmeHealthLogic.ParseResponse(data, 560, "0"));
        }

        [TestMethod]
        public void OversizedHoursAndMissingTemperature_AreUnknown()
        {
            var data = Response();
            data[49] = 0; data[50] = 0; data[48 + 128 + 8] = 1;
            var result = NvmeHealthLogic.ParseResponse(data, data.Length, "0");
            Assert.IsNull(result.PowerOnHours);
            Assert.IsNull(result.TemperatureCelsius);
            data[48 + 3] = 255;
            Assert.IsNull(NvmeHealthLogic.ParseResponse(data, data.Length, "0"));
        }
    }
}
