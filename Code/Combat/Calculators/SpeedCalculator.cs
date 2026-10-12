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
            m = 1.0 + (m - 1.0) * Math.Max(0.0, combat.WeaponSpeedSpread);
            double clampMin = combat.WeaponAttackTimeClampMin > 0 ? combat.WeaponAttackTimeClampMin : 0.5;
            double clampMax = combat.WeaponAttackTimeClampMax > clampMin ? combat.WeaponAttackTimeClampMax : 1.5;
            return Math.Max(clampMin, Math.Min(clampMax, m));
        }

        /// <summary>
        /// Agility time multiplier: <see cref="CombatConfig.AgilityMinSpeedMultiplier"/> at <see cref="CombatConfig.AgilityMin"/>
        /// down to <see cref="CombatConfig.AgilityMaxSpeedMultiplier"/> at <see cref="CombatConfig.AgilityMax"/>, shaped by
        /// <see cref="CombatConfig.AgilitySpeedCurveExponent"/>.
        /// </summary>
        public static double GetAgilitySpeedMultiplier(int agility)
        {
            var combat = GameConfiguration.Instance.Combat;
            int agilityMin = combat.AgilityMin;
            int agilityMax = combat.AgilityMax;
            agility = Math.Max(agilityMin, Math.Min(agilityMax, agility));
            double agilityRange = agilityMax - agilityMin;
            double normalizedProgress = agilityRange > 0 ? (agility - agilityMin) / agilityRange : 0.0;
            double exponent = combat.AgilitySpeedCurveExponent > 0 ? combat.AgilitySpeedCurveExponent : 0.5;
            double curvedProgress = Math.Pow(normalizedProgress, exponent);
            return combat.AgilityMinSpeedMultiplier + (combat.AgilityMaxSpeedMultiplier - combat.AgilityMinSpeedMultiplier) * curvedProgress;
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
            if (actor is Enemy { Identity: { } identity })
                agilityAdjustedTime = baseAttackTime * GetAgilitySpeedMultiplier(identity.ReferenceAgility);
            else if (actor is Character agilityActor)
                agilityAdjustedTime = baseAttackTime * GetAgilitySpeedMultiplier(agilityActor.GetEffectiveAgility());

            // Apply Actor-specific modifiers
            // Enemy : Character, so enemies must be excluded here or the archetype speed branch below never runs.
            if (actor is Character charEntity && actor is not Enemy)
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
                    finalAttackTime /= Math.Max(0.1, charEntity.SlowMultiplier);
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

                // Animal ladder general bonus: % faster (shorter swing time).
                int animalSpeedPct = AnimalSetController.GetGeneralSpeedPct(charEntity);
                if (animalSpeedPct > 0)
                    finalAttackTime *= Math.Max(0.1, 1.0 - animalSpeedPct / 100.0);
                
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
                double finalAttackTime = weaponAdjustedTime * (enemyEntity.Identity?.AttackTime
                    ?? ArchetypeManager.GetArchetypeAttackTimeMultiplier(enemyEntity.AttackProfile));
                
                // Apply slow debuff if active
                if (enemyEntity.SlowTurns > 0)
                {
                    finalAttackTime /= Math.Max(0.1, enemyEntity.SlowMultiplier);
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

