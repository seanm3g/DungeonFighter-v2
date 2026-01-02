# Test coverage analysis - Count test files vs production files per system
# Usage: .\Scripts\test-coverage-analysis.ps1

Write-Host "=== Test Coverage Analysis ===" -ForegroundColor Cyan
Write-Host ""

# Test file patterns
$testPatterns = @(
    'Test',
    'Tests',
    'TestManager',
    'TestRunner',
    'TestHarness'
)

# Get all production files
$productionFiles = Get-ChildItem -Path Code -Filter *.cs -Recurse | 
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
        $relPath = $_.FullName.Replace((Get-Location).Path + '\', '')
        $folderMatch = $relPath -match '^Code\\([^\\]+)'
        $system = if ($folderMatch) { $matches[1] } else { "Other" }
        
        try {
            $content = Get-Content $_.FullName -ReadCount 0 -Raw -ErrorAction Stop
            $lineCount = ($content -split "`r?`n").Count
            [PSCustomObject]@{
                System = $system
                File = $relPath
                Lines = $lineCount
            }
        } catch {
            Write-Warning "Skipping file due to read error: $relPath"
            $null
        }
    } | Where-Object { $null -ne $_ }

# Get all test files
$testFiles = Get-ChildItem -Path Code -Filter *.cs -Recurse | 
    Where-Object { $_.FullName -notmatch 'bin\\|obj\\|Code_backup' } |
    Where-Object {
        $isTestFile = $false
        foreach ($pattern in $testPatterns) {
            if ($_.FullName -match $pattern) {
                $isTestFile = $true
                break
            }
        }
        $isTestFile
    } |
    ForEach-Object {
        $relPath = $_.FullName.Replace((Get-Location).Path + '\', '')
        
        # Try to match test file to system based on path
        $system = "Tests"
        
        # First check for Code/Tests/Unit/{System}/ pattern (files in subdirectories)
        if ($relPath -match 'Code\\Tests\\Unit\\([^\\]+)\\') {
            # Test file is in Code/Tests/Unit/{System}/, map to that system
            $system = $matches[1]
        }
        # Check for test files directly in Code/Tests/Unit/ (no subdirectory)
        # Try to infer system from filename patterns
        elseif ($relPath -match 'Code\\Tests\\Unit\\([^\\]+)\.cs$') {
            $fileName = $matches[1]
            # Map common test file patterns to systems
            if ($fileName -match '^(Combat|Battle|Combo|DiceRoll|EnemyRoll|MultiHit|StatusEffects)') {
                $system = "Combat"
            }
            elseif ($fileName -match '^(Game|GameState|Dungeon|LevelUp|XP)') {
                $system = "Game"
            }
            elseif ($fileName -match '^(UI|BlockDisplay|Canvas|Color|ColoredText|UIMessage|KeywordColor|Display|DungeonRenderer|Settings|ItemModifiers|ItemsTab|GameCanvas)') {
                $system = "UI"
            }
            elseif ($fileName -match '^(Action|ClassAction|GearAction|DefaultAction|ComboSequence|Conditional|Environmental)') {
                $system = "Actions"
            }
            elseif ($fileName -match '^(Character|Enemy|Equipment|LevelUp|Health)') {
                $system = "Entity"
            }
            elseif ($fileName -match '^(Item|Inventory|ComboManager|AttributeRequirement|BasicGear)') {
                $system = "Items"
            }
            elseif ($fileName -match '^(Data|Loader|Generator|Cache|Loot)') {
                $system = "Data"
            }
            elseif ($fileName -match '^(Config|CharacterConfig|CombatConfig|ItemConfig|EnemyConfig|UIConfig)') {
                $system = "Config"
            }
            elseif ($fileName -match '^(World|Dungeon|Room|Environment)') {
                $system = "World"
            }
            elseif ($fileName -match '^(MCP|DungeonFighterMCP)') {
                $system = "MCP"
            }
            elseif ($fileName -match '^(Simulation|Scenario)') {
                $system = "Simulation"
            }
            else {
                # Default to "Tests" if we can't determine
                $system = "Tests"
            }
        }
        # Check for test files in Code/{System}/ that contain "Test" in path
        elseif ($relPath -match 'Code\\([^\\]+)\\') {
            $potentialSystem = $matches[1]
            if ($potentialSystem -ne "Tests" -and $potentialSystem -match '^(UI|Game|Combat|Actions|Entity|Items|Data|Config|World|MCP|Simulation)$') {
                $system = $potentialSystem
            }
        }
        
        try {
            $content = Get-Content $_.FullName -ReadCount 0 -Raw -ErrorAction Stop
            $lineCount = ($content -split "`r?`n").Count
            [PSCustomObject]@{
                System = $system
                File = $relPath
                Lines = $lineCount
            }
        } catch {
            Write-Warning "Skipping file due to read error: $relPath"
            $null
        }
    } | Where-Object { $null -ne $_ }

# Calculate coverage by system
$systemCoverage = @{}
$allSystems = ($productionFiles.System + $testFiles.System) | Select-Object -Unique

foreach ($system in $allSystems) {
    $prodFiles = $productionFiles | Where-Object { $_.System -eq $system }
    $testFilesForSystem = $testFiles | Where-Object { $_.System -eq $system }
    
    $prodCount = ($prodFiles | Measure-Object).Count
    $prodLines = ($prodFiles | Measure-Object -Property Lines -Sum).Sum
    $testCount = ($testFilesForSystem | Measure-Object).Count
    $testLines = ($testFilesForSystem | Measure-Object -Property Lines -Sum).Sum
    
    $fileRatio = if ($prodCount -gt 0) { [math]::Round(($testCount / $prodCount) * 100, 1) } else { 0 }
    $lineRatio = if ($prodLines -gt 0) { [math]::Round(($testLines / $prodLines) * 100, 1) } else { 0 }
    
    $coverage = if ($fileRatio -ge 50 -and $lineRatio -ge 50) { "Good" }
                elseif ($fileRatio -ge 25 -or $lineRatio -ge 25) { "Fair" }
                else { "Poor" }
    
    $systemCoverage[$system] = [PSCustomObject]@{
        System = $system
        ProdFiles = $prodCount
        ProdLines = $prodLines
        TestFiles = $testCount
        TestLines = $testLines
        FileRatio = $fileRatio
        LineRatio = $lineRatio
        Coverage = $coverage
    }
}

# Overall statistics
$totalProdFiles = ($productionFiles | Measure-Object).Count
$totalProdLines = ($productionFiles | Measure-Object -Property Lines -Sum).Sum
$totalTestFiles = ($testFiles | Measure-Object).Count
$totalTestLines = ($testFiles | Measure-Object -Property Lines -Sum).Sum
$overallFileRatio = if ($totalProdFiles -gt 0) { [math]::Round(($totalTestFiles / $totalProdFiles) * 100, 1) } else { 0 }
$overallLineRatio = if ($totalProdLines -gt 0) { [math]::Round(($totalTestLines / $totalProdLines) * 100, 1) } else { 0 }

# Output
Write-Host "=== Overall Test Coverage ===" -ForegroundColor Yellow
Write-Host ""
Write-Host "Production: $totalProdFiles files, $totalProdLines lines" -ForegroundColor Green
Write-Host "Tests: $totalTestFiles files, $totalTestLines lines" -ForegroundColor Yellow
Write-Host "File coverage: ${overallFileRatio}% (test files / production files)" -ForegroundColor Cyan
Write-Host "Line coverage: ${overallLineRatio}% (test lines / production lines)" -ForegroundColor Cyan
Write-Host ""

Write-Host "=== Coverage by System ===" -ForegroundColor Magenta
Write-Host ""
$coverageData = $systemCoverage.Values | Sort-Object ProdFiles -Descending
$coverageData | Format-Table System, ProdFiles, TestFiles, ProdLines, TestLines, @{Label="File%"; Expression={"$($_.FileRatio)%"}}, @{Label="Line%"; Expression={"$($_.LineRatio)%"}}, Coverage -AutoSize | Out-Host
Write-Host ""

Write-Host "=== Systems with Good Coverage (>=50% files and lines) ===" -ForegroundColor Green
Write-Host ""
$goodCoverage = $coverageData | Where-Object { $_.Coverage -eq "Good" }
if ($goodCoverage.Count -gt 0) {
    $goodCoverage | Format-Table System, ProdFiles, TestFiles, FileRatio, LineRatio -AutoSize | Out-Host
} else {
    Write-Host "  No systems with good coverage yet." -ForegroundColor Gray
}
Write-Host ""

Write-Host "=== Systems Needing More Tests (Poor coverage) ===" -ForegroundColor Red
Write-Host ""
$poorCoverage = $coverageData | Where-Object { $_.Coverage -eq "Poor" -and $_.ProdFiles -gt 0 }
if ($poorCoverage.Count -gt 0) {
    $poorCoverage | Format-Table System, ProdFiles, TestFiles, FileRatio, LineRatio -AutoSize | Out-Host
} else {
    Write-Host "  All systems have at least fair coverage!" -ForegroundColor Green
}
Write-Host ""

Write-Host "=== Largest Test Files ===" -ForegroundColor Yellow
Write-Host ""
$testFiles | Sort-Object Lines -Descending | Select-Object -First 10 | Format-Table File, System, Lines -AutoSize | Out-Host
Write-Host ""

