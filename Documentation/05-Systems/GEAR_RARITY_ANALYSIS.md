# Gear Rarity System Analysis & Tuning Guide

## Overview

The gear rarity system determines how powerful items are when they drop. Rarity affects:
- **Drop Frequency**: How often each rarity appears
- **Stat Bonuses**: Number of stat bonuses applied
- **Action Bonuses**: Number of action bonuses applied  
- **Modifications**: Number of modifications (special effects) applied

## Current Configuration

### Rarity Table (`GameData/RarityTable.json`)

| Rarity | Weight | Stat Bonuses | Action Bonuses | Modifications | Drop Rate % |
|--------|--------|--------------|----------------|---------------|-------------|
| Common | 500 | 0* | 0 | 0* | 87.8% |
| Uncommon | 50 | 2 | 1 | 1 | 8.8% |
| Rare | 15 | 3 | 2 | 2 | 2.6% |
| Epic | 3 | 3 | 2 | 2 | 0.5% |
| Legendary | 1 | 3 | 2 | 3 | 0.2% |
| Mythic | 0.1 | 4 | 3 | 4 | 0.02% |
| Transcendent | 0.01 | 5 | 4 | 5 | 0.002% |

**Total Weight**: 569.11

**Drop Rate Calculation**: `(Rarity Weight / Total Weight) × 100%`

*Note: Common items have a special 25% chance to get 1 stat bonus and 1 modification, otherwise they get none.

### Drop Rate Breakdown

```
Common:        500 / 569.11 = 87.8%  (most items)
Uncommon:       50 / 569.11 =  8.8%  (occasional upgrade)
Rare:           15 / 569.11 =  2.6%  (good find)
Epic:            3 / 569.11 =  0.5%  (rare find)
Legendary:       1 / 569.11 =  0.2%  (very rare)
Mythic:        0.1 / 569.11 =  0.02% (extremely rare)
Transcendent: 0.01 / 569.11 =  0.002% (nearly impossible)
```

## How Rarity Works

### 1. Rarity Roll Process

When an item drops, the system:
1. Rolls a random number between 0 and total weight (569.11)
2. Selects the rarity based on cumulative weight thresholds
3. Applies bonuses based on the rarity's configuration

**Code Location**: `Code/Data/LootRarityProcessor.cs` - `RollRarity()` method

### 2. Bonus Application

Each rarity level grants a specific number of bonuses:

#### Stat Bonuses
- Applied from `GameData/StatBonuses.json`
- Examples: "+2 Strength", "+10 Health", "+5 Armor"
- Weighted selection (common bonuses more likely)
- Range: Simple (+2 stat) to powerful (+10 to all stats)

#### Action Bonuses  
- Applied from action bonus pool
- Adds special actions to the item
- Typically 1-4 actions depending on rarity

#### Modifications
- Applied from `GameData/Modifications.json`
- Special effects like "Sharp (+2 damage)", "Vampiric (lifesteal)", "Masterwork (15% more damage)"
- Uses tier-based rolling system (1-31 dice result)
- Higher tier items get better modification chances

**Code Location**: `Code/Data/LootBonusApplier.cs` - `ApplyBonuses()` method

### 3. Common Item Special Case

Common items have a special rule:
- **75% chance**: No bonuses (pure base item)
- **25% chance**: 1 stat bonus + 1 modification (slightly enhanced)

This creates variety even in common items.

## Power Scaling Analysis

### Expected Bonuses Per Rarity

| Rarity | Total Bonuses | Power Level | Example Impact |
|--------|--------------|-------------|----------------|
| Common | 0-2* | Baseline | Base stats only |
| Uncommon | 4 | +20-30% | Noticeable upgrade |
| Rare | 7 | +40-60% | Significant boost |
| Epic | 7 | +40-60% | Strong item |
| Legendary | 8 | +50-70% | Very strong |
| Mythic | 11 | +70-100% | Exceptional |
| Transcendent | 14 | +100-150% | Game-changing |

*Common: 0 normally, 2 with 25% chance

### Modification Power Tiers

Modifications are rolled using a 1-31 dice system with tier-based weighting:

- **Dice 1-4**: Common (negative to small positive)
- **Dice 5-8**: Uncommon (+2 damage, +10% speed)
- **Dice 9-12**: Rare (+3 damage, +20% speed, lifesteal)
- **Dice 13-16**: Epic (+2-3 damage, +30% speed, status effects)
- **Dice 17-20**: Legendary (15% damage multiplier, +40% speed)
- **Dice 21-24**: Mythic (25% damage, +50% speed, auto-success)
- **Dice 25-28**: Transcendent (50%+ damage, game-breaking effects)

**Code Location**: `Code/Utils/Dice.cs` - `RollModification()` method

## Tuning Guidelines

### 1. Adjusting Drop Rates

To change how often rarities appear, modify **weights** in `RarityTable.json`:

```json
{ "Name": "Rare", "Weight": 15, ... }  // Current: 2.6% drop rate
```

**Tuning Tips**:
- **Increase weight** = more common (e.g., `Weight: 30` → 5.2% drop rate)
- **Decrease weight** = more rare (e.g., `Weight: 7.5` → 1.3% drop rate)
- **Maintain ratios**: Keep relative differences between rarities

**Example**: To make Rare items twice as common:
```json
{ "Name": "Rare", "Weight": 30, ... }  // 2.6% → 5.2%
```

### 2. Adjusting Bonus Counts

To change how powerful each rarity is, modify bonus counts:

```json
{ "Name": "Rare", "StatBonuses": 3, "ActionBonuses": 2, "Modifications": 2 }
```

**Tuning Tips**:
- **StatBonuses**: Primary power source (stats, health, armor, damage)
- **ActionBonuses**: Adds variety and tactical options
- **Modifications**: Adds special effects and multipliers

**Power Balance**:
- Each stat bonus ≈ +10-20% power
- Each modification ≈ +15-30% power (varies by tier)
- Action bonuses add utility, not raw power

### 3. Common Item Tuning

The 25% bonus chance for Common items is hardcoded in `LootBonusApplier.cs`:

```csharp
if (_random.NextDouble() < 0.25)  // Line 30
```

To change this:
- **More common bonuses**: Increase `0.25` (e.g., `0.40` = 40% chance)
- **Rarer bonuses**: Decrease `0.25` (e.g., `0.15` = 15% chance)

### 4. Modification Power Tuning

Modification rarity is controlled by `TuningConfig.json` → `ModificationRarity`:

```json
"ModificationRarity": {
  "Common": 35.0,      // 35% chance for dice 1-4
  "Uncommon": 25.0,    // 25% chance for dice 5-8
  "Rare": 20.0,        // 20% chance for dice 9-12
  "Epic": 12.0,        // 12% chance for dice 13-16
  "Legendary": 6.0,    // 6% chance for dice 17-20
  "Mythic": 1.8,       // 1.8% chance for dice 21-24
  "Transcendent": 0.2  // 0.2% chance for dice 25-28
}
```

**Tuning Tips**:
- Total should equal 100.0
- Shift percentages to make better mods more/less common
- Higher tier items get bonus to roll (via `TierBonusPerLevel`)

## Balance Considerations

### Current State Analysis

**Strengths**:
- Clear rarity progression (7 tiers)
- Significant power differences between tiers
- Common items provide baseline, higher rarities feel rewarding
- Special case for Common items adds variety

**Potential Issues**:
1. **Epic vs Rare**: Same bonus counts (7 each), only 0.3% drop rate difference
   - **Suggestion**: Give Epic 1 more modification (8 total vs 7)
   
2. **Transcendent**: Extremely rare (0.002%) but only 14 bonuses
   - **Suggestion**: Consider making it more impactful or slightly more common

3. **Mythic vs Legendary**: Only 0.18% drop rate difference
   - **Suggestion**: Consider adjusting weights to create clearer distinction

### Recommended Tuning Scenarios

#### Scenario 1: More Generous (Easier Game)
```json
[
  { "Name": "Common", "Weight": 400, ... },      // 70% (was 87.8%)
  { "Name": "Uncommon", "Weight": 80, ... },     // 14% (was 8.8%)
  { "Name": "Rare", "Weight": 30, ... },         // 5.3% (was 2.6%)
  { "Name": "Epic", "Weight": 8, ... },          // 1.4% (was 0.5%)
  { "Name": "Legendary", "Weight": 3, ... },     // 0.5% (was 0.2%)
  { "Name": "Mythic", "Weight": 0.5, ... },      // 0.09% (was 0.02%)
  { "Name": "Transcendent", "Weight": 0.05, ... } // 0.009% (was 0.002%)
]
```

#### Scenario 2: More Challenging (Rarer Good Items)
```json
[
  { "Name": "Common", "Weight": 600, ... },      // 92% (was 87.8%)
  { "Name": "Uncommon", "Weight": 35, ... },    // 5.4% (was 8.8%)
  { "Name": "Rare", "Weight": 8, ... },          // 1.2% (was 2.6%)
  { "Name": "Epic", "Weight": 1.5, ... },        // 0.23% (was 0.5%)
  { "Name": "Legendary", "Weight": 0.5, ... },   // 0.08% (was 0.2%)
  { "Name": "Mythic", "Weight": 0.05, ... },     // 0.008% (was 0.02%)
  { "Name": "Transcendent", "Weight": 0.005, ... } // 0.0008% (was 0.002%)
]
```

#### Scenario 3: Better Epic/Legendary Distinction
```json
[
  { "Name": "Epic", "Weight": 3, "StatBonuses": 3, "ActionBonuses": 2, "Modifications": 3 },  // +1 mod
  { "Name": "Legendary", "Weight": 1, "StatBonuses": 4, "ActionBonuses": 2, "Modifications": 3 } // +1 stat
]
```

## Testing Your Changes

### 1. Use the Rarity Analyzer Tool

A built-in analyzer tool is available at `Code/Utils/RarityAnalyzer.cs`:

```csharp
// Load rarity data (from your data cache or loader)
var rarityData = LoadRarityData(); // Load from RarityTable.json
var analyzer = new RarityAnalyzer(rarityData);
analyzer.AnalyzeAndPrint();
```

This will print:
- Drop rate analysis with percentages
- Power scaling analysis
- Tuning recommendations
- Simulation results (10,000 drops)

### 2. Manual Simulation

You can also manually simulate drop rates:

```csharp
// Simulate 10,000 drops
var results = new Dictionary<string, int>();
for (int i = 0; i < 10000; i++)
{
    var rarity = RarityProcessor.RollRarity();
    results[rarity.Name] = results.GetValueOrDefault(rarity.Name, 0) + 1;
}

// Print percentages
foreach (var kvp in results)
{
    Console.WriteLine($"{kvp.Key}: {kvp.Value / 100.0}%");
}
```

### 2. Test Power Scaling

Generate items of each rarity and compare:
- Total stat bonuses
- Number of modifications
- Overall power level

### 3. Playtest Impact

After tuning:
- Play through several dungeons
- Note how often you see each rarity
- Assess if progression feels right
- Check if game feels too easy/hard

## Magic Find (Future Enhancement)

The `RollRarity()` method accepts a `magicFind` parameter, but it's currently **not implemented**:

```csharp
public RarityData RollRarity(double magicFind = 0.0, int playerLevel = 1)
{
    // magicFind parameter is ignored currently
    // TODO: Implement magic find scaling
}
```

**Future Implementation Ideas**:
- Increase weights for higher rarities based on magic find
- Formula: `adjustedWeight = baseWeight * (1 + magicFind / 100)`
- Or: Shift roll result toward higher rarities

## Related Systems

### Item Tier System
- **Separate from rarity**: Items have tiers (1-5) based on level
- **Tier affects**: Base stats, modification roll bonuses
- **Rarity affects**: Number of bonuses, not base stats

### Loot Generation Flow
1. Calculate loot level (player level vs dungeon level)
2. Roll item tier (1-5) based on loot level
3. Select base item from tier
4. Apply tier-based stat scaling
5. **Roll rarity** (this system)
6. Apply rarity bonuses
7. Generate item name with bonuses

**Code Location**: `Code/Data/LootGenerator.cs` - `GenerateLoot()` method

## Summary

### Key Tuning Points

1. **Drop Rates**: Adjust weights in `RarityTable.json`
   - Higher weight = more common
   - Maintain relative ratios for balance

2. **Power Levels**: Adjust bonus counts in `RarityTable.json`
   - StatBonuses: Primary power
   - Modifications: Special effects
   - ActionBonuses: Tactical variety

3. **Common Items**: Modify 25% chance in `LootBonusApplier.cs`
   - Controls how often common items get bonuses

4. **Modification Quality**: Adjust `ModificationRarity` in `TuningConfig.json`
   - Controls power level of modifications

### Quick Reference

| File | What It Controls |
|------|-----------------|
| `RarityTable.json` | Drop rates (weights) and bonus counts |
| `LootBonusApplier.cs` | Common item bonus chance (25%) |
| `TuningConfig.json` | Modification rarity distribution |
| `StatBonuses.json` | Available stat bonus pool |
| `Modifications.json` | Available modification pool |

### Recommended Starting Point

If unsure how to tune, start with small adjustments:
- **10-20% changes** to weights
- **+1/-1** to bonus counts
- Test and iterate

The current system is well-balanced for a challenging but fair experience. Only adjust if you have specific goals (easier/harder game, more/less variety, etc.).

