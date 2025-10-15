# Pattern-Based Color Architecture

## Overview

The color system now uses **pattern-based separation** where text and color information remain separate until the final rendering moment. This eliminates parsing issues and text corruption.

---

## Core Concept

### Separation of Concerns

```
TEXT (content)  +  PATTERN (style)  =  RENDERING (visual)
     ↓                    ↓                    ↓
"Celestial..."      "astral" template    Colored segments
```

**Key Principle:** Text is never modified or embedded with color codes. The pattern is applied dynamically at render time.

---

## Architecture

### Data Flow

```
┌─────────────────────────────────────────────────────┐
│                 APPLICATION LAYER                    │
│  Creates ColoredText with text + pattern reference  │
└─────────────────────┬───────────────────────────────┘
                      │
                      ↓
┌─────────────────────────────────────────────────────┐
│                 PRESENTATION LAYER                   │
│      ColoredText.GetSegments() applies pattern      │
└─────────────────────┬───────────────────────────────┘
                      │
                      ↓
┌─────────────────────────────────────────────────────┐
│                 RENDERING LAYER                      │
│        ColoredTextWriter renders segments            │
└─────────────────────┬───────────────────────────────┘
                      │
                      ↓
┌─────────────────────────────────────────────────────┐
│                   CANVAS LAYER                       │
│       GameCanvasControl draws to screen             │
└─────────────────────────────────────────────────────┘
```

---

## Components

### 1. ColoredText

**Purpose:** Represents text with an associated color pattern

**Structure:**
```csharp
public class ColoredText
{
    public string Text { get; set; }              // Clean text
    public ColorTemplate? Template { get; set; }  // Pattern to apply
    public char? SimpleColorCode { get; set; }    // Simple color option
    
    public List<ColoredSegment> GetSegments()     // Apply pattern
}
```

**Responsibility:** Store text and pattern reference, apply pattern on demand

### 2. ColorTemplate

**Purpose:** Defines how colors are applied to text

**Types:**
- `Sequence` - Each character gets next color in sequence
- `Alternation` - Characters alternate between colors
- `Solid` - All text uses same color

**Example:**
```json
{
  "name": "astral",
  "shaderType": "sequence",
  "colors": ["M", "B", "Y", "C"]
}
```

### 3. ColoredTextBuilder

**Purpose:** Build multi-part colored text with different patterns

**Usage:**
```csharp
var builder = new ColoredTextBuilder()
    .Add("Part 1", 'R')           // Simple color
    .Add("Part 2", "fiery")       // Template
    .Add("Part 3");               // Default color

var segments = builder.Build();
```

### 4. ColoredTextWriter

**Purpose:** Render colored text and segments to canvas

**Methods:**
```csharp
// New preferred methods
WriteLineColored(ColoredText text, int x, int y)
RenderSegments(List<ColoredSegment> segments, int x, int y)

// Legacy method (still supported)
WriteLineColored(string markup, int x, int y)
```

---

## Pattern Application

### Template Application Process

1. **Text:** `"Celestial Observatory"`
2. **Template:** `astral` with colors `[M, B, Y, C]`
3. **Application:**
   ```
   C → M (magenta)
   e → B (blue)
   l → Y (white)
   e → C (cyan)
   s → M (magenta)
   t → B (blue)
   i → Y (white)
   a → C (cyan)
   l → M (magenta)
     → (no color)
   O → B (blue)
   ...
   ```
4. **Result:** 21 segments with appropriate colors

### Whitespace Handling

Whitespace characters receive NO color:
```csharp
if (char.IsWhiteSpace(text[i]))
{
    segments.Add(new ColoredSegment(text[i].ToString()));  // No color
}
```

This preserves text integrity and avoids rendering issues.

---

## Usage Patterns

### Pattern 1: Simple Single-Color Text

```csharp
// Create
var text = ColoredText.FromColor("HP: 100", 'R');

// Render
textWriter.WriteLineColored(text, x, y);
```

### Pattern 2: Template-Based Text

```csharp
// Create
var text = ColoredText.FromTemplate("Blazing Sword", "fiery");

// Render
textWriter.WriteLineColored(text, x, y);
```

### Pattern 3: Multi-Part Composite Text

```csharp
// Build
var builder = new ColoredTextBuilder()
    .Add("[1] ", 'y')
    .Add("Celestial Observatory", "astral")
    .Add(" (lvl 5)", 'Y');

// Render
var segments = builder.Build();
textWriter.RenderSegments(segments, x, y);
```

### Pattern 4: Direct Segment Rendering

```csharp
// Get segments
var segments = coloredText.GetSegments();

// Optionally process segments
var optimized = OptimizeSegments(segments);

// Render
textWriter.RenderSegments(optimized, x, y);
```

---

## Benefits

### 1. No Text Corruption

**Before:** Color codes embedded in text could be misparsed
```
"&MC&Be&Yl..." → parsing errors → corrupted text
```

**After:** Text stays clean, pattern applied at render time
```
"Celestial..." + astral → segments → rendering
```

### 2. Performance

**Fewer Operations:**
- No string concatenation for markup
- No regex matching for embedded codes
- No double conversion (segments → codes → segments)
- Single template application

**Fewer Allocations:**
- No intermediate markup strings
- No color code strings
- Direct segment creation

### 3. Clarity

**Explicit Intent:**
```csharp
// OLD: What template is this?
var text = ColorParser.Colorize(name, templateName);

// NEW: Clear pattern reference
var text = ColoredText.FromTemplate(name, "astral");
```

### 4. Type Safety

**Compile-Time Checks:**
```csharp
// OLD: String, any content
string text = "...";

// NEW: Typed structure
ColoredText text = ...;
```

---

## Best Practices

### Do's ✅

1. **Use ColoredText for new code:**
   ```csharp
   var text = ColoredText.FromTemplate(dungeon.Name, "astral");
   ```

2. **Use ColoredTextBuilder for multi-part:**
   ```csharp
   var builder = new ColoredTextBuilder()
       .Add(prefix, 'y')
       .Add(content, template)
       .Add(suffix, 'Y');
   ```

3. **Keep text clean:**
   ```csharp
   text.Text = "Celestial Observatory";  // No codes!
   ```

4. **Apply patterns at render time:**
   ```csharp
   var segments = text.GetSegments();  // Apply now
   textWriter.RenderSegments(segments, x, y);
   ```

### Don'ts ❌

1. **Don't embed color codes in ColoredText.Text:**
   ```csharp
   // WRONG
   text.Text = "&RCelestial&Y Observatory";
   ```

2. **Don't convert to markup strings:**
   ```csharp
   // WRONG
   string markup = ColorParser.Colorize(text.Text, template);
   ```

3. **Don't parse already-colored text:**
   ```csharp
   // WRONG
   var segments = ColorParser.Parse(coloredText.ToString());
   ```

---

## Migration Strategy

### Phase 1: Critical Systems (Completed ✅)

- ✅ Dungeon selection rendering
- ✅ ColoredTextWriter updates

### Phase 2: High-Traffic Areas (Next)

- ⏳ Combat log rendering
- ⏳ Item display
- ⏳ Character sheet

### Phase 3: Complete Migration

- ⏳ All menu systems
- ⏳ All text rendering
- ⏳ Deprecate old string-based API

---

## Testing

### Unit Tests

```csharp
[Test]
public void ColoredText_PreservesTextIntegrity()
{
    var text = ColoredText.FromTemplate("Test", "fiery");
    var segments = text.GetSegments();
    var reconstructed = string.Concat(segments.Select(s => s.Text));
    
    Assert.Equal("Test", reconstructed);
}

[Test]
public void ColoredTextBuilder_CombinesCorrectly()
{
    var builder = new ColoredTextBuilder()
        .Add("Hello", 'R')
        .Add(" ")
        .Add("World", 'B');
    
    var segments = builder.Build();
    var text = string.Concat(segments.Select(s => s.Text));
    
    Assert.Equal("Hello World", text);
}
```

### Integration Tests

```csharp
[Test]
public void DungeonRenderer_DisplaysCorrectly()
{
    var dungeon = new Dungeon { Name = "Test Dungeon", Theme = "Astral" };
    
    // Build colored text
    var builder = new ColoredTextBuilder()
        .Add("[1] ", 'y')
        .Add(dungeon.Name, "astral")
        .Add(" (lvl 5)", 'Y');
    
    // Render
    var segments = builder.Build();
    
    // Verify segments
    Assert.True(segments.Count > 0);
    Assert.Equal("[1] Test Dungeon (lvl 5)", 
        string.Concat(segments.Select(s => s.Text)));
}
```

---

## Related Documentation

- [COLOR_SYSTEM.md](COLOR_SYSTEM.md) - Overall color system
- [COLOR_TEMPLATES.md](../04-Reference/COLOR_TEMPLATES.md) - Template definitions
- [RENDERING_ARCHITECTURE.md](RENDERING_ARCHITECTURE.md) - Rendering pipeline

---

**Date:** October 12, 2025  
**Status:** ✅ ACTIVE ARCHITECTURE  
**Version:** 2.0 (Pattern-Based)

