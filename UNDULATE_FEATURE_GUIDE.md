# Undulate Feature Guide

## Overview

The **undulate** feature creates a shimmering effect by offsetting color patterns. When enabled, the color sequence starts at a different position, making colors appear to "wave" across the text.

---

## How It Works

### Normal Pattern

```
Pattern: [R, O, Y, R, O, Y, ...]
Text: "HELLO"

H → R (Red)
E → O (Orange)
L → Y (Yellow)
L → R (Red)
O → O (Orange)
```

### With Undulate (Offset = 1)

```
Pattern: [R, O, Y, R, O, Y, ...]
Text: "HELLO"
Offset: 1

H → O (Orange)  ← Started at position 1
E → Y (Yellow)
L → R (Red)
L → O (Orange)
O → Y (Yellow)
```

### With Undulate (Offset = 2)

```
Pattern: [R, O, Y, R, O, Y, ...]
Text: "HELLO"
Offset: 2

H → Y (Yellow)  ← Started at position 2
E → R (Red)
L → O (Orange)
L → Y (Yellow)
O → R (Red)
```

---

## Usage

### Static Undulation (Single Offset)

```csharp
// Create text with undulate enabled
var text = ColoredText.FromTemplate("Celestial Observatory", "astral", undulate: true);

// Set a specific offset
text.UndulateOffset = 1;

// Render (colors are offset by 1)
textWriter.WriteLineColored(text, x, y);
```

### Animated Undulation (Continuous Shimmer)

```csharp
// Create text with undulate enabled
var text = ColoredText.FromTemplate("Shimmering Portal", "crystalline", undulate: true);

// In your update/render loop:
void Update()
{
    // Advance the offset each frame (or every N frames for slower shimmer)
    text.AdvanceUndulation();
    
    // Render with new offset
    textWriter.WriteLineColored(text, x, y);
}
```

### With ColoredTextBuilder

```csharp
// Build multi-part text with undulation
var builder = new ColoredTextBuilder()
    .Add("[1] ", 'y')                                    // Static grey
    .Add("Mystical Portal", "crystalline", undulate: true)  // Undulating!
    .Add(" (lvl 10)", 'Y');                             // Static white

var segments = builder.Build();
textWriter.RenderSegments(segments, x, y);
```

---

## Animation Patterns

### Slow Shimmer

```csharp
private int frameCounter = 0;
private ColoredText shimmeringText = ColoredText.FromTemplate("Magic Spell", "arcane", undulate: true);

void Update()
{
    frameCounter++;
    
    // Advance every 10 frames for slow shimmer
    if (frameCounter % 10 == 0)
    {
        shimmeringText.AdvanceUndulation();
    }
    
    Render();
}
```

### Fast Shimmer

```csharp
void Update()
{
    // Advance every frame for fast shimmer
    shimmeringText.AdvanceUndulation();
    Render();
}
```

### Ping-Pong Effect

```csharp
private int direction = 1;
private ColoredText text = ColoredText.FromTemplate("Oscillating Text", "electric", undulate: true);

void Update()
{
    // Advance forward then reverse
    text.UndulateOffset += direction;
    
    if (text.UndulateOffset >= text.Template.ColorSequence.Count - 1)
        direction = -1;
    else if (text.UndulateOffset <= 0)
        direction = 1;
    
    Render();
}
```

---

## Use Cases

### 1. Portal/Gateway Names

```csharp
var portalText = ColoredText.FromTemplate("Ancient Portal", "ethereal", undulate: true);
// Shimmers continuously to draw attention
```

### 2. Legendary Items

```csharp
var itemText = ColoredText.FromTemplate("Sword of Flames", "fiery", undulate: true);
// Creates a blazing, animated effect
```

### 3. Boss Names

```csharp
var bossText = ColoredText.FromTemplate("Dragon Lord", "demonic", undulate: true);
// Menacing undulating colors
```

### 4. Magic Spells

```csharp
var spellText = ColoredText.FromTemplate("Arcane Missile", "arcane", undulate: true);
// Magical shimmering effect
```

### 5. Special Dungeons

```csharp
var dungeonText = ColoredText.FromTemplate("Crystalline Caverns", "crystalline", undulate: true);
// Draws attention to special/rare dungeons
```

---

## Best Practices

### Do's ✅

1. **Use for special/rare items:**
   ```csharp
   if (item.Rarity == Rarity.Legendary)
   {
       text = ColoredText.FromTemplate(item.Name, "golden", undulate: true);
   }
   ```

2. **Control animation speed:**
   ```csharp
   // Advance every N frames based on effect desired
   if (frameCounter % animationSpeed == 0)
       text.AdvanceUndulation();
   ```

3. **Apply to important UI elements:**
   ```csharp
   // Highlight current selection
   if (menuItem.IsSelected)
       text.Undulate = true;
   ```

### Don'ts ❌

1. **Don't overuse:**
   ```csharp
   // BAD - Too many shimmering elements is distracting
   every_text.Undulate = true;  // ❌
   ```

2. **Don't undulate normal text:**
   ```csharp
   // BAD - Regular UI text shouldn't shimmer
   var normalText = ColoredText.FromTemplate("HP: 100", "red", undulate: true);  // ❌
   ```

3. **Don't undulate with solid colors:**
   ```csharp
   // POINTLESS - Solid colors won't show undulation effect
   var text = ColoredText.FromColor("Test", 'R');
   text.Undulate = true;  // ❌ Does nothing
   ```

---

## Examples

### Example 1: Dungeon Selection with Undulate

```csharp
public void RenderDungeonSelection(List<Dungeon> dungeons)
{
    for (int i = 0; i < dungeons.Count; i++)
    {
        var dungeon = dungeons[i];
        string template = GetDungeonThemeTemplate(dungeon.Theme);
        
        // Special dungeons get undulation
        bool isSpecial = dungeon.Rarity == DungeonRarity.Legendary;
        
        var builder = new ColoredTextBuilder()
            .Add($"[{i + 1}] ", 'y')
            .Add(dungeon.Name, template, undulate: isSpecial)  // Only special shimmer!
            .Add($" (lvl {dungeon.MinLevel})", 'Y');
        
        var segments = builder.Build();
        textWriter.RenderSegments(segments, x, y + i);
    }
}
```

### Example 2: Animated Menu Selection

```csharp
public class AnimatedMenu
{
    private List<ColoredText> menuItems = new();
    private int selectedIndex = 0;
    
    public void Initialize()
    {
        menuItems.Add(ColoredText.FromTemplate("New Game", "holy", undulate: false));
        menuItems.Add(ColoredText.FromTemplate("Load Game", "arcane", undulate: false));
        menuItems.Add(ColoredText.FromTemplate("Settings", "crystalline", undulate: false));
    }
    
    public void Update()
    {
        // Enable undulation for selected item only
        for (int i = 0; i < menuItems.Count; i++)
        {
            menuItems[i].Undulate = (i == selectedIndex);
            if (menuItems[i].Undulate)
                menuItems[i].AdvanceUndulation();
        }
    }
    
    public void Render()
    {
        for (int i = 0; i < menuItems.Count; i++)
        {
            textWriter.WriteLineColored(menuItems[i], x, y + i * 2);
        }
    }
}
```

### Example 3: Item Rarity Display

```csharp
public ColoredText GetItemDisplayName(Item item)
{
    string template = GetRarityTemplate(item.Rarity);
    bool undulate = item.Rarity >= Rarity.Epic;  // Epic and Legendary shimmer
    
    return ColoredText.FromTemplate(item.Name, template, undulate);
}

private string GetRarityTemplate(Rarity rarity)
{
    return rarity switch
    {
        Rarity.Common => "grey",
        Rarity.Uncommon => "green",
        Rarity.Rare => "blue",
        Rarity.Epic => "purple",
        Rarity.Legendary => "golden",
        _ => "grey"
    };
}
```

---

## Performance Considerations

### Undulation is Cheap

```csharp
// Each frame:
text.AdvanceUndulation();  // O(1) - just increments a counter

// Rendering:
var segments = text.GetSegments();  // O(n) where n = text length (same as without undulate)
```

**Impact:** Minimal. The offset is just a starting position in the color sequence. No additional allocations or processing.

### When to Optimize

If you have **many** undulating texts (100+), consider:

1. **Update less frequently:**
   ```csharp
   if (frameCounter % 3 == 0)  // Update every 3 frames instead of every frame
       text.AdvanceUndulation();
   ```

2. **Share offsets:**
   ```csharp
   // All shimmering items share the same offset
   int globalUndulateOffset = (frameCounter / 3) % maxColorCount;
   foreach (var item in shimmeringItems)
   {
       item.UndulateOffset = globalUndulateOffset;
   }
   ```

---

## Advanced Techniques

### Custom Offset Patterns

```csharp
// Wave effect across multiple lines
for (int i = 0; i < dungeonList.Count; i++)
{
    var text = ColoredText.FromTemplate(dungeonList[i].Name, "ocean", undulate: true);
    text.UndulateOffset = (globalOffset + i * 2) % 4;  // Each line offset by 2
    
    textWriter.WriteLineColored(text, x, y + i);
}
```

### Reverse Undulation

```csharp
// Undulate in reverse direction
text.UndulateOffset = (text.Template.ColorSequence.Count - globalOffset) % text.Template.ColorSequence.Count;
```

### Synchronized Undulation

```csharp
// Multiple texts undulate together
var portal1 = ColoredText.FromTemplate("Portal A", "ethereal", undulate: true);
var portal2 = ColoredText.FromTemplate("Portal B", "ethereal", undulate: true);

void Update()
{
    int sharedOffset = (frameCounter / 5) % 7;  // Shared offset
    portal1.UndulateOffset = sharedOffset;
    portal2.UndulateOffset = sharedOffset;
}
```

---

## Template Compatibility

All shader types support undulation:

| Shader Type | Undulate Effect |
|-------------|-----------------|
| **Sequence** | ✅ Offsets starting color in sequence |
| **Alternation** | ✅ Offsets starting color in alternation |
| **Solid** | ❌ No effect (only one color) |

---

## Summary

✅ **Simple API:** `undulate: true` enables the effect  
✅ **Flexible:** Static offset or animated shimmer  
✅ **Performant:** O(1) overhead per text  
✅ **Pattern-Based:** Works with existing template system  
✅ **Customizable:** Control speed, direction, synchronization  

Use undulation to highlight special items, create atmosphere, and draw player attention to important elements!

---

**Date:** October 12, 2025  
**Feature:** Undulate - Shimmering Color Patterns  
**Status:** ✅ IMPLEMENTED

