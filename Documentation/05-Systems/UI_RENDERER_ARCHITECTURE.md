# UI Renderer Architecture

## Overview

The DungeonFighter-v2 UI rendering system uses a modular architecture with specialized renderers for different screen types. This document describes the current architecture after the CanvasUICoordinator refactoring (previously known as CanvasUICoordinator).

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                         MainWindow.axaml.cs                      │
│                    (Avalonia Window / Event Handler)             │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                    CanvasUICoordinator.cs                        │
│                         (Orchestrator)                           │
│                                                                   │
│  Responsibilities:                                                │
│  • Screen layout coordination (PersistentLayoutManager)          │
│  • Renderer initialization and management                        │
│  • State tracking (character, enemy, dungeon context)            │
│  • Display buffer management (scrolling combat log)              │
│  • Mouse interaction coordination (clickable elements)           │
└───┬───────┬───────┬───────┬───────┬──────────────────────────────┘
    │       │       │       │       │
    │       │       │       │       └─────────────────┐
    │       │       │       │                         │
    ▼       ▼       ▼       ▼       ▼                 ▼
┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐   ┌──────────────────┐
│Menu │ │Comb-│ │Inv- │ │Dung-│ │Color│   │PersistentLayout  │
│Rend-│ │atRe-│ │entRe│ │eonRe│ │edTex│   │Manager           │
│erer │ │ndrer│ │ndrer│ │ndrer│ │tWrit│   │(Layout system)   │
└─────┘ └─────┘ └─────┘ └─────┘ └──┬──┘   └──────────────────┘
                                    │
                                    ▼
                          ┌──────────────────┐
                          │GameCanvasControl │
                          │(Low-level render)│
                          └──────────────────┘
```

## Component Responsibilities

### CanvasUICoordinator (Orchestrator)
**File**: `Code/UI/Avalonia/CanvasUICoordinator.cs`
**Lines**: ~542 (refactored from monolithic CanvasUICoordinator)
**Role**: High-level coordinator

**Responsibilities**:
- Initialize all renderers
- Manage persistent layout (character stats, health bars)
- Coordinate screen rendering with proper layout
- Track game state (current character, enemy, dungeon)
- Manage scrolling display buffer
- Handle mouse interaction (clickable elements, hover states)

**Key Methods**:
- `RenderMainMenu(...)` → Delegates to `MenuRenderer`
- `RenderCombat(...)` → Delegates to `CombatRenderer`
- `RenderInventory(...)` → Delegates to `InventoryRenderer`
- `RenderDungeonSelection(...)` → Delegates to `DungeonRenderer`
- `RenderWithLayout(...)` → Coordinates layout and rendering

### ColoredTextWriter (Utility)
**File**: `Code/UI/Avalonia/Renderers/ColoredTextWriter.cs`
**Lines**: ~200
**Role**: Text rendering utilities

**Responsibilities**:
- Parse color markup (`&R`, `&G`, `{{template|text}}`)
- Render colored text to canvas
- Wrap text with indentation preservation
- Split text while preserving markup integrity

**Key Methods**:
- `WriteLineColored(message, x, y)` - Render text with colors
- `WriteLineColoredWrapped(message, x, y, maxWidth)` - Render with wrapping
- `WrapText(text, maxWidth)` - Wrap preserving indentation
- `SplitPreservingMarkup(text)` - Split on spaces, keep markup together

**Used By**: All renderers

### MenuRenderer (Specialized)
**File**: `Code/UI/Avalonia/Renderers/MenuRenderer.cs`
**Lines**: ~220
**Role**: Menu screen rendering

**Responsibilities**:
- Render main menu with color gradients
- Render settings screen
- Render in-game menu
- Create clickable menu options

**Key Features**:
- Uses `ColorLayerSystem` for warm/cool white gradients
- Respects `WhiteTemperatureIntensity` configuration
- Handles saved game display
- Delete character confirmation UI

**Screens**:
- Main Menu (New Game, Load Game, Settings, Quit)
- Settings (Game settings, controls, delete save)
- Game Menu (Go to Dungeon, Show Inventory, Save & Exit)

### CombatRenderer (Specialized)
**File**: `Code/UI/Avalonia/Renderers/CombatRenderer.cs`
**Lines**: ~130
**Role**: Combat screen rendering

**Responsibilities**:
- Render combat screen (enemy info + combat log)
- Render enemy encounter (dungeon context)
- Render combat results (victory/defeat + narrative)

**Key Features**:
- Scrolling combat log with text wrapping
- Enemy information display
- Battle narrative integration
- Combat action menu (Attack, Use Item, Flee)

**Screens**:
- Combat (active battle)
- Enemy Encounter (pre-combat context)
- Combat Result (post-combat summary)

### InventoryRenderer (Specialized)
**File**: `Code/UI/Avalonia/Renderers/InventoryRenderer.cs`
**Lines**: ~180
**Role**: Inventory screen rendering

**Responsibilities**:
- Render inventory items with color-coded rarity
- Display item stats (damage, armor, bonuses)
- Create clickable item elements
- Render inventory action buttons

**Key Features**:
- Item color formatting (common, uncommon, rare, etc.)
- Item stat display (damage, speed, armor)
- Two-column action button layout
- Clickable items for selection

**Actions**:
- Equip Item
- Unequip Item
- Discard Item
- Manage Combo Actions
- Continue to Dungeon
- Return to Main Menu
- Exit Game

### DungeonRenderer (Specialized)
**File**: `Code/UI/Avalonia/Renderers/DungeonRenderer.cs`
**Lines**: ~280
**Role**: Dungeon-related screen rendering

**Responsibilities**:
- Render dungeon selection with theme-based colors
- Render dungeon entry information
- Render room descriptions
- Render room/dungeon completion

**Key Features**:
- Theme-based dungeon name coloring (ocean, crystal, etc.)
- Room description with text wrapping
- Threat detection display
- Victory screens with theme colors

**Screens**:
- Dungeon Selection (available dungeons list)
- Dungeon Start (dungeon information)
- Room Entry (room description + enemy detection)
- Room Completion (victory + health status)
- Dungeon Completion (full victory screen)

## Data Flow

### Rendering a Combat Screen

```
1. GameLoop calls: canvasUIManager.RenderCombat(player, enemy, combatLog)
                   ↓
2. CanvasUICoordinator: RenderWithLayout(player, "COMBAT", renderContent, enemy)
                   ↓
3. PersistentLayoutManager: Renders character panel + health bars
                   ↓
4. CanvasUICoordinator: Calls renderContent callback with content coordinates
                   ↓
5. renderContent: combatRenderer.RenderCombat(x, y, width, height, ...)
                   ↓
6. CombatRenderer: Uses textWriter.WriteLineColoredWrapped(...)
                   ↓
7. ColoredTextWriter: Parses colors, wraps text, renders to canvas
                   ↓
8. GameCanvasControl: Low-level pixel rendering
```

### Color System Integration

```
MenuRenderer requests color
      ↓
ColorLayerSystem.GetWhite(WhiteTemperature.Warm, 1.0f)
      ↓
ColorLayerSystem loads WhiteTemperatureIntensity from UIConfiguration.json
      ↓
Calculates warm/cool RGB values based on intensity
      ↓
Returns ColorRGB
      ↓
MenuRenderer converts to Avalonia.Media.Color
      ↓
Rendered to canvas
```

## Interaction Flow

### Mouse Click on Menu Option

```
1. MainWindow.axaml.cs detects mouse click
                   ↓
2. Converts pixel coordinates to character grid position
                   ↓
3. Calls: canvasUIManager.GetElementAt(charX, charY)
                   ↓
4. CanvasUICoordinator searches clickableElements list
                   ↓
5. Returns matching ClickableElement (or null)
                   ↓
6. MainWindow processes the click (calls game logic)
```

## Testing Strategy

### Unit Testing
```csharp
// Test ColoredTextWriter independently
[Test]
public void ColoredTextWriter_WrapText_PreservesIndentation()
{
    var mockCanvas = new MockGameCanvasControl();
    var writer = new ColoredTextWriter(mockCanvas);
    
    var result = writer.WrapText("    Long indented text...", 20);
    
    Assert.That(result[0], Does.StartWith("    "));
}
```

### Integration Testing
```csharp
// Test renderer with real dependencies
[Test]
public void MenuRenderer_RenderMainMenu_CreatesClickableElements()
{
    var canvas = new GameCanvasControl(...);
    var clickableElements = new List<ClickableElement>();
    var renderer = new MenuRenderer(canvas, clickableElements);
    
    renderer.RenderMainMenu(true, "Hero", 5);
    
    Assert.That(clickableElements.Count, Is.EqualTo(4)); // 4 menu options
}
```

## Extension Points

### Adding a New Renderer

**Example: Character Sheet Renderer**

```csharp
// 1. Create new renderer
namespace RPGGame.UI.Avalonia.Renderers
{
    public class CharacterSheetRenderer
    {
        private readonly GameCanvasControl canvas;
        private readonly ColoredTextWriter textWriter;
        
        public CharacterSheetRenderer(GameCanvasControl canvas, ColoredTextWriter textWriter)
        {
            this.canvas = canvas;
            this.textWriter = textWriter;
        }
        
        public void RenderCharacterSheet(int x, int y, int width, int height, Character character)
        {
            // Render stats, equipment, skills, etc.
            textWriter.WriteLineColored($"&G{character.Name}", x + 2, y);
            canvas.AddText(x + 2, y + 2, $"Level: {character.Level}", AsciiArtAssets.Colors.White);
            // ... more rendering ...
        }
    }
}

// 2. Add to CanvasUICoordinator
public class CanvasUICoordinator : IUIManager
{
    private readonly Renderers.CharacterSheetRenderer characterSheetRenderer;
    
    public CanvasUICoordinator(GameCanvasControl canvas)
    {
        // ... existing initialization ...
        this.characterSheetRenderer = new Renderers.CharacterSheetRenderer(canvas, textWriter);
    }
    
    public void RenderCharacterSheet(Character character)
    {
        RenderWithLayout(character, "CHARACTER SHEET", (contentX, contentY, contentWidth, contentHeight) =>
        {
            characterSheetRenderer.RenderCharacterSheet(contentX, contentY, contentWidth, contentHeight, character);
        });
    }
}
```

## Performance Considerations

### Text Wrapping Optimization
- Pre-calculate display lengths (excluding markup)
- Cache wrapped results when possible
- Use efficient string building (avoid repeated concatenation)

### Render Batching
- Group canvas operations together
- Call `canvas.Refresh()` once after all rendering
- Use Avalonia's `Dispatcher.UIThread` for UI updates

### Memory Management
- Reuse `ClickableElement` collections
- Clear collections when switching screens
- Avoid creating new objects in tight loops

## Future Enhancements

### 1. Interface-Based Renderers
```csharp
public interface IScreenRenderer
{
    void Render(int x, int y, int width, int height);
    void Clear();
}

public class MenuRenderer : IScreenRenderer { ... }
```

### 2. Screen Transition System
```csharp
public class ScreenCoordinator
{
    public void TransitionTo(IScreenRenderer newScreen, TransitionType type)
    {
        // Fade out current screen
        // Fade in new screen
    }
}
```

### 3. Render Pipeline
```csharp
public class RenderPipeline
{
    public void AddPostProcessor(IPostProcessor processor) { }
    public void Render(IRenderableScreen screen) { }
}
```

## Troubleshooting

### Common Issues

**Issue**: Text not wrapping correctly
**Solution**: Check `ColoredTextWriter.WrapText()` - ensure maxWidth is correct

**Issue**: Colors not showing
**Solution**: Verify color markup syntax (`&R`, `{{template|text}}`) and that `ColorParser` is being used

**Issue**: Clickable elements not responding
**Solution**: Check that `clickableElements` list is being populated and cleared correctly

**Issue**: Screen not refreshing
**Solution**: Ensure `canvas.Refresh()` is called after all rendering operations

## Screen Coordination Pattern

### GameScreenCoordinator

**File**: `Code/Game/GameScreenCoordinator.cs`  
**Role**: Central coordinator for high-level screen transitions

#### Purpose

The `GameScreenCoordinator` provides a **single place** to manage how core game screens are rendered and how state transitions occur. This pattern addresses the problem of screen rendering logic being scattered across multiple handlers and the `Game` class.

#### Benefits

1. **Single Source of Truth**: All screen transitions for core flows (GameLoop, Inventory, DungeonCompletion) are defined in one place
2. **Easier Debugging**: When a screen doesn't render, you know exactly where to look
3. **Explicit Invariants**: The coordinator validates required state (player, dungeon, UI manager) before rendering
4. **Reduced Duplication**: No need to repeat "clear context, set character, render screen" logic in multiple handlers

#### Current Implementation

The coordinator now handles ALL screen transitions using the standardized `ScreenTransitionProtocol`:

- **`ShowGameLoop()`**: Renders the main in-game menu with persistent character panel
- **`ShowInventory()`**: Renders inventory screen with clean transitions from other screens
- **`ShowDungeonCompletion(...)`**: Renders dungeon completion screen with rewards
- **`ShowMainMenu(...)`**: Renders main menu with saved game info
- **`ShowDeathScreen(Character)`**: Renders death screen with run statistics
- **`ShowDungeonSelection()`**: Renders dungeon selection screen
- **`ShowSettings()`**: Renders settings menu
- **`ShowCharacterInfo()`**: Shows character info in persistent layout
- **`ShowWeaponSelection(List<StartingWeapon>)`**: Renders weapon selection screen
- **`ShowCharacterCreation(Character)`**: Renders character creation screen

All methods use `ScreenTransitionProtocol` internally for consistent behavior.

#### Usage Pattern

```csharp
// In Game.cs
public void ShowInventory()
{
    // Delegate to centralized coordinator
    screenCoordinator.ShowInventory();
}

// Handlers trigger events that call Game methods
// Game methods delegate to GameScreenCoordinator
// Coordinator handles all CanvasUICoordinator calls
```

#### Migration Strategy

When adding new screens or refactoring existing ones:

1. **Add method to `GameScreenCoordinator`** that handles all rendering logic
2. **Update `Game.ShowX()` method** to delegate to the coordinator
3. **Simplify handlers** to just trigger events (or call `Game.ShowX()` directly)
4. **Document the screen** in this section

#### Screen Transition Protocol

All screen transitions now use the standardized `ScreenTransitionProtocol` which ensures:

1. **Consistent Sequence**: All screens follow the same 9-step protocol
2. **Automatic Display Buffer Management**: `DisplayBufferManager` handles suppression/restoration automatically
3. **Explicit Canvas Clearing**: Canvas is always cleared explicitly, preventing title-change detection issues
4. **State Tracking**: Last rendered screen state is tracked to prevent unnecessary re-renders

See **[Canvas Rendering Architecture Analysis](./CANVAS_RENDERING_ARCHITECTURE_ANALYSIS.md)** for detailed protocol documentation.

## Related Documentation

- **[CanvasUICoordinator Refactoring](../02-Development/CANVASUIMANAGER_REFACTORING.md)** - Detailed refactoring guide
- **[Color System](COLOR_LAYER_SYSTEM.md)** - Color temperature and layer system
- **[White Temperature Implementation](WHITE_TEMPERATURE_IMPLEMENTATION.md)** - WhiteTemperatureIntensity parameter
- **[Avalonia UI Guide](../02-Development/AVALONIA_UI_GUIDE.md)** - Avalonia-specific patterns

## Conclusion

The renderer architecture provides:
- ✅ Clear separation of concerns
- ✅ Easy extensibility (add new renderers)
- ✅ Testable components
- ✅ Maintainable codebase
- ✅ Consistent rendering patterns

Each renderer focuses on a specific screen type, uses shared utilities (`ColoredTextWriter`), and is coordinated by `CanvasUICoordinator` for a clean, modular UI system.

