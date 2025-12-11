# Setting Up DungeonFighter MCP Server with Claude Desktop

This guide will help you configure Claude Desktop to connect to your DungeonFighter MCP server.

## Prerequisites

1. **Claude Desktop installed** - Download from [claude.ai/download](https://claude.ai/download)
2. **DungeonFighter project built** - Run `dotnet build` in the project directory
3. **.NET 8.0 SDK installed** - Required to run the MCP server
4. **MCP SDK Package** - Already included in the project (`ModelContextProtocol` v0.5.0-preview.1)

## Step 1: Find Claude Desktop Config File

The config file location depends on your operating system:

### Windows
```
%APPDATA%\Claude\claude_desktop_config.json
```
Full path example: `C:\Users\YourUsername\AppData\Roaming\Claude\claude_desktop_config.json`

### macOS
```
~/Library/Application Support/Claude/claude_desktop_config.json
```

### Linux
```
~/.config/Claude/claude_desktop_config.json
```

## Step 2: Create or Edit Config File

1. **If the file doesn't exist**, create it with this structure:
```json
{
  "mcpServers": {}
}
```

2. **If the file exists**, add your server to the `mcpServers` object.

## Step 3: Add DungeonFighter MCP Server Configuration

Add this configuration to your `claude_desktop_config.json`:

### Windows Configuration

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

### macOS/Linux Configuration

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
      "cwd": "/path/to/DungeonFighter-v2"
    }
  }
}
```

## Step 4: Update the Path

**IMPORTANT**: Replace the `cwd` (current working directory) path with your actual project path:

- **Windows**: Use full path with backslashes, e.g., `"D:\\Code Projects\\github projects\\DungeonFighter-v2"`
- **macOS/Linux**: Use full path with forward slashes, e.g., `"/Users/yourname/Projects/DungeonFighter-v2"`

### Finding Your Project Path

**Windows (PowerShell)**:
```powershell
(Get-Location).Path
```

**macOS/Linux (Terminal)**:
```bash
pwd
```

Copy the full path and use it in the `cwd` field.

## Step 5: Complete Example Config

Here's a complete example config file (Windows):

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
      "cwd": "D:\\Code Projects\\github projects\\DungeonFighter-v2",
      "env": {}
    }
  }
}
```

## Step 6: Restart Claude Desktop

1. **Close Claude Desktop completely** (not just the window)
2. **Restart Claude Desktop**
3. The MCP server will start automatically when Claude Desktop launches

## Step 7: Verify Connection

1. **Open Claude Desktop**
2. **Start a new conversation**
3. **Ask Claude**: "What MCP tools are available?"
4. **Or try**: "Can you start a new game of DungeonFighter?"

Claude should be able to see and use your MCP tools. You can verify by asking:
- "What tools do you have access to?"
- "Show me the DungeonFighter game tools"

**Note**: The MCP server runs in the same executable as the game. When you run with the "MCP" argument, it starts the MCP server instead of the GUI. The server communicates via stdio (standard input/output) with Claude Desktop.

## Troubleshooting

### Server Won't Start

**Error**: "Command not found" or "dotnet not found"
- **Solution**: Ensure .NET 8.0 SDK is installed and in your PATH
- **Verify**: Run `dotnet --version` in terminal

**Error**: "Project file not found"
- **Solution**: Check that the `cwd` path is correct and points to the project root
- **Verify**: The path should contain `Code/Code.csproj`

**Error**: "Build failed"
- **Solution**: Run `dotnet restore` and `dotnet build` in the project directory
- **Check**: Ensure all NuGet packages are restored

### Tools Not Appearing

**Issue**: Claude doesn't see the tools
- **Solution**: Check Claude Desktop logs for errors
- **Windows logs**: `%APPDATA%\Claude\logs\`
- **macOS logs**: `~/Library/Logs/Claude/`
- **Linux logs**: `~/.local/share/Claude/logs/`

**Issue**: Server starts but tools aren't registered
- **Solution**: Verify `McpTools.cs` has the `[McpServerToolType]` attribute
- **Check**: Ensure all tool methods have `[McpServerTool]` attributes

### Path Issues (Windows)

**Issue**: Path with spaces not working
- **Solution**: Use double backslashes: `"D:\\Code Projects\\..."` 
- **Alternative**: Use forward slashes: `"D:/Code Projects/..."`

**Issue**: Relative paths not working
- **Solution**: Always use absolute paths in the `cwd` field

## Testing the Server Manually

Before configuring Claude Desktop, you can test the server manually:

1. **Open terminal in project directory**
2. **Run**: `dotnet run -- MCP`
3. **The server should start** and wait for input on stdin
4. **Press Ctrl+C** to stop

If this works, the server is ready for Claude Desktop.

## Alternative: Using Executable Path

If you prefer to use a compiled executable instead of `dotnet run`:

1. **Build the project**: `dotnet build -c Release`
2. **Find the executable**: `Code/bin/Release/net8.0/DF.exe` (Windows) or `Code/bin/Release/net8.0/DF` (macOS/Linux)
3. **Update config**:

**Windows:**
```json
{
  "mcpServers": {
    "dungeonfighter": {
      "command": "D:\\Code Projects\\github projects\\DungeonFighter-v2\\Code\\bin\\Release\\net8.0\\DF.exe",
      "args": ["MCP"],
      "cwd": "D:\\Code Projects\\github projects\\DungeonFighter-v2"
    }
  }
}
```

**macOS/Linux:**
```json
{
  "mcpServers": {
    "dungeonfighter": {
      "command": "/path/to/DungeonFighter-v2/Code/bin/Release/net8.0/DF",
      "args": ["MCP"],
      "cwd": "/path/to/DungeonFighter-v2"
    }
  }
}
```

## Quick Reference: Information to Provide

When asking for help setting this up, provide:

1. **Your operating system**: Windows / macOS / Linux
2. **Project path**: The full path to your DungeonFighter-v2 directory
3. **.NET version**: Output of `dotnet --version`
4. **Any error messages**: From Claude Desktop logs or terminal

## Example Conversation with Claude

Once set up, you can interact with the game like this:

**You**: "Start a new DungeonFighter game"

**Claude**: [Uses `start_new_game` tool, returns game state]

**You**: "What's my current health and level?"

**Claude**: [Uses `get_player_stats` tool, tells you your stats]

**You**: "Enter the first dungeon"

**Claude**: [Uses `handle_input` tool with appropriate menu selections]

**You**: "Attack the enemy"

**Claude**: [Uses `handle_input` tool with combat action, shows results]

## Next Steps

After setup:
1. Test basic game operations
2. Try different tools
3. Play through a full combat encounter
4. Experiment with inventory management
5. Explore dungeon navigation

Enjoy playing DungeonFighter with AI assistance!

