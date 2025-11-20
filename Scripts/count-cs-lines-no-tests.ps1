# Count lines in .cs files excluding test classes and show files above 400 lines, sorted by line count
# Usage: .\Scripts\count-cs-lines-no-tests.ps1

$threshold = 400

# Test files to exclude
$testExclusions = @(
    'TestManager.cs',
    'GameSystemTestRunner.cs',
    'Program.cs',  # StandaloneColorDemo
    'ColorSystemTest',
    'Tests',
    'Test.cs'
)

Write-Host "Scanning .cs files (excluding test classes) for files above $threshold lines..." -ForegroundColor Cyan
Write-Host ""

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
        $relPath = $_.FullName.Replace((Get-Location).Path + '\', '')
        $lineCount = (Get-Content $_.FullName -ErrorAction SilentlyContinue | Measure-Object -Line).Lines
        [PSCustomObject]@{
            File = $relPath
            Lines = $lineCount
        }
    } | 
    Where-Object { $_.Lines -gt $threshold } | 
    Sort-Object Lines -Descending

$productionFiles | Format-Table File, Lines -AutoSize

# Total for files above threshold
$total = $productionFiles | Measure-Object -Property Lines -Sum | Select-Object -ExpandProperty Sum
$count = $productionFiles | Measure-Object | Select-Object -ExpandProperty Count

# Total for ALL production files
$allProductionFiles = Get-ChildItem -Path Code -Filter *.cs -Recurse | 
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
        (Get-Content $_.FullName -ErrorAction SilentlyContinue | Measure-Object -Line).Lines
    }

$totalAllLines = ($allProductionFiles | Measure-Object -Sum).Sum
$totalFileCount = ($allProductionFiles | Measure-Object).Count

Write-Host "Production files above ${threshold} lines: $count" -ForegroundColor Green
Write-Host "Total lines in files above ${threshold}: $total" -ForegroundColor Green
Write-Host ""
Write-Host "ALL production files: $totalFileCount" -ForegroundColor Cyan
Write-Host "Total lines in ALL production code: $totalAllLines" -ForegroundColor Cyan
Write-Host ""

# Show test files separately for reference
Write-Host "Test/Utility files (excluded):" -ForegroundColor Yellow
Get-ChildItem -Path Code -Filter *.cs -Recurse | 
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
        $lineCount = (Get-Content $_.FullName -ErrorAction SilentlyContinue | Measure-Object -Line).Lines
        [PSCustomObject]@{
            File = $relPath
            Lines = $lineCount
        }
    } |
    Sort-Object Lines -Descending |
    Format-Table File, Lines -AutoSize

