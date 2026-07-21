using System;
using RPGGame.Items;

namespace RPGGame.Combat.Calculators
{
    /// <summary>
    /// Handles attack speed calculations for characters and enemies
    /// </summary>
    public static class SpeedCalculator
    {
        /// <summary>
        /// Weapon sheet <c>attackSpeed</c> / <see cref="WeaponItem.BaseAttackSpeed"/>: 1 = baseline cadence;
        /// values above 1 lengthen swing spacing (slower); below 1 shorten it (faster), relative to agility-adjusted time.
        /// </summary>
        public static double GetWeaponAttackTimeMultiplier(WeaponItem w)
        {
            double m = w.BaseAttackSpeed;
            if (m <= 0 || double.IsNaN(m) || double.IsInfinity(m))
                m = 1.0;
            var combat = GameConfiguration.Instance.Combat;
            double clampMin = combat.WeaponAttackTimeClampMin > 0 ? combat.WeaponAttackTimeClampMin : 0.5;
            double clampMax = combat.WeaponAttackTimeClampMax > clampMin ? combat.WeaponAttackTimeClampMax : 1.5;
            return Math.Max(clampMin, Math.Min(clampMax, m));
        }

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
                // Weapon attackSpeed multiplier: ×1 baseline; >1 slower (longer time); <1 faster (shorter time).
                double weaponTimeMul = 1.0;
                if (charEntity.Weapon is WeaponItem w)
                {
                    weaponTimeMul = GetWeaponAttackTimeMultiplier(w);
                    if (GameConfiguration.IsDebugEnabled)
                    {
                    }
                }
                // Flat WEAPON_SPEED cadence points: each point lowers weapon time mult by 0.1 (same as item BonusAttackSpeed).
                if (charEntity.Effects.ConsumedWeaponSpeedFlat != 0)
                    weaponTimeMul = Math.Max(0.1, weaponTimeMul - charEntity.Effects.ConsumedWeaponSpeedFlat * 0.1);

                double weaponAdjustedTime = agilityAdjustedTime * weaponTimeMul;
                
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
                finalAttackTime /= speedMultiplier;

                if (charEntity.Weapon is WeaponItem speedWeapon)
                {
                    double classSpeed = ClassBalanceHelper.GetSpeedMultiplier(speedWeapon.WeaponType);
                    if (classSpeed > 0)
                        finalAttackTime /= classSpeed;
                }
                
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
                double weaponTimeMul = 1.0;
                if (enemyEntity.Weapon is WeaponItem w)
                    weaponTimeMul = GetWeaponAttackTimeMultiplier(w);
                if (enemyEntity.Effects.ConsumedWeaponSpeedFlat != 0)
                    weaponTimeMul = Math.Max(0.1, weaponTimeMul - enemyEntity.Effects.ConsumedWeaponSpeedFlat * 0.1);
                double weaponAdjustedTime = agilityAdjustedTime * weaponTimeMul;
                
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

