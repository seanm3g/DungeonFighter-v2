# Color System Migration Guide

## Overview

This guide explains how to migrate from the old color system to the new, modern color system.

## Old System Problems

The old system had several issues:
- **Cryptic color codes**: `&R`, `&y`, `&G` were hard to remember
- **Spacing artifacts**: Adding color codes could break text layout
- **Two-phase processing**: Templates → codes → segments was complex
- **Hard to maintain**: Color codes embedded in strings were error-prone
- **AI-unfriendly**: Difficult for AI assistants to modify reliably

## New System Benefits

The new system provides:
- **Clear, readable code**: `ColorPalette.Damage` instead of `&R`
- **No spacing issues**: Colors are separate from text content
- **Character-specific colors**: Each character can have custom color schemes
- **Pattern-based coloring**: Use semantic patterns like "damage", "healing"
- **Multiple output formats**: HTML, ANSI, plain text, debug
- **Easy to maintain**: Clear separation of content and presentation

## Migration Examples

### Before (Old System)
```csharp
// Old way - cryptic and error-prone
string text = "&RDanger&y is &Gahead&y!";
var segments = ColorParser.Parse(text);
```

### After (New System)
```csharp
// New way - clear and maintainable
var coloredText = new ColoredTextBuilder()
    .Add("Danger ", ColorPalette.Damage)
    .Add("is ", Colors.White)
    .Add("ahead", ColorPalette.Success)
    .Add("!", Colors.White)
    .Build();
```

### Pattern-Based Coloring
```csharp
// Use semantic patterns
var coloredText = new ColoredTextBuilder()
    .AddWithPattern("25", "damage")
    .Add(" damage to ", Colors.White)
    .AddWithPattern("Goblin", "enemy")
    .Build();
```

### Character-Specific Colors
```csharp
// Set up character color profile
var playerProfile = new CharacterColorProfile("Player");
playerProfile.PrimaryColor = ColorPalette.Cyan;
playerProfile.SetCustomPattern("spell", ColorPalette.Magenta);
CharacterColorManager.SetProfile("Player", playerProfile);

// Use character-specific colors
var coloredText = new ColoredTextBuilder()
    .Add("Player ", ColorPalette.Player)  // Uses character's primary color
    .Add("casts ", Colors.White)
    .AddWithPattern("Fireball", "spell")  // Uses character's custom spell color
    .Build();
```

### Markup Parsing
```csharp
// Parse markup text
var markupText = "[damage]25[/damage] damage to [enemy]Goblin[/enemy]!";
var coloredText = ColoredTextParser.Parse(markupText);
```

## Migration Steps

### 1. Replace Color Code Usage
**Find and replace:**
- `&R` → `ColorPalette.Damage`
- `&G` → `ColorPalette.Success`
- `&B` → `ColorPalette.Info`
- `&Y` → `ColorPalette.Warning`
- `&y` → `Colors.White` (reset)

### 2. Update Text Building
**Replace string concatenation with ColoredTextBuilder:**
```csharp
// Old
string text = "You deal " + damage + " damage!";

// New
var coloredText = new ColoredTextBuilder()
    .Add("You deal ", Colors.White)
    .Add(damage.ToString(), ColorPalette.Damage)
    .Add(" damage!", Colors.White)
    .Build();
```

### 3. Use Patterns for Consistency
**Replace hardcoded colors with patterns:**
```csharp
// Old
.Add("25", ColorPalette.Damage)

// New
.AddWithPattern("25", "damage")
```

### 4. Set Up Character Profiles
**Create character-specific color schemes:**
```csharp
var playerProfile = new CharacterColorProfile("Player");
playerProfile.PrimaryColor = ColorPalette.Cyan;
playerProfile.DamageColor = ColorPalette.Red;
CharacterColorManager.SetProfile("Player", playerProfile);
```

### 5. Update Rendering
**Replace old rendering with new renderer:**
```csharp
// Old
var segments = ColorParser.Parse(text);
foreach (var segment in segments)
{
    // Render segment
}

// New
var coloredText = ColoredTextParser.Parse(text);
var plainText = ColoredTextRenderer.RenderAsPlainText(coloredText);
var html = ColoredTextRenderer.RenderAsHtml(coloredText);
```

## Available Color Palettes

### Basic Colors
- `ColorPalette.White`, `ColorPalette.Black`, `ColorPalette.Gray`
- `ColorPalette.Red`, `ColorPalette.Green`, `ColorPalette.Blue`
- `ColorPalette.Yellow`, `ColorPalette.Cyan`, `ColorPalette.Magenta`

### Game-Specific Colors
- `ColorPalette.Damage`, `ColorPalette.Healing`, `ColorPalette.Critical`
- `ColorPalette.Success`, `ColorPalette.Warning`, `ColorPalette.Error`
- `ColorPalette.Common`, `ColorPalette.Rare`, `ColorPalette.Epic`, `ColorPalette.Legendary`

### UI Colors
- `ColorPalette.Background`, `ColorPalette.Foreground`, `ColorPalette.Border`
- `ColorPalette.Highlight`, `ColorPalette.Disabled`

## Available Patterns

### Combat Patterns
- `"damage"`, `"healing"`, `"critical"`, `"miss"`, `"block"`, `"dodge"`
- `"attack"`, `"hit"`, `"strike"`, `"slash"`, `"pierce"`, `"crush"`

### Status Patterns
- `"success"`, `"warning"`, `"error"`, `"info"`
- `"victory"`, `"defeat"`, `"level_up"`, `"experience"`

### Rarity Patterns
- `"common"`, `"uncommon"`, `"rare"`, `"epic"`, `"legendary"`

### Element Patterns
- `"fire"`, `"ice"`, `"lightning"`, `"poison"`, `"dark"`, `"light"`
- `"arcane"`, `"nature"`

### Character Patterns
- `"player"`, `"enemy"`, `"npc"`, `"boss"`, `"minion"`

### Item Patterns
- `"weapon"`, `"armor"`, `"potion"`, `"scroll"`, `"gem"`, `"coin"`

## Best Practices

### 1. Use Patterns for Consistency
```csharp
// Good - uses semantic pattern
.AddWithPattern("25", "damage")

// Avoid - hardcoded color
.Add("25", ColorPalette.Red)
```

### 2. Set Up Character Profiles
```csharp
// Set up profiles once at startup
var playerProfile = new CharacterColorProfile("Player");
playerProfile.PrimaryColor = ColorPalette.Cyan;
CharacterColorManager.SetProfile("Player", playerProfile);
```

### 3. Use Builder Pattern
```csharp
// Good - fluent and readable
var text = new ColoredTextBuilder()
    .Add("Hello ", Colors.White)
    .Add("World", ColorPalette.Success)
    .Build();

// Avoid - manual list building
var text = new List<ColoredText>
{
    new ColoredText("Hello ", Colors.White),
    new ColoredText("World", ColorPalette.Success)
};
```

### 4. Leverage Multiple Output Formats
```csharp
var coloredText = CreateCombatMessage();

// Use appropriate format for context
var plainText = ColoredTextRenderer.RenderAsPlainText(coloredText);  // For logging
var html = ColoredTextRenderer.RenderAsHtml(coloredText);            // For web UI
var ansi = ColoredTextRenderer.RenderAsAnsi(coloredText);            // For console
```

## Common Migration Patterns

### Combat Messages
```csharp
// Old
string text = $"&R{damage}&y damage to &G{enemyName}&y!";

// New
var coloredText = new ColoredTextBuilder()
    .Add(damage.ToString(), ColorPalette.Damage)
    .Add(" damage to ", Colors.White)
    .Add(enemyName, ColorPalette.Enemy)
    .Add("!", Colors.White)
    .Build();
```

### Status Messages
```csharp
// Old
string text = $"&GLevel up!&y You are now level &B{level}&y!";

// New
var coloredText = new ColoredTextBuilder()
    .AddWithPattern("Level up!", "success")
    .Add(" You are now level ", Colors.White)
    .Add(level.ToString(), ColorPalette.Info)
    .Add("!", Colors.White)
    .Build();
```

### Item Descriptions
```csharp
// Old
string text = $"&P{Rarity}&y {ItemName} - &R{Damage}&y damage";

// New
var coloredText = new ColoredTextBuilder()
    .AddWithPattern(rarity, rarity.ToLower())
    .Add(" ", Colors.White)
    .Add(itemName, ColorPalette.White)
    .Add(" - ", Colors.White)
    .Add(damage.ToString(), ColorPalette.Damage)
    .Add(" damage", Colors.White)
    .Build();
```

## Testing the Migration

### 1. Visual Testing
- Compare old and new output side by side
- Verify colors match expectations
- Check for spacing issues

### 2. Functional Testing
- Test all color patterns
- Verify character-specific colors work
- Test markup parsing

### 3. Performance Testing
- Measure rendering performance
- Check memory usage
- Verify no memory leaks

## Troubleshooting

### Common Issues

1. **Colors not showing**: Check if color palette is correct
2. **Spacing issues**: Use ColoredTextBuilder instead of string concatenation
3. **Character colors not working**: Verify character profile is set up
4. **Patterns not working**: Check if pattern exists in ColorPatterns

### Debug Tips

1. **Use debug rendering**: `ColoredTextRenderer.RenderAsDebug(coloredText)`
2. **Check pattern existence**: `ColorPatterns.HasPattern(pattern)`
3. **Verify character profile**: `CharacterColorManager.GetProfile(characterName)`
4. **Test with plain text**: `ColoredTextRenderer.RenderAsPlainText(coloredText)`

## Conclusion

The new color system provides a much more maintainable and flexible approach to text coloring. While migration requires some effort, the benefits in terms of code clarity, maintainability, and functionality make it worthwhile.

Take the time to set up character profiles and use patterns consistently - it will pay off in the long run.
