using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CNSysInfo.Tests
{
    [TestClass]
    [DoNotParallelize]
    public class CollectionSessionTests
    {
        [TestMethod]
        public void ExpiredDeadline_DoesNotLaunchProvider()
        {
            using var session = new CollectionSession(0);
            bool called = false;
            var result = session.Read(() => { called = true; return "current"; }, "activation", "fallback",
                unavailable: reason => reason);
            Assert.AreEqual("deadline", result);
            Assert.IsFalse(called);
            Assert.AreEqual(1, session.Warnings.Count);
        }

        [TestMethod]
        public void OccupiedWorkers_ReturnBusy_AndLateResultsStayAbandoned()
        {
            using var release = new ManualResetEventSlim(false);
            using var finished = new CountdownEvent(4);
            var history = new ActivationHistory();
            var previous = new ActivationResult(WindowsLicenseStatus.Unlicensed, ActivationUnavailableReason.None,
                new DateTime(2026, 10, 17, 12, 0, 0));
            history.Observe(previous);
            try
            {
                for (int i = 0; i < 4; i++)
                {
                    using var session = new CollectionSession(1000);
                    var result = session.Read(() =>
                    {
                        try
                        {
                            release.Wait();
                            return new ActivationResult(WindowsLicenseStatus.Licensed, ActivationUnavailableReason.None,
                                new DateTime(2026, 10, 17, 13, 0, 0));
                        }
                        finally { finished.Signal(); }
                    }, "activation", ActivationResult.Unavailable(ActivationUnavailableReason.Error), 20,
                        reason => ActivationResult.Unavailable(reason == "timeout" ? ActivationUnavailableReason.Timeout : ActivationUnavailableReason.Error));
                    Assert.AreEqual(ActivationUnavailableReason.Timeout, result.Reason);
                    history.Observe(result);
                }
                using var busy = new CollectionSession(1000);
                bool called = false;
                var blocked = busy.Read(() => { called = true; return "current"; }, "activation", "fallback",
                    unavailable: reason => reason);
                Assert.AreEqual("busy", blocked);
                Assert.IsFalse(called);
            }
            finally { release.Set(); Assert.IsTrue(finished.Wait(TimeSpan.FromSeconds(10)), "Workers must be released after the test."); }
            StringAssert.StartsWith(history.FormatCurrent(ActivationResult.Unavailable(ActivationUnavailableReason.Timeout)), "Not Activated");
        }
    }
}
