# Quick Clean - Fast version for common cache issues
# Just removes bin/obj and rebuilds without clearing NuGet cache

Write-Host "Quick cleaning build cache..." -ForegroundColor Cyan

$codeDir = Join-Path $PSScriptRoot "..\Code"
if (-not (Test-Path $codeDir)) {
    Write-Host "Error: Code directory not found" -ForegroundColor Red
    exit 1
}

Set-Location $codeDir

Remove-Item -Recurse -Force -ErrorAction SilentlyContinue "bin", "obj"
dotnet clean --verbosity quiet 2>&1 | Out-Null
Start-Sleep -Milliseconds 300

Write-Host "Rebuilding..." -ForegroundColor Yellow
dotnet build

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nBuild succeeded!" -ForegroundColor Green
} else {
    Write-Host "`nBuild failed. Try running fix-build-cache.ps1 for a more thorough clean." -ForegroundColor Yellow
}

