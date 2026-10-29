using System;
using System.Linq;
using System.IO;
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
        public string ReleaseUrl;
    }

    internal static class UpdateLogic
    {
        public const string Repository = "iAhmedEladwy/SysInfo";
        public const long MaximumPackageBytes = 200L * 1024 * 1024;
        public static string PackageName(string version) =>
            "SysInfo-v" + version + ".exe";

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

        public static string ReleasePageUrl(string tag)
        {
            if (ParseVersion(tag) == null) return null;
            return "https://github.com/" + Repository + "/releases/tag/" + Uri.EscapeDataString(tag);
        }
        public static AppUpdate Find(GitHubRelease release, Version current, string skipped)
        {
            var version = ParseVersion(release?.Tag);
            if (release == null || release.Draft || release.Prerelease || version == null || current == null ||
                version <= ParseVersion(current.ToString()) || version == ParseVersion(skipped)) return null;
            string versionText = version.ToString(3);
            string name = PackageName(versionText);
            var asset = release.Assets?.SingleOrDefault(a => a.Name == name);
            if (asset == null || asset.Size <= 0 || asset.Size > MaximumPackageBytes || !ValidDownloadUrl(asset.Url, release.Tag, name)) return null;
            return new AppUpdate { VersionText = versionText, PackageName = name, ReleaseUrl = ReleasePageUrl(release.Tag) };
        }
    }
}
