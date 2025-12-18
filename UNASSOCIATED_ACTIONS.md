# Actions Without Weapon, Enemy, or Environment Associations

This document lists all actions that do not have an associated weapon, enemy, or environment tag.

## Summary

**Current Actions.json**: All actions in the current `GameData/Actions.json` file have proper associations (weapon, enemy, or environment tags).

**Backup File**: The backup file (`GameData/Actions.json.backup`) contains 25 actions that lack weapon/enemy/environment associations.

## Complete List of Unassociated Actions

### Character/Combo Actions
These actions are tagged with "character", "combo", or similar tags but not with weapon/enemy/environment:

1. **BASIC ATTACK** - Basic character attack action
2. **JAB** - Quick jab that resets enemy combo (Note: This exists in current file with "enemy" tag, but backup version lacks it)
3. **CLEAVE** - Fast cleave that hits 3 times
4. **LUCKY STRIKE** - Quick lucky strike that adds +2 to next roll
5. **MOMENTUM BASH** - Gain 1 STR for the duration of dungeon
6. **DEAL WITH THE DEVIL** - Do 5% damage to yourself
7. **SECOND WIND** - If 2nd slot, heal for 5 health

### Tactical Actions
These actions are tagged with "tactical" but not with weapon/enemy/environment:

8. **VAMPIRE STRIKE** - Life-draining attack that heals for 25% of damage dealt
9. **BRACE** - Defensive stance that reduces incoming damage by 25%
10. **BLITZ ATTACK** - Fast attack that allows immediate follow-up action
11. **RIPOSTE** - Counter-attack that triggers when enemy misses, deals 150% damage

### Class Actions
These actions are tagged with "class" but not with weapon/enemy/environment:

12. **BERSERKER RAGE** - Barbarian tier 1 ability (self-damage, combo amplification scaling)
13. **HEROIC STRIKE** - Warrior tier 1 ability (combo step scaling)
14. **SHADOW STRIKE** - Rogue tier 1 ability (combo scaling, bleed)
15. **METEOR** - Wizard tier 1 ability (combo scaling spell)
16. **BARBARIAN TIER 2** - Furious strike (damage based on missing health)
17. **BARBARIAN TIER 3** - Ultimate berserker (massive damage, 50% lifesteal)
18. **WARRIOR TIER 2** - Shield bash (stun, damage reduction)
19. **WARRIOR TIER 3** - Charging strike (damage, armor bonus)
20. **ROGUE TIER 2** - Backstab (high roll bonus, bleed)
21. **ROGUE TIER 3** - Assassinate (damage, bleed)
22. **WIZARD TIER 2** - Chain lightning (multi-hit spell)
23. **WIZARD TIER 3** - Arcane explosion (area damage, weaken)

### Armor Actions
24. **HEADBUTT** - Quick head strike that weakens enemy (tagged with "armor")

### Test Actions
25. **TEST ALL MECHANICS** - Comprehensive test action with all mechanics

## Notes

- All actions in the **current** `Actions.json` file have proper weapon/enemy/environment associations
- The unassociated actions listed above are from the backup file and may represent:
  - Actions that were removed from the current file
  - Actions that need to be properly tagged
  - Actions that are used differently (e.g., class actions, tactical actions)
- Some actions like class abilities (BERSERKER RAGE, HEROIC STRIKE, etc.) may intentionally not have weapon/enemy/environment tags as they are character-class-specific
- Tactical actions (VAMPIRE STRIKE, BRACE, BLITZ ATTACK, RIPOSTE) may also be intentionally unassociated as they are general character abilities

## Recommendations

1. **Review class actions**: Determine if class-specific actions should have additional tags or remain unassociated
2. **Review tactical actions**: Consider if tactical actions should be tagged differently or remain general character abilities
3. **Review combo actions**: Some combo actions may need to be associated with specific weapon types
4. **Update current file**: If any of these actions should be in the current `Actions.json`, ensure they have proper tags

