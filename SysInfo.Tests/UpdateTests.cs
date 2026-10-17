using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CNSysInfo.Tests
{
    [TestClass]
    public class UpdateTests
    {
        private static GitHubRelease Release(string version = "2.2.3", bool standalone = false)
        {
            var name = UpdateLogic.PackageName(version, standalone);
            return new GitHubRelease { Tag = "v" + version, Assets = new[] { new GitHubAsset { Name = name, Size = 1234,
                Url = "https://github.com/" + UpdateLogic.Repository + "/releases/download/v" + version + "/" + name,
                Digest = "sha256:" + new string('a', 64) } } };
        }

        [TestMethod]
        public void StableNewerMatchingPackageIsOffered()
        {
            var update = UpdateLogic.Find(Release(), new Version(2, 2, 2, 0), "", false);
            Assert.IsNotNull(update);
            Assert.AreEqual("2.2.3", update.VersionText);
            Assert.AreEqual("SysInfo-WPF-v2.2.3.zip", update.PackageName);
            Assert.IsNull(UpdateLogic.Find(Release(), new Version(2, 2, 2), "", true));
            Assert.IsNotNull(UpdateLogic.Find(Release(standalone: true), new Version(2, 2, 2), "", true));
        }

        [TestMethod]
        public void CurrentOlderSkippedAndPrereleaseVersionsStaySilent()
        {
            Assert.IsNull(UpdateLogic.Find(Release(), new Version(2, 2, 3), "", false));
            Assert.IsNull(UpdateLogic.Find(Release(), new Version(2, 2, 4), "", false));
            Assert.IsNull(UpdateLogic.Find(Release(), new Version(2, 2, 2), "v2.2.3", false));
            var release = Release(); release.Prerelease = true;
            Assert.IsNull(UpdateLogic.Find(release, new Version(2, 2, 2), "", false));
            release.Prerelease = false; release.Draft = true;
            Assert.IsNull(UpdateLogic.Find(release, new Version(2, 2, 2), "", false));
            release.Draft = false; release.Tag = "v2.2.3-beta";
            Assert.IsNull(UpdateLogic.Find(release, new Version(2, 2, 2), "", false));
        }

        [TestMethod]
        public void InvalidMissingOrWrongSourceArtifactsAreRejected()
        {
            var release = Release(); release.Assets[0].Digest = null;
            Assert.IsNull(UpdateLogic.Find(release, new Version(2, 2, 2), "", false));
            release = Release(); release.Assets[0].Url = release.Assets[0].Url.Replace("github.com", "github.com.evil.invalid");
            Assert.IsNull(UpdateLogic.Find(release, new Version(2, 2, 2), "", false));
            release = Release(); release.Assets[0].Size = UpdateLogic.MaximumPackageBytes + 1;
            Assert.IsNull(UpdateLogic.Find(release, new Version(2, 2, 2), "", false));
            Assert.IsNull(UpdateLogic.Find(null, new Version(2, 2, 2), "", false));
            Assert.IsNull(UpdateLogic.ParseVersion("v2.2"));
        }

        [TestMethod]
        public void CacheUnitsAreKilobytesAndUnavailableIsNotZero()
        {
            Assert.AreEqual("L2: 1.5 MB | L3: 9 MB", Presentation.CpuCacheText(1536, 9216));
            Assert.AreEqual("L2: Unknown | L3: Unknown", Presentation.CpuCacheText(null, 0));
        }
    }
}
