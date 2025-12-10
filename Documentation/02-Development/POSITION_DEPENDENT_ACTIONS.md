# Position-Dependent Actions Design Document

## Overview
This document outlines action concepts where the position in the combo sequence significantly affects how the action works. These actions enable different build strategies - some work best as openers (front of sequence), others as finishers (back of sequence), and some scale with position.

## Current System Support
- ✅ `TriggerOnlyInSlot` - Action only works in a specific slot
- ✅ `IfComboPosition` - Conditional triggers based on combo position
- ✅ Combo routing system tracks position via `ComboStep` and slot index

## Action Categories

### 1. OPENER ACTIONS (Strongest in Slot 1)

#### **MOMENTUM BUILDER**
- **Concept**: Sets up the rest of the combo
- **Front Position (Slot 1)**: Grants +2 damage per action in combo sequence (stacks)
- **Other Positions**: Only grants +1 damage per action
- **Build Strategy**: Place at front to maximize damage scaling for entire combo
- **Example JSON**:
```json
{
  "name": "MOMENTUM BUILDER",
  "type": "Attack",
  "damageMultiplier": 0.7,
  "length": 1.0,
  "description": "A quick strike that builds momentum. Grants +2 damage per combo action when used first, +1 otherwise.",
  "tags": ["weapon", "sword"],
  "comboRouting": {
    "triggerOnlyInSlot": 0
  },
  "advanced": {
    "statBonus": 0,
    "extraDamage": 2
  }
}
```

#### **MARK TARGET**
- **Concept**: Marks enemy for bonus damage
- **Front Position**: Marks enemy, all subsequent actions deal +30% damage to marked target
- **Other Positions**: Marks enemy, but bonus is only +15%
- **Build Strategy**: Front-loaded setup action
- **Example JSON**:
```json
{
  "name": "MARK TARGET",
  "type": "Attack",
  "damageMultiplier": 0.5,
  "length": 0.9,
  "description": "Marks the enemy for increased damage. When used first, all following attacks deal +30% damage to marked targets.",
  "tags": ["weapon", "dagger"],
  "causesMark": true,
  "advanced": {
    "extraDamage": 0
  }
}
```

#### **BATTLE CRY**
- **Concept**: Buffs all following actions
- **Front Position**: Grants +3 STR/AGI/INT to all following actions in combo (duration: combo length)
- **Other Positions**: Grants +1 stat bonus
- **Build Strategy**: Front-loaded buff action
- **Example JSON**:
```json
{
  "name": "BATTLE CRY",
  "type": "Buff",
  "damageMultiplier": 0.0,
  "length": 0.8,
  "description": "A rallying cry that empowers your combo. When used first, grants +3 STR to all following actions.",
  "tags": ["generic"],
  "advanced": {
    "statBonus": 3,
    "statBonusType": "STR",
    "statBonusDuration": 999
  }
}
```

#### **OPENING STRIKE**
- **Concept**: Stronger when used first
- **Front Position**: 1.8x damage multiplier
- **Other Positions**: 1.0x damage multiplier
- **Build Strategy**: High-damage opener
- **Example JSON**:
```json
{
  "name": "OPENING STRIKE",
  "type": "Attack",
  "damageMultiplier": 1.8,
  "length": 1.2,
  "description": "A powerful opening attack. Deals 1.8x damage when used first in combo, 1.0x otherwise.",
  "tags": ["weapon", "sword"],
  "triggers": {
    "triggerConditions": ["IfComboPosition"],
    "comboPosition": 0
  },
  "advanced": {
    "conditionalDamageMultiplier": 1.8
  }
}
```

---

### 2. FINISHER ACTIONS (Strongest in Last Slot)

#### **FINISHING BLOW**
- **Concept**: Scales with combo length
- **Last Position**: Deals base damage + (combo length × 0.3x multiplier)
- **Other Positions**: Standard damage
- **Build Strategy**: Place at end of long combos for maximum damage
- **Example JSON**:
```json
{
  "name": "FINISHING BLOW",
  "type": "Attack",
  "damageMultiplier": 1.2,
  "length": 1.5,
  "description": "A devastating finisher. Deals bonus damage based on combo length when used last.",
  "tags": ["weapon", "mace"],
  "advanced": {
    "comboAmplifierMultiplier": 1.3
  }
}
```

#### **COMBO ENDER**
- **Concept**: Consumes combo bonuses for burst damage
- **Last Position**: Deals damage equal to (base × 1.5) + (number of combo actions × 0.5x)
- **Other Positions**: Standard damage, doesn't consume bonuses
- **Build Strategy**: Build long combos, finish with this
- **Example JSON**:
```json
{
  "name": "COMBO ENDER",
  "type": "Attack",
  "damageMultiplier": 1.5,
  "length": 1.6,
  "description": "Consumes all combo momentum for a massive final strike. Strongest when used last.",
  "tags": ["weapon", "sword"],
  "advanced": {
    "comboAmplifierMultiplier": 2.0
  }
}
```

#### **EXECUTION STRIKE**
- **Concept**: Finisher for low-health enemies
- **Last Position**: Deals 2.5x damage if enemy below 30% health
- **Other Positions**: Deals 1.5x damage if enemy below 30% health
- **Build Strategy**: Use at end to finish off weakened enemies
- **Example JSON**:
```json
{
  "name": "EXECUTION STRIKE",
  "type": "Attack",
  "damageMultiplier": 1.0,
  "length": 1.3,
  "description": "A precise strike to finish weakened foes. Deals 2.5x damage to enemies below 30% health when used last.",
  "tags": ["weapon", "dagger"],
  "advanced": {
    "healthThreshold": 0.3,
    "conditionalDamageMultiplier": 2.5
  }
}
```

---

### 3. POSITION-SCALING ACTIONS (Scales with Position)

#### **BUILDING POWER**
- **Concept**: Gets stronger as combo progresses
- **Position 1**: 0.8x damage
- **Position 2**: 1.0x damage
- **Position 3**: 1.2x damage
- **Position 4+**: 1.4x damage
- **Build Strategy**: Works anywhere, but better later in combo
- **Example JSON**:
```json
{
  "name": "BUILDING POWER",
  "type": "Attack",
  "damageMultiplier": 1.0,
  "length": 1.1,
  "description": "Gains power as the combo builds. Damage increases with position: 0.8x (1st), 1.0x (2nd), 1.2x (3rd), 1.4x (4th+).",
  "tags": ["weapon", "sword"],
  "advanced": {
    "comboAmplifierMultiplier": 1.2
  }
}
```

#### **REVERSE POWER**
- **Concept**: Stronger early, weaker later
- **Position 1**: 1.5x damage
- **Position 2**: 1.2x damage
- **Position 3**: 1.0x damage
- **Position 4+**: 0.8x damage
- **Build Strategy**: Use early in combo or as opener
- **Example JSON**:
```json
{
  "name": "REVERSE POWER",
  "type": "Attack",
  "damageMultiplier": 1.5,
  "length": 1.0,
  "description": "Most effective when used early. Damage decreases with position: 1.5x (1st), 1.2x (2nd), 1.0x (3rd), 0.8x (4th+).",
  "tags": ["weapon", "mace"],
  "advanced": {
    "comboAmplifierMultiplier": 0.9
  }
}
```

#### **MIDDLE GROUND**
- **Concept**: Strongest in middle positions
- **Position 1**: 0.9x damage
- **Position 2**: 1.3x damage
- **Position 3**: 1.3x damage
- **Position 4+**: 1.0x damage
- **Build Strategy**: Place in middle slots (2-3) for optimal damage
- **Example JSON**:
```json
{
  "name": "MIDDLE GROUND",
  "type": "Attack",
  "damageMultiplier": 1.3,
  "length": 1.2,
  "description": "Peaks in the middle of combos. Deals 1.3x damage in positions 2-3, 0.9x first, 1.0x later.",
  "tags": ["weapon", "sword"],
  "advanced": {
    "comboAmplifierMultiplier": 1.0
  }
}
```

---

### 4. DUAL-PURPOSE ACTIONS (Different Roles Based on Position)

#### **ADAPTIVE STRIKE**
- **Concept**: Changes behavior based on position
- **Front Position (1-2)**: Grants +2 AGI for 3 turns (setup)
- **Back Position (Last)**: Deals 1.5x damage (finisher)
- **Middle Position**: Standard attack
- **Build Strategy**: Flexible - works as opener or finisher
- **Example JSON**:
```json
{
  "name": "ADAPTIVE STRIKE",
  "type": "Attack",
  "damageMultiplier": 1.0,
  "length": 1.1,
  "description": "Adapts to position. Grants +2 AGI when used early, deals 1.5x damage when used last.",
  "tags": ["weapon", "sword"],
  "triggers": {
    "triggerConditions": ["IfComboPosition"]
  },
  "advanced": {
    "statBonus": 2,
    "statBonusType": "AGI",
    "statBonusDuration": 3,
    "conditionalDamageMultiplier": 1.5
  }
}
```

#### **SETUP OR FINISH**
- **Concept**: Two different effects based on position
- **Front Position**: Applies "Vulnerability" debuff (enemy takes +25% damage)
- **Back Position**: Deals 2.0x damage to vulnerable enemies
- **Build Strategy**: Use both - setup early, finish late
- **Example JSON**:
```json
{
  "name": "SETUP OR FINISH",
  "type": "Attack",
  "damageMultiplier": 0.7,
  "length": 1.0,
  "description": "Applies vulnerability when used first, deals 2x damage to vulnerable enemies when used last.",
  "tags": ["weapon", "dagger"],
  "causesVulnerability": true,
  "advanced": {
    "conditionalDamageMultiplier": 2.0
  }
}
```

---

## Implementation Notes

### Required System Enhancements
1. **Position-Based Damage Scaling**: Need to calculate position in combo and apply multipliers
2. **Combo Length Tracking**: Track total combo length for finisher calculations
3. **Position-Aware Conditionals**: Enhance conditional system to check if action is first/last/middle

### Suggested Properties to Add
- `positionBasedDamageMultiplier`: Object with multipliers for different positions
- `isOpenerAction`: Boolean flag for opener-specific behavior
- `isFinisherAction`: Boolean flag for finisher-specific behavior
- `comboLengthBonus`: Bonus damage per action in combo (for finishers)

### Example Position Calculation Logic
```csharp
int GetComboPosition(Character character, List<Action> comboSequence)
{
    if (comboSequence.Count == 0) return 0;
    int currentSlotIndex = character.ComboStep % comboSequence.Count;
    return currentSlotIndex + 1; // 1-indexed for clarity
}

bool IsFirstInCombo(Character character, List<Action> comboSequence)
{
    return GetComboPosition(character, comboSequence) == 1;
}

bool IsLastInCombo(Character character, List<Action> comboSequence)
{
    int position = GetComboPosition(character, comboSequence);
    return position == comboSequence.Count;
}
```

---

## Build Examples

### **Opener Build**
1. MOMENTUM BUILDER (sets up scaling)
2. MARK TARGET (marks for bonus damage)
3. Standard attacks
4. Standard attacks

### **Finisher Build**
1. Standard attacks
2. Standard attacks
3. Standard attacks
4. FINISHING BLOW (scales with combo length)

### **Hybrid Build**
1. BATTLE CRY (buffs all following)
2. BUILDING POWER (scales with position)
3. BUILDING POWER (even stronger)
4. COMBO ENDER (consumes all bonuses)

---

## Balance Considerations

- **Opener Actions**: Should provide value that scales with combo length, but not be overpowered standalone
- **Finisher Actions**: Should reward long combos, but not make short combos useless
- **Position Scaling**: Should create interesting choices without making one position always optimal
- **Dual-Purpose**: Should be viable in multiple positions, but excel in specific ones

---

## Future Enhancements

1. **Position-Based Status Effects**: Effects that change based on position
2. **Combo Length Requirements**: Actions that only work in combos of certain lengths
3. **Position Synergies**: Actions that work better when combined with specific positions
4. **Dynamic Position Effects**: Effects that change based on relative position (e.g., "stronger if used after a spell")

