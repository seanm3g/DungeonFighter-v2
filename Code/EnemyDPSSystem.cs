using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Redesigned Enemy DPS System: Level determines target DPS, archetype distributes it
    /// </summary>
    public static class EnemyDPSSystem
    {
        /// <summary>
        /// Calculates the target DPS for an enemy at a given level
        /// </summary>
        public static double CalculateTargetDPS(int level)
        {
            var config = TuningConfig.Instance.EnemyDPS;
            if (config == null) return 3.0 + (level * 2.5); // Fallback
            
            // Use formula evaluation for flexibility
            var variables = new Dictionary<string, double>
            {
                ["BaseDPSAtLevel1"] = config.BaseDPSAtLevel1,
                ["Level"] = level,
                ["DPSPerLevel"] = config.DPSPerLevel
            };
            
            return FormulaEvaluator.Evaluate(config.DPSScalingFormula, variables);
        }
        
        /// <summary>
        /// Calculates damage and attack speed to achieve target DPS for given archetype
        /// </summary>
        public static (double damagePerHit, double attackSpeed) CalculateArchetypeStats(
            double targetDPS, EnemyArchetype archetype)
        {
            var config = TuningConfig.Instance.EnemyDPS;
            if (config == null || !config.Archetypes.TryGetValue(archetype.ToString(), out var archetypeConfig))
            {
                // Fallback to hardcoded values
                return CalculateFallbackArchetypeStats(targetDPS, archetype);
            }
            
            // Calculate attack speed and damage to achieve target DPS
            // Formula: DPS = Damage / AttackTime
            // We want: Damage * SpeedRatio = targetDPS * DamageRatio
            
            double baseAttackTime = TuningConfig.Instance.Combat.BaseAttackTime;
            double targetAttackTime = baseAttackTime / archetypeConfig.SpeedRatio;
            double targetDamage = targetDPS * targetAttackTime;
            
            // Apply damage ratio adjustment
            targetDamage *= archetypeConfig.DamageRatio;
            
            return (targetDamage, targetAttackTime);
        }
        
        private static (double damagePerHit, double attackSpeed) CalculateFallbackArchetypeStats(
            double targetDPS, EnemyArchetype archetype)
        {
            double baseAttackTime = TuningConfig.Instance.Combat.BaseAttackTime;
            
            return archetype switch
            {
                EnemyArchetype.Berserker => (targetDPS * (baseAttackTime / 1.4) * 0.71, baseAttackTime / 1.4),
                EnemyArchetype.Assassin => (targetDPS * (baseAttackTime / 1.2) * 0.83, baseAttackTime / 1.2),
                EnemyArchetype.Warrior => (targetDPS * baseAttackTime, baseAttackTime),
                EnemyArchetype.Brute => (targetDPS * (baseAttackTime / 0.75) * 1.33, baseAttackTime / 0.75),
                EnemyArchetype.Juggernaut => (targetDPS * (baseAttackTime / 0.6) * 1.67, baseAttackTime / 0.6),
                _ => (targetDPS * baseAttackTime, baseAttackTime)
            };
        }
        
        /// <summary>
        /// Calculates what an enemy's stats should be to achieve target damage and speed
        /// </summary>
        public static (int strength, int agility) CalculateRequiredStats(
            double targetDamage, double targetAttackSpeed, int level, EnemyArchetype archetype)
        {
            var tuning = TuningConfig.Instance;
            
            // Get archetype profile for accurate calculations
            var archetypeProfile = EnemyDPSCalculator.GetArchetypeProfile(archetype);
            
            // Work backwards from damage formula:
            // Final damage = (strength + strength + level*2) * archetypeMultiplier
            // So: baseDamage = targetDamage / archetypeMultiplier
            double requiredBaseDamage = targetDamage / archetypeProfile.DamageMultiplier;
            double levelBonus = level * 0; // Current level scaling in Combat.cs (no level bonus)
            double requiredStatSum = Math.Max(0, requiredBaseDamage - levelBonus);
            
            // For enemies, strength = highestAttribute, so:
            // requiredStatSum = strength + strength = 2 * strength
            int requiredStrength = Math.Max(3, (int)Math.Ceiling(requiredStatSum / 2));
            
            // Work backwards from attack speed formula:
            // finalAttackTime = (baseAttackTime - agility * agilityReduction) * archetypeSpeedMultiplier
            // So: baseAttackTime = targetAttackSpeed / archetypeSpeedMultiplier
            double baseAttackTime = tuning.Combat.BaseAttackTime;
            double agilityReduction = tuning.Combat.AgilitySpeedReduction;
            double requiredBaseTime = targetAttackSpeed / archetypeProfile.SpeedMultiplier;
            
            double agilityNeeded = (baseAttackTime - requiredBaseTime) / agilityReduction;
            int requiredAgility = Math.Max(1, (int)Math.Round(agilityNeeded));
            
            return (requiredStrength, requiredAgility);
        }
        
        /// <summary>
        /// Overload for backward compatibility
        /// </summary>
        public static (int strength, int agility) CalculateRequiredStats(
            double targetDamage, double targetAttackSpeed, int level)
        {
            return CalculateRequiredStats(targetDamage, targetAttackSpeed, level, EnemyArchetype.Warrior);
        }
        
        /// <summary>
        /// Applies DPS-based scaling to an enemy
        /// </summary>
        public static void ApplyDPSScaling(Enemy enemy)
        {
            // Calculate target DPS for this enemy's level
            double targetDPS = CalculateTargetDPS(enemy.Level);
            
            // Get archetype-specific damage and speed distribution
            var (targetDamage, targetAttackSpeed) = CalculateArchetypeStats(targetDPS, enemy.Archetype);
            
            // Calculate required stats to achieve this performance
            var (requiredStrength, requiredAgility) = CalculateRequiredStats(targetDamage, targetAttackSpeed, enemy.Level);
            
            // Apply the calculated stats (this would require modifying Enemy class)
            // For now, we'll store this in a way that the combat system can use
            enemy.SetTargetDPS(targetDPS);
            enemy.SetTargetDamage(targetDamage);
            enemy.SetTargetAttackSpeed(targetAttackSpeed);
        }
        
        /// <summary>
        /// Validates that an enemy's actual DPS matches target DPS within tolerance
        /// </summary>
        public static DPSValidationResult ValidateEnemyDPS(Enemy enemy)
        {
            double targetDPS = CalculateTargetDPS(enemy.Level);
            double actualDPS = CalculateActualEnemyDPS(enemy);
            
            var config = TuningConfig.Instance.EnemyDPS?.BalanceValidation;
            double tolerance = config?.TolerancePercentage ?? 10.0;
            
            double deviationPercent = Math.Abs(actualDPS - targetDPS) / targetDPS * 100;
            bool isWithinTolerance = deviationPercent <= tolerance;
            
            return new DPSValidationResult
            {
                EnemyName = enemy.Name ?? "Unknown",
                Level = enemy.Level,
                Archetype = enemy.Archetype,
                TargetDPS = targetDPS,
                ActualDPS = actualDPS,
                DeviationPercent = deviationPercent,
                IsWithinTolerance = isWithinTolerance,
                Recommendation = GenerateRecommendation(targetDPS, actualDPS, enemy.Archetype)
            };
        }
        
        private static double CalculateActualEnemyDPS(Enemy enemy)
        {
            // Use existing DPS calculator but with new system
            return EnemyDPSCalculator.CalculateEnemyDPS(enemy, enemy.Archetype);
        }
        
        private static string GenerateRecommendation(double targetDPS, double actualDPS, EnemyArchetype archetype)
        {
            if (Math.Abs(actualDPS - targetDPS) < 0.1) return "Perfect balance";
            
            if (actualDPS < targetDPS)
            {
                double deficit = targetDPS - actualDPS;
                return $"Increase damage by {deficit:F1} DPS (consider +STR or +level scaling)";
            }
            else
            {
                double excess = actualDPS - targetDPS;
                return $"Reduce damage by {excess:F1} DPS (consider archetype rebalancing)";
            }
        }
        
        /// <summary>
        /// Analyzes all enemies and their DPS vs targets
        /// </summary>
        public static void AnalyzeAllEnemyDPS()
        {
            UIManager.WriteSystemLine("=== DPS-BASED ENEMY ANALYSIS ===");
            UIManager.WriteSystemLine("");
            
            EnemyLoader.LoadEnemies();
            var enemyDataList = EnemyLoader.GetAllEnemyData();
            
            if (!enemyDataList.Any())
            {
                UIManager.WriteSystemLine("No enemy data found!");
                return;
            }
            
            UIManager.WriteSystemLine("Enemy\t\tLevel\tArchetype\tTarget DPS\tActual DPS\tDev%\tStatus");
            UIManager.WriteSystemLine("".PadRight(85, '='));
            
            var testLevels = new[] { 1, 5, 10, 15, 20 };
            
            foreach (var enemyData in enemyDataList.Take(6))
            {
                foreach (int level in testLevels)
                {
                    var enemy = EnemyLoader.CreateEnemy(enemyData.Name, level);
                    if (enemy == null) continue;
                    
                    var validation = ValidateEnemyDPS(enemy);
                    string status = validation.IsWithinTolerance ? "✓" : "⚠";
                    
                    UIManager.WriteSystemLine($"{enemyData.Name.PadRight(12)}\tL{level}\t{validation.Archetype.ToString().PadRight(8)}\t{validation.TargetDPS:F1}\t\t{validation.ActualDPS:F1}\t\t{validation.DeviationPercent:F0}%\t{status}");
                }
                UIManager.WriteSystemLine("");
            }
            
            UIManager.WriteSystemLine("");
            UIManager.WriteSystemLine("=== STAT SCALING COMPARISON ===");
            UIManager.WriteSystemLine("Enemy\t\tLevel\tBase STR/AGI\tCalc STR/AGI\tFinal STR/AGI");
            UIManager.WriteSystemLine("".PadRight(75, '='));
            
            foreach (var enemyData in enemyDataList.Take(4))
            {
                foreach (int level in new[] { 1, 5, 10 })
                {
                    // Calculate what stats would be with DPS system
                    var archetype = EnemyDPSCalculator.SuggestArchetypeForEnemy(enemyData.Name, enemyData.Strength, enemyData.Agility, enemyData.Technique, enemyData.Intelligence);
                    double targetDPS = CalculateTargetDPS(level);
                    var (targetDamage, targetAttackSpeed) = CalculateArchetypeStats(targetDPS, archetype);
                    var (calcStr, calcAgi) = CalculateRequiredStats(targetDamage, targetAttackSpeed, level, archetype);
                    
                    // Show actual enemy stats
                    var enemy = EnemyLoader.CreateEnemy(enemyData.Name, level);
                    int finalStr = enemy?.Strength ?? 0;
                    int finalAgi = enemy?.Agility ?? 0;
                    
                    UIManager.WriteSystemLine($"{enemyData.Name.PadRight(12)}\tL{level}\t{enemyData.Strength}/{enemyData.Agility}\t\t{calcStr}/{calcAgi}\t\t{finalStr}/{finalAgi}");
                }
                UIManager.WriteSystemLine("");
            }
            
            UIManager.WriteSystemLine("");
            UIManager.WriteSystemLine("=== DPS SCALING CURVE ===");
            UIManager.WriteSystemLine("Level\tTarget DPS");
            UIManager.WriteSystemLine("".PadRight(20, '-'));
            
            for (int level = 1; level <= 20; level++)
            {
                double targetDPS = CalculateTargetDPS(level);
                UIManager.WriteSystemLine($"{level}\t{targetDPS:F1}");
            }
        }
    }
    
    public class DPSValidationResult
    {
        public string EnemyName { get; set; } = "";
        public int Level { get; set; }
        public EnemyArchetype Archetype { get; set; }
        public double TargetDPS { get; set; }
        public double ActualDPS { get; set; }
        public double DeviationPercent { get; set; }
        public bool IsWithinTolerance { get; set; }
        public string Recommendation { get; set; } = "";
    }
}
