# TuningConfig.json Usage Analysis & Recommendations

## 📊 Current Usage Status

### ✅ **FULLY USED** Sections
These sections are actively used throughout the codebase:

1. **GameData** - Used in `Program.cs` and `GameDataGenerator.cs`
2. **Debug** - Used via `TuningConfig.IsDebugEnabled` throughout codebase
3. **UI** - Used in `UIManager.cs` and `DungeonManager.cs` for all delay settings
4. **GameSpeed** - Used in `GameTicker.cs` for timing
5. **EnemyDPS** - Used in `EnemyDPSSystem.cs` and `GameDataGenerator.cs`
6. **RarityScaling** - Used in `LootGenerator.cs` for magic find and level-based scaling
7. **WeaponScaling** - Used in `GameInitializer.cs` for starting weapon damage
8. **Poison** - Used in `Entity.cs` for poison effect calculations

### ⚠️ **PARTIALLY USED** Sections
These sections have some values used but others are unused:

1. **Character** - `PlayerBaseHealth` and `HealthPerLevel` used, but `EnemyHealthPerLevel` is used
2. **Attributes** - All values are used in `CharacterStats.cs` and `Enemy.cs`
3. **Combat** - `BaseAttackTime` used, but other values may be hardcoded elsewhere
4. **Progression** - All values used in `Enemy.cs` and `DungeonManager.cs`

### ❌ **UNUSED** Sections
These sections exist in JSON but are not referenced in code:

1. **Equipment** - `BonusDamagePerTier` and `BonusAttackSpeedRange` not used
2. **XPRewards** - Complex XP calculation system not implemented
3. **Loot** - `LootChanceBase`, `LootChancePerLevel`, `MaximumLootChance`, `MagicFindLootChanceMultiplier` not used
4. **RollSystem** - `MissThreshold`, `BasicAttackThreshold`, `ComboThreshold`, `CriticalThreshold` not used
5. **ComboSystem** - `ComboAmplifierAtTech5`, `ComboAmplifierMax`, `ComboAmplifierMaxTech` not used
6. **EnemyScaling** - `EnemyHealthMultiplier`, `EnemyDamageMultiplier`, `EnemyLevelVariance` not used
7. **ItemScaling** - Complex weapon/armor scaling formulas not implemented
8. **ProgressionCurves** - Experience and attribute growth formulas not implemented

## 🔧 **HARDCODED VALUES** That Should Be Configurable

### **Combat System**
```csharp
// In CombatCalculator.cs and other files
- Critical hit calculations (hardcoded 20 threshold)
- Damage calculation formulas
- Attack speed calculations
- Armor reduction formulas
```

### **Character Progression**
```csharp
// In CharacterProgression.cs
- Experience requirements (hardcoded formulas)
- Level-up stat gains
- Skill point allocation
```

### **Loot System**
```csharp
// In LootGenerator.cs and related files
- Drop chance calculations
- Item tier distributions
- Rarity roll thresholds
- Magic find effectiveness
```

### **Dungeon Generation**
```csharp
// In DungeonManager.cs and Environment.cs
- Room count scaling (hardcoded 0.5)
- Enemy spawn rates
- Reward calculations
- Difficulty scaling
```

### **UI and Timing**
```csharp
// Scattered throughout codebase
- Text display delays
- Animation speeds
- Menu navigation timing
```

## 🎯 **RECOMMENDED ADDITIONS** to TuningConfig.json

### **1. Combat Balance**
```json
{
  "CombatBalance": {
    "ArmorReductionFormula": "Damage * (1 - Armor / (Armor + 100))",
    "CriticalHitChance": 0.05,
    "CriticalHitDamageMultiplier": 1.5,
    "BlockChance": 0.1,
    "DodgeChance": 0.05,
    "ParryChance": 0.03
  }
}
```

### **2. Experience & Progression**
```json
{
  "ExperienceSystem": {
    "BaseXPFormula": "BaseXP * (Level^1.5)",
    "LevelCap": 100,
    "StatPointsPerLevel": 2,
    "SkillPointsPerLevel": 1,
    "AttributeCap": 100
  }
}
```

### **3. Loot & Economy**
```json
{
  "LootSystem": {
    "BaseDropChance": 0.3,
    "DropChancePerLevel": 0.02,
    "MaxDropChance": 0.8,
    "MagicFindEffectiveness": 0.01,
    "GoldDropMultiplier": 1.0,
    "ItemValueMultiplier": 1.0
  }
}
```

### **4. Dungeon Scaling**
```json
{
  "DungeonScaling": {
    "RoomCountBase": 3,
    "RoomCountPerLevel": 0.5,
    "EnemyCountPerRoom": 2,
    "BossRoomChance": 0.1,
    "TrapRoomChance": 0.2,
    "TreasureRoomChance": 0.15
  }
}
```

### **5. Class Balance**
```json
{
  "ClassBalance": {
    "Barbarian": {
      "HealthMultiplier": 1.2,
      "DamageMultiplier": 1.1,
      "SpeedMultiplier": 0.9
    },
    "Warrior": {
      "HealthMultiplier": 1.1,
      "DamageMultiplier": 1.0,
      "SpeedMultiplier": 1.0
    },
    "Rogue": {
      "HealthMultiplier": 0.9,
      "DamageMultiplier": 1.2,
      "SpeedMultiplier": 1.1
    },
    "Wizard": {
      "HealthMultiplier": 0.8,
      "DamageMultiplier": 1.3,
      "SpeedMultiplier": 1.0
    }
  }
}
```

### **6. Status Effects**
```json
{
  "StatusEffects": {
    "Bleed": {
      "DamagePerTick": 2,
      "TickInterval": 3.0,
      "MaxStacks": 5
    },
    "Burn": {
      "DamagePerTick": 3,
      "TickInterval": 2.0,
      "MaxStacks": 3
    },
    "Freeze": {
      "SpeedReduction": 0.5,
      "Duration": 5.0
    },
    "Stun": {
      "SkipTurns": 1,
      "Duration": 2.0
    }
  }
}
```

### **7. Equipment Scaling**
```json
{
  "EquipmentScaling": {
    "WeaponDamagePerTier": 2,
    "ArmorValuePerTier": 1,
    "SpeedBonusPerTier": 0.1,
    "MaxTier": 5,
    "EnchantmentChance": 0.1
  }
}
```

### **8. Difficulty Settings**
```json
{
  "DifficultySettings": {
    "Easy": {
      "EnemyHealthMultiplier": 0.8,
      "EnemyDamageMultiplier": 0.8,
      "XPMultiplier": 1.2,
      "LootMultiplier": 1.1
    },
    "Normal": {
      "EnemyHealthMultiplier": 1.0,
      "EnemyDamageMultiplier": 1.0,
      "XPMultiplier": 1.0,
      "LootMultiplier": 1.0
    },
    "Hard": {
      "EnemyHealthMultiplier": 1.3,
      "EnemyDamageMultiplier": 1.2,
      "XPMultiplier": 1.5,
      "LootMultiplier": 1.3
    }
  }
}
```

## 🚀 **IMPLEMENTATION PRIORITIES**

### **High Priority** (Immediate Impact)
1. **Combat Balance** - Critical for game feel
2. **Loot System** - Affects player progression
3. **Experience System** - Core progression mechanic

### **Medium Priority** (Quality of Life)
1. **Dungeon Scaling** - Improves replayability
2. **Status Effects** - Adds depth to combat
3. **Equipment Scaling** - Balances item progression

### **Low Priority** (Nice to Have)
1. **Class Balance** - If class system is expanded
2. **Difficulty Settings** - For accessibility options

## 📝 **NEXT STEPS**

1. **Audit Current Usage** - Remove unused sections or implement them
2. **Implement High Priority** - Add combat, loot, and experience systems
3. **Update Code References** - Replace hardcoded values with config lookups
4. **Test Balance** - Validate that configurable values work as expected
5. **Documentation** - Update architecture docs with new configurable systems

This analysis shows that while TuningConfig.json has a comprehensive structure, many sections are not yet implemented in the codebase. The recommendations focus on making the most impactful systems configurable while maintaining the existing architecture.
