param(
    [Parameter(Mandatory=$true)][string]$Executable,
    [Parameter(Mandatory=$true)][string]$Package,
    [Parameter(Mandatory=$true)][string]$TargetRoot
)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName PresentationFramework,PresentationCore,WindowsBase,System.Runtime.Serialization,System.IO.Compression.FileSystem
$Executable=(Resolve-Path -LiteralPath $Executable).Path
$Package=(Resolve-Path -LiteralPath $Package).Path
$TargetRoot=(Resolve-Path -LiteralPath $TargetRoot).Path
$assembly=[Reflection.Assembly]::LoadFrom($Executable)
$version=$assembly.GetName().Version.ToString(3)
$installer=$assembly.GetType('CNSysInfo.UpdateInstaller')
$cache=$installer.GetProperty('CacheRoot').GetValue($null,$null)
if([IO.Path]::GetPathRoot($cache) -eq [IO.Path]::GetPathRoot($TargetRoot)){throw 'TargetRoot must be on a different drive from the updater cache.'}
$cacheDrive=[IO.Path]::GetPathRoot($cache)
$targetDrive=[IO.Path]::GetPathRoot($TargetRoot)
$index=0
foreach($alias in 'SysInfo.exe','SysInfo - Cross Drive.exe'){
    $index++
    $id=[Guid]::NewGuid().ToString('N')
    $jobRoot=Join-Path $cache ('cross-drive-test-'+$id)
    $install=Join-Path $TargetRoot ('cross-drive-test-'+$id)
    $null=New-Item -ItemType Directory -Path $jobRoot,$install
    $processName=[IO.Path]::GetFileNameWithoutExtension($alias)
    try{
        $stage=$installer.GetMethod('StagePackage').Invoke($null,@([string]$Package,[string]$jobRoot,[string]$version))
        $target=Join-Path $install $alias
        $otherName=if($alias -eq 'SysInfo.exe'){'OtherApp.exe'}else{'SysInfo.exe'}
        $other=Join-Path $install $otherName
        [IO.File]::WriteAllText($other,'Unrelated executable')
        [IO.File]::WriteAllText(($other+'.config'),'<configuration><!-- unrelated --></configuration>')
        $otherHash=(Get-FileHash $other).Hash
        $otherConfigHash=(Get-FileHash ($other+'.config')).Hash
        $fixture=Join-Path $jobRoot ('HostFixture'+$id+'.exe')
        $class='CrossDriveHost'+$id
        $engineLiteral=$Executable.Replace('"','""')
        $stageLiteral=$stage.Replace('"','""')
        $errorLiteral=(Join-Path $jobRoot 'parent-error.txt').Replace('"','""')
        $hostSource=@"
using System;
using System.Reflection;
using System.Threading;
public class $class {
    public static int Main(string[] args) {
        try {
            var engine = Assembly.LoadFrom(@"$engineLiteral");
            var updater = engine.GetType("CNSysInfo.UpdateInstaller");
            if(args.Length > 0 && args[0] == "--apply-update") {
                updater.GetMethod("TryRun").Invoke(null, new object[] { args });
                return Environment.ExitCode;
            }
            updater.GetMethod("LaunchHelper").Invoke(null, new object[] { @"$stageLiteral", "$version" });
            Thread.Sleep(1000);
            return 0;
        } catch(Exception ex) { System.IO.File.WriteAllText(@"$errorLiteral",ex.ToString()); return 1; }
    }
}
"@
        Add-Type -TypeDefinition $hostSource -OutputAssembly $fixture -OutputType ConsoleApplication
        Copy-Item -LiteralPath $fixture -Destination $target
        if($index -eq 1){Copy-Item -LiteralPath ($Executable+'.config') -Destination ($target+'.config')}
        $hadConfig=Test-Path -LiteralPath ($target+'.config')
        $originalHash=(Get-FileHash $target).Hash
        $originalConfigHash=if($hadConfig){(Get-FileHash ($target+'.config')).Hash}else{$null}
        $parent=Start-Process -FilePath $target -WindowStyle Hidden -PassThru
        if(-not $parent.WaitForExit(10000)){throw 'Parent did not exit'}
        if(Test-Path -LiteralPath (Join-Path $jobRoot 'parent-error.txt')){throw [IO.File]::ReadAllText((Join-Path $jobRoot 'parent-error.txt'))}
        $spec=Join-Path $jobRoot 'job.json'
        $jobType=$assembly.GetType('CNSysInfo.UpdateJob')
        $serializer=New-Object Runtime.Serialization.Json.DataContractJsonSerializer($jobType)
        $stream=[IO.File]::OpenRead($spec)
        try{$job=$serializer.ReadObject($stream)}finally{$stream.Dispose()}
        if($job.TargetDirectory -ne $install -or $job.TargetExecutableName -ne $alias){throw 'Incorrect generated target job'}
        $watch=[Diagnostics.Stopwatch]::StartNew();$updated=$null
        while(-not $updated -and $watch.Elapsed.TotalSeconds -lt 30){
            $updated=@(Get-Process -Name $processName -ErrorAction SilentlyContinue | Where-Object {$_.Path -eq $target -and $_.MainWindowHandle -ne 0}) | Select-Object -First 1
            if(-not $updated){Start-Sleep -Milliseconds 100}
        }
        if(-not $updated){throw 'Cross-drive update did not relaunch'}
        foreach($name in 'SysInfo.exe','SysInfo.exe.config'){
            $installed=if($name -eq 'SysInfo.exe'){$target}else{$target+'.config'}
            if((Get-FileHash $installed).Hash -ne (Get-FileHash (Join-Path $stage $name)).Hash){throw 'Installed file differs from stage'}
        }
        $null=$updated.CloseMainWindow()
        if(-not $updated.WaitForExit(10000)){$updated.Kill();$updated.WaitForExit()}
        if((Get-FileHash $other).Hash -ne $otherHash -or (Get-FileHash ($other+'.config')).Hash -ne $otherConfigHash){throw 'Unrelated files changed'}
        if((Get-FileHash (Join-Path $jobRoot ('backup\'+$alias))).Hash -ne $originalHash){throw 'Cross-drive backup differs from original'}
        Write-Output "PASS: $cacheDrive stage/helper/backup -> $targetDrive target, production launch/install/relaunch, case $index."

        # Reset the disposable installation to exercise a failed relaunch transaction.
        Copy-Item -LiteralPath (Join-Path $jobRoot ('backup\'+$alias)) -Destination $target -Force
        if($hadConfig){Copy-Item -LiteralPath (Join-Path $jobRoot ('backup\'+$alias+'.config')) -Destination ($target+'.config') -Force}
        else{Remove-Item -LiteralPath ($target+'.config') -Force}
        $script:launchAttempted=$false
        $fail=[Action[string]]{param($path) $script:launchAttempted=$true;throw 'Expected relaunch failure'}
        $failed=$false
        try{$null=$installer.GetMethod('ReplaceFiles',[Reflection.BindingFlags]'Static,NonPublic').Invoke($null,@($job,[string](Join-Path $jobRoot 'rollback-backup'),$fail))}
        catch{$failed=$true}
        if(-not $failed -or -not $script:launchAttempted -or (Get-FileHash $target).Hash -ne $originalHash){throw 'Cross-drive rollback failed'}
        if($hadConfig){if((Get-FileHash ($target+'.config')).Hash -ne $originalConfigHash){throw 'Config rollback failed'}}
        elseif(Test-Path -LiteralPath ($target+'.config')){throw 'Rollback retained a newly added config'}
        if((Get-FileHash $other).Hash -ne $otherHash -or (Get-FileHash ($other+'.config')).Hash -ne $otherConfigHash){throw 'Rollback changed unrelated files'}
        if(@(Get-ChildItem -LiteralPath $install -File | Where-Object {$_.Extension -eq '.new' -or $_.Extension -eq '.rollback'}).Count -ne 0){throw 'Temporary replacement files remain'}
        Write-Output "PASS: $cacheDrive backup -> $targetDrive atomic rollback, exact config state and unrelated-file preservation, case $index."
    }finally{
        foreach($p in @(Get-Process -Name (@('SysInfo',$processName) | Select-Object -Unique) -ErrorAction SilentlyContinue)){
            if($p.Path -and ($p.Path.StartsWith($jobRoot+'\',[StringComparison]::OrdinalIgnoreCase) -or $p.Path.StartsWith($install+'\',[StringComparison]::OrdinalIgnoreCase))){$p.Kill();$p.WaitForExit()}
        }
        foreach($dir in @($install,$jobRoot)){
            $resolved=[IO.Path]::GetFullPath($dir)
            $parent=[IO.Path]::GetDirectoryName($resolved)
            if([IO.Path]::GetFileName($resolved) -notlike 'cross-drive-test-*' -or ($parent -ne [IO.Path]::GetFullPath($cache) -and $parent -ne [IO.Path]::GetFullPath($TargetRoot))){throw 'Unsafe test cleanup target'}
            Remove-Item -LiteralPath $resolved -Recurse -Force
        }
    }
}
