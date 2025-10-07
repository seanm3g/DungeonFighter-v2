# Balance Tuning Strategy - Binary Search Approach

## 🎯 **BINARY SEARCH BALANCE TUNING**

### **Core Philosophy**
When balance is significantly off-target (<50% accuracy), make **MAJOR changes**. When it swings too far in the opposite direction, **halve the adjustment** and tune in the other direction. Continue until the metric stops improving, then move to the next tuning factor.

### **Target Metrics**
- **Primary Target**: 85-98% player win rate
- **Accuracy Threshold**: <50% accuracy triggers major changes
- **Overshoot Response**: Halve adjustment and reverse direction

---

## 📊 **TUNING FACTORS PRIORITY ORDER**

### **1. StatConversionRates (Global Impact)**
**Location**: `TuningConfig.json` → `EnemyBalance.StatConversionRates`
**Impact**: Affects ALL enemies globally
**Range**: 0.1 - 3.0 (typical range)

#### **Binary Search Strategy**:
- **Major Change**: ±50% of current value
- **Fine Tune**: ±25% of current value
- **Micro Adjust**: ±10% of current value

#### **Individual Rates**:
- `StrengthPerPoint`: Controls enemy damage output
- `AgilityPerPoint`: Controls enemy attack speed
- `TechniquePerPoint`: Controls enemy combo amplification
- `IntelligencePerPoint`: Controls enemy roll bonuses
- `HealthPerPoint`: Controls enemy survivability
- `ArmorPerPoint`: Controls enemy damage reduction

### **2. Attribute/Sustain Pools (Global Impact)**
**Location**: `TuningConfig.json` → `EnemyBalance.AttributePool` & `SustainPool`
**Impact**: Affects total points available to ALL enemies

#### **Binary Search Strategy**:
- **Major Change**: ±40% of current value
- **Fine Tune**: ±20% of current value
- **Micro Adjust**: ±10% of current value

#### **Pool Types**:
- `AttributePool.BasePointsAtLevel1`: Total attribute points
- `SustainPool.BasePointsAtLevel1`: Total health/armor points

### **3. ArchetypeConfigs (Group Impact)**
**Location**: `TuningConfig.json` → `EnemyBalance.ArchetypeConfigs`
**Impact**: Affects enemy groups by archetype

#### **Binary Search Strategy**:
- **Major Change**: ±30% of current ratio
- **Fine Tune**: ±15% of current ratio
- **Micro Adjust**: ±5% of current ratio

#### **Key Ratios**:
- `StrengthRatio`, `AgilityRatio`, `TechniqueRatio`, `IntelligenceRatio`
- `SUSTAINHealthRatio`, `SUSTAINArmorRatio`

### **4. Individual Enemy Stats (Specific Impact)**
**Location**: `TuningConfig.json` → `EnemyBalance.BaseEnemyConfigs`
**Impact**: Affects specific enemies only

#### **Binary Search Strategy**:
- **Major Change**: ±25% of current stat
- **Fine Tune**: ±12% of current stat
- **Micro Adjust**: ±5% of current stat

#### **Key Stats**:
- `BaseHealth`: Enemy survivability
- `BaseStats.Strength`: Enemy damage
- `BaseStats.Agility`: Enemy speed
- `BaseArmor`: Enemy damage reduction

### **5. Action Damage Multipliers (Action-Specific Impact)**
**Location**: `GameData/Actions.json`
**Impact**: Affects all enemies using specific actions

#### **Binary Search Strategy**:
- **Major Change**: ±40% of current multiplier
- **Fine Tune**: ±20% of current multiplier
- **Micro Adjust**: ±10% of current multiplier

---

## 🔄 **BINARY SEARCH ALGORITHM**

### **Step 1: Assess Current State**
1. Read latest balance analysis file
2. Calculate accuracy: `enemies_in_target_range / total_enemies`
3. Determine if <50% accuracy (trigger major changes)

### **Step 2: Identify Primary Issue**
1. **Too Easy (>98% win rate)**: Increase enemy effectiveness
2. **Too Hard (<85% win rate)**: Decrease enemy effectiveness
3. **Mixed Results**: Focus on largest outlier group

### **Step 3: Apply Binary Search**
**CRITICAL: Find the TWO BIG tunings that bracket the optimal value FIRST**

1. **Bound Finding Phase**:
   - **Upper Bound**: Find value where enemies are too hard (0% win rate)
   - **Lower Bound**: Find value where enemies are too easy (≥98% win rate)
   - **Use ±50% major changes** to establish both bounds
   - **NEVER fine-tune until both bounds are found**

2. **Fine-Tuning Phase** (only after bounds established):
   - **Apply ±25% adjustments** between established bounds
   - **Test**: Run balance analysis
   - **Evaluate**: Check if accuracy improved
   - **Overshoot Check**: If swung too far, halve adjustment and reverse
   - **Continue**: Repeat until metric stops improving

### **Step 4: Move to Next Factor**
1. When current factor stops improving accuracy
2. Move to next priority factor
3. Repeat binary search process

---

## 🚨 **CRITICAL LESSON LEARNED**

### **Binary Search Strategy Error**
**MISTAKE**: Moving to fine-tuning before finding the two BIG tunings that bracket the optimal value.

**CORRECT APPROACH**:
1. **Find Upper Bound**: Value where enemies are too hard (0% win rate)
2. **Find Lower Bound**: Value where enemies are too easy (≥98% win rate)
3. **Then Fine-Tune**: Between the established bounds

**Example of Error**:
- Started with StatConversionRates = 1.0
- Found 0% win rate (too hard) → reduced to 0.85 (fine-tuning)
- **WRONG**: Should have made BIG reduction to find lower bound first

**Correct Process**:
- Found 0% win rate (too hard) → make BIG reduction to 0.5 to find lower bound
- Once both bounds found (e.g., 0.5 = too easy, 1.125 = too hard)
- Then fine-tune between 0.5 and 1.125

---

## 📈 **ADJUSTMENT MAGNITUDES**

### **Major Changes (Accuracy <50%)**
- **StatConversionRates**: ±50%
- **Pools**: ±40%
- **Archetypes**: ±30%
- **Individual Stats**: ±25%
- **Action Multipliers**: ±40%

### **Fine Tuning (Accuracy 50-80%)**
- **StatConversionRates**: ±25%
- **Pools**: ±20%
- **Archetypes**: ±15%
- **Individual Stats**: ±12%
- **Action Multipliers**: ±20%

### **Micro Adjustments (Accuracy >80%)**
- **StatConversionRates**: ±10%
- **Pools**: ±10%
- **Archetypes**: ±5%
- **Individual Stats**: ±5%
- **Action Multipliers**: ±10%

---

## 🎯 **SUCCESS CRITERIA**

### **Target Achievement**
- **85-98% win rate** for ALL enemies
- **Accuracy >90%** (enemies in target range)
- **Weapon balance** across all weapon types

### **Stopping Conditions**
1. **Accuracy >95%**: Stop tuning
2. **No improvement** after 3 iterations on same factor
3. **Overshoot detected**: Halve and reverse direction
4. **All factors exhausted**: Manual review required

---

## 🔧 **IMPLEMENTATION NOTES**

### **File Tracking**
- Track changes in `Documentation/BALANCE_CHANGES_LOG.md`
- Record each adjustment with reasoning
- Maintain change history for rollback

### **Testing Protocol**
- Run balance analysis after each major change
- Compare results with previous iteration
- Document improvement or regression

### **Rollback Strategy**
- Keep previous configuration as backup
- Ability to revert to last known good state
- Incremental rollback if overshoot occurs

---

## 📋 **TUNING CHECKLIST**

### **Before Starting**
- [ ] Read latest balance analysis
- [ ] Calculate current accuracy
- [ ] Identify primary issue
- [ ] Select appropriate tuning factor

### **During Tuning**
- [ ] Apply binary search adjustment
- [ ] Run balance analysis
- [ ] Evaluate accuracy improvement
- [ ] Check for overshoot
- [ ] Document changes

### **After Tuning**
- [ ] Verify target achievement
- [ ] Check weapon balance
- [ ] Update change log
- [ ] Prepare for next iteration if needed

---

*This strategy ensures systematic, efficient balance tuning with clear stopping criteria and rollback capabilities.*
