using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Handles combat action execution, combo logic, and action selection
    /// </summary>
    public static class CombatActions
    {
        /// <summary>
        /// Executes a single action with all its effects
        /// </summary>
        /// <param name="source">The entity performing the action</param>
        /// <param name="target">The target entity</param>
        /// <param name="environment">The environment affecting the action</param>
        /// <param name="lastPlayerAction">The last player action for DEJA VU functionality</param>
        /// <returns>A string describing the result of the action</returns>
        public static string ExecuteAction(Entity source, Entity target, Environment? environment = null, Action? lastPlayerAction = null, Action? forcedAction = null)
        {
            // Use forced action if provided (for combo system), otherwise select action normally
            var selectedAction = forcedAction ?? source.SelectAction();
            if (selectedAction == null)
            {
                return $"[{source.Name}] has no actions available.";
            }
            
            // Initialize results list
            var results = new List<string>();
            
            // Handle unique action chance for characters (not enemies)
            if (source is Character character && !(character is Enemy))
            {
                selectedAction = HandleUniqueActionChance(character, selectedAction);
            }
            
            // Calculate roll bonus based on entity type and action
            int rollBonus = CalculateRollBonus(source, selectedAction);
            
            // Roll for attack
            int baseRoll = Dice.Roll(1, 20);
            int attackRoll = baseRoll + rollBonus;
            int cappedRoll = Math.Min(attackRoll, 20);
            
            // Check for hit
            bool hit = CombatCalculator.CalculateHit(source, target, rollBonus, baseRoll);
            
            if (hit)
            {
                // Only apply damage for Attack-type actions
                if (selectedAction.Type == ActionType.Attack)
                {
                    // Calculate damage with entity-specific modifiers
                    double damageMultiplier = CalculateDamageMultiplier(source, selectedAction);
                    int damage = CombatCalculator.CalculateDamage(source, target, selectedAction, damageMultiplier, 1.0, rollBonus, baseRoll);
                    
                    // Apply damage
                    ApplyDamage(target, damage);
                    
                    // Add damage message
                    results.Add(CombatResults.FormatDamageDisplay(source, target, damage, damage, selectedAction, damageMultiplier, 1.0, rollBonus, baseRoll));
                }
                else
                {
                    // For non-attack actions, just show the action was successful
                    results.Add(CombatResults.FormatNonAttackAction(source, target, selectedAction, baseRoll, rollBonus));
                }
                
                // Apply status effects for all action types
                CombatEffects.ApplyStatusEffects(selectedAction, source, target, results);
                
                // Handle combo advancement for characters
                if (source is Character comboCharacter && !(comboCharacter is Enemy))
                {
                    comboCharacter.ComboStep++;
                }
            }
            else
            {
                results.Add(CombatResults.FormatMissMessage(source, target, selectedAction, baseRoll, rollBonus));
            }
            
            return string.Join("\n", results);
        }

        /// <summary>
        /// Handles unique action chance for characters
        /// </summary>
        private static Action HandleUniqueActionChance(Character character, Action selectedAction)
        {
            double uniqueActionChance = character.GetModificationUniqueActionChance();
            if (uniqueActionChance > 0.0)
            {
                double roll = Dice.Roll(1, 100) / 100.0;
                if (roll < uniqueActionChance)
                {
                    var availableUniqueActions = character.GetAvailableUniqueActions();
                    if (availableUniqueActions.Count > 0)
                    {
                        int randomIndex = Dice.Roll(1, availableUniqueActions.Count) - 1;
                        selectedAction = availableUniqueActions[randomIndex];
                        UIManager.WriteCombatLine($"[{character.Name}] channels unique power and uses [{selectedAction.Name}]!");
                    }
                }
            }
            return selectedAction;
        }
        
        /// <summary>
        /// Calculates roll bonus based on entity type and action
        /// </summary>
        public static int CalculateRollBonus(Entity source, Action? action)
        {
            int rollBonus = 0;
            
            if (source is Character character)
            {
                // Character-specific roll bonuses
                rollBonus += character.GetIntelligenceRollBonus();
                rollBonus += character.GetModificationRollBonus();
                rollBonus += character.GetEquipmentRollBonus();
                
                // Action-specific roll bonus
                if (action != null)
                {
                    rollBonus += action.RollBonus;
                    
                    // Combo scaling bonuses
                    if (action.Tags.Contains("comboScaling"))
                    {
                        rollBonus += character.ComboSequence.Count;
                    }
                    else if (action.Tags.Contains("comboStepScaling"))
                    {
                        rollBonus += (character.ComboStep % character.ComboSequence.Count) + 1;
                    }
                    else if (action.Tags.Contains("comboAmplificationScaling"))
                    {
                        var combatBalance = TuningConfig.Instance.CombatBalance;
                        rollBonus += (int)(character.GetComboAmplifier() * combatBalance.RollDamageMultipliers.ComboAmplificationScalingMultiplier);
                    }
                }
            }
            else if (source is Enemy enemy)
            {
                // Enemy-specific roll bonuses
                if (action != null)
                {
                    rollBonus += action.RollBonus;
                }
            }
            
            // Apply roll penalty
            rollBonus -= source.RollPenalty;
            
            return rollBonus;
        }
        
        /// <summary>
        /// Calculates damage multiplier based on entity type and action
        /// </summary>
        private static double CalculateDamageMultiplier(Entity source, Action action)
        {
            if (source is Character character)
            {
                return character.GetCurrentComboAmplification();
            }
            return 1.0;
        }
        
        /// <summary>
        /// Applies damage to target entity
        /// </summary>
        private static void ApplyDamage(Entity target, int damage)
        {
            if (target is Character targetCharacter)
            {
                targetCharacter.TakeDamage(damage);
            }
            else if (target is Enemy targetEnemy)
            {
                targetEnemy.TakeDamage(damage);
            }
        }


        /// <summary>
        /// Executes multiple attacks per turn based on the source's attack speed
        /// </summary>
        /// <param name="source">The entity performing the attacks</param>
        /// <param name="target">The entity receiving the attacks</param>
        /// <param name="environment">The environment affecting the attacks</param>
        /// <returns>A string describing the results of all attacks</returns>
        public static string ExecuteMultiAttack(Entity source, Entity target, Environment? environment = null)
        {
            if (source is Character character)
            {
                int attacksPerTurn = character.GetAttacksPerTurn();
                var results = new List<string>();
                
                for (int i = 0; i < attacksPerTurn; i++)
                {
                    // Check if target is alive
                    bool isAlive = true;
                    if (target is Character targetCharacter)
                        isAlive = targetCharacter.CurrentHealth > 0;
                    else if (target is Enemy targetEnemy)
                        isAlive = targetEnemy.CurrentHealth > 0;
                    
                    if (!isAlive) break; // Stop if target is dead
                    
                    string result = ExecuteAction(source, target, environment);
                    if (!string.IsNullOrEmpty(result))
                    {
                        results.Add(result);
                    }
                }
                
                return string.Join("\n", results);
            }
            else
            {
                // For non-character entities, just execute one action
                return ExecuteAction(source, target, environment);
            }
        }

        /// <summary>
        /// Executes an area of effect action
        /// </summary>
        /// <param name="source">The entity performing the action</param>
        /// <param name="targets">List of target entities</param>
        /// <param name="environment">The environment affecting the action</param>
        /// <param name="selectedAction">The action to perform (optional)</param>
        /// <returns>A string describing the results</returns>
        public static string ExecuteAreaOfEffectAction(Entity source, List<Entity> targets, Environment? environment = null, Action? selectedAction = null)
        {
            var results = new List<string>();
            
            // Get the action to use
            var action = selectedAction ?? source.SelectAction();
            if (action == null)
            {
                return $"[{source.Name}] has no actions available.";
            }
            
            // For environmental actions, use special duration-based system
            if (source is Environment)
            {
                return ExecuteEnvironmentalAction(source, targets, action, environment);
            }
            
            // Execute the action on each target (for non-environmental actions)
            foreach (var target in targets)
            {
                bool isAlive = false;
                if (target is Character targetCharacter)
                    isAlive = targetCharacter.CurrentHealth > 0;
                else if (target is Enemy targetEnemy)
                    isAlive = targetEnemy.CurrentHealth > 0;
                
                if (isAlive)
                {
                    string result = ExecuteAction(source, target, environment);
                    if (!string.IsNullOrEmpty(result))
                    {
                        results.Add(result);
                    }
                }
            }
            
            return string.Join("\n", results);
        }

        /// <summary>
        /// Executes an environmental action with duration-based effects
        /// </summary>
        /// <param name="source">The environment performing the action</param>
        /// <param name="targets">List of target entities</param>
        /// <param name="action">The action to perform</param>
        /// <param name="environment">The environment context</param>
        /// <returns>A string describing the results</returns>
        private static string ExecuteEnvironmentalAction(Entity source, List<Entity> targets, Action action, Environment? environment = null)
        {
            var results = new List<string>();
            
            // Roll 2d2-2 to determine duration (0-2 turns)
            int duration = Dice.Roll(1, 2) + Dice.Roll(1, 2) - 2;
            
            // If duration is 0, the effect is not applied
            if (duration == 0)
            {
                // Still show the action attempt but indicate no effect
                results.Add($"[{source.Name}]'s {action.Name} has no effect!");
                return string.Join("\n", results);
            }
            
            // Apply the same effect to all alive targets with the determined duration
            foreach (var target in targets)
            {
                bool isAlive = false;
                if (target is Character targetCharacter)
                    isAlive = targetCharacter.CurrentHealth > 0;
                else if (target is Enemy targetEnemy)
                    isAlive = targetEnemy.CurrentHealth > 0;
                
                if (isAlive)
                {
                    // Apply environmental effects based on action type
                    ApplyEnvironmentalEffect(source, target, action, duration, results);
                }
            }
            
            return string.Join("\n", results);
        }

        /// <summary>
        /// Applies environmental effects to a target
        /// </summary>
        /// <param name="source">The environment source</param>
        /// <param name="target">The target entity</param>
        /// <param name="action">The action being applied</param>
        /// <param name="duration">Duration of the effect</param>
        /// <param name="results">List to add result messages to</param>
        private static void ApplyEnvironmentalEffect(Entity source, Entity target, Action action, int duration, List<string> results)
        {
            // Apply effects based on action type and properties
            if (action.CausesBleed)
            {
                target.ApplyPoison(2, duration, true); // 2 damage per turn, bleeding type
                results.Add($"[{source.Name}] uses [{action.Name}] on [{target.Name}]!\n    [{target.Name}] is bleeding for {duration} turns!");
            }
            else if (action.CausesWeaken)
            {
                target.ApplyWeaken(duration);
                results.Add($"[{source.Name}] uses [{action.Name}] on [{target.Name}]!\n    [{target.Name}] is weakened for {duration} turns!");
            }
            else if (action.CausesSlow)
            {
                // Apply slow effect - for characters, use the character-specific method
                if (target is Character character)
                {
                    character.ApplySlow(0.5, duration); // 50% speed reduction
                }
                else
                {
                    // For enemies, we can't easily apply slow without modifying the base class
                    // For now, just show the message
                }
                results.Add($"[{source.Name}] uses [{action.Name}] on [{target.Name}]!\n    [{target.Name}] is slowed for {duration} turns!");
            }
            else if (action.CausesPoison)
            {
                target.ApplyPoison(2, duration); // 2 damage per turn for the duration
                results.Add($"[{source.Name}] uses [{action.Name}] on [{target.Name}]!\n    [{target.Name}] is poisoned for {duration} turns!");
            }
            else if (action.CausesStun)
            {
                target.IsStunned = true;
                target.StunTurnsRemaining = duration;
                results.Add($"[{source.Name}] uses [{action.Name}] on [{target.Name}]!\n    [{target.Name}] is stunned for {duration} turns!");
            }
            else if (action.Type == ActionType.Attack)
            {
                // For environmental attacks, calculate damage normally
                double damageMultiplier = CalculateDamageMultiplier(source, action);
                int damage = CombatCalculator.CalculateDamage(source, target, action, damageMultiplier, 1.0, 0, 0);
                
                // Apply damage
                ApplyDamage(target, damage);
                
                // Add damage message
                results.Add(CombatResults.FormatDamageDisplay(source, target, damage, damage, action, damageMultiplier, 1.0, 0, 0));
            }
            else
            {
                // Generic environmental effect
                results.Add($"[{source.Name}] uses [{action.Name}] on [{target.Name}]!\n    Effect lasts for {duration} turns!");
            }
        }


        /// <summary>
        /// Handles divine reroll functionality
        /// </summary>
        /// <param name="player">The player character</param>
        /// <param name="baseRoll">The original roll</param>
        /// <param name="totalRollBonus">Total roll bonus</param>
        /// <returns>New roll result and whether reroll was used</returns>
        public static (int newRoll, bool rerollUsed) HandleDivineReroll(Character player, int baseRoll, int totalRollBonus)
        {
            // For now, return the original roll without divine reroll functionality
            // This would need to be implemented based on the actual divine reroll system
            return (baseRoll, false);
        }
    }
}

