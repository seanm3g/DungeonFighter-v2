# Text System Tuning Guide

**Last Updated:** January 2025  
**Purpose:** Guide for tuning and maintaining text system accuracy (spacing, overlap, colors)

---

## Overview

The text system has three main areas that need tuning:
1. **Word Spacing** - Ensuring correct number of spaces between words
2. **Blank Line Spacing** - Ensuring correct number of blank lines between blocks
3. **Text Overlap** - Preventing text from overlapping with other text
4. **Color Application** - Ensuring colors are applied consistently

---

## 1. Adjusting Word Spacing Rules

### Location
`Code/UI/CombatLogSpacingManager.cs`

### Key Methods

#### `ShouldAddSpaceBetween()`
Determines if a space should be added between two text segments.

**Parameters:**
- `previousText` - The text that came before
- `nextText` - The text that comes next
- `checkWordBoundary` - If true, prevents spacing in multi-color templates

**Common Adjustments:**

```csharp
// To prevent spacing after a specific character:
private static readonly HashSet<char> NoSpaceAfter = new HashSet<char>
{
    '!', '?', '.', ',', ':', ';', '[', '(', '{', '\n', '\r',
    // Add your character here
};

// To prevent spacing before a specific character:
private static readonly HashSet<char> NoSpaceBefore = new HashSet<char>
{
    '!', '?', '.', ',', ':', ';', ']', ')', '}', '\'', '\n', '\r',
    // Add your character here
};
```

### Testing Word Spacing

Use the validation tool:
```csharp
var result = TextSpacingValidator.ValidateWordSpacing("your text here");
if (!result.IsValid)
{
    foreach (var issue in result.Issues)
    {
        Console.WriteLine(issue);
    }
}
```

### Common Issues

**Double Spaces:**
- Cause: Multiple calls adding spaces, or text already containing double spaces
- Fix: Use `NormalizeSpacing()` before displaying

**Missing Spaces:**
- Cause: `ShouldAddSpaceBetween()` returning false when it should return true
- Fix: Add exception cases to the method

**Multi-Color Template Spacing:**
- Cause: Word boundary detection not working correctly
- Fix: Use `checkWordBoundary: true` parameter when calling `ShouldAddSpaceBetween()`

---

## 2. Adjusting Blank Line Rules

### Location
`Code/UI/TextSpacingSystem.cs`

### Key Components

#### `SpacingRules` Dictionary
Defines blank lines between block transitions.

**Format:**
```csharp
{ (PreviousBlockType, CurrentBlockType), numberOfBlankLines }
```

**Example:**
```csharp
// Add blank line between combat actions and environmental actions
{ (BlockType.CombatAction, BlockType.EnvironmentalAction), 1 },

// No blank line between consecutive combat actions
{ (BlockType.CombatAction, BlockType.CombatAction), 0 },
```

### Adding New Block Types

1. Add to `BlockType` enum:
```csharp
public enum BlockType
{
    // ... existing types ...
    YourNewBlockType,  // Add here
}
```

2. Add spacing rules for transitions:
```csharp
{ (BlockType.ExistingBlock, BlockType.YourNewBlockType), 1 },
{ (BlockType.YourNewBlockType, BlockType.AnotherBlock), 0 },
```

### Testing Blank Line Rules

Use the validation method:
```csharp
var issues = TextSpacingSystem.ValidateSpacingRules();
if (issues.Count > 0)
{
    foreach (var issue in issues)
    {
        Console.WriteLine($"Missing rule: {issue}");
    }
}
```

### Common Issues

**Too Many Blank Lines:**
- Cause: Rule set to 1 when it should be 0
- Fix: Change rule value from 1 to 0

**Missing Blank Lines:**
- Cause: No rule defined for transition, or rule set to 0 when it should be 1
- Fix: Add rule or change value

**Incorrect Spacing:**
- Cause: Wrong block type recorded or spacing applied incorrectly
- Fix: Verify `RecordBlockDisplayed()` is called after each block

---

## 3. Preventing Text Overlap

### Location
`Code/UI/Avalonia/GameCanvasControl.cs` and `Code/UI/Avalonia/Renderers/ColoredTextWriter.cs`

### Key Methods

#### `AddText()` in GameCanvasControl
Removes existing text at the same position to prevent overlap.

**Enhancement:**
- Now detects near-overlaps (within 1 character position)
- Logs potential overlaps in debug mode

#### `RenderSegments()` in ColoredTextWriter
Renders colored text segments with overlap detection.

**Features:**
- Tracks rendered positions
- Detects overlaps within 2 character positions
- Auto-fixes overlaps by adjusting positions

### Testing Overlap Prevention

Visual testing is best:
1. Run the game
2. Display text that might overlap
3. Check for overlapping characters
4. Check debug output for overlap warnings

### Common Issues

**Text Overlapping:**
- Cause: Position calculation rounding issues, or segments rendering at same position
- Fix: Use `MeasureTextWidth()` for accurate positioning, or adjust overlap detection threshold

**Gaps Between Segments:**
- Cause: Overlap detection being too aggressive
- Fix: Adjust overlap detection threshold or disable for same-color segments

**Position Rounding:**
- Cause: Converting from pixel positions to character positions
- Fix: Use precise X tracking in `StandardSegmentRenderer`

---

## 4. Ensuring Color Application Consistency

### Location
Multiple files - see `Code/UI/ColorApplicationValidator.cs` for validation

### Key Areas

#### Combat Actions
- Damage numbers should use `ColorPalette.Damage`
- Player names should use `ColorPalette.Player`
- Enemy names should use `ColorPalette.Enemy`

#### Item Names
- Should use rarity-based colors
- Check `ItemDisplayFormatter.GetColoredItemName()`

#### Status Effects
- Should use appropriate status colors
- Check `BlockDisplayManager.DisplayEffectBlock()`

### Testing Color Application

Use the validation tool:
```csharp
// Check for double-coloring
var result = ColorApplicationValidator.ValidateNoDoubleColoring(text);
if (!result.IsValid)
{
    Console.WriteLine(ColorApplicationValidator.GenerateReport(result));
}

// Check combat text colors
var segments = new List<ColoredText> { /* your segments */ };
var colorResult = ColorApplicationValidator.ValidateCombatTextColors(segments);
```

### Common Issues

**Missing Colors:**
- Cause: Text not using `ColoredTextBuilder` or not applying colors
- Fix: Use `ColoredTextBuilder` with appropriate color methods

**Double-Coloring:**
- Cause: Both explicit color codes and keyword coloring applied
- Fix: Ensure `ApplyKeywordColoring()` doesn't modify text with existing colors

**Incorrect Colors:**
- Cause: Wrong `ColorPalette` value used
- Fix: Check color palette usage, use semantic colors (e.g., `ColorPalette.Damage` not hardcoded red)

---

## 5. Validation Tools

### TextSpacingValidator

**Location:** `Code/UI/TextSpacingValidator.cs`

**Methods:**
- `ValidateWordSpacing()` - Checks word spacing in text
- `ValidateBlankLineSpacing()` - Checks blank line spacing between blocks
- `ValidateColoredTextSpacing()` - Checks spacing in ColoredText segments
- `GenerateReport()` - Creates human-readable report

**Usage:**
```csharp
var result = TextSpacingValidator.ValidateWordSpacing("your text");
if (!result.IsValid)
{
    Console.WriteLine(TextSpacingValidator.GenerateReport(result));
}
```

### ColorApplicationValidator

**Location:** `Code/UI/ColorApplicationValidator.cs`

**Methods:**
- `ValidateCombatTextColors()` - Validates colors in combat text
- `ValidateNoDoubleColoring()` - Checks for double-coloring issues
- `ValidateItemNameColors()` - Validates item name colors
- `GenerateReport()` - Creates human-readable report

**Usage:**
```csharp
var result = ColorApplicationValidator.ValidateNoDoubleColoring(text);
Console.WriteLine(ColorApplicationValidator.GenerateReport(result));
```

### Test Suite

**Location:** `Code/Tests/TextSystemAccuracyTests.cs`

**Run all tests:**
```csharp
TextSystemAccuracyTests.RunAllTests();
```

**Individual tests:**
- `TestWordSpacing()` - Tests word spacing accuracy
- `TestBlankLineSpacing()` - Tests blank line spacing
- `TestTextOverlap()` - Tests overlap prevention
- `TestColorApplication()` - Tests color application
- `TestEdgeCases()` - Tests edge cases

---

## 6. Debugging Tips

### Enable Debug Logging

Overlap detection logs warnings in debug mode. To see them:
1. Build in Debug configuration
2. Check debug output for overlap warnings

### Visual Inspection

The best way to verify text accuracy:
1. Run the game
2. Capture screenshots of text output
3. Compare against reference output
4. Document discrepancies

### Step-by-Step Debugging

1. **Identify the issue:**
   - Is it spacing? Overlap? Color?
   - Where does it occur? (Combat, menus, etc.)

2. **Use validation tools:**
   - Run appropriate validator
   - Check test suite results

3. **Trace the code:**
   - Find where the text is generated
   - Check spacing/color application
   - Verify rendering logic

4. **Fix and test:**
   - Make incremental changes
   - Test after each change
   - Verify fix doesn't break other areas

---

## 7. Best Practices

### Spacing
- Always use `CombatLogSpacingManager` for spacing logic
- Use `NormalizeSpacing()` before displaying text
- Let `ColoredTextBuilder` handle spacing automatically

### Blank Lines
- Always use `TextSpacingSystem` for blank line spacing
- Call `ApplySpacingBefore()` before displaying
- Call `RecordBlockDisplayed()` after displaying

### Colors
- Use `ColoredTextBuilder` for building colored text
- Use `ColorPalette` enum, not hardcoded colors
- Avoid double-coloring (explicit codes + keywords)

### Overlap
- Use `MeasureTextWidth()` for accurate positioning
- Let `ColoredTextWriter` handle segment rendering
- Check debug output for overlap warnings

---

## 8. Common Patterns

### Pattern 1: Adding New Text Type

1. Determine block type (or create new one)
2. Add spacing rules if needed
3. Use `ColoredTextBuilder` for formatting
4. Apply colors using `ColorPalette`
5. Display using appropriate `BlockDisplayManager` method

### Pattern 2: Fixing Spacing Issue

1. Identify where spacing is wrong
2. Check if it's word spacing or blank line spacing
3. Use appropriate validator to confirm issue
4. Adjust spacing logic or rules
5. Test with validator and visually

### Pattern 3: Fixing Color Issue

1. Identify missing or incorrect color
2. Check if text uses `ColoredTextBuilder`
3. Verify correct `ColorPalette` value
4. Check for double-coloring
5. Test with color validator

---

## 9. Reference Documents

- **Formatting Guide:** `Documentation/04-Reference/FORMATTING_SYSTEM_GUIDE.md`
- **Text Display Rules:** `Documentation/05-Systems/TEXT_DISPLAY_RULES.md`
- **Color System:** `Documentation/05-Systems/COLOR_SYSTEM_*.md`
- **Spacing System:** `Documentation/05-Systems/TEXT_SPACING_SYSTEM.md`

---

## 10. Getting Help

If you encounter issues:

1. **Check validation tools** - Run validators to identify specific issues
2. **Review reference docs** - Check formatting guide and rules
3. **Run test suite** - See if tests reveal the problem
4. **Visual inspection** - Compare against reference output
5. **Debug logging** - Enable debug mode to see overlap warnings

---

**Remember:** The text system is designed to be accurate and consistent. When in doubt, use the validation tools and follow the established patterns!

