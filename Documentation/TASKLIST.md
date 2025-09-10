# Task List


---

*Tasks will be updated as the project progresses.* 

## Action Combo System Tasks

1. **Design Action Data Structures**
   - Define the four actions (Taunt, Jab, Stun, Crit) with properties: damage %, length, description, and special effects.
2. **Implement Combo Logic**
   - Create logic to handle action sequences, dice rolls (1d20 + bonus), and combo progression.
   - Implement damage amplification (1.85x per successful combo step).
   - Reset combo on failed roll.
3. **Integrate Loot and Bonuses**
   - Add loot that grants +1 (or more) to action rolls.
   - Ensure loot bonuses are factored into combo checks.
4. **Update Combat System**
   - Integrate the action combo system into the main combat loop.
   - Ensure compatibility with existing character and enemy systems.
5. **Testing**
   - Write unit tests for action resolution, combo progression, and loot effects.
   - Test edge cases (e.g., max combo, repeated failures, stacking loot bonuses).
6. **Documentation**
   - Update documentation to reflect new mechanics and usage.

## Battle Narrative System Tasks

1. **Design Battle Narrative System** ✅
   - Create BattleEvent class to track combat events.
   - Create BattleNarrative class to generate poetic 3-act battle descriptions.
   - Implement event collection and analysis logic.
2. **Update Combat System** ✅
   - Modify Combat.ExecuteAction to record events instead of returning messages.
   - Add StartBattleNarrative and EndBattleNarrative methods.
   - Maintain backward compatibility with old message format.
3. **Update Game Loop** ✅
   - Modify Game class to use narrative system.
   - Remove individual action message printing.
   - Display poetic battle narrative at end of each encounter.
4. **Testing** ✅
   - Write unit tests for BattleNarrative generation.
   - Test various battle scenarios (player victory, enemy victory, combos, etc.).
   - Test integration with combat system.
   - Test edge cases and error conditions.
   - Consolidate all tests into Program.cs for easy access.
5. **Enhance Narrative Poetics** ✅
   - Rewrite narrative generation to focus on momentum shifts and health changes.
   - Replace action-specific descriptions with abstract, poetic interpretations.
   - Emphasize spirit, determination, and emotional impact over mechanical actions.
   - Use metaphorical language to describe damage and combat intensity.
6. **Implement Event-Driven Narrative** ✅
   - Redesign narrative system to be event-driven rather than always poetic.
   - Add significant event detection (first blood, health reversal, near death, good combos).
   - Implement informational summaries for regular combat actions.
   - Add poetic narrative only for significant moments.
   - Create demo functionality to showcase event-driven approach.
   - Update documentation to explain the new event-driven system.
7. **Documentation** ✅
   - Update documentation to reflect new event-driven narrative system.
   - Document significant events and their triggers.

## Game Balance Tasks

1. **Hero Base Stats Boost** ✅
   - Increased hero starting stats from (10, 8, 12) to (25, 20, 18) for Strength, Agility, Technique.
   - This gives the hero a significant advantage over enemies at level 1.

2. **Enemy Base Stats Reduction** ✅
   - Reduced enemy base stats across all enemy types and environments.
   - Lowered strength, agility, and technique values by approximately 30-40%.
   - Maintained relative differences between enemy types while making them less overpowered.

3. **Starting Gear Enhancement** ✅
   - Boosted starting weapon damage: Sword (5→12), Axe (7→15), Dagger (3→8).
   - Increased starting armor values: Helmet (2→5), Chest (4→8), Boots (1→3).
   - This provides better initial combat capability for new heroes.

4. **Balance Testing** ⏳
   - Test combat encounters to ensure hero can now win against level 1 enemies.
   - Verify that the balance changes create engaging but winnable battles.
   - Adjust further if needed based on testing results.

## Enemy Combat System Tasks

1. **Enemy d20 Roll System** ✅
   - Modified enemies to use d20 roll system like characters.
   - Enemies now roll d20 + level-based difficulty (8 + Level/2).
   - Higher level enemies have better accuracy.
   - Integrated with narrative system for consistent battle reporting.
   - Added Roll and Difficulty properties to BattleEvent class.
   - Updated Game.cs to use enemy's AttemptAction method instead of ExecuteAction.

2. **Narrative Integration** ✅
   - Updated enemy AttemptAction to work with battle narrative system.
   - Added IsInNarrativeMode() and AddBattleEvent() helper methods to Combat class.
   - Enemies now record their actions in the battle narrative for consistent reporting.
   - Maintained backward compatibility with old message format.

3. **Testing** ⏳
   - Test enemy d20 roll system to ensure it works correctly.
   - Verify that enemy actions are properly recorded in battle narratives.
   - Test difficulty scaling with enemy levels.
   - Ensure the system creates balanced and engaging combat encounters.

4. **Cooldown System Enhancement** ✅
   - Restored cooldown-based combat timing system.
   - Actions have different lengths that determine combat sequence.
   - Missing an action (failing d20 roll) costs 1/4 of the cooldown length.
   - Player failures detected by combo reset (ComboStep == 0).
   - Enemy failures tracked via return tuple (result, success).
   - Combat sequence determined by shortest cooldown.
   - Environment actions also use cooldown system.

5. **Real-Time Action Display** ✅
   - Changed default narrative balance to 0.0 (event-driven).
   - Actions are now displayed as they happen instead of waiting for battle end.
   - Modified Combat.ExecuteAction to show messages when narrative balance is 0.
   - Updated enemy AttemptAction to show real-time messages.
   - Removed end-of-battle narrative display since actions are shown immediately.
   - All combat actions (player, enemy, environment) now display in real-time.

6. **Menu Error Handling** ✅
   - Fixed main menu to continue on invalid input instead of exiting game.
   - Changed error messages to be more helpful ("Please enter 1, 2, or 3").
   - Used continue statements to loop back to menu prompt.
   - Improved user experience by preventing accidental game exits.

7. **Environment Action Fix** ✅
   - Fixed "Entrance Room is not a valid character for this action!" error.
   - Modified Combat.ExecuteAction to handle Environment entities properly.
   - Added theme-based scaling for environment actions (Lava: 1.5x, Crypt: 1.2x, Forest: 1.1x).
   - Environment actions now use their base value with theme-based multipliers.
   - Maintained compatibility with existing character and enemy action systems.

8. **Narrative Display Fix** ✅
   - Fixed issue where poetic narrative flavor text wasn't being displayed.
   - Changed default NarrativeBalance from 0.0 to 0.7 for better narrative experience.
   - Modified Game.cs to actually display battle narrative when narrative balance > 0.3.
   - Battle narrative now shows after each encounter when poetic mode is enabled.
   - Players will now see rich, poetic descriptions of their battles.

9. **Intelligent Delay System** ✅
   - Added EnableTextDisplayDelays setting to GameSettings to control when delays are applied.
   - Created ApplyTextDisplayDelay method in Combat class that only applies delays when text is displayed.
   - Modified combat loop to use intelligent delay system instead of cooldown-based timing.
   - Removed hardcoded Thread.Sleep calls from Combat class.
   - Delays now match action length and respect CombatSpeed setting.
   - When delays are disabled, calculations happen quickly in background for full narrative mode.
   - When delays are enabled, text display speed matches action length for action-by-action mode.

10. **Inventory Menu Exit Bug Fix** ✅
    - Fixed bug where choosing "Exit" (option 4) from inventory menu would continue to dungeon selection.
    - Modified ShowGearMenu method to return boolean indicating whether to continue or exit.
    - Updated Game.cs to handle the return value and return to main menu when user chooses exit.
    - Now option 3 (Continue to dungeon) and option 4 (Exit) work as expected.

11. **New Dice Mechanics System** ✅
    - Implemented new dice mechanics: 1-5 fail, 6-15 normal attack, 16-20 combo attack.
    - Added DiceResult class to track roll results, success status, and combo triggers.
    - Created RollComboAction() and RollComboContinue() methods for different roll types.
    - Added ComboModeActive property to Character class to track combo state.
    - Updated Combat.cs to use new dice mechanics and handle combo mode activation/deactivation.
    - Added ActivateComboMode(), DeactivateComboMode(), and ResetCombo() methods.
    - Once combo mode is triggered, subsequent rolls use 11+ threshold to continue.
    - Added comprehensive test to verify dice mechanics work correctly.
    - Removed old comboThreshold constant as it's no longer needed.

12. I want to reference the code in the folder D:\Code Projects\Sokobon for how they're replicating a terminal but getting text color and things like that.

13. Commit to git

14. setup unity integration

