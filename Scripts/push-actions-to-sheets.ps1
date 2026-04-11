# Push GameData/Actions.json to Google Sheets using SheetsPushConfig.json (OAuth 2.0 Desktop client).
# Requires: Google Sheets API enabled, spreadsheet shared with the service account (Editor).

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root

Write-Host "Pushing Actions.json to Google Sheets (PUSH_ACTIONS)..." -ForegroundColor Cyan
dotnet run --project Code\Code.csproj -- PUSH_ACTIONS
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host "Done." -ForegroundColor Green
