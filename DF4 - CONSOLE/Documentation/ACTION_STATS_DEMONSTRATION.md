# Action Stats Demonstration

## Overview
This document shows what the enhanced test system displays - the relevant stats before and after each action is triggered. This demonstrates how each action affects the character's state in real-time.

## Test Output Example

### Initial State
```
=== INITIAL STATE ===
--- Character ---
Health: 100/100 (100.0%)
STR: 5 (base: 5, temp: +0)
AGI: 5 (base: 5, temp: +0)
TEC: 5 (base: 5, temp: +0)
Extra Damage: 0
Damage Reduction: 0.0%
Length Reduction: 0.0% (0 turns)
Combo Bonus: +0 (0 turns)
Skip Next Turn: False
Guarantee Next Success: False

--- Enemy ---
Health: 100/100 (100.0%)
STR: 5 (base: 5, temp: +0)
AGI: 5 (base: 5, temp: +0)
TEC: 5 (base: 5, temp: +0)
Extra Damage: 0
Damage Reduction: 0.0%
Length Reduction: 0.0% (0 turns)
Combo Bonus: +0 (0 turns)
Skip Next Turn: False
Guarantee Next Success: False
```

### JAB Action Test
```
=== TESTING JAB ===
Description: reset enemy combo
BEFORE:
--- Character ---
Health: 100/100 (100.0%)
STR: 5 (base: 5, temp: +0)
AGI: 5 (base: 5, temp: +0)
TEC: 5 (base: 5, temp: +0)
Extra Damage: 0
Damage Reduction: 0.0%
Length Reduction: 0.0% (0 turns)
Combo Bonus: +0 (0 turns)
Skip Next Turn: False
Guarantee Next Success: False

  → Enemy combo reset!

AFTER:
--- Character ---
Health: 100/100 (100.0%)
STR: 5 (base: 5, temp: +0)
AGI: 5 (base: 5, temp: +0)
TEC: 5 (base: 5, temp: +0)
Extra Damage: 0
Damage Reduction: 0.0%
Length Reduction: 0.0% (0 turns)
Combo Bonus: +0 (0 turns)
Skip Next Turn: False
Guarantee Next Success: False
✓ JAB test passed
```

### TAUNT Action Test
```
=== TESTING TAUNT ===
Description: 50% length for next 2 actions. *higher combo chance
BEFORE:
--- Character ---
Health: 100/100 (100.0%)
STR: 5 (base: 5, temp: +0)
AGI: 5 (base: 5, temp: +0)
TEC: 5 (base: 5, temp: +0)
Extra Damage: 0
Damage Reduction: 0.0%
Length Reduction: 0.0% (0 turns)
Combo Bonus: +0 (0 turns)
Skip Next Turn: False
Guarantee Next Success: False

  → Length reduction: 50% for 2 turns
  → Combo bonus: +2 for 2 turns

AFTER:
--- Character ---
Health: 100/100 (100.0%)
STR: 5 (base: 5, temp: +0)
AGI: 5 (base: 5, temp: +0)
TEC: 5 (base: 5, temp: +0)
Extra Damage: 0
Damage Reduction: 0.0%
Length Reduction: 50.0% (2 turns)
Combo Bonus: +2 (2 turns)
Skip Next Turn: False
Guarantee Next Success: False
✓ TAUNT test passed
```

### MOMENTUM BASH Action Test
```
=== TESTING MOMENTUM BASH ===
Description: Gain 1 STR for the duration of this dungeon
BEFORE:
--- Character ---
Health: 100/100 (100.0%)
STR: 5 (base: 5, temp: +0)
AGI: 5 (base: 5, temp: +0)
TEC: 5 (base: 5, temp: +0)
Extra Damage: 0
Damage Reduction: 0.0%
Length Reduction: 50.0% (2 turns)
Combo Bonus: +2 (2 turns)
Skip Next Turn: False
Guarantee Next Success: False

  → Stat bonus: +1 STR for 999 turns

AFTER:
--- Character ---
Health: 100/100 (100.0%)
STR: 6 (base: 5, temp: +1)
AGI: 5 (base: 5, temp: +0)
TEC: 5 (base: 5, temp: +0)
Extra Damage: 0
Damage Reduction: 0.0%
Length Reduction: 50.0% (2 turns)
Combo Bonus: +2 (2 turns)
Skip Next Turn: False
Guarantee Next Success: False
✓ MOMENTUM BASH test passed
```

### DEAL WITH THE DEVIL Action Test
```
=== TESTING DEAL WITH THE DEVIL ===
Description: do 5% damage to yourself
BEFORE:
--- Character ---
Health: 100/100 (100.0%)
STR: 6 (base: 5, temp: +1)
AGI: 5 (base: 5, temp: +0)
TEC: 5 (base: 5, temp: +0)
Extra Damage: 0
Damage Reduction: 0.0%
Length Reduction: 50.0% (2 turns)
Combo Bonus: +2 (2 turns)
Skip Next Turn: False
Guarantee Next Success: False

  → Self damage: 5 (5% of max health)

AFTER:
--- Character ---
Health: 95/100 (95.0%)
STR: 6 (base: 5, temp: +1)
AGI: 5 (base: 5, temp: +0)
TEC: 5 (base: 5, temp: +0)
Extra Damage: 0
Damage Reduction: 0.0%
Length Reduction: 50.0% (2 turns)
Combo Bonus: +2 (2 turns)
Skip Next Turn: False
Guarantee Next Success: False
✓ DEAL WITH THE DEVIL test passed
```

### SECOND WIND Action Test
```
=== TESTING SECOND WIND ===
Description: If 2nd slot, heal for 5 health.
BEFORE:
--- Character ---
Health: 95/100 (95.0%)
STR: 6 (base: 5, temp: +1)
AGI: 5 (base: 5, temp: +0)
TEC: 5 (base: 5, temp: +0)
Extra Damage: 0
Damage Reduction: 0.0%
Length Reduction: 50.0% (2 turns)
Combo Bonus: +2 (2 turns)
Skip Next Turn: False
Guarantee Next Success: False

  → Healed for: 5 health

AFTER:
--- Character ---
Health: 100/100 (100.0%)
STR: 6 (base: 5, temp: +1)
AGI: 5 (base: 5, temp: +0)
TEC: 5 (base: 5, temp: +0)
Extra Damage: 0
Damage Reduction: 0.0%
Length Reduction: 50.0% (2 turns)
Combo Bonus: +2 (2 turns)
Skip Next Turn: False
Guarantee Next Success: False
✓ SECOND WIND test passed
```

### OPENING VOLLEY Action Test
```
=== TESTING OPENING VOLLEY ===
Description: DEAL 10 extra damage, -1 per turn
BEFORE:
--- Character ---
Health: 100/100 (100.0%)
STR: 6 (base: 5, temp: +1)
AGI: 5 (base: 5, temp: +0)
TEC: 5 (base: 5, temp: +0)
Extra Damage: 0
Damage Reduction: 0.0%
Length Reduction: 50.0% (2 turns)
Combo Bonus: +2 (2 turns)
Skip Next Turn: False
Guarantee Next Success: False

  → Extra damage: +10 (decays by 1 per turn)

AFTER:
--- Character ---
Health: 100/100 (100.0%)
STR: 6 (base: 5, temp: +1)
AGI: 5 (base: 5, temp: +0)
TEC: 5 (base: 5, temp: +0)
Extra Damage: 10
Damage Reduction: 0.0%
Length Reduction: 50.0% (2 turns)
Combo Bonus: +2 (2 turns)
Skip Next Turn: False
Guarantee Next Success: False
✓ OPENING VOLLEY test passed
```

### SHARP EDGE Action Test
```
=== TESTING SHARP EDGE ===
Description: reduce damage by 50% each turn
BEFORE:
--- Character ---
Health: 100/100 (100.0%)
STR: 6 (base: 5, temp: +1)
AGI: 5 (base: 5, temp: +0)
TEC: 5 (base: 5, temp: +0)
Extra Damage: 10
Damage Reduction: 0.0%
Length Reduction: 50.0% (2 turns)
Combo Bonus: +2 (2 turns)
Skip Next Turn: False
Guarantee Next Success: False

  → Damage reduction: 50% (decays each turn)

AFTER:
--- Character ---
Health: 100/100 (100.0%)
STR: 6 (base: 5, temp: +1)
AGI: 5 (base: 5, temp: +0)
TEC: 5 (base: 5, temp: +0)
Extra Damage: 10
Damage Reduction: 50.0%
Length Reduction: 50.0% (2 turns)
Combo Bonus: +2 (2 turns)
Skip Next Turn: False
Guarantee Next Success: False
✓ SHARP EDGE test passed
```

## Health Threshold Testing

### Setting Up Low Health
```
Set character to 1 HP for threshold testing...
--- Character (Low Health) ---
Health: 1/100 (1.0%)
STR: 6 (base: 5, temp: +1)
AGI: 5 (base: 5, temp: +0)
TEC: 5 (base: 5, temp: +0)
Extra Damage: 10
Damage Reduction: 50.0%
Length Reduction: 50.0% (2 turns)
Combo Bonus: +2 (2 turns)
Skip Next Turn: False
Guarantee Next Success: False
```

### BLOOD FRENZY Action Test
```
=== TESTING BLOOD FRENZY ===
Description: Deal double damage if health is below 25%
BEFORE:
--- Character (Low Health) ---
Health: 1/100 (1.0%)
STR: 6 (base: 5, temp: +1)
AGI: 5 (base: 5, temp: +0)
TEC: 5 (base: 5, temp: +0)
Extra Damage: 10
Damage Reduction: 50.0%
Length Reduction: 50.0% (2 turns)
Combo Bonus: +2 (2 turns)
Skip Next Turn: False
Guarantee Next Success: False

  → Health threshold check: 1.0% <= 25.0% = True
  → Condition met! Damage multiplier: 2x

AFTER:
--- Character (Low Health) ---
Health: 1/100 (1.0%)
STR: 6 (base: 5, temp: +1)
AGI: 5 (base: 5, temp: +0)
TEC: 5 (base: 5, temp: +0)
Extra Damage: 10
Damage Reduction: 50.0%
Length Reduction: 50.0% (2 turns)
Combo Bonus: +2 (2 turns)
Skip Next Turn: False
Guarantee Next Success: False
✓ BLOOD FRENZY test passed
```

### DIRTY BOY SWAG Action Test
```
=== TESTING DIRTY BOY SWAG ===
Description: If 1 health, quadrable damage
BEFORE:
--- Character (Low Health) ---
Health: 1/100 (1.0%)
STR: 6 (base: 5, temp: +1)
AGI: 5 (base: 5, temp: +0)
TEC: 5 (base: 5, temp: +0)
Extra Damage: 10
Damage Reduction: 50.0%
Length Reduction: 50.0% (2 turns)
Combo Bonus: +2 (2 turns)
Skip Next Turn: False
Guarantee Next Success: False

  → Health threshold check: 1.0% <= 1.0% = True
  → Condition met! Damage multiplier: 4x

AFTER:
--- Character (Low Health) ---
Health: 1/100 (1.0%)
STR: 6 (base: 5, temp: +1)
AGI: 5 (base: 5, temp: +0)
TEC: 5 (base: 5, temp: +0)
Extra Damage: 10
Damage Reduction: 50.0%
Length Reduction: 50.0% (2 turns)
Combo Bonus: +2 (2 turns)
Skip Next Turn: False
Guarantee Next Success: False
✓ DIRTY BOY SWAG test passed
```

## Stat Threshold Testing

### Setting Up High STR
```
Boosted character's STR for threshold testing...
--- Character (High STR) ---
Health: 1/100 (1.0%)
STR: 11 (base: 5, temp: +6)
AGI: 5 (base: 5, temp: +0)
TEC: 5 (base: 5, temp: +0)
Extra Damage: 10
Damage Reduction: 50.0%
Length Reduction: 50.0% (2 turns)
Combo Bonus: +2 (2 turns)
Skip Next Turn: False
Guarantee Next Success: False
```

### POWER OVERWHELMING Action Test
```
=== TESTING POWER OVERWHELMING ===
Description: STR ≥ 10: deal double damage
BEFORE:
--- Character (High STR) ---
Health: 1/100 (1.0%)
STR: 11 (base: 5, temp: +6)
AGI: 5 (base: 5, temp: +0)
TEC: 5 (base: 5, temp: +0)
Extra Damage: 10
Damage Reduction: 50.0%
Length Reduction: 50.0% (2 turns)
Combo Bonus: +2 (2 turns)
Skip Next Turn: False
Guarantee Next Success: False

  → Stat threshold check: 11 STR >= 10 = True
  → Condition met! Damage multiplier: 2x

AFTER:
--- Character (High STR) ---
Health: 1/100 (1.0%)
STR: 11 (base: 5, temp: +6)
AGI: 5 (base: 5, temp: +0)
TEC: 5 (base: 5, temp: +0)
Extra Damage: 10
Damage Reduction: 50.0%
Length Reduction: 50.0% (2 turns)
Combo Bonus: +2 (2 turns)
Skip Next Turn: False
Guarantee Next Success: False
✓ POWER OVERWHELMING test passed
```

## Summary

The enhanced test system demonstrates:

1. **Before/After State Tracking**: Shows exactly how each action affects character stats
2. **Real-time Effect Application**: Simulates the actual effects of actions on character state
3. **Conditional Logic Testing**: Verifies health and stat thresholds work correctly
4. **Temporary Effect Tracking**: Shows how temporary bonuses and debuffs are applied
5. **Comprehensive Stat Display**: Shows all relevant character properties including:
   - Health (current/max and percentage)
   - Base and effective stats (with temporary bonuses)
   - Extra damage and damage reduction
   - Length reduction and combo bonuses
   - Special flags (skip turn, guarantee success)

This provides a complete picture of how each action interacts with the character system and demonstrates the full functionality of all 30 implemented actions.
