using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CNSysInfo.Tests
{
    [TestClass]
    public class UiStatusTests
    {
        [TestMethod]
        public void RestartReminder_OnlyAt48HoursOfKnownUptime()
        {
            Assert.IsFalse(UiStatusLogic.ShowRestartReminder(TimeSpan.FromHours(48) - TimeSpan.FromTicks(1), true));
            Assert.IsTrue(UiStatusLogic.ShowRestartReminder(TimeSpan.FromHours(48), true));
            Assert.IsTrue(UiStatusLogic.ShowRestartReminder(TimeSpan.FromDays(7), true));
            Assert.IsFalse(UiStatusLogic.ShowRestartReminder(TimeSpan.FromDays(7), false));
            Assert.IsFalse(UiStatusLogic.ShowRestartReminder(TimeSpan.FromSeconds(-1), true));
        }

        [TestMethod]
        public void CaptureMetadata_UsesSnapshotTimeAndHonestUnknownZone()
        {
            var captured = new DateTime(2026, 10, 17, 12, 34, 56);
            Assert.AreEqual("Timezone: Cairo  |  Captured: 2026-10-17 12:34:56", UiStatusLogic.CaptureMetadata("Cairo", captured));
            StringAssert.Contains(UiStatusLogic.CaptureMetadata(null, captured), "Timezone: Unknown");
            // Refresh states never supply a new clock time or alter capture metadata.
            var before = UiStatusLogic.CaptureMetadata("Cairo", captured);
            UiStatusLogic.CollectionState(true, false, true, 0);
            UiStatusLogic.CollectionState(false, true, true, 0);
            Assert.AreEqual(before, UiStatusLogic.CaptureMetadata("Cairo", captured));
        }

        [TestMethod]
        public void CollectionStates_KeepFailuresAndWarningsDistinctFromMetadata()
        {
            Assert.AreEqual("Refreshing...", UiStatusLogic.CollectionState(true, false, true, 2));
            Assert.AreEqual("Refresh failed", UiStatusLogic.CollectionState(false, true, true, 0));
            Assert.AreEqual("Collection failed", UiStatusLogic.CollectionState(false, true, false, 0));
            Assert.AreEqual("1 unavailable read", UiStatusLogic.CollectionState(false, false, true, 1));
            Assert.AreEqual("2 unavailable reads", UiStatusLogic.CollectionState(false, false, true, 2));
            Assert.AreEqual(string.Empty, UiStatusLogic.CollectionState(false, false, true, 0));
        }
    }
}
