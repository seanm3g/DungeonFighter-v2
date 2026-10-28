# Registers (or removes) the "DungeonFighter Idle Balance" scheduled task: runs Scripts/idle-balance.ps1 after
# 2 minutes without input, as the current user, only while logged on. The runner stops itself when you return.
#
#   .\Scripts\install-idle-balance.ps1            # install / update
#   .\Scripts\install-idle-balance.ps1 -Remove    # uninstall
param([switch]$Remove, [int]$IdleMinutes = 2)

$ErrorActionPreference = 'Stop'
$TaskName = 'DungeonFighter Idle Balance'
$TrayShortcut = Join-Path ([Environment]::GetFolderPath('Startup')) 'DungeonFighter Idle Balance Tray.lnk'

if ($Remove) {
    Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false
    Remove-Item $TrayShortcut -ErrorAction SilentlyContinue
    Write-Host "Removed '$TaskName' and its tray icon at logon (an open tray icon stays until you hide it)"
    return
}

# Tray icon (pause / resume) at every logon, and now.
$tray = Join-Path $PSScriptRoot 'idle-balance-tray.ps1'
$trayArgs = "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File `"$tray`""
$lnk = (New-Object -ComObject WScript.Shell).CreateShortcut($TrayShortcut)
$lnk.TargetPath = 'powershell.exe'
$lnk.Arguments = $trayArgs
$lnk.WindowStyle = 7
$lnk.Save()
Start-Process powershell.exe -ArgumentList $trayArgs -WindowStyle Hidden

$runner = Join-Path $PSScriptRoot 'idle-balance.ps1'
$action = New-ScheduledTaskAction -Execute 'powershell.exe' `
    -Argument "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File `"$runner`""
$trigger = Get-CimClass -ClassName MSFT_TaskIdleTrigger -Namespace Root/Microsoft/Windows/TaskScheduler |
    New-CimInstance -ClientOnly
$settings = New-ScheduledTaskSettingsSet -RunOnlyIfIdle -IdleDuration (New-TimeSpan -Minutes $IdleMinutes) `
    -IdleWaitTimeout (New-TimeSpan -Hours 1) -RestartOnIdle -ExecutionTimeLimit ([TimeSpan]::Zero) `
    -MultipleInstances IgnoreNew -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries
# The runner watches input itself and kills the whole Claude/dotnet tree; don't let Task Scheduler cut it first.
$settings.IdleSettings.StopOnIdleEnd = $false
$principal = New-ScheduledTaskPrincipal -UserId "$env:USERDOMAIN\$env:USERNAME" -LogonType Interactive -RunLevel Limited

Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger -Settings $settings `
    -Principal $principal -Description 'Headless Claude balance passes for DungeonFighter-v2 while the PC is idle.' -Force | Out-Null
Write-Host "Installed '$TaskName' (starts after $IdleMinutes idle min). Logs: $env:LOCALAPPDATA\DungeonFighterIdleBalance\logs"
