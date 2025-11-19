# Menu Input Debugging Guide

## Current Status
- ✅ H (help) key works
- ❌ Menu keys (1, 2, 3, 0) don't work

This guide will help diagnose where the input is being lost.

## What I've Done
Added debug logging to trace the input flow:

1. **Game.cs** - Logs when HandleInput is called with:
   - Input value
   - Current game state
   - Whether mainMenuHandler is initialized
   
2. **MainMenuHandler.cs** - Logs when input reaches the handler with:
   - Raw input value
   - Trimmed input value
   - Which case (1, 2, 3, 0) is being processed

## How to Check the Debug Logs

### Step 1: Look for the Debug File
After running the game and trying to press menu buttons, look for:

```
Code/DebugAnalysis/debug_analysis_YYYY-MM-DD_HH-mm-ss.txt
```

### Step 2: What to Look For

**Successful Input Flow** would look like:
```
DEBUG [Game]: HandleInput: input='1', state=MainMenu, mainMenuHandler=True
DEBUG [Game]: Routing to MainMenuHandler.HandleMenuInput('1')
DEBUG [MainMenuHandler]: HandleMenuInput: raw='1', trimmed='1'
DEBUG [MainMenuHandler]: Processing 'New Game' (1)
```

**Problem Scenario 1** - Input not reaching Game:
```
(No debug output when you press 1, 2, 3, or 0)
```
→ Issue is in MainWindow input capture or ConvertKeyToInput

**Problem Scenario 2** - State is wrong:
```
DEBUG [Game]: HandleInput: input='1', state=UnexpectedState, mainMenuHandler=True
```
→ Current state is not MainMenu when it should be

**Problem Scenario 3** - Handler is null:
```
DEBUG [Game]: HandleInput: input='1', state=MainMenu, mainMenuHandler=False
```
→ mainMenuHandler failed to initialize

**Problem Scenario 4** - Invalid choice message:
```
DEBUG [MainMenuHandler]: Invalid choice 'something'
```
→ Input value is not "1", "2", "3", or "0" (maybe it's "enter" or something else)

## Run Game and Test

1. Start the game
2. Try pressing: 1, 2, 3, 0
3. Note which ones work/don't work
4. Close the game
5. Check the debug file in `Code/DebugAnalysis/` folder
6. Share the relevant debug output lines

## Expected Input Flow

```
MainWindow.axaml.cs (OnKeyDown)
  ↓
if (e.Key == Key.D1)  // Check key code
  ↓
ConvertKeyToInput returns "1"
  ↓
game.HandleInput("1")
  ↓
Game.cs HandleInput() 
  ├─ Check state = MainMenu ✓
  ├─ Call mainMenuHandler.HandleMenuInput("1")
  │
  └─→ MainMenuHandler.HandleMenuInput()
      ├─ Trim input = "1"
      ├─ Match case "1"
      └─ Call StartNewGame()
```

## Key Questions to Answer

1. Does the debug log show ANY entries for menu key presses?
2. If yes, what values are being logged?
3. What is the current game state when menu keys are pressed?
4. Is mainMenuHandler initialized (True/False)?
5. Are the input strings matching the switch cases?

## Possible Root Causes

1. **Key Not Mapped** - The key might not be D1, D2, etc.
   - Solution: Check what key code is actually being sent
   
2. **State Not MainMenu** - Game state might be something else
   - Solution: Check what state is set when menu appears
   
3. **Input String Wrong** - The string might be "1 " (with space) or something else
   - Solution: Check raw and trimmed input in logs
   
4. **Handler Not Initialized** - mainMenuHandler might be null
   - Solution: Check why initialization failed
   
5. **Exception Silently Caught** - An exception might be thrown and caught
   - Solution: Add more error handling

## Next Steps

1. Run the game
2. Check the debug file
3. Share the debug output that shows what's happening when you press 1, 2, 3, 0
4. We can then pinpoint exactly where the input is being lost

Debug is enabled in `GameData/TuningConfig.json`:
```json
"Debug": {
  "EnableDebugOutput": true
}
```

The debug logs will be created in `Code/DebugAnalysis/` directory with timestamps.

