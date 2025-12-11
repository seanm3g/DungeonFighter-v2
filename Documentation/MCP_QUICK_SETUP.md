# Quick Setup Guide for Claude Desktop

## Information to Provide to Claude Desktop

Copy and paste this configuration into your Claude Desktop config file, **replacing the path with your actual project path**:

### Windows

```json
{
  "mcpServers": {
    "dungeonfighter": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "Code/Code.csproj",
        "--",
        "MCP"
      ],
      "cwd": "D:\\Code Projects\\github projects\\DungeonFighter-v2"
    }
  }
}
```

### macOS/Linux

```json
{
  "mcpServers": {
    "dungeonfighter": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "Code/Code.csproj",
        "--",
        "MCP"
      ],
      "cwd": "/Users/yourname/path/to/DungeonFighter-v2"
    }
  }
}
```

## Config File Locations

- **Windows**: `%APPDATA%\Claude\claude_desktop_config.json`
- **macOS**: `~/Library/Application Support/Claude/claude_desktop_config.json`
- **Linux**: `~/.config/Claude/claude_desktop_config.json`

## Steps

1. **Find your project path**:
   - Windows (PowerShell): `(Get-Location).Path`
   - macOS/Linux: `pwd`

2. **Replace the `cwd` value** in the JSON above with your actual path

3. **Save the config file**

4. **Restart Claude Desktop**

5. **Test**: Ask Claude "What MCP tools do you have access to?"

## Troubleshooting

- **Path with spaces**: Use double backslashes (`\\`) on Windows
- **Server won't start**: Run `dotnet restore` and `dotnet build` first
- **Tools not appearing**: Check Claude Desktop logs for errors

