using System;
using System.Globalization;

namespace CNSysInfo
{
    /// <summary>
    /// CPUID-based family/model/stepping, decoded from the Win32_Processor.ProcessorId string.
    /// Win32_Processor.Family is an SMBIOS enum (for example 198 = "Core i7") and Win32_Processor has
    /// no "Model" property, so the real CPUID values must come from ProcessorId (EDX then EAX, 16 hex chars).
    /// </summary>
    internal static class CpuId
    {
        public struct Signature
        {
            public bool IsValid;
            public int Family;
            public int Model;
            public int Stepping;
        }

        public static Signature Parse(string processorId)
        {
            var result = new Signature();
            if (string.IsNullOrWhiteSpace(processorId))
            {
                return result;
            }

            var text = processorId.Trim();
            if (text.Length != 16)
            {
                return result;
            }

            // Layout: first 8 chars = EDX (feature flags), last 8 chars = EAX (signature).
            if (!uint.TryParse(text.Substring(8, 8), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var eax))
            {
                return result;
            }

            var stepping = (int)(eax & 0xF);
            var baseModel = (int)((eax >> 4) & 0xF);
            var baseFamily = (int)((eax >> 8) & 0xF);
            var extModel = (int)((eax >> 16) & 0xF);
            var extFamily = (int)((eax >> 20) & 0xFF);

            var family = baseFamily == 0xF ? baseFamily + extFamily : baseFamily;
            var model = (baseFamily == 0x6 || baseFamily == 0xF) ? (extModel << 4) + baseModel : baseModel;

            result.IsValid = true;
            result.Family = family;
            result.Model = model;
            result.Stepping = stepping;
            return result;
        }
    }
}
