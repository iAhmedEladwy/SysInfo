using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Json;

namespace CNSysInfo
{
    [DataContract]
    internal sealed class UpdateJob
    {
        [DataMember] public string TargetDirectory;
        [DataMember] public string TargetExecutableName;
        [DataMember] public string StageDirectory;
        [DataMember] public string ExpectedVersion;
        [DataMember] public int ParentPid;
        [DataMember] public long ParentStarted;
        [DataMember] public string[] Files;
    }

    internal static class UpdateInstaller
    {
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern void SHChangeNotify(uint eventId, uint flags, string path, IntPtr unused);

        private static void RefreshExplorerIcon(string path)
        {
            try { SHChangeNotify(0x00002000, 0x0005, path, IntPtr.Zero); }
            catch (Exception ex) { Debug.WriteLine("Explorer icon refresh failed: " + ex.GetType().Name); }
        }

        public static string CacheRoot => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ELADWYSOFT", "SysInfo", "Updates");

        public static string ExecutablePath => Process.GetCurrentProcess().MainModule.FileName;

        public static string StagePackage(string package, string jobDirectory, string expectedVersion)
        {
            string stage = Path.Combine(jobDirectory, "stage");
            Directory.CreateDirectory(stage);
            var download = new FileInfo(package);
            if (!download.Exists || download.Length <= 0 || download.Length > UpdateLogic.MaximumPackageBytes)
                throw new InvalidDataException("Invalid update executable.");
            File.Copy(package, Path.Combine(stage, "SysInfo.exe"));
            var version = UpdateLogic.ParseVersion(FileVersionInfo.GetVersionInfo(Path.Combine(stage, "SysInfo.exe")).FileVersion);
            if (version == null || version != UpdateLogic.ParseVersion(expectedVersion))
                throw new InvalidDataException("The downloaded app version does not match the release.");
            return stage;
        }

        public static void LaunchHelper(string stage, string expectedVersion)
        {
            string executable = ExecutablePath;
            string target = Path.GetDirectoryName(executable);
            string executableName = UpdateLogic.InstalledExecutableName(Path.GetFileName(executable));
            // Check permissions before closing the working app; never request elevation.
            string probe = Path.Combine(target, ".sysinfo-update-" + Guid.NewGuid().ToString("N"));
            File.WriteAllText(probe, string.Empty); File.Delete(probe);
            string root = Path.GetDirectoryName(stage);
            string helperDirectory = Path.Combine(root, "helper");
            Directory.CreateDirectory(helperDirectory);
            string helper = Path.Combine(helperDirectory, "SysInfo.exe");
            File.Copy(executable, helper);
            if (File.Exists(executable + ".config")) File.Copy(executable + ".config", helper + ".config");
            var process = Process.GetCurrentProcess();
            var job = new UpdateJob { TargetDirectory = target, TargetExecutableName = executableName, StageDirectory = stage, ExpectedVersion = expectedVersion,
                ParentPid = process.Id, ParentStarted = process.StartTime.ToUniversalTime().Ticks,
                Files = Directory.GetFiles(stage).Select(Path.GetFileName).ToArray() };
            string spec = Path.Combine(root, "job.json");
            using (var file = File.Create(spec)) new DataContractJsonSerializer(typeof(UpdateJob)).WriteObject(file, job);
            if (Process.Start(new ProcessStartInfo(helper, "--apply-update \"" + spec + "\"")
                { UseShellExecute = false, WorkingDirectory = helperDirectory, CreateNoWindow = true }) == null)
                throw new IOException("Could not start the updater.");
        }

        public static bool TryRun(string[] args)
        {
            if (args.Length == 0 || args[0] != "--apply-update") return false;
            try
            {
                if (args.Length != 2) throw new InvalidDataException("Invalid updater arguments.");
                Apply(args[1]);
            }
            catch (Exception ex)
            {
                Environment.ExitCode = 1;
                System.Windows.MessageBox.Show("The update could not be completed. Your previous version was retained.\n\n" + ex.Message,
                    "SysInfo update", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            }
            return true;
        }

        private static void Apply(string spec)
        {
            UpdateJob job;
            if (new FileInfo(spec).Length > 65536) throw new InvalidDataException("Invalid update job.");
            using (var file = File.OpenRead(spec)) job = (UpdateJob)new DataContractJsonSerializer(typeof(UpdateJob)).ReadObject(file);
            string root = Path.GetDirectoryName(Path.GetFullPath(spec));
            if (!root.StartsWith(Path.GetFullPath(CacheRoot) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(Path.GetFullPath(job.StageDirectory), Path.Combine(root, "stage"), StringComparison.OrdinalIgnoreCase) ||
                job.Files == null || job.Files.Length == 0 || job.Files.Distinct(StringComparer.OrdinalIgnoreCase).Count() != job.Files.Length ||
                job.Files.Any(f => f != "SysInfo.exe" && f != "SysInfo.exe.config") || !job.Files.Contains("SysInfo.exe"))
                throw new InvalidDataException("Invalid update paths or file list.");
            var version = UpdateLogic.ParseVersion(FileVersionInfo.GetVersionInfo(Path.Combine(job.StageDirectory, "SysInfo.exe")).FileVersion);
            if (version == null || version != UpdateLogic.ParseVersion(job.ExpectedVersion)) throw new InvalidDataException("Invalid staged application.");
            string targetExe = Path.Combine(Path.GetFullPath(job.TargetDirectory), UpdateLogic.InstalledExecutableName(job.TargetExecutableName));
            try
            {
                using (var parent = Process.GetProcessById(job.ParentPid))
                {
                    if (parent.StartTime.ToUniversalTime().Ticks != job.ParentStarted ||
                        !parent.MainModule.FileName.Equals(targetExe, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("The running application does not match the update target.");
                    if (!parent.WaitForExit(60000)) throw new IOException("SysInfo is still running. Close it and try again.");
                }
            }
            catch (ArgumentException) { /* Parent has already exited. */ }
            ReplaceFiles(job, Path.Combine(root, "backup"), target =>
                Process.Start(new ProcessStartInfo(target) { UseShellExecute = true, WorkingDirectory = job.TargetDirectory }));
        }

        internal static void ReplaceFiles(UpdateJob job, string backup, Action<string> launch)
        {
            var applied = new List<string>();
            Directory.CreateDirectory(backup);
            try
            {
                foreach (var name in job.Files)
                {
                    string installedName = UpdateLogic.InstalledFileName(name, job.TargetExecutableName);
                    string target = Path.Combine(job.TargetDirectory, installedName);
                    string temporary = target + "." + Guid.NewGuid().ToString("N") + ".new";
                    try
                    {
                        File.Copy(Path.Combine(job.StageDirectory, name), temporary);
                        UpdateFileOperations.Install(temporary, target, Path.Combine(backup, installedName));
                        applied.Add(installedName);
                    }
                    finally { if (File.Exists(temporary)) File.Delete(temporary); }
                }
                RefreshExplorerIcon(Path.Combine(job.TargetDirectory, UpdateLogic.InstalledExecutableName(job.TargetExecutableName)));
                launch(Path.Combine(job.TargetDirectory, UpdateLogic.InstalledExecutableName(job.TargetExecutableName)));
            }
            catch
            {
                foreach (var name in applied)
                {
                    string old = Path.Combine(backup, name), target = Path.Combine(job.TargetDirectory, name);
                    if (File.Exists(old)) UpdateFileOperations.Restore(old, target); else if (File.Exists(target)) File.Delete(target);
                }
                RefreshExplorerIcon(Path.Combine(job.TargetDirectory, UpdateLogic.InstalledExecutableName(job.TargetExecutableName)));
                throw;
            }
        }
    }
}
