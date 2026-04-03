# Canvas Clearing Guidelines

This document explains the unified canvas clearing system and when to use different clearing methods.

## Architecture Overview

The clearing system follows a layered architecture:

```
CanvasUICoordinator (public API)
  ↓
UtilityCoordinator / DisplayUpdateCoordinator (coordination)
  ↓
GameCanvasControl (implementation)
  ↓
CanvasElementManager (actual clearing)
```

## Single Source of Truth

**All canvas clearing operations must go through `DisplayUpdateCoordinator` via `CanvasUICoordinator`.**

### Allowed Direct Clearing

Only these locations are allowed to call `canvas.Clear()` directly:

1. **LayoutCoordinator** (line 52) - Single clearing point for layout operations
2. **GameCanvasControl** - Low-level implementation
3. **MessageDisplayRenderer** and **HelpSystemRenderer** - Use clearing actions (delegates) for full-screen operations

All other code must use the coordinator methods.

## Clearing Methods

### CanvasUICoordinator.Clear()

**Use for:** General canvas clearing operations

**What it does:** Clears all canvas elements (text, boxes, progress bars)

**When to use:**
- Before rendering a new screen
- When transitioning between major UI states
- When you need a completely clean canvas

**Example:**
```csharp
canvasUI.Clear();
canvasUI.RenderMainMenu(hasSavedGame, characterName, characterLevel);
```

### CanvasUICoordinator.ClearDisplayBuffer()

**Use for:** Clearing the display buffer (combat log, dungeon messages)

**What it does:** Clears the text buffer that displays game messages

**When to use:**
- When switching to menu screens
- When you want to clear message history
- Before suppressing display buffer rendering

**Example:**
```csharp
canvasUI.ClearDisplayBuffer();
```

### CanvasUICoordinator.ClearDisplayBufferWithoutRender()

**Use for:** Clearing display buffer without triggering a render

**What it does:** Clears the buffer but doesn't immediately render (prevents flicker)

**When to use:**
- In ScreenTransitionProtocol before rendering menu screens
- When you're about to render a new screen and don't want buffer content to show

**Example:**
```csharp
canvasUI.ClearDisplayBufferWithoutRender();
canvasUI.RenderMainMenu(...);
```

## Screen Transition Pattern

### Using ScreenTransitionProtocol

**Always use `ScreenTransitionProtocol` for menu screen transitions.**

The protocol handles:
1. State transition
2. Display buffer suppression and clearing
3. Interactive elements clearing
4. Context clearing
5. Canvas clearing
6. Screen rendering
7. Refresh

**Important:** When render methods are called through `ScreenTransitionProtocol`, they should pass `clearCanvas: false` to `RenderWithLayout()` to avoid double-clearing.

**Example:**
```csharp
ScreenTransitionProtocol.TransitionToMenuScreen(
    stateManager,
    canvasUI,
    GameState.MainMenu,
    (ui) => ui.RenderMainMenu(hasSavedGame, characterName, characterLevel),
    character: null,
    clearEnemyContext: true,
    clearDungeonContext: true
);
```

### Render Method Pattern

Render methods called through `ScreenTransitionProtocol` should:

```csharp
public void RenderMainMenu(bool hasSavedGame, string? characterName, int characterLevel)
{
    // ScreenTransitionProtocol already cleared the canvas
    // Pass clearCanvas: false to avoid double-clearing
    RenderWithLayout(null, "MAIN MENU", (contentX, contentY, contentWidth, contentHeight) =>
    {
        menuRenderer.RenderMainMenuContent(contentX, contentY, contentWidth, contentHeight, ...);
    }, new CanvasContext(), null, null, null, clearCanvas: false);
}
```

## LayoutCoordinator Clearing

`LayoutCoordinator` is the **single clearing point** for layout-based rendering.

### When clearCanvas=true

- Canvas is cleared immediately before rendering
- Use for clean transitions between different screens
- Prevents flicker by clearing and rendering in quick succession

### When clearCanvas=false

- Canvas is NOT cleared
- Use when canvas was already cleared (e.g., by ScreenTransitionProtocol)
- Preserves existing content and only updates what's needed

**Rule:** If `ScreenTransitionProtocol` was used, always pass `clearCanvas: false`.

## Direct Render Calls (Without ScreenTransitionProtocol)

For direct render calls (not through ScreenTransitionProtocol):

1. **Clear the canvas first** using `canvasUI.Clear()`
2. **Call the render method** which should use `clearCanvas: false` in `RenderWithLayout()`

**Example:**
```csharp
canvasUI.Clear();
canvasUI.RenderGameMenu(player, inventory);
```

## Special Cases

### MessageDisplayRenderer and HelpSystemRenderer

These renderers use clearing actions (delegates) because they're used for full-screen operations that need immediate clearing. The clearing action is passed during construction.

### LayoutCoordinator

This is the ONLY place allowed to call `canvas.Clear()` directly for layout operations. All layout-based clearing goes through this coordinator.

## Common Patterns

### Pattern 1: Menu Screen Transition

```csharp
// Use ScreenTransitionProtocol
ScreenTransitionProtocol.TransitionToMenuScreen(
    stateManager,
    canvasUI,
    GameState.Inventory,
    (ui) => ui.RenderInventory(character, inventory),
    character: character
);
```

### Pattern 2: Direct Render (No Protocol)

```csharp
// Clear first, then render with clearCanvas: false
canvasUI.Clear();
canvasUI.RenderGameMenu(player, inventory);
```

### Pattern 3: Display Buffer Management

```csharp
// Suppress and clear buffer before menu
canvasUI.SuppressDisplayBufferRendering();
canvasUI.ClearDisplayBufferWithoutRender();
// ... render menu ...
```

## Anti-Patterns (Don't Do This)

### ❌ Double Clearing

```csharp
// BAD: Clearing twice
canvasUI.Clear();
canvasUI.RenderMainMenu(...); // This also clears if clearCanvas: true
```

### ❌ Direct canvas.Clear() in Renderers

```csharp
// BAD: Direct canvas access
public void RenderSomething()
{
    canvas.Clear(); // Don't do this!
}
```

### ❌ Clearing After ScreenTransitionProtocol

```csharp
// BAD: Protocol already clears
ScreenTransitionProtocol.TransitionToMenuScreen(...);
canvasUI.Clear(); // Unnecessary!
```

## Summary

1. **Always use `CanvasUICoordinator.Clear()`** for public API clearing
2. **Use `ScreenTransitionProtocol`** for all menu screen transitions
3. **Pass `clearCanvas: false`** when called through ScreenTransitionProtocol
4. **LayoutCoordinator** is the single clearing point for layout operations
5. **No direct `canvas.Clear()`** calls except in allowed locations
6. **Clear display buffer** when switching to menu screens

## Related Files

- `Code/UI/Avalonia/Display/DisplayUpdateCoordinator.cs` - Core clearing coordinator
- `Code/UI/Avalonia/CanvasUICoordinator.cs` - Public API
- `Code/UI/Avalonia/Layout/LayoutCoordinator.cs` - Layout clearing point
- `Code/UI/Avalonia/Transitions/ScreenTransitionProtocol.cs` - Screen transition protocol
