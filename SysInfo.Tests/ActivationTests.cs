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
            for (int i = 0; i <= 7; i++)
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
                calls++; Assert.AreEqual(calls > 1, fallback); Assert.AreEqual(2000, budget);
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
            foreach (var result in new[] { new ActivationResult(WindowsLicenseStatus.Notification, ActivationUnavailableReason.None, DateTime.Now), ActivationResult.Unavailable(ActivationUnavailableReason.Busy) })
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
        [TestMethod] public void DigitalLicenseWithoutPartialKeyIsAcceptedForCurrentEdition()
        {
            var digital = Row("Professional", 1, false);
            Assert.AreEqual("Activated", ActivationLogic.SelectRows(new[] { digital }, "Professional").DisplayText);
            Assert.IsFalse(ActivationLogic.SelectRows(new[] { Row("Professional", 0, false) }, "Professional").IsAvailable);
        }
        [TestMethod] public void OfficeAddonsAndOtherEditionsCannotActivateCurrentWindows()
        {
            var office = Row("Professional", 1, true); office.ApplicationId = "Office";
            var addon = Row("Professional", 1, true); addon.IsAddon = true;
            var other = Row("Enterprise", 1, true);
            Assert.IsFalse(ActivationLogic.SelectRows(new[] { office, addon, other }, "Professional").IsAvailable);
            Assert.AreEqual("Not Activated", ActivationLogic.SelectRows(new[] { addon, Row("Professional", 0, true) }, "Professional").DisplayText);
            Assert.IsTrue(ActivationLogic.MatchesEdition("Windows(R), Professional edition", "Professional"));
            Assert.IsFalse(ActivationLogic.MatchesEdition("Windows(R), ProfessionalWorkstation edition", "Professional"));
        }
        [TestMethod] public void ProviderErrorCanUseIndependentFallback()
        {
            int calls = 0;
            var result = ActivationLogic.Read((fallback, ms) => {
                calls++; return fallback ? new ActivationResult(WindowsLicenseStatus.Licensed, ActivationUnavailableReason.None, DateTime.Now) : ActivationResult.Unavailable(ActivationUnavailableReason.Error);
            }, () => 6000);
            Assert.IsTrue(result.IsAvailable); Assert.AreEqual(2, calls);
        }
        private static ActivationCandidate Row(string family, int status, bool key) => new ActivationCandidate {
            ApplicationId = ActivationLogic.WindowsApplicationId, Name = "Windows(R), " + family + " edition", Family = family, Status = status, HasPartialKey = key
        };    }
}
