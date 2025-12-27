# Combat Color System Refactoring

**Date:** 2025-01-27  
**Purpose:** Centralize combat color decisions for better maintainability and reliability

## Problem Statement

The previous implementation had several issues:

1. **Scattered Color Logic**: Color decisions were embedded directly in formatting code, making it hard to maintain consistency
2. **Duplicated Logic**: The logic for determining if something is critical, combo action, etc. was repeated in multiple places
3. **Hard to Extend**: Adding new outcome types or colors required changes in multiple files
4. **No Single Source of Truth**: Color decisions were made in different ways across different formatters

## Solution

Created a centralized system with two new components:

### 1. `CombatOutcome` Class
Encapsulates all information about a combat outcome:
- `IsCritical`, `IsMiss`, `IsBlock`, `IsDodge`
- `IsComboAction`, `IsCriticalMiss`
- Action reference and roll information
- Factory methods for creating different outcome types

### 2. `CombatColorStrategy` Class
Centralized strategy for determining colors:
- `GetHitsColor()` - Color for "hits" verb
- `GetActionColor()` - Color for action names
- `GetDamageColor()` - Color for damage numbers
- `GetMissColor()` - Color for miss messages
- `GetBlockColor()` - Color for block messages
- `GetDodgeColor()` - Color for dodge messages
- `IsComboAction()` - Helper to determine combo actions

## Benefits

### ✅ Single Source of Truth
All color decisions are now made in one place (`CombatColorStrategy`), ensuring consistency across all formatters.

### ✅ Easier to Maintain
Changing color logic (e.g., "what color should combo actions use?") now only requires updating one method instead of searching through multiple files.

### ✅ More Reliable
The `CombatOutcome` type ensures all relevant information is available when making color decisions, reducing the chance of missing edge cases.

### ✅ Easier to Extend
Adding new outcome types or colors is straightforward:
1. Add properties to `CombatOutcome`
2. Add factory method to `CombatOutcome`
3. Add color method to `CombatColorStrategy`
4. Update formatters to use the new outcome type

### ✅ Better Testability
The color strategy can be tested independently of the formatting logic, making it easier to verify color decisions are correct.

## Migration Path

The refactoring maintains backward compatibility:
- Existing formatter methods still work the same way
- New code can use `CombatOutcome` and `CombatColorStrategy`
- Old code can be gradually migrated

## Example Usage

### Before (Scattered Logic)
```csharp
// Logic embedded in formatter
bool isCritical = totalRoll >= 20;
bool isComboAction = actionName != "BASIC ATTACK";
ColorPalette hitsColor;
if (isCritical)
    hitsColor = ColorPalette.Critical;
else if (isComboAction)
    hitsColor = ColorPalette.Green;
else
    hitsColor = ColorPalette.Damage;
```

### After (Centralized)
```csharp
// Create outcome
var outcome = CombatOutcome.CreateHit(action, totalRoll, roll, isComboAction);

// Get colors from strategy
ColorPalette hitsColor = CombatColorStrategy.GetHitsColor(outcome);
ColorPalette actionColor = CombatColorStrategy.GetActionColor(outcome);
ColorPalette damageColor = CombatColorStrategy.GetDamageColor(outcome);
```

## Files Modified

1. **Created:**
   - `Code/Combat/Formatting/CombatOutcome.cs`
   - `Code/Combat/Formatting/CombatColorStrategy.cs`

2. **Updated:**
   - `Code/Combat/Formatting/DamageFormatter.cs` - Uses new system
   - `Code/Combat/CombatResultsColoredText.cs` - Uses new system for miss messages

## Future Improvements

1. **Data-Driven Colors**: Move color mappings to JSON configuration
2. **Theme Support**: Allow different color schemes per dungeon theme
3. **Accessibility**: Support for colorblind-friendly palettes
4. **Customization**: Allow players to customize combat colors

## Testing Recommendations

1. Test all outcome types (hit, miss, crit, block, dodge)
2. Test combo actions vs basic attacks
3. Test critical hits vs normal hits
4. Test critical misses vs normal misses
5. Verify colors match expected values from `ColorPalette`

