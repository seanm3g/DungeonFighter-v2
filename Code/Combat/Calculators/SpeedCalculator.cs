using System;

namespace RPGGame.Combat.Calculators
{
    /// <summary>
    /// Handles attack speed calculations for characters and enemies
    /// </summary>
    public static class SpeedCalculator
    {
        /// <summary>
        /// Calculates attack speed for any Actor (shared logic)
        /// </summary>
        /// <param name="actor">The Actor to calculate attack speed for</param>
        /// <returns>Attack speed in seconds</returns>
        public static double CalculateAttackSpeed(Actor actor)
        {
            var tuning = GameConfiguration.Instance;
            double baseAttackTime = tuning.Combat.BaseAttackTime;
            
            // Agility reduces attack time (makes you faster)
            double agilityReduction = 0;
            if (actor is Character character)
            {
                agilityReduction = character.Agility * tuning.Combat.AgilitySpeedReduction;
            }
            else if (actor is Enemy enemy)
            {
                agilityReduction = enemy.Agility * tuning.Combat.AgilitySpeedReduction;
            }
            double agilityAdjustedTime = baseAttackTime - agilityReduction;
            
            // Apply Actor-specific modifiers
            if (actor is Character charEntity)
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
            else if (actor is Enemy enemyEntity)
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
            
            // Fallback for other Actor types
            return Math.Max(tuning.Combat.MinimumAttackTime, agilityAdjustedTime);
        }
    }
}

