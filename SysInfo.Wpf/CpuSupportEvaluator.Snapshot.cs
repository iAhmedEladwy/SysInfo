namespace CNSysInfo
{
    public static partial class CpuSupportEvaluator
    {
        internal static CpuSupportLevel Evaluate(SystemInfoSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return CpuSupportLevel.Unknown;
            }

            return Evaluate(snapshot.Cpu, snapshot.CpuManufacturer, snapshot.CpuFamily, snapshot.CpuModel);
        }
    }
}
