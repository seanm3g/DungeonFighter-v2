# Builds the MCP server binary to Code/bin/_agent_build (separate from Debug).
# While MCP is running, it locks _agent_build\DF.exe — use normal `dotnet build` for Debug/tests instead.
# To deploy MCP changes: restart MCP in Cursor, then run this script.

$ErrorActionPreference = "Stop"
$codeDir = (Join-Path (Join-Path $PSScriptRoot "..") "Code") | Resolve-Path
$outputDir = Join-Path $codeDir "bin\_agent_build"

Write-Host "Building MCP server to $outputDir ..."
Push-Location $codeDir
try {
    dotnet build -o $outputDir
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
finally {
    Pop-Location
}

Write-Host ""
Write-Host "Done. Restart the dungeonfighter MCP server in Cursor to load the new build."
