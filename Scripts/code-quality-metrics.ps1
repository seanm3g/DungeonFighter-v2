# Combined code quality metrics - Generate a health score for the codebase
# Usage: .\Scripts\code-quality-metrics.ps1

Write-Host "=== Code Quality Metrics Dashboard ===" -ForegroundColor Cyan
Write-Host ""

# Test files to exclude
$testExclusions = @(
    'TestManager.cs',
    'GameSystemTestRunner.cs',
    'UISystemTestRunner.cs',
    'Program.cs',
    'ColorSystemTest',
    'Tests',
    'Test.cs'
)

# Get all production files
$productionFiles = Get-ChildItem -Path Code -Filter *.cs -Recurse | 
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
        try {
            $content = Get-Content $_.FullName -ReadCount 0 -Raw -ErrorAction Stop
            $lines = ($content -split "`r?`n").Count
            
            # Count comments (single-line and multi-line)
            $singleLineComments = ([regex]::Matches($content, '^\s*//')).Count
            $multiLineComments = ([regex]::Matches($content, '/\*')).Count
            $commentLines = $singleLineComments + $multiLineComments
            
            # Count code lines (non-empty, non-comment)
            $codeLines = ($content -split "`r?`n" | Where-Object { 
                $line = $_.Trim()
                $line -ne '' -and -not $line.StartsWith('//') -and -not $line.StartsWith('/*') -and -not $line.StartsWith('*')
            }).Count
            
            [PSCustomObject]@{
                File = $_.FullName.Replace((Get-Location).Path + '\', '')
                Lines = $lines
                CommentLines = $commentLines
                CodeLines = $codeLines
            }
        } catch {
            $null
        }
    } | Where-Object { $null -ne $_ }

# Get test files
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
        try {
            $content = Get-Content $_.FullName -ReadCount 0 -Raw -ErrorAction Stop
            ($content -split "`r?`n").Count
        } catch { 0 }
    }
$totalTestLines = ($testFiles | Measure-Object -Sum).Sum

# Calculate metrics
$totalFiles = ($productionFiles | Measure-Object).Count
$totalLines = ($productionFiles | Measure-Object -Property Lines -Sum).Sum
$totalCodeLines = ($productionFiles | Measure-Object -Property CodeLines -Sum).Sum
$totalCommentLines = ($productionFiles | Measure-Object -Property CommentLines -Sum).Sum
$avgLinesPerFile = if ($totalFiles -gt 0) { [math]::Round($totalLines / $totalFiles, 1) } else { 0 }
$commentRatio = if ($totalCodeLines -gt 0) { [math]::Round(($totalCommentLines / $totalCodeLines) * 100, 1) } else { 0 }

# Files above threshold
$threshold = 400
$filesAboveThreshold = $productionFiles | Where-Object { $_.Lines -gt $threshold }
$filesAboveThresholdCount = ($filesAboveThreshold | Measure-Object).Count
$filesAboveThresholdLines = ($filesAboveThreshold | Measure-Object -Property Lines -Sum).Sum
$largeFilePercentage = if ($totalLines -gt 0) { [math]::Round(($filesAboveThresholdLines / $totalLines) * 100, 1) } else { 0 }

# Test coverage
$testFileCount = ($testFiles | Measure-Object).Count
$testCoverageFile = if ($totalFiles -gt 0) { [math]::Round(($testFileCount / $totalFiles) * 100, 1) } else { 0 }
$testCoverageLine = if ($totalLines -gt 0) { [math]::Round(($totalTestLines / $totalLines) * 100, 1) } else { 0 }

# Calculate health scores (0-100)
$fileSizeScore = if ($largeFilePercentage -lt 10) { 100 }
                elseif ($largeFilePercentage -lt 20) { 80 }
                elseif ($largeFilePercentage -lt 30) { 60 }
                elseif ($largeFilePercentage -lt 40) { 40 }
                else { 20 }

$commentScore = if ($commentRatio -ge 20) { 100 }
                elseif ($commentRatio -ge 15) { 80 }
                elseif ($commentRatio -ge 10) { 60 }
                elseif ($commentRatio -ge 5) { 40 }
                else { 20 }

$testScore = if ($testCoverageLine -ge 50) { 100 }
            elseif ($testCoverageLine -ge 30) { 70 }
            elseif ($testCoverageLine -ge 15) { 50 }
            elseif ($testCoverageLine -ge 5) { 30 }
            else { 10 }

$avgSizeScore = if ($avgLinesPerFile -lt 200) { 100 }
                elseif ($avgLinesPerFile -lt 300) { 80 }
                elseif ($avgLinesPerFile -lt 400) { 60 }
                elseif ($avgLinesPerFile -lt 500) { 40 }
                else { 20 }

$overallHealth = [math]::Round(($fileSizeScore + $commentScore + $testScore + $avgSizeScore) / 4, 1)

# Health grade
$grade = if ($overallHealth -ge 90) { "A" }
        elseif ($overallHealth -ge 80) { "B" }
        elseif ($overallHealth -ge 70) { "C" }
        elseif ($overallHealth -ge 60) { "D" }
        else { "F" }

# Output
Write-Host "=== Codebase Statistics ===" -ForegroundColor Yellow
Write-Host ""
Write-Host "Production files: $totalFiles" -ForegroundColor Cyan
Write-Host "Total lines: $totalLines" -ForegroundColor Cyan
Write-Host "Code lines: $totalCodeLines" -ForegroundColor Cyan
Write-Host "Comment lines: $totalCommentLines" -ForegroundColor Cyan
Write-Host "Average lines per file: $avgLinesPerFile" -ForegroundColor Cyan
Write-Host "Comment ratio: ${commentRatio}%" -ForegroundColor Cyan
Write-Host ""

Write-Host "=== File Size Health ===" -ForegroundColor Yellow
Write-Host ""
Write-Host "Files above $threshold lines: $filesAboveThresholdCount" -ForegroundColor $(if ($filesAboveThresholdCount -gt 0) { "Red" } else { "Green" })
Write-Host "Lines in large files: $filesAboveThresholdLines ($largeFilePercentage% of total)" -ForegroundColor $(if ($largeFilePercentage -gt 20) { "Red" } else { "Yellow" })
Write-Host "Score: $fileSizeScore/100" -ForegroundColor $(if ($fileSizeScore -ge 80) { "Green" } elseif ($fileSizeScore -ge 60) { "Yellow" } else { "Red" })
Write-Host ""

Write-Host "=== Documentation Health ===" -ForegroundColor Yellow
Write-Host ""
Write-Host "Comment ratio: ${commentRatio}%" -ForegroundColor Cyan
Write-Host "Score: $commentScore/100" -ForegroundColor $(if ($commentScore -ge 80) { "Green" } elseif ($commentScore -ge 60) { "Yellow" } else { "Red" })
Write-Host ""

Write-Host "=== Test Coverage Health ===" -ForegroundColor Yellow
Write-Host ""
Write-Host "Test files: $testFileCount" -ForegroundColor Cyan
Write-Host "Test lines: $totalTestLines" -ForegroundColor Cyan
Write-Host "File coverage: ${testCoverageFile}%" -ForegroundColor Cyan
Write-Host "Line coverage: ${testCoverageLine}%" -ForegroundColor Cyan
Write-Host "Score: $testScore/100" -ForegroundColor $(if ($testScore -ge 80) { "Green" } elseif ($testScore -ge 60) { "Yellow" } else { "Red" })
Write-Host ""

Write-Host "=== Average File Size Health ===" -ForegroundColor Yellow
Write-Host ""
Write-Host "Average lines per file: $avgLinesPerFile" -ForegroundColor Cyan
Write-Host "Score: $avgSizeScore/100" -ForegroundColor $(if ($avgSizeScore -ge 80) { "Green" } elseif ($avgSizeScore -ge 60) { "Yellow" } else { "Red" })
Write-Host ""

Write-Host "======================================================================" -ForegroundColor Magenta
Write-Host "=== OVERALL CODE HEALTH SCORE ===" -ForegroundColor Magenta
Write-Host "======================================================================" -ForegroundColor Magenta
Write-Host ""
$color = if ($overallHealth -ge 80) { "Green" } elseif ($overallHealth -ge 60) { "Yellow" } else { "Red" }
Write-Host "Health Score: $overallHealth/100 (Grade: $grade)" -ForegroundColor $color
Write-Host ""
Write-Host "Breakdown:" -ForegroundColor Cyan
Write-Host "  File Size:     $fileSizeScore/100" -ForegroundColor $(if ($fileSizeScore -ge 80) { "Green" } elseif ($fileSizeScore -ge 60) { "Yellow" } else { "Red" })
Write-Host "  Documentation: $commentScore/100" -ForegroundColor $(if ($commentScore -ge 80) { "Green" } elseif ($commentScore -ge 60) { "Yellow" } else { "Red" })
Write-Host "  Test Coverage: $testScore/100" -ForegroundColor $(if ($testScore -ge 80) { "Green" } elseif ($testScore -ge 60) { "Yellow" } else { "Red" })
Write-Host "  Avg File Size: $avgSizeScore/100" -ForegroundColor $(if ($avgSizeScore -ge 80) { "Green" } elseif ($avgSizeScore -ge 60) { "Yellow" } else { "Red" })
Write-Host ""

