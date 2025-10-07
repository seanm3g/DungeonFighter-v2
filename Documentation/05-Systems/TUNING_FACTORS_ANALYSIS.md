# DungeonFighter Tuning Factors Analysis

## 🎯 **CRITICAL DISCOVERY: EnemyScaling Not Applied**

**The `EnemyScaling` multipliers in `TuningConfig.json` are NOT being applied to enemy stats!**

### **Current Enemy Creation Flow:**
1. `EnemyLoader.CreateEnemy()` loads stats directly from `Enemies.json`
2. `Enemy` constructor uses stats as-is (no scaling applied)
3. `EnemyScaling` configuration is completely ignored

### **Why Global Scaling Changes Had No Effect:**
- `EnemyHealthMultiplier`: 1.2 → 0.5 (NOT APPLIED)
- `EnemyDamageMultiplier`: 0.8 → 0.25 (NOT APPLIED)  
- `EnemyBaseArmorAtLevel1`: 6 → 1 (NOT APPLIED)

---

## 📊 **TUNING FACTORS AND THEIR EFFECTS**

### **1. EnemyBalance Configuration**
**Location**: `TuningConfig.json` → `EnemyBalance` section
**Applied via**: `EnemyBalanceCalculator.CalculateStats()`

#### **AttributePool.BasePointsAtLevel1**
- **Increase**: More total attribute points → Higher enemy stats → Lower player win rate
- **Decrease**: Fewer total attribute points → Lower enemy stats → Higher player win rate

#### **SustainPool.BasePointsAtLevel1**
- **Increase**: More health/armor points → Higher enemy survivability → Lower player win rate
- **Decrease**: Fewer health/armor points → Lower enemy survivability → Higher player win rate

#### **StatConversionRates.StrengthPerPoint**
- **Increase**: More strength per point → Higher enemy damage → Lower player win rate
- **Decrease**: Less strength per point → Lower enemy damage → Higher player win rate

#### **StatConversionRates.AgilityPerPoint**
- **Increase**: More agility per point → Faster enemy attacks → Lower player win rate
- **Decrease**: Less agility per point → Slower enemy attacks → Higher player win rate

#### **StatConversionRates.TechniquePerPoint**
- **Increase**: More technique per point → Higher enemy combo amplification → Lower player win rate
- **Decrease**: Less technique per point → Lower enemy combo amplification → Higher player win rate

#### **StatConversionRates.IntelligencePerPoint**
- **Increase**: More intelligence per point → Higher enemy roll bonuses → Lower player win rate
- **Decrease**: Less intelligence per point → Lower enemy roll bonuses → Higher player win rate

#### **StatConversionRates.HealthPerPoint**
- **Increase**: More health per point → Higher enemy health → Lower player win rate
- **Decrease**: Less health per point → Lower enemy health → Higher player win rate

#### **StatConversionRates.ArmorPerPoint**
- **Increase**: More armor per point → Higher enemy armor → Lower player win rate
- **Decrease**: Less armor per point → Lower enemy armor → Higher player win rate

### **2. ArchetypeConfigs**
**Location**: `TuningConfig.json` → `ArchetypeConfigs` section
**Applied via**: `ArchetypeConfigHelper.GetArchetypeConfig()`

#### **SUSTAINHealthRatio**
- **Increase**: More health allocation → Higher enemy health → Lower player win rate
- **Decrease**: Less health allocation → Lower enemy health → Higher player win rate

#### **SUSTAINArmorRatio**
- **Increase**: More armor allocation → Higher enemy armor → Lower player win rate
- **Decrease**: Less armor allocation → Lower enemy armor → Higher player win rate

#### **StrengthRatio, AgilityRatio, TechniqueRatio, IntelligenceRatio**
- **Increase**: More attribute allocation → Higher enemy stats → Lower player win rate
- **Decrease**: Less attribute allocation → Lower enemy stats → Higher player win rate

### **3. Individual Enemy Stats**
**Location**: `GameData/Enemies.json`
**Applied via**: `EnemyLoader.CreateEnemy()`

#### **baseHealth**
- **Increase**: Higher enemy health → Lower player win rate
- **Decrease**: Lower enemy health → Higher player win rate

#### **baseStats.strength**
- **Increase**: Higher enemy damage → Lower player win rate
- **Decrease**: Lower enemy damage → Higher player win rate

#### **baseStats.agility**
- **Increase**: Faster enemy attacks → Lower player win rate
- **Decrease**: Slower enemy attacks → Higher player win rate

#### **baseStats.technique**
- **Increase**: Higher enemy combo amplification → Lower player win rate
- **Decrease**: Lower enemy combo amplification → Higher player win rate

#### **baseStats.intelligence**
- **Increase**: Higher enemy roll bonuses → Lower player win rate
- **Decrease**: Lower enemy roll bonuses → Higher player win rate

#### **baseArmor**
- **Increase**: Higher enemy armor → Lower player win rate
- **Decrease**: Lower enemy armor → Higher player win rate

### **4. Action Damage Multipliers**
**Location**: `GameData/Actions.json`
**Applied via**: `CombatCalculator.CalculateRawDamage()`

#### **damageMultiplier**
- **Increase**: Higher action damage → Lower player win rate
- **Decrease**: Lower action damage → Higher player win rate

---

## 🚫 **TUNING FACTORS THAT DON'T WORK**

### **EnemyScaling Configuration**
**Location**: `TuningConfig.json` → `EnemyScaling` section
**Status**: Configuration exists but is never used in enemy creation

#### **EnemyHealthMultiplier**
- **Effect**: NOT APPLIED - No impact on enemy health

#### **EnemyDamageMultiplier**
- **Effect**: NOT APPLIED - No impact on enemy damage

#### **EnemyBaseArmorAtLevel1**
- **Effect**: NOT APPLIED - No impact on enemy armor

#### **EnemyArmorPerLevel**
- **Effect**: NOT APPLIED - No impact on enemy armor scaling

---

## 🔧 **TECHNICAL IMPLEMENTATION**

### **EnemyScaling Fix**
**Problem**: `EnemyScaling` multipliers are not applied to enemy stats
**Solution**: Modify `EnemyLoader.CreateEnemy()` to apply scaling:

```csharp
// In EnemyLoader.cs, after loading enemy data:
var scaling = GameConfiguration.Instance.EnemyScaling;
if (scaling != null)
{
    data.BaseHealth = (int)(data.BaseHealth * scaling.EnemyHealthMultiplier);
    data.BaseStats.Strength = (int)(data.BaseStats.Strength * scaling.EnemyDamageMultiplier);
    data.BaseStats.Agility = (int)(data.BaseStats.Agility * scaling.EnemyDamageMultiplier);
    data.BaseStats.Technique = (int)(data.BaseStats.Technique * scaling.EnemyDamageMultiplier);
    data.BaseStats.Intelligence = (int)(data.BaseStats.Intelligence * scaling.EnemyDamageMultiplier);
    data.BaseArmor = (int)(data.BaseArmor * scaling.EnemyBaseArmorAtLevel1);
}
```

---

## 📈 **TUNING FACTOR PRIORITY**

1. **StatConversionRates** - Affects all enemies globally
2. **ArchetypeConfigs** - Affects enemy groups by archetype
3. **Individual Enemy Stats** - Affects specific enemies
4. **Action Damage Multipliers** - Affects all enemies using that action
5. **EnemyScaling** - Currently broken, needs fix
