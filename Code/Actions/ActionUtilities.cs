using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using RPGGame.UI.ColorSystem;

namespace RPGGame
{
    /// <summary>
    /// Shared utilities for action-related operations
    /// Consolidates duplicate logic from ActionExecutor, ActionSelector, and CombatActions
    /// </summary>
    public static class ActionUtilities
    {
        /// <summary>
        /// Gets combo actions for an entity
        /// </summary>
        /// <param name="source">The entity to get combo actions for</param>
        /// <returns>List of combo actions</returns>
        public static List<Action> GetComboActions(Actor source)
        {
            if (source is Character character)
            {
                return character.GetComboActions();
            }
            else
            {
                // For enemies, get combo actions from ActionPool
                return source.ActionPool.Where(a => a.action.IsComboAction).Select(a => a.action).ToList();
            }
        }
        
        /// <summary>
        /// Gets the current combo step for an entity
        /// </summary>
        /// <param name="source">The entity to get combo step for</param>
        /// <returns>Current combo step</returns>
        public static int GetComboStep(Actor source)
        {
            if (source is Character character)
            {
                return character.ComboStep;
            }
            else
            {
                return 0; // Enemies don't have combo steps
            }
        }

        /// <summary>
        /// Calculates roll bonus based on entity type and action
        /// </summary>
        /// <param name="source">The entity performing the action</param>
        /// <param name="action">The action being performed (optional)</param>
        /// <returns>Roll bonus value</returns>
        public static int CalculateRollBonus(Actor source, Action? action = null)
        {
            return CombatCalculator.CalculateRollBonus(source, action, GetComboActions(source), GetComboStep(source));
        }

        /// <summary>
        /// Calculates damage multiplier based on entity type and action
        /// </summary>
        /// <param name="source">The entity performing the action</param>
        /// <param name="action">The action being performed</param>
        /// <returns>Damage multiplier value</returns>
        public static double CalculateDamageMultiplier(Actor source, Action action)
        {
            if (source is Character character)
            {
                // Only apply combo amplification to combo actions, and only after the first one
                if (action.IsComboAction && character.ComboStep > 0)
                {
                    return character.GetCurrentComboAmplification();
                }
            }
            else if (source is Enemy enemy)
            {
                // Enemies also get combo amplification (same as heroes)
                if (action.IsComboAction && enemy.ComboStep > 0)
                {
                    return enemy.GetCurrentComboAmplification();
                }
            }
            return 1.0;
        }

        /// <summary>
        /// Calculates the amount of healing for a healing action
        /// </summary>
        /// <param name="source">The entity performing the healing</param>
        /// <param name="action">The healing action</param>
        /// <returns>Healing amount</returns>
        public static int CalculateHealAmount(Actor source, Action action)
        {
            // Base healing from action properties
            int baseHeal = action.Advanced.HealAmount;
            
            // Add technique-based healing for characters
            if (source is Character character)
            {
                baseHeal += character.Technique;
            }
            else if (source is Enemy enemy)
            {
                // For enemies, use a simple calculation based on their stats
                baseHeal += enemy.Technique;
            }
            
            return Math.Max(1, baseHeal); // Ensure at least 1 healing
        }

        /// <summary>
        /// Applies damage to target entity
        /// </summary>
        /// <param name="target">The entity receiving damage</param>
        /// <param name="damage">The amount of damage to apply</param>
        public static void ApplyDamage(Actor target, int damage)
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
        /// Applies healing to a target entity
        /// </summary>
        /// <param name="target">The entity receiving healing</param>
        /// <param name="amount">The amount of healing to apply</param>
        public static void ApplyHealing(Actor target, int amount)
        {
            if (target is Character character)
            {
                character.Heal(amount);
            }
            else if (target is Enemy enemy)
            {
                // For enemies, we need to add a Heal method or use direct health modification
                enemy.CurrentHealth = Math.Min(enemy.MaxHealth, enemy.CurrentHealth + amount);
            }
        }

        /// <summary>
        /// Gets the current health of an entity
        /// </summary>
        /// <param name="entity">The entity to get health for</param>
        /// <returns>Current health value</returns>
        public static int GetEntityHealth(Actor entity)
        {
            return entity switch
            {
                Enemy enemy => enemy.CurrentHealth,
                Character character => character.CurrentHealth,
                _ => 0
            };
        }

        /// <summary>
        /// Handles unique action chance for characters
        /// </summary>
        /// <param name="character">The character to check for unique action chance</param>
        /// <param name="selectedAction">The currently selected action</param>
        /// <returns>The action to use (may be different from selectedAction)</returns>
        public static Action HandleUniqueActionChance(Character character, Action selectedAction)
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
                        int randomIndex = availableUniqueActions.Count > 1 ? Dice.Roll(1, availableUniqueActions.Count) - 1 : 0;
                        selectedAction = availableUniqueActions[randomIndex];
                        // Use ColoredText for unique action message
                        var uniqueBuilder = new ColoredTextBuilder();
                        uniqueBuilder.Add(character.Name, ColorPalette.Player);
                        uniqueBuilder.Add(" channels unique power and uses ", Colors.White);
                        uniqueBuilder.Add(selectedAction.Name, ColorPalette.Warning);
                        uniqueBuilder.Add("!", Colors.White);
                        TextDisplayIntegration.DisplayCombatAction(uniqueBuilder.Build(), new List<ColoredText>(), null, null);
                    }
                }
            }
            return selectedAction;
        }

        /// <summary>
        /// Creates and adds a BattleEvent to the current battle narrative
        /// </summary>
        /// <param name="source">The entity performing the action</param>
        /// <param name="target">The target entity</param>
        /// <param name="action">The action being performed</param>
        /// <param name="damage">Damage dealt (0 for non-damage actions)</param>
        /// <param name="totalRoll">Total roll value</param>
        /// <param name="rollBonus">Roll bonus applied</param>
        /// <param name="isSuccess">Whether the action was successful</param>
        /// <param name="isCombo">Whether this was a combo action</param>
        /// <param name="comboStep">Current combo step</param>
        /// <param name="healAmount">Healing amount (0 for non-healing actions)</param>
        /// <param name="isCritical">Whether this was a critical hit</param>
        /// <param name="battleNarrative">The battle narrative to add the event to</param>
        public static void CreateAndAddBattleEvent(Actor source, Actor target, Action action, int damage, int totalRoll, int rollBonus, bool isSuccess, bool isCombo, int comboStep, int healAmount, bool isCritical, int naturalRoll, BattleNarrative? battleNarrative)
        {
            try
            {
                if (battleNarrative == null)
                {
                    return; // No active battle narrative
                }

                // Create the battle event
                var battleEvent = new BattleEvent
                {
                    Actor = source.Name,
                    Target = target.Name,
                    Action = action.Name,
                    Damage = damage,
                    IsSuccess = isSuccess,
                    IsCombo = isCombo,
                    ComboStep = comboStep,
                    IsHeal = healAmount > 0,
                    HealAmount = healAmount,
                    Roll = totalRoll - rollBonus, // Base roll without bonuses
                    NaturalRoll = naturalRoll, // Natural dice roll (1-20) before any modifications
                    Difficulty = 0, // Action doesn't have Difficulty property, use 0
                    IsCritical = isCritical,
                    ActorHealthBefore = GetEntityHealth(source),
                    TargetHealthBefore = GetEntityHealth(target),
                    ActorHealthAfter = GetEntityHealth(source),
                    TargetHealthAfter = GetEntityHealth(target) - damage + healAmount
                };

                // Add the event to the narrative
                battleNarrative.AddEvent(battleEvent);
            }
            catch (Exception)
            {
                // Log error but don't break combat
                if (!ActionExecutor.DisableCombatDebugOutput)
                {
                }
            }
        }
    }
}

