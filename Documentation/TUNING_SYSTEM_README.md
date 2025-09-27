# Dynamic Tuning System

## Overview

The Dynamic Tuning System allows real-time adjustment and balancing of game parameters without requiring code recompilation. This system provides comprehensive tools for tuning combat mechanics, item scaling, progression curves, and rarity systems.

## Features

### 🔧 Real-time Parameter Adjustment
- Modify combat parameters (damage, critical hits, attack speeds)
- Adjust item scaling formulas for weapons and armor
- Fine-tune rarity system multipliers
- Customize progression curves for experience and attributes

### 📊 Balance Analysis Tools
- DPS calculation and analysis across different levels and tiers
- Item distribution analysis with statistical reporting
- Automated combat scenario testing
- Progression curve analysis and growth rate calculations

### 💾 Configuration Management
- Export current configurations to timestamped files
- Import configuration presets
- Hot-reload configurations without restarting the game
- Formula-based scaling with mathematical expression support

## How to Access

1. **From Main Menu**: Select option "4. Tuning Console" from the main game menu
2. **In-Game**: The tuning console provides real-time access to all parameters

## Tuning Console Menu

### 1. Combat Parameters
- Critical Hit Threshold and Multiplier
- Minimum Damage values
- Base Attack Time and Agility modifiers

### 2. Item Scaling Formulas
- Weapon damage scaling by tier and level
- Armor value scaling formulas
- Real-time formula adjustment with immediate effect

### 3. Rarity System
- Stat bonus multipliers for each rarity tier
- Drop chance formulas
- Rarity distribution tuning

### 4. Progression Curves
- Experience requirement formulas
- Attribute growth equations
- Linear and quadratic growth factors

### 5. Test Scaling Calculations
- Live testing of scaling formulas
- Weapon and armor scaling demonstrations
- Rarity multiplier verification

### 6. Run Balance Analysis
- Comprehensive DPS analysis
- Item generation distribution testing
- Combat scenario simulations
- Progression curve analysis

## Configuration Files

### Primary Configuration
- `GameData/TuningConfig.json` - Main configuration file with all tuning parameters

### Extended Configuration
- `GameData/ItemScalingConfig.json` - Weapon-type specific scaling formulas and rarity modifiers

## Formula System

The system supports mathematical expressions with variables:

```json
{
  "WeaponDamageFormula": {
    "Formula": "BaseDamage * (1 + (Tier - 1) * TierScaling + Level * LevelScaling)",
    "TierScaling": 2.5,
    "LevelScaling": 0.8
  }
}
```

### Supported Operations
- Basic arithmetic: `+`, `-`, `*`, `/`
- Power operations: `^` (converted to Math.Pow)
- Parentheses for grouping
- Variable substitution with exact matching

### Common Variables
- `BaseDamage`, `BaseArmor` - Base item values
- `Tier` - Item tier (1-5)
- `Level` - Player/enemy level
- `PlayerLevel` - Specific player level
- `TierScaling`, `LevelScaling` - Scaling factors

## Balance Analysis Features

### DPS Analysis
- Calculates theoretical DPS for different weapon tiers and player levels
- Factors in critical hits, combo potential, and attribute scaling
- Provides growth rate analysis across level ranges

### Item Distribution Analysis
- Simulates large-scale item generation (1000+ samples)
- Reports tier and rarity distribution percentages
- Identifies potential balance issues automatically

### Combat Scenarios
- Automated combat simulations with various player/enemy matchups
- Win rate calculations and average combat duration
- Player survivability analysis

### Progression Analysis
- Experience requirement curves
- Attribute growth visualization
- Growth rate calculations and balance recommendations

## Usage Examples

### Adjusting Weapon Scaling
1. Access Tuning Console → Item Scaling Formulas
2. Modify Tier Scaling or Level Scaling values
3. Test changes with "Test Scaling Calculations"
4. Run balance analysis to verify impact

### Balancing Rarity System
1. Access Tuning Console → Rarity System
2. Adjust multipliers for specific rarity tiers
3. Run "Item Distribution Analysis" to verify changes
4. Export configuration when satisfied

### Combat Balancing
1. Access Tuning Console → Combat Parameters
2. Adjust critical hit rates, damage multipliers
3. Run "Combat Scenario Testing" to evaluate impact
4. Fine-tune based on win rates and combat duration

## Tips for Effective Tuning

### Start Small
- Make incremental changes (10-20% adjustments)
- Test after each change
- Use the analysis tools to verify impact

### Use Analysis Tools
- Always run balance analysis after major changes
- Pay attention to growth rates and distribution warnings
- Test multiple scenarios before finalizing changes

### Document Changes
- Export configurations with descriptive names
- Keep notes on what changes were made and why
- Test both early game and late game scenarios

### Common Balance Points
- **Early Game**: Focus on tier 1-2 weapons, levels 1-5
- **Mid Game**: Balance tier 3-4 equipment, levels 6-15
- **Late Game**: Ensure tier 5 items and high-level scaling remain challenging

## Troubleshooting

### Formula Errors
- Check variable names match exactly (case-sensitive)
- Ensure parentheses are balanced
- Verify mathematical operators are supported

### Configuration Issues
- Use "Reload Configuration" if changes don't appear
- Check JSON syntax in configuration files
- Restart application if major structural changes are made

### Performance Considerations
- Large analysis samples (>5000) may take time
- Complex formulas with many operations may slow calculations
- Consider simpler formulas for frequently-called calculations

## Integration with Existing Systems

The tuning system integrates seamlessly with:
- **LootGenerator**: Uses scaling formulas for item generation
- **Combat System**: Applies tuning parameters to damage calculations
- **Character Progression**: Uses progression curves for leveling
- **Enemy Scaling**: Applies scaling formulas to enemy stats

All changes take effect immediately without requiring game restart, making iterative tuning fast and efficient.
