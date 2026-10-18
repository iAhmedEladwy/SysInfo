param([Parameter(Mandatory=$true)][string]$Version)
$ErrorActionPreference = 'Stop'
if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw 'Expected a numeric major.minor.patch version.' }
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$output = Join-Path $root "dist/$Version"
$framework = Join-Path $root 'SysInfo.Wpf/bin/Release/net47'
$exe = Join-Path $framework 'SysInfo.exe'
$actual = [Diagnostics.FileVersionInfo]::GetVersionInfo($exe).FileVersion
if ([Version]$actual -ne [Version]"$Version.0") { throw "Unexpected file version: $actual" }
$files = @('SysInfo.exe','SysInfo.exe.config') | ForEach-Object {
    $file = Join-Path $framework $_
    if (!(Test-Path -LiteralPath $file)) { throw "Missing $file" }
    $file
}
New-Item -ItemType Directory -Path $output -Force | Out-Null
$zip = Join-Path $output "SysInfo-WPF-v$Version.zip"
Compress-Archive -LiteralPath $files -DestinationPath $zip -Force
$hash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText($zip+'.sha256', "$hash  $([IO.Path]::GetFileName($zip))`n", [Text.UTF8Encoding]::new($false))
Write-Output ([IO.Path]::GetFileName($zip) + ': ' + (Get-Item $zip).Length + ' bytes; SHA256 ' + $hash)
