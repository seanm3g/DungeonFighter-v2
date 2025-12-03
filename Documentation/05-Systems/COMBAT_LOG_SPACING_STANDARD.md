# Combat Log Spacing Standard

**Date:** Current  
**Status:** ✅ Standardized  
**Purpose:** Standardize spacing management across the combat log system

---

## Overview

The combat log spacing system has been standardized to eliminate inconsistencies and make spacing management more efficient. This document describes the standardized approach and how to use it.

---

## Two Types of Spacing

The combat log uses two distinct types of spacing:

### 1. **Horizontal Spacing** (Between Words/Segments)
- Managed by: `CombatLogSpacingManager`
- Used for: Spacing between words, segments, and text elements within a line
- Example: `"hits target for 42 damage"` - spaces between words

### 2. **Vertical Spacing** (Between Blocks)
- Managed by: `TextSpacingSystem`
- Used for: Blank lines between different display blocks (actions, narratives, etc.)
- Example: Blank line between enemy stats and first combat action

---

## Standardized Components

### CombatLogSpacingManager

**Location:** `Code/UI/CombatLogSpacingManager.cs`

**Purpose:** Centralized horizontal spacing management for combat log text.

**Key Features:**
- Standard spacing constants (`SingleSpace`, `IndentSpacing`)
- Standardized spacing rules (punctuation handling, whitespace detection)
- Helper methods for formatting and validation
- Consistent spacing logic used throughout the system

**Key Methods:**
```csharp
// Check if space is needed between two text segments
bool ShouldAddSpaceBetween(string? previousText, string? nextText)

// Get space string if needed
string GetSpaceIfNeeded(string? previousText, string? nextText)

// Normalize spacing in a string (remove double spaces)
string NormalizeSpacing(string text)

// Format multiple parts with proper spacing
string FormatWithSpacing(params string?[] parts)

// Validate spacing in text
List<string> ValidateSpacing(string text)
```

### TextSpacingSystem

**Location:** `Code/UI/TextSpacingSystem.cs`

**Purpose:** Manages vertical spacing (blank lines) between display blocks.

**Usage:**
- Call `ApplySpacingBefore(BlockType)` before displaying a block
- Call `RecordBlockDisplayed(BlockType)` after displaying a block
- Call `Reset()` when starting a new battle/dungeon

**See:** `Documentation/05-Systems/TEXT_SPACING_SYSTEM.md` for complete documentation.

### ColoredTextBuilder

**Location:** `Code/UI/ColorSystem/Core/ColoredTextBuilder.cs`

**Purpose:** Builds colored text segments with automatic spacing.

**Standard Usage:**
```csharp
var builder = new ColoredTextBuilder();
builder.Add("hits", Colors.White);
builder.Add("target", ColorPalette.Enemy);
builder.Add("for ", Colors.White);
builder.Add("42", ColorPalette.Damage);
builder.Add("damage", Colors.White);
var result = builder.Build(); // Automatically adds spaces between segments
```

**Key Points:**
- **Don't add spaces manually** - spacing is handled automatically
- Uses `CombatLogSpacingManager` internally for consistent spacing rules
- Text segments should be provided without leading/trailing spaces

---

## Standard Usage Patterns

### Pattern 1: Building ColoredText (Recommended)

**Use:** `ColoredTextBuilder` for all colored text construction.

```csharp
var builder = new ColoredTextBuilder();
builder.Add(attacker.Name, ColorPalette.Player);
builder.Add("hits", Colors.White);
builder.Add(target.Name, ColorPalette.Enemy);
builder.Add("for ", Colors.White);
builder.Add(damage.ToString(), ColorPalette.Damage);
builder.Add("damage", Colors.White);
var coloredText = builder.Build(); // Spacing handled automatically
```

**Benefits:**
- Automatic spacing between segments
- Consistent spacing rules
- No manual space management needed

### Pattern 2: Building Plain Strings

**Use:** `CombatLogSpacingManager.FormatWithSpacing()` for string interpolation.

```csharp
// Instead of:
string text = $"{attacker} hits {target} for {damage} damage";

// Use:
string text = CombatLogSpacingManager.FormatWithSpacing(
    attacker, "hits", target, "for", damage.ToString(), "damage"
);
```

**Benefits:**
- Normalizes spacing automatically
- Handles null/empty values gracefully
- Consistent spacing rules

### Pattern 3: Roll Info Formatting

**Use:** `CombatLogSpacingManager.FormatRollInfo()` for consistent roll info display.

```csharp
string rollInfo = CombatLogSpacingManager.FormatRollInfo(
    rollText: "9 + 2 = 11",
    attackText: "attack 4 - 2 armor",
    speedText: "8.5s",
    amplifierText: "1.5x"
);
// Result: "    (roll: 9 + 2 = 11 | attack 4 - 2 armor | speed: 8.5s | amp: 1.5x)"
```

**Benefits:**
- Standard indentation (4 spaces)
- Consistent formatting
- Handles optional parts gracefully

### Pattern 4: Vertical Spacing (Between Blocks)

**Use:** `TextSpacingSystem` for blank lines between blocks.

```csharp
// Before displaying a combat action
TextSpacingSystem.ApplySpacingBefore(TextSpacingSystem.BlockType.CombatAction);

// Display the action
BlockDisplayManager.DisplayActionBlock(actionText, rollInfo);

// Record that it was displayed
TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.CombatAction);
```

**Benefits:**
- Context-aware spacing (depends on what came before)
- Centralized spacing rules
- Easy to adjust spacing globally

---

## Spacing Rules

### Horizontal Spacing Rules

**Always add space:**
- Between words: `"hits"` + `"target"` → `"hits target"`
- Between numbers and words: `"42"` + `"damage"` → `"42 damage"`

**Never add space:**
- After punctuation: `"!"`, `"?"`, `"."`, `","`, `":"`, `";"`
- Before punctuation: `"!"`, `"?"`, `"."`, `","`, `":"`, `";"`
- After opening brackets: `"["`, `"("`, `"{"`
- Before closing brackets: `"]"`, `")"`, `"}"`
- Around newlines: `"\n"`, `"\r"`

**Examples:**
```csharp
// Correct
"hits target for 42 damage!"
"roll: 9 | attack 4 - 2 armor"

// Incorrect
"hits target for 42 damage !"  // Space before punctuation
"roll : 9 | attack 4 - 2 armor"  // Space after colon
```

### Vertical Spacing Rules

See `Documentation/05-Systems/TEXT_SPACING_SYSTEM.md` for complete vertical spacing rules.

**Key Rules:**
- 1 blank line: Section transitions (dungeon → room, room → enemy, enemy stats → combat)
- 0 blank lines: Consecutive combat actions
- 1 blank line: Before environmental actions
- 1 blank line: Before poison damage
- 1 blank line: Before narratives

---

## Migration Guide

### Before (Inconsistent)

```csharp
// Manual spacing - inconsistent
builder.Add("hits", Colors.White);
builder.Add(" ");  // Manual space
builder.Add(target.Name, ColorPalette.Enemy);
builder.Add(" ");  // Manual space
builder.Add("for", Colors.White);
builder.Add(" ");  // Manual space
builder.Add(damage.ToString(), ColorPalette.Damage);
```

### After (Standardized)

```csharp
// Automatic spacing - consistent
builder.Add("hits", Colors.White);
builder.Add(target.Name, ColorPalette.Enemy);
builder.Add("for", Colors.White);
builder.Add(damage.ToString(), ColorPalette.Damage);
builder.Add("damage", Colors.White);
// Spacing handled automatically by Build()
```

---

## Validation

Use `CombatLogSpacingManager.ValidateSpacing()` to check for spacing issues:

```csharp
var issues = CombatLogSpacingManager.ValidateSpacing(text);
if (issues.Count > 0)
{
    foreach (var issue in issues)
    {
        Console.WriteLine($"Spacing issue: {issue}");
    }
}
```

**Checks:**
- Double spaces
- Spaces before punctuation
- Other spacing inconsistencies

---

## Best Practices

1. **Use ColoredTextBuilder** for all colored text construction
2. **Don't add spaces manually** - let the system handle it
3. **Use TextSpacingSystem** for vertical spacing between blocks
4. **Use CombatLogSpacingManager** for string formatting scenarios
5. **Validate spacing** during development if issues arise
6. **Follow the standard patterns** - don't create custom spacing logic

---

## Troubleshooting

### Issue: Double Spaces

**Cause:** Manual space addition + automatic spacing

**Solution:** Remove manual spaces, let `ColoredTextBuilder` handle it automatically

### Issue: Missing Spaces

**Cause:** Text segments include spaces, which get trimmed

**Solution:** Don't include spaces in text segments - spacing is automatic

### Issue: Incorrect Punctuation Spacing

**Cause:** Manual spacing doesn't respect punctuation rules

**Solution:** Use `CombatLogSpacingManager.ShouldAddSpaceBetween()` or let `ColoredTextBuilder` handle it

### Issue: Inconsistent Vertical Spacing

**Cause:** Not using `TextSpacingSystem` for block spacing

**Solution:** Always use `TextSpacingSystem.ApplySpacingBefore()` before displaying blocks

---

## Related Documentation

- `Documentation/05-Systems/TEXT_SPACING_SYSTEM.md` - Vertical spacing system
- `Documentation/03-Quality/SPACING_ISSUES_INVESTIGATION.md` - Historical spacing issues
- `Documentation/03-Quality/SPACING_FIX_ROLL_INFO.md` - Roll info spacing fix
- `Documentation/02-Development/COLOR_SPACING_FIX.md` - Color spacing fixes

---

## Summary

The combat log spacing system is now standardized:

1. **Horizontal spacing** → `CombatLogSpacingManager` + `ColoredTextBuilder`
2. **Vertical spacing** → `TextSpacingSystem`
3. **Standard patterns** → Use the recommended patterns above
4. **No manual spacing** → Let the system handle it automatically

This standardization makes spacing management more efficient and eliminates inconsistencies.

