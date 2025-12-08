# Action Mechanics Documentation

This document outlines all mechanics available in the Actions.json file for DungeonFighter-v2.

## Table of Contents
1. [Action Types](#action-types)
2. [Core Properties](#core-properties)
3. [Status Effects](#status-effects)
4. [Combat Mechanics](#combat-mechanics)
5. [Combo System](#combo-system)
6. [Scaling Mechanics](#scaling-mechanics)
7. [Special Mechanics](#special-mechanics)
8. [Action Categories](#action-categories)

---

## Action Types

Actions can be one of the following types:

- **Attack**: Physical or magical damage-dealing action
- **Spell**: Magical attack (typically uses Intelligence)
- **Heal**: Restores health to the caster or target
- **Buff**: Provides beneficial effects to the caster
- **Debuff**: Applies negative effects to the target

---

## Core Properties

### Basic Properties

| Property | Type | Description | Example |
|----------|------|-------------|---------|
| `name` | string | Unique identifier for the action | "SWORD SLASH" |
| `type` | ActionType | Type of action (Attack, Spell, Heal, Buff, Debuff) | "Attack" |
| `damageMultiplier` | double | Multiplier for base damage (1.0 = 100%) | 1.0, 1.2, 1.5 |
| `length` | double | Action speed/duration (lower = faster) | 1.0, 1.1, 0.8 |
| `description` | string | Flavor text describing the action | "Fast horizontal slash" |

### Combo Properties

| Property | Type | Description | Example |
|----------|------|-------------|---------|
| `comboOrder` | int | Order in combo sequence (0 = not in combo) | 0, 1, 2, 3 |
| `isComboAction` | bool | Whether this action can be part of a combo | true, false |
| `comboBonusAmount` | int | Bonus amount for combo system | 2 |
| `comboBonusDuration` | int | Turns the combo bonus lasts | 2 |

---

## Status Effects

Actions can apply various status effects to targets:

### Bleeding (`causesBleed`)
- **Effect**: Target takes damage over time
- **Example**: POISON BLADE, SHADOW STRIKE, CRYSTAL SHARDS

### Poison (`causesPoison`)
- **Effect**: Target takes poison damage over time
- **Example**: POISON BLADE, ENEMY POISON ATTACK

### Weaken (`causesWeaken`)
- **Effect**: Reduces target's effectiveness
- **Example**: HEADBUTT, CURSE OF THE CRYPT, ENVIRONMENTAL WEAKEN

### Stun (`causesStun`)
- **Effect**: Prevents target from acting
- **Example**: WARRIOR TIER 2, DUNGEON COLLAPSE, TIME STOP

### Slow (`causesSlow`)
- **Effect**: Reduces target's action speed
- **Example**: ENEMY SLOW ATTACK

### Burn (`causesBurn`)
- **Effect**: Target takes fire damage over time
- **Example**: ENEMY BURN ATTACK

---

## Combat Mechanics

### Damage Modifiers

| Property | Type | Description | Example |
|----------|------|-------------|---------|
| `damageMultiplier` | double | Base damage multiplier | 1.0 (normal), 1.5 (150%) |
| `multiHitCount` | int | Number of hits in multi-hit attack | 3 |
| `multiHitDamagePercent` | double | Damage per hit (as percentage) | 0.35 (35% per hit) |

**Multi-Hit Example**: CLEAVE hits 3 times for 35% damage each = 105% total damage

### Roll Modifiers

| Property | Type | Description | Example |
|----------|------|-------------|---------|
| `rollBonus` | int | Bonus added to attack roll | +2, +3, +5 |
| `enemyRollPenalty` | int | Penalty applied to enemy's next roll | -2, -3 |

**Roll System**: Actions roll d20 + rollBonus. Higher rolls = better hit chance and damage.

### Healing

| Property | Type | Description | Example |
|----------|------|-------------|---------|
| `healAmount` | int | Flat health restored | 5, 10, 15 |
| `lifesteal` | double | Percentage of damage dealt as healing | 0.25 (25%) |

**Examples**:
- SECOND WIND: Heals 5 health
- VAMPIRE STRIKE: Heals 25% of damage dealt
- BARBARIAN TIER 3: Heals 50% of damage dealt

### Self-Damage

| Property | Type | Description | Example |
|----------|------|-------------|---------|
| `selfDamagePercent` | int | Percentage of max health lost | 5, 10 |

**Examples**:
- DEAL WITH THE DEVIL: Deals 5% self-damage
- BERSERKER RAGE: Deals 10% self-damage

### Damage Reduction

| Property | Type | Description | Example |
|----------|------|-------------|---------|
| `damageReduction` | double | Percentage of incoming damage reduced | 0.25 (25%), 0.4 (40%) |

**Examples**:
- BRACE: Reduces incoming damage by 25%
- WARRIOR TIER 2: Reduces incoming damage by 20%

### Armor Modifiers

| Property | Type | Description | Example |
|----------|------|-------------|---------|
| `armorBonus` | int | Flat armor bonus granted | 2 |
| `armorBonusDuration` | int | Turns the armor bonus lasts | 3 |

**Example**: WARRIOR TIER 3 grants +2 armor for 3 turns

---

## Combo System

### Combo Order
Actions with `comboOrder > 0` can be chained in sequence:
- Order 1: First action in combo
- Order 2: Second action in combo
- Order 3+: Subsequent actions

### Combo Reset
- `resetEnemyCombo`: Resets the enemy's combo counter
- **Example**: JAB resets enemy combo

### Combo Scaling Tags
Actions can scale based on combo state:

| Tag | Description | Example |
|-----|-------------|---------|
| `comboScaling` | Scales with total combo length | SHADOW STRIKE, METEOR |
| `comboStepScaling` | Scales with position in combo | HEROIC STRIKE |
| `comboAmplificationScaling` | Scales with combo amplification | BERSERKER RAGE |

---

## Scaling Mechanics

### Stat Bonuses

| Property | Type | Description | Example |
|----------|------|-------------|---------|
| `statBonus` | int | Amount of stat bonus | 1 |
| `statBonusType` | string | Type of stat (STR, AGI, TEC, INT) | "STR" |
| `statBonusDuration` | int | Turns bonus lasts (-1 = permanent) | -1, 3 |

**Example**: MOMENTUM BASH grants +1 STR for the duration of the dungeon (duration: -1)

### Conditional Scaling

| Property | Type | Description |
|----------|------|-------------|
| `healthThreshold` | double | Health percentage threshold (0.0-1.0) |
| `statThreshold` | double | Stat value threshold |
| `statThresholdType` | string | Stat type to check |
| `conditionalDamageMultiplier` | double | Damage multiplier when condition met |

**Note**: These properties exist in the Action class but are not currently used in Actions.json.

---

## Special Mechanics

### Extra Attacks

| Property | Type | Description | Example |
|----------|------|-------------|---------|
| `extraAttacks` | int | Number of additional attacks granted | 1 |

**Example**: BLITZ ATTACK grants 1 extra attack

### Reactive Actions

| Property | Type | Description | Example |
|----------|------|-------------|---------|
| `reactive` | bool | Triggers automatically on certain conditions | true |

**Example**: RIPOSTE triggers when enemy misses

### Action Skipping

| Property | Type | Description |
|----------|------|-------------|
| `skipNextTurn` | bool | Skips the next turn |
| `guaranteeNextSuccess` | bool | Guarantees next action succeeds |

**Note**: These properties exist but are not currently used in Actions.json.

### Action Repetition

| Property | Type | Description |
|----------|------|-------------|
| `repeatLastAction` | bool | Repeats the previous action |

**Note**: This property exists but is not currently used in Actions.json.

---

## Action Categories

Actions are organized by tags for filtering and selection:

### Character Actions
- **Basic**: BASIC ATTACK
- **Combo**: JAB, CLEAVE, LUCKY STRIKE, MOMENTUM BASH, DEAL WITH THE DEVIL, SECOND WIND

### Weapon Actions
- **Sword**: SWORD SLASH
- **Mace**: CRUSHING BLOW
- **Dagger**: POISON BLADE
- **Wand**: MAGIC MISSILE

### Armor Actions
- **Armor**: HEADBUTT (and other armor-based attacks)

### Class Actions

#### Barbarian (3 tiers)
- **Tier 1**: BERSERKER RAGE (self-damage, combo amplification scaling)
- **Tier 2**: Furious strike (damage based on missing health)
- **Tier 3**: Ultimate berserker (massive damage, 50% lifesteal)

#### Warrior (3 tiers)
- **Tier 1**: HEROIC STRIKE (combo step scaling)
- **Tier 2**: Shield bash (stun, damage reduction)
- **Tier 3**: Charging strike (damage, armor bonus)

#### Rogue (3 tiers)
- **Tier 1**: SHADOW STRIKE (combo scaling, bleed)
- **Tier 2**: Backstab (high roll bonus, bleed)
- **Tier 3**: Assassinate (damage, bleed)

#### Wizard (3 tiers)
- **Tier 1**: METEOR (combo scaling spell)
- **Tier 2**: Chain lightning (multi-hit spell)
- **Tier 3**: Arcane explosion (area damage, weaken)

### Tactical Actions
- **Lifesteal**: VAMPIRE STRIKE
- **Defensive**: BRACE
- **Speed**: BLITZ ATTACK
- **Counter**: RIPOSTE

### Enemy Actions
- **Basic**: ENEMY BASIC ATTACK
- **Status Effects**: BLEED, POISON, WEAKEN, STUN, SLOW, BURN attacks
- **Support**: ENEMY HEAL
- **Special**: MULTI-HIT, ROLL PENALTY

### Environment Actions
One unique action per dungeon theme (25 total):
- Forest, Lava, Crypt, Crystal, Temple, Generic, Ice, Shadow, Steampunk, Swamp, Astral, Underground, Storm, Nature, Arcane, Desert, Volcano, Ruins, Ocean, Mountain, Temporal, Dream, Dimensional, Divine, Void

---

## Action Selection

Actions are selected based on:
1. **Entity Type**: Character, Enemy, or Environment
2. **Tags**: Filter by weapon type, class, tactical category
3. **Combo State**: Actions with `isComboAction: true` can be chained
4. **Availability**: Actions must not be on cooldown

---

## Implementation Notes

### Default Values
- `damageMultiplier`: 1.0 (100% damage)
- `length`: 1.0 (normal speed)
- `comboOrder`: 0 (not in combo)
- `isComboAction`: false
- `multiHitCount`: 1 (single hit)
- `multiHitDamagePercent`: 1.0 (100% per hit)

### Duration Values
- Positive integers: Duration in turns
- `-1`: Permanent (lasts for dungeon duration)
- `0`: Instant (no duration)

### Percentage Values
- Damage multipliers: 1.0 = 100%, 1.5 = 150%
- Percentages: 0.25 = 25%, 0.5 = 50%
- Health thresholds: 0.25 = 25% health, 1.0 = 100% health

---

## Examples

### Basic Attack
```json
{
  "name": "BASIC ATTACK",
  "type": "Attack",
  "damageMultiplier": 1.0,
  "length": 1.0,
  "description": "A standard physical attack",
  "comboOrder": 0,
  "isComboAction": false
}
```

### Multi-Hit Attack
```json
{
  "name": "CLEAVE",
  "type": "Attack",
  "damageMultiplier": 1.0,
  "length": 1.0,
  "description": "Fast cleave that hits 3 times for 35% damage each",
  "comboOrder": 2,
  "isComboAction": true,
  "multiHitCount": 3,
  "multiHitDamagePercent": 0.35
}
```

### Status Effect Attack
```json
{
  "name": "POISON BLADE",
  "type": "Attack",
  "damageMultiplier": 1.0,
  "length": 1.0,
  "description": "Quick dagger strike that applies poison",
  "comboOrder": 0,
  "isComboAction": true,
  "causesPoison": true
}
```

### Class Action with Scaling
```json
{
  "name": "BERSERKER RAGE",
  "type": "Attack",
  "damageMultiplier": 1.0,
  "length": 1.0,
  "description": "Enter a berserker rage, dealing massive damage but taking 10% self-damage. Damage scales with combo amplification.",
  "comboOrder": 0,
  "isComboAction": true,
  "selfDamagePercent": 10,
  "tags": ["class", "barbarian", "tier1", "unique", "comboAmplificationScaling"]
}
```

---

## Advanced Mechanics (v7.0+)

### Roll Modification System

Actions can modify dice rolls through various mechanisms:

| Property | Type | Description | Example |
|----------|------|-------------|---------|
| `rollModifierAdditive` | int | Flat bonus/penalty to roll | +5, -3 |
| `rollModifierMultiplier` | double | Multiplier for roll value | 1.5x, 0.8x |
| `rollModifierMin` | int | Minimum roll value (clamp) | 5 |
| `rollModifierMax` | int | Maximum roll value (clamp) | 18 |
| `allowReroll` | bool | Allows rerolling the dice | true |
| `rerollChance` | double | Probability of reroll (0.0-1.0) | 0.5 (50%) |
| `explodingDice` | bool | Enables exploding dice mechanic | true |
| `explodingDiceThreshold` | int | Roll value that triggers explosion | 20 |
| `multipleDiceCount` | int | Number of dice to roll | 2, 3 |
| `multipleDiceMode` | string | How to combine dice ("Sum", "TakeLowest", "TakeHighest", "TakeAverage") | "Sum" |

**Examples**:
- **Advantage**: `multipleDiceCount: 2, multipleDiceMode: "TakeHighest"` (roll 2d20, take higher)
- **Disadvantage**: `multipleDiceCount: 2, multipleDiceMode: "TakeLowest"` (roll 2d20, take lower)
- **Exploding Dice**: `explodingDice: true, explodingDiceThreshold: 20` (on 20, roll again and add)

### Threshold Overrides

Actions can modify critical hit, combo, and hit thresholds:

| Property | Type | Description | Example |
|----------|------|-------------|---------|
| `criticalHitThresholdOverride` | int | Override critical hit threshold (0 = use default) | 18 |
| `comboThresholdOverride` | int | Override combo threshold (0 = use default) | 12 |
| `hitThresholdOverride` | int | Override hit threshold (0 = use default) | 4 |

**Note**: Threshold overrides apply to the actor using the action, not globally.

### Conditional Triggers

Actions can trigger based on combat events:

| Property | Type | Description | Example |
|----------|------|-------------|---------|
| `triggerConditions` | string[] | List of conditions to check | ["OnMiss", "OnCritical"] |
| `exactRollTriggerValue` | int | Trigger on exact roll value (0 = disabled) | 20 |
| `requiredTag` | string | Required tag for trigger | "FIRE" |

**Available Conditions**:
- `OnMiss`: Triggers when action misses
- `OnHit`: Triggers when action hits
- `OnCritical`: Triggers on critical hit
- `OnCombo`: Triggers during combo
- `OnEnemyDeath`: Triggers when enemy dies
- `OnHPThreshold`: Triggers at health threshold

### Advanced Status Effects

New status effects beyond the basic 6:

| Effect | Property | Description |
|--------|----------|-------------|
| **Vulnerability** | `causesVulnerability` | Target takes more damage |
| **Harden** | `causesHarden` | Target takes less damage |
| **Fortify** | `causesFortify` | Increases target's armor |
| **Focus** | `causesFocus` | Increases outgoing damage |
| **Expose** | `causesExpose` | Reduces target's armor |
| **HP Regen** | `causesHPRegen` | Heals target over time |
| **Armor Break** | `causesArmorBreak` | Significantly reduces armor |
| **Pierce** | `causesPierce` | Ignores armor |
| **Reflect** | `causesReflect` | Returns damage to attacker |
| **Silence** | `causesSilence` | Disables combo |
| **Stat Drain** | `causesStatDrain` | Steals stats from target |
| **Absorb** | `causesAbsorb` | Stores damage, releases at threshold |
| **Temporary HP** | `causesTemporaryHP` | Overheal/shields |
| **Confusion** | `causesConfusion` | Chance to attack self/ally |
| **Cleanse** | `causesCleanse` | Reduces negative effect stacks |
| **Mark** | `causesMark` | Next hit guaranteed crit |
| **Disrupt** | `causesDisrupt` | Resets combo |

**Note**: These effects use the same registry system as basic effects and can be stacked.

### Combo Routing

Actions can control combo flow:

| Property | Type | Description | Example |
|----------|------|-------------|---------|
| `comboJumpToSlot` | int | Jump to slot N in combo (0 = disabled) | 1 |
| `comboSkipNext` | bool | Skip next action in combo | true |
| `comboRepeatPrevious` | bool | Repeat previous action | true |
| `comboLoopToStart` | bool | Loop back to slot 1 | true |
| `comboStopEarly` | bool | Stop combo early | true |
| `comboDisableSlot` | bool | Disable this slot | true |
| `comboRandomAction` | bool | Random next action | true |
| `comboTriggerOnlyInSlot` | int | Only trigger if in slot N (0 = always) | 2 |

**Examples**:
- **Loop Combo**: `comboLoopToStart: true` - After this action, restart combo from beginning
- **Skip Weak Action**: `comboSkipNext: true` - Skip the next action in sequence
- **Random Combo**: `comboRandomAction: true` - Next action chosen randomly from combo pool

### Tag System

Actions and entities can have tags for matching and filtering:

| Property | Type | Description | Example |
|----------|------|-------------|---------|
| `tags` | string[] | List of tags for the action | ["FIRE", "WIZARD", "EPIC"] |

**Common Tags**:
- **Elements**: FIRE, WATER, ICE, EARTH, AIR, LIGHTNING
- **Classes**: WIZARD, WARRIOR, ROGUE, BARBARIAN
- **Rarities**: COMMON, UNCOMMON, RARE, EPIC, LEGENDARY, MYTHIC, TRANSCENDENT
- **Weapons**: SWORD, MACE, DAGGER, WAND
- **Scaling**: comboScaling, comboStepScaling, comboAmplificationScaling

Tags can be used for:
- Damage modification (fire vs ice)
- Action filtering
- Conditional effects
- Equipment matching

### Outcome Handlers

Actions can trigger outcomes based on combat results:

| Property | Type | Description |
|----------|------|-------------|
| `outcomeHandlers` | object[] | List of outcome handlers |

**Outcome Types**:
- **Enemy Death**: Triggers when enemy dies
- **HP Threshold**: Triggers at 50%, 25%, 10% health
- **Combo End**: Triggers when combo ends
- **Conditional XP**: Grants XP on specific conditions

**Note**: Outcome handlers are configured in JSON and processed by the outcome system.

---

## Summary

The action system supports:
- **5 Action Types**: Attack, Spell, Heal, Buff, Debuff
- **6 Basic Status Effects**: Bleed, Poison, Weaken, Stun, Slow, Burn
- **17 Advanced Status Effects**: Vulnerability, Harden, Fortify, Focus, Expose, HP Regen, Armor Break, Pierce, Reflect, Silence, Stat Drain, Absorb, Temporary HP, Confusion, Cleanse, Mark, Disrupt
- **Roll Modification**: Additive, multiplicative, clamp, reroll, exploding dice, multiple dice
- **Threshold Overrides**: Dynamic critical hit, combo, and hit thresholds
- **Conditional Triggers**: Event-driven action effects
- **Combo Routing**: Jump, skip, repeat, loop, stop, random actions
- **Tag System**: Flexible matching and filtering
- **Outcome Handlers**: Conditional effects based on combat results
- **Multiple Damage Modifiers**: Multipliers, multi-hit, conditional scaling
- **Combo System**: Chained actions with scaling bonuses
- **4 Weapon Types**: Sword, Mace, Dagger, Wand
- **4 Classes × 3 Tiers**: 12 unique class actions
- **25 Environment Actions**: One per dungeon theme
- **Tactical Actions**: Lifesteal, defensive, speed, counter mechanics

This system provides a flexible foundation for diverse combat mechanics while maintaining balance and clarity.

