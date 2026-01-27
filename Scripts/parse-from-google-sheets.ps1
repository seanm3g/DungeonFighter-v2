# PowerShell script to parse actions from a published Google Sheets CSV URL
param(
    [Parameter(Mandatory=$true)]
    [string]$GoogleSheetsUrl,
    
    [Parameter(Mandatory=$false)]
    [string]$OutputPath = "GameData\Actions.json"
)

Write-Host "Parsing actions from Google Sheets..." -ForegroundColor Cyan
Write-Host "URL: $GoogleSheetsUrl" -ForegroundColor Yellow
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

# Run the parser with the Google Sheets URL
Write-Host "Running parser..." -ForegroundColor Cyan
dotnet run --project Code\Code.csproj -- PARSE $GoogleSheetsUrl $OutputPath

if ($LASTEXITCODE -eq 0)
{
    Write-Host ""
    Write-Host "Success! Actions parsed and saved to: $OutputPath" -ForegroundColor Green
}
else
{
    Write-Host ""
    Write-Host "Error: Parser failed" -ForegroundColor Red
    exit 1
}
