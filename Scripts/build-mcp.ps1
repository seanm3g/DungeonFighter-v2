# Builds the MCP server now (game + tests) into a fresh Code/bin/_mcp/<timestamp>/ folder.
# Normally not needed: Scripts/mcp-server.ps1 (what .mcp.json runs) rebuilds by itself when Code/ changed.
# Restart the dungeonfighter MCP server afterwards to load the new build.
& (Join-Path $PSScriptRoot 'mcp-server.ps1') -Rebuild -BuildOnly
exit $LASTEXITCODE
