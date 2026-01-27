# Spreadsheet Action Analysis

## Overview

This document analyzes the Google Sheets spreadsheet structure and maps it to the ACTION/ATTACK keyword mechanics system. The spreadsheet contains action definitions with extensive columns for various combat mechanics.

## Spreadsheet Structure

### Key Columns for ACTION/ATTACK Keywords

Based on the spreadsheet data, the following columns are critical for the keyword system:

| Column | Header | Description | Notes |
|--------|--------|-------------|-------|
| A | ACTION | Action name | Primary identifier |
| B | DESCRIPTION | Action description | Flavor text |
| J | DURATION | Duration/Count | Number of applications (for numbered keywords) |
| K | CADENCE | Keyword Type | Contains "ACTION", "ATTACK", "ATTACKS", "ACTIONS", "FIGHT", "DUNGEON", "CHAIN", "COMBO" |

### Bonus Type Columns

The spreadsheet contains columns for various bonus types that can be applied via ACTION/ATTACK keywords:

#### Roll-Based Bonuses
- **Column M**: FINISHER (appears unused in current data)
- **Column N**: ACCURACY (Hero accuracy bonus)
- **Column O**: HIT (Hero hit bonus)
- **Column P**: COMBO (Hero combo bonus)
- **Column Q**: CRIT (Hero crit bonus)
- **Column R**: ACCURACY (Enemy accuracy - appears unused)
- **Column S**: HIT (Enemy hit - appears unused)
- **Column T**: COMBO (Enemy combo - appears unused)
- **Column U**: CRIT (Enemy crit - appears unused)

#### Stat Bonuses
- **Column V**: STR (Hero Strength)
- **Column W**: AGI (Hero Agility)
- **Column X**: TECH (Hero Technique)
- **Column Y**: INT (Hero Intelligence)
- **Column Z**: STR (Enemy Strength - appears unused)
- **Column AA**: AGI (Enemy Agility - appears unused)
- **Column AB**: TECH (Enemy Technique - appears unused)
- **Column AC**: INT (Enemy Intelligence - appears unused)

## Keyword Usage Patterns in Spreadsheet

### ACTION Keyword Examples

| Action Name | CADENCE | DURATION | Bonus Type | Value | Notes |
|-------------|---------|----------|------------|-------|-------|
| CONCENTRATE | ATTACK | 1 | (appears to be HIT/ACCURACY) | 1 | Single attack bonus |
| FLURRY | ACTION | 1 | (multiple hits) | 3 | Next action gets 3 extra attacks |
| MOMENTUM BASH | DUNGEON | 1 | STR | 1 | Permanent STR bonus |
| BULK DAY | ACTION | 1 | (damage mod) | 0.2, 150% | Speed and damage mods |
| GYM PUMP | ACTION | 1 | (damage mod) | 50% | Damage modifier |
| QUICK REFLEXES | ACTION | 1 | (speed mod) | -5 | Speed modification |
| FOLLOW THROUGH | ACTION | 1 | (damage mod) | 25% | Damage modifier |
| LUCKY PUNCH | ACTION | 1 | COMBO | -14, 500% | Combo threshold and damage |
| SLAM | ACTION | 2 | (damage mod) | 200% | Damage modifier for 2 actions |
| OPENING CUT | ACTION | 1 | (multiple bonuses) | 2, 25% | Multiple bonuses |
| MEASURED PATIENCE | ACTION | 1 | COMBO | 2 | Combo bonus |
| MEASURED ADVANCE | ACTION | 1 | (bonus) | 1 | Single bonus |
| STATIC ODDS | ACTION | 1 | (bonus) | 1 | Single bonus |
| PERFECT LINE | ACTION | 1 | (bonus) | 4 | Multiple bonuses (4x) |
| CONTROLLED BURST | ACTION | 1 | COMBO | 1 | Combo bonus |
| CALM BEFORE STORM | ACTION | 1 | (damage mods) | 20%, 50% | Damage modifiers |
| CHANNEL | ACTION | 1 | (bonus) | 1 | Single bonus |
| CALCULATED RISK | ACTION | 1 | (bonuses) | -2, 2 | Multiple bonuses |
| AMPLIFY ACCURACY | ACTION | 1 | ACCURACY | 20 | Accuracy bonus |

### ATTACK Keyword Examples

| Action Name | CADENCE | DURATION | Bonus Type | Value | Notes |
|-------------|---------|----------|------------|-------|-------|
| CHEAP SHOT | ATTACK | 3 | (appears to be HIT) | -1 | 3 attacks with -1 HIT |
| GRUNT | ATTACKS | 2 | (appears to be HIT) | 1 | 2 attacks with +1 HIT |
| FUMBLE | ATTACKS | 3 | (appears to be HIT) | -3 | 3 attacks with -3 HIT |
| ONE SHOT | ATTACKS | 3 | (damage mod) | -50% | 3 attacks with damage penalty |
| DRUNKEN BRAWLER | ATTACKS | 1 | STR, AGI | -5, -5 | Stat penalties |
| ANIME POWERUP | ATTACKS | 1 | STR, AGI, TECH | 1, 2, 3 | Multiple stat bonuses |
| CHUG ENERGY DRINK | ATTACKS | 3 | (damage mod) | 25% | Damage modifier |
| TRUE STRIKE | ATTACKS | 1 | ACCURACY | 10, 200% | Accuracy and damage |
| AVENGE | ATTACK | 1 | ACCURACY | 20 | Accuracy bonus |
| OPENING VOLLEY | ATTACKS | 4 | ACCURACY | 20, -5 | Multiple bonuses |
| WEINING LIGHT | ATTACK | 1 | ACCURACY | 20, -1 | Accuracy and damage |
| LUCKY CHARM | ATTACK | 1 | ACCURACY | 14 | Accuracy bonus |
| BRUTAL FLOOR | ATTACK | 1 | ACCURACY | 5 | Accuracy bonus |

### Other Duration Types

The spreadsheet also uses other duration types:
- **FIGHT**: Lasts for the entire fight
- **DUNGEON**: Lasts for the entire dungeon
- **CHAIN**: Related to combo chain mechanics
- **COMBO**: Related to combo system

## Mapping Spreadsheet to Keyword Syntax

### Current Spreadsheet Format

The spreadsheet uses a columnar format where:
- **CADENCE** column contains the keyword type (ACTION, ATTACK, ATTACKS, etc.)
- **DURATION** column contains the count (1, 2, 3, etc.)
- Various bonus columns contain the bonus values

### Target Keyword Syntax

Based on the ACTION/ATTACK keyword mechanics guide, actions should be described as:
- `"For the Next ACTION: +X BONUSTYPE"`
- `"For the Next N ACTIONS: +X BONUSTYPE"`
- `"For the Next ATTACK: +X BONUSTYPE"`
- `"For the Next N ATTACKS: +X BONUSTYPE"`

### Conversion Examples

#### Example 1: CONCENTRATE
- **Spreadsheet**: CADENCE="ATTACK", DURATION=1, (appears to grant +1 to hit/accuracy)
- **Keyword Syntax**: `"For the Next ATTACK: +1 HIT"` or `"For the Next ATTACK: +1 ACCURACY"`

#### Example 2: FLURRY
- **Spreadsheet**: CADENCE="ACTION", DURATION=1, # OF HITS=5, DAMAGE=30%
- **Keyword Syntax**: `"For the Next ACTION: +4 Extra Hits (30% damage each)"`
- **Note**: This is a special case - grants extra attacks, not a simple bonus

#### Example 3: GRUNT
- **Spreadsheet**: CADENCE="ATTACKS", DURATION=2, (appears to grant +1 HIT)
- **Keyword Syntax**: `"For the Next 2 ATTACKS: +1 HIT"`

#### Example 4: MOMENTUM BASH
- **Spreadsheet**: CADENCE="DUNGEON", DURATION=1, STR=1
- **Keyword Syntax**: `"For the Next ACTION: +1 STR (DUNGEON duration)"`
- **Note**: DUNGEON duration means permanent for the dungeon

#### Example 5: AMPLIFY ACCURACY
- **Spreadsheet**: CADENCE="ACTION", DURATION=1, ACCURACY=20
- **Keyword Syntax**: `"For the Next ACTION: +20 ACCURACY"`

## Complex Actions Analysis

### Actions with Multiple Bonuses

Several actions grant multiple bonuses simultaneously:

#### BULK DAY
- CADENCE: ACTION
- DURATION: 1
- SPEED MOD: 0.2 (20% speed)
- DAMAGE MOD: 150%
- **Keyword Syntax**: `"For the Next ACTION: +20% Speed, +50% Damage"`

#### ANIME POWERUP
- CADENCE: ATTACKS
- DURATION: 1
- STR: 1, AGI: 2, TECH: 3
- **Keyword Syntax**: `"For the Next ATTACK: +1 STR, +2 AGI, +3 TECH"`

#### OPENING VOLLEY
- CADENCE: ATTACKS
- DURATION: 4
- ACCURACY: 20
- DAMAGE MOD: -5 (weapon damage)
- **Keyword Syntax**: `"For the Next 4 ATTACKS: +20 ACCURACY, -5 Weapon Damage"`

### Actions with Conditional Logic

Some actions have conditional triggers or thresholds:

#### BERZERK
- CADENCE: (none specified, appears to be conditional)
- TARGET: SELF
- THRESHOLD CATEGORY: HEALTH
- THRESHOLD AMOUNT: <25%
- BONUS: 5 STR
- **Keyword Syntax**: `"When Health <25%: +5 STR (SELF)"`

#### ROGUES GAMBIT
- TARGET: SELF
- THRESHOLD CATEGORY: HEALTH
- THRESHOLD AMOUNT: <25%
- BONUS: 2x AGILITY
- **Keyword Syntax**: `"When Health <25%: +2x AGI (SELF)"`

## Implementation Notes

### Column Mapping for Parser

To convert spreadsheet data to action definitions, map:

1. **Keyword Type**: Read from CADENCE column (K)
   - "ACTION" → ACTION keyword
   - "ATTACK" → ATTACK keyword (singular)
   - "ATTACKS" → ATTACK keyword (plural, use DURATION for count)
   - "ACTIONS" → ACTION keyword (plural, use DURATION for count)

2. **Duration/Count**: Read from DURATION column (J)
   - If CADENCE is "ATTACK" or "ACTION" (singular), default to 1
   - If CADENCE is "ATTACKS" or "ACTIONS" (plural), use DURATION value

3. **Bonus Values**: Read from appropriate bonus columns
   - ACCURACY: Column N (Hero) or R (Enemy)
   - HIT: Column O (Hero) or S (Enemy)
   - COMBO: Column P (Hero) or T (Enemy)
   - CRIT: Column Q (Hero) or U (Enemy)
   - STR: Column V (Hero) or Z (Enemy)
   - AGI: Column W (Hero) or AA (Enemy)
   - TECH: Column X (Hero) or AB (Enemy)
   - INT: Column Y (Hero) or AC (Enemy)

4. **Special Modifiers**: Read from modifier columns
   - SPEED MOD: Column AD
   - DAMAGE MOD: Column AE
   - MULTIHIT MOD: Column AF
   - AMP_MOD: Column AG

### Data Validation Rules

1. **CADENCE Values**: Must be one of: ACTION, ACTIONS, ATTACK, ATTACKS, FIGHT, DUNGEON, CHAIN, COMBO, or empty
2. **DURATION Values**: Must be positive integer when CADENCE is plural (ATTACKS/ACTIONS)
3. **Bonus Values**: Can be positive or negative integers/floats
4. **Multiple Bonuses**: Actions can have multiple bonus types simultaneously

## Next Steps

1. **Create Parser**: Build a parser to convert spreadsheet rows to action JSON definitions
2. **Validate Syntax**: Ensure all actions follow the ACTION/ATTACK keyword syntax
3. **Handle Edge Cases**: Process special cases like FLURRY (extra hits), conditional bonuses, etc.
4. **Generate Action Definitions**: Create JSON action definitions compatible with the game's action system
5. **Test Integration**: Verify actions work correctly with the combat system

## Questions for Clarification

1. **FLURRY**: Grants 5 extra hits - should this be "For the Next ACTION: +5 Extra Hits" or a different mechanic?
2. **DURATION Types**: How should FIGHT, DUNGEON, CHAIN, COMBO durations work with ACTION/ATTACK keywords?
3. **Multiple Bonuses**: Should multiple bonuses be combined into one keyword string or separate entries?
4. **Conditional Bonuses**: How should threshold-based bonuses (like BERZERK) integrate with ACTION/ATTACK keywords?
5. **Empty CADENCE**: Some actions have no CADENCE value - are these immediate effects or should they use a default?
