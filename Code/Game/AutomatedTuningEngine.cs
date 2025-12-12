using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Config;
using RPGGame.Utils;

namespace RPGGame
{
    /// <summary>
    /// Automated tuning engine that analyzes battle results and suggests specific adjustments
    /// Makes simulations valuable by providing actionable tuning recommendations
    /// </summary>
    public static class AutomatedTuningEngine
    {
        /// <summary>
        /// Tuning suggestion for a specific adjustment
        /// </summary>
        public class TuningSuggestion
        {
            public string Id { get; set; } = "";
            public BalanceTuningGoals.TuningPriority Priority { get; set; }
            public string Category { get; set; } = ""; // "global", "archetype", "weapon", "enemy"
            public string Target { get; set; } = ""; // What to adjust
            public string Parameter { get; set; } = ""; // Which parameter
            public double CurrentValue { get; set; }
            public double SuggestedValue { get; set; }
            public double AdjustmentMagnitude { get; set; } // Percentage change
            public string Reason { get; set; } = "";
            public string Impact { get; set; } = ""; // Expected impact description
            public List<string> AffectedMatchups { get; set; } = new();
        }

        /// <summary>
        /// Complete tuning analysis with suggestions
        /// </summary>
        public class TuningAnalysis
        {
            public double QualityScore { get; set; }
            public double OverallWinRate { get; set; }
            public double AverageCombatDuration { get; set; }
            public double WeaponVariance { get; set; }
            public double EnemyVariance { get; set; }
            public List<TuningSuggestion> Suggestions { get; set; } = new();
            public Dictionary<BalanceTuningGoals.TuningPriority, int> SuggestionCounts { get; set; } = new();
            public string Summary { get; set; } = "";
        }

        /// <summary>
        /// Analyze battle results and generate tuning suggestions
        /// </summary>
        public static TuningAnalysis AnalyzeAndSuggest(
            BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult testResult)
        {
            var analysis = new TuningAnalysis
            {
                OverallWinRate = testResult.OverallWinRate,
                AverageCombatDuration = testResult.OverallAverageTurns
            };

            // Calculate weapon variance
            if (testResult.WeaponStatistics.Count > 0)
            {
                var weaponWinRates = testResult.WeaponStatistics.Values.Select(w => w.WinRate).ToList();
                analysis.WeaponVariance = weaponWinRates.Max() - weaponWinRates.Min();
            }

            // Calculate enemy variance
            if (testResult.EnemyStatistics.Count > 0)
            {
                var enemyWinRates = testResult.EnemyStatistics.Values.Select(e => e.WinRate).ToList();
                analysis.EnemyVariance = enemyWinRates.Max() - enemyWinRates.Min();
            }

            // Calculate quality score
            analysis.QualityScore = BalanceTuningGoals.CalculateQualityScore(
                analysis.OverallWinRate,
                analysis.AverageCombatDuration,
                analysis.WeaponVariance,
                analysis.EnemyVariance);

            // Generate suggestions
            var suggestions = new List<TuningSuggestion>();

            // 1. Overall win rate adjustments (global multipliers)
            suggestions.AddRange(SuggestGlobalAdjustments(testResult, analysis));

            // 2. Player adjustments (attributes and health)
            suggestions.AddRange(SuggestPlayerAdjustments(testResult, analysis));

            // 3. Enemy baseline adjustments (attributes and health)
            suggestions.AddRange(SuggestEnemyBaselineAdjustments(testResult, analysis));

            // 4. Weapon balance adjustments
            suggestions.AddRange(SuggestWeaponBalanceAdjustments(testResult, analysis));

            // 5. Enemy-specific adjustments (archetypes)
            suggestions.AddRange(SuggestEnemyAdjustments(testResult, analysis));

            // 6. Combat duration adjustments
            suggestions.AddRange(SuggestDurationAdjustments(testResult, analysis));

            // Sort by priority
            suggestions = suggestions.OrderBy(s => (int)s.Priority).ThenByDescending(s => Math.Abs(s.AdjustmentMagnitude)).ToList();

            analysis.Suggestions = suggestions;

            // Count suggestions by priority
            analysis.SuggestionCounts = suggestions
                .GroupBy(s => s.Priority)
                .ToDictionary(g => g.Key, g => g.Count());

            // Generate summary
            analysis.Summary = GenerateSummary(analysis, suggestions);

            return analysis;
        }

        /// <summary>
        /// Suggest global enemy multiplier adjustments
        /// </summary>
        private static List<TuningSuggestion> SuggestGlobalAdjustments(
            BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult testResult,
            TuningAnalysis analysis)
        {
            var suggestions = new List<TuningSuggestion>();
            var config = GameConfiguration.Instance;
            var multipliers = config.EnemySystem.GlobalMultipliers;

            // Overall win rate too low (<85%) - enemies too strong
            if (analysis.OverallWinRate < BalanceTuningGoals.WinRateTargets.MinTarget)
            {
                var magnitude = BalanceTuningGoals.GetAdjustmentMagnitude(
                    analysis.OverallWinRate, 
                    BalanceTuningGoals.WinRateTargets.OptimalMin);
                
                // Reduce enemy effectiveness
                var healthAdjustment = 1.0 - (magnitude * 0.6); // Reduce health more
                var damageAdjustment = 1.0 - (magnitude * 0.4); // Reduce damage less

                suggestions.Add(new TuningSuggestion
                {
                    Id = "global_health_reduce",
                    Priority = analysis.OverallWinRate < BalanceTuningGoals.WinRateTargets.CriticalLow 
                        ? BalanceTuningGoals.TuningPriority.Critical 
                        : BalanceTuningGoals.TuningPriority.High,
                    Category = "global",
                    Target = "All Enemies",
                    Parameter = "HealthMultiplier",
                    CurrentValue = multipliers.HealthMultiplier,
                    SuggestedValue = multipliers.HealthMultiplier * healthAdjustment,
                    AdjustmentMagnitude = (1.0 - healthAdjustment) * 100.0,
                    Reason = $"Overall win rate {analysis.OverallWinRate:F1}% is below target (85-98%). Enemies are too strong.",
                    Impact = $"Reduce enemy health by {(1.0 - healthAdjustment) * 100.0:F1}% to make enemies easier",
                    AffectedMatchups = testResult.CombinationResults.Select(c => $"{c.WeaponType} vs {c.EnemyType}").ToList()
                });

                suggestions.Add(new TuningSuggestion
                {
                    Id = "global_damage_reduce",
                    Priority = analysis.OverallWinRate < BalanceTuningGoals.WinRateTargets.CriticalLow 
                        ? BalanceTuningGoals.TuningPriority.Critical 
                        : BalanceTuningGoals.TuningPriority.High,
                    Category = "global",
                    Target = "All Enemies",
                    Parameter = "DamageMultiplier",
                    CurrentValue = multipliers.DamageMultiplier,
                    SuggestedValue = multipliers.DamageMultiplier * damageAdjustment,
                    AdjustmentMagnitude = (1.0 - damageAdjustment) * 100.0,
                    Reason = $"Overall win rate {analysis.OverallWinRate:F1}% is below target. Reducing enemy damage.",
                    Impact = $"Reduce enemy damage by {(1.0 - damageAdjustment) * 100.0:F1}%",
                    AffectedMatchups = testResult.CombinationResults.Select(c => $"{c.WeaponType} vs {c.EnemyType}").ToList()
                });
            }
            // Overall win rate too high (>98%) - enemies too weak
            else if (analysis.OverallWinRate > BalanceTuningGoals.WinRateTargets.MaxTarget)
            {
                var magnitude = BalanceTuningGoals.GetAdjustmentMagnitude(
                    analysis.OverallWinRate, 
                    BalanceTuningGoals.WinRateTargets.OptimalMax);
                
                // Increase enemy effectiveness
                var healthAdjustment = 1.0 + (magnitude * 0.6);
                var damageAdjustment = 1.0 + (magnitude * 0.4);

                suggestions.Add(new TuningSuggestion
                {
                    Id = "global_health_increase",
                    Priority = analysis.OverallWinRate > BalanceTuningGoals.WinRateTargets.CriticalHigh 
                        ? BalanceTuningGoals.TuningPriority.Critical 
                        : BalanceTuningGoals.TuningPriority.High,
                    Category = "global",
                    Target = "All Enemies",
                    Parameter = "HealthMultiplier",
                    CurrentValue = multipliers.HealthMultiplier,
                    SuggestedValue = multipliers.HealthMultiplier * healthAdjustment,
                    AdjustmentMagnitude = (healthAdjustment - 1.0) * 100.0,
                    Reason = $"Overall win rate {analysis.OverallWinRate:F1}% is above target (85-98%). Enemies are too weak.",
                    Impact = $"Increase enemy health by {(healthAdjustment - 1.0) * 100.0:F1}% to make enemies harder",
                    AffectedMatchups = testResult.CombinationResults.Select(c => $"{c.WeaponType} vs {c.EnemyType}").ToList()
                });

                suggestions.Add(new TuningSuggestion
                {
                    Id = "global_damage_increase",
                    Priority = analysis.OverallWinRate > BalanceTuningGoals.WinRateTargets.CriticalHigh 
                        ? BalanceTuningGoals.TuningPriority.Critical 
                        : BalanceTuningGoals.TuningPriority.High,
                    Category = "global",
                    Target = "All Enemies",
                    Parameter = "DamageMultiplier",
                    CurrentValue = multipliers.DamageMultiplier,
                    SuggestedValue = multipliers.DamageMultiplier * damageAdjustment,
                    AdjustmentMagnitude = (damageAdjustment - 1.0) * 100.0,
                    Reason = $"Overall win rate {analysis.OverallWinRate:F1}% is above target. Increasing enemy damage.",
                    Impact = $"Increase enemy damage by {(damageAdjustment - 1.0) * 100.0:F1}%",
                    AffectedMatchups = testResult.CombinationResults.Select(c => $"{c.WeaponType} vs {c.EnemyType}").ToList()
                });
            }

            return suggestions;
        }

        /// <summary>
        /// Suggest player attribute and health adjustments
        /// </summary>
        private static List<TuningSuggestion> SuggestPlayerAdjustments(
            BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult testResult,
            TuningAnalysis analysis)
        {
            var suggestions = new List<TuningSuggestion>();
            var config = GameConfiguration.Instance;

            // If win rate is too low, suggest buffing player
            if (analysis.OverallWinRate < BalanceTuningGoals.WinRateTargets.MinTarget)
            {
                var magnitude = BalanceTuningGoals.GetAdjustmentMagnitude(
                    analysis.OverallWinRate, 
                    BalanceTuningGoals.WinRateTargets.OptimalMin);

                // Suggest increasing player base strength (damage)
                var currentStrength = config.Attributes.PlayerBaseAttributes.Strength;
                var strengthIncrease = (int)Math.Ceiling(currentStrength * magnitude * 0.5); // 50% of magnitude

                suggestions.Add(new TuningSuggestion
                {
                    Id = "player_strength_increase",
                    Priority = analysis.OverallWinRate < BalanceTuningGoals.WinRateTargets.CriticalLow 
                        ? BalanceTuningGoals.TuningPriority.Critical 
                        : BalanceTuningGoals.TuningPriority.High,
                    Category = "player",
                    Target = "Player",
                    Parameter = "BaseStrength",
                    CurrentValue = currentStrength,
                    SuggestedValue = currentStrength + strengthIncrease,
                    AdjustmentMagnitude = (strengthIncrease / (double)currentStrength) * 100.0,
                    Reason = $"Overall win rate {analysis.OverallWinRate:F1}% is below target. Player needs more damage output.",
                    Impact = $"Increase player base Strength by {strengthIncrease} (from {currentStrength} to {currentStrength + strengthIncrease})",
                    AffectedMatchups = testResult.CombinationResults.Select(c => $"{c.WeaponType} vs {c.EnemyType}").ToList()
                });

                // Suggest increasing player base health (survivability)
                var currentHealth = config.Character.PlayerBaseHealth;
                var healthIncrease = (int)Math.Ceiling(currentHealth * magnitude * 0.3); // 30% of magnitude

                suggestions.Add(new TuningSuggestion
                {
                    Id = "player_health_increase",
                    Priority = analysis.OverallWinRate < BalanceTuningGoals.WinRateTargets.CriticalLow 
                        ? BalanceTuningGoals.TuningPriority.Critical 
                        : BalanceTuningGoals.TuningPriority.High,
                    Category = "player",
                    Target = "Player",
                    Parameter = "BaseHealth",
                    CurrentValue = currentHealth,
                    SuggestedValue = currentHealth + healthIncrease,
                    AdjustmentMagnitude = (healthIncrease / (double)currentHealth) * 100.0,
                    Reason = $"Overall win rate {analysis.OverallWinRate:F1}% is below target. Player needs more survivability.",
                    Impact = $"Increase player base health by {healthIncrease} (from {currentHealth} to {currentHealth + healthIncrease})",
                    AffectedMatchups = testResult.CombinationResults.Select(c => $"{c.WeaponType} vs {c.EnemyType}").ToList()
                });
            }
            // If win rate is too high, suggest nerfing player
            else if (analysis.OverallWinRate > BalanceTuningGoals.WinRateTargets.MaxTarget)
            {
                var magnitude = BalanceTuningGoals.GetAdjustmentMagnitude(
                    analysis.OverallWinRate, 
                    BalanceTuningGoals.WinRateTargets.OptimalMax);

                // Suggest decreasing player base strength
                var currentStrength = config.Attributes.PlayerBaseAttributes.Strength;
                var strengthDecrease = (int)Math.Ceiling(currentStrength * magnitude * 0.5);

                suggestions.Add(new TuningSuggestion
                {
                    Id = "player_strength_decrease",
                    Priority = analysis.OverallWinRate > BalanceTuningGoals.WinRateTargets.CriticalHigh 
                        ? BalanceTuningGoals.TuningPriority.Critical 
                        : BalanceTuningGoals.TuningPriority.High,
                    Category = "player",
                    Target = "Player",
                    Parameter = "BaseStrength",
                    CurrentValue = currentStrength,
                    SuggestedValue = Math.Max(1, currentStrength - strengthDecrease),
                    AdjustmentMagnitude = (strengthDecrease / (double)currentStrength) * 100.0,
                    Reason = $"Overall win rate {analysis.OverallWinRate:F1}% is above target. Player is too strong.",
                    Impact = $"Decrease player base Strength by {strengthDecrease} (from {currentStrength} to {Math.Max(1, currentStrength - strengthDecrease)})",
                    AffectedMatchups = testResult.CombinationResults.Select(c => $"{c.WeaponType} vs {c.EnemyType}").ToList()
                });
            }

            return suggestions;
        }

        /// <summary>
        /// Suggest enemy baseline stat adjustments
        /// </summary>
        private static List<TuningSuggestion> SuggestEnemyBaselineAdjustments(
            BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult testResult,
            TuningAnalysis analysis)
        {
            var suggestions = new List<TuningSuggestion>();
            var config = GameConfiguration.Instance;
            var baselineStats = config.EnemySystem.BaselineStats;

            // If win rate is too low, suggest reducing enemy baseline stats
            if (analysis.OverallWinRate < BalanceTuningGoals.WinRateTargets.MinTarget)
            {
                var magnitude = BalanceTuningGoals.GetAdjustmentMagnitude(
                    analysis.OverallWinRate, 
                    BalanceTuningGoals.WinRateTargets.OptimalMin);

                // Reduce enemy baseline strength (damage)
                var currentStrength = baselineStats.Strength;
                var strengthDecrease = (int)Math.Ceiling(currentStrength * magnitude * 0.4);

                suggestions.Add(new TuningSuggestion
                {
                    Id = "enemy_baseline_strength_decrease",
                    Priority = analysis.OverallWinRate < BalanceTuningGoals.WinRateTargets.CriticalLow 
                        ? BalanceTuningGoals.TuningPriority.Critical 
                        : BalanceTuningGoals.TuningPriority.High,
                    Category = "enemy_baseline",
                    Target = "All Enemies",
                    Parameter = "BaselineStrength",
                    CurrentValue = currentStrength,
                    SuggestedValue = Math.Max(1, currentStrength - strengthDecrease),
                    AdjustmentMagnitude = (strengthDecrease / (double)currentStrength) * 100.0,
                    Reason = $"Overall win rate {analysis.OverallWinRate:F1}% is below target. Enemies deal too much damage.",
                    Impact = $"Decrease enemy baseline Strength by {strengthDecrease} (from {currentStrength} to {Math.Max(1, currentStrength - strengthDecrease)})",
                    AffectedMatchups = testResult.CombinationResults.Select(c => $"{c.WeaponType} vs {c.EnemyType}").ToList()
                });

                // Reduce enemy baseline health
                var currentHealth = baselineStats.Health;
                var healthDecrease = (int)Math.Ceiling(currentHealth * magnitude * 0.5);

                suggestions.Add(new TuningSuggestion
                {
                    Id = "enemy_baseline_health_decrease",
                    Priority = analysis.OverallWinRate < BalanceTuningGoals.WinRateTargets.CriticalLow 
                        ? BalanceTuningGoals.TuningPriority.Critical 
                        : BalanceTuningGoals.TuningPriority.High,
                    Category = "enemy_baseline",
                    Target = "All Enemies",
                    Parameter = "BaselineHealth",
                    CurrentValue = currentHealth,
                    SuggestedValue = Math.Max(1, currentHealth - healthDecrease),
                    AdjustmentMagnitude = (healthDecrease / (double)currentHealth) * 100.0,
                    Reason = $"Overall win rate {analysis.OverallWinRate:F1}% is below target. Enemies have too much health.",
                    Impact = $"Decrease enemy baseline health by {healthDecrease} (from {currentHealth} to {Math.Max(1, currentHealth - healthDecrease)})",
                    AffectedMatchups = testResult.CombinationResults.Select(c => $"{c.WeaponType} vs {c.EnemyType}").ToList()
                });
            }
            // If win rate is too high, suggest increasing enemy baseline stats
            else if (analysis.OverallWinRate > BalanceTuningGoals.WinRateTargets.MaxTarget)
            {
                var magnitude = BalanceTuningGoals.GetAdjustmentMagnitude(
                    analysis.OverallWinRate, 
                    BalanceTuningGoals.WinRateTargets.OptimalMax);

                // Increase enemy baseline strength
                var currentStrength = baselineStats.Strength;
                var strengthIncrease = (int)Math.Ceiling(currentStrength * magnitude * 0.4);

                suggestions.Add(new TuningSuggestion
                {
                    Id = "enemy_baseline_strength_increase",
                    Priority = analysis.OverallWinRate > BalanceTuningGoals.WinRateTargets.CriticalHigh 
                        ? BalanceTuningGoals.TuningPriority.Critical 
                        : BalanceTuningGoals.TuningPriority.High,
                    Category = "enemy_baseline",
                    Target = "All Enemies",
                    Parameter = "BaselineStrength",
                    CurrentValue = currentStrength,
                    SuggestedValue = currentStrength + strengthIncrease,
                    AdjustmentMagnitude = (strengthIncrease / (double)currentStrength) * 100.0,
                    Reason = $"Overall win rate {analysis.OverallWinRate:F1}% is above target. Enemies need more damage.",
                    Impact = $"Increase enemy baseline Strength by {strengthIncrease} (from {currentStrength} to {currentStrength + strengthIncrease})",
                    AffectedMatchups = testResult.CombinationResults.Select(c => $"{c.WeaponType} vs {c.EnemyType}").ToList()
                });

                // Increase enemy baseline health
                var currentHealth = baselineStats.Health;
                var healthIncrease = (int)Math.Ceiling(currentHealth * magnitude * 0.5);

                suggestions.Add(new TuningSuggestion
                {
                    Id = "enemy_baseline_health_increase",
                    Priority = analysis.OverallWinRate > BalanceTuningGoals.WinRateTargets.CriticalHigh 
                        ? BalanceTuningGoals.TuningPriority.Critical 
                        : BalanceTuningGoals.TuningPriority.High,
                    Category = "enemy_baseline",
                    Target = "All Enemies",
                    Parameter = "BaselineHealth",
                    CurrentValue = currentHealth,
                    SuggestedValue = currentHealth + healthIncrease,
                    AdjustmentMagnitude = (healthIncrease / (double)currentHealth) * 100.0,
                    Reason = $"Overall win rate {analysis.OverallWinRate:F1}% is above target. Enemies need more health.",
                    Impact = $"Increase enemy baseline health by {healthIncrease} (from {currentHealth} to {currentHealth + healthIncrease})",
                    AffectedMatchups = testResult.CombinationResults.Select(c => $"{c.WeaponType} vs {c.EnemyType}").ToList()
                });
            }

            return suggestions;
        }

        /// <summary>
        /// Suggest weapon balance adjustments
        /// </summary>
        private static List<TuningSuggestion> SuggestWeaponBalanceAdjustments(
            BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult testResult,
            TuningAnalysis analysis)
        {
            var suggestions = new List<TuningSuggestion>();

            if (analysis.WeaponVariance > BalanceTuningGoals.WeaponBalanceTargets.MaxVariance)
            {
                var weaponStats = testResult.WeaponStatistics.OrderBy(kvp => kvp.Value.WinRate).ToList();
                var worstWeapon = weaponStats.First();
                var bestWeapon = weaponStats.Last();

                var worstWinRate = worstWeapon.Value.WinRate;
                var bestWinRate = bestWeapon.Value.WinRate;
                var targetWinRate = (worstWinRate + bestWinRate) / 2.0;

                // Suggest buffing worst weapon
                if (worstWinRate < BalanceTuningGoals.WinRateTargets.OptimalMin)
                {
                    var magnitude = BalanceTuningGoals.GetAdjustmentMagnitude(worstWinRate, targetWinRate, 0.05, 0.30);
                    suggestions.Add(new TuningSuggestion
                    {
                        Id = $"weapon_buff_{worstWeapon.Key}",
                        Priority = worstWinRate < BalanceTuningGoals.WinRateTargets.CriticalLow 
                            ? BalanceTuningGoals.TuningPriority.Critical 
                            : BalanceTuningGoals.TuningPriority.High,
                        Category = "weapon",
                        Target = worstWeapon.Key.ToString(),
                        Parameter = "DamageMultiplier",
                        CurrentValue = 1.0, // Would need to get actual weapon scaling
                        SuggestedValue = 1.0 + magnitude,
                        AdjustmentMagnitude = magnitude * 100.0,
                        Reason = $"{worstWeapon.Key} has {worstWinRate:F1}% win rate (worst weapon). Weapon balance variance is {analysis.WeaponVariance:F1}%.",
                        Impact = $"Buff {worstWeapon.Key} damage by {magnitude * 100.0:F1}% to improve balance",
                        AffectedMatchups = testResult.CombinationResults
                            .Where(c => c.WeaponType == worstWeapon.Key)
                            .Select(c => $"{c.WeaponType} vs {c.EnemyType}")
                            .ToList()
                    });
                }

                // Suggest nerfing best weapon (if it's too strong)
                if (bestWinRate > BalanceTuningGoals.WinRateTargets.OptimalMax)
                {
                    var magnitude = BalanceTuningGoals.GetAdjustmentMagnitude(bestWinRate, targetWinRate, 0.05, 0.30);
                    suggestions.Add(new TuningSuggestion
                    {
                        Id = $"weapon_nerf_{bestWeapon.Key}",
                        Priority = bestWinRate > BalanceTuningGoals.WinRateTargets.CriticalHigh 
                            ? BalanceTuningGoals.TuningPriority.Critical 
                            : BalanceTuningGoals.TuningPriority.High,
                        Category = "weapon",
                        Target = bestWeapon.Key.ToString(),
                        Parameter = "DamageMultiplier",
                        CurrentValue = 1.0,
                        SuggestedValue = 1.0 - magnitude,
                        AdjustmentMagnitude = magnitude * 100.0,
                        Reason = $"{bestWeapon.Key} has {bestWinRate:F1}% win rate (best weapon). Weapon balance variance is {analysis.WeaponVariance:F1}%.",
                        Impact = $"Nerf {bestWeapon.Key} damage by {magnitude * 100.0:F1}% to improve balance",
                        AffectedMatchups = testResult.CombinationResults
                            .Where(c => c.WeaponType == bestWeapon.Key)
                            .Select(c => $"{c.WeaponType} vs {c.EnemyType}")
                            .ToList()
                    });
                }
            }

            return suggestions;
        }

        /// <summary>
        /// Suggest enemy-specific adjustments
        /// </summary>
        private static List<TuningSuggestion> SuggestEnemyAdjustments(
            BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult testResult,
            TuningAnalysis analysis)
        {
            var suggestions = new List<TuningSuggestion>();
            var config = GameConfiguration.Instance;

            // Find problematic matchups
            var problematicMatchups = testResult.CombinationResults
                .Where(c => c.WinRate < BalanceTuningGoals.WinRateTargets.MinTarget || 
                           c.WinRate > BalanceTuningGoals.WinRateTargets.MaxTarget)
                .OrderBy(c => Math.Abs(c.WinRate - 91.5)) // Distance from optimal (91.5% = middle of 85-98%)
                .Take(5) // Top 5 most problematic
                .ToList();

            foreach (var matchup in problematicMatchups)
            {
                var priority = BalanceTuningGoals.GetMatchupPriority(matchup.WinRate, matchup.AverageTurns);
                
                if (matchup.WinRate < BalanceTuningGoals.WinRateTargets.MinTarget)
                {
                    // Enemy too strong - find archetype and suggest reduction
                    // Note: This is simplified - would need to map enemy type to archetype
                    var magnitude = BalanceTuningGoals.GetAdjustmentMagnitude(
                        matchup.WinRate, 
                        BalanceTuningGoals.WinRateTargets.OptimalMin,
                        0.05, 0.25);

                    suggestions.Add(new TuningSuggestion
                    {
                        Id = $"enemy_nerf_{matchup.EnemyType}",
                        Priority = priority,
                        Category = "enemy",
                        Target = matchup.EnemyType,
                        Parameter = "HealthMultiplier",
                        CurrentValue = 1.0, // Would need actual enemy config
                        SuggestedValue = 1.0 - magnitude,
                        AdjustmentMagnitude = magnitude * 100.0,
                        Reason = $"{matchup.WeaponType} vs {matchup.EnemyType}: {matchup.WinRate:F1}% win rate (below 85% target)",
                        Impact = $"Reduce {matchup.EnemyType} health by {magnitude * 100.0:F1}%",
                        AffectedMatchups = new List<string> { $"{matchup.WeaponType} vs {matchup.EnemyType}" }
                    });
                }
                else if (matchup.WinRate > BalanceTuningGoals.WinRateTargets.MaxTarget)
                {
                    // Enemy too weak
                    var magnitude = BalanceTuningGoals.GetAdjustmentMagnitude(
                        matchup.WinRate, 
                        BalanceTuningGoals.WinRateTargets.OptimalMax,
                        0.05, 0.25);

                    suggestions.Add(new TuningSuggestion
                    {
                        Id = $"enemy_buff_{matchup.EnemyType}",
                        Priority = priority,
                        Category = "enemy",
                        Target = matchup.EnemyType,
                        Parameter = "HealthMultiplier",
                        CurrentValue = 1.0,
                        SuggestedValue = 1.0 + magnitude,
                        AdjustmentMagnitude = magnitude * 100.0,
                        Reason = $"{matchup.WeaponType} vs {matchup.EnemyType}: {matchup.WinRate:F1}% win rate (above 98% target)",
                        Impact = $"Increase {matchup.EnemyType} health by {magnitude * 100.0:F1}%",
                        AffectedMatchups = new List<string> { $"{matchup.WeaponType} vs {matchup.EnemyType}" }
                    });
                }
            }

            return suggestions;
        }

        /// <summary>
        /// Suggest combat duration adjustments
        /// </summary>
        private static List<TuningSuggestion> SuggestDurationAdjustments(
            BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult testResult,
            TuningAnalysis analysis)
        {
            var suggestions = new List<TuningSuggestion>();

            if (analysis.AverageCombatDuration < BalanceTuningGoals.CombatDurationTargets.MinTarget)
            {
                // Combat too short - increase enemy health or reduce damage
                var magnitude = (BalanceTuningGoals.CombatDurationTargets.OptimalMin - analysis.AverageCombatDuration) / 
                               BalanceTuningGoals.CombatDurationTargets.OptimalMin;
                magnitude = Math.Clamp(magnitude, 0.05, 0.30);

                suggestions.Add(new TuningSuggestion
                {
                    Id = "duration_increase_health",
                    Priority = analysis.AverageCombatDuration < BalanceTuningGoals.CombatDurationTargets.CriticalShort 
                        ? BalanceTuningGoals.TuningPriority.Critical 
                        : BalanceTuningGoals.TuningPriority.High,
                    Category = "global",
                    Target = "All Enemies",
                    Parameter = "HealthMultiplier",
                    CurrentValue = GameConfiguration.Instance.EnemySystem.GlobalMultipliers.HealthMultiplier,
                    SuggestedValue = GameConfiguration.Instance.EnemySystem.GlobalMultipliers.HealthMultiplier * (1.0 + magnitude),
                    AdjustmentMagnitude = magnitude * 100.0,
                    Reason = $"Average combat duration {analysis.AverageCombatDuration:F1} turns is below target (8-15 turns)",
                    Impact = $"Increase enemy health by {magnitude * 100.0:F1}% to lengthen combat",
                    AffectedMatchups = testResult.CombinationResults.Select(c => $"{c.WeaponType} vs {c.EnemyType}").ToList()
                });
            }
            else if (analysis.AverageCombatDuration > BalanceTuningGoals.CombatDurationTargets.MaxTarget)
            {
                // Combat too long - reduce enemy health or increase damage
                var magnitude = (analysis.AverageCombatDuration - BalanceTuningGoals.CombatDurationTargets.OptimalMax) / 
                               BalanceTuningGoals.CombatDurationTargets.OptimalMax;
                magnitude = Math.Clamp(magnitude, 0.05, 0.30);

                suggestions.Add(new TuningSuggestion
                {
                    Id = "duration_decrease_health",
                    Priority = analysis.AverageCombatDuration > BalanceTuningGoals.CombatDurationTargets.CriticalLong 
                        ? BalanceTuningGoals.TuningPriority.Critical 
                        : BalanceTuningGoals.TuningPriority.High,
                    Category = "global",
                    Target = "All Enemies",
                    Parameter = "HealthMultiplier",
                    CurrentValue = GameConfiguration.Instance.EnemySystem.GlobalMultipliers.HealthMultiplier,
                    SuggestedValue = GameConfiguration.Instance.EnemySystem.GlobalMultipliers.HealthMultiplier * (1.0 - magnitude),
                    AdjustmentMagnitude = magnitude * 100.0,
                    Reason = $"Average combat duration {analysis.AverageCombatDuration:F1} turns is above target (8-15 turns)",
                    Impact = $"Reduce enemy health by {magnitude * 100.0:F1}% to shorten combat",
                    AffectedMatchups = testResult.CombinationResults.Select(c => $"{c.WeaponType} vs {c.EnemyType}").ToList()
                });
            }

            return suggestions;
        }

        /// <summary>
        /// Generate summary text
        /// </summary>
        private static string GenerateSummary(TuningAnalysis analysis, List<TuningSuggestion> suggestions)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Balance Quality Score: {analysis.QualityScore:F1}/100");
            sb.AppendLine($"Overall Win Rate: {analysis.OverallWinRate:F1}% (Target: 85-98%)");
            sb.AppendLine($"Average Combat Duration: {analysis.AverageCombatDuration:F1} turns (Target: 8-15)");
            sb.AppendLine($"Weapon Balance Variance: {analysis.WeaponVariance:F1}% (Target: <10%)");
            sb.AppendLine($"Enemy Differentiation: {analysis.EnemyVariance:F1}% (Target: >3%)");
            sb.AppendLine();
            sb.AppendLine($"Tuning Suggestions: {suggestions.Count} total");
            foreach (var priority in Enum.GetValues<BalanceTuningGoals.TuningPriority>())
            {
                var count = suggestions.Count(s => s.Priority == priority);
                if (count > 0)
                {
                    sb.AppendLine($"  {priority}: {count} suggestions");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Apply a tuning suggestion
        /// </summary>
        public static bool ApplySuggestion(TuningSuggestion suggestion)
        {
            try
            {
                switch (suggestion.Category)
                {
                    case "global":
                        if (suggestion.Parameter == "HealthMultiplier" || suggestion.Parameter == "health")
                        {
                            return BalanceTuningConsole.AdjustGlobalEnemyMultiplier("health", suggestion.SuggestedValue);
                        }
                        else if (suggestion.Parameter == "DamageMultiplier" || suggestion.Parameter == "damage")
                        {
                            return BalanceTuningConsole.AdjustGlobalEnemyMultiplier("damage", suggestion.SuggestedValue);
                        }
                        else if (suggestion.Parameter == "ArmorMultiplier" || suggestion.Parameter == "armor")
                        {
                            return BalanceTuningConsole.AdjustGlobalEnemyMultiplier("armor", suggestion.SuggestedValue);
                        }
                        else if (suggestion.Parameter == "SpeedMultiplier" || suggestion.Parameter == "speed")
                        {
                            return BalanceTuningConsole.AdjustGlobalEnemyMultiplier("speed", suggestion.SuggestedValue);
                        }
                        break;

                    case "player":
                        if (suggestion.Parameter == "BaseStrength")
                        {
                            return BalanceTuningConsole.AdjustPlayerBaseAttribute("strength", (int)suggestion.SuggestedValue);
                        }
                        else if (suggestion.Parameter == "BaseAgility")
                        {
                            return BalanceTuningConsole.AdjustPlayerBaseAttribute("agility", (int)suggestion.SuggestedValue);
                        }
                        else if (suggestion.Parameter == "BaseTechnique")
                        {
                            return BalanceTuningConsole.AdjustPlayerBaseAttribute("technique", (int)suggestion.SuggestedValue);
                        }
                        else if (suggestion.Parameter == "BaseIntelligence")
                        {
                            return BalanceTuningConsole.AdjustPlayerBaseAttribute("intelligence", (int)suggestion.SuggestedValue);
                        }
                        else if (suggestion.Parameter == "BaseHealth")
                        {
                            return BalanceTuningConsole.AdjustPlayerBaseHealth((int)suggestion.SuggestedValue);
                        }
                        else if (suggestion.Parameter == "AttributesPerLevel")
                        {
                            return BalanceTuningConsole.AdjustPlayerAttributesPerLevel((int)suggestion.SuggestedValue);
                        }
                        else if (suggestion.Parameter == "HealthPerLevel")
                        {
                            return BalanceTuningConsole.AdjustPlayerHealthPerLevel((int)suggestion.SuggestedValue);
                        }
                        break;

                    case "enemy_baseline":
                        if (suggestion.Parameter == "BaselineStrength")
                        {
                            return BalanceTuningConsole.AdjustEnemyBaselineStat("strength", suggestion.SuggestedValue);
                        }
                        else if (suggestion.Parameter == "BaselineAgility")
                        {
                            return BalanceTuningConsole.AdjustEnemyBaselineStat("agility", suggestion.SuggestedValue);
                        }
                        else if (suggestion.Parameter == "BaselineTechnique")
                        {
                            return BalanceTuningConsole.AdjustEnemyBaselineStat("technique", suggestion.SuggestedValue);
                        }
                        else if (suggestion.Parameter == "BaselineIntelligence")
                        {
                            return BalanceTuningConsole.AdjustEnemyBaselineStat("intelligence", suggestion.SuggestedValue);
                        }
                        else if (suggestion.Parameter == "BaselineHealth")
                        {
                            return BalanceTuningConsole.AdjustEnemyBaselineStat("health", suggestion.SuggestedValue);
                        }
                        else if (suggestion.Parameter == "BaselineArmor")
                        {
                            return BalanceTuningConsole.AdjustEnemyBaselineStat("armor", suggestion.SuggestedValue);
                        }
                        break;

                    case "archetype":
                        // Would need to map enemy type to archetype
                        // For now, this is a placeholder
                        ScrollDebugLogger.Log($"AutomatedTuningEngine: Archetype adjustments not yet fully implemented");
                        break;

                    case "weapon":
                        return BalanceTuningConsole.AdjustWeaponScaling(suggestion.Target, "damage", suggestion.SuggestedValue);

                    case "enemy":
                        // Enemy-specific adjustments would require modifying Enemies.json
                        ScrollDebugLogger.Log($"AutomatedTuningEngine: Enemy-specific adjustments require Enemies.json modification");
                        break;
                }

                return false;
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"AutomatedTuningEngine: Error applying suggestion: {ex.Message}");
                return false;
            }
        }
    }
}

