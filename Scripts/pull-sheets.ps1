# Pull from published CSV URLs in GameData/SheetsConfig.json (actions + optional tabs).
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root

Write-Host "Pulling game data from Google Sheets (PULL_SHEETS)..." -ForegroundColor Cyan
dotnet run --project Code\Code.csproj -- PULL_SHEETS
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host "Done." -ForegroundColor Green
