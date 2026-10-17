using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CNSysInfo
{
    // CpuSupportLevel enum moved to SpecRating.cs (so rating logic is testable without WMI/WinForms)

    /// <summary>
    /// Heuristic check of the CPU against Microsoft's Windows 11 processor requirements
    /// (Intel 8th gen and newer, Ryzen 2000 and newer, and so on). It looks at the CPU name first and only
    /// uses the CPUID model when the name gives no answer. Anything it cannot decide is Unknown, never Supported.
    /// It does NOT check TPM 2.0, Secure Boot, RAM or disk requirements.
    /// </summary>
    public static partial class CpuSupportEvaluator
    {
        // Intel CPUID family 6 models that map to exactly one generation. Models shared by several
        // generations (0x8E: Kaby Lake R / Coffee Lake U, 0x9E: Kaby Lake / Coffee Lake desktop) are left out on purpose.
        private static readonly HashSet<int> IntelSupportedModels = new HashSet<int>
        {
            0x55, // Skylake-SP / Cascade Lake Xeon Scalable
            0x6A, 0x6C, // Ice Lake-SP
            0x7D, 0x7E, // Ice Lake client
            0x8C, 0x8D, // Tiger Lake
            0x8F, // Sapphire Rapids
            0x97, 0x9A, // Alder Lake
            0x9C, // Jasper Lake
            0xA5, 0xA6, // Comet Lake
            0xA7, // Rocket Lake
            0xAA, 0xAC, // Meteor Lake
            0xAF, // Sierra Forest
            0xB7, 0xBA, 0xBF, // Raptor Lake
            0xBD, // Lunar Lake
            0xC5, 0xC6 // Arrow Lake
        };

        private static readonly HashSet<int> IntelUnsupportedModels = new HashSet<int>
        {
            0x1A, 0x1E, 0x1F, 0x25, 0x2C, 0x2E, 0x2F, // Nehalem / Westmere
            0x2A, 0x2D, // Sandy Bridge
            0x3A, 0x3E, // Ivy Bridge
            0x3C, 0x3F, 0x45, 0x46, // Haswell
            0x3D, 0x47, 0x4F, 0x56, // Broadwell
            0x4E, 0x5E // Skylake client
        };

        public static CpuSupportLevel Evaluate(string name, string manufacturer, int cpuidFamily, int cpuidModel)
        {
            name = name ?? string.Empty;
            manufacturer = manufacturer ?? string.Empty;

            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(manufacturer))
            {
                return CpuSupportLevel.Unknown;
            }

            var isAmd = Contains(manufacturer, "AuthenticAMD") || Contains(manufacturer, "AMD") ||
                        Contains(name, "AMD") || Contains(name, "Ryzen") || Contains(name, "EPYC");
            var isIntel = Contains(manufacturer, "Intel") || Contains(name, "Intel");

            if (isIntel && !isAmd)
            {
                return EvaluateIntel(name, cpuidFamily, cpuidModel);
            }

            if (isAmd && !isIntel)
            {
                return EvaluateAmd(name);
            }

            return CpuSupportLevel.Unknown;
        }

        private static bool Contains(string text, string value)
        {
            return text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static CpuSupportLevel EvaluateIntel(string name, int family, int model)
        {
            // "Core Ultra 7 155H", "Core(TM) 5 120U" (2024 naming without the i-prefix).
            if (Regex.IsMatch(name, @"Core(?:\(TM\))?\s+Ultra\b", RegexOptions.IgnoreCase) ||
                Regex.IsMatch(name, @"Core(?:\(TM\))?\s+[3579]\s+\d{3}", RegexOptions.IgnoreCase))
            {
                return CpuSupportLevel.Supported;
            }

            // Alder Lake-N: "Core i3-N305", "Intel N100".
            if (Regex.IsMatch(name, @"\bi[3579]-N\d{3}\b", RegexOptions.IgnoreCase) ||
                Regex.IsMatch(name, @"Intel\(R\)\s+(?:Processor\s+)?N\d{3}\b", RegexOptions.IgnoreCase))
            {
                return CpuSupportLevel.Supported;
            }

            var core = Regex.Match(name, @"\bi[3579]-(\d{3,5})([A-Za-z]*\d?[A-Za-z]*)", RegexOptions.IgnoreCase);
            if (core.Success)
            {
                var digits = core.Groups[1].Value;
                int generation;
                switch (digits.Length)
                {
                    case 3:
                        generation = 1; // i7-920, i5-750, i3-540
                        break;
                    case 4:
                        // 10th gen and newer mobile parts use four digits starting 10-15 (i7-1165G7, i3-1005G1).
                        generation = digits[0] == '1' && digits[1] >= '0' && digits[1] <= '5' && core.Groups[2].Value.Length > 0 && char.IsLetter(core.Groups[2].Value[0])
                            ? int.Parse(digits.Substring(0, 2), CultureInfo.InvariantCulture)
                            : digits[0] - '0';
                        break;
                    default:
                        generation = int.Parse(digits.Substring(0, 2), CultureInfo.InvariantCulture);
                        break;
                }

                if (generation >= 8)
                {
                    return CpuSupportLevel.Supported;
                }

                // Microsoft lists the Core i7-7820HQ (Surface Studio 2) as the only 7th gen exception.
                if (digits == "7820" && Contains(name, "7820HQ"))
                {
                    return CpuSupportLevel.Supported;
                }

                return CpuSupportLevel.Unsupported;
            }

            if (Contains(name, "Xeon"))
            {
                if (Regex.IsMatch(name, @"Xeon\(R\)\s+(Platinum|Gold|Silver|Bronze)\s+\d{4}", RegexOptions.IgnoreCase) ||
                    Regex.IsMatch(name, @"Xeon\(R\)\s+W-?(2\d{3}|3\d{3})", RegexOptions.IgnoreCase))
                {
                    return CpuSupportLevel.Supported;
                }

                var e = Regex.Match(name, @"Xeon\(R\)\s+(?:CPU\s+)?E-(\d{4})", RegexOptions.IgnoreCase);
                if (e.Success)
                {
                    return int.Parse(e.Groups[1].Value, CultureInfo.InvariantCulture) >= 2100
                        ? CpuSupportLevel.Supported
                        : CpuSupportLevel.Unsupported;
                }

                // E3/E5/E7 v1-v4 (pre-Skylake) are unsupported.
                if (Regex.IsMatch(name, @"\bE[357]-\d{4}\s*(v[1-4])?", RegexOptions.IgnoreCase) &&
                    Regex.IsMatch(name, @"\bv[1-4]\b", RegexOptions.IgnoreCase))
                {
                    return CpuSupportLevel.Unsupported;
                }
            }

            // Pentium/Celeron: only the clearly decidable patterns.
            var small = Regex.Match(name, @"\b([NJ])(\d)(\d{3})\b");
            if (small.Success && (Contains(name, "Celeron") || Contains(name, "Pentium")))
            {
                return int.Parse(small.Groups[2].Value, CultureInfo.InvariantCulture) >= 4
                    ? CpuSupportLevel.Supported
                    : CpuSupportLevel.Unsupported;
            }

            var gSeries = Regex.Match(name, @"\bG(\d{2})(\d{2})\b");
            if (gSeries.Success && (Contains(name, "Celeron") || Contains(name, "Pentium")))
            {
                return int.Parse(gSeries.Groups[1].Value, CultureInfo.InvariantCulture) >= 49
                    ? CpuSupportLevel.Supported
                    : CpuSupportLevel.Unsupported;
            }

            // No usable name: fall back to CPUID, but only for models that identify a single generation.
            if (family == 6 && model > 0)
            {
                if (IntelSupportedModels.Contains(model))
                {
                    return CpuSupportLevel.Supported;
                }

                if (IntelUnsupportedModels.Contains(model))
                {
                    return CpuSupportLevel.Unsupported;
                }
            }

            return CpuSupportLevel.Unknown;
        }

        private static CpuSupportLevel EvaluateAmd(string name)
        {
            // EPYC model numbers: last digit is the generation (7551 = gen 1, 7402 = gen 2, 7543 = gen 3, 9654 = gen 4).
            var epyc = Regex.Match(name, @"EPYC\s+(\d{4})", RegexOptions.IgnoreCase);
            if (epyc.Success)
            {
                var lastDigit = epyc.Groups[1].Value[3] - '0';
                return lastDigit >= 2 ? CpuSupportLevel.Supported : CpuSupportLevel.Unsupported;
            }

            if (Regex.IsMatch(name, @"Ryzen\s+(?:\S+\s+)*?Z[12]\b", RegexOptions.IgnoreCase))
            {
                return CpuSupportLevel.Supported;
            }

            var ryzen = Contains(name, "Ryzen");
            if (ryzen)
            {
                // Ignore tier digit (3/5/7/9) and words such as AI, PRO, Threadripper; take the first model token.
                var tokens = Regex.Matches(name.Substring(name.IndexOf("Ryzen", StringComparison.OrdinalIgnoreCase) + 5), @"[A-Za-z0-9\-]+");
                var skippedTier = false;
                foreach (Match token in tokens)
                {
                    var value = token.Value;
                    if (!skippedTier && value.Length == 1 && char.IsDigit(value[0]))
                    {
                        skippedTier = true;
                        continue;
                    }

                    var model = Regex.Match(value, @"^(\d{3,4})[A-Za-z0-9]*$");
                    if (!model.Success)
                    {
                        continue;
                    }

                    var number = model.Groups[1].Value;
                    if (number.Length == 3)
                    {
                        // Ryzen AI 300 series and similar three digit names are all new generations.
                        return Contains(name, "Ryzen AI") ? CpuSupportLevel.Supported : CpuSupportLevel.Unknown;
                    }

                    return number[0] >= '2' ? CpuSupportLevel.Supported : CpuSupportLevel.Unsupported;
                }

                return CpuSupportLevel.Unknown;
            }

            var athlon = Regex.Match(name, @"Athlon(?:\(tm\))?\s+(?:Gold\s+|Silver\s+|PRO\s+)?(\d{4})", RegexOptions.IgnoreCase);
            if (athlon.Success)
            {
                return athlon.Groups[1].Value[0] >= '3' ? CpuSupportLevel.Supported : CpuSupportLevel.Unsupported;
            }

            if (Regex.IsMatch(name, @"\b(Phenom|Sempron|Opteron|Athlon(?:\(tm\))?\s+(?:II|64|X[234]|XP)|FX(?:\(tm\))?-?\s?\d|A\d{1,2}-\d{4})", RegexOptions.IgnoreCase))
            {
                return CpuSupportLevel.Unsupported;
            }

            return CpuSupportLevel.Unknown;
        }
    }
}
