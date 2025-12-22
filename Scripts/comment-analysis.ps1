# Comment and documentation ratio analysis
# Usage: .\Scripts\comment-analysis.ps1

Write-Host "=== Comment & Documentation Analysis ===" -ForegroundColor Cyan
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
$fileStats = Get-ChildItem -Path Code -Filter *.cs -Recurse | 
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
            $allLines = ($content -split "`r?`n")
            $totalLines = $allLines.Count
            
            # Count comment lines
            $commentLines = 0
            $inMultiLineComment = $false
            
            foreach ($line in $allLines) {
                $trimmed = $line.Trim()
                
                # Check for multi-line comment start/end
                if ($trimmed -match '/\*') {
                    $inMultiLineComment = $true
                    $commentLines++
                } elseif ($trimmed -match '\*/') {
                    $inMultiLineComment = $true
                    $commentLines++
                    $inMultiLineComment = $false
                } elseif ($inMultiLineComment) {
                    $commentLines++
                } elseif ($trimmed.StartsWith('//')) {
                    $commentLines++
                } elseif ($trimmed -match '^\s*//') {
                    $commentLines++
                }
            }
            
            # Count code lines (non-empty, non-comment)
            $codeLines = ($allLines | Where-Object { 
                $line = $_.Trim()
                $line -ne '' -and 
                -not $line.StartsWith('//') -and 
                -not $line.StartsWith('/*') -and 
                -not $line.StartsWith('*') -and
                -not $line.StartsWith('*/')
            }).Count
            
            $commentRatio = if ($codeLines -gt 0) { [math]::Round(($commentLines / $codeLines) * 100, 1) } else { 0 }
            
            [PSCustomObject]@{
                File = $relPath
                Folder = $folder
                TotalLines = $totalLines
                CodeLines = $codeLines
                CommentLines = $commentLines
                CommentRatio = $commentRatio
            }
        } catch {
            Write-Warning "Skipping file due to read error: $relPath"
            $null
        }
    } | Where-Object { $null -ne $_ }

# Calculate totals
$totalFiles = ($fileStats | Measure-Object).Count
$totalLines = ($fileStats | Measure-Object -Property TotalLines -Sum).Sum
$totalCodeLines = ($fileStats | Measure-Object -Property CodeLines -Sum).Sum
$totalCommentLines = ($fileStats | Measure-Object -Property CommentLines -Sum).Sum
$overallCommentRatio = if ($totalCodeLines -gt 0) { [math]::Round(($totalCommentLines / $totalCodeLines) * 100, 1) } else { 0 }

# Files with low comment ratio (< 5%)
$lowCommentFiles = $fileStats | Where-Object { $_.CommentRatio -lt 5 -and $_.CodeLines -gt 50 } | Sort-Object CommentRatio

# Files with good comment ratio (>= 15%)
$goodCommentFiles = $fileStats | Where-Object { $_.CommentRatio -ge 15 } | Sort-Object CommentRatio -Descending

# Folder-level statistics
$folderStats = $fileStats |
    Group-Object -Property Folder | 
    ForEach-Object {
        $folderCodeLines = ($_.Group | Measure-Object -Property CodeLines -Sum).Sum
        $folderCommentLines = ($_.Group | Measure-Object -Property CommentLines -Sum).Sum
        $folderRatio = if ($folderCodeLines -gt 0) { [math]::Round(($folderCommentLines / $folderCodeLines) * 100, 1) } else { 0 }
        
        [PSCustomObject]@{
            Folder = $_.Name
            Files = ($_.Group | Measure-Object).Count
            CodeLines = $folderCodeLines
            CommentLines = $folderCommentLines
            CommentRatio = $folderRatio
        }
    } | Sort-Object CommentRatio -Descending

# Output
Write-Host "=== Overall Statistics ===" -ForegroundColor Yellow
Write-Host ""
Write-Host "Total files analyzed: $totalFiles" -ForegroundColor Cyan
Write-Host "Total lines: $totalLines" -ForegroundColor Cyan
Write-Host "Code lines: $totalCodeLines" -ForegroundColor Cyan
Write-Host "Comment lines: $totalCommentLines" -ForegroundColor Cyan
Write-Host "Overall comment ratio: ${overallCommentRatio}%" -ForegroundColor $(if ($overallCommentRatio -ge 15) { "Green" } elseif ($overallCommentRatio -ge 10) { "Yellow" } else { "Red" })
Write-Host ""

Write-Host "=== Comment Ratio by Folder ===" -ForegroundColor Magenta
Write-Host ""
$folderStats | Format-Table Folder, Files, CodeLines, CommentLines, @{Label="Comment%"; Expression={"$($_.CommentRatio)%"}} -AutoSize | Out-Host
Write-Host ""

Write-Host "=== Files with Low Comment Ratio (<5%, >50 lines) ===" -ForegroundColor Red
Write-Host ""
if ($lowCommentFiles.Count -gt 0) {
    $lowCommentFiles | Select-Object -First 20 | Format-Table File, Folder, CodeLines, CommentLines, @{Label="Comment%"; Expression={"$($_.CommentRatio)%"}} -AutoSize | Out-Host
    Write-Host "Total files with low comments: $($lowCommentFiles.Count)" -ForegroundColor Yellow
} else {
    Write-Host "  ✅ All files have adequate comments!" -ForegroundColor Green
}
Write-Host ""

Write-Host "=== Well-Documented Files (>=15% comment ratio) ===" -ForegroundColor Green
Write-Host ""
if ($goodCommentFiles.Count -gt 0) {
    $goodCommentFiles | Select-Object -First 15 | Format-Table File, Folder, CodeLines, CommentLines, @{Label="Comment%"; Expression={"$($_.CommentRatio)%"}} -AutoSize | Out-Host
    Write-Host "Total well-documented files: $($goodCommentFiles.Count)" -ForegroundColor Green
} else {
    Write-Host "  No files meet the well-documented threshold (>=15%)" -ForegroundColor Yellow
}
Write-Host ""

# Distribution
$ratioRanges = @(
    @{Label="0-5%"; Min=0; Max=5},
    @{Label="5-10%"; Min=5; Max=10},
    @{Label="10-15%"; Min=10; Max=15},
    @{Label="15-20%"; Min=15; Max=20},
    @{Label="20%+"; Min=20; Max=[int]::MaxValue}
)

$distribution = foreach ($range in $ratioRanges) {
    $count = ($fileStats | Where-Object { $_.CommentRatio -ge $range.Min -and $_.CommentRatio -lt $range.Max }).Count
    $percentage = if ($totalFiles -gt 0) { [math]::Round(($count / $totalFiles) * 100, 1) } else { 0 }
    [PSCustomObject]@{
        Range = $range.Label
        Files = $count
        Percentage = "$percentage%"
    }
}

Write-Host "=== Comment Ratio Distribution ===" -ForegroundColor Yellow
Write-Host ""
$distribution | Format-Table Range, Files, Percentage -AutoSize | Out-Host
Write-Host ""

