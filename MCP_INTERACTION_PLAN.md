# MCP Server Interaction Plan for Claude Code

## Overview
This plan outlines how to interact directly with the DungeonFighter v2 MCP server to play through complete game sessions. Instead of using stdio transport (which requires an external client), we'll leverage the internal architecture to call tools directly from C#.

## Architecture Analysis

### Current MCP Architecture
```
┌─────────────────────────────────────┐
│     Claude Code / External Client   │
│                                     │
│  (stdio transport via JSON-RPC)     │
└──────────────┬──────────────────────┘
               │
        ┌──────▼──────────┐
        │  MCP Server     │
        │  (stdio)        │
        └──────┬──────────┘
               │
        ┌──────▼──────────────────┐
        │  McpToolState (shared)  │
        ├─────────────────────────┤
        │ - GameWrapper           │
        │ - Test Results          │
        │ - Variable Editor       │
        └──────┬──────────────────┘
               │
        ┌──────▼──────────┐
        │  Tool Layer     │
        │  (100+ tools)   │
        └──────┬──────────┘
               │
        ┌──────▼──────────┐
        │  GameWrapper    │
        │  (headless game)│
        └──────┬──────────┘
               │
        ┌──────▼──────────────┐
        │  GameCoordinator    │
        │  (game logic)       │
        └─────────────────────┘
```

### Key Insight: Direct Tool Access

The MCP server uses `[McpServerTool]` attributes and automatic discovery via `.WithToolsFromAssembly()`. However, the actual tool implementations are accessible as regular C# methods in:

- `McpTools.*` partial classes (wrapper layer)
- `Tools/*.cs` classes (implementation layer)
- `McpToolState` (shared state management)

**This means we can:**
1. Directly instantiate a `GameWrapper`
2. Inject it into `McpToolState`
3. Call tool methods directly without stdio transport
4. Get responses directly without JSON serialization

## Three Interaction Approaches

### Approach 1: Direct Tool Invocation (RECOMMENDED)
**Pros:**
- No external process/stdio needed
- Direct C# method calls
- Faster execution
- Better error handling
- Full control over game state
- Can add breakpoints/debugging

**Cons:**
- Not technically "using the MCP server"
- Bypasses JSON serialization layer
- Internal coupling to tool implementations

**Implementation:**
```csharp
// Initialize state
var gameWrapper = new GameWrapper();
McpToolState.GameWrapper = gameWrapper;

// Call tools directly
await GameControlTools.StartNewGame();
await NavigationTools.HandleInput("1");
var state = await InformationTools.GetGameState();
```

### Approach 2: In-Process MCP Server Simulation
**Pros:**
- Closer to real MCP protocol
- Tests serialization/deserialization
- Better validation of tool contracts
- Mimics external client behavior

**Cons:**
- More complex setup
- Still no true stdio transport
- More error handling needed

**Implementation:**
```csharp
// Start in-process server
var server = new DungeonFighterMCPServer();
await server.RunAsync();

// Simulate JSON-RPC requests
var request = new JsonRpcRequest {
    Id = 1,
    Method = "tools/call",
    Params = new { name = "start_new_game" }
};

// Process and get response
var response = ProcessMcpRequest(request);
```

### Approach 3: Full Subprocess MCP Server
**Pros:**
- True MCP implementation
- Works like production environment
- Real stdio transport
- Complete separation of concerns

**Cons:**
- Complex subprocess management
- Stdio parsing and communication
- Error handling across process boundaries
- Debugging is harder

**Implementation:**
```csharp
// Start subprocess
var process = new Process {
    StartInfo = new ProcessStartInfo {
        FileName = "dotnet",
        Arguments = "run --project Code/Code.csproj -- MCP",
        UseShellExecute = false,
        RedirectStandardInput = true,
        RedirectStandardOutput = true
    }
};

// Send JSON-RPC requests via stdin
// Parse JSON-RPC responses from stdout
```

## Recommended Plan: Hybrid Approach

### Phase 1: Direct Tool Invocation (Immediate)
Create an interactive game player that uses direct tool invocation for fast iteration and testing.

**Files to Create:**
1. `Code/Game/InteractiveMCPGamePlayer.cs` - Main game player
2. `Code/Game/GamePlaySession.cs` - Session management
3. `Code/Game/GamePlayActions.cs` - Action definitions

**Implementation Strategy:**
```csharp
public class InteractiveMCPGamePlayer
{
    private GameWrapper _gameWrapper;

    public async Task PlayInteractiveGame()
    {
        // Initialize
        _gameWrapper = new GameWrapper();
        McpToolState.GameWrapper = _gameWrapper;

        // Start game
        var response = await GameControlTools.StartNewGame();
        Console.WriteLine("Game started!");

        // Game loop
        while (true)
        {
            // Get current state
            var state = await InformationTools.GetGameState();

            // Display state
            DisplayGameState(state);

            // Get available actions
            var actions = await NavigationTools.GetAvailableActions();
            DisplayActions(actions);

            // Get user input or AI decision
            string input = GetNextAction(state, actions);

            // Execute action
            var result = await NavigationTools.HandleInput(input);

            // Check win/lose conditions
            if (IsGameOver(result))
                break;
        }

        // Cleanup
        _gameWrapper.DisposeGame();
    }
}
```

### Phase 2: Automated AI Gameplay (Medium Complexity)
Use direct tool invocation to create an AI player that makes intelligent decisions.

**Implementation:**
- Analyze game state
- Determine optimal action based on rules
- Execute action
- Track statistics and metrics
- Can run multiple game sessions for testing

### Phase 3: MCP Protocol Simulation (Optional)
If full MCP protocol validation is needed, create a wrapper that simulates JSON-RPC.

**Implementation:**
```csharp
public class JsonRpcGameClient
{
    private readonly InteractiveMCPGamePlayer _player;

    public async Task<string> SendJsonRpcRequest(string jsonRequest)
    {
        var request = JsonSerializer.Deserialize<JsonRpcRequest>(jsonRequest);
        var response = await ProcessRequest(request);
        return JsonSerializer.Serialize(response);
    }

    private async Task<JsonRpcResponse> ProcessRequest(JsonRpcRequest request)
    {
        var toolName = request.Params["name"];
        var args = request.Params["arguments"];

        var result = toolName switch
        {
            "start_new_game" => await GameControlTools.StartNewGame(),
            "handle_input" => await NavigationTools.HandleInput((string)args["input"]),
            "get_game_state" => await InformationTools.GetGameState(),
            // ... more tools
        };

        return new JsonRpcResponse { Id = request.Id, Result = result };
    }
}
```

## Step-by-Step Implementation Plan

### Step 1: Create GamePlaySession Manager
**File:** `Code/Game/GamePlaySession.cs`

```csharp
public class GamePlaySession
{
    private GameWrapper _gameWrapper;
    private GameStateSnapshot _currentState;
    private List<string> _actionHistory;
    private int _turnCount;

    public async Task Initialize()
    {
        _gameWrapper = new GameWrapper();
        McpToolState.GameWrapper = _gameWrapper;
        _actionHistory = new List<string>();
        _turnCount = 0;
    }

    public async Task StartNewGame()
    {
        var response = await GameControlTools.StartNewGame();
        _currentState = ParseGameState(response);
    }

    public async Task ExecuteAction(string action)
    {
        _actionHistory.Add(action);
        _turnCount++;

        var response = await NavigationTools.HandleInput(action);
        _currentState = ParseGameState(response);
    }

    public async Task<List<string>> GetAvailableActions()
    {
        return await NavigationTools.GetAvailableActions();
    }

    public GameStateSnapshot CurrentState => _currentState;
    public int TurnCount => _turnCount;
    public List<string> ActionHistory => _actionHistory;

    public void Dispose()
    {
        _gameWrapper?.DisposeGame();
    }
}
```

### Step 2: Create Interactive Game Player
**File:** `Code/Game/InteractiveMCPGamePlayer.cs`

```csharp
public class InteractiveMCPGamePlayer
{
    private GamePlaySession _session;

    public async Task Play()
    {
        _session = new GamePlaySession();
        await _session.Initialize();

        Console.WriteLine("╔════════════════════════════════════╗");
        Console.WriteLine("║  DUNGEON FIGHTER v2 - MCP Player   ║");
        Console.WriteLine("╚════════════════════════════════════╝\n");

        await _session.StartNewGame();

        while (true)
        {
            DisplayCurrentState();
            var actions = await _session.GetAvailableActions();
            DisplayActions(actions);

            string input = GetUserInput();

            if (input.ToLower() == "quit")
                break;

            try
            {
                await _session.ExecuteAction(input);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            Console.WriteLine();
        }

        _session.Dispose();
    }

    private void DisplayCurrentState()
    {
        Console.WriteLine("\n" + new string('═', 40));
        var state = _session.CurrentState;

        Console.WriteLine($"Turn: {_session.TurnCount}");
        Console.WriteLine($"Status: {state.CurrentState}");

        if (state.Player != null)
        {
            Console.WriteLine($"Player: {state.Player.Name} (Level {state.Player.Level})");
            Console.WriteLine($"Health: {state.Player.CurrentHealth}/{state.Player.MaxHealth}");
        }

        if (state.CurrentDungeon != null)
        {
            Console.WriteLine($"Dungeon: {state.CurrentDungeon.Name}");
            Console.WriteLine($"Room: {state.CurrentDungeon.CurrentRoom}/{state.CurrentDungeon.TotalRooms}");
        }

        Console.WriteLine(new string('═', 40));
    }

    private void DisplayActions(List<string> actions)
    {
        Console.WriteLine("\nAvailable Actions:");
        for (int i = 0; i < actions.Count; i++)
        {
            Console.WriteLine($"  {i + 1}) {actions[i]}");
        }
    }

    private string GetUserInput()
    {
        Console.Write("\nEnter action (number or command, 'quit' to exit): ");
        return Console.ReadLine() ?? "";
    }
}
```

### Step 3: Create Automated AI Player
**File:** `Code/Game/AutomatedMCPGamePlayer.cs`

```csharp
public class AutomatedMCPGamePlayer
{
    private GamePlaySession _session;
    private GameAIStrategy _strategy;

    public async Task<GamePlaySessionResult> PlayAutomatedGame(int maxTurns = 500)
    {
        _session = new GamePlaySession();
        await _session.Initialize();
        await _session.StartNewGame();

        var result = new GamePlaySessionResult
        {
            StartTime = DateTime.UtcNow
        };

        while (_session.TurnCount < maxTurns)
        {
            var actions = await _session.GetAvailableActions();
            var state = _session.CurrentState;

            // AI decision making
            var bestAction = _strategy.ChooseBestAction(state, actions);

            await _session.ExecuteAction(bestAction);

            // Check win/lose
            if (IsGameOver(state))
            {
                result.EndTime = DateTime.UtcNow;
                result.TurnsPlayed = _session.TurnCount;
                result.Success = IsPlayerVictory(state);
                break;
            }
        }

        _session.Dispose();
        return result;
    }
}
```

### Step 4: Update Program.cs Entry Point
Add a new mode to support interactive game playing:

```csharp
if (args.Length > 0 && args[0].ToLower() == "play")
{
    var player = new InteractiveMCPGamePlayer();
    await player.Play();
    return;
}
```

## Integration Points

### 1. McpToolState
**Current Implementation:**
```csharp
public static class McpToolState
{
    public static GameWrapper? GameWrapper { get; set; }
    // ... other state
}
```

**How We Use It:**
```csharp
McpToolState.GameWrapper = new GameWrapper();
// Tools automatically access it
```

### 2. Tool Methods
All tool methods follow the pattern:
```csharp
public static Task<string> ToolName(params...)
{
    return McpToolExecutor.ExecuteAsync(async () =>
    {
        var wrapper = McpToolState.GameWrapper;
        // implementation
    });
}
```

**Direct Invocation:**
```csharp
await NavigationTools.HandleInput("1");
// Returns Task<string> with JSON response
```

### 3. Game State Serialization
The tools return JSON-serialized `GameStateSnapshot`:

```csharp
var response = await InformationTools.GetGameState();
// response is a JSON string like:
// {"currentState":"MainMenu","player":null,...}
```

**Parsing:**
```csharp
var snapshot = JsonSerializer.Deserialize<GameStateSnapshot>(response);
```

## Execution Flow for Interactive Gameplay

```
Program.Main("play")
    ↓
InteractiveMCPGamePlayer.Play()
    ↓
GamePlaySession.Initialize()
    ├─ Create GameWrapper
    ├─ Set McpToolState.GameWrapper
    └─ Ready for tool calls
    ↓
GamePlaySession.StartNewGame()
    └─ GameControlTools.StartNewGame()
        └─ wrapper.InitializeGame()
        └─ wrapper.ShowMainMenu()
        └─ Returns JSON state
    ↓
Game Loop:
    ├─ GetCurrentState()
    │   └─ InformationTools.GetGameState()
    │       └─ GameStateSerializer.SerializeGameState()
    │       └─ Returns JSON snapshot
    │
    ├─ DisplayState()
    │   └─ Parse JSON → Display to user
    │
    ├─ GetAvailableActions()
    │   └─ NavigationTools.GetAvailableActions()
    │       └─ wrapper.GetAvailableActions()
    │       └─ Returns list of action strings
    │
    ├─ GetUserInput() / AI Decision
    │   └─ Parse input to action
    │
    ├─ ExecuteAction(action)
    │   └─ NavigationTools.HandleInput(action)
    │       └─ wrapper.HandleInput(action)
    │       └─ Process game logic
    │       └─ Returns updated JSON state
    │
    └─ Repeat until game over
    ↓
Cleanup()
    └─ wrapper.DisposeGame()
```

## Benefits of This Approach

1. **Full MCP Tool Access** - Uses all 100+ existing tools
2. **Direct C# Integration** - No subprocess overhead
3. **Incremental State Snapshots** - Optimized for performance
4. **Output Capture** - Captures all game messages
5. **Clean Abstraction** - GameWrapper handles complexity
6. **Extensibility** - Can add custom tools easily
7. **Testing Ready** - Can run multiple sessions, gather stats
8. **AI Compatible** - Can implement intelligent decision-making

## Files to Create

1. ✅ `Code/Game/GamePlaySession.cs` - Session management
2. ✅ `Code/Game/InteractiveMCPGamePlayer.cs` - Interactive player
3. ✅ `Code/Game/AutomatedMCPGamePlayer.cs` - AI player
4. ✅ `Code/Game/GameAIStrategy.cs` - Decision making
5. ✅ `Code/Game/GamePlaySessionResult.cs` - Result tracking
6. ✅ Update `Code/Game/Program.cs` - Add "play" entry point

## Success Criteria

- [ ] Interactive player runs without errors
- [ ] Can see game state displayed in console
- [ ] User can enter actions and see results
- [ ] Multiple complete game sessions can be played
- [ ] AI player can complete dungeons autonomously
- [ ] Statistics are collected and displayed
- [ ] Proper cleanup on exit

## Next Steps

1. **Implement Phase 1** - Direct tool invocation
2. **Test with Interactive Player** - Manual gameplay
3. **Implement Phase 2** - AI gameplay
4. **Gather Statistics** - Analyze game balance
5. **Optional: Phase 3** - Full MCP protocol simulation

This plan leverages the existing MCP architecture while providing immediate playability and testing capabilities!
