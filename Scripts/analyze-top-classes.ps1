# Analyze top classes by size in UI, Game, and Combat systems
# Usage: .\Scripts\analyze-top-classes.ps1

Write-Host "=== Top Classes Analysis for Test Coverage ===" -ForegroundColor Cyan
Write-Host ""

$systems = @("UI", "Game", "Combat")
$testPatterns = @('Test', 'Tests', 'TestManager', 'TestRunner', 'TestHarness')

foreach ($system in $systems) {
    Write-Host "=== $system System ===" -ForegroundColor Yellow
    Write-Host ""
    
    $prodFiles = Get-ChildItem -Path "Code\$system" -Filter *.cs -Recurse | 
        Where-Object { $_.FullName -notmatch 'bin\\|obj\\|Code_backup' } |
        Where-Object {
            $isTestFile = $false
            foreach ($pattern in $testPatterns) {
                if ($_.FullName -match $pattern) {
                    $isTestFile = $true
                    break
                }
            }
            -not $isTestFile
        } |
        ForEach-Object {
            try {
                $content = Get-Content $_.FullName -ReadCount 0 -Raw -ErrorAction Stop
                $lineCount = ($content -split "`r?`n").Count
                $relPath = $_.FullName.Replace((Get-Location).Path + '\', '')
                [PSCustomObject]@{
                    File = $_.Name
                    Path = $relPath
                    Lines = $lineCount
                }
            } catch {
                Write-Warning "Skipping file: $($_.FullName)"
                $null
            }
        } | Where-Object { $null -ne $_ }
    
    # Check for test files
    $testFiles = Get-ChildItem -Path "Code\Tests" -Filter *.cs -Recurse -ErrorAction SilentlyContinue |
        Where-Object {
            $relPath = $_.FullName.Replace((Get-Location).Path + '\', '')
            $relPath -match "\\$system\\" -or $relPath -match "\\Tests\\Unit\\$system\\"
        } |
        ForEach-Object {
            try {
                $content = Get-Content $_.FullName -ReadCount 0 -Raw -ErrorAction Stop
                $lineCount = ($content -split "`r?`n").Count
                [PSCustomObject]@{
                    File = $_.Name
                    Lines = $lineCount
                }
            } catch {
                $null
            }
        } | Where-Object { $null -ne $_ }
    
    $testCount = ($testFiles | Measure-Object).Count
    
    Write-Host "Top 10 Largest Classes:" -ForegroundColor Cyan
    $topClasses = $prodFiles | Sort-Object Lines -Descending | Select-Object -First 10
    $topClasses | Format-Table File, Lines, Path -AutoSize | Out-Host
    
    Write-Host "Test Files for $system : $testCount" -ForegroundColor $(if ($testCount -gt 0) { "Green" } else { "Red" })
    Write-Host ""
}

Write-Host "=== Summary: Top 3 Classes Needing Coverage ===" -ForegroundColor Magenta
Write-Host ""

$allClasses = @()
foreach ($system in $systems) {
    $prodFiles = Get-ChildItem -Path "Code\$system" -Filter *.cs -Recurse | 
        Where-Object { $_.FullName -notmatch 'bin\\|obj\\|Code_backup' } |
        Where-Object {
            $isTestFile = $false
            foreach ($pattern in $testPatterns) {
                if ($_.FullName -match $pattern) {
                    $isTestFile = $true
                    break
                }
            }
            -not $isTestFile
        } |
        ForEach-Object {
            try {
                $content = Get-Content $_.FullName -ReadCount 0 -Raw -ErrorAction Stop
                $lineCount = ($content -split "`r?`n").Count
                $relPath = $_.FullName.Replace((Get-Location).Path + '\', '')
                [PSCustomObject]@{
                    System = $system
                    File = $_.Name
                    Path = $relPath
                    Lines = $lineCount
                }
            } catch {
                $null
            }
        } | Where-Object { $null -ne $_ }
    
    $allClasses += $prodFiles
}

$top3 = $allClasses | Sort-Object Lines -Descending | Select-Object -First 3
$top3 | Format-Table System, File, Lines, Path -AutoSize | Out-Host

Write-Host ""
Write-Host "These are the 3 largest classes across UI, Game, and Combat systems." -ForegroundColor Yellow
Write-Host "They should be prioritized for test coverage." -ForegroundColor Yellow
