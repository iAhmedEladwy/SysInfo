using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CNSysInfo.Tests
{
    [TestClass]
    public class ActivationTests
    {
        [TestMethod]
        public void PreservesAllDefinedStates()
        {
            for (int i = 0; i <= 6; i++)
            {
                var r = new ActivationResult((WindowsLicenseStatus)i, ActivationUnavailableReason.None, DateTime.Now);
                Assert.IsTrue(r.IsAvailable);
                Assert.AreEqual(i, (int)r.Status.Value);
                Assert.AreNotEqual("Unknown", r.DisplayText);
                if (i > 1) Assert.AreNotEqual("Not Activated", r.DisplayText);
            }
        }
        [TestMethod]
        public void CompletedTransientFailureRetriesOnceInSameCall()
        {
            int calls = 0;
            var r = ActivationLogic.Read((fallback, budget) => {
                calls++; Assert.IsFalse(fallback); Assert.AreEqual(2000, budget);
                return calls == 1 ? ActivationResult.Unavailable(ActivationUnavailableReason.Timeout) :
                    new ActivationResult(WindowsLicenseStatus.Licensed, ActivationUnavailableReason.None, DateTime.Now);
            }, () => 6000);
            Assert.IsTrue(r.IsAvailable); Assert.AreEqual(2, calls);
        }
        [TestMethod]
        public void DeadlinePreventsRetryAndNoRowsUsesFallback()
        {
            int checks = 0, calls = 0;
            var r = ActivationLogic.Read((fallback, budget) => { calls++; return ActivationResult.Unavailable(ActivationUnavailableReason.Timeout); }, () => checks++ == 0 ? 100 : 0);
            Assert.AreEqual(1, calls); Assert.AreEqual(ActivationUnavailableReason.Deadline, r.Reason);
            calls = 0;
            ActivationLogic.Read((fallback, budget) => { Assert.AreEqual(calls++ == 1, fallback); return ActivationResult.Unavailable(ActivationUnavailableReason.NoInstalledRows); }, () => 100);
            Assert.AreEqual(2, calls);
        }
        [TestMethod]
        public void DefinitiveResultsAndBusyNeverRetry()
        {
            foreach (var result in new[] { new ActivationResult(WindowsLicenseStatus.Notification, ActivationUnavailableReason.None, DateTime.Now), ActivationResult.Unavailable(ActivationUnavailableReason.Busy), ActivationResult.Unavailable(ActivationUnavailableReason.Error) })
            {
                int calls = 0;
                ActivationLogic.Read((f, b) => { calls++; return result; }, () => 6000);
                Assert.AreEqual(1, calls);
            }
        }
        [TestMethod]
        public void HistoryIsDatedTransparentAndDefinitiveStateReplacesSuccess()
        {
            var h = new ActivationHistory(); var time = new DateTime(2026, 10, 17, 12, 30, 0);
            var unknown = ActivationResult.Unavailable(ActivationUnavailableReason.Error);
            Assert.AreEqual("Unknown (current check unavailable)", h.FormatCurrent(unknown));
            h.Observe(new ActivationResult(WindowsLicenseStatus.Licensed, ActivationUnavailableReason.None, time));
            h.Observe(unknown);
            StringAssert.Contains(h.FormatCurrent(unknown), "2026-10-17 12:30:00");
            StringAssert.Contains(h.FormatCurrent(unknown), "current check unavailable");
            h.Observe(new ActivationResult(WindowsLicenseStatus.Unlicensed, ActivationUnavailableReason.None, time.AddMinutes(1)));
            StringAssert.StartsWith(h.FormatCurrent(unknown), "Not Activated");
            h.Observe(new ActivationResult(WindowsLicenseStatus.Licensed, ActivationUnavailableReason.None, time));
            StringAssert.StartsWith(h.FormatCurrent(unknown), "Not Activated");
        }
    }
}
