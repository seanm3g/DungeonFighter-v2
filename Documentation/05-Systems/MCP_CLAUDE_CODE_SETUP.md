# Setting Up DungeonFighter MCP Server with Claude Code

This guide will help you configure Claude Code (Anthropic's AI coding assistant) to connect to your DungeonFighter MCP server.

## Prerequisites

1. **Claude Code installed** - Claude Code is Anthropic's AI coding assistant that works within IDEs
2. **DungeonFighter project built** - Run `dotnet build` in the project directory
3. **.NET 8.0 SDK installed** - Required to run the MCP server
4. **MCP SDK Package** - Already included in the project (`ModelContextProtocol` v0.5.0-preview.1)

## Current MCP Server Compatibility

✅ **The MCP server is compatible with Claude Code!**

The server uses:
- **stdio transport** - Standard MCP transport protocol
- **ModelContextProtocol SDK v0.5.0-preview.1** - Compatible with Claude Code
- **Standard MCP protocol** - Works with any MCP-compatible client

## Configuration Options

Claude Code can be configured in different ways depending on your IDE setup. Here are the common approaches:

### Option 1: Project-Level Configuration (`.mcp.json`)

Create a `.mcp.json` file in your project root directory:

**Windows:**
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

**macOS/Linux:**
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

### Option 2: User-Level Configuration

Claude Code may also support user-level configuration files. Check your IDE's documentation for the exact location, which may be similar to:
- **Windows**: `%APPDATA%\Claude Code\mcp.json` or `%USERPROFILE%\.claude-code\mcp.json`
- **macOS**: `~/Library/Application Support/Claude Code/mcp.json` or `~/.claude-code/mcp.json`
- **Linux**: `~/.config/claude-code/mcp.json` or `~/.claude-code/mcp.json`

### Option 3: IDE-Specific Configuration

Some IDEs may have their own configuration files. Check your IDE's Claude Code extension documentation for:
- VS Code: Check extension settings or workspace configuration
- JetBrains IDEs: Check plugin settings
- Other IDEs: Refer to Claude Code documentation for your specific IDE

## Step-by-Step Setup

### Step 1: Find Your Project Path

**Windows (PowerShell):**
```powershell
(Get-Location).Path
```

**macOS/Linux (Terminal):**
```bash
pwd
```

### Step 2: Create Configuration File

1. **Create `.mcp.json`** in your project root directory
2. **Copy the configuration** from Option 1 above
3. **Replace the `cwd` path** with your actual project path

**Important Path Notes:**
- **Windows**: Use double backslashes (`\\`) or forward slashes (`/`)
- **macOS/Linux**: Use forward slashes (`/`)
- Always use **absolute paths** (not relative)

### Step 3: Alternative - Using Compiled Executable

If you prefer to use a compiled executable instead of `dotnet run`:

1. **Build the project**: `dotnet build -c Release`
2. **Find the executable**: 
   - Windows: `Code/bin/Release/net8.0/DF.exe`
   - macOS/Linux: `Code/bin/Release/net8.0/DF`
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

### Step 4: Restart Your IDE

1. **Close your IDE completely**
2. **Restart your IDE**
3. The MCP server should start automatically when Claude Code connects

### Step 5: Verify Connection

1. **Open your IDE with Claude Code**
2. **Start a conversation with Claude Code**
3. **Ask Claude Code**: "What MCP tools are available?"
4. **Or try**: "Can you start a new game of DungeonFighter?"

Claude Code should be able to see and use your MCP tools. You can verify by asking:
- "What tools do you have access to?"
- "Show me the DungeonFighter game tools"
- "List all available MCP servers"

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

**Issue**: Claude Code doesn't see the tools
- **Solution**: Check IDE logs for errors
- **Check**: Look for MCP-related errors in your IDE's output/console
- **Verify**: Ensure the configuration file is in the correct location

**Issue**: Server starts but tools aren't registered
- **Solution**: Verify `McpTools.cs` has the `[McpServerToolType]` attribute
- **Check**: Ensure all tool methods have `[McpServerTool]` attributes
- **Verify**: Check that the MCP SDK is properly referenced

### Path Issues (Windows)

**Issue**: Path with spaces not working
- **Solution**: Use double backslashes: `"D:\\Code Projects\\..."` 
- **Alternative**: Use forward slashes: `"D:/Code Projects/..."`

**Issue**: Relative paths not working
- **Solution**: Always use absolute paths in the `cwd` field

### Configuration File Not Found

**Issue**: Claude Code can't find the configuration
- **Solution**: Check your IDE's Claude Code documentation for the correct config file location
- **Alternative**: Try placing `.mcp.json` in different locations:
  - Project root
  - User home directory
  - IDE-specific config directory

## Testing the Server Manually

Before configuring Claude Code, you can test the server manually:

1. **Open terminal in project directory**
2. **Run**: `dotnet run --project Code/Code.csproj -- MCP`
3. **The server should start** and wait for input on stdin
4. **Press Ctrl+C** to stop

If this works, the server is ready for Claude Code.

## MCP Server Features

The DungeonFighter MCP server provides comprehensive tools for:

### Game Control
- `start_new_game` - Start a new game instance
- `save_game` - Save the current game state
- `handle_input` - Handle game input (menu selection, combat actions)
- `get_available_actions` - Get available actions for current state

### Information Retrieval
- `get_game_state` - Get comprehensive game state snapshot
- `get_player_stats` - Get player character statistics
- `get_current_dungeon` - Get current dungeon information
- `get_inventory` - Get player's inventory
- `get_combat_state` - Get current combat information
- `get_recent_output` - Get recent game output/messages

### Balance Simulation & Analysis
- `run_battle_simulation` - Run comprehensive battle simulations
- `analyze_battle_results` - Analyze simulation results
- `validate_balance` - Validate balance against target metrics
- `analyze_fun_moments` - Analyze fun moment data
- `suggest_tuning` - Get automated tuning suggestions

### Balance Adjustment
- `adjust_global_enemy_multiplier` - Adjust enemy multipliers
- `adjust_archetype` - Adjust archetype stats
- `adjust_weapon_scaling` - Adjust weapon scaling
- `apply_preset` - Apply quick presets
- `save_configuration` - Save current configuration

### Variable Editor
- `list_variables` - List all editable variables
- `get_variable` - Get variable value
- `set_variable` - Set variable value
- `save_variable_changes` - Save variable changes

See `MCP_BALANCE_SIMULATION_TOOLS.md` for detailed documentation on balance simulation and adjustment tools.

## Example Usage with Claude Code

Once set up, you can interact with the game like this:

**You**: "Start a new DungeonFighter game"

**Claude Code**: [Uses `start_new_game` tool, returns game state]

**You**: "What's my current health and level?"

**Claude Code**: [Uses `get_player_stats` tool, tells you your stats]

**You**: "Run a battle simulation to test balance"

**Claude Code**: [Uses `run_battle_simulation` tool, analyzes results]

**You**: "Adjust enemy health multiplier to 1.2"

**Claude Code**: [Uses `adjust_global_enemy_multiplier` tool, applies change]

## Differences from Claude Desktop

The MCP server implementation is **identical** for both Claude Desktop and Claude Code:
- Same stdio transport protocol
- Same tool registration system
- Same MCP SDK version

The only difference is the **configuration file location**:
- **Claude Desktop**: Uses `claude_desktop_config.json` in app data directory
- **Claude Code**: May use `.mcp.json` in project or IDE-specific location

## Next Steps

After setup:
1. Test basic game operations
2. Try different tools
3. Play through a full combat encounter
4. Experiment with balance simulation tools
5. Use automated tuning suggestions
6. Explore variable editor capabilities

## Additional Resources

- `MCP_CLAUDE_DESKTOP_SETUP.md` - Setup guide for Claude Desktop (similar process)
- `MCP_BALANCE_SIMULATION_TOOLS.md` - Detailed balance simulation documentation
- `MCP_SERVER.md` - Technical details about the MCP server implementation
- `MCP_QUICK_SETUP.md` - Quick reference guide

## Support

If you encounter issues:
1. Check the troubleshooting section above
2. Verify your configuration file syntax (valid JSON)
3. Test the server manually (see "Testing the Server Manually")
4. Check IDE logs for MCP-related errors
5. Ensure .NET 8.0 SDK is installed and accessible

Enjoy using DungeonFighter with Claude Code!
