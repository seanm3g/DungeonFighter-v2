# Analyze latest debug output
Write-Host "Analyzing latest debug output..." -ForegroundColor Green
Write-Host ""

# Find the most recent debug analysis file
$debugFiles = Get-ChildItem -Path "Code\DebugAnalysis\debug_analysis_*.txt" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending

if ($debugFiles.Count -gt 0) {
    $latestFile = $debugFiles[0]
    Write-Host "Latest debug file: $($latestFile.Name)" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "===== DEBUG ANALYSIS =====" -ForegroundColor Cyan
    Get-Content $latestFile.FullName
} else {
    Write-Host "No debug analysis files found." -ForegroundColor Red
    Write-Host "Run the game and select option 5 to generate debug output." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Press any key to continue..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
