# MCP Server for DungeonFighter

This document describes the Model Context Protocol (MCP) server implementation for DungeonFighter, which allows AI assistants to interact with and play the game.

## Overview

The MCP server exposes game functionality as tools that AI assistants can use to:
- Query game state (player stats, current dungeon, combat status)
- Execute game actions (handle input, navigate menus, perform combat actions)
- Get game output and messages
- Control game flow (start new game, save, load)

### Token Usage Optimization

The MCP server has been optimized to minimize token usage:
- **Compact JSON**: All responses use compact JSON (no indentation) to reduce token count
- **Reduced Buffer**: Output buffer limited to 200 messages (down from 1000)
- **Excluded Recent Output**: Game state snapshots exclude recent output by default (use `get_recent_output` tool separately if needed)
- **Message Truncation**: Messages longer than 500 characters are truncated
- **Blank Line Filtering**: Blank lines are filtered out to save tokens
- **Limited Defaults**: `get_recent_output` defaults to 10 messages (max 100) instead of 50

These optimizations can reduce token usage by **60-80%** compared to the original implementation.

## Architecture

The MCP server consists of several components:

```
Code/MCP/
├── OutputCapture.cs          # Custom IUIManager that captures game output
├── GameWrapper.cs            # Wraps game instance and provides clean API
├── GameStateSerializer.cs    # Serializes game state to JSON
├── DungeonFighterMCPServer.cs # Main MCP server class
├── MCPServerProgram.cs       # Entry point for MCP server
├── McpTools.cs               # MCP tool definitions with attributes
└── Models/
    └── GameStateSnapshot.cs  # Serializable game state models
```

### Component Details

1. **OutputCapture**: Implements `IUIManager` to capture all game messages/output for AI context. Stores messages in a thread-safe buffer (max 200 messages, optimized for token efficiency). Filters blank lines and truncates messages over 500 characters.

2. **GameWrapper**: Manages headless game instance and provides methods for MCP tools. Handles game initialization, input processing, and state retrieval.

3. **GameStateSerializer**: Converts game objects to JSON-serializable snapshots. Extracts player stats, dungeon info, combat state, and available actions. Returns compact JSON (no indentation) and excludes recent output by default to minimize token usage.

4. **DungeonFighterMCPServer**: Main server class that uses Microsoft.Extensions.Hosting to set up the MCP server with stdio transport. Automatically discovers tools from the assembly using `WithToolsFromAssembly()`.

5. **McpTools**: Static class containing all MCP tool methods, decorated with `[McpServerTool]` attributes. Tools are automatically discovered and registered by the MCP SDK.

6. **MCPServerProgram**: Entry point that can be called from the main `Program.cs` when "MCP" argument is provided. Handles graceful shutdown on Ctrl+C.

### Integration with Main Program

The MCP server is integrated into the main game entry point (`Code/Game/Program.cs`). When the program is run with the "MCP" argument, it calls `MCPServerProgram.Main()` instead of launching the Avalonia GUI:

```csharp
if (args.Length > 0 && args[0] == "MCP")
{
    RPGGame.MCP.MCPServerProgram.Main(args).Wait();
    return;
}
```

This allows the same executable to serve both as the GUI game and the MCP server.

## Setup

### Prerequisites

1. .NET 8.0 SDK
2. Microsoft MCP C# SDK (NuGet package - see below)

### Installation Steps

1. **Install MCP SDK Package**

   The package is already added to `Code.csproj`:
   
   ```xml
   <PackageReference Include="ModelContextProtocol" Version="0.5.0-preview.1" />
   <PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
   ```
   
   If you need to install manually:
   ```bash
   dotnet add package ModelContextProtocol --version 0.5.0-preview.1
   dotnet add package Microsoft.Extensions.Hosting --version 8.0.0
   ```

2. **Restore Packages**

   ```bash
   dotnet restore
   ```

3. **Build the Project**

   ```bash
   dotnet build
   ```

## Usage

### Running the MCP Server

The MCP server runs as a separate process and communicates via stdio:

```bash
dotnet run --project Code/Code.csproj -- MCP
```

Or create a separate executable entry point.

### Connecting AI Assistants

Configure your AI assistant (Claude Desktop, ChatGPT, etc.) to use the MCP server:

**Claude Desktop Configuration** (`claude_desktop_config.json`):
```json
{
  "mcpServers": {
    "dungeonfighter": {
      "command": "dotnet",
      "args": ["run", "--project", "path/to/Code/Code.csproj", "--", "MCP"]
    }
  }
}
```

## Available MCP Tools

The following tools are currently implemented and available for use:

### Game Control Tools

#### `start_new_game`
Starts a new game instance.

**Parameters**: None

**Returns**: Game state snapshot

#### `save_game`
Saves the current game.

**Parameters**: None

**Returns**: Success status

#### `load_game` ⚠️ Not Yet Implemented
Loads an existing saved game. (Currently not implemented - will be added in future update)

**Parameters**: None

**Returns**: Game state snapshot

### Navigation Tools

#### `handle_input`
Handles game input (menu selection, combat actions, etc.).

**Parameters**:
- `input` (string): The input to send to the game

**Returns**: Updated game state snapshot

#### `get_available_actions`
Gets available actions for the current game state.

**Parameters**: None

**Returns**: List of available action identifiers

### Information Tools

#### `get_game_state`
Gets comprehensive game state snapshot.

**Parameters**: None

**Returns**: Complete game state including player, dungeon, room, combat status

#### `get_player_stats`
Gets player character statistics.

**Parameters**: None

**Returns**: Player stats (health, level, XP, attributes, equipment)

#### `get_current_dungeon`
Gets current dungeon information.

**Parameters**: None

**Returns**: Dungeon details (name, level, theme, rooms)

#### `get_inventory`
Gets player inventory.

**Parameters**: None

**Returns**: List of items in inventory

#### `get_combat_state`
Gets current combat information (if in combat).

**Parameters**: None

**Returns**: Combat state (current enemy, available actions, turn status)

#### `get_recent_output`
Gets recent game output/messages. **Note**: Recent output is excluded from game state snapshots by default to save tokens. Use this tool separately when you need output context.

**Parameters**:
- `count` (int, optional): Number of messages to retrieve (default: 10, max: 100)

**Returns**: List of recent game messages

**Token Optimization**: Default reduced from 50 to 10 messages. Maximum capped at 100 to prevent excessive token usage.

### Dungeon Tools

#### `select_dungeon` ⚠️ Not Yet Implemented
Selects a dungeon to enter. (Currently not implemented - use `handle_input` with dungeon number instead)

**Parameters**:
- `dungeon_index` (int): Index of dungeon to select (1-based)

**Returns**: Updated game state

**Note**: For now, use `handle_input` with the dungeon number (e.g., "1", "2") to select a dungeon.

#### `get_available_dungeons` ⚠️ Partially Implemented
Lists available dungeons. (Currently returns a placeholder message - implementation in progress)

**Parameters**: None

**Returns**: List of available dungeons with details

**Note**: This tool currently returns a "not yet implemented" message. Use `get_game_state` to check dungeon information from the game state.

**Note**: To select a dungeon, use `handle_input` with the dungeon number (e.g., "1", "2") when in the dungeon selection menu.

---

## Complete Tool List Summary

### ✅ Fully Implemented Tools (10)

1. `start_new_game` - Starts a new game instance
2. `save_game` - Saves the current game
3. `handle_input` - Handles game input (menu selection, combat actions)
4. `get_available_actions` - Gets available actions for current state
5. `get_game_state` - Gets comprehensive game state snapshot
6. `get_player_stats` - Gets player character statistics
7. `get_current_dungeon` - Gets current dungeon information
8. `get_inventory` - Gets player inventory items
9. `get_combat_state` - Gets current combat information
10. `get_recent_output` - Gets recent game output/messages

### ⚠️ Partially Implemented / Not Implemented (3)

1. `load_game` - Not yet implemented (documented but missing from `McpTools.cs`)
2. `select_dungeon` - Not yet implemented (use `handle_input` instead)
3. `get_available_dungeons` - Returns placeholder message

## Game State Snapshot Structure

The game state snapshot includes:

```json
{"currentState":"Combat","player":{"name":"Player Name","level":5,"xp":150,"currentHealth":45,"maxHealth":60,"healthPercentage":0.75,"strength":12,"agility":10,"technique":8,"intelligence":6,"weapon":{...},"armor":{...},"inventory":[...],"comboStep":2},"currentDungeon":{...},"currentRoom":{...},"availableActions":["1","2","3"],"recentOutput":[],"combat":{"currentEnemy":{...},"availableCombatActions":["1","2"],"isPlayerTurn":true}}
```

**Note**: 
- JSON is returned in compact format (no indentation) to minimize token usage
- `recentOutput` is excluded by default (empty array) to save tokens. Use `get_recent_output` tool separately if you need output context.

## Implementation Status

### ✅ Completed

- Output capture system (`OutputCapture.cs`)
- Game wrapper (`GameWrapper.cs`)
- State serialization (`GameStateSerializer.cs`)
- Game state models (`GameStateSnapshot.cs`)
- MCP server implementation (`DungeonFighterMCPServer.cs`)
- MCP tools registration (`McpTools.cs`) - Core tools implemented with SDK attributes
- Entry point (`MCPServerProgram.cs`) - Integrated with main Program.cs
- NuGet packages added (`ModelContextProtocol` v0.5.0-preview.1, `Microsoft.Extensions.Hosting` v8.0.0)
- Core game control tools: `start_new_game`, `save_game`
- Navigation tools: `handle_input`, `get_available_actions`
- Information tools: `get_game_state`, `get_player_stats`, `get_current_dungeon`, `get_inventory`, `get_combat_state`, `get_recent_output`

### ⏳ Pending / Partially Implemented

- `load_game` - Not yet implemented (documented but missing from `McpTools.cs`)
- `select_dungeon` - Not yet implemented (documented but missing from `McpTools.cs`)
- `get_available_dungeons` - Partially implemented (returns placeholder message)
- Test with AI assistants (Claude Desktop, ChatGPT, etc.)
- Verify tool registration and parameter schemas
- Test error handling and edge cases
- Complete dungeon selection functionality

## Next Steps

1. **Restore and Build**
   ```bash
   dotnet restore
   dotnet build
   ```

2. **Test the MCP Server**
   - Run the server: `dotnet run -- MCP`
   - Verify it starts without errors
   - Check that tools are registered correctly

3. **Configure AI Assistant**
   - Set up Claude Desktop or other MCP-compatible AI
   - Configure the MCP server connection
   - Test basic tool calls

4. **Testing**
   - Test each tool individually
   - Verify game state serialization
   - Test error cases and edge conditions
   - Test full gameplay flow through AI

5. **Enhancements** (Planned)
   - Implement `load_game` tool
   - Implement `select_dungeon` tool
   - Complete `get_available_dungeons` implementation
   - Add more detailed combat information
   - Add inventory management tools (equip, unequip, use items)

## Troubleshooting

### Server Won't Start

- Verify .NET 8.0 is installed
- Check that MCP SDK package is added
- Ensure all dependencies are restored (`dotnet restore`)

### Tools Not Available

- Verify tool registration in `DungeonFighterMCPServer.cs`
- Check that game is initialized before calling tools
- Review error messages in server output

### Game State Issues

- Ensure `OutputCapture` is set as UI manager
- Verify game state serialization logic
- Check for null references in state snapshot

## References

- [MCP Specification](https://modelcontextprotocol.io)
- [Microsoft MCP C# SDK Documentation](https://modelcontextprotocol.github.io/csharp-sdk/)
- [MCP for Beginners](https://github.com/microsoft/mcp-for-beginners)

