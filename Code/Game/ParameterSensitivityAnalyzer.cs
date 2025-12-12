using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RPGGame.Config;
using RPGGame.Utils;

namespace RPGGame
{
    /// <summary>
    /// Analyzes parameter sensitivity to understand which parameters have the most impact on balance
    /// </summary>
    public static class ParameterSensitivityAnalyzer
    {
        /// <summary>
        /// Analyzes sensitivity of a single parameter
        /// </summary>
        public static async Task<ParameterSensitivityResult> AnalyzeParameter(
            string parameterName,
            string range,
            int testPoints = 10,
            int battlesPerPoint = 50,
            IProgress<(int completed, int total, string status)>? progress = null)
        {
            // Parse range (e.g., "0.8,1.2" means 80% to 120%)
            var rangeParts = range.Split(',');
            if (rangeParts.Length != 2 || 
                !double.TryParse(rangeParts[0].Trim(), out double minPercent) ||
                !double.TryParse(rangeParts[1].Trim(), out double maxPercent))
            {
                throw new ArgumentException($"Invalid range format: {range}. Expected format: 'min,max' (e.g., '0.8,1.2')");
            }

            // Get current value
            double currentValue = GetParameterValue(parameterName);
            if (currentValue == 0)
            {
                throw new ArgumentException($"Parameter '{parameterName}' not found or has zero value");
            }

            double minValue = currentValue * minPercent;
            double maxValue = currentValue * maxPercent;
            double step = (maxValue - minValue) / (testPoints - 1);

            var result = new ParameterSensitivityResult
            {
                ParameterName = parameterName,
                MinValue = minValue,
                MaxValue = maxValue,
                TestPoints = testPoints
            };

            var testPointsData = new List<ParameterTestPoint>();
            int totalTests = testPoints * battlesPerPoint;
            int completedTests = 0;

            // Test each point
            for (int i = 0; i < testPoints; i++)
            {
                double testValue = minValue + (step * i);
                
                // Apply test value
                SetParameterValue(parameterName, testValue);
                
                // Run battles
                var config = new BattleStatisticsRunner.BattleConfiguration
                {
                    PlayerDamage = 10,
                    PlayerAttackSpeed = 1.0,
                    PlayerArmor = 5,
                    PlayerHealth = 100,
                    EnemyDamage = 8,
                    EnemyAttackSpeed = 1.0,
                    EnemyArmor = 3,
                    EnemyHealth = 80
                };

                progress?.Report((completedTests, totalTests, $"Testing {parameterName}={testValue:F2} ({i + 1}/{testPoints})"));

                var battleResult = await BattleStatisticsRunner.RunParallelBattles(config, battlesPerPoint, null);
                
                // Calculate quality score for this test point
                var qualityScore = BalanceTuningGoals.CalculateQualityScore(
                    battleResult.WinRate * 100,
                    battleResult.AverageTurns,
                    0, // Weapon variance (not applicable for single parameter test)
                    0  // Enemy variance (not applicable)
                );

                testPointsData.Add(new ParameterTestPoint
                {
                    ParameterValue = testValue,
                    WinRate = battleResult.WinRate * 100,
                    AverageCombatDuration = battleResult.AverageTurns,
                    QualityScore = qualityScore,
                    BattlesTested = battlesPerPoint
                });

                completedTests += battlesPerPoint;
            }

            // Restore original value
            SetParameterValue(parameterName, currentValue);

            result.TestPointsData = testPointsData;

            // Find optimal value (highest quality score)
            var optimalPoint = testPointsData.OrderByDescending(p => p.QualityScore).FirstOrDefault();
            if (optimalPoint != null)
            {
                result.OptimalValue = optimalPoint.ParameterValue;
                result.OptimalQualityScore = optimalPoint.QualityScore;
            }

            // Calculate sensitivity score (0-1, higher = more sensitive)
            // Sensitivity = (max quality - min quality) / average quality
            var maxQuality = testPointsData.Max(p => p.QualityScore);
            var minQuality = testPointsData.Min(p => p.QualityScore);
            var avgQuality = testPointsData.Average(p => p.QualityScore);
            result.SensitivityScore = avgQuality > 0 ? (maxQuality - minQuality) / avgQuality : 0;

            // Generate recommendation
            if (result.SensitivityScore > 0.3)
            {
                result.Recommendation = $"High sensitivity parameter. Small changes have significant impact. Current value: {currentValue:F2}, Optimal: {result.OptimalValue:F2}";
            }
            else if (result.SensitivityScore > 0.1)
            {
                result.Recommendation = $"Moderate sensitivity parameter. Changes have noticeable impact. Current value: {currentValue:F2}, Optimal: {result.OptimalValue:F2}";
            }
            else
            {
                result.Recommendation = $"Low sensitivity parameter. Changes have minimal impact. Current value: {currentValue:F2}";
            }

            return result;
        }

        /// <summary>
        /// Gets the current value of a parameter
        /// </summary>
        private static double GetParameterValue(string parameterName)
        {
            var config = GameConfiguration.Instance;
            var parts = parameterName.Split('.');

            try
            {
                return parts[0].ToLower() switch
                {
                    "enemy" when parts.Length >= 3 && parts[1].ToLower() == "globalmultipliers" => parts[2].ToLower() switch
                    {
                        "health" => config.EnemySystem.GlobalMultipliers.HealthMultiplier,
                        "damage" => config.EnemySystem.GlobalMultipliers.DamageMultiplier,
                        "armor" => config.EnemySystem.GlobalMultipliers.ArmorMultiplier,
                        "speed" => config.EnemySystem.GlobalMultipliers.SpeedMultiplier,
                        _ => 0
                    },
                    "enemy" when parts.Length >= 3 && parts[1].ToLower() == "baselinestats" => parts[2].ToLower() switch
                    {
                        "health" => config.EnemySystem.BaselineStats.Health,
                        "strength" => config.EnemySystem.BaselineStats.Strength,
                        "agility" => config.EnemySystem.BaselineStats.Agility,
                        "technique" => config.EnemySystem.BaselineStats.Technique,
                        "intelligence" => config.EnemySystem.BaselineStats.Intelligence,
                        "armor" => config.EnemySystem.BaselineStats.Armor,
                        _ => 0
                    },
                    "player" when parts.Length >= 3 && parts[1].ToLower() == "baseattributes" => parts[2].ToLower() switch
                    {
                        "strength" => config.Attributes.PlayerBaseAttributes.Strength,
                        "agility" => config.Attributes.PlayerBaseAttributes.Agility,
                        "technique" => config.Attributes.PlayerBaseAttributes.Technique,
                        "intelligence" => config.Attributes.PlayerBaseAttributes.Intelligence,
                        _ => 0
                    },
                    "player" when parts.Length >= 2 && parts[1].ToLower() == "basehealth" => config.Character.PlayerBaseHealth,
                    "combat" when parts.Length >= 2 => parts[1].ToLower() switch
                    {
                        "criticalhitthreshold" => config.Combat.CriticalHitThreshold,
                        "criticalhitmultiplier" => config.Combat.CriticalHitMultiplier,
                        "minimumdamage" => config.Combat.MinimumDamage,
                        "baseattacktime" => config.Combat.BaseAttackTime,
                        _ => 0
                    },
                    _ => 0
                };
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Sets the value of a parameter
        /// </summary>
        private static void SetParameterValue(string parameterName, double value)
        {
            var config = GameConfiguration.Instance;
            var parts = parameterName.Split('.');

            try
            {
                switch (parts[0].ToLower())
                {
                    case "enemy" when parts.Length >= 3 && parts[1].ToLower() == "globalmultipliers":
                        switch (parts[2].ToLower())
                        {
                            case "health":
                                config.EnemySystem.GlobalMultipliers.HealthMultiplier = value;
                                break;
                            case "damage":
                                config.EnemySystem.GlobalMultipliers.DamageMultiplier = value;
                                break;
                            case "armor":
                                config.EnemySystem.GlobalMultipliers.ArmorMultiplier = value;
                                break;
                            case "speed":
                                config.EnemySystem.GlobalMultipliers.SpeedMultiplier = value;
                                break;
                        }
                        break;
                    case "enemy" when parts.Length >= 3 && parts[1].ToLower() == "baselinestats":
                        switch (parts[2].ToLower())
                        {
                            case "health":
                                config.EnemySystem.BaselineStats.Health = (int)value;
                                break;
                            case "strength":
                                config.EnemySystem.BaselineStats.Strength = (int)value;
                                break;
                            case "agility":
                                config.EnemySystem.BaselineStats.Agility = (int)value;
                                break;
                            case "technique":
                                config.EnemySystem.BaselineStats.Technique = (int)value;
                                break;
                            case "intelligence":
                                config.EnemySystem.BaselineStats.Intelligence = (int)value;
                                break;
                            case "armor":
                                config.EnemySystem.BaselineStats.Armor = (int)value;
                                break;
                        }
                        break;
                    case "player" when parts.Length >= 3 && parts[1].ToLower() == "baseattributes":
                        switch (parts[2].ToLower())
                        {
                            case "strength":
                                config.Attributes.PlayerBaseAttributes.Strength = (int)value;
                                break;
                            case "agility":
                                config.Attributes.PlayerBaseAttributes.Agility = (int)value;
                                break;
                            case "technique":
                                config.Attributes.PlayerBaseAttributes.Technique = (int)value;
                                break;
                            case "intelligence":
                                config.Attributes.PlayerBaseAttributes.Intelligence = (int)value;
                                break;
                        }
                        break;
                    case "player" when parts.Length >= 2 && parts[1].ToLower() == "basehealth":
                        config.Character.PlayerBaseHealth = (int)value;
                        break;
                    case "combat" when parts.Length >= 2:
                        switch (parts[1].ToLower())
                        {
                            case "criticalhitthreshold":
                                config.Combat.CriticalHitThreshold = (int)value;
                                break;
                            case "criticalhitmultiplier":
                                config.Combat.CriticalHitMultiplier = value;
                                break;
                            case "minimumdamage":
                                config.Combat.MinimumDamage = (int)value;
                                break;
                            case "baseattacktime":
                                config.Combat.BaseAttackTime = value;
                                break;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"ParameterSensitivityAnalyzer: Error setting parameter {parameterName} to {value}: {ex.Message}");
            }
        }
    }
}

