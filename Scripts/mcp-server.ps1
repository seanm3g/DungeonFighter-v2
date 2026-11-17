# Launches the DungeonFighter MCP server from an up-to-date build (used by .mcp.json).
#
# When any source under Code/ (outside bin/obj/Tests) is newer than the newest MCP build, it builds again into a
# new folder Code/bin/_mcp/<timestamp>/, so a server still running from an older folder never blocks the build.
# Older build folders are removed when nothing has them open. Build output goes to stderr: stdout is the MCP
# protocol channel and must stay clean.
#
# Force a rebuild:   powershell -File Scripts/mcp-server.ps1 -Rebuild -BuildOnly
param(
    [switch]$Rebuild,
    [switch]$BuildOnly
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$codeDir = Join-Path $repoRoot 'Code'
$buildRoot = Join-Path $codeDir 'bin\_mcp'

function Write-Log([string]$message) { [Console]::Error.WriteLine("[mcp-server] $message") }

function Get-NewestBuild {
    if (-not (Test-Path $buildRoot)) { return $null }
    Get-ChildItem $buildRoot -Directory |
        Where-Object { Test-Path (Join-Path $_.FullName 'DF.exe') } |
        Sort-Object Name -Descending |
        Select-Object -First 1
}

function Get-NewestSourceTime {
    # Walk Code/ but never descend into bin/obj (they hold many full builds and make a plain -Recurse slow).
    $newest = [datetime]::MinValue
    $skip = @('bin', 'obj', 'Tests', '.vs')
    $stack = New-Object System.Collections.Stack
    $stack.Push($codeDir)
    while ($stack.Count -gt 0) {
        $dir = $stack.Pop()
        foreach ($f in [System.IO.Directory]::EnumerateFiles($dir)) {
            $ext = [System.IO.Path]::GetExtension($f)
            if ($ext -in '.cs', '.csproj', '.axaml', '.json') {
                $t = [System.IO.File]::GetLastWriteTime($f)
                if ($t -gt $newest) { $newest = $t }
            }
        }
        foreach ($d in [System.IO.Directory]::EnumerateDirectories($dir)) {
            if ($skip -notcontains [System.IO.Path]::GetFileName($d)) { $stack.Push($d) }
        }
    }
    return $newest
}

$build = Get-NewestBuild
$needsBuild = $Rebuild -or ($null -eq $build)
if (-not $needsBuild) {
    # The folder is created when its build starts; DF.exe can keep an older time when the compile was up to date.
    $builtAt = $build.CreationTime
    $sourceAt = Get-NewestSourceTime
    if ($sourceAt -gt $builtAt) {
        Write-Log "Code changed since the last MCP build ($sourceAt > $builtAt); rebuilding."
        $needsBuild = $true
    }
}

if ($needsBuild) {
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $outDir = Join-Path $buildRoot $stamp
    # Tests.csproj builds the game too and puts DF.Tests.dll next to DF.exe (the MCP test tools load it from there).
    Write-Log "Building Code/Tests/Tests.csproj (game + tests) -> $outDir"
    $buildLog = & dotnet build (Join-Path $codeDir 'Tests\Tests.csproj') -c Debug -o $outDir "-p:GameOutputDir=$outDir\" -nologo -v q 2>&1
    $buildLog | ForEach-Object { [Console]::Error.WriteLine($_) }
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path (Join-Path $outDir 'DF.exe'))) {
        Write-Log "Build failed (exit $LASTEXITCODE)."
        if (Test-Path $outDir) { Remove-Item $outDir -Recurse -Force -ErrorAction SilentlyContinue }
        if ($null -eq $build) { exit 1 }
        Write-Log "Falling back to the previous build: $($build.FullName)"
    }
    else {
        $build = Get-Item $outDir
    }
}

# Remove older build folders that are not in use (a running server keeps its DF.exe locked; skip those).
if (Test-Path $buildRoot) {
    Get-ChildItem $buildRoot -Directory | Where-Object { $_.FullName -ne $build.FullName } | ForEach-Object {
        try { Remove-Item $_.FullName -Recurse -Force -ErrorAction Stop } catch { }
    }
}

if ($BuildOnly) {
    Write-Log "Build ready: $($build.FullName)"
    exit 0
}

$exe = Join-Path $build.FullName 'DF.exe'
Write-Log "Starting $exe MCP"
# Start-Process with -NoNewWindow inherits this process's stdin/stdout/stderr handles directly, so the MCP
# stdio stream reaches DF.exe untouched (PowerShell's own pipeline would re-encode it).
$proc = Start-Process -FilePath $exe -ArgumentList 'MCP' -WorkingDirectory $repoRoot -NoNewWindow -Wait -PassThru
exit $proc.ExitCode
