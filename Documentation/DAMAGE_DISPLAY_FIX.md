# Damage Display Fix - Show Actual Damage Dealt

## Problem Identified
The combat log was showing raw damage before armor reduction, which was confusing and misleading:

**Example from terminal:**
```
[Kobold] uses [Sneak Attack] on [Pax Moonwhisper]: deals 48 damage.
```

But the actual damage dealt was only 8 damage (48 - 40 armor = 8), making the display inaccurate.

## Root Cause
The damage display was using `finalEffect` (raw damage before armor) instead of the actual damage dealt after armor reduction.

## Fix Applied
Modified all damage display messages to show the actual damage dealt by calling `CalculateDamage()` which includes armor reduction.

### Files Modified

#### `Code/Combat.cs`
**Before:**
```csharp
string result = $"[{source.Name}] uses [{selectedAction.Name}] on [{target.Name}]: deals {finalEffect} damage";
```

**After:**
```csharp
int actualDamage = CalculateDamage(source, target, selectedAction, comboAmplifier, damageMultiplier, rollBonus, roll);
string result = $"[{source.Name}] uses [{selectedAction.Name}] on [{target.Name}]: deals {actualDamage} damage";
```

**Fixed all damage displays:**
- Basic attacks
- Critical attacks  
- Combo attacks
- Unique actions
- Auto-success attacks
- Divine reroll attacks
- DEJA VU attacks
- Enemy attacks

#### `Code/Enemy.cs`
**Before:**
```csharp
string actionResult = $"[{Name}] uses [{action.Name}] on [{target.Name}]: deals {finalEffect} damage";
```

**After:**
```csharp
int actualDamage = Combat.CalculateDamage(this, target, action, 1.0, 1.0, 0, roll);
string actionResult = $"[{Name}] uses [{action.Name}] on [{target.Name}]: deals {actualDamage} damage";
```

## Expected Results

### Before Fix
```
[Kobold] uses [Sneak Attack] on [Pax Moonwhisper]: deals 48 damage.
```
*Hero has 40 armor, so only takes 8 damage but display shows 48*

### After Fix
```
[Kobold] uses [Sneak Attack] on [Pax Moonwhisper]: deals 8 damage.
```
*Display now shows the actual 8 damage dealt after armor reduction*

## Benefits
1. **Accurate Information**: Players see exactly how much damage was actually dealt
2. **Clear Combat Feedback**: No confusion about why high damage numbers don't match health loss
3. **Better Game Understanding**: Players can see the effectiveness of armor
4. **Consistent Display**: All damage messages now show actual damage dealt

## Technical Details
- Uses existing `CalculateDamage()` method which already handles armor reduction
- No changes to damage calculation logic, only display
- Maintains all existing damage modifiers (critical hits, combos, etc.)
- Works for both player and enemy attacks

This fix ensures that the combat log accurately reflects the actual damage dealt to characters, making the game much clearer and more intuitive for players.
