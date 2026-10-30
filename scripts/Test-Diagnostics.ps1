param([Parameter(Mandatory=$true)][string]$Executable)
$ErrorActionPreference='Stop'
if($PSVersionTable.PSEdition -eq 'Core'){throw 'Run this WPF integration check in Windows PowerShell (powershell.exe -STA).'}
Add-Type -AssemblyName PresentationFramework,PresentationCore,WindowsBase,System.Xaml,System.Runtime.Serialization,System.Management,System.Windows.Forms
$asm=[Reflection.Assembly]::LoadFrom((Resolve-Path -LiteralPath $Executable).Path)
$flags=[Reflection.BindingFlags]'Instance,NonPublic,DeclaredOnly'
function New-Model([string]$name){[Activator]::CreateInstance($asm.GetType('CNSysInfo.'+$name),$true)}
function Field($instance,[string]$name){$instance.GetType().GetField($name,$flags).GetValue($instance)}
function Call($instance,[string]$name,[object[]]$arguments){$instance.GetType().GetMethod($name,$flags).Invoke($instance,$arguments)}
function Pump([int]$milliseconds){
 $frame=New-Object Windows.Threading.DispatcherFrame
 $timer=New-Object Windows.Threading.DispatcherTimer;$timer.Interval=[TimeSpan]::FromMilliseconds($milliseconds)
 $timer.Tag=$frame;$timer.Add_Tick({param($sender,$args) $sender.Stop();$sender.Tag.Continue=$false});$timer.Start()
 [Windows.Threading.Dispatcher]::PushFrame($frame)
}
$app=New-Object Windows.Application;$app.ShutdownMode=[Windows.ShutdownMode]::OnExplicitShutdown
$window=New-Model 'MainWindow';$snapshot=New-Model 'UiSnapshot';$s=$snapshot.System
$s.Cpu='Intel(R) Core(TM) i7-8850H CPU @ 2.60GHz';$s.CpuCoreCount=6;$s.CpuLogicalProcessorCount=12;$s.CapturedAt=[DateTime]::Now
$s.MachineName='Fixture';$s.SystemModel='Latitude 5491';$s.OsDisplay='Windows 11 Pro 64-bit';$s.MemoryLoadPercent=50
$b=New-Model 'BatteryInfo';$b.ChargeLevel=72;$b.HealthPercentage=38;$b.Status='Charging';$b.DesignCapacity=67997;$b.FullChargeCapacity=25612;$s.BatteryInfo=$b
$window.GetType().GetField('_ui',$flags).SetValue($window,$snapshot)
# Disable only expensive snapshot/update-on-load work; keep production power monitoring and window hooks.
$loaded=$window.GetType().GetMethod('Window_Loaded',$flags)
$handler=[Delegate]::CreateDelegate([Windows.RoutedEventHandler],$window,$loaded)
$window.Remove_Loaded($handler)
$window.ShowInTaskbar=$false;$window.ShowActivated=$false;$window.WindowStartupLocation='Manual';$window.Left=-10000;$window.Top=-10000
try{
 Call $window 'Render' @($false) | Out-Null
 $window.Show();Pump 100
 Call $window 'FitInitialContent' @() | Out-Null
 $width=$window.Width;$height=$window.Height
 if($width -ne 760){throw 'Unexpected default width'}
 $powerTimer=Field $window '_powerTimer'
 if(!$powerTimer.IsEnabled){throw 'Live monitoring did not start'}
 $testButtons=@('btnKeyboard','btnSpeakers','btnCamera' | ForEach-Object {Field $window $_})
 if($testButtons.Count -ne 3 -or @($testButtons | Where-Object {$_.ContextMenu}).Count -ne 0){throw 'Tests must be directly available as toolbar buttons'}
 $group=$testButtons[0].Parent
 if($group.Children[0].Text -ne 'Tests:' -or $testButtons[1].Parent -ne $group -or $testButtons[2].Parent -ne $group){throw 'Discoverable Tests group label missing'}
 $toolbar=Field $window 'toolbar'
 if($toolbar.Child.DesiredSize.Width -gt 488){throw ('Toolbar does not fit minimum width: '+$toolbar.Child.DesiredSize.Width)}
 $script:checked=$null;$script:dialogFailure=$null
 foreach($item in $testButtons){
  $timer=New-Object Windows.Threading.DispatcherTimer;$timer.Interval=[TimeSpan]::FromMilliseconds(250)
  $timer.Add_Tick({param($sender,$args)
   $sender.Stop()
   foreach($dialog in @($app.Windows)){
    if($dialog -eq $window){continue}
    try{
     $script:checked=$dialog.Title
     if($dialog.Title -eq 'Keyboard test'){
      $dialog.UpdateLayout()
      if($dialog.ActualWidth -gt 925 -or $dialog.ActualHeight -gt 360){throw 'Keyboard test has unnecessary empty space'}
      $pad=Field $dialog 'numpad'
      if($pad.Visibility -ne 'Visible'){throw 'Numeric keypad must always be visible'}
      $tiles=Field $dialog 'keyTiles'
      $ins=$tiles[[Windows.Input.Key]::Insert][0];$del=$tiles[[Windows.Input.Key]::Delete][0]
      if([Windows.Controls.Grid]::GetRow($ins) -ne 1 -or [Windows.Controls.Grid]::GetRow($del) -ne 2){throw 'Navigation keys are not in natural rows'}
      if($pad.Parent.Children[1] -ne $ins.Parent){throw 'Navigation cluster is not between letters and numpad'}
      if($tiles[[Windows.Input.Key]::Return].Count -ne 2){throw 'Both physical Enter keys must be shown'}
      $header=$dialog.Content.Children[0]
      if($header.Children.Count -ne 2 -or $header.Children[0].Content -ne 'Reset'){throw 'Reset must share the information line'}
      if($dialog.Content.Children.Count -ne 2){throw 'Unexpected standalone controls or info rows'}
     }
     if($dialog.Title -eq 'Camera test' -and (Field $dialog 'preview') -ne $null){throw 'Camera started automatically'}
     if($dialog.Title -eq 'Speaker quality test' -and (Field $dialog 'player') -ne $null){throw 'Audio started automatically'}
    }catch{$script:dialogFailure=$_}
    finally{$dialog.Close()}
   }
  })
  $timer.Start()
  $item.RaiseEvent([Windows.RoutedEventArgs]::new([Windows.Controls.Button]::ClickEvent))
  if($script:dialogFailure){throw $script:dialogFailure}
  if(!$script:checked){throw 'Diagnostic did not open'}
  if($window.Width -ne $width -or $window.Height -ne $height){throw 'Opening a diagnostic resized the main window'}
  Write-Output ('PASS: modal '+$script:checked+' opens/closes, no automatic camera/audio, main size unchanged.')
  $script:checked=$null
 }
 Call $window 'UpdateLivePower' @() | Out-Null
 if($b.ChargeLevel -ne 72 -or $b.HealthPercentage -ne 38){throw 'Live updates rewrote the snapshot'}
 $live=Field $window '_livePower'
 if($live.BatteryPresent -eq $true){
  $powerText=(Field $window 'txtBattery').Text
  if(!$powerText.Contains('Design 67,997') -or !$powerText.Contains('Full 25,612')){throw 'Battery capacities not visible'}
  $old=$asm.GetType('CNSysInfo.LivePowerState').GetMethod('FromNative').Invoke($null,@($true,[byte]1,[byte]128,[byte]255))
  $window.GetType().GetField('_livePower',$flags).SetValue($window,$old)
  Call $window 'UpdateLivePower' @() | Out-Null
  if((Field $window 'lblBattery').Text -ne 'Refresh'){throw 'Reconnected battery reused stale health'}
  Call $window 'Render' @($false) | Out-Null
  if((Field $window 'lblBattery').Text -ne '38%'){throw 'Refresh did not restore captured health'}
 }
 Pump 2200
 if($window.Width -ne $width -or $window.Height -ne $height){throw 'Live update resized window'}
 Write-Output ('PASS: live OS power updates without snapshot mutation or resize; main size '+$width+'x'+[Math]::Round($height,2)+'.')
}finally{
 $window.Close()
 if($powerTimer -and $powerTimer.IsEnabled){throw 'Power timer remained active after close'}
 $app.Shutdown()
}
'PASS: power monitoring stops on close.'
