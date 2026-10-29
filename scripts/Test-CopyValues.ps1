param([Parameter(Mandatory=$true)][string]$Executable,[switch]$CheckCommandsOnly)
$ErrorActionPreference='Stop'
if($PSVersionTable.PSEdition -eq 'Core'){throw 'Use powershell.exe -STA for WPF clipboard checks.'}
Add-Type -AssemblyName PresentationFramework,PresentationCore,WindowsBase,System.Xaml
$asm=[Reflection.Assembly]::LoadFrom((Resolve-Path -LiteralPath $Executable).Path)
$window=[Activator]::CreateInstance($asm.GetType('CNSysInfo.MainWindow'),$true)
$flags=[Reflection.BindingFlags]'Instance,NonPublic,DeclaredOnly'
$original=$null; $savedClipboard=$false
try{
 if(!$CheckCommandsOnly){$original=[Windows.Clipboard]::GetDataObject();$savedClipboard=$true;[Windows.Clipboard]::SetText("SysInfo copy-test preflight")}
 foreach($name in 'txtPc','txtUser','txtMaker','txtModel','txtSerial','txtAsset','txtOs','txtAct','txtUptime','txtBios','txtCpu','txtCpuCache','txtMem','txtStorage','txtBattery','txtGpu','txtScreen','lblBattery','lblMem','lblDisk','txtRating'){
  $value=$window.GetType().GetField($name,$flags).GetValue($window)
  $fixture='Example '+$name+"`n"+'Multiline value Ω 123'
  $value.Text=$fixture
  if(!$value.ContextMenu -or $value.ContextMenu.Items[0].Header -ne 'Copy value'){throw ('Copy action missing: '+$name)}
  $item=$value.ContextMenu.Items[0]
  if($item.CommandTarget -ne $value){throw 'Copy targets wrong value'}
  if(![Windows.Input.ApplicationCommands]::Copy.CanExecute($null,$value)){throw 'Copy command disabled'}
  if(!$CheckCommandsOnly){[Windows.Input.ApplicationCommands]::Copy.Execute($null,$value)}
  if(!$CheckCommandsOnly -and [Windows.Clipboard]::GetText() -ne $fixture){throw 'Clipboard differs from full displayed value'}
  $value.Text='Updated live value'
  if(!$CheckCommandsOnly){[Windows.Input.ApplicationCommands]::Copy.Execute($null,$value)}
  if(!$CheckCommandsOnly -and [Windows.Clipboard]::GetText() -ne 'Updated live value'){throw 'Copied stale value'}
  $value.Text='...'
  if([Windows.Input.ApplicationCommands]::Copy.CanExecute($null,$value)){throw 'Loading placeholder should not copy'}
 }
 if($CheckCommandsOnly){'PASS: copy commands/menu targets on all 21 fields/captions; placeholders disabled. Clipboard not exercised.'}else{'PASS: all 21 full current values copied and verified.'}
}finally{
 if($savedClipboard){if($original){[Windows.Clipboard]::SetDataObject($original,$true)}else{[Windows.Clipboard]::Clear()}}
 $window.Close()
}
if($savedClipboard){'PASS: prior clipboard contents restored.'}
