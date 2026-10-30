# ELADWYSOFT SysInfo

A compact Windows system-information tool with a light WPF interface, hardware details, CPU cache, storage usage and health, battery health, report export and Windows utilities.

## Download

Get the EXE from [GitHub Releases](https://github.com/iAhmedEladwy/SysInfo/releases).

**SysInfo** is the portable Windows build. It uses .NET Framework 4.7 or later, included in Windows 10 version 1703 and newer and Windows 11.

Download the release EXE to a writable folder and run it. No configuration file is required. Update preferences are stored in HKCU\Software\ELADWYSOFT\SysInfo\Updates. When a newer version is available, SysInfo opens its official GitHub release page so you can download the EXE manually. Close the running app before replacing your old EXE. Keep its old filename if you renamed it; registry preferences are retained.

## Updates

Startup checks query this repository's latest stable GitHub release without GitHub credentials. The footer shows a temporary checking status, then restores its previous message. No dialog is shown when no update is available or the network check fails. A newer release offers **Open GitHub release**, **Skip this release**, and **Don't check for updates automatically**.

Preferences are per Windows user under `HKEY_CURRENT_USER\Software\ELADWYSOFT\SysInfo\Updates`: `CheckOnStartup` (DWORD) and `SkippedVersion` (string). Use **Tools > Update settings** to enable checks again or clear the skipped release. **Check for updates** checks manually even when automatic checking is disabled.

The update checker only reads release metadata and opens a release URL built for this repository. It does not download, execute, install or replace application files, start an updater helper, or request administrator rights. Downloading manually is not a guarantee against malware; normal Windows download and execution protections still apply.

Click the version and **About** text in the footer to see the app version, ELADWYSOFT branding, developer contact and GitHub links.

## Copy individual values

Click a displayed hardware value once to copy it. A hand cursor and tooltip indicate this action; a brief **Copied ✓** confirmation appears next to the value after a successful copy. Keyboard users can focus a value and press **Ctrl+C**, **Enter** or **Space**. The optional right-click menu is also available. This copies the full current value, including multiline or visually truncated text and live battery/power information. Percentage captions and the rating are also copyable. The existing **Copy info** action still copies the report. These actions only write to the Windows clipboard when requested.

## Interactive checks and live power

The **Tests** menu opens separate keyboard, stereo speaker and camera windows. Keyboard highlights apply only while the test is focused; Fn and reserved Windows shortcuts may be unavailable. Stereo checks use the default Windows playback device, short generated left/right tones and a Stop button. Mono audio, balance and output routing can affect what you hear. Camera preview starts only after selecting **Start preview**, stops when the test loses focus or closes, and does not record or save media. Camera access may require Windows desktop-app camera permission.

These are interactive functional checks, not automatic hardware-health certification. Screen and mouse tests are not included yet.

The **Power / capacity** row reads live Windows status every two seconds and on power notifications, independently of the full hardware scan. It distinguishes charging, plugged in but not charging, on battery, full charge and unavailable readings. Battery health/capacity still come from the captured hardware snapshot and update with Refresh; a battery presence change asks for a health refresh. Live values do not rewrite the report's captured snapshot or capture timestamp. Design and full-charge capacity remain visible beside live charge/power; remaining battery health stays in its separate meter. Full details are also available in the tooltip. The keyboard window fits a full layout with a permanently visible numpad, a standard navigation/arrow cluster between letters and numpad, and one compact information line with Reset. Use the window title-bar X to close it.

Startup width is 760 logical pixels (previously 800). The six-button toolbar stays on one line at the minimum width; opening a test or receiving a power update does not resize the main window. Initial content fitting is capped to the previous layout height; long wrapped values use the existing content scroll.
## Resource reminders

Compact badges beside the existing meters show high RAM use at 80% and critical use at 95%. The Windows system-volume badge warns at 10 GB or less free, or at 10% or less when no more than 20 GB remains. It becomes critical at 2 GB or less, or at 5% or less when no more than 5 GB remains. Tooltips provide cleanup and app-management guidance. Readings and badges update when you use Refresh; these are snapshot reminders rather than continuous monitoring.

## Health information

Storage health combines Windows storage status, supported ATA SMART signals and native NVMe SMART data. SSD remaining endurance is not a guarantee against failure. Unsupported readings stay unknown. The amber 48-hour uptime badge is a reminder to consider restarting, not a diagnosis of slow performance; Fast Startup can preserve kernel uptime across shutdowns.

## Build

With the .NET 8 SDK:

```powershell
dotnet test SysInfo.Tests -c Release
dotnet build SysInfo.Wpf -c Release
./scripts/Package-Release.ps1 -Version 2.2.10
```

Windows UI/power lifecycle checks are available in `scripts/Test-Diagnostics.ps1` (run with `powershell.exe -STA -File scripts/Test-Diagnostics.ps1 -Executable <built EXE>`). They exercise the actual menu/dialog paths, ensure camera/audio do not start automatically, verify stable main-window sizing, and check power-monitor shutdown. They do not replace physical keyboard/audio/camera/charger acceptance.


The main solution contains the WPF app and its tests. The original C# WinForms implementation remains available in Git history at commit `866b9a3`.
