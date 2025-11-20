using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Simplified combat effects handler using the effect registry pattern
    /// Replaces the original CombatEffects.cs with cleaner, more maintainable code
    /// </summary>
    public static class CombatEffectsSimplified
    {
        private static readonly EffectHandlerRegistry _effectRegistry = new EffectHandlerRegistry();

        /// <summary>
        /// Applies status effects from an action to the target
        /// </summary>
        /// <param name="action">The action being performed</param>
        /// <param name="attacker">The attacking Actor</param>
        /// <param name="target">The target Actor</param>
        /// <param name="results">List to add effect messages to</param>
        /// <returns>True if any effects were applied</returns>
        public static bool ApplyStatusEffects(Action action, Actor attacker, Actor target, List<string> results)
        {
            bool effectsApplied = false;
            var effectTypes = GetEffectTypesFromAction(action);
            
            foreach (var effectType in effectTypes)
            {
                if (_effectRegistry.ApplyEffect(effectType, target, action, results))
                {
                    effectsApplied = true;
                }
            }
            
            // If any effects were applied, append the status message to the last result
            if (effectsApplied && results.Count > 0)
            {
                // Effects are already added to results by individual handlers
            }
            
            return effectsApplied;
        }

        /// <summary>
        /// Gets all effect types that an action can cause
        /// </summary>
        private static List<string> GetEffectTypesFromAction(Action action)
        {
            var effects = new List<string>();
            
            if (action.CausesBleed) effects.Add("bleed");
            if (action.CausesWeaken) effects.Add("weaken");
            if (action.CausesSlow) effects.Add("slow");
            if (action.CausesPoison) effects.Add("poison");
            if (action.CausesStun) effects.Add("stun");
            if (action.CausesBurn) effects.Add("burn");
            
            return effects;
        }

        /// <summary>
        /// Processes all active status effects for an Actor at the start of their turn
        /// </summary>
        /// <param name="Actor">The Actor to process effects for</param>
        /// <param name="results">List to add effect messages to</param>
        /// <returns>Total damage dealt by effects</returns>
        public static int ProcessStatusEffects(Actor Actor, List<string> results)
        {
            int totalEffectDamage = 0;
            double currentTime = GameTicker.Instance.GetCurrentGameTime();
            
            // Process poison damage
            if (Actor.PoisonStacks > 0)
            {
                int poisonDamage = Actor.ProcessPoison(currentTime);
                if (poisonDamage > 0)
                {
                    totalEffectDamage += poisonDamage;
                    string damageType = Actor.GetDamageTypeText();
                    results.Add($"[{Actor.Name}] takes {poisonDamage} {damageType} damage");
                }
                
                // Check if effect ended (regardless of whether damage was dealt)
                if (Actor.PoisonStacks > 0)
                {
                    string damageType = Actor.GetDamageTypeText();
                    results.Add($"    ({damageType}: {Actor.PoisonStacks} stacks remain)");
                }
                else
                {
                    string damageType = Actor.GetDamageTypeText();
                    string effectEndMessage = damageType == "bleed" ? "bleeding" : "poisoned";
                    results.Add($"    ([{Actor.Name}] is no longer {effectEndMessage}!)");
                }
            }
            
            // Process burn damage
            if (Actor.BurnStacks > 0)
            {
                int burnDamage = Actor.ProcessBurn(currentTime);
                if (burnDamage > 0)
                {
                    totalEffectDamage += burnDamage;
                    results.Add($"[{Actor.Name}] takes {burnDamage} burn damage");
                }
                
                // Check if effect ended (regardless of whether damage was dealt)
                if (Actor.BurnStacks > 0)
                {
                    results.Add($"    (burn: {Actor.BurnStacks} stacks remain)");
                }
                else
                {
                    results.Add($"    ([{Actor.Name}] is no longer burning!)");
                }
            }
            
            return totalEffectDamage;
        }

        /// <summary>
        /// Checks if an Actor can act based on status effects
        /// </summary>
        /// <param name="Actor">The Actor to check</param>
        /// <param name="results">List to add effect messages to</param>
        /// <returns>True if the Actor can act, false if stunned or otherwise incapacitated</returns>
        public static bool CanEntityAct(Actor Actor, List<string> results)
        {
            // Check for stun effect
            if (Actor.IsStunned)
            {
                results.Add($"[{Actor.Name}] is stunned and cannot act!");
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// Applies environmental debuffs to entities using the effect registry
        /// </summary>
        /// <param name="source">The source of the debuff (usually environment)</param>
        /// <param name="target">The target Actor</param>
        /// <param name="action">The action causing the debuff</param>
        /// <param name="debuffType">Type of debuff to apply</param>
        /// <param name="results">List to add effect messages to</param>
        /// <returns>True if debuff was applied</returns>
        public static bool ApplyEnvironmentalDebuff(Actor source, Actor target, Action action, string debuffType, List<string> results)
        {
            // Use the effect registry to apply environmental effects
            bool applied = _effectRegistry.ApplyEffect(debuffType, target, action, results);
            
            if (applied)
            {
                // Modify the result message to indicate environmental source
                if (results.Count > 0)
                {
                    string lastResult = results[results.Count - 1];
                    results[results.Count - 1] = lastResult.Replace($"[{target.Name}]", $"[{target.Name}]").Replace("!", " by the environment!");
                }
            }
            
            return applied;
        }

        /// <summary>
        /// Checks and applies bleed chance for critical hits
        /// </summary>
        /// <param name="attacker">The attacking character</param>
        /// <param name="target">The target Actor</param>
        /// <param name="results">List to add effect messages to</param>
        public static void CheckAndApplyBleedChance(Character attacker, Actor target, List<string> results)
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
        /// Gets the effect type description for an action using the registry
        /// </summary>
        /// <param name="action">The action to describe</param>
        /// <returns>Description of the effect type</returns>
        public static string GetEffectType(Action action)
        {
            return _effectRegistry.GetEffectType(action);
        }

        /// <summary>
        /// Calculates effect duration based on attacker's stats
        /// </summary>
        /// <param name="baseDuration">Base duration of the effect</param>
        /// <param name="attacker">The attacking Actor</param>
        /// <param name="target">The target Actor</param>
        /// <returns>Modified duration</returns>
        public static int CalculateEffectDuration(int baseDuration, Actor attacker, Actor target)
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
        /// Clears all temporary effects from an Actor
        /// </summary>
        /// <param name="Actor">The Actor to clear effects from</param>
        /// <param name="results">List to add effect messages to</param>
        public static void ClearAllTemporaryEffects(Actor Actor, List<string> results)
        {
            bool hadEffects = Actor.PoisonStacks > 0 || Actor.BurnStacks > 0 || Actor.IsBleeding || 
                             Actor.IsStunned || Actor.IsWeakened;
            
            if (hadEffects)
            {
                Actor.ClearAllTempEffects();
                results.Add($"[{Actor.Name}]'s temporary effects are cleared!");
            }
        }
    }
}


