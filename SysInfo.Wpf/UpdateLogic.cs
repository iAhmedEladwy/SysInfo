using System;
using System.Linq;
using System.Runtime.Serialization;

namespace CNSysInfo
{
    [DataContract]
    internal sealed class GitHubRelease
    {
        [DataMember(Name = "tag_name")] public string Tag { get; set; }
        [DataMember(Name = "draft")] public bool Draft { get; set; }
        [DataMember(Name = "prerelease")] public bool Prerelease { get; set; }
        [DataMember(Name = "assets")] public GitHubAsset[] Assets { get; set; }
    }

    [DataContract]
    internal sealed class GitHubAsset
    {
        [DataMember(Name = "name")] public string Name { get; set; }
        [DataMember(Name = "browser_download_url")] public string Url { get; set; }
        [DataMember(Name = "size")] public long Size { get; set; }
        [DataMember(Name = "digest")] public string Digest { get; set; }
    }

    internal sealed class AppUpdate
    {
        public string VersionText;
        public string PackageName;
        public string PackageUrl;
        public long PackageSize;
        public string Sha256;
        public string ChecksumUrl;
    }

    internal static class UpdateLogic
    {
        public const string Repository = "iAhmedEladwy/SysInfo";
        public const long MaximumPackageBytes = 200L * 1024 * 1024;
        public static string PackageName(string version, bool standalone) =>
            standalone ? "SysInfo-Standalone-win-x64-v" + version + ".zip" : "SysInfo-WPF-v" + version + ".zip";

        public static Version ParseVersion(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            if (text.StartsWith("v", StringComparison.OrdinalIgnoreCase)) text = text.Substring(1);
            if (!Version.TryParse(text, out var value) || value.Major < 0 || value.Minor < 0 || value.Build < 0) return null;
            return new Version(value.Major, value.Minor, value.Build, Math.Max(0, value.Revision));
        }

        public static bool ValidDownloadUrl(string url, string tag, string assetName)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == "https" &&
                uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) && uri.IsDefaultPort &&
                uri.UserInfo.Length == 0 && uri.Query.Length == 0 && uri.Fragment.Length == 0 &&
                uri.AbsolutePath == "/" + Repository + "/releases/download/" + Uri.EscapeDataString(tag) + "/" + assetName;
        }

        public static bool ValidHash(string hash) => hash != null && hash.Length == 64 &&
            hash.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'));

        public static AppUpdate Find(GitHubRelease release, Version current, string skipped, bool standalone)
        {
            var version = ParseVersion(release?.Tag);
            if (release == null || release.Draft || release.Prerelease || version == null || current == null ||
                version <= ParseVersion(current.ToString()) || version == ParseVersion(skipped)) return null;
            string versionText = version.ToString(3);
            string name = PackageName(versionText, standalone);
            var asset = release.Assets?.SingleOrDefault(a => a.Name == name);
            if (asset == null || asset.Size <= 0 || asset.Size > MaximumPackageBytes || !ValidDownloadUrl(asset.Url, release.Tag, name)) return null;
            string hash = asset.Digest?.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase) == true ? asset.Digest.Substring(7) : null;
            var checksum = release.Assets.SingleOrDefault(a => a.Name == name + ".sha256");
            string checksumUrl = checksum != null && checksum.Size > 0 && checksum.Size <= 1024 &&
                ValidDownloadUrl(checksum.Url, release.Tag, checksum.Name) ? checksum.Url : null;
            if (!ValidHash(hash) && checksumUrl == null) return null;
            return new AppUpdate { VersionText = versionText, PackageName = name, PackageUrl = asset.Url,
                PackageSize = asset.Size, Sha256 = ValidHash(hash) ? hash : null, ChecksumUrl = checksumUrl };
        }
    }
}
