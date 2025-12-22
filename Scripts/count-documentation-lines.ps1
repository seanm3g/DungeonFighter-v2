# Count lines in Documentation folder and analyze documentation vs code ratio
# Usage: .\Scripts\count-documentation-lines.ps1

Write-Host "=== Documentation Analysis ===" -ForegroundColor Cyan
Write-Host ""

# Get all markdown files in Documentation folder
$docFiles = Get-ChildItem -Path Documentation -Filter *.md -Recurse -ErrorAction SilentlyContinue | 
    ForEach-Object {
        $relPath = $_.FullName.Replace((Get-Location).Path + '\', '')
        
        # Extract category (folder structure: Documentation\01-Core, 02-Development, etc.)
        $categoryMatch = $relPath -match 'Documentation\\([^\\]+)'
        $category = if ($categoryMatch) { $matches[1] } else { "Root" }
        
        # Extract subfolder if exists
        $subfolderMatch = $relPath -match 'Documentation\\[^\\]+\\([^\\]+)'
        $subfolder = if ($subfolderMatch) { $matches[1] } else { "(root)" }
        
        try {
            $content = Get-Content $_.FullName -ReadCount 0 -Raw -ErrorAction Stop
            $lineCount = ($content -split "`r?`n").Count
            $fileSize = $_.Length
            
            [PSCustomObject]@{
                File = $relPath
                Category = $category
                Subfolder = $subfolder
                Lines = $lineCount
                SizeKB = [math]::Round($fileSize / 1KB, 2)
            }
        } catch {
            Write-Warning "Skipping file due to read error: $relPath"
            $null
        }
    } | Where-Object { $null -ne $_ }

# Get code files for comparison
$testExclusions = @(
    'TestManager.cs',
    'GameSystemTestRunner.cs',
    'UISystemTestRunner.cs',
    'Program.cs',
    'ColorSystemTest',
    'Tests',
    'Test.cs'
)

$codeFiles = Get-ChildItem -Path Code -Filter *.cs -Recurse | 
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
            $content = Get-Content $_.FullName -ReadCount 0 -Raw -ErrorAction SilentlyContinue
            ($content -split "`r?`n").Count
        } catch { 0 }
    }
$totalCodeLines = ($codeFiles | Measure-Object -Sum).Sum

# Calculate totals
$totalDocFiles = ($docFiles | Measure-Object).Count
$totalDocLines = ($docFiles | Measure-Object -Property Lines -Sum).Sum
$totalDocSize = ($docFiles | Measure-Object -Property SizeKB -Sum).Sum

# Category breakdown
$categoryStats = $docFiles |
    Group-Object -Property Category | 
    ForEach-Object {
        $categoryLines = ($_.Group | Measure-Object -Property Lines -Sum).Sum
        $categoryFiles = ($_.Group | Measure-Object).Count
        $categorySize = ($_.Group | Measure-Object -Property SizeKB -Sum).Sum
        $avgLines = if ($categoryFiles -gt 0) { [math]::Round($categoryLines / $categoryFiles, 1) } else { 0 }
        
        [PSCustomObject]@{
            Category = $_.Name
            Files = $categoryFiles
            Lines = $categoryLines
            SizeKB = [math]::Round($categorySize, 2)
            AvgLines = $avgLines
        }
    } | Sort-Object Lines -Descending

# Largest documentation files
$topDocFiles = $docFiles | Sort-Object Lines -Descending | Select-Object -First 20

# Size distribution
$sizeRanges = @(
    @{Label="0-100 lines"; Min=0; Max=100},
    @{Label="101-300 lines"; Min=101; Max=300},
    @{Label="301-500 lines"; Min=301; Max=500},
    @{Label="501-1000 lines"; Min=501; Max=1000},
    @{Label="1000+ lines"; Min=1001; Max=[int]::MaxValue}
)

$distribution = foreach ($range in $sizeRanges) {
    $count = ($docFiles | Where-Object { $_.Lines -ge $range.Min -and $_.Lines -le $range.Max }).Count
    $percentage = if ($totalDocFiles -gt 0) { [math]::Round(($count / $totalDocFiles) * 100, 1) } else { 0 }
    [PSCustomObject]@{
        Range = $range.Label
        Files = $count
        Percentage = "$percentage%"
    }
}

# Output
Write-Host "=== Overall Statistics ===" -ForegroundColor Yellow
Write-Host ""
Write-Host "Total documentation files: $totalDocFiles" -ForegroundColor Cyan
Write-Host "Total documentation lines: $totalDocLines" -ForegroundColor Cyan
Write-Host "Total documentation size: $([math]::Round($totalDocSize, 2)) KB" -ForegroundColor Cyan
Write-Host ""
Write-Host "Total code lines: $totalCodeLines" -ForegroundColor Green
if ($totalCodeLines -gt 0) {
    $docToCodeRatio = [math]::Round(($totalDocLines / $totalCodeLines) * 100, 1)
    Write-Host "Documentation-to-Code ratio: ${docToCodeRatio}% (doc lines / code lines)" -ForegroundColor Yellow
    
    if ($docToCodeRatio -ge 30) {
        Write-Host "  ✅ Excellent documentation coverage!" -ForegroundColor Green
    } elseif ($docToCodeRatio -ge 20) {
        Write-Host "  ✓ Good documentation coverage" -ForegroundColor Yellow
    } else {
        Write-Host "  ⚠ Consider adding more documentation" -ForegroundColor Red
    }
}
Write-Host ""

Write-Host "=== Documentation by Category ===" -ForegroundColor Magenta
Write-Host ""
$categoryStats | Format-Table Category, Files, Lines, SizeKB, AvgLines -AutoSize | Out-Host
Write-Host ""

Write-Host "=== Category Distribution (Percentage) ===" -ForegroundColor Yellow
Write-Host ""
$categoryStats | ForEach-Object {
    $pct = if ($totalDocLines -gt 0) { [math]::Round(($_.Lines / $totalDocLines) * 100, 1) } else { 0 }
    [PSCustomObject]@{
        Category = $_.Category
        Lines = $_.Lines
        Percentage = "$pct%"
    }
} | Format-Table Category, Lines, Percentage -AutoSize | Out-Host
Write-Host ""

Write-Host "=== Size Distribution ===" -ForegroundColor Yellow
Write-Host ""
$distribution | Format-Table Range, Files, Percentage -AutoSize | Out-Host
Write-Host ""

Write-Host "=== Top 20 Largest Documentation Files ===" -ForegroundColor Yellow
Write-Host ""
$topDocFiles | Format-Table @{Label="File"; Expression={$_.File}}, @{Label="Category"; Expression={$_.Category}}, Lines, SizeKB -AutoSize | Out-Host
Write-Host ""

