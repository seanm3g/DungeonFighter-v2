# Playthrough balance loop: 10 runs x 4 classes per iteration (class-playthrough-balance profile)
param(
    [int]$RunsPerClass = 10,
    [int]$MaxIterations = 8,
    [switch]$DryRun,
    [switch]$SimOnly,
    [switch]$AnalyzeOnly
)

$ErrorActionPreference = "Stop"
$repoRoot = (Join-Path $PSScriptRoot "..") | Resolve-Path
$exe = Join-Path $repoRoot "Code\bin\_agent_build\DF.exe"

if (-not (Test-Path $exe)) {
    Write-Host "Building MCP/tuning binary..."
    & (Join-Path $PSScriptRoot "build-mcp.ps1")
}

$common = @("--runs-per-class", $RunsPerClass.ToString())

if ($SimOnly) {
    & $exe TUNESIM --profile class-playthrough-balance @common
    exit $LASTEXITCODE
}

if ($AnalyzeOnly) {
    & $exe TUNEANALYZE
    exit $LASTEXITCODE
}

$args = @("PLAYTHROUGHTUNING", "--max-iterations", $MaxIterations.ToString()) + $common
if ($DryRun) { $args += "--dry-run" }

Write-Host "Playthrough tune loop: $RunsPerClass runs/class, $MaxIterations max iterations"
& $exe @args
exit $LASTEXITCODE
