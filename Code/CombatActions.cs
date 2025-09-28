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
        public static string ExecuteAction(Entity source, Entity target, Environment? environment = null, Action? lastPlayerAction = null)
        {
            // Get the selected action for all entities
            var selectedAction = source.SelectAction();
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
                // Calculate damage with entity-specific modifiers
                double damageMultiplier = CalculateDamageMultiplier(source, selectedAction);
                int damage = CombatCalculator.CalculateDamage(source, target, selectedAction, damageMultiplier, 1.0, rollBonus, baseRoll);
                
                // Apply damage
                ApplyDamage(target, damage);
                
                // Add damage message
                results.Add(CombatResults.FormatDamageDisplay(source, target, damage, damage, selectedAction, damageMultiplier, 1.0, rollBonus, baseRoll));
                
                // Apply status effects
                CombatEffects.ApplyStatusEffects(selectedAction, source, target, results);
                
                // Check for bleed chance (characters only)
                if (source is Character characterSource)
                {
                    CombatEffects.CheckAndApplyBleedChance(characterSource, target, results);
                }
                
                // Handle combo advancement for characters
                if (source is Character comboCharacter && !(comboCharacter is Enemy))
                {
                    comboCharacter.ComboStep++;
                }
            }
            else
            {
                results.Add($"[{source.Name}]'s {selectedAction.Name} misses {target.Name}!");
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
                        CombatLogger.Log($"[{character.Name}] channels unique power and uses [{selectedAction.Name}]!");
                    }
                }
            }
            return selectedAction;
        }
        
        /// <summary>
        /// Calculates roll bonus based on entity type and action
        /// </summary>
        private static int CalculateRollBonus(Entity source, Action action)
        {
            int rollBonus = 0;
            
            if (source is Character character)
            {
                // Character-specific roll bonuses
                rollBonus += character.GetIntelligenceRollBonus();
                rollBonus += character.GetModificationRollBonus();
                rollBonus += character.GetEquipmentRollBonus();
                
                // Action-specific roll bonus
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
                    rollBonus += (int)(character.GetComboAmplifier() * 2);
                }
            }
            else if (source is Enemy enemy)
            {
                // Enemy-specific roll bonuses
                rollBonus += action.RollBonus;
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
                return character.GetComboAmplifier();
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
            
            // Execute the action on each target
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

