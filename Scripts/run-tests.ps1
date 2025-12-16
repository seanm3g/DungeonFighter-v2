# Automated test execution script for PowerShell
# Runs all unit tests and generates coverage report

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  DUNGEON FIGHTER v2 - TEST SUITE" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location (Join-Path $scriptPath "..")

# Build the project first
Write-Host "Building project..." -ForegroundColor Yellow
$buildResult = dotnet build Code/Code.csproj --configuration Debug
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Running tests..." -ForegroundColor Yellow
Write-Host ""

# Run tests through the game's test system
$testExecutable = Join-Path (Get-Location) "Code\bin\Debug\net8.0\DF.exe"
if (Test-Path $testExecutable) {
    & $testExecutable --run-tests
} else {
    Write-Host "Test executable not found. Please build the project first." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  TEST EXECUTION COMPLETE" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
