using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CNSysInfo.Tests
{
    [TestClass]
    public class LivePowerTests
    {
        [TestMethod] public void ChargeIsIndependentOfHealthAndIncludesZero()
        {
            Assert.AreEqual("72% · Charging", LivePowerState.FromNative(true, 1, 8, 72).Text);
            Assert.AreEqual("0% · On battery", LivePowerState.FromNative(true, 0, 4, 0).Text);
            Assert.AreEqual("100% · Fully charged · Plugged in", LivePowerState.FromNative(true, 1, 1, 100).Text);
        }
        [TestMethod] public void PluggedInDoesNotMeanCharging()
        {
            var state = LivePowerState.FromNative(true, 1, 1, 80);
            Assert.AreEqual(false, state.Charging);
            Assert.AreEqual("80% · Plugged in · Not charging", state.Text);
        }
        [TestMethod] public void UnknownFlagsAreNotNoBatteryOrCharging()
        {
            var state = LivePowerState.FromNative(true, 255, 255, 255);
            Assert.IsNull(state.BatteryPresent);
            Assert.IsNull(state.Charging);
            Assert.IsNull(state.Charge);
            Assert.AreEqual("Charge unknown · Power status unknown", state.Text);
            Assert.AreEqual("Live power unavailable", LivePowerState.FromNative(false, 0, 0, 0).Text);
        }
        [TestMethod] public void DesktopBatteryFlagOverridesMisleadingPercentage()
        {
            var state = LivePowerState.FromNative(true, 1, 128, 100);
            Assert.AreEqual(false, state.BatteryPresent);
            Assert.IsNull(state.Charge);
            Assert.AreEqual("No battery detected · AC power", state.Text);
        }
        [TestMethod] public void CapacityIsVisibleAndDoesNotReplaceChargeOrHealth()
        {
            Assert.AreEqual("Design 67,997 / Full 25,612 mWh", BatteryMapper.CapacityText(67997, 25612));
            Assert.AreEqual("Full 25,612 mWh", BatteryMapper.CapacityText(null, 25612));
            Assert.AreEqual("", BatteryMapper.CapacityText(0, null));
        }
        [TestMethod] public void UnknownChargeDoesNotBecomeZero()
        {
            Assert.AreEqual("Charge unknown · Charging", LivePowerState.FromNative(true, 1, 8, 255).Text);
            Assert.AreEqual("Charge unknown · On battery", BatteryMapper.ChargeAndPowerText(null, "On battery"));
        }
    }
}
