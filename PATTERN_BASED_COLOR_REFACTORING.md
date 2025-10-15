# Pattern-Based Color System Refactoring

## Overview

Refactored the color system to **keep text and color patterns separate** until the final rendering moment. This eliminates the text corruption issues caused by weaving color codes into strings.

---

## The Problem (Before)

### Old Architecture: Embedded Color Codes

```
Text: "Celestial Observatory"
  ↓
Markup: {{astral|Celestial Observatory}}
  ↓
Expanded: &MC&Be&Yl&Ce&Ms&Bt&Bi&Ba&Yl &MO&Bb&Bs&Ce&Mr&Yv&Ca&Mt&Bo&Yr&Cy
  ↓
Parsed: 21 segments
  ↓
Rendered: 21 separate render calls
```

**Issues:**
- Double conversion (segments → color codes → segments)
- Color codes embedded in text strings
- Parsing errors cause character corruption
- `FindColorCode()` can return null, losing characters
- Whitespace handling special cases
- Position miscalculation from many tiny segments

---

## The Solution (After)

### New Architecture: Pattern-Based Separation

```
Text: "Celestial Observatory"  (clean string)
Pattern: "astral" template reference (metadata)
  ↓
At render time only:
  Template.Apply(text) → segments
  ↓
Rendered: segments applied directly
```

**Benefits:**
- ✅ Text stays clean and uncorrupted
- ✅ Pattern is just metadata
- ✅ No parsing or conversion
- ✅ No embedded color codes
- ✅ Applied once at render time
- ✅ No character loss possible

---

## New Data Structure

### ColoredText Class

Represents text with a color pattern reference:

```csharp
public class ColoredText
{
    public string Text { get; set; }              // Clean text, no codes
    public ColorTemplate? Template { get; set; }  // Pattern to apply
    public char? SimpleColorCode { get; set; }    // Or simple single color
}
```

**Creation Methods:**

```csharp
// From template
var text = ColoredText.FromTemplate("Celestial Observatory", "astral");

// From simple color
var text = ColoredText.FromColor("HP: 100", 'R');

// Plain text (default color)
var text = ColoredText.Plain("Press any key");

// Implicit from string
ColoredText text = "Hello World";
```

**Rendering:**

```csharp
// Pattern is applied at render time ONLY
var segments = coloredText.GetSegments();
// Segments rendered directly to canvas
```

### ColoredTextBuilder

For building multi-part colored text:

```csharp
var builder = new ColoredTextBuilder()
    .Add("[1] ", 'y')                    // Grey bracket
    .Add("Celestial Observatory", "astral")  // Themed name
    .Add(" (lvl 5)", 'Y');               // White level

var segments = builder.Build();  // Get all segments
textWriter.RenderSegments(segments, x, y);
```

---

## Updated Rendering System

### ColoredTextWriter

Added overload for ColoredText:

```csharp
// OLD WAY: String with embedded color codes
public void WriteLineColored(string message, int x, int y)
{
    // Parses color codes from string
    // Subject to corruption
}

// NEW WAY: ColoredText with pattern reference
public void WriteLineColored(ColoredText coloredText, int x, int y)
{
    var segments = coloredText.GetSegments();  // Apply pattern
    RenderSegments(segments, x, y);            // Render directly
}

// DIRECT: Pre-built segments
public void RenderSegments(List<ColoredSegment> segments, int x, int y)
{
    // Renders segments directly to canvas
}
```

---

## Example Usage

### Dungeon Selection (Before)

```csharp
// OLD: Embedded color codes
string templateName = GetDungeonThemeTemplate(dungeon.Theme);
string coloredName = ColorParser.Colorize(dungeon.Name, templateName);
string displayText = $"&y[{i + 1}] {coloredName} &Y(lvl {dungeon.MinLevel})";

// Parse and render (subject to corruption)
textWriter.WriteLineColored(displayText, x + 4, y);
```

**Problems:**
- `coloredName` contains: `{{astral|Celestial Observatory}}`
- Gets expanded to: `&MC&Be&Yl&Ce...` (42+ characters of codes!)
- Parsing this can fail or corrupt text

### Dungeon Selection (After)

```csharp
// NEW: Pattern-based approach
var builder = new ColoredTextBuilder()
    .Add($"[{i + 1}] ", 'y')
    .Add(dungeon.Name, GetDungeonThemeTemplate(dungeon.Theme))
    .Add($" (lvl {dungeon.MinLevel})", 'Y');

var segments = builder.Build();
textWriter.RenderSegments(segments, x + 4, y);
```

**Benefits:**
- Text stays clean: `"[1] Celestial Observatory (lvl 5)"`
- Pattern applied once at render time
- No parsing, no conversion, no corruption
- Direct segment rendering

---

## Migration Guide

### For Simple Text

**Before:**
```csharp
string text = ColorParser.Colorize("Damage", "R");
textWriter.WriteLineColored(text, x, y);
```

**After:**
```csharp
var text = ColoredText.FromColor("Damage", 'R');
textWriter.WriteLineColored(text, x, y);
```

### For Template-Based Text

**Before:**
```csharp
string text = ColorParser.Colorize("Blazing Sword", "fiery");
textWriter.WriteLineColored(text, x, y);
```

**After:**
```csharp
var text = ColoredText.FromTemplate("Blazing Sword", "fiery");
textWriter.WriteLineColored(text, x, y);
```

### For Multi-Part Text

**Before:**
```csharp
string text = $"&y[{num}] {ColorParser.Colorize(name, "rare")} &Y({stats})";
textWriter.WriteLineColored(text, x, y);
```

**After:**
```csharp
var segments = new ColoredTextBuilder()
    .Add($"[{num}] ", 'y')
    .Add(name, "rare")
    .Add($" ({stats})", 'Y')
    .Build();
textWriter.RenderSegments(segments, x, y);
```

---

## Implementation Details

### Pattern Application Flow

1. **Creation:**
   ```csharp
   var text = ColoredText.FromTemplate("Test", "fiery");
   // Stores: Text = "Test", Template = fiery template
   ```

2. **Rendering:**
   ```csharp
   var segments = text.GetSegments();
   // Calls: Template.Apply("Test")
   // Returns: List<ColoredSegment> directly
   ```

3. **Canvas Drawing:**
   ```csharp
   foreach (var segment in segments)
   {
       canvas.AddText(x + pos, y, segment.Text, segment.Foreground);
       pos += segment.Text.Length;
   }
   ```

### No Intermediate Conversions

The new system eliminates ALL intermediate conversions:

```
OLD:
Text → Template Markup → Template Applied → Color Codes → Parsed → Segments → Rendered

NEW:
Text + Pattern → Template Applied → Segments → Rendered
```

Removed steps:
- ❌ Template markup creation (`{{...}}`)
- ❌ Color code generation (`&MC&Be...`)
- ❌ Color code parsing
- ❌ Double segment creation

---

## Performance Benefits

### Fewer Operations

**Before:**
- String concatenation for markup
- Regex matching for templates
- Template application
- Color code lookup
- String building for codes
- Color code parsing
- Segment creation (twice!)

**After:**
- Template application
- Segment creation (once!)

### Fewer String Allocations

**Before (for "Celestial Observatory"):**
- Original text: 21 chars
- Template markup: `{{astral|Celestial Observatory}}` = 32 chars
- Expanded color codes: `&MC&Be&Yl...` = ~50+ chars
- **Total: 100+ characters created**

**After:**
- Original text: 21 chars
- **Total: 21 characters used**

---

## Backward Compatibility

### Old String-Based API Still Works

The old `WriteLineColored(string)` method still exists:

```csharp
// This still works for combat log, etc.
textWriter.WriteLineColored("&RDamage: 10&y", x, y);
```

**When to use old API:**
- Combat log with embedded codes
- Legacy code not yet refactored
- Simple one-off messages

**When to use new API:**
- Dungeon/menu rendering
- Any template-based coloring
- Multi-part colored text
- Performance-critical paths

---

## Testing

### Verification

```csharp
// Create colored text
var text = ColoredText.FromTemplate("Celestial Observatory", "astral");

// Get segments
var segments = text.GetSegments();

// Verify text integrity
string reconstructed = string.Concat(segments.Select(s => s.Text));
Assert.Equal("Celestial Observatory", reconstructed);

// Verify segment count (one per non-space character + space)
Assert.Equal(21, segments.Count);

// Verify colors applied
Assert.All(segments.Where(s => !char.IsWhiteSpace(s.Text[0])), 
    s => Assert.NotNull(s.Foreground));
```

### Test Cases

1. ✅ Short names: "Cave", "Ocean"
2. ✅ Medium names: "Crystal Caverns"
3. ✅ Long names: "Celestial Observatory"
4. ✅ With spaces: "Ancient Library"
5. ✅ All themes: Astral, Crystal, Ocean, etc.

---

## Future Enhancements

### Possible Optimizations

1. **Segment Caching:**
   ```csharp
   private List<ColoredSegment>? cachedSegments;
   
   public List<ColoredSegment> GetSegments()
   {
       if (cachedSegments == null)
           cachedSegments = Template.Apply(Text);
       return cachedSegments;
   }
   ```

2. **Batch Rendering:**
   Combine consecutive same-color segments before rendering

3. **Color Interpolation:**
   Smooth color transitions for gradients

4. **Animation Support:**
   Time-based color pattern animation

---

## Summary

✅ **Problem Solved:** Text corruption from embedded color codes  
✅ **Architecture:** Separation of text and color patterns  
✅ **Performance:** Fewer conversions and allocations  
✅ **Maintainability:** Cleaner, more explicit code  
✅ **Backward Compatible:** Old API still works  

The pattern-based approach is now the **preferred method** for all new colored text rendering.

---

## Files Changed

- ✅ `Code/UI/ColoredText.cs` - New data structure
- ✅ `Code/UI/Avalonia/Renderers/ColoredTextWriter.cs` - Updated rendering
- ✅ `Code/UI/Avalonia/Renderers/DungeonRenderer.cs` - Migrated to new API

## Files to Migrate (Future)

- ⏳ Combat log rendering
- ⏳ Item display
- ⏳ Character sheet
- ⏳ Menu systems

---

**Date:** October 12, 2025  
**Status:** ✅ IMPLEMENTED AND TESTED  
**Impact:** Eliminates text corruption in dungeon selection and all future template-based rendering

