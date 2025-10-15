# Undulate Feature Summary

## What It Does

The **undulate** parameter creates a **shimmering effect** by offsetting the color pattern by one (or more) characters. This makes colors appear to "wave" or "shimmer" across text.

---

## Quick Examples

### Static Offset (Single Shimmer Position)

```csharp
// Create text with undulate enabled
var text = ColoredText.FromTemplate("Celestial Observatory", "astral", undulate: true);

// Set specific offset (shifts colors by 2)
text.UndulateOffset = 2;

// Render
textWriter.WriteLineColored(text, x, y);
```

### Animated Shimmer (Continuous Movement)

```csharp
// Create text with undulate enabled
var text = ColoredText.FromTemplate("Shimmering Portal", "crystalline", undulate: true);

// In your update loop:
void Update()
{
    text.AdvanceUndulation();  // Shifts offset by 1 each frame
    Render();
}
```

### Multi-Part Text with Undulate

```csharp
var builder = new ColoredTextBuilder()
    .Add("[1] ", 'y')                                  // Static
    .Add("Mystical Portal", "ethereal", undulate: true)  // Shimmering!
    .Add(" (lvl 10)", 'Y');                            // Static

var segments = builder.Build();
textWriter.RenderSegments(segments, x, y);
```

---

## Visual Example

### Pattern: `[R, O, Y]` (Red, Orange, Yellow)
### Text: `"HELLO"`

**Offset = 0 (Normal):**
```
H → R (Red)
E → O (Orange)
L → Y (Yellow)
L → R (Red)
O → O (Orange)
```

**Offset = 1 (Undulate):**
```
H → O (Orange)  ← Started at position 1
E → Y (Yellow)
L → R (Red)
L → O (Orange)
O → Y (Yellow)
```

**Offset = 2 (Undulate):**
```
H → Y (Yellow)  ← Started at position 2
E → R (Red)
L → O (Orange)
L → Y (Yellow)
O → R (Red)
```

Advancing the offset each frame creates a flowing, shimmering effect!

---

## Use Cases

### 1. Highlight Special Dungeons

```csharp
bool isLegendary = dungeon.Rarity == DungeonRarity.Legendary;

var builder = new ColoredTextBuilder()
    .Add($"[{num}] ", 'y')
    .Add(dungeon.Name, themeName, undulate: isLegendary)  // Legendary shimmers!
    .Add($" (lvl {level})", 'Y');
```

### 2. Animate Menu Selection

```csharp
void RenderMenu()
{
    for (int i = 0; i < menuItems.Count; i++)
    {
        // Selected item shimmers
        menuItems[i].Undulate = (i == selectedIndex);
        if (menuItems[i].Undulate)
            menuItems[i].AdvanceUndulation();
        
        textWriter.WriteLineColored(menuItems[i], x, y + i * 2);
    }
}
```

### 3. Legendary Item Display

```csharp
if (item.Rarity >= Rarity.Epic)
{
    var text = ColoredText.FromTemplate(item.Name, "golden", undulate: true);
    text.AdvanceUndulation();  // Animated shimmer
    return text;
}
```

---

## Key Methods

### ColoredText

```csharp
// Create with undulate enabled
ColoredText.FromTemplate(text, templateName, undulate: true);

// Enable/disable undulate
text.Undulate = true;

// Set specific offset
text.UndulateOffset = 2;

// Advance offset by 1 (for animation)
text.AdvanceUndulation();
```

### ColoredTextBuilder

```csharp
// Add part with undulate
builder.Add(text, templateName, undulate: true);
```

---

## Animation Speeds

### Slow Shimmer (Every 10 Frames)

```csharp
if (frameCounter % 10 == 0)
    text.AdvanceUndulation();
```

### Medium Shimmer (Every 5 Frames)

```csharp
if (frameCounter % 5 == 0)
    text.AdvanceUndulation();
```

### Fast Shimmer (Every Frame)

```csharp
text.AdvanceUndulation();
```

---

## Performance

**Impact:** Negligible

- Offset is just an integer counter
- `AdvanceUndulation()` is O(1)
- Rendering cost is same as normal coloring
- No additional allocations

You can have hundreds of undulating texts with minimal performance impact!

---

## Best Practices

✅ **Use for special items/dungeons** - Draws attention  
✅ **Use for selected menu items** - Clear visual feedback  
✅ **Control animation speed** - Match game feel  
✅ **Apply selectively** - Don't overuse  

❌ **Don't use on all text** - Too distracting  
❌ **Don't use with solid colors** - No effect  
❌ **Don't use for normal UI text** - Reserve for special cases  

---

## Technical Details

### How It Works

1. Template has color sequence: `[M, B, Y, C]`
2. Without undulate: Start at index 0
3. With undulate (offset=1): Start at index 1
4. `AdvanceUndulation()`: Increment offset, wrap at sequence length
5. Colors "shift" across the text

### Pattern Compatibility

| Shader Type | Undulate Support |
|-------------|------------------|
| Sequence | ✅ Full support |
| Alternation | ✅ Full support |
| Solid | ❌ No effect (single color) |

---

## Implementation Details

### Files Changed

✅ `Code/UI/ColoredText.cs`
- Added `Undulate` property
- Added `UndulateOffset` property
- Added `AdvanceUndulation()` method
- Updated `GetSegments()` to pass offset

✅ `Code/UI/ColorTemplate.cs`
- Added `offset` parameter to `Apply()`
- Updated `ApplySequence()` to start at offset
- Updated `ApplyAlternation()` to start at offset

✅ `Code/Tests/UndulateEffectTest.cs`
- Comprehensive test suite
- Visual demonstrations

---

## Examples in Production

### Dungeon Selection (Example)

```csharp
// In DungeonRenderer.cs
var builder = new ColoredTextBuilder()
    .Add($"[{i + 1}] ", 'y')
    .Add(dungeon.Name, themeName, undulate: dungeon.IsSpecial)
    .Add($" (lvl {dungeon.MinLevel})", 'Y');
```

Result:
```
[1] Crystal Caverns (lvl 5)        ← Normal (no shimmer)
[2] Celestial Observatory (lvl 7)  ← Special (shimmering!)
[3] Ocean Depths (lvl 3)           ← Normal
```

---

## Summary

✅ **Feature:** Undulate - Color pattern offset for shimmering  
✅ **Usage:** `undulate: true` parameter  
✅ **Animation:** `AdvanceUndulation()` method  
✅ **Performance:** Negligible overhead  
✅ **Use Cases:** Special items, selection, legendary equipment  
✅ **Status:** Implemented and tested  

The undulate feature adds dynamic visual interest without text corruption or parsing issues, thanks to the pattern-based color system!

---

**Date:** October 12, 2025  
**Feature Status:** ✅ FULLY IMPLEMENTED  
**Build Status:** ✅ COMPILES SUCCESSFULLY  
**Documentation:** ✅ COMPLETE

