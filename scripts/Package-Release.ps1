param([Parameter(Mandatory=$true)][string]$Version)
$ErrorActionPreference = 'Stop'
if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw 'Expected a numeric major.minor.patch version.' }
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$output = Join-Path $root "dist/$Version"
$framework = Join-Path $root 'SysInfo.Wpf/bin/Release/net47'
$exe = Join-Path $framework 'SysInfo.exe'
$actual = [Diagnostics.FileVersionInfo]::GetVersionInfo($exe).FileVersion
if ([Version]$actual -ne [Version]"$Version.0") { throw "Unexpected file version: $actual" }
New-Item -ItemType Directory -Path $output -Force | Out-Null
$destination = Join-Path $output "SysInfo-v$Version.exe"
Copy-Item -LiteralPath $exe -Destination $destination -Force
$hash = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash.ToLowerInvariant()
Write-Output ([IO.Path]::GetFileName($destination) + ': ' + (Get-Item $destination).Length + ' bytes; SHA256 ' + $hash)