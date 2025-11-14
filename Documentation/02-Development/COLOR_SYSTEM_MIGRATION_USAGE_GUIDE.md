# Color System Migration Usage Guide
**Date:** October 12, 2025  
**Status:** ✅ Ready for Use  
**Priority:** HIGH

---

## Overview

This guide provides complete examples of how to use the new ColoredText system in your code. All formatters are ready to use and require minimal code changes.

---

## Quick Start

### Basic Pattern

```csharp
// 1. Call the appropriate formatter
var coloredText = [Helper].Format[Something](parameters);

// 2. Display using UIManager
UIManager.WriteLineColoredSegments(coloredText, UIMessageType.System);
```

That's it! No color codes, no spacing issues, no complexity.

---

## Item Display Examples

### Display Simple Item Name

```csharp
// Using the new system
var itemName = ItemDisplayFormatter.GetColoredItemNameNew(item);
UIManager.WriteLineColoredSegments(itemName);

// Or directly
var itemName = ItemDisplayColoredText.FormatSimpleItemName(item);
UIManager.WriteLineColoredSegments(itemName);
```

### Display Full Item Name (with Prefixes/Suffixes)

```csharp
var fullName = ItemDisplayFormatter.GetColoredFullItemNameNew(item);
UIManager.WriteLineColoredSegments(fullName);
```

### Display Item with Rarity Tag

```csharp
var itemWithRarity = ItemDisplayFormatter.GetColoredItemWithRarityNew(item);
UIManager.WriteLineColoredSegments(itemWithRarity);
// Output: "Flaming Steel Sword of Power [Legendary]"
```

### Display Complete Item Stats

```csharp
var statsLines = ItemDisplayFormatter.FormatItemStatsNew(item);
foreach (var line in statsLines)
{
    UIManager.WriteLineColoredSegments(line);
}
// Output:
//   Type: Weapon | Tier: 5
//   Damage: 25
//   Stats: +5 STR, +3 AGI
//   Modifiers: Flaming, of Power
```

### Display Item in Inventory List

```csharp
for (int i = 0; i < inventory.Count; i++)
{
    var itemLine = ItemDisplayFormatter.FormatInventoryItemNew(i + 1, inventory[i]);
    UIManager.WriteLineColoredSegments(itemLine);
}
// Output:
// 1. Steel Sword [Weapon T3]
// 2. Iron Helmet [Head T2]
// 3. Health Potion [Consumable T1]
```

### Display Equipped Item

```csharp
var weaponDisplay = ItemDisplayFormatter.FormatEquippedItemNew("Weapon", character.EquippedWeapon);
UIManager.WriteLineColoredSegments(weaponDisplay);
// Output: "Weapon: Steel Sword (15 dmg)"

var emptySlot = ItemDisplayFormatter.FormatEquippedItemNew("Head", null);
UIManager.WriteLineColoredSegments(emptySlot);
// Output: "Head: (empty)"
```

### Display Item Comparison

```csharp
var comparison = ItemDisplayFormatter.FormatItemComparisonNew(newItem, currentItem);
foreach (var line in comparison)
{
    UIManager.WriteLineColoredSegments(line);
}
// Output:
// NEW: Flaming Steel Sword [Legendary]
//   Type: Weapon | Tier: 5
//   Damage: 25
//   Stats: +5 STR
//
// CURRENT: Iron Sword [Common]
//   Type: Weapon | Tier: 2
//   Damage: 10
//   Stats: +2 STR
```

### Display Loot Drop

```csharp
var lootMessage = ItemDisplayFormatter.FormatLootDropNew(item);
UIManager.WriteLineColoredSegments(lootMessage);
// Output: "Found: Flaming Steel Sword of Power [Legendary]!"
```

---

## Menu Display Examples

### Display Menu Title

```csharp
var title = MenuDisplayColoredText.FormatMenuTitle("Main Menu", centered: true);
UIManager.WriteLineColoredSegments(title);
// Output: "=== Main Menu ==="
```

### Display Menu with Options

```csharp
var options = new List<string> { "New Game", "Load Game", "Settings", "Exit" };
var menuLines = MenuDisplayColoredText.FormatMenu("Main Menu", options, selectedIndex: 0);
foreach (var line in menuLines)
{
    UIManager.WriteLineColoredSegments(line);
}
// Output:
// === Main Menu ===
//
// 1. New Game     (highlighted)
// 2. Load Game
// 3. Settings
// 4. Exit
```

### Display Menu Option

```csharp
var option = MenuDisplayColoredText.FormatMenuOption(1, "New Game", selected: true);
UIManager.WriteLineColoredSegments(option);
// Output: "1. New Game" (highlighted)
```

### Display Section Header

```csharp
var header = MenuDisplayColoredText.FormatSectionHeader("Character Stats");
UIManager.WriteLineColoredSegments(header);
// Output: "--- Character Stats ---"
```

### Display Key-Value Pair

```csharp
var keyValue = MenuDisplayColoredText.FormatKeyValue("Level", "5");
UIManager.WriteLineColoredSegments(keyValue);
// Output: "Level: 5"
```

### Display Messages

```csharp
// Success
var success = MenuDisplayColoredText.FormatSuccess("Item equipped successfully!");
UIManager.WriteLineColoredSegments(success);
// Output: "✓ Item equipped successfully!"

// Error
var error = MenuDisplayColoredText.FormatError("Invalid selection!");
UIManager.WriteLineColoredSegments(error);
// Output: "ERROR: Invalid selection!"

// Warning
var warning = MenuDisplayColoredText.FormatWarning("Low health!");
UIManager.WriteLineColoredSegments(warning);
// Output: "⚠ Low health!"

// Info
var info = MenuDisplayColoredText.FormatInfo("Press any key to continue...");
UIManager.WriteLineColoredSegments(info);
// Output: "ℹ Press any key to continue..."
```

### Display Status Bar

```csharp
var healthBar = MenuDisplayColoredText.FormatStatusBar("HP", 75, 100, barWidth: 20);
UIManager.WriteLineColoredSegments(healthBar);
// Output: "HP: [███████████████░░░░░] 75/100"
```

### Display Confirmation

```csharp
var confirmation = MenuDisplayColoredText.FormatConfirmation("Are you sure?");
foreach (var line in confirmation)
{
    UIManager.WriteLineColoredSegments(line);
}
// Output:
// Are you sure?
//
// 1. Yes
// 2. No
```

### Display Dungeon Option

```csharp
var dungeonOption = MenuDisplayColoredText.FormatDungeonOption(1, "Dark Cave", 5, "Normal");
UIManager.WriteLineColoredSegments(dungeonOption);
// Output: "1. Dark Cave [Lvl 5] (Normal)"
```

### Display Progress

```csharp
var progress = MenuDisplayColoredText.FormatProgress("Level", 5, 10);
UIManager.WriteLineColoredSegments(progress);
// Output: "Level: 5 / 10 (50%)"
```

---

## Character Display Examples

### Display Character Header

```csharp
var header = CharacterDisplayColoredText.FormatCharacterHeader(
    character.Name, 
    character.CharacterClass, 
    character.Level
);
UIManager.WriteLineColoredSegments(header);
// Output: "Hero - Level 5 Warrior"
```

### Display Health

```csharp
var health = CharacterDisplayColoredText.FormatHealth(
    character.CurrentHealth, 
    character.GetEffectiveMaxHealth()
);
UIManager.WriteLineColoredSegments(health);
// Output: "HP: 75/100"
```

### Display Health Bar

```csharp
var healthBar = CharacterDisplayColoredText.FormatHealthBar(
    character.CurrentHealth, 
    character.GetEffectiveMaxHealth()
);
UIManager.WriteLineColoredSegments(healthBar);
// Output: "HP: [███████████████░░░░░] 75/100"
```

### Display Experience

```csharp
var xp = CharacterDisplayColoredText.FormatExperience(
    character.Experience, 
    character.ExperienceToNextLevel
);
UIManager.WriteLineColoredSegments(xp);
// Output: "XP: 450 / 600 (75%)"
```

### Display Single Stat

```csharp
var stat = CharacterDisplayColoredText.FormatStat("STR", 10, 2);
UIManager.WriteLineColoredSegments(stat);
// Output: "STR: 10 (+2)"
```

### Display All Stats

```csharp
var statsLines = CharacterDisplayColoredText.FormatStats(character);
foreach (var line in statsLines)
{
    UIManager.WriteLineColoredSegments(line);
}
// Output:
// STR: 10 (+2)
// AGI: 8 (+1)
// TEC: 7
// INT: 6
```

### Display Combat Stats

```csharp
var combatStats = CharacterDisplayColoredText.FormatCombatStats(character);
foreach (var line in combatStats)
{
    UIManager.WriteLineColoredSegments(line);
}
// Output:
// Attack: 15
// Defense: 10
// Speed: 1.00
```

### Display Equipment Summary

```csharp
var equipment = CharacterDisplayColoredText.FormatEquipmentSummary(character);
foreach (var line in equipment)
{
    UIManager.WriteLineColoredSegments(line);
}
// Output:
// Weapon: Steel Sword (15 dmg)
// Head: Iron Helmet (5 armor)
// Body: Chain Mail (10 armor)
// Feet: Leather Boots (3 armor)
```

### Display Complete Character Sheet

```csharp
var sheet = CharacterDisplayColoredText.FormatCharacterSheet(character);
foreach (var line in sheet)
{
    UIManager.WriteLineColoredSegments(line);
}
// Output: Complete formatted character sheet with all stats, equipment, etc.
```

### Display Level Up

```csharp
var levelUp = CharacterDisplayColoredText.FormatLevelUp(character.Name, newLevel);
UIManager.WriteLineColoredSegments(levelUp);
// Output: "★ Hero reached level 6! ★"
```

### Display Stat Increase

```csharp
var statIncrease = CharacterDisplayColoredText.FormatStatIncrease("STR", 10, 11);
UIManager.WriteLineColoredSegments(statIncrease);
// Output: "STR: 10 → 11 (+1)"
```

### Display Combat Summary

```csharp
var summary = CharacterDisplayColoredText.FormatCombatSummary(character);
UIManager.WriteLineColoredSegments(summary);
// Output: "Hero [Lvl 5] HP: 75/100"
```

---

## Combat Display Examples

### Display Damage Message

```csharp
var damage = CombatResults.FormatDamageDisplayColored(
    attacker, target, rawDamage, actualDamage, action, 
    comboAmplifier, damageMultiplier, rollBonus, roll
);
UIManager.WriteLineColoredSegments(damage, UIMessageType.Combat);
// Output: "Player hits Enemy for 25 damage"
```

### Display Miss Message

```csharp
var miss = CombatResults.FormatMissMessageColored(attacker, target, action, roll, rollBonus);
UIManager.WriteLineColoredSegments(miss, UIMessageType.Combat);
// Output: "Player misses Enemy"
```

### Display Status Effect

```csharp
var effect = CombatResults.FormatStatusEffectColored(target, "poison", isApplied: true, duration: 3, stackCount: 2);
UIManager.WriteLineColoredSegments(effect, UIMessageType.Combat);
// Output: "Enemy is affected by poison (x2) [3 turns]!"
```

### Display Healing

```csharp
var healing = CombatResults.FormatHealingMessageColored(healer, target, amount);
UIManager.WriteLineColoredSegments(healing, UIMessageType.Combat);
// Output: "Player heals Player for 30 health!"
```

### Display Victory

```csharp
var victory = CombatResults.FormatVictoryMessageColored(victor, defeated);
UIManager.WriteLineColoredSegments(victory, UIMessageType.Combat);
// Output: "Player has defeated Enemy!"
```

---

## Custom Colored Text

If none of the formatters fit your needs, you can build custom colored text:

```csharp
var builder = new ColoredTextBuilder();

builder.Add("Custom ", Colors.White);
builder.Add("colored", ColorPalette.Success);
builder.Add(" text with ", Colors.White);
builder.Add("multiple", ColorPalette.Warning);
builder.Add(" colors!", Colors.White);

var result = builder.Build();
UIManager.WriteLineColoredSegments(result);
```

Or use patterns:

```csharp
var builder = new ColoredTextBuilder();

builder.Add("Player ", ColorPalette.Player);
builder.AddWithPattern("deals", "damage");
builder.Add(" ", Colors.White);
builder.AddWithPattern("critical", "critical");
builder.Add(" damage!", Colors.White);

var result = builder.Build();
UIManager.WriteLineColoredSegments(result);
```

---

## Migration Checklist

When migrating existing code:

1. ✅ **Find color markup** - Search for `&R`, `&G`, `{{template}}`, etc.
2. ✅ **Identify the display type** - Is it an item? menu? character stat?
3. ✅ **Use appropriate formatter** - Call the matching formatter method
4. ✅ **Update UIManager call** - Use `WriteLineColoredSegments()` instead of `WriteLine()`
5. ✅ **Test visually** - Verify colors and spacing look correct
6. ✅ **Remove old code** - Clean up unused color markup

---

## Common Patterns

### Pattern 1: Display List of Items

```csharp
for (int i = 0; i < items.Count; i++)
{
    var itemLine = ItemDisplayColoredText.FormatInventoryItem(i + 1, items[i]);
    UIManager.WriteLineColoredSegments(itemLine);
}
```

### Pattern 2: Display Menu with Selection

```csharp
var menuLines = MenuDisplayColoredText.FormatMenu("Title", options, selectedIndex);
foreach (var line in menuLines)
{
    UIManager.WriteLineColoredSegments(line);
}
```

### Pattern 3: Display Multi-Line Data

```csharp
var dataLines = SomeFormatter.FormatSomething(data);
foreach (var line in dataLines)
{
    UIManager.WriteLineColoredSegments(line);
}
```

### Pattern 4: Display Single Line

```csharp
var line = SomeFormatter.FormatSomething(data);
UIManager.WriteLineColoredSegments(line);
```

---

## Performance Tips

1. **Cache results** - If displaying the same text multiple times, cache the ColoredText list
2. **Batch operations** - Format all lines first, then display them
3. **Use appropriate formatter** - Don't build custom when a formatter exists
4. **Avoid string concatenation** - Use ColoredTextBuilder methods

---

## Troubleshooting

### Issue: Colors not showing
**Solution:** Make sure you're calling `UIManager.WriteLineColoredSegments()` not `WriteLine()`

### Issue: Wrong colors
**Solution:** Check that you're using the correct formatter for your data type

### Issue: Spacing looks wrong
**Solution:** This shouldn't happen with ColoredText! If it does, file a bug report

### Issue: Performance slow
**Solution:** Check if you're recreating ColoredText unnecessarily - cache when possible

---

## Quick Reference

| Display Type | Formatter Class | Example Method |
|--------------|----------------|----------------|
| Items | `ItemDisplayColoredText` | `FormatSimpleItemName()` |
| Menus | `MenuDisplayColoredText` | `FormatMenuOption()` |
| Characters | `CharacterDisplayColoredText` | `FormatCharacterHeader()` |
| Combat | `CombatResultsColoredText` | `FormatDamageDisplayColored()` |
| Custom | `ColoredTextBuilder` | `.Add()` methods |

---

**Status:** ✅ Complete and ready to use  
**Last Updated:** October 12, 2025  
**Priority:** HIGH - Use this system for all new colored text!
