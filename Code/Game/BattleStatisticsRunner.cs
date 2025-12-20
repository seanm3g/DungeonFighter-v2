using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using RPGGame.Combat;

namespace RPGGame
{
    /// <summary>
    /// Runs parallel battles for statistical analysis on different stat combinations
    /// Facade coordinating battle execution, statistics calculation, and weapon testing
    /// </summary>
    public class BattleStatisticsRunner
    {
        // Nested classes for backward compatibility
        public class BattleResult : RPGGame.BattleStatistics.BattleResult { }
        public class BattleConfiguration : RPGGame.BattleStatistics.BattleConfiguration { }
        public class StatisticsResult : RPGGame.BattleStatistics.StatisticsResult { }
        public class WeaponTestResult : RPGGame.BattleStatistics.WeaponTestResult { }
        public class WeaponEnemyCombinationResult : RPGGame.BattleStatistics.WeaponEnemyCombinationResult { }
        public class WeaponOverallStats : RPGGame.BattleStatistics.WeaponOverallStats { }
        public class EnemyOverallStats : RPGGame.BattleStatistics.EnemyOverallStats { }
        public class ComprehensiveWeaponEnemyTestResult : RPGGame.BattleStatistics.ComprehensiveWeaponEnemyTestResult { }

        /// <summary>
        /// Runs parallel battles for a given configuration
        /// </summary>
        public static async Task<StatisticsResult> RunParallelBattles(
            BattleConfiguration config,
            int numberOfBattles = 100,
            IProgress<(int completed, int total, string status)>? progress = null)
        {
            const int TIMEOUT_MINUTES = 15;
            var result = new StatisticsResult
            {
                Config = config,
                TotalBattles = numberOfBattles
            };

            var originalDisableFlag = CombatManager.DisableCombatUIOutput;
            CombatManager.DisableCombatUIOutput = true;

            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(TIMEOUT_MINUTES));

            try
            {
                var battleTasks = new List<Task<RPGGame.BattleStatistics.BattleResult>>();
                var semaphore = new System.Threading.SemaphoreSlim(System.Environment.ProcessorCount * 2, System.Environment.ProcessorCount * 2);

                for (int i = 0; i < numberOfBattles; i++)
                {
                    int battleIndex = i;
                    var task = Task.Run(async () =>
                    {
                        await semaphore.WaitAsync(cts.Token);
                        try
                        {
                            var battleResult = await RPGGame.BattleStatistics.BattleExecutor.RunSingleBattle(config, battleIndex);
                            progress?.Report((battleIndex + 1, numberOfBattles, $"Battle {battleIndex + 1}/{numberOfBattles}"));
                            return battleResult;
                        }
                        catch (OperationCanceledException)
                        {
                            var errorResult = new RPGGame.BattleStatistics.BattleResult
                            {
                                ErrorMessage = "Battle cancelled (overall timeout)",
                                PlayerWon = false,
                                Turns = 0
                            };
                            progress?.Report((battleIndex + 1, numberOfBattles, $"Battle {battleIndex + 1}/{numberOfBattles} (Cancelled)"));
                            return errorResult;
                        }
                        catch (Exception ex)
                        {
                            var errorResult = new RPGGame.BattleStatistics.BattleResult
                            {
                                ErrorMessage = $"Task exception: {ex.GetType().Name}: {ex.Message}",
                                PlayerWon = false,
                                Turns = 0
                            };
                            Utils.ScrollDebugLogger.Log($"BattleStatisticsRunner: Battle {battleIndex} exception: {ex.GetType().Name}: {ex.Message}");
                            progress?.Report((battleIndex + 1, numberOfBattles, $"Battle {battleIndex + 1}/{numberOfBattles} (Error)"));
                            return errorResult;
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }, cts.Token);
                    battleTasks.Add(task);
                }

                try
                {
                    var results = await Task.WhenAll(battleTasks).ConfigureAwait(false);
                    result.BattleResults = results.ToList();
                    Utils.ScrollDebugLogger.Log($"BattleStatisticsRunner: All {numberOfBattles} battles completed successfully");
                }
                catch (OperationCanceledException)
                {
                    Utils.ScrollDebugLogger.Log($"BattleStatisticsRunner: Battle suite timed out after {TIMEOUT_MINUTES} minutes");
                    var completedResults = new List<RPGGame.BattleStatistics.BattleResult>();
                    for (int i = 0; i < battleTasks.Count; i++)
                    {
                        if (battleTasks[i].IsCompleted && !battleTasks[i].IsFaulted && !battleTasks[i].IsCanceled)
                        {
                            try
                            {
                                var baseResult = await battleTasks[i];
                                completedResults.Add(baseResult);
                            }
                            catch (Exception ex)
                            {
                                Utils.ScrollDebugLogger.Log($"BattleStatisticsRunner: Error collecting battle {i} result: {ex.Message}");
                                completedResults.Add(new RPGGame.BattleStatistics.BattleResult { ErrorMessage = "Task failed", PlayerWon = false });
                            }
                        }
                        else
                        {
                            completedResults.Add(new RPGGame.BattleStatistics.BattleResult { ErrorMessage = $"Task incomplete/failed (state: {GetTaskState(battleTasks[i])})", PlayerWon = false });
                        }
                    }
                    result.BattleResults = completedResults;
                }

                RPGGame.BattleStatistics.BattleStatisticsCalculator.CalculateStatistics(result);
            }
            finally
            {
                CombatManager.DisableCombatUIOutput = originalDisableFlag;
            }

            return result;
        }

        /// <summary>
        /// Helper method to get task state for logging
        /// </summary>
        private static string GetTaskState(Task task)
        {
            if (task.IsFaulted) return "Faulted";
            if (task.IsCanceled) return "Cancelled";
            if (task.IsCompleted) return "Completed";
            if (task.IsCompletedSuccessfully) return "CompletedSuccessfully";
            return "Running";
        }

        /// <summary>
        /// Runs multiple configurations in parallel for comparison
        /// </summary>
        public static async Task<List<StatisticsResult>> RunMultipleConfigurations(
            List<BattleConfiguration> configurations,
            int battlesPerConfig = 100,
            IProgress<(int completed, int total, string status)>? progress = null)
        {
            var results = new List<StatisticsResult>();
            
            for (int i = 0; i < configurations.Count; i++)
            {
                var config = configurations[i];
                progress?.Report((i, configurations.Count, $"Configuration {i + 1}/{configurations.Count}"));
                
                var result = await RunParallelBattles(config, battlesPerConfig, 
                    new Progress<(int, int, string)>(p => 
                    {
                        progress?.Report((i * battlesPerConfig + p.Item1, configurations.Count * battlesPerConfig, 
                            $"Config {i + 1}/{configurations.Count}: {p.Item3}"));
                    }));
                
                results.Add(result);
            }
            
            return results;
        }

        /// <summary>
        /// Runs battle tests for each weapon type against random enemies
        /// </summary>
        public static async Task<List<WeaponTestResult>> RunWeaponTypeTests(
            int battlesPerWeapon = 50,
            int playerLevel = 1,
            int enemyLevel = 1,
            IProgress<(int completed, int total, string status)>? progress = null)
        {
            var results = await RPGGame.BattleStatistics.WeaponTestRunner.RunWeaponTypeTests(battlesPerWeapon, playerLevel, enemyLevel, progress);
            return results.Select(r => new WeaponTestResult
            {
                WeaponType = r.WeaponType,
                TotalBattles = r.TotalBattles,
                PlayerWins = r.PlayerWins,
                EnemyWins = r.EnemyWins,
                WinRate = r.WinRate,
                AverageTurns = r.AverageTurns,
                AveragePlayerDamageDealt = r.AveragePlayerDamageDealt,
                AverageEnemyDamageDealt = r.AverageEnemyDamageDealt,
                MinTurns = r.MinTurns,
                MaxTurns = r.MaxTurns,
                BattleResults = r.BattleResults
            }).ToList();
        }

        /// <summary>
        /// Runs comprehensive tests: every weapon against every enemy
        /// </summary>
        public static async Task<ComprehensiveWeaponEnemyTestResult> RunComprehensiveWeaponEnemyTests(
            int battlesPerCombination = 10,
            int playerLevel = 1,
            int enemyLevel = 1,
            IProgress<(int completed, int total, string status)>? progress = null)
        {
            var baseResult = await RPGGame.BattleStatistics.WeaponTestRunner.RunComprehensiveWeaponEnemyTests(battlesPerCombination, playerLevel, enemyLevel, progress);
            // Create wrapper instance - assign all properties including CombinationResults from base
            var result = new ComprehensiveWeaponEnemyTestResult
            {
                WeaponTypes = baseResult.WeaponTypes,
                EnemyTypes = baseResult.EnemyTypes,
                TotalBattles = baseResult.TotalBattles,
                TotalPlayerWins = baseResult.TotalPlayerWins,
                TotalEnemyWins = baseResult.TotalEnemyWins,
                OverallWinRate = baseResult.OverallWinRate,
                OverallAverageTurns = baseResult.OverallAverageTurns,
                OverallAveragePlayerDamage = baseResult.OverallAveragePlayerDamage,
                OverallAverageEnemyDamage = baseResult.OverallAverageEnemyDamage,
                OverallAverageFunScore = baseResult.OverallAverageFunScore,
                OverallAverageActionVariety = baseResult.OverallAverageActionVariety,
                OverallAverageTurnVariance = baseResult.OverallAverageTurnVariance,
                TotalHealthLeadChanges = baseResult.TotalHealthLeadChanges,
                TotalComebacks = baseResult.TotalComebacks,
                TotalCloseCalls = baseResult.TotalCloseCalls,
                WeaponStatistics = baseResult.WeaponStatistics,
                EnemyStatistics = baseResult.EnemyStatistics
            };
            // CombinationResults property is inherited from base class with type List<RPGGame.BattleStatistics.WeaponEnemyCombinationResult>
            // We can assign the base list directly since the property type matches
            var propertyInfo = typeof(RPGGame.BattleStatistics.ComprehensiveWeaponEnemyTestResult).GetProperty("CombinationResults", BindingFlags.Public | BindingFlags.Instance);
            if (propertyInfo != null)
            {
                propertyInfo.SetValue(result, baseResult.CombinationResults);
            }
            return result;
        }
    }
}
