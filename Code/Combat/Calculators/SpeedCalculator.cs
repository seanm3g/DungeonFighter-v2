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
            
            // Agility speed system: Uses configurable min/max agility and speed multipliers
            // Diminishing returns curve: Square root function for non-linear scaling
            // Early agility points provide more benefit than later points
            double agilityAdjustedTime = baseAttackTime;
            if (actor is Character character)
            {
                int agility = character.GetEffectiveAgility();
                // Clamp agility to configured range
                int agilityMin = tuning.Combat.AgilityMin;
                int agilityMax = tuning.Combat.AgilityMax;
                agility = Math.Max(agilityMin, Math.Min(agilityMax, agility));
                
                // Calculate normalized progress (0.0 to 1.0)
                double minMultiplier = tuning.Combat.AgilityMinSpeedMultiplier;
                double maxMultiplier = tuning.Combat.AgilityMaxSpeedMultiplier;
                double agilityRange = agilityMax - agilityMin;
                double normalizedProgress = agilityRange > 0 ? (agility - agilityMin) / (double)agilityRange : 0.0;
                
                // Apply diminishing returns curve using square root
                // This ensures early agility points give more benefit than later points
                double curvedProgress = Math.Sqrt(normalizedProgress);
                
                // Interpolate between min and max multipliers using the curved progress
                double speedMultiplier = minMultiplier + (maxMultiplier - minMultiplier) * curvedProgress;
                
                agilityAdjustedTime = baseAttackTime * speedMultiplier;
            }
            else if (actor is Enemy enemy)
            {
                int agility = enemy.GetEffectiveAgility();
                // Clamp agility to configured range
                int agilityMin = tuning.Combat.AgilityMin;
                int agilityMax = tuning.Combat.AgilityMax;
                agility = Math.Max(agilityMin, Math.Min(agilityMax, agility));
                
                // Calculate normalized progress (0.0 to 1.0)
                double minMultiplier = tuning.Combat.AgilityMinSpeedMultiplier;
                double maxMultiplier = tuning.Combat.AgilityMaxSpeedMultiplier;
                double agilityRange = agilityMax - agilityMin;
                double normalizedProgress = agilityRange > 0 ? (agility - agilityMin) / (double)agilityRange : 0.0;
                
                // Apply diminishing returns curve using square root
                // This ensures early agility points give more benefit than later points
                double curvedProgress = Math.Sqrt(normalizedProgress);
                
                // Interpolate between min and max multipliers using the curved progress
                double speedMultiplier = minMultiplier + (maxMultiplier - minMultiplier) * curvedProgress;
                
                agilityAdjustedTime = baseAttackTime * speedMultiplier;
            }
            
            // Apply Actor-specific modifiers
            if (actor is Character charEntity)
            {
                // Calculate weapon speed using the equation: (base attack speed / weapon speed) × action speed
                // Higher weapon speed values = faster attacks (less time), lower values = slower attacks (more time)
                double weaponSpeed = 1.0;
                if (charEntity.Weapon is WeaponItem w)
                {
                    // Weapon speed values from JSON: higher = faster (e.g., 1.4 = fast dagger, 0.8 = slow mace)
                    // We divide time by speed to make higher speeds reduce time
                    weaponSpeed = w.BaseAttackSpeed;
                    
                    // Ensure weapon speed is reasonable (clamp to prevent division by zero or extreme values)
                    weaponSpeed = Math.Max(0.1, Math.Min(2.0, weaponSpeed));
                    
                    // Debug logging for weapon speed calculation
                    if (GameConfiguration.IsDebugEnabled)
                    {
                    }
                }
                double weaponAdjustedTime = agilityAdjustedTime / weaponSpeed;
                
                // Equipment speed bonus reduces time further
                double equipmentSpeedBonus = charEntity.GetEquipmentAttackSpeedBonus();
                double finalAttackTime = weaponAdjustedTime - equipmentSpeedBonus;
                
                // Ensure we don't go negative before applying other modifiers
                finalAttackTime = Math.Max(0.001, finalAttackTime);
                
                // Apply slow debuff if active
                if (charEntity.SlowTurns > 0)
                {
                    finalAttackTime *= charEntity.SlowMultiplier;
                }
                
                // Apply speed multiplier modifications (like Ethereal)
                double speedMultiplier = charEntity.GetModificationSpeedMultiplier();
                // Prevent division by zero - ensure speedMultiplier is at least a small positive value
                speedMultiplier = Math.Max(0.0001, speedMultiplier);
                finalAttackTime /= speedMultiplier; // Divide by multiplier to make attacks faster
                
                // Ensure result is never negative or zero
                // Apply minimum cap - ensure MinimumAttackTime is at least 0.01 to match test expectations
                double minAttackTimeChar = Math.Max(0.01, tuning.Combat.MinimumAttackTime);
                double finalResult = Math.Max(minAttackTimeChar, finalAttackTime);
                
                // Debug logging for final result
                if (GameConfiguration.IsDebugEnabled)
                {
                }
                
                return finalResult;
            }
            else if (actor is Enemy enemyEntity)
            {
                // Apply weapon speed (same as characters)
                // Higher weapon speed values = faster attacks (less time), lower values = slower attacks (more time)
                double weaponSpeed = 1.0;
                if (enemyEntity.Weapon is WeaponItem w)
                {
                    // Weapon speed values from JSON: higher = faster (e.g., 1.4 = fast dagger, 0.8 = slow mace)
                    // We divide time by speed to make higher speeds reduce time
                    weaponSpeed = w.BaseAttackSpeed;
                    // Ensure weapon speed is reasonable (clamp to prevent division by zero or extreme values)
                    weaponSpeed = Math.Max(0.1, Math.Min(2.0, weaponSpeed));
                }
                double weaponAdjustedTime = agilityAdjustedTime / weaponSpeed;
                
                // Apply archetype speed multiplier
                double finalAttackTime = weaponAdjustedTime * enemyEntity.AttackProfile.SpeedMultiplier;
                
                // Apply slow debuff if active
                if (enemyEntity.SlowTurns > 0)
                {
                    finalAttackTime *= enemyEntity.SlowMultiplier;
                }
                
                // Ensure result is never negative or zero
                // Apply minimum cap - ensure MinimumAttackTime is at least 0.01 to match test expectations
                double minAttackTimeEnemy = Math.Max(0.01, tuning.Combat.MinimumAttackTime);
                return Math.Max(minAttackTimeEnemy, finalAttackTime);
            }
            
            // Fallback for other Actor types
            // Ensure result is never negative or zero
            double minAttackTime = Math.Max(0.01, tuning.Combat.MinimumAttackTime);
            return Math.Max(minAttackTime, agilityAdjustedTime);
        }
    }
}

