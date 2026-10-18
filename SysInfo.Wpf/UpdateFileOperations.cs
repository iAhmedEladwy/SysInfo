using System;
using System.IO;

namespace CNSysInfo
{
    internal static class UpdateFileOperations
    {
        public static void Install(string staged, string target, string backup)
        {
            if (File.Exists(target))
            {
                // The backup may live on another drive. ReplaceFile requires its
                // replacement and destination on one volume, so copy the backup first.
                File.Copy(target, backup, true);
                File.Replace(staged, target, null);
            }
            else File.Move(staged, target);
        }

        public static void Restore(string backup, string target)
        {
            string temporary = target + "." + Guid.NewGuid().ToString("N") + ".rollback";
            try
            {
                File.Copy(backup, temporary);
                if (File.Exists(target)) File.Replace(temporary, target, null);
                else File.Move(temporary, target);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
}
