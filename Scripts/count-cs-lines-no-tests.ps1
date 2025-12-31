# Count lines in .cs files excluding test classes and show files above 400 lines, sorted by line count
# Usage: .\Scripts\count-cs-lines-no-tests.ps1

$threshold = 400

# Test files to exclude
$testExclusions = @(
    'TestManager.cs',
    'GameSystemTestRunner.cs',
    'UISystemTestRunner.cs',
    'Program.cs',  # StandaloneColorDemo
    'ColorSystemTest',
    'Tests',
    'Test.cs'
)

Write-Host "Scanning .cs files (excluding test classes)..." -ForegroundColor Cyan
Write-Host ""

# Collect all file data with folder info for metrics
$allFilesWithDetails = Get-ChildItem -Path Code -Filter *.cs -Recurse | 
    Where-Object { $_.FullName -notmatch 'bin\\|obj\\|Code_backup' } |
    Where-Object { 
        $isTestFile = $false
        foreach ($testExclude in $testExclusions) {
            if ($_.FullName -match $testExclude) {
                $isTestFile = $true
                break
            }
        }
        -not $isTestFile
    } |
    ForEach-Object { 
        $relPath = $_.FullName.Replace((Get-Location).Path + '\', '')
        $folderMatch = $relPath -match '^Code\\([^\\]+)'
        $folder = if ($folderMatch) { $matches[1] } else { "Other" }
        
        try {
            $content = Get-Content $_.FullName -ReadCount 0 -Raw -ErrorAction Stop
            $lineCount = ($content -split "`r?`n").Count
            [PSCustomObject]@{
                File = $relPath
                Folder = $folder
                Lines = $lineCount
            }
        } catch {
            Write-Warning "Skipping file due to read error: $relPath"
            $null
        }
    } | Where-Object { $null -ne $_ }

# Collect test files data for ratio calculation
$testFiles = Get-ChildItem -Path Code -Filter *.cs -Recurse | 
    Where-Object { $_.FullName -notmatch 'bin\\|obj\\|Code_backup' } |
    Where-Object { 
        $isTestFile = $false
        foreach ($testExclude in $testExclusions) {
            if ($_.FullName -match $testExclude) {
                $isTestFile = $true
                break
            }
        }
        $isTestFile
    } |
    ForEach-Object { 
        $relPath = $_.FullName.Replace((Get-Location).Path + '\', '')
        try {
            $content = Get-Content $_.FullName -ReadCount 0 -Raw -ErrorAction Stop
            $lineCount = ($content -split "`r?`n").Count
            [PSCustomObject]@{
                File = $relPath
                Lines = $lineCount
            }
        } catch {
            Write-Warning "Skipping file due to read error: $relPath"
            $null
        }
    } | Where-Object { $null -ne $_ }

# Calculate summary statistics
$totalAllLines = ($allFilesWithDetails | Measure-Object -Property Lines -Sum).Sum
$totalFileCount = ($allFilesWithDetails | Measure-Object).Count
$avgLinesPerFile = if ($totalFileCount -gt 0) { [math]::Round($totalAllLines / $totalFileCount, 1) } else { 0 }

# Get files above threshold (most important - will be shown at bottom)
$productionFiles = $allFilesWithDetails | 
    Where-Object { $_.Lines -gt $threshold } |
    Sort-Object Lines -Descending

$filesAboveThresholdCount = ($productionFiles | Measure-Object).Count
$filesAboveThresholdLines = ($productionFiles | Measure-Object -Property Lines -Sum).Sum

# Calculate folder stats
$folderStats = $allFilesWithDetails |
    Group-Object -Property Folder | 
    ForEach-Object {
        $folderLines = ($_.Group | Measure-Object -Property Lines -Sum).Sum
        $folderFiles = ($_.Group | Measure-Object).Count
        $avgLines = if ($folderFiles -gt 0) { [math]::Round($folderLines / $folderFiles, 1) } else { 0 }
        [PSCustomObject]@{
            Folder = $_.Name
            Files = $folderFiles
            Lines = $folderLines
            AvgLines = $avgLines
        }
    } | Sort-Object Lines -Descending

# Calculate test vs production ratio
$testFileCount = ($testFiles | Measure-Object).Count
$testLines = ($testFiles | Measure-Object -Property Lines -Sum).Sum
$totalCodeFiles = $totalFileCount + $testFileCount
$totalCodeLines = $totalAllLines + $testLines

# Calculate file size distribution
$sizeRanges = @(
    @{Label="0-100 lines"; Min=0; Max=100},
    @{Label="101-200 lines"; Min=101; Max=200},
    @{Label="201-300 lines"; Min=201; Max=300},
    @{Label="301-400 lines"; Min=301; Max=400},
    @{Label="401-500 lines"; Min=401; Max=500},
    @{Label="501-750 lines"; Min=501; Max=750},
    @{Label="751-1000 lines"; Min=751; Max=1000},
    @{Label="1000+ lines"; Min=1001; Max=[int]::MaxValue}
)

$distribution = foreach ($range in $sizeRanges) {
    if ($null -eq $allFilesWithDetails -or $allFilesWithDetails.Count -eq 0) {
        $count = 0
    } else {
        $min = $range.Min
        $max = $range.Max
        $count = ($allFilesWithDetails | Where-Object { $_.Lines -ge $min -and $_.Lines -le $max }).Count
    }
    $percentage = if ($totalFileCount -gt 0) { [math]::Round(($count / $totalFileCount) * 100, 1) } else { 0 }
    [PSCustomObject]@{
        Range = $range.Label
        Files = $count
        Percentage = "$percentage%"
    }
}

# Get top 20 largest files
$topFiles = $allFilesWithDetails | Sort-Object Lines -Descending | Select-Object -First 20

# ============================================================================
# OUTPUT SECTION - Ordered: Overview -> 400+ Lines -> Total
# ============================================================================

# 1. OVERVIEW: Lines per major folder (code per section)
Write-Host "=== Code Overview by Section ===" -ForegroundColor Magenta
Write-Host ""
$folderStats | Format-Table Folder, Files, Lines, @{Label="Avg/File"; Expression={$_.AvgLines}} -AutoSize | Out-Host
$folderTotalLines = ($folderStats | Measure-Object -Property Lines -Sum).Sum
Write-Host ""

# Subfolder breakdown for folders over 3000 lines
$largeFolders = $folderStats | Where-Object { $_.Lines -gt 3000 }
if ($largeFolders.Count -gt 0) {
    Write-Host "=== Subfolder Breakdown (Folders with > 3000 lines) ===" -ForegroundColor Yellow
    Write-Host ""
    
    foreach ($largeFolder in $largeFolders) {
        $folderName = $largeFolder.Folder
        Write-Host "  $folderName ($($largeFolder.Lines) total lines):" -ForegroundColor Cyan
        
        $subfolderStats = Get-ChildItem -Path "Code\$folderName" -Filter *.cs -Recurse -ErrorAction SilentlyContinue | 
            Where-Object { $_.FullName -notmatch 'bin\\|obj\\|Code_backup' } |
            Where-Object { 
                $isTestFile = $false
                foreach ($testExclude in $testExclusions) {
                    if ($_.FullName -match $testExclude) {
                        $isTestFile = $true
                        break
                    }
                }
                -not $isTestFile
            } |
            ForEach-Object { 
                $relPath = $_.FullName.Replace((Get-Location).Path + '\', '')
                $folderPrefix = "Code\$folderName\"
                if ($relPath.StartsWith($folderPrefix)) {
                    $subfolderPath = $relPath.Substring($folderPrefix.Length)
                    if ($subfolderPath.Contains('\')) {
                        $subfolder = $subfolderPath.Split('\')[0]
                    } else {
                        $subfolder = "(root)"
                    }
                } else {
                    $subfolder = "(root)"
                }
                
                try {
                    $content = Get-Content $_.FullName -ReadCount 0 -Raw -ErrorAction Stop
                    $lineCount = ($content -split "`r?`n").Count
                    [PSCustomObject]@{
                        Subfolder = $subfolder
                        Lines = $lineCount
                        File = $relPath
                    }
                } catch {
                    Write-Warning "Skipping file due to read error: $relPath"
                    $null
                }
            } | Where-Object { $null -ne $_ } |
            Group-Object -Property Subfolder | 
            ForEach-Object {
                $subfolderLines = ($_.Group | Measure-Object -Property Lines -Sum).Sum
                $subfolderFiles = ($_.Group | Measure-Object).Count
                [PSCustomObject]@{
                    Subfolder = $_.Name
                    Files = $subfolderFiles
                    Lines = $subfolderLines
                }
            } | Sort-Object Lines -Descending
        
        if ($subfolderStats.Count -gt 0) {
            $subfolderStats | Format-Table @{Label="Subfolder"; Expression={$_.Subfolder}}, Files, Lines -AutoSize | Out-Host
        } else {
            Write-Host "    (no subfolders or all files in root)" -ForegroundColor Gray
        }
        Write-Host ""
    }
}

# 2. FILES ABOVE 400+ LINES
Write-Host "======================================================================" -ForegroundColor Red
Write-Host "=== Files Above ${threshold} Lines ===" -ForegroundColor Red
Write-Host "======================================================================" -ForegroundColor Red
Write-Host ""
Write-Host "Production files above ${threshold} lines: $filesAboveThresholdCount" -ForegroundColor Red
Write-Host "Total lines in files above ${threshold}: $filesAboveThresholdLines" -ForegroundColor Red
if ($totalAllLines -gt 0) {
    $percentage = [math]::Round(($filesAboveThresholdLines / $totalAllLines) * 100, 1)
    Write-Host "Percentage of total codebase: ${percentage}%" -ForegroundColor Red
}
Write-Host ""
Write-Host "These files may need refactoring consideration:" -ForegroundColor Yellow
Write-Host ""

if ($filesAboveThresholdCount -gt 0) {
    $productionFiles | Format-Table @{Label="File"; Expression={$_.File}}, @{Label="Folder"; Expression={$_.Folder}}, Lines -AutoSize | Out-Host
} else {
    Write-Host "  ✅ No files above ${threshold} lines - Excellent code organization!" -ForegroundColor Green
}
Write-Host ""

# 3. TOTAL LINES (at the very bottom)
Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host "=== TOTAL LINES ===" -ForegroundColor Cyan
Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "TOTAL production files: $totalFileCount" -ForegroundColor Cyan
Write-Host "TOTAL lines in production code: $totalAllLines" -ForegroundColor Cyan
Write-Host "Average lines per file: $avgLinesPerFile" -ForegroundColor Cyan
Write-Host ""

