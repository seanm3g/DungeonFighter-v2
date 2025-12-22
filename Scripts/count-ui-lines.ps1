# Count UI files (AXAML and code-behind) and analyze UI vs business logic ratio
# Usage: .\Scripts\count-ui-lines.ps1

Write-Host "=== UI File Analysis ===" -ForegroundColor Cyan
Write-Host ""

# Get AXAML files
$axamlFiles = Get-ChildItem -Path Code -Filter *.axaml -Recurse | 
    Where-Object { $_.FullName -notmatch 'bin\\|obj\\|Code_backup' } |
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
                Type = "AXAML"
            }
        } catch {
            Write-Warning "Skipping file due to read error: $relPath"
            $null
        }
    } | Where-Object { $null -ne $_ }

# Get UI-related C# files (code-behind and ViewModels)
$uiCodeFiles = Get-ChildItem -Path Code\UI -Filter *.cs -Recurse | 
    Where-Object { $_.FullName -notmatch 'bin\\|obj\\|Code_backup' } |
    ForEach-Object {
        $relPath = $_.FullName.Replace((Get-Location).Path + '\', '')
        $folderMatch = $relPath -match '^Code\\UI\\([^\\]+)'
        $subfolder = if ($folderMatch) { $matches[1] } else { "(root)" }
        
        try {
            $content = Get-Content $_.FullName -ReadCount 0 -Raw -ErrorAction Stop
            $lineCount = ($content -split "`r?`n").Count
            [PSCustomObject]@{
                File = $relPath
                Folder = "UI"
                Subfolder = $subfolder
                Lines = $lineCount
                Type = "C# (UI)"
            }
        } catch {
            Write-Warning "Skipping file due to read error: $relPath"
            $null
        }
    } | Where-Object { $null -ne $_ }

# Get business logic files (non-UI C# files)
$testExclusions = @(
    'TestManager.cs',
    'GameSystemTestRunner.cs',
    'UISystemTestRunner.cs',
    'Program.cs',
    'ColorSystemTest',
    'Tests',
    'Test.cs'
)

$businessLogicFiles = Get-ChildItem -Path Code -Filter *.cs -Recurse | 
    Where-Object { $_.FullName -notmatch 'bin\\|obj\\|Code_backup|UI\\' } |
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
                Type = "C# (Business Logic)"
            }
        } catch {
            Write-Warning "Skipping file due to read error: $relPath"
            $null
        }
    } | Where-Object { $null -ne $_ }

# Calculate totals
$totalAxamlFiles = ($axamlFiles | Measure-Object).Count
$totalAxamlLines = ($axamlFiles | Measure-Object -Property Lines -Sum).Sum
$totalUICodeFiles = ($uiCodeFiles | Measure-Object).Count
$totalUICodeLines = ($uiCodeFiles | Measure-Object -Property Lines -Sum).Sum
$totalBusinessLogicFiles = ($businessLogicFiles | Measure-Object).Count
$totalBusinessLogicLines = ($businessLogicFiles | Measure-Object -Property Lines -Sum).Sum

$totalUILines = $totalAxamlLines + $totalUICodeLines
$totalUIFiles = $totalAxamlFiles + $totalUICodeFiles
$totalCodeLines = $totalUILines + $totalBusinessLogicLines
$totalCodeFiles = $totalUIFiles + $totalBusinessLogicFiles

# UI subfolder breakdown
$uiSubfolderStats = $uiCodeFiles |
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

# Output
Write-Host "=== Overall Statistics ===" -ForegroundColor Yellow
Write-Host ""
Write-Host "AXAML files: $totalAxamlFiles files, $totalAxamlLines lines" -ForegroundColor Cyan
Write-Host "UI C# files: $totalUICodeFiles files, $totalUICodeLines lines" -ForegroundColor Cyan
Write-Host "Total UI: $totalUIFiles files, $totalUILines lines" -ForegroundColor Green
Write-Host ""
Write-Host "Business Logic: $totalBusinessLogicFiles files, $totalBusinessLogicLines lines" -ForegroundColor Yellow
Write-Host ""
Write-Host "Total Codebase: $totalCodeFiles files, $totalCodeLines lines" -ForegroundColor Magenta
Write-Host ""

if ($totalCodeLines -gt 0) {
    $uiPercentage = [math]::Round(($totalUILines / $totalCodeLines) * 100, 1)
    $businessPercentage = [math]::Round(($totalBusinessLogicLines / $totalCodeLines) * 100, 1)
    
    Write-Host "=== Code Distribution ===" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "UI: ${uiPercentage}% of total codebase" -ForegroundColor Green
    Write-Host "Business Logic: ${businessPercentage}% of total codebase" -ForegroundColor Yellow
    Write-Host ""
}

Write-Host "=== UI Subfolder Breakdown ===" -ForegroundColor Magenta
Write-Host ""
if ($uiSubfolderStats.Count -gt 0) {
    $uiSubfolderStats | Format-Table Subfolder, Files, Lines -AutoSize | Out-Host
} else {
    Write-Host "  (no subfolders in UI)" -ForegroundColor Gray
}
Write-Host ""

Write-Host "=== Largest AXAML Files ===" -ForegroundColor Yellow
Write-Host ""
if ($totalAxamlFiles -gt 0) {
    $axamlFiles | Sort-Object Lines -Descending | Select-Object -First 10 | Format-Table File, Lines -AutoSize | Out-Host
} else {
    Write-Host "  No AXAML files found" -ForegroundColor Gray
}
Write-Host ""

Write-Host "=== Largest UI C# Files ===" -ForegroundColor Yellow
Write-Host ""
if ($totalUICodeFiles -gt 0) {
    $uiCodeFiles | Sort-Object Lines -Descending | Select-Object -First 10 | Format-Table File, Subfolder, Lines -AutoSize | Out-Host
} else {
    Write-Host "  No UI C# files found" -ForegroundColor Gray
}
Write-Host ""

