# Count lines by file type across the entire project
# Usage: .\Scripts\multi-language-count.ps1

Write-Host "=== Multi-Language Codebase Analysis ===" -ForegroundColor Cyan
Write-Host ""

# Define file types to analyze
$fileTypes = @{
    'C#' = @('*.cs')
    'AXAML' = @('*.axaml')
    'JSON' = @('*.json')
    'Markdown' = @('*.md')
    'PowerShell' = @('*.ps1')
    'Batch' = @('*.bat')
    'Python' = @('*.py')
    'Shell' = @('*.sh')
    'SVG' = @('*.svg')
    'TXT' = @('*.txt')
}

# Exclude patterns
$excludePatterns = @('bin\\', 'obj\\', 'Code_backup', '\.backup', 'node_modules\\')

# Collect statistics for each file type
$languageStats = @{}

foreach ($lang in $fileTypes.Keys) {
    $extensions = $fileTypes[$lang]
    $files = @()
    
    foreach ($ext in $extensions) {
        $foundFiles = Get-ChildItem -Path . -Filter $ext -Recurse -ErrorAction SilentlyContinue |
            Where-Object {
                $exclude = $false
                foreach ($pattern in $excludePatterns) {
                    if ($_.FullName -match $pattern) {
                        $exclude = $true
                        break
                    }
                }
                -not $exclude
            }
        $files += $foundFiles
    }
    
    $fileCount = ($files | Measure-Object).Count
    $totalLines = 0
    
    foreach ($file in $files) {
        try {
            $content = Get-Content $file.FullName -ReadCount 0 -Raw -ErrorAction Stop
            $lineCount = ($content -split "`r?`n").Count
            $totalLines += $lineCount
        } catch {
            # Skip files that can't be read
        }
    }
    
    $avgLines = if ($fileCount -gt 0) { [math]::Round($totalLines / $fileCount, 1) } else { 0 }
    
    $languageStats[$lang] = [PSCustomObject]@{
        Language = $lang
        Files = $fileCount
        Lines = $totalLines
        AvgLines = $avgLines
    }
}

# Calculate totals
$totalFiles = ($languageStats.Values | Measure-Object -Property Files -Sum).Sum
$totalLines = ($languageStats.Values | Measure-Object -Property Lines -Sum).Sum

# Sort by line count
$sortedStats = $languageStats.Values | Sort-Object Lines -Descending

# Output
Write-Host "=== Overall Project Statistics ===" -ForegroundColor Yellow
Write-Host ""
Write-Host "Total files: $totalFiles" -ForegroundColor Cyan
Write-Host "Total lines: $totalLines" -ForegroundColor Cyan
Write-Host ""

Write-Host "=== Lines by Language ===" -ForegroundColor Magenta
Write-Host ""
$sortedStats | Format-Table Language, Files, Lines, AvgLines -AutoSize | Out-Host
Write-Host ""

Write-Host "=== Language Distribution (Percentage) ===" -ForegroundColor Yellow
Write-Host ""
$sortedStats | ForEach-Object {
    $filePct = if ($totalFiles -gt 0) { [math]::Round(($_.Files / $totalFiles) * 100, 1) } else { 0 }
    $linePct = if ($totalLines -gt 0) { [math]::Round(($_.Lines / $totalLines) * 100, 1) } else { 0 }
    [PSCustomObject]@{
        Language = $_.Language
        Files = $_.Files
        FilePercent = "$filePct%"
        Lines = $_.Lines
        LinePercent = "$linePct%"
    }
} | Format-Table Language, Files, FilePercent, Lines, LinePercent -AutoSize | Out-Host
Write-Host ""

# Code vs Configuration vs Documentation breakdown
$codeLanguages = @('C#', 'AXAML', 'PowerShell', 'Batch', 'Python', 'Shell')
$configLanguages = @('JSON')
$docLanguages = @('Markdown', 'TXT')
$otherLanguages = @('SVG')

$codeStats = $sortedStats | Where-Object { $codeLanguages -contains $_.Language }
$configStats = $sortedStats | Where-Object { $configLanguages -contains $_.Language }
$docStats = $sortedStats | Where-Object { $docLanguages -contains $_.Language }
$otherStats = $sortedStats | Where-Object { $otherLanguages -contains $_.Language }

$codeLines = ($codeStats | Measure-Object -Property Lines -Sum).Sum
$configLines = ($configStats | Measure-Object -Property Lines -Sum).Sum
$docLines = ($docStats | Measure-Object -Property Lines -Sum).Sum
$otherLines = ($otherStats | Measure-Object -Property Lines -Sum).Sum

Write-Host "=== Project Composition ===" -ForegroundColor Green
Write-Host ""
Write-Host "Code: $codeLines lines ($([math]::Round(($codeLines / $totalLines) * 100, 1))%)" -ForegroundColor Green
Write-Host "Configuration: $configLines lines ($([math]::Round(($configLines / $totalLines) * 100, 1))%)" -ForegroundColor Yellow
Write-Host "Documentation: $docLines lines ($([math]::Round(($docLines / $totalLines) * 100, 1))%)" -ForegroundColor Cyan
Write-Host "Other: $otherLines lines ($([math]::Round(($otherLines / $totalLines) * 100, 1))%)" -ForegroundColor Gray
Write-Host ""

