# Persistent Layout System

## Overview

The Persistent Layout System provides a unified, consistent interface where character information is always visible, and only the central content area changes based on the current game phase. This creates a cohesive user experience where players can always see their hero's stats, health, and equipment without needing to switch screens.

## Architecture

### Layout Structure

```
┌─────────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                    DUNGEON FIGHTER v2 - ASCII EDITION                                │
├───────────┬───────────────────────────────────────────────────────────────────────────┬──────────────┤
│           │                                                                            │              │
│  LEFT     │                         CENTER CONTENT                                     │   RIGHT      │
│  PANEL    │                           (74% width)                                      │   PANEL      │
│  (13%)    │                        Dynamic Content Area                                │   (13%)      │
│           │                                                                            │              │
│ ═══HERO═══│  ┌──────────────────────────────────────────────────────────────────┐     │ ═LOCATION════│
│ Name      │  │                                                                  │     │ Dungeon:     │
│ Lvl 5     │  │  Content changes based on game phase:                            │     │ Dark Cave    │
│ Warrior   │  │  - Inventory                                                     │     │              │
│           │  │  - Dungeon Exploration                                           │     │ Room:        │
│ ═HEALTH═══│  │  - Combat                                                        │     │ Entry Hall   │
│ HP:       │  │  - Game Menu                                                     │     │              │
│ ████████  │  │  - Dungeon Selection                                             │     │ ═══ENEMY═════│
│ 150/150   │  │                                                                  │     │ Goblin       │
│           │  │  Actions and options specific to current phase                   │     │ Lvl 3        │
│ ══STATS═══│  │                                                                  │     │ HP:          │
│ STR: 10   │  │                                                                  │     │ ████████     │
│ AGI: 8    │  │  [Wider center panel for better readability]                     │     │ 45/45        │
│ TEC: 7    │  │                                                                  │     │              │
│ INT: 6    │  └──────────────────────────────────────────────────────────────────┘     │              │
│           │                                                                            │              │
│ ═══GEAR═══│                                                                            │              │
│ Weapon:   │                                                                            │              │
│ Steel     │                                                                            │              │
│ Sword     │                                                                            │              │
│           │                                                                            │              │
│ Head:     │                                                                            │              │
│ Iron Helm │                                                                            │              │
│           │                                                                            │              │
│ Body:     │                                                                            │              │
│ Chain     │                                                                            │              │
│ Mail      │                                                                            │              │
│           │                                                                            │              │
│ Feet:     │                                                                            │              │
│ Leather   │                                                                            │              │
│ Boots     │                                                                            │              │
│           │                                                                            │              │
└───────────┴───────────────────────────────────────────────────────────────────────────┴──────────────┘
```

### Core Components

#### 1. PersistentLayoutManager
**Location:** `Code/UI/Avalonia/PersistentLayoutManager.cs`

**Purpose:** Manages the persistent layout structure, rendering character information and delegating center content to specialized renderers.

**Key Methods:**
- `RenderLayout(Character character, Action<int, int, int, int> renderCenterContent, string title, Enemy? enemy, string? dungeonName, string? roomName)` - Main rendering method
- `RenderCharacterPanel(Character character)` - Renders the left character info panel
- `RenderRightPanel(Enemy? enemy, string? dungeonName, string? roomName)` - Renders the right location/enemy info panel
- `RenderEquipmentSlot(int x, ref int y, string slotName, string? itemName, Color color, int spacingAfter)` - Helper for rendering equipment slots
- `RenderEmptyCharacterPanel()` - Renders empty state when no character is loaded
- `GetCenterContentArea()` - Returns the dimensions of the center content area

**Layout Constants:**
```csharp
// Screen dimensions
SCREEN_WIDTH = 210
SCREEN_HEIGHT = 60

// Left panel (Character Info) - 13% of width
LEFT_PANEL_X = 0
LEFT_PANEL_Y = 2
LEFT_PANEL_WIDTH = 27
LEFT_PANEL_HEIGHT = 56

// Center panel (Dynamic Content) - 74% of width
CENTER_PANEL_X = 28
CENTER_PANEL_Y = 2
CENTER_PANEL_WIDTH = 154
CENTER_PANEL_HEIGHT = 56

// Right panel (Location/Enemy Info) - 13% of width
RIGHT_PANEL_X = 183
RIGHT_PANEL_Y = 2
RIGHT_PANEL_WIDTH = 27
RIGHT_PANEL_HEIGHT = 56
```

#### 2. CanvasUIManager (Refactored)
**Location:** `Code/UI/Avalonia/CanvasUIManager.cs`

**Changes:**
- Added `PersistentLayoutManager` instance
- Added `currentCharacter` field for persistent character reference
- Added `SetCharacter(Character character)` method to update character display
- Refactored all render methods to use persistent layout
- Created specialized content renderers for each game phase

**Refactored Render Methods:**
- `RenderInventory()` → Uses `RenderInventoryContent()`
- `RenderDungeonExploration()` → Uses `RenderDungeonContent()`
- `RenderCombat()` → Uses `RenderCombatContent()`
- `RenderGameMenu()` → Uses `RenderGameMenuContent()`
- `RenderDungeonSelection()` → Uses `RenderDungeonSelectionContent()`

#### 3. Game.cs Updates
**Location:** `Code/Game/Game.cs`

**Changes:**
- Added `SetCharacter()` calls in `StartNewGame()` to set the character in the UI manager
- Character is now automatically set when loading an existing save or creating a new character

## Game Phases with Persistent Layout

### 1. Inventory Screen
**Title:** "INVENTORY"
**Center Content:**
- List of inventory items with stats
- Item selection and equipping
- Quick actions (Equip Item, Enter Dungeon)

**Always Visible:**
- Hero name, level, class
- Current health and max health
- All stats (STR, AGI, TEC, INT)
- Equipped weapon and armor

### 2. Dungeon Exploration
**Title:** "DUNGEON EXPLORATION"
**Center Content:**
- Current location description
- Recent events log
- Available actions
- Quick actions (Inventory)

**Always Visible:**
- Hero information
- Health status updates in real-time
- Current equipment
- Stats

### 3. Combat
**Title:** "COMBAT"
**Center Content:**
- Enemy information and health
- Combat log with recent actions
- Combat actions (Attack, Use Item, Flee)

**Always Visible:**
- Hero health (updates as combat progresses)
- Hero stats
- Equipment
- Character level

### 4. Game Menu
**Title:** "WELCOME, [HERO NAME]!"
**Center Content:**
- Main game options (Go to Dungeon, Show Inventory, Save & Exit)
- Centered menu presentation

**Always Visible:**
- Full character overview
- Equipment status
- Health and stats

### 5. Dungeon Selection
**Title:** "DUNGEON SELECTION"
**Center Content:**
- List of available dungeons
- Dungeon difficulty and level
- Return to menu option

**Always Visible:**
- Character level and readiness
- Current health
- Equipment

## Benefits

### 1. Consistent User Experience
- Players always know where to look for character information
- No jarring transitions between screens
- Unified design language across all game phases

### 2. Better Gameplay Flow
- Check character status without leaving current screen
- Make informed decisions with full context
- Easier to track health and resources during dungeons

### 3. Improved UX
- Reduced cognitive load (character info always in same place)
- Faster decision-making
- Better spatial awareness

### 4. Development Benefits
- Single layout system to maintain
- Easier to add new game phases
- Consistent rendering pipeline
- Better code organization

## Usage Examples

### Rendering a Custom Phase
```csharp
public void RenderCustomPhase(Character player)
{
    currentCharacter = player;
    ClearClickableElements();
    
    // Use persistent layout with character info always visible
    layoutManager.RenderLayout(player, (contentX, contentY, contentWidth, contentHeight) =>
    {
        // Render your custom content in the center panel
        RenderCustomContent(contentX, contentY, contentWidth, contentHeight);
    }, "CUSTOM PHASE TITLE");
}

private void RenderCustomContent(int x, int y, int width, int height)
{
    // Your custom rendering code here
    canvas.AddText(x + 2, y, "Custom Content", AsciiArtAssets.Colors.White);
    // ... more rendering
}
```

### Updating Character Display
```csharp
// When character state changes (level up, health change, equipment change)
if (customUIManager is CanvasUIManager canvasUI)
{
    canvasUI.SetCharacter(currentPlayer);
}
```

### Getting Center Content Area Dimensions
```csharp
var (x, y, width, height) = layoutManager.GetCenterContentArea();
// Use these dimensions for custom rendering
```

## Technical Details

### Left Panel Sections (Character Info)
1. **Hero Section**
   - Character name
   - Level
   - Class

2. **Health Section**
   - Health bar visualization
   - Current/Max health numbers

3. **Stats Section**
   - STR, AGI, TEC, INT display
   - Color-coded stats (Red, Green, Blue, Purple)

4. **Gear Section**
   - Weapon name (truncated at 15 chars → 12 + "...")
   - Head armor (truncated at 15 chars → 12 + "...")
   - Body armor (truncated at 15 chars → 12 + "...")
   - Feet armor (truncated at 15 chars → 12 + "...")

### Right Panel Sections (Location/Enemy Info)
1. **Location Section**
   - Dungeon name (truncated at 20 chars → 17 + "...")
   - Room name (truncated at 20 chars → 17 + "...")

2. **Enemy Section** (when in combat)
   - Enemy name (truncated at 20 chars → 17 + "...")
   - Enemy level
   - Enemy health bar
   - Current/Max health numbers

3. **Empty State** (when not in dungeon)
   - "No Active Dungeon" message

### Content Area Rendering
- Content renderers receive (x, y, width, height) parameters
- All rendering is relative to these coordinates
- Clickable elements are positioned based on content area
- Bottom-aligned actions use `y + height - offset` for consistent placement

### Performance Considerations
- Character panel renders once per frame
- Center content can update independently
- Efficient text truncation for long names
- Minimal canvas operations for smooth rendering

## Future Enhancements

### Potential Improvements
1. **Collapsible Character Panel**
   - Toggle character panel visibility
   - More screen space for content when needed
   - Hotkey to show/hide

2. **Responsive Layout**
   - Adjust panel sizes based on window size
   - Different layouts for different resolutions
   - Mobile-friendly adaptations

3. **Animated Transitions**
   - Smooth transitions between game phases
   - Health bar animations
   - Highlight important stat changes

4. **Enhanced Character Panel**
   - Mini equipment previews
   - Status effect icons
   - Experience bar
   - Gold/currency display

5. **Contextual Information**
   - Show relevant stats based on current phase
   - Highlight stats that matter in combat
   - Equipment comparison tooltips

## Testing

### Test Coverage
- [x] Inventory rendering with persistent layout
- [x] Dungeon exploration with character panel
- [x] Combat with real-time health updates
- [x] Game menu with full character overview
- [x] Dungeon selection with character info
- [ ] Character state changes (health, level, equipment)
- [ ] Long item names truncation
- [ ] Different character levels and states
- [ ] Mouse interaction with new element positions

### Test Scenarios
1. **Character Creation** - Verify character appears in panel after creation
2. **Equipment Change** - Check panel updates when equipping items
3. **Combat** - Verify health updates in real-time during combat
4. **Level Up** - Confirm stats update when leveling up
5. **Long Names** - Test truncation with very long item names
6. **Phase Transitions** - Verify smooth transitions between game phases

## Related Documentation
- **ARCHITECTURE.md** - System architecture overview
- **UI_SYSTEM.md** - UI system documentation
- **COLOR_SYSTEM.md** - Color system and markup
- **TASKLIST.md** - Development task list

---

*The Persistent Layout System provides a consistent, professional game interface that enhances player experience and simplifies development.*

