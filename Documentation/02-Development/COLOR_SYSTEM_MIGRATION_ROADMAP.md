# Color System Migration Roadmap
**Date:** October 12, 2025  
**Status:** 📋 Planning  
**Priority:** HIGH

---

## Overview

This document provides a step-by-step roadmap for migrating from the current color markup system to the proposed structured `ColoredText` system.

**Goal:** Reliable, readable, maintainable color system that's easy for both humans and AI to work with.

---

## Why Migrate?

### Current Problems (Summary)
- ❌ Spacing issues from embedded color codes
- ❌ Hard to read (cryptic codes like `&R`, `&y`)
- ❌ Hard to modify (find all code locations)
- ❌ AI struggles to make changes reliably
- ❌ Multiple transformation passes (slow, error-prone)
- ❌ Content mixed with presentation

### After Migration Benefits
- ✅ No spacing issues
- ✅ Easy to read (`Colors.Red`, `Colors.Green`)
- ✅ Easy to modify (change colors by name)
- ✅ AI can work reliably
- ✅ Single-pass rendering
- ✅ Content separated from presentation

---

## Migration Phases

### Phase 0: Preparation (1-2 hours)
**Goal:** Set up foundation without breaking anything

**Tasks:**
1. ✅ Document current system problems (DONE)
2. ✅ Create redesign proposal (DONE)
3. ✅ Create migration roadmap (THIS DOCUMENT)
4. ⏳ Get approval to proceed

**Deliverables:**
- Documentation of problems and solutions
- Approval to proceed with migration

---

### Phase 1: Core Infrastructure (4-6 hours)
**Goal:** Add new classes alongside existing system

**Tasks:**

#### 1.1 Create `ColoredText` Class
```csharp
// Location: Code/UI/ColoredText.cs
public class ColoredText
{
    private List<TextSegment> _segments;
    
    public ColoredText Add(string text, ColorRGB? fg, ColorRGB? bg);
    public ColoredText Plain(string text);
    public ColoredText Red(string text);
    public ColoredText Green(string text);
    // ... more convenience methods
    
    public string GetPlainText();
    public int GetDisplayLength();
    public List<ColoredSegment> ToSegments();
}

public class TextSegment
{
    public string Text { get; set; }
    public ColorRGB? Foreground { get; set; }
    public ColorRGB? Background { get; set; }
}
```

#### 1.2 Create `Colors` Static Class
```csharp
// Location: Code/UI/Colors.cs
public static class Colors
{
    // Primary colors
    public static readonly ColorRGB Red = new(215, 66, 0);
    public static readonly ColorRGB DarkRed = new(166, 74, 46);
    public static readonly ColorRGB Green = new(0, 196, 32);
    // ... all game colors
    
    // Game-specific semantic colors
    public static readonly ColorRGB Damage = DarkRed;
    public static readonly ColorRGB Heal = Green;
    public static readonly ColorRGB Mana = Blue;
    
    // Utility methods
    public static ColorRGB? FromCode(char code);
    public static char? ToCode(ColorRGB color);
}
```

#### 1.3 Create `ColoredTextBuilder` (Optional)
```csharp
// Location: Code/UI/ColoredTextBuilder.cs
// Fluent API for building complex colored text
public class ColoredTextBuilder
{
    public ColoredTextBuilder WithText(string text);
    public ColoredTextBuilder WithColor(ColorRGB color);
    public ColoredTextBuilder WithTemplate(string templateName);
    public ColoredText Build();
}
```

#### 1.4 Add Backward Compatibility
```csharp
// Add to ColoredText class:
public static ColoredText FromMarkup(string markup)
{
    // Parse existing &X codes into ColoredText
    var segments = ColorParser.Parse(markup);
    return segments.ToColoredText();
}

public string ToMarkup()
{
    // Convert ColoredText back to &X codes for legacy systems
}
```

#### 1.5 Create Unit Tests
```csharp
// Location: Code/Tests/ColoredTextTests.cs
[TestFixture]
public class ColoredTextTests
{
    [Test] public void Add_PlainText_CreatesSegment();
    [Test] public void Add_WithColor_CreatesColoredSegment();
    [Test] public void GetPlainText_ReturnsUncoloredText();
    [Test] public void GetDisplayLength_IgnoresColorCodes();
    [Test] public void ToMarkup_GeneratesCorrectCodes();
    [Test] public void FromMarkup_ParsesCorrectly();
}
```

**Deliverables:**
- New color classes working alongside old system
- Full backward compatibility
- 100% test coverage for new classes

**Validation:**
- All existing tests still pass
- New classes have comprehensive tests
- Can convert between old and new formats

---

### Phase 2: Renderer Updates (2-3 hours)
**Goal:** Update renderers to support ColoredText directly

#### 2.1 Create `IColoredTextRenderer` Interface
```csharp
// Location: Code/UI/IColoredTextRenderer.cs
public interface IColoredTextRenderer
{
    void Render(ColoredText text, int x, int y);
    void RenderLine(ColoredText text);
    Size MeasureText(ColoredText text);
}
```

#### 2.2 Implement Canvas Renderer
```csharp
// Location: Code/UI/Avalonia/CanvasColoredTextRenderer.cs
public class CanvasColoredTextRenderer : IColoredTextRenderer
{
    public void Render(ColoredText text, int x, int y)
    {
        int currentX = x;
        foreach (var segment in text.ToSegments())
        {
            var color = segment.Foreground ?? Colors.White;
            _canvas.DrawText(currentX, y, segment.Text, color);
            currentX += segment.Text.Length;  // No spacing issues!
        }
    }
}
```

#### 2.3 Implement Console Renderer
```csharp
// Location: Code/UI/ConsoleColoredTextRenderer.cs
public class ConsoleColoredTextRenderer : IColoredTextRenderer
{
    public void Render(ColoredText text, int x, int y)
    {
        Console.SetCursorPosition(x, y);
        foreach (var segment in text.ToSegments())
        {
            SetConsoleColor(segment.Foreground);
            Console.Write(segment.Text);
        }
        Console.ResetColor();
    }
}
```

#### 2.4 Update UIManager
```csharp
// Add to UIManager:
public static void WriteLine(ColoredText text, UIMessageType type = UIMessageType.Standard)
{
    var renderer = GetRenderer();
    renderer.RenderLine(text);
}

// Keep old method for backward compatibility:
[Obsolete("Use ColoredText version instead")]
public static void WriteLine(string text, UIMessageType type = UIMessageType.Standard)
{
    WriteLine(ColoredText.FromMarkup(text), type);
}
```

**Deliverables:**
- Renderers support ColoredText directly
- UIManager supports both old and new formats
- No breaking changes to existing code

**Validation:**
- Rendering tests pass
- Performance equal or better
- No visual changes to existing displays

---

### Phase 3: High-Impact Migrations (8-12 hours)

#### 3.1 Migrate Combat System (4 hours)
**Files:**
- `Code/Combat/CombatResults.cs`
- `Code/UI/BlockDisplayManager.cs`
- `Code/UI/TextDisplayIntegration.cs`

**Before:**
```csharp
string damageText = $"{attacker.Name} hits {target.Name} for &R{damage}&y damage";
```

**After:**
```csharp
var damageText = new ColoredText()
    .Plain(attacker.Name)
    .Plain(" hits ")
    .Plain(target.Name)
    .Plain(" for ")
    .Red(damage.ToString())
    .Plain(" damage");
```

**Tests:**
- Combat messages display correctly
- No spacing issues
- Roll info stays white (not keyword colored)
- Damage numbers are red
- Miss messages are formatted correctly

#### 3.2 Migrate Title Screen (4 hours)
**Files:**
- `Code/UI/TitleScreen/TitleFrameBuilder.cs`
- `Code/UI/TitleScreen/TitleColorApplicator.cs` (can be removed)
- `Code/UI/TitleScreen/TitleArtAssets.cs`

**Before:**
```csharp
frameList.Add($"&k                                                  {line}");
```

**After:**
```csharp
var frame = new ColoredText()
    .Add(new string(' ', 50), Colors.Black)  // Background padding
    .Add(line, Colors.Gold);  // Title text
```

**Benefits:**
- Can see actual text
- Easy to adjust padding (change number, not counting spaces)
- Easy to change colors (change Colors.Gold to Colors.Red)
- No embedded color codes

**Tests:**
- Title animation displays correctly
- Colors transition smoothly
- DUNGEON is gold
- FIGHTER is red
- Background is black

#### 3.3 Migrate Roll Info / Stats Display (2 hours)
**Files:**
- `Code/UI/BlockDisplayManager.cs`

**Current Issue:**
Stats were being keyword colored, causing spacing issues.

**After:**
```csharp
// Roll info explicitly stays white:
var rollInfo = ColoredText.Plain($"(roll: {roll} | attack {atk} - {def} armor)");
UIManager.WriteLine(rollInfo, UIMessageType.RollInfo);
```

**No more:**
- Keyword coloring fights
- Spacing artifacts
- Uncertainty about what gets colored

**Tests:**
- Roll info displays in white
- No extra spaces
- All stats visible

**Deliverables:**
- Combat messages use ColoredText
- Title screen uses ColoredText
- Roll info uses ColoredText
- All tests passing

**Validation:**
- Play through 10 combats - no spacing issues
- Watch title animation - colors correct
- Check roll info - white text, no gaps

---

### Phase 4: Template System (4-6 hours)
**Goal:** Migrate template effects to ColoredText

#### 4.1 Create Template Applicators
```csharp
// Location: Code/UI/ColorTemplates.cs
public static class ColorTemplates
{
    public static ColoredText Fiery(string text)
    {
        var result = new ColoredText();
        var colors = new[] { Colors.Red, Colors.Orange, Colors.Gold, Colors.White };
        
        for (int i = 0; i < text.Length; i++)
        {
            result.Add(text[i].ToString(), colors[i % colors.Length]);
        }
        
        return result;
    }
    
    public static ColoredText Solid(string text, ColorRGB color)
    {
        return new ColoredText().Add(text, color);
    }
    
    public static ColoredText Alternating(string text, ColorRGB color1, ColorRGB color2)
    {
        var result = new ColoredText();
        for (int i = 0; i < text.Length; i++)
        {
            result.Add(text[i].ToString(), i % 2 == 0 ? color1 : color2);
        }
        return result;
    }
    
    // More templates...
}
```

#### 4.2 Create Template Parser
```csharp
// Parse {{template|text}} syntax:
public static ColoredText ParseTemplates(string input)
{
    var result = new ColoredText();
    var regex = new Regex(@"\{\{(\w+)\|([^}]+)\}\}");
    
    int lastIndex = 0;
    foreach (Match match in regex.Matches(input))
    {
        // Add text before template
        if (match.Index > lastIndex)
        {
            result.Add(input.Substring(lastIndex, match.Index - lastIndex));
        }
        
        // Apply template
        string templateName = match.Groups[1].Value;
        string text = match.Groups[2].Value;
        var templated = ColorTemplates.Apply(templateName, text);
        result.Merge(templated);
        
        lastIndex = match.Index + match.Length;
    }
    
    // Add remaining text
    if (lastIndex < input.Length)
    {
        result.Add(input.Substring(lastIndex));
    }
    
    return result;
}
```

#### 4.3 Migrate Item Display
**Files:**
- `Code/Items/ItemDisplay.cs`
- `Code/UI/MenuDisplay.cs`

**Before:**
```csharp
string itemName = "{{fiery|Blazing Sword}}";
// Gets expanded to: "&rB&Ol&Wa&Yz&ri&On&Wg&Y &rS&Ow&Wo&Yr&rd"
```

**After:**
```csharp
var itemName = ColorTemplates.Fiery("Blazing Sword");
// Direct ColoredText, no expansion needed
```

**Tests:**
- Item names display with templates
- Templates work correctly
- No spacing issues

**Deliverables:**
- Template system uses ColoredText
- All template effects work
- Item display migrated

---

### Phase 5: Keyword System (3-4 hours)
**Goal:** Make keyword coloring opt-in, not automatic

#### 5.1 Explicit Keyword Coloring
**Before (automatic, uncontrolled):**
```csharp
string text = "attack 15";
// Keyword system automatically colors "attack"
// Result: "{{damage|attack}} 15" → "&rattack 15" → spacing issues
```

**After (explicit, controlled):**
```csharp
var text = new ColoredText()
    .Damage("attack")  // Explicitly colored
    .Plain(" 15");
    
// Or if you want keyword coloring:
var text = ColoredText.WithKeywords("attack 15");
// Only use when you want keywords colored!
```

#### 5.2 Update KeywordColorSystem
```csharp
// Location: Code/UI/KeywordColorSystem.cs
public static ColoredText ApplyKeywords(string text)
{
    // Only called when explicitly requested
    // No more automatic application
}

// Add warning to old method:
[Obsolete("Use ColoredText.WithKeywords instead")]
public static string Colorize(string text)
{
    // Keep for backward compatibility
}
```

**Tests:**
- Keywords only colored when explicitly requested
- No automatic coloring of stats
- Roll info stays white

**Deliverables:**
- Keyword coloring is opt-in
- No more unexpected coloring
- All tests passing

---

### Phase 6: UI Menus (4-6 hours)
**Goal:** Migrate all menu systems

#### 6.1 Migrate Menu Items
**Files:**
- `Code/UI/Menu/*.cs`

**Before:**
```csharp
menuItems.Add(new MenuItem("&G[1]&y Attack", () => DoAttack()));
```

**After:**
```csharp
var label = new ColoredText()
    .Green("[1]")
    .Plain(" Attack");
menuItems.Add(new MenuItem(label, () => DoAttack()));
```

#### 6.2 Migrate Status Display
**Files:**
- `Code/UI/StatusDisplay.cs`

**Before:**
```csharp
WriteLine($"HP: &R{hp}&y/{maxHp}");
```

**After:**
```csharp
var hpDisplay = new ColoredText()
    .Plain("HP: ")
    .Red(hp.ToString())
    .Plain("/")
    .Plain(maxHp.ToString());
WriteLine(hpDisplay);
```

**Tests:**
- Menus display correctly
- Status bars work
- Character sheet displays

**Deliverables:**
- All menus use ColoredText
- Status display uses ColoredText
- No visual regressions

---

### Phase 7: Deprecation (2-3 hours)
**Goal:** Mark old system as obsolete

#### 7.1 Add Obsolete Attributes
```csharp
[Obsolete("Use ColoredText instead. Will be removed in v3.0")]
public static class ColorParser
{
    [Obsolete("Use ColoredText.FromMarkup")]
    public static List<ColoredSegment> Parse(string text) { ... }
    
    [Obsolete("Use ColoredText.GetDisplayLength")]
    public static int GetDisplayLength(string text) { ... }
}

[Obsolete("Use ColorTemplates instead. Will be removed in v3.0")]
public class ColorTemplate { ... }

[Obsolete("Use ColoredText.WithKeywords instead")]
public static class KeywordColorSystem { ... }
```

#### 7.2 Update Documentation
- Mark old docs as deprecated
- Add migration guide
- Update all examples to use new system

#### 7.3 Add Compiler Warnings
All old color code will now show warnings:
```
Warning CS0618: 'ColorParser.Parse' is obsolete: 'Use ColoredText.FromMarkup instead'
```

**Deliverables:**
- Old system marked obsolete
- Documentation updated
- Migration guide created

---

### Phase 8: Final Cleanup (2-3 hours)
**Goal:** Remove old system entirely

#### 8.1 Remove Obsolete Classes
- Delete `ColorParser.cs` (keep extension methods that are still used)
- Delete `ColorTemplate.cs` (old template system)
- Delete `KeywordColorSystem.cs` (old keyword system)

#### 8.2 Clean Up Data Files
- Remove `ColorTemplates.json` (migrate to code)
- Remove `KeywordColorGroups.json` (migrate to code)
- Update `COLOR_*.md` guides

#### 8.3 Final Testing
- Run full test suite
- Manual testing of all screens
- Performance testing
- Verify no spacing issues anywhere

**Deliverables:**
- Old system removed
- All tests passing
- No regressions
- Clean codebase

---

## Success Criteria

At the end of migration, we should have:

- ✅ **No spacing issues** - Test all text displays
- ✅ **Readable code** - Can understand colored text without running
- ✅ **Easy to modify** - Change colors in <5 minutes
- ✅ **AI-friendly** - AI can reliably modify colors
- ✅ **Good performance** - Equal or better than old system
- ✅ **100% test coverage** - All color code tested
- ✅ **Zero regressions** - All displays work as before

---

## Risk Mitigation

### Risk: Breaking existing displays
**Mitigation:** 
- Migrate incrementally
- Keep backward compatibility during migration
- Test each phase thoroughly

### Risk: Performance degradation
**Mitigation:**
- Benchmark before and after
- Optimize ColoredText operations
- Use object pooling if needed

### Risk: Missed migration spots
**Mitigation:**
- Use compiler warnings (Obsolete attribute)
- Grep for color codes (`&R`, `&y`, etc.)
- Thorough testing

### Risk: User-created content breaks
**Mitigation:**
- Keep backward compatibility in data loading
- Auto-convert old format to new
- Provide migration tool for mods

---

## Timeline Estimate

| Phase | Estimate | Cumulative |
|-------|----------|------------|
| Phase 0: Preparation | 1-2 hours | 2 hours |
| Phase 1: Core Infrastructure | 4-6 hours | 8 hours |
| Phase 2: Renderer Updates | 2-3 hours | 11 hours |
| Phase 3: High-Impact Migrations | 8-12 hours | 23 hours |
| Phase 4: Template System | 4-6 hours | 29 hours |
| Phase 5: Keyword System | 3-4 hours | 33 hours |
| Phase 6: UI Menus | 4-6 hours | 39 hours |
| Phase 7: Deprecation | 2-3 hours | 42 hours |
| Phase 8: Final Cleanup | 2-3 hours | 45 hours |

**Total Estimate:** 40-45 hours (~1 week of full-time work)

**Incremental Approach:** Can be done in 2-hour sessions over 3-4 weeks

---

## Decision Points

### ✅ Proceed with Migration?
**Recommendation:** YES
- Problems are fundamental architectural issues
- Will keep recurring if not fixed
- Migration is feasible and low-risk
- Benefits outweigh costs

### ✅ Full Migration or Hybrid?
**Recommendation:** FULL MIGRATION
- Clean break is better long-term
- Hybrid causes confusion (two systems)
- Incremental approach makes it safe

### ✅ When to Start?
**Recommendation:** After approval
- Preparation is complete
- Roadmap is clear
- Can start Phase 1 immediately

---

## Next Steps

1. **Get Approval** - Review proposal with stakeholders
2. **Start Phase 1** - Implement core ColoredText classes
3. **Validate Phase 1** - Ensure backward compatibility works
4. **Proceed to Phase 2** - Update renderers
5. **Continue incrementally** - One phase at a time

---

**Status:** 📋 Ready for approval  
**Estimated Effort:** 40-45 hours  
**Risk Level:** LOW (incremental approach)  
**Priority:** HIGH (affects quality and maintainability)

---

**Related Documents:**
- [Color System Redesign Proposal](COLOR_SYSTEM_REDESIGN_PROPOSAL.md)
- [Color System Problems - Examples](COLOR_SYSTEM_PROBLEMS_EXAMPLES.md)
- [Spacing Issues Investigation](../03-Quality/SPACING_ISSUES_INVESTIGATION.md)

