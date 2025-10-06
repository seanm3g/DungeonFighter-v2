using System;

namespace RPGGame
{
    /// <summary>
    /// Handles combat calculations including damage, hit/miss logic, and roll bonuses
    /// </summary>
    public static class CombatCalculator
    {
        /// <summary>
        /// Calculates raw damage before armor reduction
        /// </summary>
        /// <param name="attacker">The entity dealing damage</param>
        /// <param name="action">The action being performed</param>
        /// <param name="comboAmplifier">Combo amplification multiplier</param>
        /// <param name="damageMultiplier">Additional damage multiplier</param>
        /// <param name="roll">The actual roll result</param>
        /// <returns>Raw damage before armor reduction</returns>
        public static int CalculateRawDamage(Entity attacker, Action? action = null, double comboAmplifier = 1.0, double damageMultiplier = 1.0, int roll = 0)
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
                // Critical hit on total roll of 20 or higher (baseRoll + rollBonus) - FIXED: Allow 20+
                if (roll >= combatConfig.CriticalHitThreshold)
                {
                    if (GameConfiguration.IsDebugEnabled)
                    {
                        if (!CombatActions.DisableCombatDebugOutput)
                        {
                        }
                    }
                    totalDamage *= combatBalance.CriticalHitDamageMultiplier;
                    if (GameConfiguration.IsDebugEnabled)
                    {
                        if (!CombatActions.DisableCombatDebugOutput)
                        {
                        }
                    }
                }
                // Enhanced damage scaling based on roll using RollSystem configuration
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
            
            return (int)totalDamage;
        }

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
            // Calculate raw damage before armor
            int totalDamage = CalculateRawDamage(attacker, action, comboAmplifier, damageMultiplier, roll);
            
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
            
            // Use configurable armor reduction formula if available
            if (!string.IsNullOrEmpty(combatBalance.ArmorReductionFormula))
            {
                // For now, use the existing simple formula but make it configurable
                // TODO: Implement formula evaluator for armor reduction
                finalDamage = Math.Max(GameConfiguration.Instance.Combat.MinimumDamage, (int)totalDamage - targetArmor);
            }
            else
            {
                // Fallback to simple armor reduction
                finalDamage = Math.Max(GameConfiguration.Instance.Combat.MinimumDamage, (int)totalDamage - targetArmor);
            }
            
            // Apply weakened effect if target is weakened
            if (target.IsWeakened && showWeakenedMessage)
            {
                finalDamage = (int)(finalDamage * 1.5); // 50% more damage to weakened targets
            }
            
            return finalDamage;
        }

        /// <summary>
        /// Calculates hit/miss based on roll value only
        /// 1-5: Miss, 6-13: Regular attack, 14-19: Combo, 20: Combo + Critical
        /// </summary>
        /// <param name="attacker">The attacking entity</param>
        /// <param name="target">The target entity</param>
        /// <param name="rollBonus">Roll bonus for the attack</param>
        /// <param name="roll">The attack roll result</param>
        /// <returns>True if the attack hits, false if it misses</returns>
        public static bool CalculateHit(Entity attacker, Entity target, int rollBonus, int roll)
        {
            // Hit/miss is based on roll value only, not target defense
            // 1-5: Miss, 6-20: Hit
            return roll >= 6;
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
            else if (attacker is Enemy enemy)
            {
                totalBonus += enemy.GetIntelligenceRollBonus();
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
            // Natural 20 or higher is always a critical hit - FIXED: Allow 20+
            if (roll >= 20)
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
            
            // Use configurable armor reduction formula if available
            var combatBalance = GameConfiguration.Instance.CombatBalance;
            int finalDamage;
            
            if (!string.IsNullOrEmpty(combatBalance.ArmorReductionFormula))
            {
                // For now, use the existing simple formula but make it configurable
                // TODO: Implement formula evaluator for armor reduction
                finalDamage = Math.Max(GameConfiguration.Instance.Combat.MinimumDamage, (int)((damage - armorReduction) * damageReductionMultiplier));
            }
            else
            {
                // Fallback to simple armor reduction
                finalDamage = Math.Max(GameConfiguration.Instance.Combat.MinimumDamage, (int)((damage - armorReduction) * damageReductionMultiplier));
            }
            
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
            // Check if action can cause any status effect
            if (!action.CausesBleed && !action.CausesWeaken && !action.CausesSlow && 
                !action.CausesPoison && !action.CausesStun && !action.CausesBurn)
            {
                return false; // No status effects possible
            }
            
            // Use 2d2-2 roll for status effect chance
            // 2d2 gives range 2-4, minus 2 gives range 0-2
            // 0 = no effect, 1+ = effect applied
            int roll1 = Dice.Roll(1, 2); // First d2
            int roll2 = Dice.Roll(1, 2); // Second d2
            int result = (roll1 + roll2) - 2; // 2d2-2
            
            // Effect is applied if result is 1 or 2 (66.7% chance)
            return result >= 1;
        }
        
        /// <summary>
        /// Calculates attack speed for any entity (shared logic)
        /// </summary>
        /// <param name="entity">The entity to calculate attack speed for</param>
        /// <returns>Attack speed in seconds</returns>
        public static double CalculateAttackSpeed(Entity entity)
        {
            var tuning = GameConfiguration.Instance;
            double baseAttackTime = tuning.Combat.BaseAttackTime;
            
            // Agility reduces attack time (makes you faster)
            double agilityReduction = 0;
            if (entity is Character character)
            {
                agilityReduction = character.Agility * tuning.Combat.AgilitySpeedReduction;
            }
            else if (entity is Enemy enemy)
            {
                agilityReduction = enemy.Agility * tuning.Combat.AgilitySpeedReduction;
            }
            double agilityAdjustedTime = baseAttackTime - agilityReduction;
            
            // Apply entity-specific modifiers
            if (entity is Character charEntity)
            {
                // Calculate weapon speed using the equation: (base attack speed + weapon) × action speed
                double weaponSpeedModifier = 0.0;
                if (charEntity.Weapon is WeaponItem w)
                {
                    // Weapon speed is added to base attack time, then multiplied by action length
                    // Fast weapons have negative values (speed up), slow weapons have positive values (slow down)
                    weaponSpeedModifier = w.BaseAttackSpeed;
                    
                    // Debug logging for weapon speed calculation
                    if (GameConfiguration.IsDebugEnabled)
                    {
                    }
                }
                double weaponAdjustedTime = (agilityAdjustedTime + weaponSpeedModifier);
                
                // Equipment speed bonus reduces time further
                double equipmentSpeedBonus = charEntity.GetEquipmentAttackSpeedBonus();
                double finalAttackTime = weaponAdjustedTime - equipmentSpeedBonus;
                
                // Apply slow debuff if active
                if (charEntity.SlowTurns > 0)
                {
                    finalAttackTime *= charEntity.SlowMultiplier;
                }
                
                // Apply speed multiplier modifications (like Ethereal)
                double speedMultiplier = charEntity.GetModificationSpeedMultiplier();
                finalAttackTime /= speedMultiplier; // Divide by multiplier to make attacks faster
                
                // Apply minimum cap
                double finalResult = Math.Max(tuning.Combat.MinimumAttackTime, finalAttackTime);
                
                // Debug logging for final result
                if (GameConfiguration.IsDebugEnabled)
                {
                }
                
                return finalResult;
            }
            else if (entity is Enemy enemyEntity)
            {
                // Apply weapon speed modifier (same as characters)
                double weaponSpeedModifier = 0.0;
                if (enemyEntity.Weapon is WeaponItem w)
                {
                    weaponSpeedModifier = w.BaseAttackSpeed;
                }
                double weaponAdjustedTime = (agilityAdjustedTime + weaponSpeedModifier);
                
                // Apply archetype speed multiplier
                double finalAttackTime = weaponAdjustedTime * enemyEntity.AttackProfile.SpeedMultiplier;
                
                // Apply slow debuff if active
                if (enemyEntity.SlowTurns > 0)
                {
                    finalAttackTime *= enemyEntity.SlowMultiplier;
                }
                
                // Apply minimum cap
                return Math.Max(tuning.Combat.MinimumAttackTime, finalAttackTime);
            }
            
            // Fallback for other entity types
            return Math.Max(tuning.Combat.MinimumAttackTime, agilityAdjustedTime);
        }

    }
}
