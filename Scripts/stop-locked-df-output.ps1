# Kill processes holding Code build outputs (DF.exe / DF.dll) so MSBuild can copy.
# Safe to run repeatedly; always exits 0 so builds are not failed by this helper.
param(
    [string]$TargetPath = "",
    [string]$OutputDir = ""
)

$ErrorActionPreference = "SilentlyContinue"

function Stop-ByName([string]$name) {
    Get-Process -Name $name -ErrorAction SilentlyContinue | ForEach-Object {
        try {
            Write-Host "stop-locked-df-output: stopping $($_.ProcessName) (PID $($_.Id))"
            Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
        } catch { }
    }
    # Also try taskkill for stubborn handles
    & taskkill.exe /F /IM "$name.exe" 2>$null | Out-Null
}

# Image-name kills (MSBuild may report FileDescription "EnemyImportProbe" for DF.exe)
Stop-ByName "DF"
Stop-ByName "EnemyImportProbe"

# Path-based kill: any process whose main module is our apphost / output exe
$paths = New-Object System.Collections.Generic.HashSet[string] ([StringComparer]::OrdinalIgnoreCase)
if (-not [string]::IsNullOrWhiteSpace($TargetPath)) {
    if (Test-Path -LiteralPath $TargetPath) {
        [void]$paths.Add((Resolve-Path -LiteralPath $TargetPath).Path)
    } else {
        [void]$paths.Add($TargetPath)
    }
}
if (-not [string]::IsNullOrWhiteSpace($OutputDir) -and (Test-Path -LiteralPath $OutputDir)) {
    Get-ChildItem -LiteralPath $OutputDir -Filter "DF.exe" -Recurse -ErrorAction SilentlyContinue | ForEach-Object { [void]$paths.Add($_.FullName) }
    Get-ChildItem -LiteralPath $OutputDir -Filter "EnemyImportProbe.exe" -Recurse -ErrorAction SilentlyContinue | ForEach-Object { [void]$paths.Add($_.FullName) }
}

if ($paths.Count -gt 0) {
    Get-Process -ErrorAction SilentlyContinue | ForEach-Object {
        $proc = $_
        try {
            $mod = $proc.MainModule.FileName
            if ($mod -and $paths.Contains($mod)) {
                Write-Host "stop-locked-df-output: stopping path-locked $($proc.ProcessName) (PID $($proc.Id)) -> $mod"
                Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
            }
        } catch { }
    }
}

Start-Sleep -Milliseconds 250
exit 0
