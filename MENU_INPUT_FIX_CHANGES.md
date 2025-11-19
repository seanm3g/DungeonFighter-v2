# Menu Input Fix - Changes Made

## Summary
You reported that H (help) key works, but menu keys (1, 2, 3, 0) don't work. I've added comprehensive debugging and made improvements to help diagnose and fix the issue.

## Changes Made

### 1. **Code/Game/Game.cs** - Input Handler Improvements
**Purpose**: Trace input flow through the main input handler

```csharp
// Before:
if (mainMenuHandler == null || inventoryMenuHandler == null) return;

// After:
if (string.IsNullOrEmpty(input)) return;

// Added debug logging:
DebugLogger.Log("Game", $"HandleInput: input='{input}', state={stateManager.CurrentState}, mainMenuHandler={mainMenuHandler != null}");

// When routing to MainMenu:
if (mainMenuHandler != null)
{
    DebugLogger.Log("Game", $"Routing to MainMenuHandler.HandleMenuInput('{input}')");
    await mainMenuHandler.HandleMenuInput(input);
}
else
{
    DebugLogger.Log("Game", "ERROR: mainMenuHandler is null!");
}
```

**What this does**:
- Logs whether input reaches the Game.HandleInput method
- Shows current game state
- Shows whether mainMenuHandler is initialized
- Logs the routing decision

### 2. **Code/Game/MainMenuHandler.cs** - Input Processing Tracing
**Purpose**: Trace what happens inside the main menu input handler

```csharp
// Added at the start of HandleMenuInput:
DebugLogger.Log("MainMenuHandler", $"HandleMenuInput: raw='{input}', trimmed='{trimmedInput}'");

// Added in each case:
case "1":
    DebugLogger.Log("MainMenuHandler", "Processing 'New Game' (1)");
    await StartNewGame();
    break;
// ... similar for other cases ...

// Added in default case:
default:
    DebugLogger.Log("MainMenuHandler", $"Invalid choice '{trimmedInput}'");
    ...
```

**What this does**:
- Logs raw input vs trimmed input (to catch whitespace issues)
- Logs which case is being processed
- Logs invalid inputs that don't match any case

### 3. **Code/UI/Avalonia/MainWindow.axaml.cs** - Focus Management
**Purpose**: Ensure the window has keyboard focus for input capture

```csharp
// Added to constructor:
this.Opened += (s, e) =>
{
    this.Focus();
    GameCanvas.Focus();
};
```

**What this does**:
- Ensures the MainWindow gets focus when opened
- Ensures the GameCanvas gets focus for keyboard input
- Prevents other UI elements from stealing focus

## Debug Output Location

After running the game and testing menu keys, check:
```
Code/DebugAnalysis/debug_analysis_YYYY-MM-DD_HH-mm-ss.txt
```

## What to Look For in Debug Output

### Successful Flow
```
DEBUG [Game]: HandleInput: input='1', state=MainMenu, mainMenuHandler=True
DEBUG [Game]: Routing to MainMenuHandler.HandleMenuInput('1')
DEBUG [MainMenuHandler]: HandleMenuInput: raw='1', trimmed='1'
DEBUG [MainMenuHandler]: Processing 'New Game' (1)
```

### Troubleshooting Scenarios

**Scenario 1: No debug output when pressing 1, 2, 3, 0**
- Issue: Input not reaching Game.HandleInput
- Check: MainWindow.OnKeyDown, ConvertKeyToInput

**Scenario 2: Debug shows wrong state**
- Example: `state=UnexpectedState instead of MainMenu`
- Issue: Game state not set correctly
- Check: MainMenuHandler.ShowMainMenu()

**Scenario 3: mainMenuHandler=False**
- Issue: Handler not initialized
- Check: Game constructor InitializeHandlers()

**Scenario 4: Invalid choice message**
- Example: `Invalid choice 'something'`
- Issue: Input string doesn't match "1", "2", "3", "0"
- Check: What the actual string value is

## Testing Steps

1. **Run the game**
2. **Wait for main menu to appear**
3. **Try each key**: 1, 2, 3, 0
4. **Close the game**
5. **Check debug file** in `Code/DebugAnalysis/`
6. **Share the relevant debug lines**

## Configuration

Debug is **enabled by default** in:
```
GameData/TuningConfig.json
```

```json
"Debug": {
  "EnableDebugOutput": true,
  "ShowCombatSimulationDebug": false,
  "LogCombatActions": false
}
```

## Files Modified

1. `Code/Game/Game.cs` - Input routing with debug logging
2. `Code/Game/MainMenuHandler.cs` - Menu input processing with debug logging  
3. `Code/UI/Avalonia/MainWindow.axaml.cs` - Focus management

## Next Steps

Once you run the game and check the debug output, we can determine:
- Where the input is being lost
- Why menu keys don't work while H works
- The exact value of the input strings
- The game state when menu is shown

Share the debug file contents and we can fix this conclusively!

