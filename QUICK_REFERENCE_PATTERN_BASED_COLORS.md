# Quick Reference: Pattern-Based Color System

## TL;DR

**Old (Broken):** Weaving color codes into strings → parsing errors → text corruption  
**New (Fixed):** Separate text and patterns → apply at render time → no corruption

---

## Quick Start

### Single Color

```csharp
var text = ColoredText.FromColor("Damage!", 'R');
textWriter.WriteLineColored(text, x, y);
```

### Template Pattern

```csharp
var text = ColoredText.FromTemplate("Blazing Sword", "fiery");
textWriter.WriteLineColored(text, x, y);
```

### Multi-Part

```csharp
var segments = new ColoredTextBuilder()
    .Add("HP: ", 'y')
    .Add("100", 'G')
    .Build();
textWriter.RenderSegments(segments, x, y);
```

---

## Color Codes

| Code | Color | Example |
|------|-------|---------|
| `R` | Bright Red | Damage, critical |
| `G` | Green | Health, success |
| `B` | Blue | Mana, water |
| `Y` | White | Important text |
| `y` | Grey | Normal text |
| `M` | Magenta | Magic, astral |
| `C` | Cyan | Ice, crystal |
| `O` | Orange | Fire, legendary |
| `W` | Yellow | Gold, divine |

---

## Common Patterns

### Dungeon Selection Entry

```csharp
var builder = new ColoredTextBuilder()
    .Add($"[{num}] ", 'y')           // Grey number
    .Add(dungeonName, themeName)      // Themed name
    .Add($" (lvl {level})", 'Y');    // White level

var segments = builder.Build();
textWriter.RenderSegments(segments, x, y);
```

### Item Display

```csharp
var builder = new ColoredTextBuilder()
    .Add(item.Name, item.RarityTemplate)  // Rarity-colored name
    .Add($" +{item.Bonus}", 'G');         // Green bonus

var segments = builder.Build();
textWriter.RenderSegments(segments, x, y);
```

### Combat Message

```csharp
var builder = new ColoredTextBuilder()
    .Add(attacker.Name, 'Y')
    .Add(" hits ", 'y')
    .Add(defender.Name, 'y')
    .Add(" for ", 'y')
    .Add(damage.ToString(), 'R')
    .Add(" damage!", 'y');

var segments = builder.Build();
textWriter.RenderSegments(segments, x, y);
```

---

## DON'Ts ❌

### Don't Embed Color Codes

```csharp
// WRONG - Don't do this with ColoredText
text.Text = "&RDamage&y";  // ❌

// RIGHT - Use simple color or template
var text = ColoredText.FromColor("Damage", 'R');  // ✅
```

### Don't Use ColorParser.Colorize()

```csharp
// WRONG - Old way with embedded codes
string markup = ColorParser.Colorize(name, "fiery");  // ❌
textWriter.WriteLineColored(markup, x, y);

// RIGHT - New way with pattern reference
var text = ColoredText.FromTemplate(name, "fiery");  // ✅
textWriter.WriteLineColored(text, x, y);
```

### Don't Convert to Markup

```csharp
// WRONG - Converting back to markup
string markup = $"{{fiery|{name}}}";  // ❌

// RIGHT - Keep as ColoredText
var text = ColoredText.FromTemplate(name, "fiery");  // ✅
```

---

## DOs ✅

### Use ColoredText

```csharp
// For single-color text
var text = ColoredText.FromColor("HP", 'G');

// For template-based text
var text = ColoredText.FromTemplate("Blazing Sword", "fiery");

// For plain text
var text = ColoredText.Plain("Press any key");
```

### Use ColoredTextBuilder

```csharp
// For multi-part colored text
var builder = new ColoredTextBuilder()
    .Add("Part 1", 'R')
    .Add("Part 2", "fiery")
    .Add("Part 3");
```

### Apply Patterns at Render Time

```csharp
// Pattern is applied only when rendering
var segments = coloredText.GetSegments();
textWriter.RenderSegments(segments, x, y);
```

---

## Templates

### Available Templates

| Template | Effect | Use Case |
|----------|--------|----------|
| `fiery` | Red→Orange→Yellow | Fire items, heat |
| `icy` | Cyan→Blue→White | Ice items, cold |
| `toxic` | Green→Yellow | Poison, corruption |
| `crystalline` | Magenta→Cyan→Yellow | Crystals, prisms |
| `electric` | Cyan→Yellow | Lightning, energy |
| `holy` | White→Yellow | Divine, blessed |
| `demonic` | Red→Magenta | Hell, cursed |
| `arcane` | Magenta→Cyan | Magic, spells |
| `shadow` | Magenta→Grey | Darkness, stealth |
| `golden` | Yellow→Orange | Gold, wealth |
| `astral` | Magenta→Blue→White→Cyan | Space, cosmic |
| `ocean` | Blue→Cyan | Water, sea |
| `forest` | Green→Brown | Nature, plants |

### Dungeon Themes

| Theme | Template |
|-------|----------|
| Astral | `astral` |
| Crystal | `crystal` |
| Ocean | `ocean` |
| Lava | `volcano` |
| Ice | `icy` |
| Forest | `forest` |
| Shadow | `shadow` |
| Temple | `temple` |
| Arcane | `arcane` |

---

## Troubleshooting

### Text Not Colored?

```csharp
// Check template exists
bool exists = ColorTemplateLibrary.HasTemplate("fiery");  // true?

// Check template is applied
var segments = text.GetSegments();  // Has colored segments?
```

### Text Corrupted?

```csharp
// Don't use old API with embedded codes
textWriter.WriteLineColored("&R{{fiery|text}}&y", x, y);  // ❌ BAD

// Use ColoredText instead
var text = ColoredText.FromTemplate("text", "fiery");  // ✅ GOOD
textWriter.WriteLineColored(text, x, y);
```

### Wrong Colors?

```csharp
// Verify color code is valid
bool valid = ColorDefinitions.IsValidColorCode('R');  // true?

// Check color definition
var color = ColorDefinitions.GetColor('R');  // RGB(255, 50, 50)?
```

---

## Migration Checklist

When migrating code to pattern-based system:

- [ ] Replace `ColorParser.Colorize()` with `ColoredText.FromTemplate()`
- [ ] Remove embedded color codes from strings
- [ ] Use `ColoredTextBuilder` for multi-part text
- [ ] Call `RenderSegments()` or `WriteLineColored(ColoredText)`
- [ ] Verify text displays correctly
- [ ] No parsing errors or corruption

---

## Example: Complete Dungeon Entry

```csharp
// Get dungeon data
var dungeon = availableDungeons[index];
string themeName = GetDungeonThemeTemplate(dungeon.Theme);

// Build colored text
var builder = new ColoredTextBuilder()
    .Add($"[{index + 1}] ", 'y')              // Grey bracket/number
    .Add(dungeon.Name, themeName)              // Themed dungeon name
    .Add($" (lvl {dungeon.MinLevel})", 'Y');  // White level info

// Render directly
var segments = builder.Build();
textWriter.RenderSegments(segments, x, y);

// Result: [1] Celestial Observatory (lvl 5)
//         ^grey ^multi-colored (astral)^ ^white^
```

---

## Key Principles

1. **Text and patterns stay separate** until rendering
2. **No embedded color codes** in ColoredText.Text
3. **Apply patterns at render time** via GetSegments()
4. **Use ColoredTextBuilder** for multi-part text
5. **Direct segment rendering** avoids parsing issues

---

**For More Information:**
- `PATTERN_BASED_COLOR_REFACTORING.md` - Full implementation guide
- `Documentation/05-Systems/PATTERN_BASED_COLOR_ARCHITECTURE.md` - Architecture details
- `SOLUTION_SUMMARY.md` - Problem and solution overview

