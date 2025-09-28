using System;

namespace RPGGame
{
    /// <summary>
    /// Handles combat calculations including damage, hit/miss logic, and roll bonuses
    /// </summary>
    public static class CombatCalculator
    {
        /// <summary>
        /// Calculates damage dealt by an attacker to a target
        /// </summary>
        /// <param name="attacker">The entity dealing damage</param>
        /// <param name="target">The entity receiving damage</param>
        /// <param name="action">The action being performed</param>
        /// <param name="comboAmplifier">Combo amplification multiplier</param>
        /// <param name="damageMultiplier">Additional damage multiplier</param>
        /// <param name="rollBonus">Roll bonus for the attack</param>
        /// <param name="roll">The actual roll result</param>
        /// <param name="showWeakenedMessage">Whether to show weakened damage message</param>
        /// <returns>The calculated damage amount</returns>
        public static int CalculateDamage(Entity attacker, Entity target, Action? action = null, double comboAmplifier = 1.0, double damageMultiplier = 1.0, int rollBonus = 0, int roll = 0, bool showWeakenedMessage = true)
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
                // For enemies, use their base damage
                baseDamage = enemy.GetEffectiveStrength();
            }
            
            // Apply action damage multiplier if action is provided
            double actionMultiplier = action?.DamageMultiplier ?? 1.0;
            
            // Calculate total damage before armor
            double totalDamage = (baseDamage * actionMultiplier * comboAmplifier * damageMultiplier);
            
            // Apply roll-based damage scaling
            if (roll > 0)
            {
                // Critical hit on natural 20
                if (roll == 20)
                {
                    totalDamage *= 2.0; // Double damage on critical hit
                }
                // Enhanced damage scaling based on roll
                else if (roll >= 15)
                {
                    totalDamage *= 1.5; // 50% bonus for high rolls
                }
                else if (roll >= 10)
                {
                    totalDamage *= 1.25; // 25% bonus for medium rolls
                }
                // Low rolls (1-9) get no bonus
            }
            
            // Get target's armor
            int targetArmor = 0;
            if (target is Character targetCharacter)
            {
                targetArmor = targetCharacter.GetTotalArmor();
            }
            else if (target is Enemy targetEnemy)
            {
                targetArmor = targetEnemy.GetTotalArmor();
            }
            
            // Calculate final damage after armor reduction
            int finalDamage = Math.Max(1, (int)totalDamage - targetArmor);
            
            // Apply weakened effect if target is weakened
            if (target.IsWeakened && showWeakenedMessage)
            {
                finalDamage = (int)(finalDamage * 1.5); // 50% more damage to weakened targets
            }
            
            return finalDamage;
        }

        /// <summary>
        /// Calculates hit/miss based on attack roll vs target's defense
        /// </summary>
        /// <param name="attacker">The attacking entity</param>
        /// <param name="target">The target entity</param>
        /// <param name="rollBonus">Roll bonus for the attack</param>
        /// <param name="roll">The attack roll result</param>
        /// <returns>True if the attack hits, false if it misses</returns>
        public static bool CalculateHit(Entity attacker, Entity target, int rollBonus, int roll)
        {
            // Calculate attack roll
            int attackRoll = roll + rollBonus;
            
            // Get target's defense (armor + agility bonus)
            int targetDefense = 0;
            if (target is Character targetCharacter)
            {
                targetDefense = targetCharacter.GetTotalArmor() + (targetCharacter.GetEffectiveAgility() / 2);
            }
            else if (target is Enemy targetEnemy)
            {
                targetDefense = targetEnemy.GetTotalArmor() + (targetEnemy.GetEffectiveAgility() / 2);
            }
            
            // Hit if attack roll meets or exceeds defense
            return attackRoll >= targetDefense;
        }

        /// <summary>
        /// Calculates total roll bonus for an attack
        /// </summary>
        /// <param name="attacker">The attacking entity</param>
        /// <param name="action">The action being performed</param>
        /// <param name="comboActions">Current combo actions for scaling</param>
        /// <param name="comboStep">Current combo step</param>
        /// <returns>Total roll bonus</returns>
        public static int CalculateRollBonus(Entity attacker, Action? action, List<Action> comboActions, int comboStep)
        {
            int totalBonus = 0;
            
            // Base action roll bonus
            if (action != null)
            {
                totalBonus += action.RollBonus;
                
                // Apply combo scaling bonuses
                if (action.Tags.Contains("comboScaling"))
                {
                    // Scale with combo length (METEOR, SHADOW STRIKE)
                    totalBonus += comboActions.Count;
                }
                else if (action.Tags.Contains("comboStepScaling"))
                {
                    // Scale with combo step position (HEROIC STRIKE)
                    totalBonus += (comboStep % comboActions.Count) + 1;
                }
                else                 if (action.Tags.Contains("comboAmplificationScaling"))
                {
                    // Scale with combo amplification (BERSERKER RAGE)
                    if (attacker is Character characterAttacker)
                    {
                        totalBonus += (int)(characterAttacker.GetComboAmplifier() * 2);
                    }
                }
            }
            
            // Add intelligence bonus
            if (attacker is Character character)
            {
                totalBonus += character.GetIntelligenceRollBonus();
                totalBonus += character.GetModificationRollBonus();
                totalBonus += character.GetEquipmentRollBonus();
            }
            
            // Apply roll penalty (for effects like Dust Cloud)
            totalBonus -= attacker.RollPenalty;
            
            return totalBonus;
        }

        /// <summary>
        /// Calculates critical hit chance and damage
        /// </summary>
        /// <param name="attacker">The attacking entity</param>
        /// <param name="roll">The attack roll</param>
        /// <returns>True if critical hit, false otherwise</returns>
        public static bool IsCriticalHit(Entity attacker, int roll)
        {
            // Natural 20 is always a critical hit
            if (roll == 20)
            {
                return true;
            }
            
            // Check for critical hit chance from equipment/modifications
            double criticalChance = 0.0;
            if (attacker is Character character)
            {
                // For now, use a simple critical hit chance calculation
                // This would need to be implemented based on equipment/modifications
                criticalChance = 0.05; // 5% base critical hit chance
            }
            
            // Roll for critical hit chance
            if (criticalChance > 0.0)
            {
                double rollValue = Dice.Roll(1, 100) / 100.0;
                return rollValue < criticalChance;
            }
            
            return false;
        }

        /// <summary>
        /// Calculates damage reduction from armor and other sources
        /// </summary>
        /// <param name="target">The target entity</param>
        /// <param name="damage">The incoming damage</param>
        /// <returns>The damage after reduction</returns>
        public static int ApplyDamageReduction(Entity target, int damage)
        {
            // Get base armor reduction
            int armorReduction = 0;
            if (target is Character targetCharacter)
            {
                armorReduction = targetCharacter.GetTotalArmor();
            }
            else if (target is Enemy targetEnemy)
            {
                armorReduction = targetEnemy.GetTotalArmor();
            }
            
            // Apply damage reduction from effects
            double damageReductionMultiplier = 1.0;
            if (target.DamageReduction > 0)
            {
                damageReductionMultiplier = 1.0 - (target.DamageReduction / 100.0);
            }
            
            // Calculate final damage
            int finalDamage = Math.Max(1, (int)((damage - armorReduction) * damageReductionMultiplier));
            
            return finalDamage;
        }

        /// <summary>
        /// Calculates status effect application chance
        /// </summary>
        /// <param name="action">The action being performed</param>
        /// <param name="attacker">The attacking entity</param>
        /// <param name="target">The target entity</param>
        /// <returns>True if status effect should be applied</returns>
        public static bool CalculateStatusEffectChance(Action action, Entity attacker, Entity target)
        {
            // Base chance from action
            double baseChance = 0.0;
            
            if (action.CausesBleed) baseChance = 0.3; // 30% chance
            else if (action.CausesWeaken) baseChance = 0.25; // 25% chance
            else if (action.CausesSlow) baseChance = 0.2; // 20% chance
            else if (action.CausesPoison) baseChance = 0.35; // 35% chance
            else if (action.CausesStun) baseChance = 0.15; // 15% chance
            
            // Modify chance based on attacker's intelligence
            double intelligenceBonus = 0.0;
            if (attacker is Character attackerCharacter)
            {
                intelligenceBonus = attackerCharacter.GetEffectiveIntelligence() * 0.01; // 1% per intelligence point
            }
            baseChance += intelligenceBonus;
            
            // Modify chance based on target's resistance
            double resistancePenalty = 0.0;
            if (target is Character targetCharacter)
            {
                resistancePenalty = targetCharacter.GetEffectiveIntelligence() * 0.005; // 0.5% resistance per intelligence point
            }
            baseChance -= resistancePenalty;
            
            // Ensure chance is within valid range
            baseChance = Math.Max(0.0, Math.Min(1.0, baseChance));
            
            // Roll for effect application
            double roll = Dice.Roll(1, 100) / 100.0;
            return roll < baseChance;
        }

    }
}
