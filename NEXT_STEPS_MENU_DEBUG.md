# Next Steps - Menu Input Debugging

## What I've Done ✅

I've added comprehensive debugging to trace where the menu input (1, 2, 3, 0) is being lost.

**Files Modified:**
1. ✅ `Code/Game/Game.cs` - Traces input at the routing level
2. ✅ `Code/Game/MainMenuHandler.cs` - Traces input at the menu processing level
3. ✅ `Code/UI/Avalonia/MainWindow.axaml.cs` - Added focus management to ensure keyboard events are captured
4. ✅ Created debugging documentation

**Debug Output Enabled:** `GameData/TuningConfig.json` - Already set to `EnableDebugOutput: true`

---

## What You Need to Do 🎯

### Step 1: Build the Game
Rebuild the solution so the debug logging code is compiled in.

### Step 2: Run and Test
1. Start the game
2. Wait for the main menu to appear
3. Try pressing these keys and note what happens:
   - `1` - Should start new game (but doesn't?)
   - `2` - Should load game
   - `3` - Should open settings
   - `0` - Should quit
4. Try `H` to verify keyboard input is working (it does work!)
5. **Close the game**

### Step 3: Check the Debug Log
Open: `Code/DebugAnalysis/debug_analysis_[timestamp].txt`

Look for entries like:
```
DEBUG [Game]: HandleInput: input='1', state=MainMenu, mainMenuHandler=True
DEBUG [MainMenuHandler]: HandleMenuInput: raw='1', trimmed='1'
DEBUG [MainMenuHandler]: Processing 'New Game' (1)
```

### Step 4: Share the Debug Output
Tell me:
- **Copy/paste the relevant debug lines** when you press menu keys
- **What state is shown** when the main menu appears
- **What happens with the input** (is it reaching the handlers?)
- **Any error messages** in the debug file

---

## Diagnostic Questions

Based on the debug output, I can determine:

1. ❓ **Is input reaching Game.HandleInput()?**
   - If yes: You'll see `DEBUG [Game]: HandleInput: input='...'`
   - If no: Nothing shows up

2. ❓ **What is the game state?**
   - Should be: `state=MainMenu`
   - Could be: Something else?

3. ❓ **Is the handler initialized?**
   - Should be: `mainMenuHandler=True`
   - Problem if: `mainMenuHandler=False`

4. ❓ **What's the exact input value?**
   - Should be: `'1'`, `'2'`, `'3'`, or `'0'`
   - Could be: `'1 '` (with space), `'enter'`, or something else?

5. ❓ **Which case is it matching?**
   - You'll see: `Processing 'New Game' (1)` or similar
   - Or: `Invalid choice 'something'`

---

## Why This Works

- **H works** → Keyboard input IS being captured by the window ✓
- **Menu keys don't** → The input IS reaching the system, but something in the routing breaks
- **The debug logs will show exactly WHERE the break is** 🔍

---

## Possible Root Causes (in order of likelihood)

1. **Whitespace Issue** - Input is `'1 '` or `' 1'` instead of `'1'`
   - Fix: The code already trims, but we need to confirm

2. **Wrong Game State** - State is not `MainMenu` when menu appears
   - Fix: Check when state is set

3. **Null Handler** - mainMenuHandler not initialized
   - Fix: Check initialization in Game constructor

4. **Key Code Issue** - Keys aren't being converted to "1", "2", etc.
   - Fix: Check ConvertKeyToInput function

5. **Focus Issue** - Window doesn't have keyboard focus
   - Fix: I added focus management code

---

## Quick Command Cheat Sheet

```bash
# Build
dotnet build

# Run
dotnet run

# Check debug files
dir Code\DebugAnalysis\
# or
Get-ChildItem Code\DebugAnalysis\ -Recurse
```

---

## What Happens Next

Once you share the debug output:
1. ✅ I'll see exactly where the input is being lost
2. ✅ I can pinpoint the root cause
3. ✅ I can make a targeted fix
4. ✅ Menu keys will work again

**This is the fastest way to solve the problem!** 🚀

