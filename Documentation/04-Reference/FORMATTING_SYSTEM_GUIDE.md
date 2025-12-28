# Formatting System Guide - Complete Reference

**Last Updated:** December 2024  
**Purpose:** Central guide for working with all formatting systems in DungeonFighter-v2

---

## 📋 Quick Reference Map

### Where to Edit What

| What You Want to Change | File to Edit | Location |
|------------------------|--------------|----------|
| **Blank lines between blocks** | `TextSpacingSystem.cs` | `Code/UI/TextSpacingSystem.cs` → `SpacingRules` dictionary (lines 77-131) |
| **Battle narrative colors/text** | `BattleNarrativeFormatters.cs` | `Code/Combat/BattleNarrativeFormatters.cs` |
| **Item display formatting** | `ItemDisplayFormatter.cs` | `Code/UI/ItemDisplayFormatter.cs` |
| **Text color/spacing normalization** | `ColoredTextBuilder.cs` | `Code/UI/ColorSystem/ColoredTextBuilder.cs` → `NormalizeSpaces()` (line 330) |
| **Text wrapping/rendering** | `ColoredTextWriter.cs` | `Code/UI/Avalonia/Renderers/ColoredTextWriter.cs` → `WrapText()` (line 183) |

---

## 🎯 Core Principles for Consistency

### 1. **Always Use BlockDisplayManager**
- ✅ **DO**: Use `BlockDisplayManager.DisplayActionBlock()` for combat actions
- ✅ **DO**: Use `BlockDisplayManager.DisplayNarrativeBlock()` for narratives
- ❌ **DON'T**: Call `UIManager.WriteLine()` directly for combat/narrative text
- ❌ **DON'T**: Manually add blank lines - let `TextSpacingSystem` handle it

### 2. **Follow the Spacing Pattern**
Every display method should follow this pattern:
```csharp
// 1. Apply spacing BEFORE displaying
TextSpacingSystem.ApplySpacingBefore(TextSpacingSystem.BlockType.CombatAction);

// 2. Display the content
UIManager.WriteColoredText(formattedText);

// 3. Record what was displayed AFTER
TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.CombatAction);
```

### 3. **Use ColoredText for All New Code**
- ✅ **DO**: Use `List<ColoredText>` for formatted text
- ✅ **DO**: Use `ColoredTextBuilder` for building colored text
- ❌ **DON'T**: Use old `&R` color codes in new code (use `ColorPalette` instead)

### 4. **Consistent Color Usage**
- Use `ColorPalette` enum values, not raw color codes
- Follow semantic naming: `ColorPalette.Damage`, `ColorPalette.Player`, etc.
- See `Code/UI/ColorSystem/ColorPalette.cs` for all available colors

### 5. **Action Block Indentation Standard**
- All subsequent lines in action blocks (roll info, status effects, etc.) use **5 spaces** of indentation
- This applies to: roll information, status effects, environmental effects, and stat bonus messages
- Use the constant `ACTION_BLOCK_INDENT = "     "` (5 spaces) for consistency
- Actor-based spacing: No blank line between actions by the same actor; blank line between different actors
- Applies to: characters, enemies, environment, and standalone status effects

---

## 📁 File-by-File Guide

### 1. TextSpacingSystem.cs - Blank Line Spacing

**Purpose:** Controls vertical spacing (blank lines) between display blocks

**Location:** `Code/UI/TextSpacingSystem.cs`

**Key Components:**
- `BlockType` enum (lines 16-44): All block types in the system
- `SpacingRules` dictionary (lines 77-131): All spacing rules

**How to Edit:**
```csharp
// To change spacing between two block types:
{ (BlockType.CombatAction, BlockType.CombatAction), 1 },  // Changed from 0 to 1

// To add a new spacing rule:
{ (BlockType.YourNewBlock, BlockType.AnotherBlock), 1 },  // Add to dictionary
```

**Rules:**
- `0` = No blank line
- `1` = One blank line
- `2` = Two blank lines (rare)

**Common Patterns:**
- Section transitions: Always `1` blank line
- Consecutive actions: Always `0` blank lines
- Environmental actions: `1` blank line before, `0` after

**Testing Checklist:**
- [ ] No double blank lines
- [ ] No missing blank lines between sections
- [ ] Combat actions appear consecutively (no blank lines between)
- [ ] Environmental actions have blank lines before them

---

### 2. BattleNarrativeFormatters.cs - Battle Text Formatting

**Purpose:** Formats battle narrative text with colors and emojis

**Location:** `Code/Combat/BattleNarrativeFormatters.cs`

**Structure:**
- Each formatter is a static class with a `Format()` method
- Returns `List<ColoredText>` for consistent output
- Uses `NarrativeTextBuilder` for building text

**How to Add a New Formatter:**
```csharp
public static class YourNewFormatter
{
    public static List<ColoredText> Format(string actorName, string narrativeText)
    {
        string text = narrativeText.Replace("{name}", actorName);
        
        return new NarrativeTextBuilder()
            .AddEmoji("🎯 ", ColorPalette.Critical)  // Optional emoji
            .AddTextWithHighlight(text, actorName, 
                ColorPalette.Info,      // Default text color
                ColorPalette.Player,    // Highlight color
                ColorPalette.Info)      // After highlight color
            .Build();
    }
}
```

**Available Formatters:**
- `FirstBloodFormatter` - First blood narrative
- `CriticalHitFormatter` - Critical hit descriptions
- `CriticalMissFormatter` - Critical miss descriptions
- `EnvironmentalActionFormatter` - Environmental actions
- `HealthRecoveryFormatter` - Health recovery messages
- `IntenseBattleFormatter` - Intense battle narratives
- `PlayerTauntFormatter` / `EnemyTauntFormatter` - Taunt messages
- `ComboFormatter` - Combo sequence descriptions

**Color Guidelines:**
- Player actions: `ColorPalette.Player` (cyan)
- Enemy actions: `ColorPalette.Enemy` (red)
- Critical events: `ColorPalette.Critical` (bright red)
- Healing: `ColorPalette.Healing` (green)
- Warnings: `ColorPalette.Warning` (orange)

---

### 3. ItemDisplayFormatter.cs - Item Display Formatting

**Purpose:** Formats item stats, bonuses, and comparisons

**Location:** `Code/UI/ItemDisplayFormatter.cs`

**Key Methods:**
- `FormatStatBonus()` - Formats stat bonuses
- `GetModificationDisplayText()` - Formats modifications
- `FormatItemBonuses()` - Formats all item bonuses
- `GetColoredItemName()` - Gets colored item name

**How to Format an Item:**
```csharp
// Get colored item name
string itemName = ItemDisplayFormatter.GetColoredItemName(item);

// Format item stats
string stats = ItemDisplayFormatter.GetItemStatsDisplay(item, player);

// Format bonuses
ItemDisplayFormatter.FormatItemBonusesWithColor(item, (text) => {
    UIManager.WriteLine(text);
});
```

**Modification Formatting:**
Modifications are automatically formatted based on their effect type:
- `damage` → `"+X damage"`
- `speedMultiplier` → `"X% faster"`
- `rollBonus` → `"+X to rolls"`
- `damageMultiplier` → `"X% more damage"`
- etc.

---

### 4. ColoredTextBuilder.cs - Text Building Utility

**Purpose:** Low-level utility for building colored text segments

**Location:** `Code/UI/ColorSystem/ColoredTextBuilder.cs`

**Usage Pattern:**
```csharp
var text = new ColoredTextBuilder()
    .Add("Player", ColorPalette.Player)
    .Add(" hits ", Colors.White)
    .Add("Enemy", ColorPalette.Enemy)
    .Add(" for ", Colors.White)
    .Add("10", ColorPalette.Damage)
    .Add(" damage", Colors.White)
    .Build();  // Returns List<ColoredText>
```

**Fluent Methods:**
- `.Red()`, `.Green()`, `.Blue()`, etc. - Color shortcuts
- `.Damage()`, `.Healing()`, `.Player()`, etc. - Semantic colors
- `.AddSpace()` - Add a space
- `.AddLine()` - Add a newline

**Space Normalization:**
The `Build()` method automatically:
- Merges adjacent segments with same color
- Normalizes multiple spaces to single spaces
- Removes empty segments

**When to Use:**
- Building complex colored text
- When you need fine-grained control
- For reusable text building logic

---

### 5. ColoredTextWriter.cs - Text Rendering

**Purpose:** Renders colored text to canvas with word wrapping

**Location:** `Code/UI/Avalonia/Renderers/ColoredTextWriter.cs`

**Key Methods:**
- `WriteLineColored()` - Write colored text at position
- `WriteLineColoredWrapped()` - Write with word wrapping
- `WrapText()` - Wrap text to max width

**Text Wrapping:**
```csharp
// Wrap text to 80 characters
var wrapped = writer.WrapText(longText, maxWidth: 80);

// Write with automatic wrapping
int linesWritten = writer.WriteLineColoredWrapped(
    message, 
    x: 0, 
    y: 10, 
    maxWidth: 80
);
```

**Wrapping Behavior:**
- Preserves leading spaces (for indentation)
- Splits on word boundaries
- Handles newlines in input text

---

## 🔄 Workflow: Adding New Formatting

### Step 1: Identify What You're Formatting
- **Combat action?** → Use `BlockDisplayManager.DisplayActionBlock()`
- **Narrative text?** → Use `BlockDisplayManager.DisplayNarrativeBlock()`
- **Item display?** → Use `ItemDisplayFormatter` methods
- **Custom text?** → Use `ColoredTextBuilder`

### Step 2: Choose the Right Block Type
Check `TextSpacingSystem.BlockType` enum:
- `CombatAction` - Combat actions
- `Narrative` - Narrative text
- `EnvironmentalAction` - Environmental actions
- `StatusEffect` - Status effects
- `SystemMessage` - System messages
- etc.

### Step 3: Apply Spacing
```csharp
TextSpacingSystem.ApplySpacingBefore(BlockType.YourBlockType);
```

### Step 4: Format the Content
```csharp
// Option A: Use existing formatter
var formatted = BattleNarrativeFormatters.CriticalHitFormatter.Format(...);

// Option B: Build custom text
var formatted = new ColoredTextBuilder()
    .Add("Your text", ColorPalette.Info)
    .Build();
```

### Step 5: Display and Record
```csharp
UIManager.WriteColoredText(formatted);
TextSpacingSystem.RecordBlockDisplayed(BlockType.YourBlockType);
```

---

## ✅ Consistency Checklist

Before committing formatting changes, verify:

### Spacing
- [ ] Used `TextSpacingSystem.ApplySpacingBefore()` before displaying
- [ ] Used `TextSpacingSystem.RecordBlockDisplayed()` after displaying
- [ ] No manual blank lines added
- [ ] Spacing matches reference output

### Colors
- [ ] Used `ColorPalette` enum, not raw color codes
- [ ] Colors are semantically correct (Player=cyan, Enemy=red, etc.)
- [ ] No hardcoded color values

### Text Building
- [ ] Used `ColoredTextBuilder` for complex text
- [ ] Used existing formatters when possible
- [ ] Text is properly normalized (no double spaces)

### Display
- [ ] Used `BlockDisplayManager` methods when appropriate
- [ ] Text wrapping is handled for long text
- [ ] No direct `UIManager.WriteLine()` calls for combat/narrative

---

## 🧪 Testing Your Changes

### Visual Testing
1. Run the game and trigger the formatting you changed
2. Check spacing matches expected output
3. Verify colors are correct
4. Check text wrapping for long messages

### Reference Output
Compare against the reference output structure in:
- `Documentation/05-Systems/TEXT_SPACING_SYSTEM.md` (lines 86-106)
- `Documentation/04-Reference/COMBAT_TEXT_FORMAT_REFERENCE.md`

### Common Issues

**Double blank lines:**
- Check if spacing rule is set to `1` when it should be `0`
- Verify you're not manually adding blank lines

**Missing blank lines:**
- Check if spacing rule exists for the transition
- Verify `ApplySpacingBefore()` is being called

**Wrong colors:**
- Check `ColorPalette` usage
- Verify color codes are parsed correctly

**Text wrapping issues:**
- Check `maxWidth` parameter
- Verify leading spaces are preserved if needed

---

## 📚 Related Documentation

- **Spacing System:** `Documentation/05-Systems/TEXT_SPACING_SYSTEM.md`
- **Text Display Rules:** `Documentation/05-Systems/TEXT_DISPLAY_RULES.md`
- **Color System:** `Documentation/05-Systems/COLOR_SYSTEM_*.md`
- **Combat Format Reference:** `Documentation/04-Reference/COMBAT_TEXT_FORMAT_REFERENCE.md`

---

## 🎨 Quick Examples

### Example 1: Adding a New Combat Action Format
```csharp
// In your combat code:
var actionText = new ColoredTextBuilder()
    .Player(actorName)
    .Add(" uses ", Colors.White)
    .Add(actionName, ColorPalette.Warning)
    .Add(" on ", Colors.White)
    .Enemy(targetName)
    .Add(" for ", Colors.White)
    .Damage($"{damage}")
    .Add(" damage", Colors.White)
    .Build();

var rollInfo = new ColoredTextBuilder()
    .Add($"     (roll: {roll} | attack {attack} - {armor} armor)", Colors.White)
    .Build();

BlockDisplayManager.DisplayActionBlock(actionText, rollInfo);
```

### Example 2: Adding a New Narrative Formatter
```csharp
// In BattleNarrativeFormatters.cs:
public static class VictoryFormatter
{
    public static List<ColoredText> Format(string playerName, string enemyName)
    {
        return new NarrativeTextBuilder()
            .AddEmoji("🎉 ", ColorPalette.Success)
            .Add(playerName, ColorPalette.Player)
            .Add(" defeats ", ColorPalette.Info)
            .Add(enemyName, ColorPalette.Enemy)
            .Add("!", ColorPalette.Success)
            .Build();
    }
}

// Usage:
var narrative = VictoryFormatter.Format("Player", "Enemy");
BlockDisplayManager.DisplayNarrativeBlock(narrative);
```

### Example 3: Adjusting Spacing
```csharp
// In TextSpacingSystem.cs, SpacingRules dictionary:
// To add blank line between combat actions:
{ (BlockType.CombatAction, BlockType.CombatAction), 1 },  // Changed from 0

// To remove blank line before environmental actions:
{ (BlockType.CombatAction, BlockType.EnvironmentalAction), 0 },  // Changed from 1
```

---

## 🚀 Best Practices

1. **Always use the highest-level API available**
   - Prefer `BlockDisplayManager` over `UIManager`
   - Prefer formatters over manual text building

2. **Keep formatting logic in formatter classes**
   - Don't inline complex formatting
   - Create reusable formatters

3. **Test spacing changes visually**
   - Run the game and verify output
   - Compare against reference

4. **Use semantic colors**
   - `ColorPalette.Player` not `ColorPalette.Cyan`
   - `ColorPalette.Damage` not `ColorPalette.Red`

5. **Document new formatters**
   - Add XML comments
   - Include usage examples

---

## ❓ FAQ

**Q: Where do I add spacing rules?**  
A: `TextSpacingSystem.cs` → `SpacingRules` dictionary (lines 77-131)

**Q: How do I change narrative colors?**  
A: Edit the formatter in `BattleNarrativeFormatters.cs` or create a new one

**Q: Can I add new block types?**  
A: Yes, add to `TextSpacingSystem.BlockType` enum and add spacing rules

**Q: How do I format items?**  
A: Use `ItemDisplayFormatter` methods - see file for available methods

**Q: What if I need custom formatting?**  
A: Use `ColoredTextBuilder` to build custom `List<ColoredText>`

---

## 🔍 Spacing Accuracy Guidelines

### Word Spacing
- **Always use single spaces** between words
- **No double spaces** - use `CombatLogSpacingManager.NormalizeSpacing()` if needed
- **Proper punctuation spacing** - no space before punctuation, space after (usually)

### Blank Line Spacing
- **Follow TextSpacingSystem rules** - don't manually add blank lines
- **Use ApplySpacingBefore()** before displaying blocks
- **Use RecordBlockDisplayed()** after displaying blocks

### Validation
Use `TextSpacingValidator` to check spacing:
```csharp
var result = TextSpacingValidator.ValidateWordSpacing(text);
if (!result.IsValid)
{
    Console.WriteLine(TextSpacingValidator.GenerateReport(result));
}
```

---

## 🎨 Color Application Guidelines

### When to Apply Colors
- **Damage numbers** → `ColorPalette.Damage`
- **Player names** → `ColorPalette.Player`
- **Enemy names** → `ColorPalette.Enemy`
- **Item names** → Rarity-based colors
- **Status effects** → Appropriate status colors

### Avoiding Double-Coloring
- **Don't apply keyword coloring** to text that already has explicit colors
- **Use ColoredTextBuilder** instead of string concatenation with color codes
- **Check for existing colors** before applying keyword coloring

### Validation
Use `ColorApplicationValidator` to check colors:
```csharp
var result = ColorApplicationValidator.ValidateNoDoubleColoring(text);
if (!result.IsValid)
{
    Console.WriteLine(ColorApplicationValidator.GenerateReport(result));
}
```

---

## 🚫 Overlap Prevention Guidelines

### Best Practices
- **Use ColoredTextWriter** for rendering colored text
- **Let segment renderers handle positioning** - don't manually calculate positions
- **Check debug output** for overlap warnings in debug mode

### Detection
Overlap detection is automatic in:
- `GameCanvasControl.AddText()` - detects same-position overlaps
- `ColoredTextWriter.RenderSegments()` - detects near-overlaps

---

## 🧪 Testing Your Changes

### Automated Tests
Run the test suite:
```csharp
TextSystemAccuracyTests.RunAllTests();
```

### Visual Testing
1. Run the game
2. Trigger the text you changed
3. Verify spacing, colors, and overlap
4. Compare against reference output

### Validation Tools
- `TextSpacingValidator` - Check spacing accuracy
- `ColorApplicationValidator` - Check color application
- `TextSpacingSystem.ValidateSpacingRules()` - Check blank line rules

---

## 📚 Troubleshooting

### Double Spaces
**Problem:** Text has double spaces  
**Solution:** Use `CombatLogSpacingManager.NormalizeSpacing()` before displaying

### Missing Blank Lines
**Problem:** No blank line between blocks  
**Solution:** Check `TextSpacingSystem` rules, ensure `ApplySpacingBefore()` is called

### Text Overlapping
**Problem:** Text overlaps with other text  
**Solution:** Check debug output, verify position calculations, use `MeasureTextWidth()`

### Missing Colors
**Problem:** Text doesn't have expected colors  
**Solution:** Use `ColoredTextBuilder` with appropriate `ColorPalette` values

### Double-Coloring
**Problem:** Text has conflicting color codes  
**Solution:** Don't apply keyword coloring to text with explicit colors

---

**Remember:** The formatting system is designed to be declarative and easy to modify. When in doubt, check the existing formatters for patterns and examples!

For detailed tuning instructions, see `Documentation/02-Development/TEXT_SYSTEM_TUNING_GUIDE.md`

