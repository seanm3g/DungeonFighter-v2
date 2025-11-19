# Count lines in .cs files and show files above 400 lines, sorted by line count
# Usage: .\Scripts\count-cs-lines.ps1

$threshold = 400

Write-Host "Scanning .cs files for files above $threshold lines..." -ForegroundColor Cyan
Write-Host ""

Get-ChildItem -Path Code -Filter *.cs -Recurse | 
    Where-Object { $_.FullName -notmatch 'bin\\|obj\\|Code_backup' } | 
    ForEach-Object { 
        $relPath = $_.FullName.Replace((Get-Location).Path + '\', '')
        $lineCount = (Get-Content $_.FullName -ErrorAction SilentlyContinue | Measure-Object -Line).Lines
        [PSCustomObject]@{
            File = $relPath
            Lines = $lineCount
        }
    } | 
    Where-Object { $_.Lines -gt $threshold } | 
    Sort-Object Lines -Descending | 
    Format-Table File, Lines -AutoSize

$total = (Get-ChildItem -Path Code -Filter *.cs -Recurse | 
    Where-Object { $_.FullName -notmatch 'bin\\|obj\\|Code_backup' } | 
    ForEach-Object { 
        (Get-Content $_.FullName -ErrorAction SilentlyContinue | Measure-Object -Line).Lines
    } | 
    Measure-Object -Sum).Sum

Write-Host "Total lines in all .cs files: $total" -ForegroundColor Green

