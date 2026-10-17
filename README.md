# ELADWYSOFT SysInfo

A compact Windows system-information tool with a light WPF interface, hardware details, CPU cache, storage usage and health, battery health, report export and Windows utilities.

## Download

Get a ZIP from [GitHub Releases](https://github.com/iAhmedEladwy/SysInfo/releases).

- **SysInfo-WPF**: small portable build, requires .NET Framework 4.7 or later (included in Windows 10 version 1703 and newer).
- **SysInfo-Standalone-win-x64**: Windows x64 build with its .NET runtime included.

Extract all files to a writable folder and run `SysInfo.exe`. Keep the executable's original name for automatic updating.

## Updates

Startup checks query this repository's latest stable GitHub release without GitHub credentials. No message is shown when no update is available or the network check fails. A newer release offers **Update now**, **Skip this release**, and **Don't check for updates automatically**.

Preferences are per Windows user under `HKEY_CURRENT_USER\Software\ELADWYSOFT\SysInfo\Updates`: `CheckOnStartup` (DWORD) and `SkippedVersion` (string). Use **Windows tools > Update settings** to enable checks again or clear the skipped release. **Check for updates** checks manually even when automatic checking is disabled.

The updater downloads the matching build, checks SHA-256 integrity and the embedded version, waits for SysInfo to exit, replaces its files and relaunches. Installation failures roll back changed files. It does not request administrator access; use a writable installation folder. Checksums protect download integrity and are not code-signing certificates.

## Health information

Storage health combines Windows storage status, supported ATA SMART signals and native NVMe SMART data. SSD remaining endurance is not a guarantee against failure. Unsupported readings stay unknown. The 48-hour uptime badge is a reminder to consider restarting, not a diagnosis of slow performance; Fast Startup can preserve kernel uptime across shutdowns.

## Build

With the .NET 8 SDK:

```powershell
dotnet test SysInfo.Tests -c Release
dotnet build SysInfo.Wpf -c Release
dotnet publish SysInfo.Standalone -c Release
./scripts/Package-Release.ps1 -Version 2.2.2
```

The main solution contains the WPF app and its tests. The original C# WinForms implementation is preserved separately under `legacy/WinForms`.
