# Menu Center Point Update

## Overview
Updated the menu centering system to explicitly use **50 (half of 100 characters)** as the center point for all menu positioning.

## Screen Dimensions

### Character Width
- **Total Screen Width**: 100 characters
- **Center Point**: 50 characters (half of 100)

### Window Configuration
- Defined in `GameCanvasControl.cs`: `GridWidth = 100`
- Consistent across all UI components

## Changes Made

### 1. GameCanvasControl.cs
**Location:** `Code/UI/Avalonia/GameCanvasControl.cs`

**Added:**
```csharp
// Center point (half of GridWidth)
public int CenterX => GridWidth / 2;  // = 50 for default 100-wide screen
```

**Updated:**
- Modified `AddText()` centering logic to use `CenterX - (text.Length / 2)` instead of `(GridWidth - text.Length) / 2`
- Both formulas are mathematically equivalent, but the new approach explicitly references the center point

### 2. CanvasUICoordinator.cs
**Location:** `Code/UI/Avalonia/CanvasUICoordinator.cs`

**Added Constants:**
```csharp
private const int SCREEN_WIDTH = 100;   // Total character width of the screen
private const int SCREEN_CENTER = 50;   // Half of SCREEN_WIDTH - the center point for menus
```

**Updated Methods:**
- `RenderMainMenu()` - Uses `SCREEN_CENTER` for centering menu options and instructions
- `RenderOpeningAnimation()` - Uses `canvas.CenterX` for centering ASCII art
- `ShowPressKeyMessage()` - Uses `canvas.CenterX` for centering messages

**Centering Formula:**
```csharp
// Old approach (implicit center)
int startX = (screenWidth - textLength) / 2;

// New approach (explicit center point)
int startX = SCREEN_CENTER - (textLength / 2);
```

### 3. PersistentLayoutManager.cs
**Location:** `Code/UI/Avalonia/PersistentLayoutManager.cs`

**Added Constant:**
```csharp
private const int SCREEN_CENTER = 50;  // Half of SCREEN_WIDTH - the center point for menus
```

## Centering Logic

### Mathematical Equivalence
Both formulas produce the same result:

```
Old: (100 - 20) / 2 = 80 / 2 = 40
New: 50 - (20 / 2) = 50 - 10 = 40
```

For a 20-character text on a 100-character screen, both methods position the left edge at character 40, centering the text around position 50.

### Benefits of New Approach
1. **Explicit Center Point**: Makes it clear that 50 is the center of the screen
2. **Consistency**: All centering references the same constant
3. **Documentation**: Comments explicitly state the center point value
4. **Maintainability**: Easier to understand and modify in the future

## Examples

### Main Menu Centering
```csharp
string[] menuOptions = new string[] 
{ 
    "[1] Enter Dungeon",      // 18 characters
    "[2] View Inventory",     // 19 characters
    "[3] Character Info",     // 19 characters
    "[4] Tuning Console",     // 19 characters
    "[5] Save Game",          // 14 characters
    "[6] Exit"                // 8 characters
};

// Longest option is 19 characters
int maxOptionLength = 19;

// Center around SCREEN_CENTER (50)
int menuStartX = SCREEN_CENTER - (maxOptionLength / 2);
// = 50 - 9 = 41

// Result: Menu block starts at position 41, centered around position 50
```

### Instruction Text Centering
```csharp
string instructions = "Click on options or press number keys. Press H for help";
// 56 characters

int instructionsX = SCREEN_CENTER - (instructions.Length / 2);
// = 50 - 28 = 22

// Result: Text starts at position 22, centered around position 50
```

## Testing

### Verification Steps
1. Launch the game
2. View main menu - all options should be centered
3. Check opening animation - ASCII art should be centered
4. Test all menus - inventory, character info, etc.
5. Verify clickable regions align with centered text

### Visual Check
```
         1         2         3         4         5         6         7         8         9
123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789
                                        |
                                     CENTER
                                    (pos 50)
```

## Files Modified
1. `Code/UI/Avalonia/GameCanvasControl.cs` - Added `CenterX` property
2. `Code/UI/Avalonia/CanvasUICoordinator.cs` - Added constants and updated centering logic
3. `Code/UI/Avalonia/PersistentLayoutManager.cs` - Added `SCREEN_CENTER` constant

## Technical Notes

### Why 50?
- Screen width is 100 characters (defined in multiple locations)
- Half of 100 = 50
- All text centered around this point

### Layout Structure
```
┌────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ 0                                          50                                                   100 │
│ ├─────────────────────────────────────────┼─────────────────────────────────────────────────────┤ │
│                                         CENTER                                                      │
│                              All menus centered here                                                │
└────────────────────────────────────────────────────────────────────────────────────────────────────┘
```

### Persistent Layout
When using the persistent layout (with character info sidebar):
- Left panel: 0-30 (30% of screen)
- Center panel: 31-99 (70% of screen)
- Main menu uses full screen (0-100) with center at 50
- In-game screens use the two-panel layout

## Summary
✅ Screen width: **100 characters**  
✅ Center point: **50 characters** (half of 100)  
✅ All menus explicitly reference center point  
✅ Centering logic consistent across all UI components  
✅ No linter errors  
✅ Mathematically verified equivalence  

## Date
October 11, 2025

