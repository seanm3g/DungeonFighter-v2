# Taskbar tray icon for the idle balance runner. Click it to pause / resume.
#   green  = on: a pass starts when the PC is idle
#   orange = a pass is running now
#   grey   = paused: no passes start, and a running pass is stopped
# Pausing writes %LOCALAPPDATA%\DungeonFighterIdleBalance\paused, which idle-balance.ps1 checks every 15 s.
# install-idle-balance.ps1 starts this at logon.

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms, System.Drawing

$mutex = New-Object System.Threading.Mutex($false, 'DungeonFighterIdleBalanceTray')
if (-not $mutex.WaitOne(0)) { return }   # already in the tray

$StateDir = Join-Path $env:LOCALAPPDATA 'DungeonFighterIdleBalance'
$PauseFile = Join-Path $StateDir 'paused'
$LockFile = Join-Path $StateDir 'runner.lock'
$BalanceLog = Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) 'DungeonFighter-v2-idle\Documentation\IDLE_BALANCE_LOG.md'
New-Item -ItemType Directory -Force (Join-Path $StateDir 'logs') | Out-Null

function New-DotIcon([System.Drawing.Color]$color) {
    $bmp = New-Object System.Drawing.Bitmap 16, 16
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.FillEllipse((New-Object System.Drawing.SolidBrush $color), 1, 1, 14, 14)
    $g.DrawEllipse((New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(90, 0, 0, 0))), 1, 1, 14, 14)
    $g.Dispose()
    [System.Drawing.Icon]::FromHandle($bmp.GetHicon())
}
$icons = @{
    On      = New-DotIcon ([System.Drawing.Color]::FromArgb(46, 160, 67))
    Running = New-DotIcon ([System.Drawing.Color]::FromArgb(230, 140, 20))
    Paused  = New-DotIcon ([System.Drawing.Color]::FromArgb(140, 140, 140))
}

function Get-State {
    if (Test-Path $PauseFile) { return 'Paused' }
    $runnerPid = Get-Content $LockFile -ErrorAction SilentlyContinue
    if ($runnerPid -and (Get-Process -Id $runnerPid -ErrorAction SilentlyContinue)) { return 'Running' }
    'On'
}

$tray = New-Object System.Windows.Forms.NotifyIcon
$menu = New-Object System.Windows.Forms.ContextMenuStrip
$toggleItem = $menu.Items.Add('Pause')
[void]$menu.Items.Add('-')
$logsItem = $menu.Items.Add('Open run logs')
$balanceItem = $menu.Items.Add('Open balance log')
[void]$menu.Items.Add('-')
$exitItem = $menu.Items.Add('Hide tray icon')
$tray.ContextMenuStrip = $menu

function Update-Tray {
    $state = Get-State
    $tray.Icon = $icons[$state]
    $tray.Text = switch ($state) {
        'Paused'  { 'Idle balance: paused (click to resume)' }
        'Running' { 'Idle balance: pass running (click to pause)' }
        default   { 'Idle balance: on when idle (click to pause)' }
    }
    $toggleItem.Text = if ($state -eq 'Paused') { 'Resume' } else { 'Pause' }
}

function Switch-Pause {
    if (Test-Path $PauseFile) { Remove-Item $PauseFile -Force }
    else { Set-Content $PauseFile (Get-Date -Format s) }
    Update-Tray
    $tray.ShowBalloonTip(2000, 'Idle balance', $(if (Test-Path $PauseFile) { 'Paused. Any running pass stops within 15 s.' } else { 'Resumed. Passes start when the PC is idle.' }), 'None')
}

$tray.add_MouseClick({ param($s, $e) if ($e.Button -eq [System.Windows.Forms.MouseButtons]::Left) { Switch-Pause } })
$toggleItem.add_Click({ Switch-Pause })
$logsItem.add_Click({ Start-Process explorer.exe (Join-Path $StateDir 'logs') })
$balanceItem.add_Click({ if (Test-Path $BalanceLog) { Start-Process notepad.exe "`"$BalanceLog`"" } })
$exitItem.add_Click({ $tray.Visible = $false; [System.Windows.Forms.Application]::Exit() })

$timer = New-Object System.Windows.Forms.Timer
$timer.Interval = 5000
$timer.add_Tick({ Update-Tray })
$timer.Start()

Update-Tray
$tray.Visible = $true
[System.Windows.Forms.Application]::Run()
$tray.Dispose()
$mutex.ReleaseMutex()
