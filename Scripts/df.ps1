param(
    [Parameter(Position = 0)]
    [string]$Command = "help",

    [Parameter(Position = 1, ValueFromRemainingArguments = $true)]
    [string[]]$Args = @()
)

$ErrorActionPreference = "Stop"

function Write-Header([string]$text) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ("  " + $text) -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
}

function Get-RepoRoot {
    return (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
}

function Invoke-DotNet([string[]]$dotnetArgs) {
    & dotnet @dotnetArgs
    return $LASTEXITCODE
}

function Show-Help {
    Write-Header "DungeonFighter v2 - Scripts"
    Write-Host "Usage:" -ForegroundColor Yellow
    Write-Host "  .\Scripts\df.ps1 <command> [args]" -ForegroundColor White
    Write-Host "  .\Scripts\df.bat <command> [args]    (Windows wrapper)" -ForegroundColor White
    Write-Host ""
    Write-Host "Core commands:" -ForegroundColor Yellow
    Write-Host "  run                 Build + run the game (Release, self-contained single file to dist/)" -ForegroundColor White
    Write-Host "  build [Debug|Release] Build Code/Code.csproj (default: Debug)" -ForegroundColor White
    Write-Host "  test                Build Debug + run DF.exe --run-tests" -ForegroundColor White
    Write-Host "  clean               Quick clean (bin/obj + rebuild)" -ForegroundColor White
    Write-Host "  clean:fix           Thorough clean (clears NuGet cache too)" -ForegroundColor White
    Write-Host "  clean:all           Alias for clean:fix (kept for familiarity)" -ForegroundColor White
    Write-Host "  metrics             Show build/execution metrics" -ForegroundColor White
    Write-Host "  dist                Build the distribution zip (existing script)" -ForegroundColor White
    Write-Host ""
    Write-Host "Legacy / analysis commands (kept as-is):" -ForegroundColor Yellow
    Write-Host "  analyze:* / count:* / sheets:*  (see Scripts/ folder)" -ForegroundColor White
    Write-Host ""
}

function Ensure-CodeDir {
    $root = Get-RepoRoot
    $codeDir = Join-Path $root "Code"
    if (-not (Test-Path $codeDir)) {
        throw "Code directory not found at $codeDir"
    }
    return $codeDir
}

function Cmd-Build {
    $config = "Debug"
    if ($Args.Count -ge 1 -and -not [string]::IsNullOrWhiteSpace($Args[0])) {
        $config = $Args[0]
    }

    $root = Get-RepoRoot
    Set-Location $root
    Write-Header "BUILD ($config)"
    $exit = Invoke-DotNet @("build", "Code/Code.csproj", "--configuration", $config)
    if ($exit -ne 0) { exit $exit }
}

function Cmd-Test {
    $root = Get-RepoRoot
    Set-Location $root

    Write-Header "TEST SUITE"
    Write-Host "Building project..." -ForegroundColor Yellow
    $exit = Invoke-DotNet @("build", "Code/Code.csproj", "--configuration", "Debug")
    if ($exit -ne 0) { exit $exit }

    $testExe = Join-Path $root "Code\bin\Debug\net8.0\DF.exe"
    if (-not (Test-Path $testExe)) {
        throw "Test executable not found at $testExe"
    }

    Write-Host ""
    Write-Host "Running tests..." -ForegroundColor Yellow
    & $testExe "--run-tests"
    $exit = $LASTEXITCODE
    if ($exit -ne 0) { exit $exit }
}

function Cmd-Run {
    $root = Get-RepoRoot
    Set-Location $root

    Write-Header "RUN (Release publish -> dist/DF.exe)"

    Write-Host "Killing any running DF.exe processes..." -ForegroundColor Yellow
    try { taskkill /f /im DF.exe 2>$null | Out-Null } catch { }

    $codeDir = Ensure-CodeDir
    Set-Location $codeDir

    Write-Host "Publishing..." -ForegroundColor Yellow
    $distDir = Join-Path $root "dist"
    $exit = Invoke-DotNet @(
        "publish",
        "-c", "Release",
        "-r", "win-x64",
        "--self-contained", "true",
        "-p:PublishSingleFile=true",
        "-o", $distDir
    )
    if ($exit -ne 0) { exit $exit }

    $exe = Join-Path $distDir "DF.exe"
    if (-not (Test-Path $exe)) {
        throw "Publish succeeded but DF.exe not found at $exe"
    }

    Write-Host ""
    Write-Host "Starting DF.exe..." -ForegroundColor Yellow
    Set-Location $root
    Start-Process -FilePath $exe -WorkingDirectory $distDir | Out-Null
}

function Cmd-CleanQuick {
    $script = Join-Path $PSScriptRoot "quick-clean.ps1"
    if (-not (Test-Path $script)) { throw "Missing script: $script" }
    & $script
    $exit = $LASTEXITCODE
    if ($exit -ne 0) { exit $exit }
}

function Cmd-CleanFix {
    $script = Join-Path $PSScriptRoot "fix-build-cache.ps1"
    if (-not (Test-Path $script)) { throw "Missing script: $script" }
    & $script
    $exit = $LASTEXITCODE
    if ($exit -ne 0) { exit $exit }
}

function Cmd-Metrics {
    $script = Join-Path $PSScriptRoot "show-metrics.ps1"
    if (-not (Test-Path $script)) { throw "Missing script: $script" }
    & $script
    $exit = $LASTEXITCODE
    if ($exit -ne 0) { exit $exit }
}

function Cmd-Dist {
    $script = Join-Path $PSScriptRoot "build-distribution.bat"
    if (-not (Test-Path $script)) { throw "Missing script: $script" }
    cmd /c "`"$script`""
    $exit = $LASTEXITCODE
    if ($exit -ne 0) { exit $exit }
}

switch ($Command.ToLowerInvariant()) {
    "help" { Show-Help }
    "-h" { Show-Help }
    "--help" { Show-Help }

    "run" { Cmd-Run }
    "build" { Cmd-Build }
    "test" { Cmd-Test }
    "metrics" { Cmd-Metrics }
    "dist" { Cmd-Dist }

    "clean" { Cmd-CleanQuick }
    "clean:fix" { Cmd-CleanFix }
    "clean:all" { Cmd-CleanFix }

    default {
        Write-Host "Unknown command: $Command" -ForegroundColor Red
        Show-Help
        exit 1
    }
}

