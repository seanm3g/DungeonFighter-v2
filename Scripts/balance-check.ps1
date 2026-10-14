<#
.SYNOPSIS
  Checks a balance patch against the designer targets in GameData/TuningProfiles/balance-targets.json.

.DESCRIPTION
  Builds the headless tools, then runs (in parallel where possible):
    - Median catalog build at every target level (Scripts/median-curve-solve.py --check: win rate + hero turns);
      -BareHero adds the old FUNDMATRIX --gear typical one-action hero as reference rows
    - PLAYTODEATHBATCH --smart --stop-at-level N on a scratch copy of GameData (survival, L10 clear; -BasicBot adds the
      non-optimizing bot as a reported floor)
    - ITEMSTATS (Rare share of drops)
  Prints a PASS/FAIL scorecard and writes GameData/TuningRuns/balance-check-<time>.json.
  Exit code 0 = every check passed.

.EXAMPLE
  .\Scripts\balance-check.ps1                      (active patch from GameData/PatchProfile.json)
  .\Scripts\balance-check.ps1 -RunsPerClass 10 -SkipSurvival
#>
param(
    # Default: the active balance patch in GameData/PatchProfile.json (what the game loads).
    [string]$Patch = "",
    [int]$Encounters = 80,
    [int]$RunsPerClass = 25,
    [switch]$SkipSurvival,
    [switch]$SkipBuild,
    # Also run the smart bot (skills, gear and sequence chosen by simulation), sharded across processes.
    # Survival / L10 clear are measured with the smart bot (skills, gear and sequence chosen by simulation), the player
    # enemies are calibrated for: 6 shards x 4 runs = 24 runs per class (~7 CPU-min per run), in parallel. -Smart is kept for old command lines.
    [switch]$Smart,
    [int]$SmartShardsPerClass = 6,
    [int]$SmartRunsPerShard = 4,
    # Also run the basic bot (never optimizes; floor check, reported only). -RunsPerClass sets its runs per class.
    [switch]$BasicBot,
    # Basic-bot survival runs are split into this many parallel processes per class (1 = legacy single process).
    [int]$BasicShardsPerClass = 4,
    # Fight rows measure the median catalog build at each level's stepping stone (Scripts/median-curve-solve.py --check),
    # the hero enemies are calibrated to. -BareHero also runs the old typical-gear one-action matrix (FUNDMATRIX).
    [switch]$BareHero,
    [int]$MedianEncounters = 8,
    # Build/output folder for the tools (use another folder when the default DF.dll is locked by running sims).
    [string]$ToolsDir = "",
    # Real dungeon clear above the survival level (Scripts/dungeon-sim.py: HP carried between fights, retreat rule)
    # at every realClearAnchors / matrix level between realClearCheckLevel and 100. ~8 min per level.
    [switch]$SkipDungeonSim,
    [int]$DungeonSimShards = 6
)

$ErrorActionPreference = "Stop"
$repo = Split-Path -Parent $PSScriptRoot
if (-not $Patch) {
    $profilePath = Join-Path $repo "GameData/PatchProfile.json"
    $Patch = if (Test-Path $profilePath) { (Get-Content $profilePath -Raw | ConvertFrom-Json).activeBalancePatch } else { "" }
    if (-not $Patch) { $Patch = "default" }
}
$targets = Get-Content (Join-Path $repo "GameData/TuningProfiles/balance-targets.json") -Raw | ConvertFrom-Json
# Playthrough bots leave a dungeon between rooms below this % HP (leaving fully heals; 0 = never).
$retreatBelow = if ($targets.retreatBelowHpPercent) { [int]$targets.retreatBelowHpPercent } else { 0 }
$toolsDir = if ($ToolsDir) { $ToolsDir } else { Join-Path $repo "Code/bin/BalanceTools" }
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
$medianJson = Join-Path $work "median.json"
$medianLog = Join-Path $work "median.log"
$jobs += Start-Process python -ArgumentList @("`"$(Join-Path $repo 'Scripts/median-curve-solve.py')`"", "--check", "--patch", $Patch, "--tools", "`"$toolsDir`"", "--encounters", $MedianEncounters, "--parallel", "10", "--json", "`"$medianJson`"") `
    -WorkingDirectory $repo -RedirectStandardOutput $medianLog -NoNewWindow -PassThru

# Real dungeon clear at mid/late levels (dungeon-sim.py copies its own scratch GameData).
$dsimJson = Join-Path $work "dungeon-sim.json"
$dsimLevels = @()
if ($targets.realClearAnchors -and -not $SkipDungeonSim) {
    $dsimLevels = @(@($targets.realClearAnchors | ForEach-Object { [int]$_.level }) + @($targets.matrixLevels | ForEach-Object { [int]$_ }) |
        Where-Object { $_ -gt [int]$targets.realClearCheckLevel -and $_ -lt 100 } | Sort-Object -Unique)
    if ($dsimLevels.Count -gt 0) {
        $jobs += Start-Process python -ArgumentList @("`"$(Join-Path $repo 'Scripts/dungeon-sim.py')`"", "--levels", ($dsimLevels -join ","), "--patch", $Patch, "--tools", "`"$toolsDir`"", "--shards", $DungeonSimShards, "--json", "`"$dsimJson`"") `
            -WorkingDirectory $repo -RedirectStandardOutput (Join-Path $work "dungeon-sim.log") -NoNewWindow -PassThru
    }
}

foreach ($level in $(if ($BareHero) { $targets.matrixLevels } else { @() })) {
    foreach ($weapon in $weapons) {
        $log = Join-Path $work "matrix-L$level-$weapon.log"
        $jobs += Start-Process dotnet -ArgumentList @("`"$dll`"", "FUNDMATRIX", "--patch", $Patch, "--gear", "typical", "--level", $level, "--weapons", $weapon, "--encounters", $Encounters, "--seed", "1", "--enemies", "`"$($targets.matrixEnemies)`"", "--label", "check-L$level-$weapon") `
            -WorkingDirectory $repo -RedirectStandardOutput $log -NoNewWindow -PassThru
    }
}

# Survival: scratch GameData so death-screen saves never touch the real folder.
$survivalProc = $null
if (-not $SkipSurvival) {
    $scratchData = Join-Path $work "GameData"
    robocopy (Join-Path $repo "GameData") $scratchData /E /XD Audio Video CombatLogs LabSnapshots TuningRuns /XF "character_*" /NFL /NDL /NJH /NJS | Out-Null
}
if ($BasicBot -and -not $SkipSurvival) {
    $env:DF_GAMEDATA_DIR = $scratchData
    # Sharded per class like the smart bot: one process ran all classes in sequence (~20 min of a ~25 min check).
    $basicRunsPerShard = [int][math]::Ceiling($RunsPerClass / [double][math]::Max(1, $BasicShardsPerClass))
    $survivalProc = @()
    foreach ($class in @("Barbarian", "Warrior", "Rogue", "Wizard")) {
        for ($shard = 1; $shard -le [math]::Max(1, $BasicShardsPerClass); $shard++) {
            $log = Join-Path $work "survival-$class-$shard.log"
            $survivalProc += Start-Process dotnet -ArgumentList @("`"$dll`"", "PLAYTODEATHBATCH", $basicRunsPerShard, "--classes", $class, "--patch", $Patch, "--stop-at-level", ([int]$targets.realClearCheckLevel + 1), "--retreat-below", $retreatBelow, "--max-actions-per-run", "5000") `
                -WorkingDirectory $repo -RedirectStandardOutput $log -NoNewWindow -PassThru
        }
    }
    Remove-Item Env:DF_GAMEDATA_DIR
}

# Smart bot: shards per class (each playthrough simulates its choices, ~7 CPU-min per run to L10).
$classes = @("Barbarian", "Warrior", "Rogue", "Wizard")
$smartProcs = @()
if (-not $SkipSurvival) {
    $env:DF_GAMEDATA_DIR = $scratchData
    foreach ($class in $classes) {
        for ($shard = 1; $shard -le $SmartShardsPerClass; $shard++) {
            $log = Join-Path $work "smart-$class-$shard.log"
            $fightsJson = Join-Path $work "fights-$class-$shard.json"
            $smartProcs += Start-Process dotnet -ArgumentList @("`"$dll`"", "PLAYTODEATHBATCH", $SmartRunsPerShard, "--classes", $class, "--smart", "--patch", $Patch, "--stop-at-level", ([int]$targets.realClearCheckLevel + 1), "--retreat-below", $retreatBelow, "--max-actions-per-run", "6000", "--fights-json", "`"$fightsJson`"") `
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
            # Deaths and retreats (last column, when present) both leave the dungeon uncleared.
            $losses += [int]$cols[2]; $dungeons += [int]$cols[7]
            if ($cols.Count -gt 9) { $losses += [int]$cols[9] }
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
# Reference rows (not counted): Result "info".
function Add-Info($name, $value, $target, [bool]$pass) {
    $script:checks += [pscustomobject]@{ Check = $name; Value = $value; Target = $target; Result = $(if ($pass) { "info" } else { "info (off)" }) }
}
function Add-Check($name, $value, $target, [bool]$pass, [bool]$unsure = $false) {
    $result = if ($pass) { "PASS" } elseif ($unsure) { "UNSURE" } else { "FAIL" }
    $script:checks += [pscustomobject]@{ Check = $name; Value = $value; Target = $target; Result = $result }
}

# 95% Wilson interval for k successes in n trials, as percentages.
function Get-Wilson([double]$k, [double]$n) {
    if ($n -le 0) { return @(0.0, 100.0) }
    $z = 1.96; $p = $k / $n; $den = 1 + $z * $z / $n
    $c = ($p + $z * $z / (2 * $n)) / $den
    $h = $z * [math]::Sqrt($p * (1 - $p) / $n + $z * $z / (4 * $n * $n)) / $den
    return @([math]::Max(0, ($c - $h) * 100), [math]::Min(100, ($c + $h) * 100))
}

# Fight rows: median catalog build (what enemies are calibrated to).
$median = if (Test-Path $medianJson) { Get-Content $medianJson -Raw | ConvertFrom-Json } else { @() }
foreach ($level in $targets.matrixLevels) {
    $row = $median | Where-Object { $_.level -eq $level } | Select-Object -First 1
    if (-not $row) { Add-Check "L$level median build" "missing results (see median.log)" "-" $false; continue }
    $target = Get-TargetFightWinRate $level
    if ($targets.realClearAnchors) {
        # Single fights from full HP: reference only once real dungeon clear (HP carried between fights) is the target.
        $script:checks += [pscustomobject]@{ Check = "L$level fight win rate (full HP)"; Value = ("{0:N1}%" -f $row.winRate); Target = ("{0:N1}% ref" -f $target); Result = "INFO" }
    } else {
        Add-Check "L$level fight win rate" ("{0:N1}%" -f $row.winRate) ("{0:N1}% +/- {1}" -f $target, $targets.winRateTolerance) ([math]::Abs($row.winRate - $target) -le $targets.winRateTolerance)
    }
    if ($level -le $targets.heroTurnsCheckMaxLevel) {
        Add-Check "L$level hero turns" ("{0:N1}" -f $row.heroTurns) ("{0} +/- {1}" -f $targets.heroTurnsTarget, $targets.heroTurnsTolerance) ([math]::Abs($row.heroTurns - $targets.heroTurnsTarget) -le $targets.heroTurnsTolerance)
        # Per enemy: the slowest / fastest single enemy for the median build (a playtest found a 21-turn L1 rat pack
        # the level mean hid). perEnemyTurnsTolerance is wider than the level tolerance: enemies may differ, not double.
        if ($row.perEnemy -and $null -ne $targets.perEnemyTurnsTolerance) {
            $per = @($row.perEnemy.PSObject.Properties | ForEach-Object { [pscustomobject]@{ Name = $_.Name; Turns = [double]$_.Value.heroTurns; P90 = [double]$_.Value.heroTurnsP90 } })
            $slow = $per | Sort-Object Turns -Descending | Select-Object -First 1
            $fast = $per | Sort-Object Turns | Select-Object -First 1
            $tol = [double]$targets.perEnemyTurnsTolerance
            $off = @($per | Where-Object { [math]::Abs($_.Turns - $targets.heroTurnsTarget) -gt $tol } | ForEach-Object { "{0} {1:N1}" -f $_.Name, $_.Turns })
            $value = if ($off.Count) { "off: " + ($off -join ", ") } else { "{0} {1:N1} (p90 {2:N0}) .. {3} {4:N1}" -f $slow.Name, $slow.Turns, $slow.P90, $fast.Name, $fast.Turns }
            Add-Check "L$level per-enemy turns" $value ("each {0} +/- {1}" -f $targets.heroTurnsTarget, $tol) ($off.Count -eq 0)
        }
    }
}

# -BareHero: typical gear, one action, no build (reference only; enemies are not calibrated to it).
foreach ($level in $(if ($BareHero) { $targets.matrixLevels } else { @() })) {
    $wrs = @(); $turnsList = @()
    foreach ($weapon in $weapons) {
        $line = Select-String -Path (Join-Path $work "matrix-L$level-$weapon.log") -Pattern "^Overall:" | Select-Object -Last 1
        if ($line) {
            $wrs += [double]([regex]::Match($line.Line, "WR ([0-9.]+)%").Groups[1].Value)
            $turnsList += [double]([regex]::Match($line.Line, "hero turns ([0-9.]+)").Groups[1].Value)
        }
    }
    if ($wrs.Count -ne $weapons.Count) { Add-Info "L$level bare hero matrix" "missing results" "-" $false; continue }
    $wr = ($wrs | Measure-Object -Average).Average
    $turns = ($turnsList | Measure-Object -Average).Average
    $target = Get-TargetFightWinRate $level
    Add-Info "L$level bare hero win rate" ("{0:N1}%" -f $wr) ("{0:N1}% +/- {1}" -f $target, $targets.winRateTolerance) ([math]::Abs($wr - $target) -le $targets.winRateTolerance)
    if ($level -le $targets.heroTurnsCheckMaxLevel) {
        Add-Info "L$level bare hero turns" ("{0:N1}" -f $turns) ("{0} +/- {1}" -f $targets.heroTurnsTarget, $targets.heroTurnsTolerance) ([math]::Abs($turns - $targets.heroTurnsTarget) -le $targets.heroTurnsTolerance)
    }
}

$survivalLogs = @(Get-ChildItem $work -Filter "survival-*.log" -ErrorAction SilentlyContinue | ForEach-Object FullName)
if (-not $SkipSurvival) {
    # Gate on the smart bot; the basic bot (-BasicBot) is reported in the table below only.
    $smartBot = Read-Playthroughs (Get-ChildItem $work -Filter "smart-*.log" | ForEach-Object FullName) $SmartRunsPerShard
    $basic = if ($BasicBot) { Read-Playthroughs $survivalLogs $basicRunsPerShard } else { $null }
    $totalRuns = ($smartBot.Runs.Values | Measure-Object -Sum).Sum
    $totalReached = ($smartBot.Reached.Values | Measure-Object -Sum).Sum
    $pct = if ($totalRuns -gt 0) { [math]::Round(100.0 * $totalReached / $totalRuns, 1) } else { -1 }
    # PASS at or above target; UNSURE when below but the 95% interval still reaches it; FAIL only when it does not.
    $ci = Get-Wilson $totalReached $totalRuns
    Add-Check "Survival to L$($targets.survivalToLevel)" ("{0}% [{1:N0}-{2:N0}] n={3}" -f $pct, $ci[0], $ci[1], $totalRuns) (">= {0}%" -f $targets.minSurvivalPercent) ($pct -ge $targets.minSurvivalPercent) ($ci[1] -ge $targets.minSurvivalPercent)
    # Real dungeon clear rate at the check level (row of the per-level table: last column).
    $lvl = [int]$targets.realClearCheckLevel
    if ($smartBot.Dungeons -gt 0) {
        $clearReal = 100.0 * (1 - $smartBot.Losses / [double]$smartBot.Dungeons)
        # realClearTarget: retreat-aware playthrough target; the anchors stay per-fight solver targets.
        $clearTarget = if ($null -ne $targets.realClearTarget) { [double]$targets.realClearTarget } else { Get-TargetWinRate $lvl }
        $ci = Get-Wilson ($smartBot.Dungeons - $smartBot.Losses) $smartBot.Dungeons
        $lo = $clearTarget - $targets.realClearTolerance; $hi = $clearTarget + $targets.realClearTolerance
        Add-Check "L$lvl real dungeon clear" ("{0:N1}% [{1:N0}-{2:N0}] n={3}" -f $clearReal, $ci[0], $ci[1], $smartBot.Dungeons) ("{0:N1}% +/- {1}" -f $clearTarget, $targets.realClearTolerance) ([math]::Abs($clearReal - $clearTarget) -le $targets.realClearTolerance) ($ci[1] -ge $lo -and $ci[0] -le $hi)
    } else { Add-Check "L$lvl real dungeon clear" "no data" "-" $false }
}

# Real fights by enemy (smart-bot playthroughs, every fight to the survival level). A playtest found a 21-turn L1 rat
# pack that every averaged row hid: gate each enemy's mean hero actions per level band, and print the slowest tails.
if (-not $SkipSurvival -and $null -ne $targets.perEnemyTurnsTolerance) {
    $fights = @(Get-ChildItem $work -Filter "fights-*.json" -ErrorAction SilentlyContinue | ForEach-Object { Get-Content $_.FullName -Raw | ConvertFrom-Json } | ForEach-Object { $_ })
    $minFights = if ($targets.perEnemyMinFights) { [int]$targets.perEnemyMinFights } else { 20 }
    $tol = [double]$targets.perEnemyTurnsTolerance
    foreach ($band in @(@(1, 3), @(4, 6), @(7, $targets.survivalToLevel))) {
        $inBand = @($fights | Where-Object { $_.HeroLevel -ge $band[0] -and $_.HeroLevel -le $band[1] })
        if ($inBand.Count -eq 0) { continue }
        $per = foreach ($g in ($inBand | Group-Object Enemy)) {
            $t = @($g.Group | ForEach-Object { [int]$_.HeroActions } | Sort-Object)
            [pscustomobject]@{ Name = $g.Name; N = $t.Count; Mean = ($t | Measure-Object -Average).Average
                P90 = $t[[math]::Min($t.Count - 1, [math]::Ceiling(0.9 * $t.Count) - 1)]; Max = $t[-1] }
        }
        $sized = @($per | Where-Object { $_.N -ge $minFights })
        $off = @($sized | Where-Object { [math]::Abs($_.Mean - $targets.heroTurnsTarget) -gt $tol } | Sort-Object Mean -Descending)
        $slow = $sized | Sort-Object Mean -Descending | Select-Object -First 1
        $value = if ($off.Count) { "off: " + (($off | ForEach-Object { "{0} {1:N1} (p90 {2}, max {3}, n={4})" -f $_.Name, $_.Mean, $_.P90, $_.Max, $_.N }) -join "; ") }
                 elseif ($slow) { "slowest {0} {1:N1} (p90 {2}, max {3}, n={4})" -f $slow.Name, $slow.Mean, $slow.P90, $slow.Max, $slow.N } else { "too few fights" }
        Add-Check ("L{0}-{1} real fights per enemy" -f $band[0], $band[1]) $value ("each {0} +/- {1}, n>={2}" -f $targets.heroTurnsTarget, $tol, $minFights) ($off.Count -eq 0)
    }
}

# Per-bot, per-class survival and the smart-vs-basic gap.
if (-not $SkipSurvival) {
    $botResults = [ordered]@{ "smart" = $smartBot }
    if ($basic) { $botResults["basic"] = $basic }
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

# Real dungeon clear (dungeon-sim.py) against realClearAnchors, interpolated by level.
function Get-RealClearTarget([int]$level) {
    $a = $targets.realClearAnchors | Sort-Object level
    if ($level -le $a[0].level) { return [double]$a[0].clear }
    for ($i = 1; $i -lt $a.Count; $i++) {
        if ($level -le $a[$i].level) {
            $t = ($level - $a[$i - 1].level) / [double]($a[$i].level - $a[$i - 1].level)
            return $a[$i - 1].clear + $t * ($a[$i].clear - $a[$i - 1].clear)
        }
    }
    return [double]$a[-1].clear
}
if ($dsimLevels.Count -gt 0) {
    $dsim = if (Test-Path $dsimJson) { Get-Content $dsimJson -Raw | ConvertFrom-Json } else { @() }
    foreach ($level in $dsimLevels) {
        $row = $dsim | Where-Object { $_.level -eq $level } | Select-Object -First 1
        if (-not $row) { Add-Check "L$level real dungeon clear (sim)" "missing results (see dungeon-sim.log)" "-" $false; continue }
        $target = Get-RealClearTarget $level
        $v = 100 * $row.clear; $lo = 100 * $row.clearCI[0]; $hi = 100 * $row.clearCI[1]
        $tol = $targets.realClearTolerance
        Add-Check "L$level real dungeon clear (sim)" ("{0:N1}% [{1:N0}-{2:N0}] n={3}, death {4:N1}%" -f $v, $lo, $hi, $row.dungeons, (100 * $row.death)) ("{0:N1}% +/- {1}" -f $target, $tol) ([math]::Abs($v - $target) -le $tol) ($hi -ge $target - $tol -and $lo -le $target + $tol)
    }
}

# Which code / data / patch produced these numbers (first tool log that printed it).
$provenance = Get-ChildItem $work -Filter "*.log" -ErrorAction SilentlyContinue |
    ForEach-Object { Select-String -Path $_.FullName -Pattern "^Provenance: (.*)$" -List } |
    ForEach-Object { $_.Matches[0].Groups[1].Value } | Sort-Object -Unique
$checks += [pscustomobject]@{ Check = "Provenance"; Value = ($provenance -join " | "); Target = "-"; Result = $(if (@($provenance).Count -gt 1) { "MIXED" } else { "INFO" }) }

Write-Host ""
Write-Host "Balance check: patch $Patch"
$checks | Format-Table -AutoSize | Out-String | Write-Host
$failed = @($checks | Where-Object Result -eq "FAIL").Count
$unsure = @($checks | Where-Object Result -eq "UNSURE").Count
$counted = @($checks | Where-Object { $_.Result -eq "PASS" -or $_.Result -eq "FAIL" -or $_.Result -eq "UNSURE" }).Count
Write-Host ("{0} of {1} checks passed{2}. Logs: {3}" -f ($counted - $failed - $unsure), $counted, $(if ($unsure) { ", $unsure unsure (inside sampling noise: rerun with more runs)" } else { "" }), $work)

$outDir = Join-Path $repo "GameData/TuningRuns"
New-Item -ItemType Directory -Force $outDir | Out-Null
$checks | ConvertTo-Json | Set-Content -Encoding utf8 (Join-Path $outDir ("balance-check-{0}-{1}.json" -f $Patch, (Get-Date -Format "yyyyMMdd-HHmmss")))
exit $(if ($failed -eq 0 -and $unsure -eq 0) { 0 } else { 1 })
