using System;
using RPGGame.Actions.RollModification;

namespace RPGGame.Combat.Calculators
{
    /// <summary>
    /// Handles damage calculations including raw damage, final damage, and damage reduction
    /// </summary>
    public static class DamageCalculator
    {
        /// <summary>
        /// Calculates raw damage before armor reduction
        /// </summary>
        public static int CalculateRawDamage(Actor attacker, Action? action = null, double comboAmplifier = 1.0, double damageMultiplier = 1.0, int roll = 0)
        {
            // Get base damage from attacker
            int baseDamage = 0;
            if (attacker is Character character)
            {
                baseDamage = character.GetEffectiveStrength();
                
                // Add weapon damage if attacker has a weapon
                if (character.Weapon is WeaponItem weapon)
                {
                    baseDamage += weapon.GetTotalDamage();
                }
                
                // Add equipment damage bonus
                baseDamage += character.GetEquipmentDamageBonus();
                
                // Add modification damage bonus
                baseDamage += character.GetModificationDamageBonus();
            }
            else if (attacker is Enemy enemy)
            {
                // For enemies, use strength + weapon damage (same as heroes)
                baseDamage = enemy.GetEffectiveStrength();
                
                // Add weapon damage if enemy has a weapon
                if (enemy.Weapon is WeaponItem weapon)
                {
                    baseDamage += weapon.GetTotalDamage();
                }
            }
            
            // Apply action damage multiplier if action is provided
            double actionMultiplier = action?.DamageMultiplier ?? 1.0;
            
            // Calculate total damage before armor
            double totalDamage = (baseDamage * actionMultiplier * comboAmplifier * damageMultiplier);
            
            // Get combat configuration
            var combatConfig = GameConfiguration.Instance.Combat;
            var combatBalance = GameConfiguration.Instance.CombatBalance;
            
            // Apply roll-based damage scaling
            if (roll > 0)
            {
                // Get threshold from threshold manager if available, otherwise use config
                int criticalThreshold = combatConfig.CriticalHitThreshold;
                if (attacker != null)
                {
                    criticalThreshold = RollModificationManager.GetThresholdManager().GetCriticalHitThreshold(attacker);
                }
                
                // Critical hit on total roll of threshold or higher - FIXED: Allow 20+
                if (roll >= criticalThreshold)
                {
                    if (GameConfiguration.IsDebugEnabled)
                    {
                        if (!ActionExecutor.DisableCombatDebugOutput)
                        {
                        }
                    }
                    totalDamage *= combatBalance.CriticalHitDamageMultiplier;
                    if (GameConfiguration.IsDebugEnabled)
                    {
                        if (!ActionExecutor.DisableCombatDebugOutput)
                        {
                        }
                    }
                }
                else
                {
                    // Enhanced damage scaling based on roll using RollSystem configuration
                    // Only apply if not a critical hit (roll < 20)
                    var rollSystem = GameConfiguration.Instance.RollSystem;
                    var rollMultipliers = combatBalance.RollDamageMultipliers;
                    if (roll >= rollSystem.ComboThreshold.Min)
                    {
                        totalDamage *= rollMultipliers.ComboRollDamageMultiplier;
                    }
                    else if (roll >= rollSystem.BasicAttackThreshold.Min)
                    {
                        totalDamage *= rollMultipliers.BasicRollDamageMultiplier;
                    }
                }
            }
            
            return (int)totalDamage;
        }

        /// <summary>
        /// Calculates damage dealt by an attacker to a target
        /// </summary>
        public static int CalculateDamage(Actor attacker, Actor target, Action? action = null, double comboAmplifier = 1.0, double damageMultiplier = 1.0, int rollBonus = 0, int roll = 0, bool showWeakenedMessage = true)
        {
            // Calculate raw damage before armor
            int totalDamage = CalculateRawDamage(attacker, action, comboAmplifier, damageMultiplier, roll);
            
            // Apply tag-based damage modification
            if (action != null)
            {
                double tagModifier = Combat.TagDamageCalculator.GetDamageModifier(action, target);
                totalDamage = (int)(totalDamage * tagModifier);
            }
            
            // Get target's armor
            int targetArmor = 0;
            if (target is Enemy targetEnemy)
            {
                // For enemies, use the Armor property directly to avoid inheritance issues
                targetArmor = targetEnemy.Armor;
            }
            else if (target is Character targetCharacter)
            {
                targetArmor = targetCharacter.GetTotalArmor();
            }
            
            // Calculate final damage after armor reduction
            int finalDamage;
            
            // Get combat configuration
            var combatBalance = GameConfiguration.Instance.CombatBalance;
            
            // Apply simple armor reduction (flat reduction)
            finalDamage = Math.Max(GameConfiguration.Instance.Combat.MinimumDamage, (int)totalDamage - targetArmor);
            
            // Apply weakened effect if target is weakened
            if (target.IsWeakened && showWeakenedMessage)
            {
                finalDamage = (int)(finalDamage * 1.5); // 50% more damage to weakened targets
            }
            
            return finalDamage;
        }

        /// <summary>
        /// Calculates damage reduction from armor and other sources
        /// </summary>
        public static int ApplyDamageReduction(Actor target, int damage)
        {
            // Get base armor reduction
            int armorReduction = 0;
            if (target is Enemy targetEnemy)
            {
                // For enemies, use the Armor property directly to avoid inheritance issues
                armorReduction = targetEnemy.Armor;
            }
            else if (target is Character targetCharacter)
            {
                armorReduction = targetCharacter.GetTotalArmor();
            }
            
            // Apply damage reduction from effects
            double damageReductionMultiplier = 1.0;
            if (target.DamageReduction > 0)
            {
                damageReductionMultiplier = 1.0 - (target.DamageReduction / 100.0);
            }
            
            // Apply simple armor reduction (flat reduction) with damage reduction multiplier
            int finalDamage = Math.Max(GameConfiguration.Instance.Combat.MinimumDamage, (int)((damage - armorReduction) * damageReductionMultiplier));
            
            return finalDamage;
        }
    }
}

