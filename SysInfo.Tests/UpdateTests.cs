using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CNSysInfo.Tests
{
    [TestClass]
    public class UpdateTests
    {
        private static GitHubRelease Release(string version = "2.2.3")
        {
            var name = UpdateLogic.PackageName(version);
            return new GitHubRelease { Tag = "v" + version, Assets = new[] { new GitHubAsset { Name = name, Size = 1234,
                Url = "https://github.com/" + UpdateLogic.Repository + "/releases/download/v" + version + "/" + name,
                Digest = "sha256:" + new string('a', 64) } } };
        }

        [TestMethod]
        public void StableNewerMatchingPackageIsOffered()
        {
            var update = UpdateLogic.Find(Release(), new Version(2, 2, 2, 0), "");
            Assert.IsNotNull(update);
            Assert.AreEqual("2.2.3", update.VersionText);
            Assert.AreEqual("SysInfo-v2.2.3.exe", update.PackageName);
        }

        [TestMethod]
        public void RetiredStandaloneArtifactIsNotOffered()
        {
            var release = Release();
            var asset = release.Assets[0];
            string retiredName = "SysInfo-Standalone-win-x64-v2.2.3.zip";
            asset.Url = asset.Url.Replace(asset.Name, retiredName);
            asset.Name = retiredName;
            Assert.IsNull(UpdateLogic.Find(release, new Version(2, 2, 2), ""));
        }

        [TestMethod]
        public void CurrentOlderSkippedAndPrereleaseVersionsStaySilent()
        {
            Assert.IsNull(UpdateLogic.Find(Release(), new Version(2, 2, 3), ""));
            Assert.IsNull(UpdateLogic.Find(Release(), new Version(2, 2, 4), ""));
            Assert.IsNull(UpdateLogic.Find(Release(), new Version(2, 2, 2), "v2.2.3"));
            var release = Release(); release.Prerelease = true;
            Assert.IsNull(UpdateLogic.Find(release, new Version(2, 2, 2), ""));
            release.Prerelease = false; release.Draft = true;
            Assert.IsNull(UpdateLogic.Find(release, new Version(2, 2, 2), ""));
            release.Draft = false; release.Tag = "v2.2.3-beta";
            Assert.IsNull(UpdateLogic.Find(release, new Version(2, 2, 2), ""));
        }

        [TestMethod]
        public void InvalidMissingOrWrongSourceArtifactsAreRejected()
        {
            var release = Release(); release.Assets[0].Url = release.Assets[0].Url.Replace("github.com", "github.com.evil.invalid");
            Assert.IsNull(UpdateLogic.Find(release, new Version(2, 2, 2), ""));
            release = Release(); release.Assets[0].Size = UpdateLogic.MaximumPackageBytes + 1;
            Assert.IsNull(UpdateLogic.Find(release, new Version(2, 2, 2), ""));
            Assert.IsNull(UpdateLogic.Find(null, new Version(2, 2, 2), ""));
            Assert.IsNull(UpdateLogic.ParseVersion("v2.2"));
        }

        [TestMethod]
        public void ManualDownloadUsesOnlyTheOfficialReleasePage()
        {
            var release = Release(); release.Assets[0].Digest = null;
            var update = UpdateLogic.Find(release, new Version(2, 2, 2), "");
            Assert.IsNotNull(update); // The browser downloads; the app no longer verifies or executes binaries.
            Assert.AreEqual("https://github.com/iAhmedEladwy/SysInfo/releases/tag/v2.2.3", update.ReleaseUrl);
            Assert.IsNull(UpdateLogic.ReleasePageUrl("https://evil.invalid/v2.2.3"));
            Assert.IsNull(UpdateLogic.ReleasePageUrl("../../v2.2.3"));
            Assert.IsNull(UpdateLogic.ReleasePageUrl("v2.2.3?download=evil"));
        }
        [TestMethod]
        public void CacheUnitsAreKilobytesAndUnavailableIsNotZero()
        {
            Assert.AreEqual("L2: 1.5 MB | L3: 9 MB", Presentation.CpuCacheText(1536, 9216));
            Assert.AreEqual("L2: Unknown | L3: Unknown", Presentation.CpuCacheText(null, 0));
        }
    }
}
