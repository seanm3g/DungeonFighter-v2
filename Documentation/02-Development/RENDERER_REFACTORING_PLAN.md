# Renderer System Refactoring Plan

## Overview
This document outlines the refactoring work completed and future improvements for the renderer system in DungeonFighter-v2.

**Last Updated**: October 12, 2025  
**Status**: Phase 1 Complete

---

## Architecture Overview

### Current UI Rendering Stack

```
MainWindow (Avalonia)
    ↓
CanvasUIManager (IUIManager)
    ↓
Renderers (IScreenRenderer, IInteractiveRenderer)
    ↓
GameCanvasControl (Avalonia Control)
    ↓
Screen Display
```

### Core Components

1. **GameCanvasControl** - Low-level canvas that draws characters at grid positions
2. **CanvasUIManager** - Main UI manager that coordinates all renderers
3. **Renderers** - Specialized classes for different screen types
4. **PersistentLayoutManager** - Manages consistent layout across screens
5. **ColoredTextWriter** - Utility for text rendering with color markup

---

## Phase 1: Foundation (COMPLETED ✅)

### 1.1 ColoredTextWriter Extraction ✅
**Status**: Complete  
**File**: `Code/UI/Avalonia/Renderers/ColoredTextWriter.cs`

**What Was Done:**
- Extracted text processing utilities into dedicated class
- Handles color markup parsing via `ColorParser`
- Implements text wrapping with indentation preservation
- Supports template-based color markup (e.g., `{{fire}}`, `{{ice}}`)

**Benefits:**
- Reusable across all renderers
- Centralized text formatting logic
- Performance optimized (pre-calculates word lengths)

### 1.2 IScreenRenderer Interface ✅
**Status**: Complete  
**File**: `Code/UI/Avalonia/Renderers/IScreenRenderer.cs`

**What Was Done:**
- Created base `IScreenRenderer` interface
- Created extended `IInteractiveRenderer` interface
- Defined standard contract for all renderers

**Interface Design:**
```csharp
public interface IScreenRenderer
{
    void Render();          // Renders content
    void Clear();           // Clears state
    int GetLineCount();     // Gets line count
}

public interface IInteractiveRenderer : IScreenRenderer
{
    List<ClickableElement> GetClickableElements();
    void UpdateHoverState(int x, int y);
    bool HandleClick(int x, int y);
}
```

### 1.3 Renderer Implementation Updates ✅
**Status**: Complete

All renderers now implement appropriate interfaces:

| Renderer | Interface | Purpose |
|----------|-----------|---------|
| MenuRenderer | IInteractiveRenderer | Main menu, settings, game menu |
| CombatRenderer | IScreenRenderer | Combat log, results (no clicking) |
| DungeonRenderer | IInteractiveRenderer | Dungeon/room selection and info |
| InventoryRenderer | IInteractiveRenderer | Inventory management |

**What Was Added:**
- Line count tracking (`currentLineCount`)
- Interface method implementations
- `ClickableElement.Contains(x, y)` for hit testing

---

## Phase 2: State Machine Pattern (PLANNED)

### 2.1 Screen State System
**Status**: Not Started  
**Priority**: Medium

**Goal**: Replace direct method calls with state-based rendering.

**Current (Direct Calls):**
```csharp
menuRenderer.RenderMainMenu(hasSaved, name, level);
combatRenderer.RenderCombat(x, y, w, h, player, enemy, log);
```

**Proposed (State-Based):**
```csharp
menuRenderer.SetState(new MainMenuState(hasSaved, name, level));
menuRenderer.Render();
```

**Benefits:**
- Cleaner separation of data and rendering
- Easier to serialize/restore UI state
- Better testing (can verify state without rendering)
- Enables undo/redo functionality

**Implementation Plan:**
1. Create `IRendererState` interface
2. Create state classes for each screen type
3. Update `Render()` from placeholder to state-based
4. Migrate existing render methods to use states

### 2.2 Renderer Factory/Registry
**Status**: Not Started  
**Priority**: Low

**Goal**: Centralized renderer management and creation.

**Proposed:**
```csharp
public class RendererFactory
{
    private Dictionary<Type, IScreenRenderer> renderers;
    
    public T GetRenderer<T>() where T : IScreenRenderer;
    public void RegisterRenderer<T>(T renderer);
}
```

**Benefits:**
- Single point for renderer initialization
- Easier dependency injection
- Simplified testing with mock renderers

---

## Phase 3: Advanced Features (FUTURE)

### 3.1 Renderer Composition
**Status**: Not Started  
**Priority**: Low

**Goal**: Allow renderers to compose other renderers.

**Example:**
```csharp
public class CompositeRenderer : IScreenRenderer
{
    private List<IScreenRenderer> children;
    
    public void Render()
    {
        foreach (var child in children)
            child.Render();
    }
}
```

**Use Cases:**
- Split screen rendering
- Overlay systems (popups, tooltips)
- Picture-in-picture displays

### 3.2 Line Count Tracking
**Status**: Partially Implemented  
**Priority**: Medium

**Current State:**
- Field exists (`currentLineCount`)
- Not actively tracked during rendering

**Goal**: Accurately track lines rendered for:
- Automatic scrolling
- Layout calculations
- Performance monitoring

**Implementation:**
- Update render methods to increment `currentLineCount`
- Add scroll position management
- Implement viewport clipping

### 3.3 Render Caching
**Status**: Not Started  
**Priority**: Low

**Goal**: Cache rendered output for unchanged screens.

**Benefits:**
- Reduced CPU usage
- Smoother performance
- Faster screen transitions

**Challenges:**
- Invalidation strategy
- Memory usage
- Hover state changes

### 3.4 Animation Support
**Status**: Not Started  
**Priority**: Very Low

**Goal**: Add support for animated elements in renderers.

**Potential Features:**
- Text reveal animations (already exists in title screen)
- Fade in/out transitions
- Color pulsing for important elements
- Smooth scrolling

---

## Technical Debt & Cleanup

### Known Issues

1. **Null Reference Warning** (Minor)
   - Location: `CanvasUIManager.cs:1188`
   - Issue: Possible null `currentCharacter` parameter
   - Fix: Add null check or make parameter nullable

2. **Placeholder Render() Methods**
   - All renderers have empty `Render()` implementations
   - Currently using specific methods (e.g., `RenderMainMenu()`)
   - Should migrate to state-based rendering (Phase 2.1)

3. **Clickable Element Tracking**
   - Currently scattered across renderers
   - Could be centralized in `CanvasUIManager`
   - Would simplify interactive renderer implementations

### Potential Improvements

1. **Renderer Base Class**
   ```csharp
   public abstract class BaseRenderer : IScreenRenderer
   {
       protected int currentLineCount;
       protected readonly GameCanvasControl canvas;
       
       public virtual void Clear() { currentLineCount = 0; }
       public int GetLineCount() => currentLineCount;
       public abstract void Render();
   }
   ```
   - Reduces boilerplate
   - Enforces consistent behavior

2. **Typed Render Methods**
   - Return strongly-typed results
   - Include metrics (lines rendered, elements created)
   - Enable better testing

3. **Renderer Events**
   ```csharp
   event EventHandler<RenderCompleteEventArgs> RenderComplete;
   event EventHandler<ElementClickedEventArgs> ElementClicked;
   ```
   - Better decoupling
   - Easier to add logging/analytics

---

## Testing Strategy

### Current Testing
- Manual testing through gameplay
- Build verification (compiles without errors)

### Recommended Testing

1. **Unit Tests**
   - Test renderer methods independently
   - Verify correct element positioning
   - Test color markup parsing
   - Validate clickable element bounds

2. **Integration Tests**
   - Test renderer interaction with canvas
   - Verify layout manager integration
   - Test hover state updates

3. **Visual Regression Tests**
   - Capture rendered output
   - Compare against baseline screenshots
   - Detect unintended visual changes

---

## Migration Guide

### For Developers Adding New Screens

1. **Create Renderer Class**
   ```csharp
   public class MyNewRenderer : IInteractiveRenderer
   {
       private readonly GameCanvasControl canvas;
       private readonly List<ClickableElement> elements;
       private int currentLineCount;
       
       // Implement interface methods...
   }
   ```

2. **Register in CanvasUIManager**
   ```csharp
   private readonly Renderers.MyNewRenderer myRenderer;
   
   public CanvasUIManager(GameCanvasControl canvas)
   {
       this.myRenderer = new Renderers.MyNewRenderer(canvas, clickableElements);
   }
   ```

3. **Add Render Method**
   ```csharp
   public void RenderMyScreen(int x, int y, int width, int height, MyData data)
   {
       // Rendering logic here
   }
   ```

4. **Call from UI Manager**
   ```csharp
   RenderWithLayout(character, "MY SCREEN", (x, y, w, h) =>
   {
       myRenderer.RenderMyScreen(x, y, w, h, data);
   });
   ```

### Best Practices

1. **Use ColoredTextWriter** for all text with color markup
2. **Use PersistentLayoutManager** for consistent headers/sidebars
3. **Track clickable elements** in interactive renderers
4. **Follow naming conventions** (RenderXxxxScreen, RenderXxxxPanel)
5. **Document coordinate systems** (absolute vs. relative positioning)

---

## Performance Considerations

### Current Performance
- Build time: ~2 seconds
- No runtime performance issues
- Single warning (null reference)

### Optimization Opportunities

1. **Object Pooling**
   - Reuse `ClickableElement` instances
   - Pool color parsing results
   - Reduce GC pressure

2. **Batch Rendering**
   - Group similar canvas operations
   - Reduce individual draw calls
   - Improve frame rate

3. **Lazy Rendering**
   - Only render visible regions
   - Skip unchanged elements
   - Implement dirty regions

---

## Related Documentation

- [ARCHITECTURE.md](../01-Core/ARCHITECTURE.md) - Overall system architecture
- [CODE_PATTERNS.md](../01-Core/CODE_PATTERNS.md) - Coding standards
- [UI_SYSTEM.md](../05-Systems/UI_SYSTEM.md) - UI system documentation
- [COLOR_SYSTEM.md](../05-Systems/COLOR_SYSTEM.md) - Color markup system
- [TESTING_STRATEGY.md](../03-Quality/TESTING_STRATEGY.md) - Testing approach

---

## Change Log

### October 12, 2025 - Phase 1 Complete
- ✅ Created `IScreenRenderer` and `IInteractiveRenderer` interfaces
- ✅ Updated all renderers to implement interfaces
- ✅ Added `ClickableElement.Contains()` method
- ✅ Verified build success with zero errors
- ✅ Documented refactoring plan

### Future Updates
- Track Phase 2 (State Machine) progress here
- Document any architectural changes
- Update as new renderers are added

---

## Questions & Feedback

**For questions about this refactoring:**
- Review the interfaces in `IScreenRenderer.cs`
- Check existing renderer implementations for examples
- Consult [ARCHITECTURE.md](../01-Core/ARCHITECTURE.md) for overall design

**Contributing:**
- Follow the migration guide above
- Update this document when adding new renderers
- Document any deviations from the plan

