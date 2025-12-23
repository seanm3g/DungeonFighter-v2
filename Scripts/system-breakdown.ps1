# Detailed breakdown by major system (Combat, Actions, Entity, etc.)
# Usage: .\Scripts\system-breakdown.ps1

Write-Host "=== System/Module Breakdown ===" -ForegroundColor Cyan
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

# Get all production files grouped by top-level folder
$systemFiles = Get-ChildItem -Path Code -Filter *.cs -Recurse | 
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
        $system = if ($folderMatch) { $matches[1] } else { "Other" }
        
        # Get subfolder if exists
        $subfolderMatch = $relPath -match '^Code\\[^\\]+\\([^\\]+)'
        $subfolder = if ($subfolderMatch) { $matches[1] } else { "(root)" }
        
        try {
            $content = Get-Content $_.FullName -ReadCount 0 -Raw -ErrorAction Stop
            $lineCount = ($content -split "`r?`n").Count
            
            # Count classes and methods (rough estimate)
            $classCount = ([regex]::Matches($content, '\bclass\s+\w+')).Count
            $methodCount = ([regex]::Matches($content, '\b(public|private|protected|internal)\s+\w+\s+\w+\s*\([^)]*\)\s*\{')).Count
            
            [PSCustomObject]@{
                System = $system
                Subfolder = $subfolder
                File = $relPath
                Lines = $lineCount
                Classes = $classCount
                Methods = $methodCount
            }
        } catch {
            Write-Warning "Skipping file due to read error: $relPath"
            $null
        }
    } | Where-Object { $null -ne $_ }

# System-level statistics
$systemStats = $systemFiles |
    Group-Object -Property System | 
    ForEach-Object {
        if ($_.Group -and $_.Group.Count -gt 0) {
            $systemLines = ($_.Group | Measure-Object -Property Lines -Sum -ErrorAction SilentlyContinue).Sum
            $systemFiles = ($_.Group | Measure-Object -ErrorAction SilentlyContinue).Count
            $systemClasses = ($_.Group | Measure-Object -Property Classes -Sum -ErrorAction SilentlyContinue).Sum
            $systemMethods = ($_.Group | Measure-Object -Property Methods -Sum -ErrorAction SilentlyContinue).Sum
            
            # Handle null values
            $systemLines = if ($null -eq $systemLines) { 0 } else { $systemLines }
            $systemFiles = if ($null -eq $systemFiles) { 0 } else { $systemFiles }
            $systemClasses = if ($null -eq $systemClasses) { 0 } else { $systemClasses }
            $systemMethods = if ($null -eq $systemMethods) { 0 } else { $systemMethods }
            
            $avgLines = if ($systemFiles -gt 0) { [math]::Round($systemLines / $systemFiles, 1) } else { 0 }
            
            [PSCustomObject]@{
                System = $_.Name
                Files = $systemFiles
                Lines = $systemLines
                Classes = $systemClasses
                Methods = $systemMethods
                AvgLinesPerFile = $avgLines
            }
        }
    } | Where-Object { $null -ne $_ } | Sort-Object Lines -Descending

# Check if we have any files
if ($null -eq $systemFiles -or $systemFiles.Count -eq 0) {
    Write-Warning "No files found to analyze. Check your filters and paths."
    exit
}

# Subfolder breakdown for each system
$subfolderStats = $systemFiles |
    Group-Object -Property System, Subfolder | 
    ForEach-Object {
        if ($_.Group -and $_.Group.Count -gt 0) {
            $subfolderLines = ($_.Group | Measure-Object -Property Lines -Sum -ErrorAction SilentlyContinue).Sum
            $subfolderFiles = ($_.Group | Measure-Object -ErrorAction SilentlyContinue).Count
            $parts = $_.Name -split ', '
            [PSCustomObject]@{
                System = $parts[0]
                Subfolder = $parts[1]
                Files = $subfolderFiles
                Lines = if ($null -eq $subfolderLines) { 0 } else { $subfolderLines }
            }
        }
    } | Where-Object { $null -ne $_ } | Sort-Object System, Lines -Descending

# Total statistics
$totalLines = ($systemFiles | Measure-Object -Property Lines -Sum -ErrorAction SilentlyContinue).Sum
$totalFiles = ($systemFiles | Measure-Object -ErrorAction SilentlyContinue).Count
$totalClasses = ($systemFiles | Measure-Object -Property Classes -Sum -ErrorAction SilentlyContinue).Sum
$totalMethods = ($systemFiles | Measure-Object -Property Methods -Sum -ErrorAction SilentlyContinue).Sum

# Handle null values
$totalLines = if ($null -eq $totalLines) { 0 } else { $totalLines }
$totalFiles = if ($null -eq $totalFiles) { 0 } else { $totalFiles }
$totalClasses = if ($null -eq $totalClasses) { 0 } else { $totalClasses }
$totalMethods = if ($null -eq $totalMethods) { 0 } else { $totalMethods }

# Output
Write-Host "=== Overall Statistics ===" -ForegroundColor Yellow
Write-Host ""
Write-Host "Total systems: $($systemStats.Count)" -ForegroundColor Cyan
Write-Host "Total files: $totalFiles" -ForegroundColor Cyan
Write-Host "Total lines: $totalLines" -ForegroundColor Cyan
Write-Host "Total classes: $totalClasses" -ForegroundColor Cyan
Write-Host "Total methods: $totalMethods" -ForegroundColor Cyan
Write-Host ""

Write-Host "=== System Breakdown (by Lines) ===" -ForegroundColor Magenta
Write-Host ""
$systemStats | Format-Table System, Files, Lines, Classes, Methods, AvgLinesPerFile -AutoSize | Out-Host
Write-Host ""

# Show percentage of total
Write-Host "=== System Distribution (Percentage) ===" -ForegroundColor Yellow
Write-Host ""
$systemStats | ForEach-Object {
    $pct = if ($totalLines -gt 0) { [math]::Round(($_.Lines / $totalLines) * 100, 1) } else { 0 }
    [PSCustomObject]@{
        System = $_.System
        Lines = $_.Lines
        Percentage = "$pct%"
    }
} | Format-Table System, Lines, Percentage -AutoSize | Out-Host
Write-Host ""

# Show subfolder breakdown for systems with > 2000 lines
$largeSystems = $systemStats | Where-Object { $_.Lines -gt 2000 }
if ($largeSystems.Count -gt 0) {
    Write-Host "=== Subfolder Breakdown (Systems with > 2000 lines) ===" -ForegroundColor Yellow
    Write-Host ""
    
    foreach ($system in $largeSystems) {
        $systemName = $system.System
        Write-Host "  $systemName ($($system.Lines) total lines):" -ForegroundColor Cyan
        
        $subfolders = $subfolderStats | Where-Object { $_.System -eq $systemName } | Select-Object -First 10
        if ($subfolders.Count -gt 0) {
            $subfolders | Format-Table @{Label="Subfolder"; Expression={$_.Subfolder}}, Files, Lines -AutoSize | Out-Host
        } else {
            Write-Host "    (no subfolders or all files in root)" -ForegroundColor Gray
        }
        Write-Host ""
    }
}

Write-Host "=== Largest Systems (Top 5) ===" -ForegroundColor Green
Write-Host ""
$systemStats | Select-Object -First 5 | Format-Table System, Files, Lines -AutoSize | Out-Host
Write-Host ""

Write-Host "=== Smallest Systems (Bottom 5) ===" -ForegroundColor Gray
Write-Host ""
$systemStats | Select-Object -Last 5 | Format-Table System, Files, Lines -AutoSize | Out-Host
Write-Host ""

