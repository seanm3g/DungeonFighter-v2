# Color System Quick Start Guide

## 5-Minute Introduction

### 1. Basic Color Codes

Use `&` followed by a letter for colors:

```csharp
UIManager.WriteLine("&RRed text");
UIManager.WriteLine("&GGreen text");
UIManager.WriteLine("&BBlue text");
```

### 2. Color Templates

Use `{{template|text}}` for effects:

```csharp
UIManager.WriteLine("{{fiery|Blazing Sword}}");
UIManager.WriteLine("{{icy|Frozen Staff}}");
UIManager.WriteLine("{{legendary|Epic Item}}");
```

### 3. Automatic Keyword Coloring

Keywords are automatically colored:

```csharp
string msg = "You hit the goblin for 25 damage!";
string colored = KeywordColorSystem.Colorize(msg);
UIManager.WriteLine(colored);
// "goblin" turns red, "hit" and "damage" turn red
```

### 4. Event Significance

Adjust brightness for important events:

```csharp
var color = ColorDefinitions.GetColor('R').Value;
var segment = ColorLayerSystem.CreateSignificantSegment(
    "CRITICAL HIT!",
    color,
    EventSignificance.Critical  // Very bright!
);
ColoredConsoleWriter.WriteSegments(new[] { segment }.ToList());
```

### 5. Dungeon Depth Effects

White text shifts from warm to cool as you go deeper:

```csharp
var segment = ColorLayerSystem.CreateDepthWhiteSegment(
    "Room description...",
    currentRoom: 5,
    totalRooms: 10
);
ColoredConsoleWriter.WriteSegments(new[] { segment }.ToList());
```

## Common Patterns

### Combat Messages

```csharp
// Method 1: Manual markup
UIManager.WriteLine("{{critical|CRITICAL HIT}}! You deal &R55&y damage!");

// Method 2: Automatic keywords
string msg = "Critical hit! You deal 55 damage to the orc!";
UIManager.WriteLine(KeywordColorSystem.Colorize(msg));

// Method 3: With significance
var redColor = ColorDefinitions.GetColor('R').Value;
var damageSegment = ColorLayerSystem.CreateSignificantSegment(
    "55 damage",
    redColor,
    EventSignificance.Critical
);
```

### Item Displays

```csharp
// Rarity coloring
UIManager.WriteLine("{{legendary|Sword of Destiny}}");
UIManager.WriteLine("{{epic|Dragon Shield}}");
UIManager.WriteLine("{{rare|Mithril Armor}}");

// Or use keywords
string item = "You found a legendary Sword of Destiny!";
UIManager.WriteLine(KeywordColorSystem.Colorize(item));
```

### Status Effects

```csharp
// Using templates
UIManager.WriteLine("You are {{poisoned|POISONED}}!");
UIManager.WriteLine("Enemy is {{stunned|STUNNED}}!");
UIManager.WriteLine("You are {{burning|BURNING}}!");

// Or keywords
string status = "You are poisoned and take 5 damage per turn.";
UIManager.WriteLine(KeywordColorSystem.Colorize(status));
```

## Color Code Reference

Quick reference for common codes:

| Code | Color | Use For |
|------|-------|---------|
| `&R` | Red | Damage, enemies, danger |
| `&G` | Green | Healing, success, nature |
| `&B` | Blue | Magic, mana, rare items |
| `&W` | Yellow/Gold | Gold, highlights, important |
| `&M` | Magenta | Epic items, magic effects |
| `&O` | Orange | Legendary items, fire |
| `&C` | Cyan | Info, ice, classes |
| `&y` | Grey | Normal text (default) |
| `&Y` | White | Emphasis, important info |

## Template Reference

Quick reference for common templates:

| Template | Effect | Use For |
|----------|--------|---------|
| `{{fiery\|...}}` | Fire colors | Fire attacks, burning |
| `{{icy\|...}}` | Ice colors | Ice attacks, freezing |
| `{{holy\|...}}` | Light colors | Divine effects, healing |
| `{{shadow\|...}}` | Dark colors | Dark magic, stealth |
| `{{critical\|...}}` | Bright red-orange | Critical hits |
| `{{legendary\|...}}` | Orange | Legendary items |
| `{{epic\|...}}` | Purple | Epic items |
| `{{rare\|...}}` | Blue | Rare items |
| `{{poisoned\|...}}` | Toxic green | Poison effects |
| `{{electric\|...}}` | Lightning | Electric attacks |

## Enable/Disable

```csharp
// Disable color markup (plain text)
UIManager.EnableColorMarkup = false;

// Re-enable
UIManager.EnableColorMarkup = true;

// Check if text has markup
if (ColorParser.HasColorMarkup(text))
{
    // Process it
}

// Strip markup to get plain text
string plain = ColorParser.StripColorMarkup(colored);
```

## Custom Keyword Groups

```csharp
// Create your own keyword group
KeywordColorSystem.CreateGroup(
    name: "boss-names",
    colorPattern: "legendary",  // Use legendary template
    caseSensitive: false,
    keywords: "Dragon Lord", "Lich King", "Demon Prince"
);

// Now these names auto-color
string msg = "You face the Dragon Lord!";
UIManager.WriteLine(KeywordColorSystem.Colorize(msg));
```

## Tips

1. **Mix and match**: Combine manual markup with keyword coloring
2. **Be consistent**: Pick one style for similar messages
3. **Use significance sparingly**: Reserve Critical for truly important events
4. **Test readability**: Make sure colors enhance, not distract
5. **Performance**: Markup parsing is fast, but check HasColorMarkup() if concerned

## Examples to Run

```csharp
// Run all demos
ColorSystemExamples.RunAllDemos();
ColorLayerExamples.RunAllDemos();
KeywordColorExamples.RunAllDemos();

// Or individual features
ColorSystemExamples.DemoBasicColors();
ColorSystemExamples.DemoTemplates();
ColorLayerExamples.DemoEventSignificance();
KeywordColorExamples.DemoCombatScenarios();
```

## Need More?

See **COLOR_SYSTEM.md** for complete documentation including:
- Full architecture details
- All available templates and codes
- Advanced usage patterns
- Integration with game systems
- Performance optimization
- Best practices

---

*Get started in 5 minutes, master in an hour!*

