# Equipment Managers - Quick Reference

## Quick Navigation

- [EquipmentSlotManager](#equipmentslotmanager) - Slot management
- [EquipmentBonusCalculator](#equipmentbonuscalculator) - Equipment stat bonuses
- [ModificationBonusCalculator](#modificationbonuscalculator) - Modification bonuses
- [ArmorStatusManager](#armorstatusmanager) - Armor effects
- [EquipmentActionProvider](#equipmentactionprovider) - Gear actions

---

## EquipmentSlotManager

**File**: `Code/Entity/EquipmentSlotManager.cs`

**Purpose**: Manage the 4 equipment slots (Head, Body, Weapon, Feet)

### Properties
```csharp
Item? Head { get; set; }
Item? Body { get; set; }
Item? Weapon { get; set; }
Item? Feet { get; set; }
```

### Main Methods
```csharp
// Equip an item to a slot, returns previous item
Item? EquipItem(Item item, string slot);

// Unequip from a slot
Item? UnequipItem(string slot);

// Get all equipped items
Item?[] GetEquippedItems();

// Check if slot is equipped
bool IsSlotEquipped(string slot);

// Get item in a slot
Item? GetSlotItem(string slot);

// Unequip all items
void UnequipAll();
```

### Example Usage
```csharp
var slotManager = new EquipmentSlotManager();

// Equip an item
var oldItem = slotManager.EquipItem(sword, "weapon");

// Check what's equipped
if (slotManager.IsSlotEquipped("head"))
{
    var helmet = slotManager.GetSlotItem("head");
}

// Get all equipped items
var equipped = slotManager.GetEquippedItems();
foreach (var item in equipped)
{
    if (item != null)
        Console.WriteLine(item.Name);
}
```

### Supported Slot Names
- "head" or "HEAD"
- "body" or "BODY"
- "weapon" or "WEAPON"
- "feet" or "FEET"

---

## EquipmentBonusCalculator

**File**: `Code/Entity/EquipmentBonusCalculator.cs`

**Purpose**: Calculate stat bonuses from equipped items

### Constructor
```csharp
var calculator = new EquipmentBonusCalculator(slotManager);
```

### Main Methods
```csharp
// Generic stat bonus calculation
int GetStatBonus(string statType);
double GetStatBonusDouble(string statType);

// Specific bonus getters
int GetDamageBonus();
int GetHealthBonus();
int GetRollBonus();
int GetMagicFind();  // Includes modification bonuses
double GetAttackSpeedBonus();
int GetHealthRegenBonus();

// Armor calculations
int GetTotalArmor();
int GetTotalRerollCharges();

// Special checks
bool HasAutoSuccess();
```

### Supported Stat Types
- "Damage" - Physical damage
- "Health" - Maximum health
- "RollBonus" - Roll/accuracy bonus
- "MagicFind" - Loot luck
- "Armor" - Armor points
- "AttackSpeed" - Attack speed modifier
- "HealthRegen" - Health regeneration
- "ALL" - Bonus applies to all stats

### Example Usage
```csharp
var equipment = new CharacterEquipment();
equipment.EquipItem(armorPiece, "body");

// Get individual bonuses
int damageBonus = equipment.GetEquipmentDamageBonus();  // 5
int armorBonus = equipment.GetEquipmentStatBonus("Armor");  // 3

// Get total armor (includes armor piece calculations)
int totalArmor = equipment.GetTotalArmor();  // 15

// Get attack speed modifier
double speedBonus = equipment.GetEquipmentAttackSpeedBonus();  // 1.05

// Check for special effects
if (equipment.HasAutoSuccess())
{
    // Next attack guaranteed to hit
}
```

---

## ModificationBonusCalculator

**File**: `Code/Entity/ModificationBonusCalculator.cs`

**Purpose**: Calculate all modification-based bonuses from equipment

### Constructor
```csharp
var calculator = new ModificationBonusCalculator(slotManager);
```

### Main Methods
```csharp
// Specific modification bonuses
int GetMagicFind();
int GetRollBonus();
int GetDamageBonus();
int GetGodlikeBonus();

// Multiplicative bonuses
double GetSpeedMultiplier();      // e.g., 1.25 = 125%
double GetDamageMultiplier();     // e.g., 1.5 = 150%

// Additive bonuses
double GetLifesteal();            // e.g., 0.15 = 15%
double GetBleedChance();          // e.g., 0.3 = 30%
double GetUniqueActionChance();   // e.g., 0.2 = 20%

// Query methods
List<ItemModification> GetAllModifications();
List<ItemModification> GetModificationsByType(string effectType);
```

### Supported Modification Types
- "magicFind" - Loot luck (additive)
- "rollBonus" - Roll bonus (additive)
- "damage" - Damage bonus (additive)
- "speedMultiplier" - Speed multiplier (multiplicative)
- "damageMultiplier" - Damage multiplier (multiplicative)
- "lifesteal" - Lifesteal (additive)
- "godlike" - Godlike effect (additive)
- "bleedChance" - Bleed chance (additive)
- "uniqueActionChance" - Unique action trigger (additive)
- "reroll" - Reroll charges (counter)
- "autoSuccess" - Auto success (flag)

### Example Usage
```csharp
var equipment = new CharacterEquipment();
equipment.EquipItem(specialSword, "weapon");

// Get modification bonuses
double speedMult = equipment.GetModificationSpeedMultiplier();  // 1.15
double dmgMult = equipment.GetModificationDamageMultiplier();   // 1.2
double lifesteal = equipment.GetModificationLifesteal();        // 0.1

// Calculate total damage with all multipliers
double baseDamage = 100;
double totalDamage = baseDamage * dmgMult;  // 120

// Get all active modifications
var mods = equipment.GetAllModifications();

// Filter for specific modification type
var damageBoosts = equipment.GetModificationsByType("damageMultiplier");
Console.WriteLine($"Found {damageBoosts.Count} damage multiplier mods");
```

---

## ArmorStatusManager

**File**: `Code/Entity/ArmorStatusManager.cs`

**Purpose**: Manage armor statuses and effects from equipped armor

### Constructor
```csharp
var manager = new ArmorStatusManager(slotManager);
```

### Main Methods
```csharp
// Get armor effects
double GetArmorSpikeDamage();

// Query armor statuses
List<ArmorStatus> GetAllArmorStatuses();
List<ArmorStatus> GetStatusesByEffect(string effectType);

// Get effect values
double GetStatusEffectValue(string effectType);
int GetStatusEffectCount(string effectType);

// Check effects
bool HasArmorStatus(string effectType);
```

### Common Armor Status Effects
- "armorSpikes" - Reflection damage
- "thorns" - Counter damage
- "barrier" - Damage reduction
- "thorny" - On-hit damage

### Example Usage
```csharp
var equipment = new CharacterEquipment();
equipment.EquipItem(spikeyArmor, "body");

// Get total spike damage
double spikeDamage = equipment.GetArmorSpikeDamage();

// Get all active armor effects
var statuses = equipment.GetEquippedArmorStatuses();
foreach (var status in statuses)
{
    Console.WriteLine($"{status.Effect}: {status.Value}");
}

// Check specific effect
if (equipment.HasArmorStatus("armorSpikes"))
{
    // Apply reflection damage on hit
    int reflectionDamage = (int)equipment.GetArmorSpikeDamage();
}
```

---

## EquipmentActionProvider

**File**: `Code/Entity/EquipmentActionProvider.cs`

**Purpose**: Manage and provide actions from equipped gear

### Constructor
```csharp
var provider = new EquipmentActionProvider(slotManager);
```

### Main Methods
```csharp
// Get actions from specific gear
List<string> GetGearActions(Item gear);

// Get all available actions from equipped gear
List<string> GetAllEquippedGearActions();
```

### How It Works

**Weapon Actions**:
- Loads from `Actions.json` using weapon type tag
- Special handling for Mace weapon type
- Returns weapon-specific actions

**Armor Actions**:
- Checks if armor has special properties (modifications, bonuses)
- Returns custom gear action if defined
- Falls back to random armor action
- Excludes basic starting gear

**Action Bonuses**:
- Adds any action bonuses defined on the item itself

### Example Usage
```csharp
var equipment = new CharacterEquipment();
equipment.EquipItem(sword, "weapon");
equipment.EquipItem(armor, "body");

// Get actions from specific item
var swordActions = equipment.GetGearActions(sword);
// Result: ["SWORD SLASH", "PARRY", "RIPOSTE"]

// Get all available actions from all equipped gear
var allActions = equipment.GetAllEquippedGearActions();
// Result: ["SWORD SLASH", "PARRY", "RIPOSTE", "SHIELD BASH", ...]

// Use actions in combat
foreach (var actionName in allActions)
{
    Console.WriteLine($"Available action: {actionName}");
}
```

### Special Cases

**Mace Weapons**:
```csharp
// Maces always have these actions:
// - "CRUSHING BLOW"
// - "SHIELD BREAK"
// - "THUNDER CLAP"
```

**Basic Starting Gear**:
```csharp
// These items don't generate actions:
// - "Leather Helmet", "Leather Armor", "Leather Boots"
// - "Cloth Hood", "Cloth Robes", "Cloth Shoes"
```

---

## Integration in CharacterEquipment

All managers work together seamlessly through the facade:

```csharp
var equipment = new CharacterEquipment();

// Use any manager's functionality through the facade
equipment.EquipItem(helmet, "head");              // SlotManager
equipment.GetEquipmentDamageBonus();              // BonusCalculator
equipment.GetModificationDamageMultiplier();      // ModificationCalculator
equipment.GetArmorSpikeDamage();                  // ArmorStatusManager
equipment.GetGearActions(helmet);                 // ActionProvider
```

---

## Common Patterns

### Calculate Total Effective Damage
```csharp
double CalculateDamage(CharacterEquipment equipment, double baseDamage)
{
    double bonus = equipment.GetModificationDamageBonus();
    double multiplier = equipment.GetModificationDamageMultiplier();
    
    return (baseDamage + bonus) * multiplier;
}
```

### Check for Special Equipment Effects
```csharp
void CheckEquipmentEffects(CharacterEquipment equipment)
{
    if (equipment.HasAutoSuccess())
        Console.WriteLine("Next action guaranteed!");
    
    if (equipment.GetArmorSpikeDamage() > 0)
        Console.WriteLine("Armor has spikes!");
    
    if (equipment.GetModificationLifesteal() > 0)
        Console.WriteLine("Gain health on hit!");
}
```

### Get All Available Actions
```csharp
void ShowAvailableActions(CharacterEquipment equipment)
{
    var actions = equipment.GetAllEquippedGearActions();
    Console.WriteLine($"Available actions ({actions.Count}):");
    foreach (var action in actions)
    {
        Console.WriteLine($"  - {action}");
    }
}
```

---

## Performance Considerations

1. **Slot Management**: O(1) for all operations
2. **Bonus Calculation**: O(n) where n = number of equipped items (max 4)
3. **Modification Bonuses**: O(m) where m = number of modifications
4. **Armor Status**: O(s) where s = number of armor statuses
5. **Action Retrieval**: O(a) where a = number of available actions

**Overall**: Very efficient, no performance concerns

---

## Error Handling

All managers handle edge cases gracefully:

- **Null items**: Safely skipped in calculations
- **Invalid slots**: Return null/false appropriately
- **Missing actions**: Return empty lists
- **No statuses**: Return 0 values

---

## Testing Checklist

### EquipmentSlotManager
- [ ] Equip and unequip items
- [ ] Get all equipped items
- [ ] Check slot status
- [ ] Handle invalid slot names

### EquipmentBonusCalculator
- [ ] Calculate single stat bonus
- [ ] Calculate multiple bonuses
- [ ] Calculate total armor
- [ ] Handle missing items

### ModificationBonusCalculator
- [ ] Calculate all modification types
- [ ] Handle multiplicative vs additive
- [ ] Filter by effect type
- [ ] Get all modifications

### ArmorStatusManager
- [ ] Calculate spike damage
- [ ] Get all statuses
- [ ] Filter by effect
- [ ] Check for specific effects

### EquipmentActionProvider
- [ ] Get weapon actions
- [ ] Get armor actions
- [ ] Detect special armor
- [ ] Handle fallbacks

---

## See Also

- [CharacterEquipment Refactoring Summary](../../EQUIPMENT_REFACTORING_SUMMARY.md)
- [ARCHITECTURE.md](../01-Core/ARCHITECTURE.md)
- [CODE_PATTERNS.md](./CODE_PATTERNS.md)

