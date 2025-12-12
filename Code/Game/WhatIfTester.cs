using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RPGGame.Config;
using RPGGame.Utils;

namespace RPGGame
{
    /// <summary>
    /// Tests hypothetical parameter changes without applying them permanently
    /// Allows safe exploration of "what-if" scenarios
    /// </summary>
    public static class WhatIfTester
    {
        /// <summary>
        /// Tests a what-if scenario: what would happen if we changed a parameter?
        /// </summary>
        public static async Task<WhatIfTestResult> TestWhatIf(
            string parameterName,
            double testValue,
            int numberOfBattles = 200,
            IProgress<(int completed, int total, string status)>? progress = null)
        {
            // Get current value using BalanceTuningConsole helper
            double currentValue = GetParameterValueHelper(parameterName);
            if (currentValue == 0)
            {
                throw new ArgumentException($"Parameter '{parameterName}' not found or has zero value");
            }

            // Run baseline test with current value
            progress?.Report((0, numberOfBattles * 2, "Running baseline test..."));
            var baselineConfig = new BattleStatisticsRunner.BattleConfiguration
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

            var baselineResult = await BattleStatisticsRunner.RunParallelBattles(baselineConfig, numberOfBattles, progress);
            var baselineQualityScore = BalanceTuningGoals.CalculateQualityScore(
                baselineResult.WinRate * 100,
                baselineResult.AverageTurns,
                0,
                0
            );

            // Apply test value temporarily
            var originalValue = currentValue;
            SetParameterValueHelper(parameterName, testValue);

            try
            {
                // Run test with new value
                progress?.Report((numberOfBattles, numberOfBattles * 2, $"Testing {parameterName}={testValue:F2}..."));
                var testResult = await BattleStatisticsRunner.RunParallelBattles(baselineConfig, numberOfBattles, progress);
                var testQualityScore = BalanceTuningGoals.CalculateQualityScore(
                    testResult.WinRate * 100,
                    testResult.AverageTurns,
                    0,
                    0
                );

                // Restore original value
                SetParameterValueHelper(parameterName, originalValue);

                // Calculate changes
                var winRateChange = (testResult.WinRate * 100) - (baselineResult.WinRate * 100);
                var durationChange = testResult.AverageTurns - baselineResult.AverageTurns;
                var qualityScoreChange = testQualityScore - baselineQualityScore;

                // Assess risk
                string riskAssessment = "low";
                if (Math.Abs(qualityScoreChange) > 20)
                    riskAssessment = "high";
                else if (Math.Abs(qualityScoreChange) > 10)
                    riskAssessment = "medium";

                // Generate recommendation
                string recommendation;
                if (qualityScoreChange > 5)
                {
                    recommendation = $"RECOMMENDED: This change improves quality score by {qualityScoreChange:F1} points. Win rate: {baselineResult.WinRate * 100:F1}% → {testResult.WinRate * 100:F1}%";
                }
                else if (qualityScoreChange < -5)
                {
                    recommendation = $"NOT RECOMMENDED: This change degrades quality score by {Math.Abs(qualityScoreChange):F1} points. Win rate: {baselineResult.WinRate * 100:F1}% → {testResult.WinRate * 100:F1}%";
                }
                else
                {
                    recommendation = $"NEUTRAL: This change has minimal impact. Quality score change: {qualityScoreChange:F1} points. Win rate: {baselineResult.WinRate * 100:F1}% → {testResult.WinRate * 100:F1}%";
                }

                var result = new WhatIfTestResult
                {
                    ParameterName = parameterName,
                    CurrentValue = currentValue,
                    TestValue = testValue,
                    WinRateChange = winRateChange,
                    DurationChange = durationChange,
                    QualityScoreChange = qualityScoreChange,
                    QualityScoreBefore = baselineQualityScore,
                    QualityScoreAfter = testQualityScore,
                    RiskAssessment = riskAssessment,
                    Recommendation = recommendation,
                    DetailedMetrics = new Dictionary<string, object>
                    {
                        ["baselineWinRate"] = baselineResult.WinRate * 100,
                        ["testWinRate"] = testResult.WinRate * 100,
                        ["baselineDuration"] = baselineResult.AverageTurns,
                        ["testDuration"] = testResult.AverageTurns,
                        ["baselinePlayerDamage"] = baselineResult.AveragePlayerDamageDealt,
                        ["testPlayerDamage"] = testResult.AveragePlayerDamageDealt,
                        ["baselineEnemyDamage"] = baselineResult.AverageEnemyDamageDealt,
                        ["testEnemyDamage"] = testResult.AverageEnemyDamageDealt
                    }
                };

                return result;
            }
            finally
            {
                // Always restore original value, even on error
                SetParameterValueHelper(parameterName, originalValue);
            }
        }

        /// <summary>
        /// Helper to get parameter value (duplicated from ParameterSensitivityAnalyzer for independence)
        /// </summary>
        private static double GetParameterValueHelper(string parameterName)
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
        /// Helper to set parameter value (duplicated from ParameterSensitivityAnalyzer for independence)
        /// </summary>
        private static void SetParameterValueHelper(string parameterName, double value)
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
                ScrollDebugLogger.Log($"WhatIfTester: Error setting parameter {parameterName} to {value}: {ex.Message}");
            }
        }

        /// <summary>
        /// Tests multiple what-if scenarios at once
        /// </summary>
        public static async Task<List<WhatIfTestResult>> TestMultipleWhatIfs(
            Dictionary<string, double> parameterChanges,
            int numberOfBattles = 200,
            IProgress<(int completed, int total, string status)>? progress = null)
        {
            var results = new List<WhatIfTestResult>();
            int totalTests = parameterChanges.Count;
            int completedTests = 0;

            foreach (var kvp in parameterChanges)
            {
                progress?.Report((completedTests, totalTests, $"Testing {kvp.Key}={kvp.Value:F2}..."));
                var result = await TestWhatIf(kvp.Key, kvp.Value, numberOfBattles, null);
                results.Add(result);
                completedTests++;
            }

            return results;
        }
    }
}

