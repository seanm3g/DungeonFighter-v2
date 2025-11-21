# Combat Text Format Reference

This document provides a visual reference for how combat text should appear in the game. Use this as a guide when implementing or modifying combat message formatting.

## Format Structure

Combat messages consist of two lines:
1. **Main line**: The action description
2. **Detail line**: Technical breakdown in parentheses (indented with 4 spaces)

## Basic Attack Format

### Standard Basic Attack
```
Quinn Stoneheart hits Skeleton for 4 damage
    (roll: 13 | attack 7 - 3 armor | speed: 8.5s)
```

### Basic Attack with Roll Bonus
```
Nolan Swiftarrow hits Goblin for 6 damage
    (roll: 12 + 3 = 15 | attack 8 - 2 armor | speed: 7.2s)
```

### Basic Attack with Roll Penalty
```
Zarek Firebrand hits Wraith for 3 damage
    (roll: 15 - 2 = 13 | attack 5 - 2 armor | speed: 9.1s)
```

## Combo Action Format

### Standard Combo Action
```
Lorin hits Umbra with POWER STRIKE for 8 damage
    (roll: 16 | attack 10 - 2 armor | speed: 6.5s | amp: 1.0x)
```

### Combo Action with Amplification
```
Xan Ironheart hits Rock Elemental with WHIRLWIND for 12 damage
    (roll: 18 | attack 15 - 3 armor | speed: 5.8s | amp: 1.5x)
```

### Critical Combo Action
```
Pax Stormrider hits Bat with CRITICAL LIGHTNING BOLT for 20 damage
    (roll: 20 | attack 22 - 2 armor | speed: 4.2s | amp: 2.0x)
```

## Critical Hit Format

### Critical Basic Attack
```
Quinn Stoneheart hits Skeleton with CRITICAL BASIC ATTACK for 10 damage
    (roll: 20 | attack 13 - 3 armor | speed: 8.5s)
```

### Critical with Roll Bonus
```
Nolan Swiftarrow hits Goblin with CRITICAL BASIC ATTACK for 12 damage
    (roll: 18 + 2 = 20 | attack 14 - 2 armor | speed: 7.2s)
```

## Status Effect Format

### Poison Status Effect
```
Cael Darkwood affected by POISON for 1 turns
```

### Multiple Status Effects
```
Cael Darkwood affected by POISON for 1 turns
Steam Golem affected by POISON for 1 turns
```

### Status Effect Rules
- Status effect messages appear on their own line
- **No leading spaces** - start at column 0
- Format: `[Target Name] affected by [EFFECT NAME] for [X] turns`
- Effect name appears in ALL CAPS (e.g., POISON, BLEED, STUN)
- Each affected target gets its own line

## Detail Line Components

The detail line contains the following components, separated by ` | ` (space-pipe-space):

### Roll Information
- **No modifiers**: `roll: 13`
- **With bonus**: `roll: 12 + 3 = 15`
- **With penalty**: `roll: 15 - 2 = 13`

### Attack vs Armor
- Format: `attack X - Y armor`
- X = raw damage before armor
- Y = target's armor value
- Example: `attack 7 - 3 armor`

### Speed Information
- Format: `speed: N.Ns`
- Shows action speed in seconds with 1 decimal place
- Example: `speed: 8.5s`

### Amplification (Combo Actions Only)
- Format: `amp: M.Mx`
- Only shown for combo actions
- Shows 1.0x for first combo action, higher values for amplified combos
- Example: `amp: 1.5x` or `amp: 2.0x`

## Complete Examples

### Example 1: Player Basic Attack
```
Quinn Stoneheart hits Skeleton for 4 damage
    (roll: 13 | attack 7 - 3 armor | speed: 8.5s)
```

### Example 2: Enemy Basic Attack
```
Skeleton hits Quinn Stoneheart for 3 damage
    (roll: 9 | attack 5 - 2 armor | speed: 8.6s)
```

### Example 3: Player Combo Action
```
Lorin hits Umbra with ARCANE SHIELD for 6 damage
    (roll: 14 | attack 8 - 2 armor | speed: 6.3s | amp: 1.0x)
```

### Example 4: Amplified Combo
```
Xan Ironheart hits Rock Elemental with SPINNING STRIKE for 15 damage
    (roll: 17 | attack 18 - 3 armor | speed: 5.5s | amp: 1.8x)
```

### Example 5: Critical Combo
```
Pax Stormrider hits Bat with CRITICAL LIGHTNING BOLT for 25 damage
    (roll: 20 | attack 27 - 2 armor | speed: 4.2s | amp: 2.0x)
```

## Formatting Rules

1. **Main line**: No indentation, ends with "damage"
2. **Detail line**: 4 spaces indentation, wrapped in parentheses
3. **Status effects**: On their own line with **no leading spaces** (starts at column 0)
4. **Separators**: Use ` | ` (space-pipe-space) between detail components
5. **Spacing**: Single space around all operators (`+`, `-`, `=`)
6. **Decimals**: Speed and amplification show 1 decimal place
7. **Critical prefix**: "CRITICAL" appears before action name in all caps
8. **Action names**: Combo actions appear in ALL CAPS
9. **Effect names**: Status effects appear in ALL CAPS (e.g., POISON, BLEED, STUN)

## Notes

- The detail line is always on a new line with 4-space indentation
- Status effect messages are on their own line with **no leading spaces**
- All components in the detail line are separated by ` | ` (space-pipe-space)
- Roll bonus/penalty only shows the `= total` part if there are modifiers
- Amplification is only shown for combo actions
- Speed is only shown if the action has a defined length
- Each target affected by a status effect gets its own separate line

