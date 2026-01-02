# Diagnostic script to check test file detection
# Usage: .\Scripts\diagnose-test-coverage.ps1

Write-Host "=== Test File Detection Diagnostic ===" -ForegroundColor Cyan
Write-Host ""

$testPatterns = @('Test', 'Tests', 'TestManager', 'TestRunner', 'TestHarness')

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
    } | ForEach-Object {
        $relPath = $_.FullName.Replace((Get-Location).Path + '\', '')
        
        # Try to match test file to system based on path
        $system = "Tests"
        if ($relPath -match 'Code\\Tests\\Unit\\([^\\]+)') {
            $system = $matches[1]
        }
        elseif ($relPath -match 'Code\\([^\\]+)') {
            $potentialSystem = $matches[1]
            if ($potentialSystem -ne "Tests") {
                $system = $potentialSystem
            }
        }
        
        [PSCustomObject]@{
            System = $system
            File = $relPath
        }
    }

Write-Host "Test Files by System:" -ForegroundColor Yellow
$testFiles | Group-Object System | Sort-Object Count -Descending | Format-Table Name, Count -AutoSize

Write-Host ""
Write-Host "Sample test files in each system:" -ForegroundColor Yellow
$testFiles | Group-Object System | ForEach-Object {
    Write-Host "`n$($_.Name) ($($_.Count) files):" -ForegroundColor Cyan
    $_.Group | Select-Object -First 5 | ForEach-Object {
        Write-Host "  - $($_.File)" -ForegroundColor Gray
    }
    if ($_.Count -gt 5) {
        Write-Host "  ... and $($_.Count - 5) more" -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "Checking for test files in Code/Tests/Unit subdirectories:" -ForegroundColor Yellow
$unitDirs = Get-ChildItem -Path "Code\Tests\Unit" -Directory -ErrorAction SilentlyContinue
if ($unitDirs) {
    $unitDirs | ForEach-Object {
        $dirName = $_.Name
        $filesInDir = Get-ChildItem -Path $_.FullName -Filter *.cs -Recurse | 
            Where-Object { $_.FullName -notmatch 'bin\\|obj\\' }
        $testFilesInDir = $filesInDir | Where-Object {
            $isTestFile = $false
            foreach ($pattern in $testPatterns) {
                if ($_.FullName -match $pattern) {
                    $isTestFile = $true
                    break
                }
            }
            $isTestFile
        }
        Write-Host "  $dirName : $($testFilesInDir.Count) test files found" -ForegroundColor $(if ($testFilesInDir.Count -gt 0) { "Green" } else { "Red" })
    }
} else {
    Write-Host "  No Code/Tests/Unit subdirectories found" -ForegroundColor Red
}

Write-Host ""
Write-Host "Total test files found: $($testFiles.Count)" -ForegroundColor $(if ($testFiles.Count -gt 0) { "Green" } else { "Red" })
