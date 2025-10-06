# PowerShell script to run balance analysis and save to timestamped file
Write-Host "Running Balance Analysis and saving to file..." -ForegroundColor Green
Write-Host ""

# Create timestamped filename
$timestamp = Get-Date -Format "HHmmss"
$filename = "balance_$timestamp.txt"

Write-Host "Saving output to: $filename" -ForegroundColor Yellow

# Run the game and save output to timestamped file
dotnet run > $filename 2>&1

Write-Host ""
Write-Host "Balance analysis complete!" -ForegroundColor Green
Write-Host "Output saved to: $filename" -ForegroundColor Yellow
Write-Host ""
Write-Host "Press any key to continue..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
