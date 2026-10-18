# ELADWYSOFT SysInfo

A compact Windows system-information tool with a light WPF interface, hardware details, CPU cache, storage usage and health, battery health, report export and Windows utilities.

## Download

Get a ZIP from [GitHub Releases](https://github.com/iAhmedEladwy/SysInfo/releases).

**SysInfo-WPF** is the portable Windows build. It uses .NET Framework 4.7 or later, included in Windows 10 version 1703 and newer and Windows 11.

Extract all files to a writable folder and run `SysInfo.exe`. Automatic updates preserve your executable filename, including renamed copies, and install its matching `.exe.config` file.

## Updates

Startup checks query this repository's latest stable GitHub release without GitHub credentials. The footer shows a temporary checking status, then restores its previous message. No dialog is shown when no update is available or the network check fails. A newer release offers **Update now**, **Skip this release**, and **Don't check for updates automatically**.

Preferences are per Windows user under `HKEY_CURRENT_USER\Software\ELADWYSOFT\SysInfo\Updates`: `CheckOnStartup` (DWORD) and `SkippedVersion` (string). Use **Windows tools > Update settings** to enable checks again or clear the skipped release. **Check for updates** checks manually even when automatic checking is disabled.

The updater downloads the WPF package, checks SHA-256 integrity and the embedded version, waits for SysInfo to exit, replaces its files and relaunches. Installation failures roll back changed files. It does not request administrator access; use a writable installation folder. Checksums protect download integrity and are not code-signing certificates.

Click the version and **About** text in the footer to see the app version, ELADWYSOFT branding, developer contact and GitHub links.

## Resource reminders

Compact badges beside the existing meters show high RAM use at 80% and critical use at 95%. The Windows system-volume badge warns at 10 GB or less free, or at 10% or less when no more than 20 GB remains. It becomes critical at 2 GB or less, or at 5% or less when no more than 5 GB remains. Tooltips provide cleanup and app-management guidance. Readings and badges update when you use Refresh; these are snapshot reminders rather than continuous monitoring.

## Health information

Storage health combines Windows storage status, supported ATA SMART signals and native NVMe SMART data. SSD remaining endurance is not a guarantee against failure. Unsupported readings stay unknown. The amber 48-hour uptime badge is a reminder to consider restarting, not a diagnosis of slow performance; Fast Startup can preserve kernel uptime across shutdowns.

## Build

With the .NET 8 SDK:

```powershell
dotnet test SysInfo.Tests -c Release
dotnet build SysInfo.Wpf -c Release
./scripts/Package-Release.ps1 -Version 2.2.6
```

Cross-drive updater regression testing is available in `scripts/Test-UpdaterCrossDrive.ps1`. Run it in Windows PowerShell with `-Executable`, `-Package` and an existing `-TargetRoot` on a different drive from the updater cache. It creates and cleans disposable installation folders.

The main solution contains the WPF app and its tests. The original C# WinForms implementation remains available in Git history at commit `866b9a3`.
