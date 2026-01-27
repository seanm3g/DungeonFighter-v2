# ACTION and ATTACK Keyword Mechanics Guide

## Overview

This document describes the **ACTION** and **ATTACK** keyword system for status effects in combat. These keywords provide a method of application and an integer duration that applies bonuses to future actions or attacks in the combat sequence.

---

## Core Concepts

### ACTION Keyword

**Definition**: The ACTION keyword applies bonuses to the **next ACTION in the sequence**. The bonus is only consumed when that action is **successfully triggered**.

**Key Characteristics**:
- Bonus applies to the next action in the action sequence
- Only consumed when the target action is successfully executed
- If Action 1 triggers, Action 2 gets the bonus, but only when Action 2 successfully triggers
- Acts as a "honeypot" or "jackpot" - you can continuously stack bonuses on the next action without having achieved it yet
- More powerful than ATTACK because it guarantees the bonus applies to the next action in sequence

**Example**: "For the Next ACTION: +5 STR"
- Action 1 triggers → Action 2 now has +5 STR bonus queued
- Action 2 successfully triggers → +5 STR is applied and consumed
- Action 2 misses → +5 STR is NOT consumed, remains queued for the next action

### ATTACK Keyword

**Definition**: The ATTACK keyword applies bonuses to the **next subsequent rolls**. The bonus is consumed regardless of whether the roll hits or misses.

**Key Characteristics**:
- Bonus applies to the next roll attempt
- Consumed on the next roll, regardless of success or failure
- If you roll 2 successful combo actions in a row, ATTACK and ACTION work the same way
- If the next roll after an ATTACK action misses, the bonus is still consumed
- Less powerful than ACTION because it's consumed even on misses

**Example**: "For the Next ATTACK: +3 HIT"
- Action 1 triggers → Next roll gets +3 HIT bonus
- Next roll (Action 2) occurs → +3 HIT is applied to the roll
- If Action 2 hits → Bonus was used
- If Action 2 misses → Bonus was still consumed (wasted)

---

## Numbered Versions

Both keywords support numbered versions to apply bonuses to multiple future actions/attacks.

### Syntax

- `"For the Next ACTION: +5 STR"` → Applies to 1 action (default)
- `"For the Next 3 ACTIONS: +5 STR"` → Applies to 3 actions
- `"For the Next ATTACK: +3 HIT"` → Applies to 1 attack (default)
- `"For the Next 5 ATTACKS: +3 HIT"` → Applies to 5 attacks

### Duration Tracking

- Each bonus type maintains a counter for remaining applications
- When an ACTION bonus is consumed (action successfully triggers), decrement the counter
- When an ATTACK bonus is consumed (roll occurs), decrement the counter
- When counter reaches 0, the bonus is removed

---

## Complex Scenario Example

**Setup**: ACTION 1 grants "For the next 3 ATTACKS: +3 HIT"

### Scenario Flow

1. **ACTION 1 is successful** → +3 HIT bonus is queued for the next 3 attacks
   - Remaining: 3 attacks

2. **Next roll (Action 2) occurs**:
   - **If Action 2 HITS**: 
     - +3 HIT bonus is applied to the roll
     - Remaining: 2 attacks
     - Action 2 executes successfully
   
   - **If Action 2 MISSES**:
     - +3 HIT bonus is still applied to the roll (but wasn't enough to hit)
     - Remaining: 2 attacks (bonus was consumed)
     - Action 2 fails, sequence returns to Action 1
     - **However**, Action 1 now has the +3 HIT bonus from the remaining stack
     - Next attempt at Action 1 will have +3 HIT bonus

3. **Subsequent rolls** continue consuming the remaining attack bonuses until the counter reaches 0

### Key Insight

When an ATTACK bonus misses, it's consumed but the sequence may loop back. The remaining bonuses in the stack still apply to future rolls, including when looping back to previous actions.

---

## Bonus Types

### Roll-Based Bonuses

These bonuses affect the dice roll and combat outcome determination:

#### +x ACCURACY
- **Effect**: Bonus to the basic roll (affects miss, hit, combo determination)
- **Application**: Added to the base d20 roll before outcome calculation
- **Example**: "+5 ACCURACY" means roll 1d20+5 instead of 1d20
- **Impact**: Increases chance of hit, combo, and critical hit

#### +x HIT
- **Effect**: Increases the chance a basic attack occurs
- **Application**: Effectively increases the range for basic attack, reducing miss chance
- **Mechanic**: Lowers the hit threshold (makes it easier to hit)
- **Example**: If hit threshold is 6, "+3 HIT" effectively makes threshold 3 (need 4+ to hit)
- **Impact**: Primarily reduces miss chance, making hits more likely

#### +x COMBO
- **Effect**: Increases the chance a combo action occurs on the next roll
- **Application**: Lowers the combo threshold
- **Mechanic**: Reduces the roll value needed to trigger combo
- **Example**: If combo threshold is 14, "+3 COMBO" makes threshold 11 (need 11+ for combo)
- **Impact**: Makes combo actions more likely to trigger

#### +x CRIT
- **Effect**: Increases or decreases your chance of a critical strike
- **Application**: Modifies the critical hit threshold
- **Mechanic**: Positive values lower threshold (easier crits), negative values raise it (harder crits)
- **Example**: If crit threshold is 20, "+2 CRIT" makes threshold 18 (need 18+ for crit)
- **Impact**: Changes critical hit frequency

#### +x CRIT MISS
- **Effect**: Increases or decreases your chance of a critical miss
- **Application**: Modifies the critical miss threshold
- **Mechanic**: Positive values raise threshold (more crit misses), negative values lower it (fewer crit misses)
- **Example**: If crit miss threshold is 1, "+2 CRIT MISS" makes threshold 3 (1-3 are crit misses)
- **Impact**: Changes critical miss frequency

### Stat Bonuses

These bonuses modify character statistics:

#### +x STR (Strength)
- **Effect**: Increases physical damage and strength-based calculations
- **Duration**: Can be temporary (N turns) or permanent (-1)

#### +x INT (Intelligence)
- **Effect**: Increases magical damage and intelligence-based calculations
- **Duration**: Can be temporary (N turns) or permanent (-1)

#### +x TEC (Technique)
- **Effect**: Increases precision, accuracy, and technique-based calculations
- **Duration**: Can be temporary (N turns) or permanent (-1)

#### +x AGI (Agility)
- **Effect**: Increases speed, dodge chance, and agility-based calculations
- **Duration**: Can be temporary (N turns) or permanent (-1)

---

## ACTION vs ATTACK Comparison

| Aspect | ACTION | ATTACK |
|--------|--------|--------|
| **Consumption** | Only on successful action execution | On next roll (hit or miss) |
| **Power Level** | Higher (guaranteed application) | Lower (can be wasted on misses) |
| **Use Case** | Guarantee bonuses to next action in sequence | Apply bonuses to next roll attempt |
| **Best For** | Building up powerful next actions | Immediate roll improvements |
| **Risk** | Lower (only consumed on success) | Higher (consumed even on miss) |

### When They Work the Same

If you roll 2 successful combo actions in a row:
- ACTION bonus: Applied to Action 2, consumed when Action 2 succeeds
- ATTACK bonus: Applied to Action 2's roll, consumed when Action 2's roll occurs
- **Result**: Both are consumed and applied in the same scenario

### When They Differ

**Scenario**: Action 1 grants bonus, Action 2 misses

- **ACTION bonus**: NOT consumed, remains queued for next action
- **ATTACK bonus**: IS consumed, wasted on the miss

---

## Implementation Notes

### Status Effect Structure

ACTION and ATTACK keywords function as status effects with:
- **Method of Application**: ACTION (sequence-based) or ATTACK (roll-based)
- **Integer Duration**: Number of applications remaining
- **Bonus Type**: ACCURACY, HIT, COMBO, CRIT, CRIT MISS, or STAT (STR/INT/TEC/AGI)
- **Bonus Value**: The numeric amount of the bonus

### Tracking Requirements

The system must track:
1. **ACTION bonuses**: Queue of bonuses waiting for next action in sequence
2. **ATTACK bonuses**: Queue of bonuses for next roll attempts
3. **Remaining applications**: Counter for each bonus type
4. **Bonus stacking**: Multiple bonuses of same type can stack
5. **Consumption timing**: When bonuses are applied and removed

### Consumption Logic

**ACTION bonuses**:
- Applied when the target action successfully executes
- Consumed after application
- Not consumed if action fails or is skipped

**ATTACK bonuses**:
- Applied to the next roll calculation
- Consumed when the roll occurs (regardless of outcome)
- Always consumed, even on misses

### Sequence Tracking

The system must track:
- Current action in sequence (for ACTION keyword)
- Roll attempts (for ATTACK keyword)
- Success/failure state of actions
- Loop-back scenarios (when sequence returns to previous actions)

---

## Example Action Descriptions

### Example 1: Simple ACTION Bonus
```
Action: "POWER STRIKE"
Effect: "For the Next ACTION: +5 STR"
Description: A powerful strike that strengthens your next action.
```

**Behavior**:
- When POWER STRIKE succeeds, next action gets +5 STR
- If next action succeeds, +5 STR is applied and consumed
- If next action misses, +5 STR remains queued

### Example 2: Multiple ATTACK Bonuses
```
Action: "PRECISION FOCUS"
Effect: "For the Next 3 ATTACKS: +2 HIT"
Description: Focus your aim for the next three attacks.
```

**Behavior**:
- When PRECISION FOCUS succeeds, next 3 rolls get +2 HIT
- Each roll consumes one application
- Even misses consume an application
- After 3 rolls, bonus is gone

### Example 3: Mixed Bonuses
```
Action: "BATTLE RAGE"
Effect: "For the Next 2 ACTIONS: +3 ACCURACY, +2 STR"
Description: Enter a rage that enhances your next two actions.
```

**Behavior**:
- When BATTLE RAGE succeeds, next 2 actions get both bonuses
- Both bonuses apply together
- Only consumed when actions succeed
- Can stack with other bonuses

### Example 4: Complex Scenario
```
Action: "COMBO SETUP"
Effect: "For the Next ACTION: +5 COMBO, For the Next 2 ATTACKS: +3 HIT"
Description: Set up for a powerful combo with improved accuracy.
```

**Behavior**:
- Next action gets +5 COMBO (ACTION bonus)
- Next 2 rolls get +3 HIT (ATTACK bonuses)
- ACTION bonus only consumed on successful action
- ATTACK bonuses consumed on next 2 rolls (hit or miss)
- Can create interesting timing scenarios

---

## Edge Cases and Special Scenarios

### Edge Case 1: Action Sequence Loop-Back

**Scenario**: Action 1 → Action 2 (misses) → Back to Action 1

- **ACTION bonus from Action 1**: Still queued for Action 2 (not consumed on miss)
- **ATTACK bonus from Action 1**: Already consumed on Action 2's roll
- **Result**: When Action 1 is attempted again, it may have different bonuses

### Edge Case 2: Multiple Bonuses Stacking

**Scenario**: Action 1 grants "+3 HIT", Action 2 grants "+2 HIT" (both ATTACK type)

- Both bonuses apply to the next roll
- Total bonus: +5 HIT
- Both are consumed on the next roll

### Edge Case 3: Bonus Expiration During Sequence

**Scenario**: "For the Next 3 ACTIONS: +5 STR" but combat ends after 2 actions

- Remaining bonus applications are lost
- No carry-over to next combat
- Each combat starts fresh

### Edge Case 4: Action Skipped or Interrupted

**Scenario**: Action has ACTION bonus queued, but action is skipped (stun, etc.)

- ACTION bonus is NOT consumed
- Remains queued for the actual next action
- ATTACK bonuses would have been consumed on the roll attempt

---

## Integration with Existing Systems

### Combo System

- ACTION bonuses can enhance combo sequences
- ATTACK bonuses can help trigger combo thresholds
- Both work alongside existing combo mechanics

### Status Effects

- ACTION/ATTACK bonuses are a type of status effect
- Can stack with other status effects
- Follow similar duration and tracking patterns

### Roll Calculation

- Bonuses apply during roll calculation phase
- ACCURACY, HIT, COMBO affect thresholds
- CRIT and CRIT MISS modify critical thresholds
- STAT bonuses affect damage and other calculations

---

## Summary

The ACTION and ATTACK keyword system provides flexible, powerful status effect mechanics:

- **ACTION**: Sequence-based, consumed only on success, more powerful
- **ATTACK**: Roll-based, consumed on roll attempt, more immediate
- **Numbered versions**: Support multiple applications
- **Multiple bonus types**: ACCURACY, HIT, COMBO, CRIT, CRIT MISS, and STAT bonuses
- **Complex interactions**: Support interesting tactical scenarios

This system enables rich action design where players can build up powerful next actions (ACTION) or improve immediate roll chances (ATTACK), creating strategic depth in combat decision-making.
