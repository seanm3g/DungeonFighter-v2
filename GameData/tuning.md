# Game Balance Tuning Configuration

This file contains all the key balance parameters that can be adjusted to tune the game difficulty and progression.

## Character & Health Scaling

### Base Health
- **Player Base Health**: 50 (starting health for new characters)
- **Health Per Level**: 3 (health gained per character level)
- **Enemy Base Health**: 50 (base health for level 1 enemies)
- **Enemy Health Per Level**: 3 (health gained per enemy level)

### Health Regeneration
- **Base Health Regen**: 0 (starting health regeneration)
- **Health Regen Scaling**: 0.1 (health regen gained per level)

## Attribute Scaling

### Player Attribute Scaling
- **Base Attributes**: STR=5, AGI=5, TEC=5, INT=5 (starting attributes)
- **Attributes Per Level**: 1 (attributes gained per level)
- **Primary Attribute Bonus**: 2 (extra attribute per level for primary stat)

### Enemy Attribute Scaling
- **Base Attributes**: STR=8, AGI=6, TEC=4, INT=4 (starting attributes)
- **Attributes Per Level**: 1 (attributes gained per enemy level)
- **Primary Attribute Bonus**: 2 (extra attribute per level for primary stat)

## Damage & Combat

### Damage Calculation
- **Base Damage Formula**: STR + Highest Attribute + Weapon Damage + Roll Bonus
- **Critical Hit Threshold**: 20 (roll needed for critical hit)
- **Critical Hit Multiplier**: 2.0 (damage multiplier for critical hits)
- **Minimum Damage**: 1 (minimum damage dealt regardless of armor)

### Attack Speed
- **Base Attack Speed**: 1.0 (starting attack speed)
- **Agility Speed Bonus**: 0.03 (attack speed gained per agility point)
- **Minimum Attack Speed**: 0.2 (minimum attack speed)

### Armor System
- **Armor Reduction**: 1:1 (1 armor reduces 1 damage)
- **Enemy Armor Per Level**: 1 (armor gained per enemy level)

## Equipment & Items

### Weapon Damage Scaling
- **Tier 1 Base Damage**: 5-8 (damage range for tier 1 weapons)
- **Tier 2 Base Damage**: 8-12 (damage range for tier 2 weapons)
- **Tier 3 Base Damage**: 12-16 (damage range for tier 3 weapons)
- **Tier 4 Base Damage**: 16-20 (damage range for tier 4 weapons)
- **Tier 5 Base Damage**: 20-25 (damage range for tier 5 weapons)

### Armor Scaling
- **Tier 1 Base Armor**: 2-4 (armor range for tier 1 armor)
- **Tier 2 Base Armor**: 4-6 (armor range for tier 2 armor)
- **Tier 3 Base Armor**: 6-8 (armor range for tier 3 armor)
- **Tier 4 Base Armor**: 8-10 (armor range for tier 4 armor)
- **Tier 5 Base Armor**: 10-12 (armor range for tier 5 armor)

### Item Bonuses
- **Bonus Damage Range**: 1-10 (random bonus damage on weapons)
- **Bonus Attack Speed Range**: 1-10 (random bonus attack speed on weapons)

## Experience & Progression

### Experience Scaling
- **Base XP to Level 2**: 100 (XP needed for first level up)
- **XP Scaling Factor**: 1.5 (multiplier for each subsequent level)
- **Enemy XP Reward Base**: 10 (base XP reward from enemies)
- **Enemy XP Per Level**: 5 (additional XP per enemy level)

### Gold Rewards
- **Enemy Gold Base**: 5 (base gold reward from enemies)
- **Enemy Gold Per Level**: 3 (additional gold per enemy level)

## Loot & Rarity

### Loot Generation
- **Loot Chance Base**: 0.3 (30% chance for loot to drop)
- **Loot Chance Per Level**: 0.05 (5% additional chance per level)
- **Maximum Loot Chance**: 0.8 (80% maximum loot chance)

### Rarity Weights
- **Common Weight**: 50 (weight for common items)
- **Uncommon Weight**: 25 (weight for uncommon items)
- **Rare Weight**: 15 (weight for rare items)
- **Epic Weight**: 8 (weight for epic items)
- **Legendary Weight**: 2 (weight for legendary items)

## Combat Mechanics

### Roll System
- **Miss Threshold**: 1-5 (rolls that result in misses)
- **Basic Attack Threshold**: 6-13 (rolls for basic attacks)
- **Combo Threshold**: 14-20 (rolls for combo actions)
- **Critical Threshold**: 20+ (rolls for critical hits)

### Combo System
- **Combo Reset on Miss**: true (combo resets when missing)
- **Combo Reset on Basic Attack**: true (combo resets on basic attacks)
- **Combo Amplifier Base**: 1.0 (base combo damage multiplier)
- **Combo Amplifier at TEC=5**: 1.05 (amplifier when Technique is 5)
- **Combo Amplifier Max**: 2.0 (maximum amplifier at max Technique)
- **Combo Amplifier Max Tech**: 100 (Technique level for maximum amplifier)

#### Combo Damage Scaling
- **Formula**: Linear scaling from 1.05 (at TEC=5) to 2.0 (at TEC=100)
- **Exponential Scaling**: Each combo step multiplies damage by amp^step
  - Combo 1: amp^1 (e.g., 1.05^1 = 1.05x damage)
  - Combo 2: amp^2 (e.g., 1.05^2 = 1.10x damage)
  - Combo 3: amp^3 (e.g., 1.05^3 = 1.16x damage)
  - Combo 4: amp^4 (e.g., 1.05^4 = 1.22x damage)
  - Combo 5: amp^5 (e.g., 1.05^5 = 1.28x damage)

## Enemy Scaling

### Enemy Difficulty
- **Enemy Health Multiplier**: 1.0 (global enemy health multiplier)
- **Enemy Damage Multiplier**: 1.0 (global enemy damage multiplier)
- **Enemy Level Variance**: ±1 (enemy level can vary by this amount from room level)

### Enemy Types
- **Goblin**: Base Health=80, STR=4, AGI=6, TEC=2, INT=3, Primary=AGI
- **Orc**: Base Health=95, STR=10, AGI=4, TEC=2, INT=2, Primary=STR
- **Skeleton**: Base Health=85, STR=6, AGI=4, TEC=3, INT=4, Primary=STR
- **Bandit**: Base Health=85, STR=6, AGI=8, TEC=3, INT=5, Primary=AGI
- **Cultist**: Base Health=85, STR=4, AGI=5, TEC=8, INT=7, Primary=TEC
- **Spider**: Base Health=75, STR=3, AGI=10, TEC=3, INT=2, Primary=AGI
- **Slime**: Base Health=95, STR=7, AGI=3, TEC=2, INT=1, Primary=STR
- **Bat**: Base Health=70, STR=3, AGI=12, TEC=4, INT=3, Primary=AGI
- **Zombie**: Base Health=100, STR=8, AGI=2, TEC=1, INT=1, Primary=STR
- **Wraith**: Base Health=85, STR=5, AGI=7, TEC=6, INT=8, Primary=TEC

## Action System

### Action Probabilities
- **Basic Attack Weight**: 0.7 (70% chance for basic attack)
- **Special Action Weight**: 0.3 (30% chance for special action)

### Action Effects
- **Bleed Damage Per Turn**: 2 (damage from bleed effect)
- **Bleed Duration**: 3 (turns bleed lasts)
- **Stun Duration**: 1 (turns stun lasts)
- **Weaken Damage Reduction**: 0.2 (20% damage reduction from weaken)

## Environment Effects

### Environmental Modifiers
- **Forest Damage Bonus**: 1.0 (no change)
- **Lava Damage Bonus**: 1.2 (20% damage increase)
- **Ice Damage Reduction**: 0.8 (20% damage reduction)
- **Swamp Speed Reduction**: 0.9 (10% speed reduction)

## UI & Display

### Text Display
- **Enable Text Delays**: true (enable combat text delays)
- **Base Delay Per Action**: 400 (milliseconds delay per action length)
- **Minimum Delay**: 50 (minimum delay in milliseconds)
- **Combat Speed Multiplier**: 1.0 (speed multiplier for combat text)

## Save System

### Auto-Save
- **Auto-Save on Level Up**: true (save automatically when leveling up)
- **Auto-Save on Death**: true (save automatically when dying)
- **Auto-Save Interval**: 300 (seconds between auto-saves)

---

## Usage Notes

- All values can be adjusted to fine-tune game balance
- Decimal values are supported for multipliers and percentages
- Boolean values use true/false
- Ranges are specified as "min-max" format
- Some values may require game restart to take effect
- Test changes in small increments to maintain game balance
