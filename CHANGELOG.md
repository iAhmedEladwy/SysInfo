# Changelog

## 2.2.6

- Fix updates when the app and updater backup cache are on different drives, including apps installed on D: with a C: user profile.
- Copy backups separately and perform installation and rollback atomically beside the app.

## 2.2.5

- Automatic updates preserve renamed executable filenames and their matching config files, including names containing spaces.
- Keep other executable copies in the same folder unchanged during updates and rollback.
- Older renamed copies need this release installed manually once, or their original SysInfo.exe filename restored for the update.

## 2.2.4

- Compact RAM and Windows drive-space warning badges with practical tooltips, without adding rows to the window.
- Warm amber restart reminder after 48 hours of uptime.
- Branded About dialog, modern app icon, ELADWYSOFT binary company metadata, developer contact and GitHub links.
- Temporary update-check status that restores the previous footer message.
- Keep the portable WPF build as the single download and remove the standalone variant and legacy source copy.

## 2.2.3

- Version advance for verifying automatic updates from the 2.2.2 release. No feature changes.

## 2.2.2

- Compact single-column light WPF interface with content-fitting startup size, centered rating, footer capture details, BIOS badge beside the computer model and a restart reminder.
- CPU L2/L3 cache details, clearer activation status, native NVMe SMART endurance/temperature, improved disk usage and battery reporting.
- Silent startup update checks with optional verified download and installation, release skipping and per-user registry preferences.
- Small .NET Framework and standalone Windows x64 downloads.
