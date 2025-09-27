# Armor Breakdown Display Enhancement

## Enhancement Request
User requested that damage displays show the armor calculation breakdown in the format:
`deals X damage -(Y armor) = Z damage`

## Implementation

### New Helper Method
Created `FormatDamageDisplay()` method in `Combat.cs` that:
- Takes raw damage, actual damage, and target information
- Calculates target's armor value
- Returns formatted string showing the breakdown

```csharp
public static string FormatDamageDisplay(Entity attacker, Entity target, int rawDamage, int actualDamage, Action? action = null, double comboAmplifier = 1.0, double damageMultiplier = 1.0, int rollBonus = 0, int roll = 0)
{
    // Get target's armor
    int armor = 0;
    if (target is Character targetChar)
    {
        if (targetChar.Head is HeadItem head) armor += head.GetTotalArmor();
        if (targetChar.Body is ChestItem chest) armor += chest.GetTotalArmor();
        if (targetChar.Feet is FeetItem feet) armor += feet.GetTotalArmor();
    }
    
    // If no armor or armor doesn't reduce damage, show simple format
    if (armor == 0 || actualDamage == rawDamage)
    {
        return $"{actualDamage} damage";
    }
    
    // Show breakdown format: deals X damage -(Y armor) = Z damage
    return $"{rawDamage} damage -({armor} armor) = {actualDamage} damage";
}
```

### Updated All Damage Displays
Modified all damage display messages throughout the codebase to use the new formatting:

#### Player Attacks
- Basic attacks
- Critical attacks
- Combo attacks
- Unique actions
- Auto-success attacks
- Divine reroll attacks
- DEJA VU attacks

#### Enemy Attacks
- All enemy attack damage displays

### Display Examples

#### Before Enhancement
```
[Kobold] uses [Sneak Attack] on [Pax Moonwhisper]: deals 8 damage.
```
*Player couldn't see why 48 raw damage became 8 actual damage*

#### After Enhancement
```
[Kobold] uses [Sneak Attack] on [Pax Moonwhisper]: deals 48 damage -(40 armor) = 8 damage.
```
*Player can clearly see the armor reduction calculation*

#### No Armor Example
```
[Kobold] uses [Sneak Attack] on [Goblin]: deals 14 damage.
```
*Simple format when no armor is involved*

## Benefits

1. **Transparency**: Players can see exactly how armor affects damage
2. **Educational**: Helps players understand the armor system
3. **Debugging**: Makes it easier to verify armor calculations
4. **Consistency**: All damage displays now use the same format
5. **Clarity**: No more confusion about why high damage numbers don't match health loss

## Technical Details

- **Smart Formatting**: Only shows breakdown when armor actually reduces damage
- **Simple Format**: Shows just the final damage when no armor is involved
- **Comprehensive**: Covers all attack types (basic, critical, combo, unique, etc.)
- **Efficient**: Reuses existing armor calculation logic
- **Maintainable**: Single method handles all formatting logic

## Files Modified

- `Code/Combat.cs`: Added `FormatDamageDisplay()` method and updated all damage displays
- `Code/Enemy.cs`: Updated enemy damage displays to use new formatting

This enhancement makes the combat system much more transparent and educational for players, clearly showing how armor protects against incoming damage.
