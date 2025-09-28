using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Handles combat status effects including poison, burn, stun, weaken, and bleed
    /// </summary>
    public static class CombatEffects
    {
        /// <summary>
        /// Applies status effects from an action to the target
        /// </summary>
        /// <param name="action">The action being performed</param>
        /// <param name="attacker">The attacking entity</param>
        /// <param name="target">The target entity</param>
        /// <param name="results">List to add effect messages to</param>
        /// <returns>True if any effects were applied</returns>
        public static bool ApplyStatusEffects(Action action, Entity attacker, Entity target, List<string> results)
        {
            bool effectsApplied = false;
            
            // Apply bleed effect
            if (action.CausesBleed && CombatCalculator.CalculateStatusEffectChance(action, attacker, target))
            {
                target.IsBleeding = true;
                results.Add($"[{target.Name}] is bleeding!");
                effectsApplied = true;
            }
            
            // Apply weaken effect
            if (action.CausesWeaken && CombatCalculator.CalculateStatusEffectChance(action, attacker, target))
            {
                target.ApplyWeaken(2); // 2 turns of weaken
                results.Add($"[{target.Name}] is weakened!");
                effectsApplied = true;
            }
            
            // Apply slow effect (simplified - just set a flag)
            if (action.CausesSlow && CombatCalculator.CalculateStatusEffectChance(action, attacker, target))
            {
                // For now, just add a message - would need proper slow implementation
                results.Add($"[{target.Name}] is slowed!");
                effectsApplied = true;
            }
            
            // Apply poison effect
            if (action.CausesPoison && CombatCalculator.CalculateStatusEffectChance(action, attacker, target))
            {
                target.ApplyPoison(4, 1); // 4 damage, 1 stack
                results.Add($"[{target.Name}] is poisoned!");
                effectsApplied = true;
            }
            
            // Apply stun effect
            if (action.CausesStun && CombatCalculator.CalculateStatusEffectChance(action, attacker, target))
            {
                target.IsStunned = true;
                target.StunTurnsRemaining = 1;
                results.Add($"[{target.Name}] is stunned!");
                effectsApplied = true;
            }
            
            return effectsApplied;
        }

        /// <summary>
        /// Processes all active status effects for an entity at the start of their turn
        /// </summary>
        /// <param name="entity">The entity to process effects for</param>
        /// <param name="results">List to add effect messages to</param>
        /// <returns>Total damage dealt by effects</returns>
        public static int ProcessStatusEffects(Entity entity, List<string> results)
        {
            int totalEffectDamage = 0;
            
            // Process poison damage
            if (entity.PoisonStacks > 0)
            {
                int poisonDamage = entity.ProcessPoison(1.0);
                if (poisonDamage > 0)
                {
                    totalEffectDamage += poisonDamage;
                    results.Add($"[{entity.Name}] takes {poisonDamage} poison damage!");
                }
            }
            
            // Process burn damage
            if (entity.BurnStacks > 0)
            {
                int burnDamage = entity.ProcessBurn(1.0);
                if (burnDamage > 0)
                {
                    totalEffectDamage += burnDamage;
                    results.Add($"[{entity.Name}] takes {burnDamage} burn damage!");
                }
            }
            
            // Process bleed damage (simplified)
            if (entity.IsBleeding)
            {
                int bleedDamage = 2; // Simple bleed damage
                totalEffectDamage += bleedDamage;
                results.Add($"[{entity.Name}] takes {bleedDamage} bleed damage!");
            }
            
            return totalEffectDamage;
        }

        /// <summary>
        /// Checks if an entity can act based on status effects
        /// </summary>
        /// <param name="entity">The entity to check</param>
        /// <param name="results">List to add effect messages to</param>
        /// <returns>True if the entity can act, false if stunned or otherwise incapacitated</returns>
        public static bool CanEntityAct(Entity entity, List<string> results)
        {
            // Check for stun effect
            if (entity.IsStunned)
            {
                results.Add($"[{entity.Name}] is stunned and cannot act!");
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// Applies environmental debuffs to entities
        /// </summary>
        /// <param name="source">The source of the debuff (usually environment)</param>
        /// <param name="target">The target entity</param>
        /// <param name="action">The action causing the debuff</param>
        /// <param name="debuffType">Type of debuff to apply</param>
        /// <param name="results">List to add effect messages to</param>
        /// <returns>True if debuff was applied</returns>
        public static bool ApplyEnvironmentalDebuff(Entity source, Entity target, Action action, string debuffType, List<string> results)
        {
            // Apply the appropriate debuff
            switch (debuffType.ToLower())
            {
                case "poison":
                    if (action.CausesPoison)
                    {
                        target.ApplyPoison(3, 1);
                        results.Add($"[{target.Name}] is poisoned by the environment!");
                        return true;
                    }
                    break;
                    
                case "slow":
                    if (action.CausesSlow)
                    {
                        results.Add($"[{target.Name}] is slowed by the environment!");
                        return true;
                    }
                    break;
                    
                case "weaken":
                    if (action.CausesWeaken)
                    {
                        target.ApplyWeaken(2);
                        results.Add($"[{target.Name}] is weakened by the environment!");
                        return true;
                    }
                    break;
                    
                case "stun":
                    if (action.CausesStun)
                    {
                        target.IsStunned = true;
                        target.StunTurnsRemaining = 1;
                        results.Add($"[{target.Name}] is stunned by the environment!");
                        return true;
                    }
                    break;
            }
            
            return false;
        }

        /// <summary>
        /// Checks and applies bleed chance for critical hits
        /// </summary>
        /// <param name="attacker">The attacking character</param>
        /// <param name="target">The target entity</param>
        /// <param name="results">List to add effect messages to</param>
        public static void CheckAndApplyBleedChance(Character attacker, Entity target, List<string> results)
        {
            // For now, use a simple bleed chance calculation
            // This would need to be implemented based on equipment/modifications
            double bleedChance = 0.1; // 10% base bleed chance
            
            if (bleedChance > 0.0)
            {
                double roll = Dice.Roll(1, 100) / 100.0;
                if (roll < bleedChance)
                {
                    target.IsBleeding = true;
                    results.Add($"[{target.Name}] starts bleeding from the wound!");
                }
            }
        }

        /// <summary>
        /// Gets the effect type description for an action
        /// </summary>
        /// <param name="action">The action to describe</param>
        /// <returns>Description of the effect type</returns>
        public static string GetEffectType(Action action)
        {
            if (action.CausesBleed) return "Bleeding";
            if (action.CausesWeaken) return "Weakening";
            if (action.CausesSlow) return "Slowing";
            if (action.CausesPoison) return "Poisoning";
            if (action.CausesStun) return "Stunning";
            if (action.CausesBurn) return "Burning";
            return "Damage";
        }

        /// <summary>
        /// Calculates effect duration based on attacker's stats
        /// </summary>
        /// <param name="baseDuration">Base duration of the effect</param>
        /// <param name="attacker">The attacking entity</param>
        /// <param name="target">The target entity</param>
        /// <returns>Modified duration</returns>
        public static int CalculateEffectDuration(int baseDuration, Entity attacker, Entity target)
        {
            // Intelligence increases effect duration
            int intelligenceBonus = 0;
            if (attacker is Character attackerCharacter)
            {
                intelligenceBonus = attackerCharacter.GetEffectiveIntelligence() / 10; // +1 turn per 10 intelligence
            }
            
            // Target's intelligence provides resistance
            int resistance = 0;
            if (target is Character targetCharacter)
            {
                resistance = targetCharacter.GetEffectiveIntelligence() / 15; // -1 turn per 15 intelligence
            }
            
            int finalDuration = baseDuration + intelligenceBonus - resistance;
            return Math.Max(1, finalDuration); // Minimum 1 turn
        }

        /// <summary>
        /// Clears all temporary effects from an entity
        /// </summary>
        /// <param name="entity">The entity to clear effects from</param>
        /// <param name="results">List to add effect messages to</param>
        public static void ClearAllTemporaryEffects(Entity entity, List<string> results)
        {
            bool hadEffects = entity.PoisonStacks > 0 || entity.BurnStacks > 0 || entity.IsBleeding || 
                             entity.IsStunned || entity.IsWeakened;
            
            if (hadEffects)
            {
                entity.ClearAllTempEffects();
                results.Add($"[{entity.Name}]'s temporary effects are cleared!");
            }
        }
    }
}
