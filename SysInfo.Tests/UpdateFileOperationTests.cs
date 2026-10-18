using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CNSysInfo.Tests
{
    [TestClass]
    public class UpdateFileOperationTests
    {
        private string root;
        private string staged, target, backup;

        [TestInitialize]
        public void SetUp()
        {
            root = Path.Combine(Path.GetTempPath(), "SysInfo-FileOps-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            Directory.CreateDirectory(Path.Combine(root, "backup"));
            staged = Path.Combine(root, "new.exe");
            target = Path.Combine(root, "My App.exe");
            backup = Path.Combine(root, "backup", "My App.exe");
            File.WriteAllText(staged, "new version");
        }

        [TestCleanup]
        public void CleanUp()
        {
            string prefix = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (root != null && Path.GetFullPath(root).StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                Path.GetFileName(root).StartsWith("SysInfo-FileOps-", StringComparison.Ordinal)) Directory.Delete(root, true);
        }

        [TestMethod]
        public void InstallBacksUpOriginalAndRestoreReplacesAtomically()
        {
            File.WriteAllText(target, "original");
            UpdateFileOperations.Install(staged, target, backup);
            Assert.AreEqual("new version", File.ReadAllText(target));
            Assert.AreEqual("original", File.ReadAllText(backup));
            Assert.IsFalse(File.Exists(staged));
            UpdateFileOperations.Restore(backup, target);
            Assert.AreEqual("original", File.ReadAllText(target));
            Assert.AreEqual(0, Directory.GetFiles(root, "*.rollback").Length);
        }

        [TestMethod]
        public void NewTargetInstallsWithoutAnOriginalBackup()
        {
            UpdateFileOperations.Install(staged, target, backup);
            Assert.AreEqual("new version", File.ReadAllText(target));
            Assert.IsFalse(File.Exists(backup));
        }

        [TestMethod]
        public void BackupFailureLeavesOriginalAndStagedFilesIntact()
        {
            File.WriteAllText(target, "original");
            bool failed = false;
            try { UpdateFileOperations.Install(staged, target, Path.GetDirectoryName(backup)); }
            catch (IOException) { failed = true; }
            catch (UnauthorizedAccessException) { failed = true; }
            Assert.IsTrue(failed);
            Assert.AreEqual("original", File.ReadAllText(target));
            Assert.AreEqual("new version", File.ReadAllText(staged));
        }

        [TestMethod]
        public void RestoreAlsoHandlesAMissingTarget()
        {
            File.WriteAllText(backup, "original");
            UpdateFileOperations.Restore(backup, target);
            Assert.AreEqual("original", File.ReadAllText(target));
            Assert.AreEqual(0, Directory.GetFiles(root, "*.rollback").Length);
        }
    }
}
