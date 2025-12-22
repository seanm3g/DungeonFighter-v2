# Count lines in GameData JSON files and analyze configuration files
# Usage: .\Scripts\count-gamedata-lines.ps1

Write-Host "=== GameData/JSON File Analysis ===" -ForegroundColor Cyan
Write-Host ""

# Get all JSON files in GameData folder
$jsonFiles = Get-ChildItem -Path GameData -Filter *.json -Recurse | 
    Where-Object { $_.FullName -notmatch 'backup|\.backup' } |
    ForEach-Object {
        $relPath = $_.FullName.Replace((Get-Location).Path + '\', '')
        $folderMatch = $relPath -match '^GameData\\([^\\]+)'
        $folder = if ($folderMatch) { $matches[1] } else { "Root" }
        
        try {
            $content = Get-Content $_.FullName -ReadCount 0 -Raw -ErrorAction Stop
            $lineCount = ($content -split "`r?`n").Count
            $fileSize = $_.Length
            [PSCustomObject]@{
                File = $relPath
                Folder = $folder
                Lines = $lineCount
                SizeKB = [math]::Round($fileSize / 1KB, 2)
            }
        } catch {
            Write-Warning "Skipping file due to read error: $relPath"
            $null
        }
    } | Where-Object { $null -ne $_ }

# Calculate totals
$totalJsonFiles = ($jsonFiles | Measure-Object).Count
$totalJsonLines = ($jsonFiles | Measure-Object -Property Lines -Sum).Sum
$totalJsonSize = ($jsonFiles | Measure-Object -Property SizeKB -Sum).Sum

# Get C# code stats for comparison
$codeFiles = Get-ChildItem -Path Code -Filter *.cs -Recurse | 
    Where-Object { $_.FullName -notmatch 'bin\\|obj\\|Code_backup' } |
    ForEach-Object {
        try {
            $content = Get-Content $_.FullName -ReadCount 0 -Raw -ErrorAction SilentlyContinue
            ($content -split "`r?`n").Count
        } catch { 0 }
    }
$totalCodeLines = ($codeFiles | Measure-Object -Sum).Sum

# Folder breakdown
$folderStats = $jsonFiles |
    Group-Object -Property Folder | 
    ForEach-Object {
        $folderLines = ($_.Group | Measure-Object -Property Lines -Sum).Sum
        $folderFiles = ($_.Group | Measure-Object).Count
        $folderSize = ($_.Group | Measure-Object -Property SizeKB -Sum).Sum
        [PSCustomObject]@{
            Folder = $_.Name
            Files = $folderFiles
            Lines = $folderLines
            SizeKB = [math]::Round($folderSize, 2)
        }
    } | Sort-Object Lines -Descending

# Top 20 largest JSON files
$topFiles = $jsonFiles | Sort-Object Lines -Descending | Select-Object -First 20

# Size distribution
$sizeRanges = @(
    @{Label="0-100 lines"; Min=0; Max=100},
    @{Label="101-500 lines"; Min=101; Max=500},
    @{Label="501-1000 lines"; Min=501; Max=1000},
    @{Label="1001-2000 lines"; Min=1001; Max=2000},
    @{Label="2000+ lines"; Min=2001; Max=[int]::MaxValue}
)

$distribution = foreach ($range in $sizeRanges) {
    $count = ($jsonFiles | Where-Object { $_.Lines -ge $range.Min -and $_.Lines -le $range.Max }).Count
    $percentage = if ($totalJsonFiles -gt 0) { [math]::Round(($count / $totalJsonFiles) * 100, 1) } else { 0 }
    [PSCustomObject]@{
        Range = $range.Label
        Files = $count
        Percentage = "$percentage%"
    }
}

# Output
Write-Host "=== Overall Statistics ===" -ForegroundColor Yellow
Write-Host ""
Write-Host "Total JSON files: $totalJsonFiles" -ForegroundColor Cyan
Write-Host "Total JSON lines: $totalJsonLines" -ForegroundColor Cyan
Write-Host "Total JSON size: $([math]::Round($totalJsonSize, 2)) KB" -ForegroundColor Cyan
Write-Host ""
Write-Host "Total C# code lines: $totalCodeLines" -ForegroundColor Green
if ($totalCodeLines -gt 0) {
    $ratio = [math]::Round(($totalJsonLines / $totalCodeLines) * 100, 1)
    Write-Host "Config-to-Code ratio: ${ratio}% (JSON lines / Code lines)" -ForegroundColor Yellow
}
Write-Host ""

Write-Host "=== Size Distribution ===" -ForegroundColor Yellow
Write-Host ""
$distribution | Format-Table Range, Files, Percentage -AutoSize | Out-Host
Write-Host ""

Write-Host "=== Lines Per Folder ===" -ForegroundColor Magenta
Write-Host ""
$folderStats | Format-Table Folder, Files, Lines, SizeKB -AutoSize | Out-Host
Write-Host ""

Write-Host "=== Top 20 Largest JSON Files ===" -ForegroundColor Yellow
Write-Host ""
$topFiles | Format-Table @{Label="File"; Expression={$_.File}}, @{Label="Folder"; Expression={$_.Folder}}, Lines, SizeKB -AutoSize | Out-Host
Write-Host ""

