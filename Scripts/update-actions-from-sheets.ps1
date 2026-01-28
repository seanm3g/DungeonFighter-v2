# PowerShell script to update Actions.json from Google Sheets
param(
    [Parameter(Mandatory=$false)]
    [string]$GoogleSheetsUrl,
    
    [Parameter(Mandatory=$false)]
    [string]$OutputPath = "GameData\Actions.json"
)

Write-Host "Updating Actions.json from Google Sheets..." -ForegroundColor Cyan

if ($GoogleSheetsUrl)
{
    Write-Host "URL: $GoogleSheetsUrl" -ForegroundColor Yellow
}
else
{
    Write-Host "Using URL from GameData\SheetsConfig.json" -ForegroundColor Yellow
}

Write-Host "Output: $OutputPath" -ForegroundColor Yellow
Write-Host ""

# Build the project first
Write-Host "Building project..." -ForegroundColor Cyan
$buildResult = dotnet build "Code\Code.csproj" 2>&1
if ($LASTEXITCODE -ne 0)
{
    Write-Host "Build failed!" -ForegroundColor Red
    Write-Host $buildResult
    exit 1
}

# Run the update service
Write-Host "Updating actions..." -ForegroundColor Cyan
if ($GoogleSheetsUrl)
{
    dotnet run --project Code\Code.csproj -- UPDATE_ACTIONS $GoogleSheetsUrl $OutputPath
}
else
{
    # Don't pass empty string - let the service load from config
    dotnet run --project Code\Code.csproj -- UPDATE_ACTIONS
}

if ($LASTEXITCODE -eq 0)
{
    Write-Host ""
    Write-Host "Success! Actions updated and saved to: $OutputPath" -ForegroundColor Green
}
else
{
    Write-Host ""
    Write-Host "Error: Update failed" -ForegroundColor Red
    exit 1
}
