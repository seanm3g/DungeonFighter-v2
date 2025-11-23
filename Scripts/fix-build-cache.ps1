# Fix Build Cache Issues
# This script performs a thorough clean of build artifacts to resolve
# compilation issues where the compiler can't find methods that clearly exist

Write-Host "Cleaning build cache..." -ForegroundColor Cyan

# Navigate to Code directory
$codeDir = Join-Path $PSScriptRoot "..\Code"
if (-not (Test-Path $codeDir)) {
    Write-Host "Error: Code directory not found at $codeDir" -ForegroundColor Red
    exit 1
}

Set-Location $codeDir

# Remove bin and obj folders
Write-Host "Removing bin and obj folders..." -ForegroundColor Yellow
Remove-Item -Recurse -Force -ErrorAction SilentlyContinue "bin"
Remove-Item -Recurse -Force -ErrorAction SilentlyContinue "obj"

# Clean using dotnet
Write-Host "Running dotnet clean..." -ForegroundColor Yellow
dotnet clean --verbosity quiet 2>&1 | Out-Null

# Clear NuGet cache (optional, but can help with stubborn issues)
Write-Host "Clearing NuGet cache..." -ForegroundColor Yellow
dotnet nuget locals all --clear 2>&1 | Out-Null

# Wait a moment for file system to catch up
Start-Sleep -Milliseconds 500

# Rebuild
Write-Host "Rebuilding project..." -ForegroundColor Yellow
$buildResult = dotnet build 2>&1

# Check if build succeeded
if ($LASTEXITCODE -eq 0) {
    Write-Host "`nBuild succeeded! Cache cleared successfully." -ForegroundColor Green
    exit 0
} else {
    Write-Host "`nBuild failed. Errors:" -ForegroundColor Red
    $buildResult | Select-String -Pattern "error|warning" | ForEach-Object {
        Write-Host $_.Line -ForegroundColor Red
    }
    exit 1
}

