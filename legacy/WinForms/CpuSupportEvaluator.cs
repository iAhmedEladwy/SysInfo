using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CNSysInfo
{
    public enum CpuSupportLevel
    {
        Unknown,
        Supported,
        Unsupported
    }

    public static class CpuSupportEvaluator
    {
        private static readonly HashSet<string> IntelWhitelistedDigits = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "7820"
        };

        private static readonly HashSet<int> IntelSupportedModels = new HashSet<int>
        {
            0x8C, // Tiger Lake-U (11th gen)
            0x8D, // Tiger Lake (11th gen)
            0x8E, // Kaby Lake R (treat cautiously, only whitelisted)
            0x8F, // Tiger Lake
            0x9C, // Ice Lake
            0x9D,
            0x9E, // Coffee Lake (8th/9th gen)
            0xA5, // Comet Lake
            0xA6, // Comet Lake
            0xA7, // Comet Lake
            0xA8, // Rocket Lake
            0xA9, // Rocket Lake
            0xAA,
            0xAB,
            0xAC,
            0xAD,
            0xAE,
            0xAF,
            0xB7, // Alder Lake
            0xB8, // Alder Lake
            0xB9,
            0xBA,
            0xBB,
            0xBC,
            0xBD,
            0xBE,
            0xBF,
            0xC0, // Raptor Lake
            0xC1,
            0xC2,
            0xC3,
            0xC4,
            0xC5,
            0xC6,
            0xC7,
            0xCF // Meteor Lake (Core Ultra)
        };

        internal static CpuSupportLevel Evaluate(SystemInfoSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return CpuSupportLevel.Unknown;
            }

            var name = snapshot.Cpu ?? string.Empty;
            var manufacturer = snapshot.CpuManufacturer ?? string.Empty;

            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(manufacturer))
            {
                return CpuSupportLevel.Unknown;
            }

            if (manufacturer.IndexOf("Intel", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Intel", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return EvaluateIntel(snapshot);
            }

            if (manufacturer.IndexOf("AMD", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("AMD", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Ryzen", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("EPYC", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Athlon", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return EvaluateAmd(snapshot);
            }

            return CpuSupportLevel.Unknown;
        }

        private static CpuSupportLevel EvaluateIntel(SystemInfoSnapshot snapshot)
        {
            var name = snapshot.Cpu ?? string.Empty;

            if (name.IndexOf("Core", StringComparison.OrdinalIgnoreCase) >= 0 &&
                name.IndexOf("Ultra", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return CpuSupportLevel.Supported;
            }

            var match = Regex.Match(name, @"i[3579]-?(\d{3,5})", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var digits = match.Groups[1].Value;
                if (digits.Length >= 4)
                {
                    if (digits.Length == 4)
                    {
                        int generation;
                        if (digits.StartsWith("10", StringComparison.Ordinal) ||
                            digits.StartsWith("11", StringComparison.Ordinal) ||
                            digits.StartsWith("12", StringComparison.Ordinal) ||
                            digits.StartsWith("13", StringComparison.Ordinal) ||
                            digits.StartsWith("14", StringComparison.Ordinal) ||
                            digits.StartsWith("15", StringComparison.Ordinal))
                        {
                            if (!int.TryParse(digits.Substring(0, 2), NumberStyles.Integer, CultureInfo.InvariantCulture, out generation))
                            {
                                generation = digits[0] - '0';
                            }
                        }
                        else
                        {
                            generation = digits[0] - '0';
                        }

                        if (IntelWhitelistedDigits.Contains(digits) &&
                            name.IndexOf("hq", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            return CpuSupportLevel.Supported;
                        }

                        if (generation >= 8)
                        {
                            return CpuSupportLevel.Supported;
                        }

                        return CpuSupportLevel.Unsupported;
                    }

                    if (digits.Length == 5)
                    {
                        if (int.TryParse(digits.Substring(0, 2), NumberStyles.Integer, CultureInfo.InvariantCulture, out var generation))
                        {
                            if (generation >= 10)
                            {
                                return CpuSupportLevel.Supported;
                            }

                            return CpuSupportLevel.Unsupported;
                        }
                    }
                }
            }

            if (name.IndexOf("Xeon", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var xeonMatch = Regex.Match(name, @"Xeon\s+(?:[WDE]-?)?(\d{3,4})", RegexOptions.IgnoreCase);
                if (xeonMatch.Success && int.TryParse(xeonMatch.Groups[1].Value, out var series))
                {
                    if (series >= 2100 || series >= 3200 || series >= 4100)
                    {
                        return CpuSupportLevel.Supported;
                    }
                }

                if (name.IndexOf("Silver", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Gold", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Platinum", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return CpuSupportLevel.Supported;
                }
            }

            if (snapshot.CpuFamily == 6 && snapshot.CpuModel > 0)
            {
                var model = snapshot.CpuModel;

                if (model == 0x8E)
                {
                    if (name.IndexOf("7820hq", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return CpuSupportLevel.Supported;
                    }

                    return CpuSupportLevel.Unsupported;
                }

                if (IntelSupportedModels.Contains(model))
                {
                    return CpuSupportLevel.Supported;
                }
            }

            return CpuSupportLevel.Unknown;
        }

        private static CpuSupportLevel EvaluateAmd(SystemInfoSnapshot snapshot)
        {
            var name = snapshot.Cpu ?? string.Empty;
            var ryzenMatch = Regex.Match(name, @"Ryzen\s+[0-9]?\s*(\d{3,4})", RegexOptions.IgnoreCase);
            if (ryzenMatch.Success)
            {
                if (int.TryParse(ryzenMatch.Groups[1].Value.Substring(0, 1), out var generation) && generation >= 2)
                {
                    return CpuSupportLevel.Supported;
                }

                return CpuSupportLevel.Unsupported;
            }

            var threadripperMatch = Regex.Match(name, @"Threadripper\s+(\d{3,4})", RegexOptions.IgnoreCase);
            if (threadripperMatch.Success)
            {
                if (int.TryParse(threadripperMatch.Groups[1].Value.Substring(0, 1), out var generation) && generation >= 2)
                {
                    return CpuSupportLevel.Supported;
                }

                return CpuSupportLevel.Unsupported;
            }

            var athlonMatch = Regex.Match(name, @"Athlon\s+(?:Gold|Silver)?\s*(\d{3,4})", RegexOptions.IgnoreCase);
            if (athlonMatch.Success)
            {
                if (int.TryParse(athlonMatch.Groups[1].Value.Substring(0, 1), out var generation) && generation >= 3)
                {
                    return CpuSupportLevel.Supported;
                }

                return CpuSupportLevel.Unsupported;
            }

            var epycMatch = Regex.Match(name, @"EPYC\s+(\d{4})", RegexOptions.IgnoreCase);
            if (epycMatch.Success)
            {
                if (int.TryParse(epycMatch.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var series) && series >= 7002)
                {
                    return CpuSupportLevel.Supported;
                }

                return CpuSupportLevel.Unsupported;
            }

            return CpuSupportLevel.Unknown;
        }
    }
}
