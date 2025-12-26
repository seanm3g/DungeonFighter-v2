# Undulate Feature Documentation

## ⚠️ Important Note

**This document describes COLOR PATTERN UNDULATION**, which is different from the brightness undulation system used in the Avalonia UI.

- **Color Pattern Undulation** (this document): Offsets color sequences in templates to create shimmering color effects
- **Brightness Undulation** (actively used): Global brightness pulsing effect - see [ANIMATION_SYSTEM.md](ANIMATION_SYSTEM.md)

The color pattern undulation feature documented here is **currently unused** in the Avalonia rendering system. The Avalonia UI uses brightness undulation instead, which is implemented in `BaseAnimationState` and its derived classes.

---

## Overview

The undulate feature creates shimmering effects by offsetting color patterns. When enabled, the color sequence starts at a different position, making colors appear to animate or "wave" across text.

**Status:** Documented but not actively used in current Avalonia implementation. Kept for potential future use.

---

## Architecture

### Data Flow

```
ColoredText
  ├─ Undulate: bool (enabled?)
  ├─ UndulateOffset: int (current offset)
  └─ GetSegments()
      └─ Template.Apply(text, offset)
          └─ ApplySequence(text, offset)
              └─ Start at colorIndex = offset
```

### How Offset Works

```csharp
// Template has colors: [R, O, Y]
int colorIndex = offset % ColorSequence.Count;

// offset=0: R, O, Y, R, O, Y, ...
// offset=1: O, Y, R, O, Y, R, ...
// offset=2: Y, R, O, Y, R, O, ...
```

---

## API Reference

### ColoredText Properties

```csharp
public class ColoredText
{
    // Enable/disable undulation
    public bool Undulate { get; set; }
    
    // Current offset (0 to ColorSequence.Count-1)
    public int UndulateOffset { get; set; }
}
```

### ColoredText Methods

```csharp
// Factory method with undulate parameter
public static ColoredText FromTemplate(string text, string templateName, bool undulate = false)

// Advance offset by 1 (wraps at sequence length)
public void AdvanceUndulation()

// Get segments with current offset applied
public List<ColoredSegment> GetSegments()
```

### ColorTemplate Methods

```csharp
// Apply template with offset
public List<ColoredSegment> Apply(string text, int offset = 0)

// Internal methods with offset support
private List<ColoredSegment> ApplySequence(string text, int offset = 0)
private List<ColoredSegment> ApplyAlternation(string text, int offset = 0)
```

### ColoredTextBuilder Methods

```csharp
// Add text with undulate parameter
public ColoredTextBuilder Add(string text, string templateName, bool undulate = false)
```

---

## Usage Patterns

### Pattern 1: Static Undulation

```csharp
// Create with fixed offset for subtle variation
var text1 = ColoredText.FromTemplate("Portal A", "ethereal", undulate: true);
text1.UndulateOffset = 0;

var text2 = ColoredText.FromTemplate("Portal B", "ethereal", undulate: true);
text2.UndulateOffset = 2;

// Same template, different offsets = visual variety
```

### Pattern 2: Frame-Based Animation

```csharp
private ColoredText shimmeringText;
private int frameCounter = 0;

void Initialize()
{
    shimmeringText = ColoredText.FromTemplate("Magic Spell", "arcane", undulate: true);
}

void Update()
{
    frameCounter++;
    
    // Advance every 5 frames
    if (frameCounter % 5 == 0)
    {
        shimmeringText.AdvanceUndulation();
    }
}

void Render()
{
    textWriter.WriteLineColored(shimmeringText, x, y);
}
```

### Pattern 3: Wave Effect

```csharp
// Create wave across multiple items
for (int i = 0; i < items.Count; i++)
{
    var text = ColoredText.FromTemplate(items[i].Name, "ocean", undulate: true);
    text.UndulateOffset = (globalOffset + i) % 4;  // Offset by index
    
    textWriter.WriteLineColored(text, x, y + i);
}
```

### Pattern 4: Conditional Undulation

```csharp
// Only special items shimmer
var text = ColoredText.FromTemplate(item.Name, template, undulate: item.IsSpecial);

if (text.Undulate)
{
    text.AdvanceUndulation();
}

textWriter.WriteLineColored(text, x, y);
```

---

## Best Practices

### Performance

```csharp
// ✅ GOOD: Shared offset for multiple items
int globalOffset = frameCounter / 5;
foreach (var item in shimmeringItems)
{
    item.UndulateOffset = globalOffset % item.Template.ColorSequence.Count;
}

// ❌ BAD: Individual timers per item (unnecessary overhead)
foreach (var item in shimmeringItems)
{
    item.AdvanceUndulation();  // Each item tracks own offset
}
```

### Visual Design

```csharp
// ✅ GOOD: Reserved for special elements
if (dungeon.Rarity >= Rarity.Legendary)
    text.Undulate = true;

// ❌ BAD: Everything shimmers (too distracting)
text.Undulate = true;  // For all text
```

### Animation Speed

```csharp
// ✅ GOOD: Match animation to content importance
int speed = item.Rarity switch
{
    Rarity.Epic => 5,       // Medium shimmer
    Rarity.Legendary => 3,  // Fast shimmer
    _ => 0                  // No shimmer
};

if (frameCounter % speed == 0)
    text.AdvanceUndulation();
```

---

## Testing

### Unit Tests

See `Code/Tests/UndulateEffectTest.cs`:

```csharp
// Test basic undulation
TestBasicUndulation()

// Test offset advancement
TestUndulationAdvance()

// Test multiple offsets
TestMultipleOffsets()

// Test with actual dungeon names
TestWithDungeonNames()

// Visual demonstration
VisualDemo()
```

### Integration Tests

```csharp
[Test]
public void Undulate_PreservesTextIntegrity()
{
    var text = ColoredText.FromTemplate("Test", "fiery", undulate: true);
    
    for (int i = 0; i < 10; i++)
    {
        var segments = text.GetSegments();
        var reconstructed = string.Concat(segments.Select(s => s.Text));
        
        Assert.Equal("Test", reconstructed);
        text.AdvanceUndulation();
    }
}

[Test]
public void Undulate_OffsetsCorrectly()
{
    var text = ColoredText.FromTemplate("AB", "fiery", undulate: true);
    // Template: [R, O, Y]
    
    // Offset 0: A→R, B→O
    text.UndulateOffset = 0;
    var segments0 = text.GetSegments();
    
    // Offset 1: A→O, B→Y
    text.UndulateOffset = 1;
    var segments1 = text.GetSegments();
    
    Assert.NotEqual(segments0[0].Foreground, segments1[0].Foreground);
}
```

---

## Implementation Notes

### Why Offset Instead of Rotation?

```csharp
// Option 1: Rotate the color sequence (allocates new list)
List<char> rotated = RotateList(ColorSequence, offset);  // ❌ Allocation

// Option 2: Offset the index (no allocation)
int colorIndex = (baseIndex + offset) % ColorSequence.Count;  // ✅ Efficient
```

**Decision:** Use offset for zero-allocation performance.

### Why Separate Undulate Property?

```csharp
// Option 1: Always apply offset (check on every render)
var segments = Template.Apply(text, UndulateOffset);  // ❌ Always check

// Option 2: Only apply when Undulate=true
int offset = Undulate ? UndulateOffset : 0;
var segments = Template.Apply(text, offset);  // ✅ Skip when disabled
```

**Decision:** Separate property for explicit control and performance.

### Wrapping Behavior

```csharp
// Offset wraps at sequence length
UndulateOffset = (UndulateOffset + 1) % Template.ColorSequence.Count;

// Examples with sequence length = 4:
// 0 → 1 → 2 → 3 → 0 → 1 → 2 → 3 → ...
```

---

## Future Enhancements

### Potential Features

1. **Undulate Speed Property**
   ```csharp
   public int UndulateSpeed { get; set; } = 1;  // Advance by N each frame
   ```

2. **Undulate Direction**
   ```csharp
   public enum UndulateDirection { Forward, Backward, PingPong }
   ```

3. **Per-Word Undulation**
   ```csharp
   public bool UndulateByWord { get; set; } = false;  // Offset each word separately
   ```

4. **Template-Level Default**
   ```json
   {
     "name": "shimmering",
     "shaderType": "sequence",
     "colors": ["M", "B", "Y"],
     "undulateByDefault": true
   }
   ```

---

## Related Documentation

- [Pattern-Based Color Architecture](PATTERN_BASED_COLOR_ARCHITECTURE.md)
- [Color System](COLOR_SYSTEM.md)
- [Color Templates Reference](../04-Reference/COLOR_TEMPLATES.md)

---

**Date:** October 12, 2025  
**Feature:** Undulate - Shimmering Color Patterns  
**Status:** ✅ PRODUCTION READY  
**Version:** 1.0

