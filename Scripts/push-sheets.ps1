# Push Actions + optional WEAPONS/MODIFICATIONS/ARMOR/CLASSES tabs (SheetsPushConfig.json + OAuth).
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root

Write-Host "Pushing game data to Google Sheets (PUSH_SHEETS)..." -ForegroundColor Cyan
dotnet run --project Code\Code.csproj -- PUSH_SHEETS
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host "Done." -ForegroundColor Green
