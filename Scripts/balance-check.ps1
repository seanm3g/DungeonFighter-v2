<#
.SYNOPSIS
  Checks a balance patch against the designer targets in GameData/TuningProfiles/balance-targets.json.

.DESCRIPTION
  Builds the headless tools, then runs (in parallel where possible):
    - FUNDMATRIX --gear typical at every target level (win rate + hero turns, deterministic)
    - PLAYTODEATHBATCH --stop-at-level N on a scratch copy of GameData (survival)
    - ITEMSTATS (Rare share of drops)
  Prints a PASS/FAIL scorecard and writes GameData/TuningRuns/balance-check-<time>.json.
  Exit code 0 = every check passed.

.EXAMPLE
  .\Scripts\balance-check.ps1 -Patch overnight-v1
  .\Scripts\balance-check.ps1 -Patch overnight-v1 -RunsPerClass 10 -SkipSurvival
#>
param(
    [string]$Patch = "overnight-v1",
    [int]$Encounters = 80,
    [int]$RunsPerClass = 25,
    [switch]$SkipSurvival,
    [switch]$SkipBuild,
    # Also run the smart bot (skills, gear and sequence chosen by simulation), sharded across processes.
    [switch]$Smart,
    [int]$SmartShardsPerClass = 6,
    [int]$SmartRunsPerShard = 4
)

$ErrorActionPreference = "Stop"
$repo = Split-Path -Parent $PSScriptRoot
$targets = Get-Content (Join-Path $repo "GameData/TuningProfiles/balance-targets.json") -Raw | ConvertFrom-Json
$toolsDir = Join-Path $repo "Code/bin/BalanceTools"
# Start-Process joins arguments without quoting, so paths with spaces are wrapped in quotes below.
$dll = Join-Path $toolsDir "DF.dll"
$work = Join-Path ([System.IO.Path]::GetTempPath()) ("df-balance-check-" + [Guid]::NewGuid().ToString("N").Substring(0, 8))
New-Item -ItemType Directory -Force $work | Out-Null

if (-not $SkipBuild) {
    Write-Host "Building tools -> $toolsDir"
    # Intermediate files go outside Code/ (the project globs every .cs under it) and to a path without spaces.
    $objDir = Join-Path $work "obj\"
    dotnet build (Join-Path $repo "Code/Code.csproj") -c Debug -o $toolsDir "-p:BaseIntermediateOutputPath=$objDir" | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Build failed." }
}

function Get-TargetWinRate([int]$level) {
    $a = $targets.winRateAnchors | Sort-Object level
    if ($level -le $a[0].level) { return [double]$a[0].winRate }
    for ($i = 1; $i -lt $a.Count; $i++) {
        if ($level -le $a[$i].level) {
            $t = ($level - $a[$i - 1].level) / [double]($a[$i].level - $a[$i - 1].level)
            return $a[$i - 1].winRate + $t * ($a[$i].winRate - $a[$i - 1].winRate)
        }
    }
    return [double]$a[-1].winRate
}

# Anchors may be dungeon clear rates; the matrix measures single fights, so convert: fight = clear^(1/fights per dungeon).
function Get-TargetFightWinRate([int]$level) {
    $clear = Get-TargetWinRate $level
    if ($targets.winRateMeaning -ne "dungeonClear") { return $clear }
    if ($clear -le 0) { return 0.0 }
    return 100.0 * [math]::Pow($clear / 100.0, 1.0 / [double]$targets.fightsPerDungeon)
}

# Matrix: one process per level x weapon, in parallel. Each weapon has the same number of pairings,
# so the level result is the plain mean of the four weapon results (identical to one combined run).
$weapons = @("Mace", "Sword", "Dagger", "Wand")
$jobs = @()
foreach ($level in $targets.matrixLevels) {
    foreach ($weapon in $weapons) {
        $log = Join-Path $work "matrix-L$level-$weapon.log"
        $jobs += Start-Process dotnet -ArgumentList @("`"$dll`"", "FUNDMATRIX", "--patch", $Patch, "--gear", "typical", "--level", $level, "--weapons", $weapon, "--encounters", $Encounters, "--seed", "1", "--enemies", "`"$($targets.matrixEnemies)`"", "--label", "check-L$level-$weapon") `
            -WorkingDirectory $repo -RedirectStandardOutput $log -NoNewWindow -PassThru
    }
}

# Survival: scratch GameData so death-screen saves never touch the real folder.
$survivalLog = Join-Path $work "survival.log"
$survivalProc = $null
if (-not $SkipSurvival) {
    $scratchData = Join-Path $work "GameData"
    robocopy (Join-Path $repo "GameData") $scratchData /E /XD Audio Video CombatLogs LabSnapshots TuningRuns /XF "character_*" /NFL /NDL /NJH /NJS | Out-Null
    $env:DF_GAMEDATA_DIR = $scratchData
    $survivalProc = Start-Process dotnet -ArgumentList @("`"$dll`"", "PLAYTODEATHBATCH", $RunsPerClass, "--patch", $Patch, "--stop-at-level", ([int]$targets.realClearCheckLevel + 1), "--max-actions-per-run", "5000") `
        -WorkingDirectory $repo -RedirectStandardOutput $survivalLog -NoNewWindow -PassThru
    Remove-Item Env:DF_GAMEDATA_DIR
}

# Smart bot: shards per class so ~100 runs finish in a few minutes.
$classes = @("Barbarian", "Warrior", "Rogue", "Wizard")
$smartProcs = @()
if ($Smart -and -not $SkipSurvival) {
    $env:DF_GAMEDATA_DIR = $scratchData
    foreach ($class in $classes) {
        for ($shard = 1; $shard -le $SmartShardsPerClass; $shard++) {
            $log = Join-Path $work "smart-$class-$shard.log"
            $smartProcs += Start-Process dotnet -ArgumentList @("`"$dll`"", "PLAYTODEATHBATCH", $SmartRunsPerShard, "--classes", $class, "--smart", "--patch", $Patch, "--stop-at-level", ([int]$targets.realClearCheckLevel + 1), "--max-actions-per-run", "6000") `
                -WorkingDirectory $repo -RedirectStandardOutput $log -NoNewWindow -PassThru
        }
    }
    Remove-Item Env:DF_GAMEDATA_DIR
}

# Reads playthrough logs: per-class runs reaching the survival level, and losses / dungeon starts at the clear-check level.
function Read-Playthroughs([string[]]$logs, [int]$runsPerLog) {
    $reached = @{}; $runs = @{}; $losses = 0; $dungeons = 0
    foreach ($log in $logs) {
        if (-not (Test-Path $log)) { continue }
        $line = Select-String -Path $log -Pattern "Reached L$($targets.survivalToLevel):" | Select-Object -Last 1
        if ($line) {
            foreach ($m in [regex]::Matches($line.Line, "(Barbarian|Warrior|Rogue|Wizard) ([0-9.]+)%")) {
                $c = $m.Groups[1].Value
                $n = if ($runsPerLog -gt 0) { $runsPerLog } else { $RunsPerClass }
                $runs[$c] = [int]$runs[$c] + $n
                $reached[$c] = [double]$reached[$c] + [double]$m.Groups[2].Value / 100.0 * $n
            }
        }
        $row = Get-Content $log | Where-Object { $_ -match ("^\s+" + [int]$targets.realClearCheckLevel + "\s+\d+") } | Select-Object -First 1
        if ($row) {
            $cols = $row.Trim() -split "\s+"
            $losses += [int]$cols[2]; $dungeons += [int]$cols[7]
        }
    }
    return [pscustomobject]@{ Reached = $reached; Runs = $runs; Losses = $losses; Dungeons = $dungeons }
}

# Items (fast, runs while the others work).
$itemLog = Join-Path $work "items.log"
& dotnet $dll ITEMSTATS --patch $Patch --levels "1,50" --count 3000 *> $itemLog

$jobs | Wait-Process
if ($survivalProc) { $survivalProc | Wait-Process }
if ($smartProcs.Count -gt 0) { $smartProcs | Wait-Process }

$checks = @()
function Add-Check($name, $value, $target, [bool]$pass) {
    $script:checks += [pscustomobject]@{ Check = $name; Value = $value; Target = $target; Result = $(if ($pass) { "PASS" } else { "FAIL" }) }
}

foreach ($level in $targets.matrixLevels) {
    $wrs = @(); $turnsList = @()
    foreach ($weapon in $weapons) {
        $line = Select-String -Path (Join-Path $work "matrix-L$level-$weapon.log") -Pattern "^Overall:" | Select-Object -Last 1
        if ($line) {
            $wrs += [double]([regex]::Match($line.Line, "WR ([0-9.]+)%").Groups[1].Value)
            $turnsList += [double]([regex]::Match($line.Line, "hero turns ([0-9.]+)").Groups[1].Value)
        }
    }
    if ($wrs.Count -ne $weapons.Count) { Add-Check "L$level matrix" "missing results" "-" $false; continue }
    $wr = ($wrs | Measure-Object -Average).Average
    $turns = ($turnsList | Measure-Object -Average).Average
    $target = Get-TargetFightWinRate $level
    Add-Check "L$level fight win rate" ("{0:N1}%" -f $wr) ("{0:N1}% +/- {1}" -f $target, $targets.winRateTolerance) ([math]::Abs($wr - $target) -le $targets.winRateTolerance)
    if ($level -le $targets.heroTurnsCheckMaxLevel) {
        Add-Check "L$level hero turns" ("{0:N1}" -f $turns) ("{0} +/- {1}" -f $targets.heroTurnsTarget, $targets.heroTurnsTolerance) ([math]::Abs($turns - $targets.heroTurnsTarget) -le $targets.heroTurnsTolerance)
    }
}

if (-not $SkipSurvival) {
    $s = Select-String -Path $survivalLog -Pattern "Reached L$($targets.survivalToLevel):" | Select-Object -Last 1
    $pct = if ($s) { [double]([regex]::Match($s.Line, "all ([0-9.]+)%").Groups[1].Value) } else { -1 }
    Add-Check "Survival to L$($targets.survivalToLevel)" ("{0}%" -f $pct) (">= {0}%" -f $targets.minSurvivalPercent) ($pct -ge $targets.minSurvivalPercent)
    # Real dungeon clear rate at the check level (row of the per-level table: last column).
    $lvl = [int]$targets.realClearCheckLevel
    $row = Get-Content $survivalLog | Where-Object { $_ -match ("^\s+" + $lvl + "\s+\d+") } | Select-Object -First 1
    if ($row) {
        $clearReal = [double](($row.Trim() -split "\s+")[-1].TrimEnd('%'))
        $clearTarget = Get-TargetWinRate $lvl
        Add-Check "L$lvl real dungeon clear" ("{0:N1}%" -f $clearReal) ("{0:N1}% +/- {1}" -f $clearTarget, $targets.realClearTolerance) ([math]::Abs($clearReal - $clearTarget) -le $targets.realClearTolerance)
    } else { Add-Check "L$lvl real dungeon clear" "no data" "-" $false }
}

# Per-bot, per-class survival and the smart-vs-basic gap.
if (-not $SkipSurvival) {
    $botResults = [ordered]@{ "basic" = (Read-Playthroughs @($survivalLog) 0) }
    if ($Smart) { $botResults["smart"] = (Read-Playthroughs (Get-ChildItem $work -Filter "smart-*.log" | ForEach-Object FullName) $SmartRunsPerShard) }
    Write-Host ""
    Write-Host ("{0,-7} {1,-10} {2,-10} {3,-10} {4,-10} {5,-14}" -f "Bot", "Barbarian", "Warrior", "Rogue", "Wizard", "L$($targets.realClearCheckLevel) clear")
    foreach ($bot in $botResults.Keys) {
        $r = $botResults[$bot]
        $cells = foreach ($c in $classes) { if ($r.Runs[$c]) { "{0:N0}%" -f (100.0 * $r.Reached[$c] / $r.Runs[$c]) } else { "-" } }
        $clear = if ($r.Dungeons -gt 0) { "{0:N1}%" -f (100.0 * (1 - $r.Losses / [double]$r.Dungeons)) } else { "-" }
        Write-Host ("{0,-7} {1,-10} {2,-10} {3,-10} {4,-10} {5,-14}" -f $bot, $cells[0], $cells[1], $cells[2], $cells[3], $clear)
    }
    Write-Host "(class columns: % of runs reaching L$($targets.survivalToLevel))"
}

$rareLines = Select-String -Path $itemLog -Pattern "^\s+(1|50)\s" | ForEach-Object { $_.Line }
foreach ($l in $rareLines) {
    $cols = ($l.Trim() -split "\s+")
    $rare = [double]($cols[3].TrimEnd('%'))
    Add-Check ("Rare drops at L" + $cols[0]) ("{0:N1}%" -f $rare) ("{0}-{1}%" -f $targets.rarePercentMin, $targets.rarePercentMax) ($rare -ge $targets.rarePercentMin -and $rare -le $targets.rarePercentMax)
}

Write-Host ""
Write-Host "Balance check: patch $Patch"
$checks | Format-Table -AutoSize | Out-String | Write-Host
$failed = @($checks | Where-Object Result -eq "FAIL").Count
Write-Host ("{0} of {1} checks passed. Logs: {2}" -f ($checks.Count - $failed), $checks.Count, $work)

$outDir = Join-Path $repo "GameData/TuningRuns"
New-Item -ItemType Directory -Force $outDir | Out-Null
$checks | ConvertTo-Json | Set-Content -Encoding utf8 (Join-Path $outDir ("balance-check-{0}-{1}.json" -f $Patch, (Get-Date -Format "yyyyMMdd-HHmmss")))
exit $(if ($failed -eq 0) { 0 } else { 1 })
