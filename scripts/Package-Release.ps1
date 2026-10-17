param([Parameter(Mandatory=$true)][string]$Version)
$ErrorActionPreference = 'Stop'
if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw 'Expected a numeric major.minor.patch version.' }
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$output = Join-Path $root "dist/$Version"
New-Item -ItemType Directory -Path $output -Force | Out-Null
$framework = Join-Path $root 'SysInfo.Wpf/bin/Release/net47'
$standalone = Join-Path $root 'SysInfo.Standalone/bin/Release/net8.0-windows/win-x64/publish'
foreach ($build in @(@{Name='WPF';Folder=$framework;Files=@('SysInfo.exe','SysInfo.exe.config')},
    @{Name='Standalone-win-x64';Folder=$standalone;Files=@('SysInfo.exe')})) {
    $exe = Join-Path $build.Folder 'SysInfo.exe'
    $actual = [Diagnostics.FileVersionInfo]::GetVersionInfo($exe).FileVersion
    if ([Version]$actual -ne [Version]"$Version.0") { throw "Unexpected file version: $actual" }
    $files = @($build.Files | ForEach-Object { $file=Join-Path $build.Folder $_; if (!(Test-Path -LiteralPath $file)) { throw "Missing $file" }; $file })
    $zip = Join-Path $output "SysInfo-$($build.Name)-v$Version.zip"
    Compress-Archive -LiteralPath $files -DestinationPath $zip -Force
    $hash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()
    [IO.File]::WriteAllText($zip+'.sha256', "$hash  $([IO.Path]::GetFileName($zip))`n", [Text.UTF8Encoding]::new($false))
    Write-Output ([IO.Path]::GetFileName($zip) + ': ' + (Get-Item $zip).Length + ' bytes; SHA256 ' + $hash)
}
