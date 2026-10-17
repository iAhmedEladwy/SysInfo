using System;
using System.Text.RegularExpressions;

namespace CNSysInfo
{
    /// <summary>
    /// Windows 10 and Windows 11 both report version 10.0. The only reliable discriminator is the build
    /// number: Windows 11 starts at build 22000. Never rely on the Caption string or on GetVersionEx-style
    /// APIs (they are manifest dependent and Caption was wrong on early Windows 11 builds).
    /// </summary>
    internal static class WindowsVersionInfo
    {
        public const int Windows11MinBuild = 22000;

        /// <summary>"Windows 11", "Windows 10", or "Windows" when the build is unknown or the major version is not 10.</summary>
        public static string GetProductName(int majorVersion, int buildNumber)
        {
            if (majorVersion != 10 || buildNumber <= 0)
            {
                return "Windows";
            }

            return buildNumber >= Windows11MinBuild ? "Windows 11" : "Windows 10";
        }

        public static bool TryParseBuild(string buildNumber, out int build)
        {
            build = 0;
            if (string.IsNullOrWhiteSpace(buildNumber))
            {
                return false;
            }

            // Registry CurrentBuild / WMI BuildNumber are plain integers. Accept "10.0.22631" as well.
            var text = buildNumber.Trim();
            var parts = text.Split('.');
            var candidate = parts.Length >= 3 ? parts[2] : parts[0];
            return int.TryParse(candidate, out build) && build > 0;
        }

        /// <summary>
        /// Edition text from WMI Caption, e.g. "Microsoft Windows 10 Pro" -> "Pro".
        /// Returns empty when the caption has no recognisable edition.
        /// </summary>
        public static string GetEdition(string caption)
        {
            if (string.IsNullOrWhiteSpace(caption))
            {
                return string.Empty;
            }

            var match = Regex.Match(caption, @"Windows\s+(?:\d+(?:\.\d+)?|Server\s+\d+\S*|Vista|XP)\s*(.*)$", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
        }

        /// <summary>
        /// "Windows 11 Pro 64-bit, Version 23H2 (Build 22631)". Missing parts are skipped rather than printed as "Unknown".
        /// </summary>
        public static string BuildDisplay(string caption, int majorVersion, int buildNumber, string architecture, string displayVersion)
        {
            var name = GetProductName(majorVersion, buildNumber);
            var edition = GetEdition(caption);
            var text = string.IsNullOrEmpty(edition) ? name : name + " " + edition;

            if (!string.IsNullOrWhiteSpace(architecture))
            {
                text += " " + architecture.Trim();
            }

            if (!string.IsNullOrWhiteSpace(displayVersion))
            {
                text += " Version " + displayVersion.Trim();
            }

            if (buildNumber > 0)
            {
                text += " (Build " + buildNumber + ")";
            }

            return text;
        }
    }
}
