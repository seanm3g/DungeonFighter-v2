# CanvasUIManager Refactoring

## Overview

The `CanvasUIManager.cs` class has been refactored to address architectural ambiguity and improve code organization. The original class was over 1,700 lines and handled too many responsibilities, making it difficult to maintain and extend.

## Problems Addressed

### 1. **God Object Anti-Pattern**
- `CanvasUIManager` was handling menu rendering, combat rendering, inventory rendering, dungeon rendering, text wrapping, color markup, mouse interaction, and more
- Violated the Single Responsibility Principle
- Made the class difficult to understand and maintain

### 2. **Architectural Ambiguity**
- Unclear where to add new rendering logic
- Mixed concerns (UI logic, color parsing, text wrapping, interaction handling)
- Hard to test individual components in isolation

### 3. **Code Duplication**
- Text wrapping and color markup logic was duplicated across methods
- Similar rendering patterns repeated for different screen types

## Refactoring Solution

### New Architecture

```
Code/UI/Avalonia/
├── CanvasUIManager.cs (Orchestrator - 700 lines)
├── Renderers/
│   ├── ColoredTextWriter.cs (Text rendering utilities)
│   ├── MenuRenderer.cs (Menu screens)
│   ├── CombatRenderer.cs (Combat screens)
│   ├── InventoryRenderer.cs (Inventory screens)
│   └── DungeonRenderer.cs (Dungeon screens)
```

### Extracted Components

#### 1. **ColoredTextWriter.cs**
**Responsibility**: Text rendering with color markup and text wrapping

**Key Methods**:
- `WriteLineColored(message, x, y)` - Renders text with color markup
- `WriteLineColoredWrapped(message, x, y, maxWidth)` - Renders with wrapping
- `WrapText(text, maxWidth)` - Wraps text preserving indentation
- `SplitPreservingMarkup(text)` - Splits text while keeping color templates intact

**Benefits**:
- Centralized text rendering logic
- Reusable across all renderers
- Easy to test and optimize

#### 2. **MenuRenderer.cs**
**Responsibility**: Rendering all menu-related screens

**Key Methods**:
- `RenderMainMenu(hasSavedGame, characterName, characterLevel)` - Main menu with color gradients
- `RenderSettings()` - Settings screen
- `RenderGameMenu(x, y, width, height)` - In-game menu

**Benefits**:
- All menu logic in one place
- Consistent menu styling
- Uses `ColorLayerSystem` for dynamic color gradients (respects `WhiteTemperatureIntensity`)

#### 3. **CombatRenderer.cs**
**Responsibility**: Rendering combat-related screens

**Key Methods**:
- `RenderCombat(x, y, width, height, player, enemy, combatLog)` - Combat screen
- `RenderEnemyEncounter(x, y, width, height, dungeonLog)` - Enemy encounter
- `RenderCombatResult(x, y, width, height, playerSurvived, enemy, battleNarrative)` - Victory/defeat

**Benefits**:
- Isolated combat UI logic
- Easy to modify combat display without affecting other screens
- Battle narrative rendering centralized

#### 4. **InventoryRenderer.cs**
**Responsibility**: Rendering inventory screens

**Key Methods**:
- `RenderInventory(x, y, width, height, character, inventory)` - Full inventory screen
- `GetItemStats(item, character)` - Item stat formatting

**Benefits**:
- All inventory display logic together
- Item rendering with color markup
- Clickable element management for inventory interactions

#### 5. **DungeonRenderer.cs**
**Responsibility**: Rendering dungeon-related screens

**Key Methods**:
- `RenderDungeonSelection(x, y, width, height, dungeons)` - Dungeon selection with themed colors
- `RenderDungeonStart(x, y, width, height, dungeon)` - Dungeon entry
- `RenderRoomEntry(x, y, width, height, room)` - Room description
- `RenderRoomCompletion(x, y, width, height, room, character)` - Room cleared
- `RenderDungeonCompletion(x, y, width, height, dungeon)` - Dungeon victory

**Benefits**:
- All dungeon UI in one place
- Theme-based color mapping centralized
- Consistent dungeon progression display

### Updated CanvasUIManager

**New Role**: Orchestrator and Coordinator

**Responsibilities**:
1. **Initialization**: Creates and manages renderer instances
2. **Layout Management**: Uses `PersistentLayoutManager` for consistent layouts
3. **Delegation**: Routes rendering requests to appropriate renderers
4. **State Management**: Tracks current character, dungeon context, enemy state
5. **Display Buffer**: Manages scrolling combat log
6. **Mouse Interaction**: Handles clickable elements and hover states

**Size Reduction**: ~1,700 lines → ~700 lines (60% reduction)

## Benefits of Refactoring

### 1. **Improved Maintainability**
- Each renderer has a clear, focused responsibility
- Easy to locate and modify specific UI elements
- Reduced cognitive load when working with the code

### 2. **Better Testability**
- Renderers can be tested independently
- ColoredTextWriter can be unit tested without Avalonia
- Mock dependencies easily for isolated testing

### 3. **Enhanced Extensibility**
- Adding new screens is straightforward (create a new renderer)
- Modifying existing screens doesn't risk breaking others
- Easy to add new rendering features to specific areas

### 4. **Code Reusability**
- ColoredTextWriter is used by all renderers
- Common rendering patterns shared across renderers
- No duplication of text wrapping or color parsing logic

### 5. **Clear Architecture**
- New developers can quickly understand the structure
- Obvious where to add new functionality
- Consistent patterns across all renderers

## Usage Examples

### Before Refactoring
```csharp
// All logic embedded in CanvasUIManager
public void RenderMainMenu(bool hasSavedGame, string? characterName, int characterLevel)
{
    // 50+ lines of rendering logic here...
}
```

### After Refactoring
```csharp
// CanvasUIManager delegates to MenuRenderer
public void RenderMainMenu(bool hasSavedGame, string? characterName, int characterLevel)
{
    menuRenderer.RenderMainMenu(hasSavedGame, characterName, characterLevel);
}
```

### Adding a New Renderer
```csharp
// 1. Create new renderer
public class CharacterSheetRenderer
{
    private readonly GameCanvasControl canvas;
    private readonly ColoredTextWriter textWriter;
    
    public CharacterSheetRenderer(GameCanvasControl canvas, ColoredTextWriter textWriter)
    {
        this.canvas = canvas;
        this.textWriter = textWriter;
    }
    
    public void RenderCharacterSheet(Character character)
    {
        // Rendering logic here...
    }
}

// 2. Add to CanvasUIManager
private readonly Renderers.CharacterSheetRenderer characterSheetRenderer;

public CanvasUIManager(GameCanvasControl canvas)
{
    // ... existing code ...
    this.characterSheetRenderer = new Renderers.CharacterSheetRenderer(canvas, textWriter);
}

// 3. Add delegation method
public void RenderCharacterSheet(Character character)
{
    RenderWithLayout(character, "CHARACTER SHEET", (contentX, contentY, contentWidth, contentHeight) =>
    {
        characterSheetRenderer.RenderCharacterSheet(character);
    });
}
```

## Future Improvements

### 1. **Extract Screen Coordinator**
- Create a `ScreenCoordinator` class to manage screen transitions
- Move screen state management out of `CanvasUIManager`

### 2. **Interface-Based Design**
- Create `IScreenRenderer` interface
- All renderers implement the interface
- Enable dependency injection and better testing

### 3. **Separate Mouse Interaction**
- Extract clickable element management to `InteractionManager`
- Separate hover state handling from rendering

### 4. **Animation System**
- Create `AnimationRenderer` for transitions and effects
- Support fade-in/fade-out for screen changes

### 5. **Render Pipeline**
- Implement a render pipeline pattern
- Support post-processing effects (blur, color grading, etc.)

## Testing Strategy

### Unit Tests
```csharp
[Test]
public void ColoredTextWriter_WrapText_PreservesIndentation()
{
    var canvas = new MockCanvas();
    var writer = new ColoredTextWriter(canvas);
    
    string input = "    This is a long indented line that should be wrapped";
    var result = writer.WrapText(input, 20);
    
    Assert.That(result[0], Does.StartWith("    "));
    Assert.That(result[1], Does.StartWith("  ")); // Continuation indent
}
```

### Integration Tests
```csharp
[Test]
public void MenuRenderer_RenderMainMenu_WithSavedGame()
{
    var canvas = new MockCanvas();
    var clickableElements = new List<ClickableElement>();
    var renderer = new MenuRenderer(canvas, clickableElements);
    
    renderer.RenderMainMenu(true, "TestHero", 5);
    
    Assert.That(canvas.GetText(), Contains.Substring("Load Game - *TestHero - lvl 5*"));
    Assert.That(clickableElements.Count, Is.EqualTo(4)); // 4 menu options
}
```

## Migration Guide

### For Developers

1. **When adding new UI**:
   - Determine which renderer should handle it
   - If no existing renderer fits, create a new one following the established pattern
   - Use `ColoredTextWriter` for all text rendering

2. **When modifying existing UI**:
   - Navigate to the appropriate renderer
   - Update the specific rendering method
   - Test that related screens still work

3. **When debugging rendering issues**:
   - Check the renderer first (specific logic)
   - Then check `CanvasUIManager` (orchestration)
   - Finally check `ColoredTextWriter` (text utilities)

## Conclusion

This refactoring transforms `CanvasUIManager` from a monolithic "god object" into a well-organized system of focused, testable components. The new architecture:

- **Reduces complexity**: Each component has a single, clear purpose
- **Improves maintainability**: Easy to find and modify specific functionality
- **Enhances extensibility**: Adding new features is straightforward
- **Follows SOLID principles**: Especially Single Responsibility and Open/Closed

The refactored code is production-ready, fully functional, and sets a strong foundation for future UI development.

