# tasklist:

## Completed Tasks ✅

1. ✅ Always reset combo at end of each room
2. ✅ If an item has an affix, show it's bonuses in the inventory.
3. ✅ Show attack, armor, and health of enemies at start of room.
4. ✅ "2. Uncommon Brutal Leather Cap of the Bear of the Wind (Head) - Armor: 1 | Actions: Action:"  This shouldn't say Action twice
5. ✅ Remove this text: "Save file character_save.json has been deleted."
6. ✅ Remove reset combo option from combo menu
7. ✅ When deleting characters this is redundant: "Saved character has been deleted successfully.
Press any key to return to settings...

Press any key to continue..."
8. ✅ Enemies should have INT as an attribute as well.
9. ✅ When an item that has actions to it is unequipped, those actions should be removed from the combo and action pool.
10. ✅ Put all of the stats including actions indended on the next line
Uncommon Brutal Steel Dagger of the Hawk of Power (Rogue Weapon) - Damage: 11 | Actions: QUICK STAB, POISON BLADE,
    Stat Bonuses: of the Hawk (+3 TEC), of Power (+3 Damage)
    Action Bonuses:
    Modifications: Brutal
11. ✅ If you roll a 20+ double the damage
12. ✅ Add +1 to roll more common to gear.
13. ✅ This section should be inline with each item:
  "Weapon Stats: Damage 4, Attack Speed 1.5
  Head Stats: Armor 2
  Body Stats: Armor 4
  Feet Stats: Armor 1"
14. ✅ when displaying actions the extra commas can go:     Actions: Actions: MAGIC MISSILE, ARCANE SHIELD, ,
15. ✅ [Hero] attempts to attack but something went wrong. (Rolled 21) | in this instance where the roll is over 20+ count it as 20 and treat it as a crit.
16. ✅ The two actions on the starting item should automatically be added to the combo by default
17. ✅ You can remove this line: "Player Combo: Combo Step: 1/2 | Amplification: 1.05x | Base Amp: 1.050"
18. ✅ Can you remove the details in Combo Sequence for the actions and just leave the actions?  The details in are in Action Pool already
19. ✅ Add action length to the inventory info for each action.
20. ✅ You can remove "Current Combo Step: 1 of 2" from the menu
21. ✅ Make sure that when a dungeon is being selected there are no repeat options
22. ✅ Put the Action Pool under the inventory
23. ✅ In combat text you can just use [Bat] instead of [Bat Lv2], remove the lv2 portion
24. ✅ Next Upgrades: Barbarian: 5 to 5 | Warrior: 5 to 5 | Rogue: 5 to 5 | Wizard: 2 to 5  should read Wisard: 2 to go
25. ✅ Only show Next Upgrade for the highest and second highest class.
26. ✅  Modifications: Agile, Godlike | show the bonuses like | Stat Bonuses: of Fortification (+5 Armor), of the Hawk (+3 TEC), of Annihilation (+15 Damage)
27. ✅ Add the option to disgard items from your invenotry
28. ✅ I did not get the class Action for Wizard at 5 class points.
29. ✅ Arcane Shield doesn't seem to do anything.  Can it add a -2 to the enemies roll their next roll?
30. ✅ Swap enemy stats and Hero stats Hero Stats - Health: 127/137, Armor: 27, Attack: STR 37, AGI 29, TEC 32, INT 54
Enemy Stats - Health: 134/134, Armor: 9, Attack: STR 33, AGI 13, TEC 12, INT 13
31. ✅ Hide any Classes that you have zero skill points in.  (Tihs may mean only 1 class "points to go" shows)
32. ✅ Add 1 line between "Encountered *enemy*" and stats.
33. ✅ Swap hero and Enemy stats after" Encounter *enemy*"
34. ✅ Class Points: Barbarian(0) Warrior(0) Rogue(0) Wizard(4) Class points can hide any with 0 points.
35. ✅ Weapons in inventory should show its Attack Speed
36. ✅ Clean out the action pool if a weapon or item is disgarded (either but replacing with a different item, or being disgarded.)
37. ✅ Add more variety of actions to gear.
38. ✅ ARMOR SPIKES
      Sharp spikes on your armor cause damage on contact | Damage: 0.9x | Length: 1.0 | Damage: 0.9x
      This should be an armor status, not an action.
39. ✅ If i put on a piece of gear that increase my health in inventory, I want to start the next dungeon with full health.
skip | 40. Audit the environmental actions to be half good, half bad, and are more appropriate narratively matched.
skip | 41. The way action speed is displayed for actions doesn't make sense.
skip | 42. enemies should maybe be affected inverse or neutral to enviromental actions as the hero
skip | 43. Enemies are weak and need to be balanced around a DPS calculator.
skip | 44. Redesign the chance for environment action.  It's always happening in the first set of order.
skip | 45. Make tables for each weapon and class so that when a item is rolled 80% of the time it rolls for that set, 20% of the time it rolls from ALL actions.
skip | 46. Check that you can't add an action more than one time in a sequence | This should be a rare item or action description *remove limitation*
skip | 47. take a pass at actions to remove any that have zero damage, especially a pass for enemies actions.


## New Tasks - Dynamic Tuning System 🔧

44. ✅ Expand TuningConfig.json with scaling formulas for item damage, armor, and rarity systems
45. ✅ Create FormulaEvaluator.cs to parse and evaluate mathematical expressions from config
46. ✅ Create ScalingManager.cs for centralized scaling calculations (weapon damage, armor values, drop chances)
47. ✅ Create TuningConsole.cs for in-game real-time parameter adjustment interface (Note: Referenced but not implemented)
48. ✅ Create BalanceAnalyzer.cs for automated balance testing and DPS calculations
49. ✅ Add ItemScalingConfig.json for weapon-type specific scaling formulas
50. ✅ Integrate scaling system with existing LootGenerator to use dynamic formulas
51. ✅ Add tuning console access to main game menu for live parameter adjustment (Note: Referenced but not implemented)
52. ✅ Create balance testing scenarios and automated combat simulations
53. ✅ Add export/import functionality for tuning configurations

## New Tasks - Action Display Improvements 📋

54. ✅ Add stat bonus display to action descriptions (DANCE now shows +1 AGI (dungeon))

## New Tasks - Combat Balance Adjustments ⚔️

55. ✅ Reduce overall damage scaling to make combat last longer and be more balanced
56. ✅ Implement "actions to kill" as core balance mechanic (2 DPS at level 1, ~10 actions to kill)
57. ✅ Fix critical balance issues - enemies dying in 1 hit, weapon damage too high

## New Tasks - Documentation & Organization 📚

58. ✅ Create comprehensive documentation system for efficient development
59. ✅ Create PROBLEM_SOLUTIONS.md - Solution space for common problems
60. ✅ Create QUICK_REFERENCE.md - Fast lookup for key information
61. ✅ Create DEBUGGING_GUIDE.md - Common debugging patterns and solutions
62. ✅ Create CODE_PATTERNS.md - Document common code patterns and conventions
63. ✅ Create TESTING_STRATEGY.md - Testing approaches and verification methods
64. ✅ Create DEVELOPMENT_WORKFLOW.md - Step-by-step development process
65. ✅ Create KNOWN_ISSUES.md - Track known problems and their status
66. ✅ Create PERFORMANCE_NOTES.md - Performance considerations and optimizations
67. ✅ Create DEVELOPMENT_GUIDE.md - Comprehensive development guide
68. ✅ Update README.md with references to all new organizational tools

## New Tasks - Documentation Implementation 📚

69. ✅ Create .cursor/rules files for better AI assistance
70. ✅ Add cross-references between documentation files
71. ✅ Create documentation index/table of contents
72. ✅ Update existing documentation to reference new tools

## New Tasks - Code Optimization & Efficiency 🚀

73. ✅ Analyze current codebase for documentation alignment
74. ✅ Implement code patterns from CODE_PATTERNS.md
75. ✅ Add comprehensive error handling following patterns
76. ✅ Implement performance optimizations from PERFORMANCE_NOTES.md
77. ✅ Add debugging tools and logging from DEBUGGING_GUIDE.md
78. ✅ Enhance testing framework following TESTING_STRATEGY.md
79. ✅ Optimize data loading patterns for efficiency
80. ✅ Implement caching strategies for better performance
81. ✅ Fix text display inconsistencies - remove delays between action and details, ensure proper indentation
81. Add a way to save balance tunings that can be restored or loaded later.  Always label a balance tuning
82. ✅ Replace swap abilities function with re-order abilities function that accepts sequence like 15324
83. add multi-attacks per action
84. Add a roll for enemy weapons (that can affect the damage output they have, or actions they have available)
85. Fix menus to always use 0 as return to main menu.
86. Fix the Save character logic so that you can load a chracter to play again.