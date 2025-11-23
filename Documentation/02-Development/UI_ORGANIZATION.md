# UI Organization and Structure

## Overview
This document describes the organization of the UI system after the rewrite to consolidate code and improve maintainability.

## File Structure

### Core UI Files
- **`Code/UI/Avalonia/AsciiArtAssets.cs`** - Centralized UI assets and constants
- **`Code/UI/Avalonia/CanvasUICoordinator.cs`** - Main UI rendering coordinator (refactored from CanvasUIManager)
- **`Code/UI/Avalonia/PersistentLayoutManager.cs`** - Handles persistent left panel layout
- **`Code/UI/Avalonia/GameCanvasControl.cs`** - Low-level canvas rendering
- **`Code/UI/OpeningAnimation.cs`** - Opening title screen animation

## AsciiArtAssets.cs Structure

### Purpose
Centralized location for all UI text, ASCII art, colors, and icons used throughout the game.

### Organization

#### 1. **Colors** (`AsciiArtAssets.Colors`)
Standard color palette used throughout the UI:
- Basic colors: White, Gray, Black, Red, Green, Blue, Yellow, etc.
- Special colors: Gold, Silver, Bronze, Cyan, Magenta
- Rarity colors: Common, Uncommon, Rare, Epic, Legendary, Mythic

#### 2. **Icons** (Multiple nested classes)
- **EquipmentIcons**: Sword, Shield, Bow, Wand, Armor, etc.
- **StatusIcons**: Burn, Freeze, Poison, Stun, Bleed, Heal, etc.
- **UIElements**: Borders, progress bars, arrows, checkmarks
- **CombatIcons**: Player, Enemy, Damage, Critical, Miss, Victory, Defeat
- **DungeonIcons**: Room, Door, Chest, Trap, Portal, etc.

#### 3. **TitleArt** (`AsciiArtAssets.TitleArt`)
```csharp
AsciiArtAssets.TitleArt.DungeonFighterTitle
```
- Contains the main "DUNGEON FIGHTER" title screen ASCII art
- 75 characters wide (including border)
- Uses Unicode block characters (█) and box-drawing characters (╔═╗║)
- Centralized so changes update both Avalonia and Console UIs

#### 4. **UIText** (`AsciiArtAssets.UIText`)
Centralized text constants for consistent messaging:

**Header decorations:**
- `HeaderPrefix` / `HeaderSuffix`: "═══"
- `Divider`: "===================================="
- `CreateHeader(text)`: Creates formatted headers

**Combat messages:**
- `CombatLogHeader`: "COMBAT LOG"
- `BattleCompleteHeader`: "BATTLE COMPLETE"
- `BattleHighlightsHeader`: "BATTLE HIGHLIGHTS"

**Room messages:**
- `EnteringDungeonHeader`: "ENTERING DUNGEON"
- `EnteringRoomHeader`: "ENTERING ROOM"
- `RoomClearedMessage`: "Room cleared!"

**Victory/Defeat:**
- `VictoryPrefix`: "[{0}] has been defeated!"
- `DefeatMessage`: "You have been defeated!"

**Format helpers:**
- `FormatEnemyStats(currentHealth, maxHealth, armor)`
- `FormatEnemyAttack(str, agi, tec, intel)`

## Usage Examples

### Using Title Art
```csharp
// Instead of hardcoding ASCII art
string[] asciiArt = AsciiArtAssets.TitleArt.DungeonFighterTitle;
```

### Using Text Constants
```csharp
// Instead of: "═══ ENTERING DUNGEON ═══"
string header = AsciiArtAssets.UIText.CreateHeader(AsciiArtAssets.UIText.EnteringDungeonHeader);

// Instead of: $"[{enemy.Name}] has been defeated!"
string message = string.Format(AsciiArtAssets.UIText.VictoryPrefix, enemy.Name);

// Instead of: $"Enemy Stats - Health: {hp}/{max}, Armor: {armor}"
string stats = AsciiArtAssets.UIText.FormatEnemyStats(currentHealth, maxHealth, armor);
```

### Using Colors
```csharp
canvas.AddText(x, y, message, AsciiArtAssets.Colors.Gold);
```

### Using Icons
```csharp
string enemyIcon = AsciiArtAssets.CombatIcons.Enemy; // 👹
string weaponIcon = AsciiArtAssets.GetWeaponIcon("sword"); // ⚔
```

## CanvasUICoordinator.cs Organization

### Purpose
Main UI rendering manager that handles all game screens using the persistent layout system.

### Method Categories

#### Combat Rendering
- `RenderCombat()` - Main combat screen with log
- `RenderCombatContent()` - Combat log display
- `RenderCombatResult()` - Victory/defeat screen
- `RenderCombatResultContent()` - Combat results

#### Dungeon/Room Rendering
- `RenderDungeonStart()` - Dungeon entry (deprecated, now uses log)
- `RenderRoomEntry()` - Room entry (deprecated, now uses log)
- `RenderEnemyEncounter()` - Shows dungeon context before combat
- `RenderEnemyEncounterContent()` - Accumulated dungeon/room/enemy info
- `RenderRoomCompletion()` - Room cleared message
- `RenderRoomCompletionContent()` - Room completion display
- `RenderDungeonCompletion()` - Dungeon finished

#### Menu Rendering
- `RenderMainMenu()` - Main game menu
- `RenderHelp()` - Help screen
- `RenderCharacterCreation()` - Character creation flow

#### Display Buffer Management
- `RenderDisplayBuffer()` - Renders the combat log with persistent layout
- `AddToDisplayBuffer()` - Adds lines to display
- `ResetForNewBattle()` - Clears combat log but preserves dungeon context
- `SetDungeonContext()` - Stores dungeon/room info for persistence
- `SetCurrentEnemy()` - Tracks enemy for left panel health bar

### Key Features

#### Dungeon Context Persistence
```csharp
// Stores dungeon/room/enemy info that persists during combat
private List<string> dungeonContext = new List<string>();

// Set before combat starts
canvasUI.SetDungeonContext(dungeonLog);

// Preserved when combat log is cleared
canvasUI.ResetForNewBattle(); // Adds dungeonContext back to display
```

#### Enemy Health Bar Persistence
```csharp
// Track enemy for left panel display
private Enemy? currentEnemy = null;

// Set before combat
canvasUI.SetCurrentEnemy(enemy);

// Passed to layout manager during rendering
layoutManager.RenderLayout(currentCharacter, contentCallback, title, currentEnemy);

// Clear after combat
canvasUI.ClearCurrentEnemy();
```

## Benefits of This Organization

### 1. **Single Source of Truth**
- All text messages defined once in `AsciiArtAssets.UIText`
- Changes propagate throughout the entire game
- No more hunting for hardcoded strings

### 2. **Easy Localization**
- All text constants in one place
- Future localization only needs to modify `AsciiArtAssets.UIText`

### 3. **Consistency**
- Same title art used in both Avalonia and Console UIs
- Same message formats across all screens
- Uniform color scheme

### 4. **Maintainability**
- Clear separation of concerns
- Well-documented methods with XML comments
- Easy to find and modify specific screens

### 5. **Type Safety**
- Using constants prevents typos
- Format helpers ensure correct parameter count
- Compile-time checking instead of runtime errors

## Combat Flow

### Before (3 Separate Screens)
1. Dungeon Start → 2s delay
2. Room Entry → 3s delay
3. Enemy Encounter → 2s delay
4. Combat begins

**Total: 7 seconds + 3 screen changes**

### After (Flowing Information)
All information accumulates in one display:
- Dungeon info
- Room description
- Enemy encounter
- Then combat log scrolls below

**Total: 1.5 seconds + 1 screen**

Enemy health bar stays visible on the left throughout!

## Future Improvements

1. **Add More Format Helpers**
   - Character stat displays
   - Item descriptions
   - Damage numbers

2. **Centralize More UI Elements**
   - Menu layouts
   - Box drawing styles
   - Animation timings

3. **Theme Support**
   - Color scheme variations
   - Dark/light mode
   - Accessibility options

4. **Localization System**
   - Language file loading
   - Format string translation
   - RTL support

## Related Documentation
- See `ARCHITECTURE.md` for overall system design
- See `CODE_PATTERNS.md` for coding conventions
- See `PERSISTENT_LAYOUT.md` for layout system details

