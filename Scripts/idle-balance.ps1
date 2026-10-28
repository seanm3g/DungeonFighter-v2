# Idle balance runner: while the PC is idle, runs headless Claude balance passes in a separate worktree.
#
#   .\Scripts\idle-balance.ps1              # loop while idle (what the scheduled task runs)
#   .\Scripts\idle-balance.ps1 -SetupOnly   # create / check the worktree and exit
#
# The worktree (..\DungeonFighter-v2-idle, branch idle-balance) is never pushed. Each pass commits there and
# appends to Documentation/IDLE_BALANCE_LOG.md. The pass is killed as soon as the user touches mouse or keyboard.
# Install the trigger with .\Scripts\install-idle-balance.ps1.
param(
    [switch]$SetupOnly,
    [int]$ReturnIdleSeconds = 60,      # input newer than this = user is back, stop
    [int]$StartIdleSeconds = 120,      # idle needed before a new pass starts
    [int]$NoCommitBackoffMinutes = 60  # idle minutes to wait after a pass that committed nothing
)

$ErrorActionPreference = 'Stop'
$Repo = Split-Path -Parent $PSScriptRoot
$Worktree = Join-Path (Split-Path -Parent $Repo) 'DungeonFighter-v2-idle'
$Branch = 'idle-balance'
$StateDir = Join-Path $env:LOCALAPPDATA 'DungeonFighterIdleBalance'
$LogDir = Join-Path $StateDir 'logs'
$LockFile = Join-Path $StateDir 'runner.lock'
$PauseFile = Join-Path $StateDir 'paused'   # set by the tray icon (idle-balance-tray.ps1)
$PromptFile = Join-Path $PSScriptRoot 'idle-balance-prompt.md'
New-Item -ItemType Directory -Force $LogDir | Out-Null
$RunnerLog = Join-Path $LogDir 'runner.log'

function Log([string]$msg) {
    $line = "{0:yyyy-MM-dd HH:mm:ss} {1}" -f (Get-Date), $msg
    Add-Content -Path $RunnerLog -Value $line -Encoding utf8
    Write-Host $line
}

Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class IdleNative {
    [StructLayout(LayoutKind.Sequential)] struct LASTINPUTINFO { public uint cbSize; public uint dwTime; }
    [DllImport("user32.dll")] static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);
    [DllImport("kernel32.dll")] public static extern uint SetThreadExecutionState(uint esFlags);
    public static double IdleSeconds() {
        var info = new LASTINPUTINFO(); info.cbSize = (uint)Marshal.SizeOf(info);
        GetLastInputInfo(ref info);
        return unchecked((uint)Environment.TickCount - info.dwTime) / 1000.0;
    }
}
'@

function Get-IdleSeconds { [IdleNative]::IdleSeconds() }
# ES_CONTINUOUS | ES_SYSTEM_REQUIRED: no sleep while a pass runs (the display may still turn off).
function Set-KeepAwake([bool]$on) { [void][IdleNative]::SetThreadExecutionState([uint32]$(if ($on) { 2147483649 } else { 2147483648 })) }

function Initialize-Worktree {
    if (Test-Path (Join-Path $Worktree '.git')) { return }
    Log "Creating worktree $Worktree on branch $Branch"
    $branchExists = (git -C $Repo branch --list $Branch) -ne $null
    if ($branchExists) { git -C $Repo worktree add $Worktree $Branch | Out-Null }
    else { git -C $Repo worktree add -b $Branch $Worktree HEAD | Out-Null }
    if ($LASTEXITCODE -ne 0) { throw "git worktree add failed" }
    if ($branchExists) { return }

    # One-time snapshot of the main checkout's uncommitted work so the idle branch starts from the live tree.
    $patch = Join-Path $StateDir 'snapshot.patch'
    git -C $Repo diff HEAD --binary "--output=$patch"
    if ((Test-Path $patch) -and (Get-Item $patch).Length -gt 0) {
        git -C $Worktree apply --whitespace=nowarn $patch
        if ($LASTEXITCODE -ne 0) { throw "Applying the uncommitted snapshot failed" }
    }
    foreach ($f in git -C $Repo ls-files --others --exclude-standard) {
        $dst = Join-Path $Worktree $f
        New-Item -ItemType Directory -Force (Split-Path -Parent $dst) | Out-Null
        Copy-Item (Join-Path $Repo $f) $dst -Force
    }
    # Gitignored config the game needs: active patch selection, settings, the patch files themselves.
    foreach ($f in 'GameData/PatchProfile.json', 'GameData/GeneralSettings.json') {
        if (Test-Path (Join-Path $Repo $f)) { Copy-Item (Join-Path $Repo $f) (Join-Path $Worktree $f) -Force }
    }
    Copy-Item (Join-Path $Repo 'GameData/Patches/*') (Join-Path $Worktree 'GameData/Patches') -Recurse -Force

    git -C $Worktree add -A | Out-Null
    git -C $Worktree commit -q -m "Idle balance: snapshot of the working tree at $(git -C $Repo rev-parse --short HEAD)" -m "Uncommitted changes from the main checkout on $(Get-Date -Format yyyy-MM-dd), the starting point for idle balance passes." | Out-Null
    Log "Snapshot committed: $(git -C $Worktree rev-parse --short HEAD)"
}

function Stop-Tree([int]$processId) { & taskkill.exe /PID $processId /T /F 2>$null | Out-Null }

# Waits while idle; returns $false as soon as the user is back.
function Wait-Idle([int]$seconds) {
    $end = (Get-Date).AddSeconds($seconds)
    while ((Get-Date) -lt $end) {
        if ((Get-IdleSeconds) -lt $ReturnIdleSeconds -or (Test-Path $PauseFile)) { return $false }
        Start-Sleep -Seconds 15
    }
    return $true
}

# --- single instance ---
if (Test-Path $LockFile) {
    $old = Get-Content $LockFile -ErrorAction SilentlyContinue
    if ($old -and (Get-Process -Id $old -ErrorAction SilentlyContinue)) { Log "Runner already active (pid $old), exiting"; exit 0 }
}
Set-Content $LockFile $PID

try {
    Initialize-Worktree
    if ($SetupOnly) { Log "Setup only: worktree ready at $Worktree"; exit 0 }

    $claude = (Get-Command claude -ErrorAction Stop).Source
    $allowed = @(
        'Read', 'Edit', 'Write', 'Glob', 'Grep', 'TodoWrite',
        'Bash(dotnet:*)', 'Bash(python:*)', 'Bash(py:*)', 'Bash(powershell:*)', 'Bash(pwsh:*)',
        'Bash(git status:*)', 'Bash(git diff:*)', 'Bash(git log:*)', 'Bash(git show:*)', 'Bash(git add:*)',
        'Bash(git commit:*)', 'Bash(git rev-parse:*)',
        'Bash(ls:*)', 'Bash(cat:*)', 'Bash(head:*)', 'Bash(tail:*)', 'Bash(grep:*)', 'Bash(wc:*)', 'Bash(find:*)',
        'Bash(mkdir:*)', 'Bash(cp:*)', 'Bash(sort:*)', 'Bash(diff:*)', 'Bash(nproc:*)', 'Bash(date:*)'
    ) -join ','
    # The scan (~8 min) and gate (10-25 min) run past Claude Code's default 10-minute Bash cap.
    $env:BASH_DEFAULT_TIMEOUT_MS = '3600000'
    $env:BASH_MAX_TIMEOUT_MS = '3600000'
    $denied ='Bash(git push:*),Bash(git stash:*),Bash(git reset:*),Bash(git checkout:*),Bash(git worktree:*),Bash(git merge:*),Bash(git rebase:*),WebFetch,WebSearch'

    while ($true) {
        if (Test-Path $PauseFile) { Log "Paused from the tray, exiting"; break }
        if ((Get-IdleSeconds) -lt $StartIdleSeconds) { Log "Not idle long enough ($([int](Get-IdleSeconds))s), exiting"; break }

        $headBefore = git -C $Worktree rev-parse HEAD
        $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
        $outLog = Join-Path $LogDir "pass-$stamp.log"
        Log "Starting pass $stamp at $($headBefore.Substring(0,8))"
        Set-KeepAwake $true
        $proc = Start-Process -FilePath $claude -WorkingDirectory $Worktree -WindowStyle Hidden -PassThru `
            -ArgumentList @('-p', '--permission-mode', 'dontAsk', '--allowedTools', "`"$allowed`"", '--disallowedTools', "`"$denied`"") `
            -RedirectStandardInput $PromptFile -RedirectStandardOutput $outLog -RedirectStandardError "$outLog.err"
        $started = Get-Date
        $userBack = $false
        while (-not $proc.HasExited) {
            Start-Sleep -Seconds 15
            if ((Get-IdleSeconds) -lt $ReturnIdleSeconds -or (Test-Path $PauseFile)) { $userBack = $true; Stop-Tree $proc.Id; break }
        }
        Set-KeepAwake $false
        $minutes = [math]::Round(((Get-Date) - $started).TotalMinutes, 1)
        $headAfter = git -C $Worktree rev-parse HEAD
        $commits = if ($headAfter -ne $headBefore) { (git -C $Worktree rev-list --count "$headBefore..$headAfter") } else { 0 }
        if ($userBack) { Log "User returned or paused after $minutes min; pass stopped ($commits commit(s) kept)"; break }
        Log "Pass finished in $minutes min, exit $($proc.ExitCode), $commits new commit(s)"

        # A pass that changed nothing (or failed fast, e.g. usage limit) waits before trying again.
        if ($commits -eq 0 -or $minutes -lt 5) {
            Log "Backing off $NoCommitBackoffMinutes idle min"
            if (-not (Wait-Idle ($NoCommitBackoffMinutes * 60))) { Log "User returned during backoff"; break }
        }
    }
}
finally {
    Set-KeepAwake $false
    Remove-Item $LockFile -ErrorAction SilentlyContinue
}
