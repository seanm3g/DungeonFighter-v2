# Balance Changes Log - DungeonFighter

## 📊 **CHANGE TRACKING**

This log tracks all balance tuning changes with reasoning, results, and accuracy metrics.

---

## 🎯 **CURRENT TARGET**
- **Win Rate Range**: 85-98% for ALL enemies
- **Accuracy Target**: >90% (enemies in target range)
- **Weapon Balance**: All weapons viable

---

## 📈 **CHANGE HISTORY**

### **Session 1: Initial Analysis (2025-10-01 22:35:32)**
**File**: `balance_analysis_2025-10-01_22-35-32.txt`
**Results**:
- Average Win Rate: 9.2%
- Accuracy: 0/76 enemies (0%)
- Status: **CRITICAL - Too Hard**

**Changes Made**:
1. **StatConversionRates Reduction**:
   - StrengthPerPoint: 3.0 → 0.8 (-73%)
   - AgilityPerPoint: 2.0 → 0.6 (-70%)
   - TechniquePerPoint: 1.5 → 0.5 (-67%)
   - IntelligencePerPoint: 1.2 → 0.4 (-67%)
   - HealthPerPoint: 2.0 → 0.8 (-60%)
   - ArmorPerPoint: 0.12 → 0.05 (-58%)

2. **Pool Reduction**:
   - AttributePool: 12 → 8 (-33%)
   - SustainPool: 11 → 6 (-45%)

3. **Individual Enemy Adjustments**:
   - Goblin: Health 25→20, Strength 3→2, Technique 2→1
   - Skeleton: Health 12→15, Strength 1→2, Agility 1→2
   - Bear: Health 35→30, Strength 4→3
   - Stone Guardian: Health 80→50, Strength 8→4, Armor 6→3

**Reasoning**: Major reduction due to 0% accuracy - enemies were massively overpowered

---

### **Session 2: Overshoot Correction (2025-10-01 22:37:22)**
**File**: `balance_analysis_2025-10-01_22-37-22.txt`
**Results**:
- Average Win Rate: 97.4%
- Accuracy: 0/76 enemies (0%)
- Status: **CRITICAL - Too Easy (Overshoot)**

**Changes Made**:
1. **StatConversionRates Increase**:
   - StrengthPerPoint: 0.8 → 1.2 (+50%)
   - AgilityPerPoint: 0.6 → 1.0 (+67%)
   - TechniquePerPoint: 0.5 → 0.8 (+60%)
   - IntelligencePerPoint: 0.4 → 0.7 (+75%)
   - HealthPerPoint: 0.8 → 1.2 (+50%)
   - ArmorPerPoint: 0.05 → 0.08 (+60%)

2. **Pool Increase**:
   - AttributePool: 8 → 10 (+25%)
   - SustainPool: 6 → 8 (+33%)

3. **Specific Outlier Fixes**:
   - Wolf: Health 22→18, Strength 3→2, Agility 3→2, Technique 2→1
   - Zombie: Health 30→25, Strength 4→3

**Reasoning**: Overshoot detected - halved previous reduction and increased enemy strength

---

### **Session 3: User Adjustments (2025-10-01 22:38:43)**
**File**: `balance_analysis_2025-10-01_22-38-43.txt`
**Results**:
- Average Win Rate: 78.9%
- Accuracy: 0/76 enemies (0%)
- Status: **MIXED - Polarized Results**

**User Changes Made**:
1. **Pool Major Increase**:
   - AttributePool: 10 → 50 (+400%)
   - SustainPool: 8 → 60 (+650%)

2. **StatConversionRates Standardization**:
   - All rates set to 1.0 (except HealthPerPoint: 1.2, ArmorPerPoint: 0.08)

3. **Individual Enemy Boosts**:
   - Treant: Health 35→40, Strength 3→4, Armor 2→3
   - Lich: Health 30→35, Technique 2→3, Intelligence 4→6, Armor 1→2
   - Crystal Golem: Health 45→55, Strength 4→5, Armor 2→3

**AI Follow-up Changes**:
1. **Specific Outlier Fixes**:
   - Wolf: Health 18→20 (Sword 0% win rate)
   - Treant: Health 40→35, Strength 4→3, Armor 3→2 (Dagger 0% win rate)
   - Lich: Health 35→30, Technique 3→2, Intelligence 6→4, Armor 2→1 (Dagger 0% win rate)
   - Crystal Golem: Health 55→45, Strength 5→4, Armor 3→2 (Dagger 0% win rate)

**Reasoning**: User made major pool increases, AI focused on specific outliers

---

### **Session 4: Binary Search Major Adjustment (2025-10-01 22:39:XX)**
**File**: `balance_analysis_2025-10-01_22-38-43.txt` (latest)
**Results**:
- Average Win Rate: 78.9%
- Accuracy: 0/76 enemies (0%)
- Status: **CRITICAL - 60 enemies too easy, 16 too hard**

**Binary Search Analysis**:
- **Accuracy <50%**: Triggered major changes (±50% adjustments)
- **Primary Issue**: 60 enemies too easy (≥98% win rate)
- **Strategy**: Increase enemy effectiveness to make them stronger

**Major Changes Applied**:
1. **StatConversionRates Major Increase (+50%)**:
   - StrengthPerPoint: 1.0 → 1.5 (+50%)
   - AgilityPerPoint: 1.0 → 1.5 (+50%)
   - TechniquePerPoint: 1.0 → 1.5 (+50%)
   - IntelligencePerPoint: 1.0 → 1.5 (+50%)
   - HealthPerPoint: 1.2 → 1.8 (+50%)
   - ArmorPerPoint: 0.08 → 0.12 (+50%)

**Reasoning**: Binary search major adjustment due to 0% accuracy - 60 enemies too easy need to be made stronger

---

### **Session 5: Binary Search Overshoot Correction (2025-10-01 22:47:16)**
**File**: `balance_analysis_2025-10-01_22-47-16.txt`
**Results**:
- Average Win Rate: 0.0%
- Accuracy: 0/76 enemies (0%)
- Status: **CRITICAL OVERSHOOT - 76 enemies too hard**

**Overshoot Analysis**:
- **Previous**: 78.9% win rate (60 too easy, 16 too hard)
- **Current**: 0.0% win rate (76 enemies too hard)
- **Overshoot**: Massive swing to opposite extreme
- **Binary Search Response**: Halve adjustment and reverse direction

**Overshoot Correction Applied**:
1. **StatConversionRates Overshoot Correction (-25% from overshoot values)**:
   - StrengthPerPoint: 1.5 → 1.125 (-25% from overshoot)
   - AgilityPerPoint: 1.5 → 1.125 (-25% from overshoot)
   - TechniquePerPoint: 1.5 → 1.125 (-25% from overshoot)
   - IntelligencePerPoint: 1.5 → 1.125 (-25% from overshoot)
   - HealthPerPoint: 1.8 → 1.35 (-25% from overshoot)
   - ArmorPerPoint: 0.12 → 0.09 (-25% from overshoot)

**Reasoning**: Binary search overshoot response - halved the +50% adjustment and reversed direction to correct the massive overshoot

---

### **Session 6: Binary Search Fine-Tuning (2025-10-01 22:48:23)**
**File**: `balance_analysis_2025-10-01_22-48-23.txt`
**Results**:
- Average Win Rate: 0.0%
- Accuracy: 0/76 enemies (0%)
- Status: **STILL TOO HARD - No improvement from overshoot correction**

**Fine-Tuning Analysis**:
- **Previous**: 0.0% win rate (76 enemies too hard)
- **Current**: 0.0% win rate (76 enemies too hard)
- **Assessment**: Overshoot correction insufficient
- **Strategy**: Continue reducing StatConversionRates (fine-tuning phase)

**Fine-Tuning Adjustment Applied**:
1. **StatConversionRates Fine-Tuning (-25% from correction values)**:
   - StrengthPerPoint: 1.125 → 0.85 (-25% from correction)
   - AgilityPerPoint: 1.125 → 0.85 (-25% from correction)
   - TechniquePerPoint: 1.125 → 0.85 (-25% from correction)
   - IntelligencePerPoint: 1.125 → 0.85 (-25% from correction)
   - HealthPerPoint: 1.35 → 1.0 (-25% from correction)
   - ArmorPerPoint: 0.09 → 0.07 (-25% from correction)

**Reasoning**: Binary search fine-tuning - overshoot correction didn't improve situation, continue reducing StatConversionRates

---

### **Session 7: Binary Search Lower Bound Search (2025-10-01 22:49:XX)**
**File**: `balance_analysis_2025-10-01_22-48-23.txt` (latest)
**Results**:
- Average Win Rate: 0.0%
- Accuracy: 0/76 enemies (0%)
- Status: **CORRECTING BINARY SEARCH STRATEGY**

**Binary Search Strategy Correction**:
- **Error**: Moved to fine-tuning before finding the two BIG tunings that bracket optimal value
- **Correct Approach**: Find lower bound (too easy) and upper bound (too hard), then fine-tune between them
- **Current Status**: 
  - Upper bound found: StatConversionRates = 1.125 (0% win rate - too hard)
  - Lower bound needed: Find StatConversionRates value where enemies become too easy

**Lower Bound Search Applied**:
1. **StatConversionRates Major Reduction (-50% from fine-tuning values)**:
   - StrengthPerPoint: 0.85 → 0.5 (-50% to find lower bound)
   - AgilityPerPoint: 0.85 → 0.5 (-50% to find lower bound)
   - TechniquePerPoint: 0.85 → 0.5 (-50% to find lower bound)
   - IntelligencePerPoint: 0.85 → 0.5 (-50% to find lower bound)
   - HealthPerPoint: 1.0 → 0.6 (-50% to find lower bound)
   - ArmorPerPoint: 0.07 → 0.04 (-50% to find lower bound)

**Reasoning**: Corrected binary search strategy - making BIG reduction to find lower bound where enemies become too easy, then will fine-tune between bounds

---

### **Session 8: Binary Search Lower Bound Continuation (2025-10-01 22:50:54)**
**File**: `balance_analysis_2025-10-01_22-50-54.txt`
**Results**:
- Average Win Rate: 1.3%
- Accuracy: 0/76 enemies (0%)
- Status: **PROGRESS TOWARD LOWER BOUND - 1 enemy too easy, 75 too hard**

**Lower Bound Search Progress**:
- **Previous**: 0.0% win rate (76 enemies too hard)
- **Current**: 1.3% win rate (75 enemies too hard, 1 too easy)
- **Assessment**: Moving toward lower bound but not there yet
- **Strategy**: Continue reducing StatConversionRates to find lower bound

**Lower Bound Search Continued**:
1. **StatConversionRates Major Reduction (-50% from 0.5 values)**:
   - StrengthPerPoint: 0.5 → 0.25 (-50% to continue lower bound search)
   - AgilityPerPoint: 0.5 → 0.25 (-50% to continue lower bound search)
   - TechniquePerPoint: 0.5 → 0.25 (-50% to continue lower bound search)
   - IntelligencePerPoint: 0.5 → 0.25 (-50% to continue lower bound search)
   - HealthPerPoint: 0.6 → 0.3 (-50% to continue lower bound search)
   - ArmorPerPoint: 0.04 → 0.02 (-50% to continue lower bound search)

**Reasoning**: Continue lower bound search - still too hard (1.3% win rate), need to find where enemies become too easy (≥98% win rate)

---

### **Session 9: Binary Search Pool Adjustment (2025-10-01 22:51:XX)**
**File**: `balance_analysis_2025-10-01_22-50-54.txt` (latest)
**Results**:
- Average Win Rate: 1.3%
- Accuracy: 0/76 enemies (0%)
- Status: **CORRECTING BINARY SEARCH FOCUS**

**Binary Search Focus Correction**:
- **Error**: Adjusting StatConversionRates when should focus on pools at this level
- **Correct Approach**: Adjust SustainPool and AttributePool BasePointsAtLevel1
- **Current Pool Values**: AttributePool=50, SustainPool=60
- **Strategy**: Reduce pools to find lower bound where enemies become too easy

**Pool Adjustment Applied**:
1. **AttributePool Major Reduction (-50% from current values)**:
   - BasePointsAtLevel1: 50 → 25 (-50% to find lower bound)
2. **SustainPool Major Reduction (-50% from current values)**:
   - BasePointsAtLevel1: 60 → 30 (-50% to find lower bound)

**Reasoning**: Corrected binary search focus - adjusting pools instead of StatConversionRates to find lower bound where enemies become too easy

---

## 📊 **ACCURACY TRACKING**

| Session | Date | Accuracy | Win Rate | Status | Action Taken |
|---------|------|----------|----------|--------|--------------|
| 1 | 22:35:32 | 0% | 9.2% | Too Hard | Major Reduction (-60-73%) |
| 2 | 22:37:22 | 0% | 97.4% | Too Easy | Major Increase (+50-75%) |
| 3 | 22:38:43 | 0% | 78.9% | Mixed | Targeted Fixes |
| 4 | 22:39:XX | 0% | 0.0% | Overshoot | Binary Search Major (+50%) |
| 5 | 22:47:16 | 0% | 0.0% | Still Too Hard | Overshoot Correction (-25%) |
| 6 | 22:48:23 | 0% | 0.0% | Strategy Error | Fine-Tuning (-25%) |
| 7 | 22:49:XX | 0% | 0.0% | Lower Bound Search | Major Reduction (-50%) |
| 8 | 22:50:54 | 0% | 1.3% | Focus Error | Lower Bound Continued (-50%) |
| 9 | 22:51:XX | TBD | TBD | TBD | Pool Adjustment (-50%) |

---

## 🎯 **NEXT STEPS**

### **Current State Analysis**
- **Accuracy**: Still 0% (no enemies in 85-98% range)
- **Win Rate**: 78.9% (closer to target but still off)
- **Issue**: Polarized results (60 too easy, 16 too hard)

### **Recommended Next Actions**
1. **Binary Search on StatConversionRates**: Fine-tune the 1.0 values
2. **Address Weapon Imbalance**: Wand still problematic (37% win rate)
3. **Focus on ArchetypeConfigs**: May need group-level adjustments
4. **Individual Enemy Refinement**: Continue targeting outliers

### **Binary Search Strategy**
- **Current Factor**: StatConversionRates (1.0 values)
- **Next Adjustment**: ±25% (fine-tuning phase)
- **Direction**: Reduce slightly (78.9% still too high)
- **Target**: Move closer to 85-98% range

---

## 📋 **LESSONS LEARNED**

1. **Major Changes Work**: Large adjustments can quickly move balance
2. **Overshoot is Common**: Need halving strategy when swinging too far
3. **Individual Enemies Matter**: Specific outliers need targeted fixes
4. **Weapon Balance**: Different weapons have different balance points
5. **Pool Impact**: Attribute/Sustain pools have massive global impact

---

*This log will be updated with each balance tuning session to track progress and maintain change history.*
