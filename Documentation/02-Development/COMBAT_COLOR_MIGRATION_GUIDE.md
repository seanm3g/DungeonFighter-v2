# Combat Color Migration Guide
**Date:** October 12, 2025  
**Status:** ✅ Ready for Use  
**Priority:** HIGH

---

## Overview

This guide shows how to migrate combat message generation from the old color markup system to the new `ColoredText` system.

---

## Benefits of Migration

### Before (Old System)
```csharp
string message = $"{attacker.Name} hits {target.Name} for &R{damage}&y damage";
UIManager.WriteLine(message);
```

**Problems:**
- ❌ Spacing issues from embedded codes
- ❌ Hard to read and maintain
- ❌ Color codes mixed with content
- ❌ Difficult for AI to modify

### After (New System)
```csharp
var message = CombatResults.FormatDamageDisplayColored(attacker, target, rawDamage, actualDamage, action, comboAmplifier, damageMultiplier, rollBonus, roll);
UIManager.WriteLineColoredSegments(message, UIMessageType.Combat);
```

**Benefits:**
- ✅ No spacing issues
- ✅ Clear and readable
- ✅ Separated concerns
- ✅ Easy to modify and maintain

---

## Migration Examples

### 1. Damage Messages

#### Old Way
```csharp
string damageText = $"{attacker.Name} hits {target.Name} for &R{actualDamage}&y damage";
UIManager.WriteLine(damageText);
```

#### New Way
```csharp
var damageMessage = CombatResults.FormatDamageDisplayColored(
    attacker, 
    target, 
    rawDamage, 
    actualDamage, 
    action, 
    comboAmplifier, 
    damageMultiplier, 
    rollBonus, 
    roll
);
UIManager.WriteLineColoredSegments(damageMessage, UIMessageType.Combat);
```

---

### 2. Miss Messages

#### Old Way
```csharp
string missText = $"{attacker.Name} {{{{miss|misses}}}} {target.Name}";
UIManager.WriteLine(missText);
```

#### New Way
```csharp
var missMessage = CombatResults.FormatMissMessageColored(attacker, target, action, roll, rollBonus);
UIManager.WriteLineColoredSegments(missMessage, UIMessageType.Combat);
```

---

### 3. Status Effect Messages

#### Old Way
```csharp
string effectText = $"{target.Name} is affected by &Ypoison&y!";
UIManager.WriteLine(effectText);
```

#### New Way
```csharp
var effectMessage = CombatResults.FormatStatusEffectColored(target, "poison", isApplied: true, duration: 3, stackCount: 2);
UIManager.WriteLineColoredSegments(effectMessage, UIMessageType.Combat);
```

---

### 4. Healing Messages

#### Old Way
```csharp
string healText = $"{healer.Name} heals {target.Name} for &G{amount}&y health!";
UIManager.WriteLine(healText);
```

#### New Way
```csharp
var healMessage = CombatResults.FormatHealingMessageColored(healer, target, amount);
UIManager.WriteLineColoredSegments(healMessage, UIMessageType.Combat);
```

---

### 5. Block/Dodge Messages

#### Old Way
```csharp
string blockText = $"{defender.Name} blocks &B{damageBlocked}&y damage!";
UIManager.WriteLine(blockText);
```

#### New Way
```csharp
// Block
var blockMessage = CombatResults.FormatBlockMessageColored(defender, attacker, damageBlocked);
UIManager.WriteLineColoredSegments(blockMessage, UIMessageType.Combat);

// Dodge
var dodgeMessage = CombatResults.FormatDodgeMessageColored(defender, attacker);
UIManager.WriteLineColoredSegments(dodgeMessage, UIMessageType.Combat);
```

---

### 6. Victory/Defeat Messages

#### Old Way
```csharp
string victoryText = $"{victor.Name} has defeated {defeated.Name}!";
UIManager.WriteLine(victoryText);
```

#### New Way
```csharp
// Victory
var victoryMessage = CombatResults.FormatVictoryMessageColored(victor, defeated);
UIManager.WriteLineColoredSegments(victoryMessage, UIMessageType.Combat);

// Defeat
var defeatMessage = CombatResults.FormatDefeatMessageColored(victor, defeated);
UIManager.WriteLineColoredSegments(defeatMessage, UIMessageType.Combat);
```

---

### 7. Health Milestones

#### Old Way
```csharp
if (healthPercentage <= 0.25)
{
    UIManager.WriteLine($"{entity.Name} is critically wounded!");
}
```

#### New Way
```csharp
if (healthPercentage <= 0.25)
{
    var milestoneMessage = CombatResults.FormatHealthMilestoneColored(entity, healthPercentage);
    UIManager.WriteLineColoredSegments(milestoneMessage, UIMessageType.Combat);
}
```

---

## Available Helper Methods

### CombatResultsColoredText Class

All methods in `CombatResultsColoredText` return `List<ColoredText>` which can be passed to `UIManager.WriteLineColoredSegments()`.

| Method | Purpose | Parameters |
|--------|---------|-----------|
| `FormatDamageDisplayColored()` | Formats damage messages | attacker, target, damages, action info |
| `FormatRollInfoColored()` | Formats roll details | roll, bonus, attack, defense, speed |
| `FormatMissMessageColored()` | Formats miss messages | attacker, target, action, roll info |
| `FormatNonAttackActionColored()` | Formats buff/debuff actions | source, target, action, roll info |
| `FormatHealthMilestoneColored()` | Formats health warnings | entity, health percentage |
| `FormatBlockMessageColored()` | Formats block messages | defender, attacker, damage blocked |
| `FormatDodgeMessageColored()` | Formats dodge messages | defender, attacker |
| `FormatStatusEffectColored()` | Formats status effects | target, effect, applied, duration, stacks |
| `FormatHealingMessageColored()` | Formats healing | healer, target, amount |
| `FormatVictoryMessageColored()` | Formats victory | victor, defeated |
| `FormatDefeatMessageColored()` | Formats defeat | victor, defeated |

### CombatResults Wrapper Methods

The `CombatResults` class now includes wrapper methods that call the colored text versions:

```csharp
// All of these return List<ColoredText>
CombatResults.FormatDamageDisplayColored(...)
CombatResults.FormatMissMessageColored(...)
CombatResults.FormatNonAttackActionColored(...)
// ... etc
```

---

## Migration Checklist

### Step 1: Identify Color Markup
Search for patterns like:
- `&R`, `&G`, `&B`, `&Y` (old color codes)
- `{{template|text}}` (template markup)
- Any string concatenation with colors

### Step 2: Replace with ColoredText Methods
Use the appropriate helper method from `CombatResultsColoredText` or `CombatResults`.

### Step 3: Update UIManager Calls
Change from:
```csharp
UIManager.WriteLine(message);
```

To:
```csharp
UIManager.WriteLineColoredSegments(coloredMessage, UIMessageType.Combat);
```

### Step 4: Test
- Verify colors display correctly
- Check for spacing issues (there shouldn't be any!)
- Ensure text wrapping works properly

---

## Complete Example: Migrating a Combat Turn

### Before (Old System)
```csharp
public void ProcessAttack(Entity attacker, Entity target, Action action)
{
    int roll = Dice.Roll(20);
    int rollBonus = attacker.GetAttackBonus();
    int totalRoll = roll + rollBonus;
    
    if (totalRoll >= target.Defense)
    {
        int damage = CalculateDamage(attacker, action);
        target.TakeDamage(damage);
        
        string message = $"{attacker.Name} hits {target.Name} for &R{damage}&y damage";
        UIManager.WriteLine(message);
        
        if (target.CurrentHealth <= 0)
        {
            UIManager.WriteLine($"{attacker.Name} has defeated {target.Name}!");
        }
    }
    else
    {
        string missMessage = $"{attacker.Name} misses {target.Name}";
        UIManager.WriteLine(missMessage);
    }
}
```

### After (New System)
```csharp
public void ProcessAttack(Entity attacker, Entity target, Action action)
{
    int roll = Dice.Roll(20);
    int rollBonus = attacker.GetAttackBonus();
    int totalRoll = roll + rollBonus;
    
    if (totalRoll >= target.Defense)
    {
        int damage = CalculateDamage(attacker, action);
        int rawDamage = damage; // Before any reductions
        target.TakeDamage(damage);
        
        var damageMessage = CombatResults.FormatDamageDisplayColored(
            attacker, target, rawDamage, damage, action, 
            comboAmplifier: 1.0, damageMultiplier: 1.0, rollBonus, roll
        );
        UIManager.WriteLineColoredSegments(damageMessage, UIMessageType.Combat);
        
        if (target.CurrentHealth <= 0)
        {
            var victoryMessage = CombatResults.FormatVictoryMessageColored(attacker, target);
            UIManager.WriteLineColoredSegments(victoryMessage, UIMessageType.Combat);
        }
    }
    else
    {
        var missMessage = CombatResults.FormatMissMessageColored(attacker, target, action, roll, rollBonus);
        UIManager.WriteLineColoredSegments(missMessage, UIMessageType.Combat);
    }
}
```

---

## Custom Combat Messages

If you need a custom combat message not covered by the helpers:

```csharp
var builder = new ColoredTextBuilder();

// Build your custom message
builder.Add("Custom ", Colors.White);
builder.Add("combat", ColorPalette.Damage);
builder.Add(" message with ", Colors.White);
builder.Add("multiple", ColorPalette.Success);
builder.Add(" colors!", Colors.White);

var message = builder.Build();
UIManager.WriteLineColoredSegments(message, UIMessageType.Combat);
```

---

## Performance Considerations

The new system is **faster** than the old system because:
- ✅ Single-pass rendering (no multiple string transformations)
- ✅ No regex parsing for every message
- ✅ Efficient color lookups
- ✅ Reduced string concatenation

---

## Testing Your Migration

1. **Visual Testing**
   - Run combat scenarios
   - Verify colors display correctly
   - Check for spacing issues

2. **Functional Testing**
   - Test all combat message types
   - Verify critical hits show proper colors
   - Test status effects display correctly

3. **Performance Testing**
   - Compare rendering speed with old system
   - Should be equal or faster

---

## Troubleshooting

### Issue: Colors not displaying
**Solution:** Make sure you're using `UIManager.WriteLineColoredSegments()` not `WriteLine()`

### Issue: Spacing looks wrong
**Solution:** The new system shouldn't have spacing issues. If you see them, verify you're using `ColoredText` objects, not string concatenation.

### Issue: Missing color
**Solution:** Check that the `ColorPalette` enum has the color you need. Add it if missing.

---

## Next Steps

Once combat messages are migrated:
1. ✅ Combat messages (DONE)
2. ⏳ Title screen animations
3. ⏳ UI menus
4. ⏳ Item displays
5. ⏳ Dungeon descriptions

---

**Status:** ✅ Combat color migration infrastructure complete  
**Estimated Effort:** 2-4 hours to migrate all combat code  
**Risk Level:** LOW (backward compatible, can migrate incrementally)  
**Priority:** HIGH (improves code quality and maintainability)
