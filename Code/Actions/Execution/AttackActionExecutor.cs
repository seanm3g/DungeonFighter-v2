using RPGGame.Combat;
using RPGGame.UI.ColorSystem;
using RPGGame.Utils;

namespace RPGGame.Actions.Execution
{
    /// <summary>
    /// Executes attack actions and returns ColoredText results
    /// Handles damage calculation, application, and formatting
    /// </summary>
    public static class AttackActionExecutor
    {
        /// <summary>
        /// Executes an attack action and returns ColoredText results
        /// </summary>
        public static (List<ColoredText> actionText, List<ColoredText> rollInfo) ExecuteAttackActionColored(
            Actor source, 
            Actor target, 
            Action selectedAction, 
            int baseRoll, 
            int rollBonus,
            int naturalRoll,
            BattleNarrative? battleNarrative)
        {
            double damageMultiplier = ActionUtilities.CalculateDamageMultiplier(source, selectedAction);
            int totalRoll = baseRoll + rollBonus;
            
            // Check for multi-hit attacks
            int multiHitCount = selectedAction.Advanced.MultiHitCount;
            
            // If multi-hit, process multiple hits; otherwise single hit
            if (multiHitCount > 1)
            {
                int totalDamage = 0;
                var allDamageText = new List<ColoredText>();
                var allRollInfo = new List<ColoredText>();
                
                // Process each hit
                for (int hit = 0; hit < multiHitCount; hit++)
                {
                    // Check if target is still alive
                    if (target is Character targetChar && targetChar.CurrentHealth <= 0)
                        break;
                    if (target is Enemy hitTargetEnemy && hitTargetEnemy.CurrentHealth <= 0)
                        break;
                    
                    // Calculate damage for this hit (action.DamageMultiplier already contains per-hit scaling)
                    int hitDamage = CombatCalculator.CalculateDamage(source, target, selectedAction, damageMultiplier, 1.0, rollBonus, totalRoll);
                    
                    // Apply damage
                    ActionUtilities.ApplyDamage(target, hitDamage);
                    totalDamage += hitDamage;
                    
                    if (!ActionExecutor.DisableCombatDebugOutput)
                    {
                        DebugLogger.WriteCombatDebug("ActionExecutor", $"{source.Name} dealt {hitDamage} damage (hit {hit + 1}/{multiHitCount}) to {target.Name} with {selectedAction.Name}");
                    }
                    
                    // Format damage display for this hit
                    var (hitDamageText, hitRollInfo) = CombatResults.FormatDamageDisplayColored(source, target, hitDamage, hitDamage, selectedAction, damageMultiplier, 1.0, rollBonus, baseRoll);
                    if (hitDamageText != null) allDamageText.AddRange(hitDamageText);
                    if (hitRollInfo != null) allRollInfo.AddRange(hitRollInfo);
                }
                
                // Track statistics for total damage
                if (source is Character character)
                {
                    ActionStatisticsTracker.RecordAttackAction(character, totalRoll, naturalRoll, rollBonus, totalDamage, selectedAction, target as Enemy);
                }
                
                if (target is Character targetCharacter)
                {
                    ActionStatisticsTracker.RecordDamageReceived(targetCharacter, totalDamage);
                }
                
                // BASIC ATTACK removed - all actions are now combo actions
                bool isCriticalHit = totalRoll >= 20;
                ActionUtilities.CreateAndAddBattleEvent(source, target, selectedAction, totalDamage, totalRoll, rollBonus, true, true, 0, 0, isCriticalHit, naturalRoll, battleNarrative);
                
                // Handle enemy roll penalty
                if (selectedAction.Advanced.EnemyRollPenalty > 0 && target is Enemy targetEnemy)
                {
                    targetEnemy.ApplyRollPenalty(selectedAction.Advanced.EnemyRollPenalty, 1);
                }
                
                // Reset combo if normal attack hits with roll 6-13
                if (source is Character resetCharacter && !(resetCharacter is Enemy))
                {
                    // Check if this is a normal attack (not a combo action)
                    bool isNormalAttack = selectedAction.Name == "BASIC ATTACK" || !selectedAction.IsComboAction;
                    
                    // If normal attack with roll 6-13, reset the combo
                    if (isNormalAttack && baseRoll >= 6 && baseRoll <= 13)
                    {
                        resetCharacter.ResetCombo();
                    }
                }
                
                // Handle combo advancement based on roll value
                // Start at step 1 (initial state, no bonus)
                // Step 1: Roll 14+ → go to step 2 (bonus applies)
                // Step 1: Roll < 14 → stay at step 1 (no bonus, still in combo mode)
                // Step 2+: Roll 14+ → continue to next step (bonus continues)
                // Step 2+: Roll < 14 → reset to step 1 (bonus resets, stay in combo mode)
                if (source is Character comboCharacter && !(comboCharacter is Enemy))
                {
                    int comboThreshold = GameConfiguration.Instance.RollSystem.ComboThreshold.Min; // 14
                    
                    if (comboCharacter.ComboStep == 1)
                    {
                        // At step 1, need 14+ to advance to step 2 (where bonus starts)
                        if (totalRoll >= comboThreshold)
                        {
                            // Advance to step 2 with routing support
                            comboCharacter.IncrementComboStep(selectedAction);
                        }
                        // If < 14, stay at step 1 (no bonus, but still in combo mode)
                    }
                    else if (comboCharacter.ComboStep >= 2)
                    {
                        // At step 2 or higher, need 14+ to continue combo
                        if (totalRoll >= comboThreshold)
                        {
                            // Combo continues - increment with routing support
                            comboCharacter.IncrementComboStep(selectedAction);
                        }
                        else
                        {
                            // Didn't get 14+, reset to step 1 (bonus resets, but stay in combo mode)
                            comboCharacter.ComboStep = 1;
                        }
                    }
                    else if (comboCharacter.ComboStep == 0)
                    {
                        // If somehow at step 0, initialize to step 1
                        comboCharacter.ComboStep = 1;
                    }
                }
                
                return (allDamageText, allRollInfo);
            }
            else
            {
                // Single hit (original behavior)
                int damage = CombatCalculator.CalculateDamage(source, target, selectedAction, damageMultiplier, 1.0, rollBonus, totalRoll);
                
                ActionUtilities.ApplyDamage(target, damage);
                
                if (!ActionExecutor.DisableCombatDebugOutput)
                {
                }
                
                // Track statistics
                if (source is Character character)
                {
                    ActionStatisticsTracker.RecordAttackAction(character, totalRoll, naturalRoll, rollBonus, damage, selectedAction, target as Enemy);
                }
                
                if (target is Character targetCharacter)
                {
                    ActionStatisticsTracker.RecordDamageReceived(targetCharacter, damage);
                }
                
                bool isCombo = selectedAction.Name != "BASIC ATTACK";
                bool isCriticalHit = totalRoll >= 20;
                ActionUtilities.CreateAndAddBattleEvent(source, target, selectedAction, damage, totalRoll, rollBonus, true, isCombo, 0, 0, isCriticalHit, naturalRoll, battleNarrative);
                
                var (damageText, rollInfo) = CombatResults.FormatDamageDisplayColored(source, target, damage, damage, selectedAction, damageMultiplier, 1.0, rollBonus, baseRoll);
                
                // Handle enemy roll penalty
                if (selectedAction.Advanced.EnemyRollPenalty > 0 && target is Enemy targetEnemy)
                {
                    targetEnemy.ApplyRollPenalty(selectedAction.Advanced.EnemyRollPenalty, 1);
                }
                
                // Reset combo if normal attack hits with roll 6-13
                if (source is Character resetCharacter && !(resetCharacter is Enemy))
                {
                    // Check if this is a normal attack (not a combo action)
                    bool isNormalAttack = selectedAction.Name == "BASIC ATTACK" || !selectedAction.IsComboAction;
                    
                    // If normal attack with roll 6-13, reset the combo
                    if (isNormalAttack && baseRoll >= 6 && baseRoll <= 13)
                    {
                        resetCharacter.ResetCombo();
                    }
                }
                
                // Handle combo advancement based on roll value
                // Start at step 1 (initial state, no bonus)
                // Step 1: Roll 14+ → go to step 2 (bonus applies)
                // Step 1: Roll < 14 → stay at step 1 (no bonus, still in combo mode)
                // Step 2+: Roll 14+ → continue to next step (bonus continues)
                // Step 2+: Roll < 14 → reset to step 1 (bonus resets, stay in combo mode)
                if (source is Character comboCharacter && !(comboCharacter is Enemy))
                {
                    int comboThreshold = GameConfiguration.Instance.RollSystem.ComboThreshold.Min; // 14
                    
                    if (comboCharacter.ComboStep == 1)
                    {
                        // At step 1, need 14+ to advance to step 2 (where bonus starts)
                        if (totalRoll >= comboThreshold)
                        {
                            // Advance to step 2 with routing support
                            comboCharacter.IncrementComboStep(selectedAction);
                        }
                        // If < 14, stay at step 1 (no bonus, but still in combo mode)
                    }
                    else if (comboCharacter.ComboStep >= 2)
                    {
                        // At step 2 or higher, need 14+ to continue combo
                        if (totalRoll >= comboThreshold)
                        {
                            // Combo continues - increment with routing support
                            comboCharacter.IncrementComboStep(selectedAction);
                        }
                        else
                        {
                            // Didn't get 14+, reset to step 1 (bonus resets, but stay in combo mode)
                            comboCharacter.ComboStep = 1;
                        }
                    }
                    else if (comboCharacter.ComboStep == 0)
                    {
                        // If somehow at step 0, initialize to step 1
                        comboCharacter.ComboStep = 1;
                    }
                }
                
                return (damageText, rollInfo);
            }
        }
    }
}

