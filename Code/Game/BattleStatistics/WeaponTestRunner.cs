using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RPGGame.Combat;
using RPGGame.Data;
using RPGGame.Entity;
using RPGGame.Utils;

namespace RPGGame.BattleStatistics
{
    /// <summary>
    /// Runs weapon-specific battle tests
    /// </summary>
    public static class WeaponTestRunner
    {
        /// <summary>
        /// Runs battle tests for each weapon type against random enemies
        /// </summary>
        public static async Task<List<WeaponTestResult>> RunWeaponTypeTests(
            int battlesPerWeapon = 50,
            int playerLevel = 1,
            int enemyLevel = 1,
            IProgress<(int completed, int total, string status)>? progress = null)
        {
            var originalDisableFlag = CombatManager.DisableCombatUIOutput;
            CombatManager.DisableCombatUIOutput = true;
            
            try
            {
                var results = new List<WeaponTestResult>();
                var allWeaponTypes = new List<WeaponType> { WeaponType.Mace, WeaponType.Sword, WeaponType.Dagger, WeaponType.Wand };
                var allEnemyTypes = EnemyLoader.GetAllEnemyTypes();
                
                if (allEnemyTypes.Count == 0)
                {
                    allEnemyTypes = new List<string> { "Goblin" };
                }
                
                int totalBattles = allWeaponTypes.Count * battlesPerWeapon;
                int completedBattles = 0;
                
                foreach (var weaponType in allWeaponTypes)
                {
                    var weaponResult = new WeaponTestResult
                    {
                        WeaponType = weaponType,
                        TotalBattles = battlesPerWeapon
                    };
                    
                    var battleTasks = new List<Task<BattleResult>>();
                    var semaphore = new System.Threading.SemaphoreSlim(System.Environment.ProcessorCount * 2, System.Environment.ProcessorCount * 2);
                    
                    for (int i = 0; i < battlesPerWeapon; i++)
                    {
                        int battleIndex = i;
                        var task = Task.Run(async () =>
                        {
                            await semaphore.WaitAsync();
                            try
                            {
                                var randomEnemyType = allEnemyTypes[RandomUtility.Next(allEnemyTypes.Count)];
                                var battleResult = await BattleExecutor.RunSingleBattleWithWeapon(weaponType, randomEnemyType, playerLevel, enemyLevel, battleIndex);
                                
                                completedBattles++;
                                progress?.Report((completedBattles, totalBattles, 
                                    $"{weaponType} vs {randomEnemyType} ({completedBattles}/{totalBattles})"));
                                
                                return battleResult;
                            }
                            catch (Exception ex)
                            {
                                var errorResult = new BattleResult
                                {
                                    ErrorMessage = $"Exception: {ex.GetType().Name}: {ex.Message}",
                                    PlayerWon = false,
                                    Turns = 0
                                };
                                completedBattles++;
                                progress?.Report((completedBattles, totalBattles, 
                                    $"{weaponType} - Error ({completedBattles}/{totalBattles})"));
                                return errorResult;
                            }
                            finally
                            {
                                semaphore.Release();
                            }
                        });
                        battleTasks.Add(task);
                    }
                    
                    var allResults = await Task.WhenAll(battleTasks);
                    weaponResult.BattleResults = allResults.ToList();
                    
                    BattleStatisticsCalculator.CalculateWeaponStatistics(weaponResult);
                    results.Add(weaponResult);
                }
                
                return results;
            }
            finally
            {
                CombatManager.DisableCombatUIOutput = originalDisableFlag;
            }
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
            var originalDisableFlag = CombatManager.DisableCombatUIOutput;
            CombatManager.DisableCombatUIOutput = true;
            
            try
            {
                var result = new ComprehensiveWeaponEnemyTestResult();
                var allWeaponTypes = new List<WeaponType> { WeaponType.Mace, WeaponType.Sword, WeaponType.Dagger, WeaponType.Wand };
                var allEnemyTypes = EnemyLoader.GetAllEnemyTypes();
                
                ScrollDebugLogger.Log($"WeaponTestRunner: Starting comprehensive test - {allWeaponTypes.Count} primary weapons, {allEnemyTypes.Count} enemies");
            
                if (allEnemyTypes.Count == 0)
                {
                    allEnemyTypes = new List<string> { "Goblin" };
                    ScrollDebugLogger.Log("WeaponTestRunner: No enemies loaded, using fallback Goblin");
                }
            
                result.WeaponTypes = allWeaponTypes;
                result.EnemyTypes = allEnemyTypes;
            
                int totalBattles = allWeaponTypes.Count * allEnemyTypes.Count * battlesPerCombination;
                int completedBattles = 0;
                int totalCombinations = allWeaponTypes.Count * allEnemyTypes.Count;
                int completedCombinations = 0;
            
                ScrollDebugLogger.Log($"WeaponTestRunner: Total combinations: {totalCombinations}, Total battles: {totalBattles}");
            
                foreach (var weaponType in allWeaponTypes)
                {
                    foreach (var enemyType in allEnemyTypes)
                    {
                        completedCombinations++;
                        ScrollDebugLogger.Log($"WeaponTestRunner: Starting combination {completedCombinations}/{totalCombinations}: {weaponType} vs {enemyType}");
                    
                        var combinationResult = new WeaponEnemyCombinationResult
                        {
                            WeaponType = weaponType,
                            EnemyType = enemyType,
                            TotalBattles = battlesPerCombination
                        };
                    
                        var battleResults = new List<BattleResult>();

                        ScrollDebugLogger.Log($"WeaponTestRunner: Starting {battlesPerCombination} sequential battles: {weaponType} vs {enemyType}");
                        var sequentialStartTime = DateTime.Now;

                        for (int i = 0; i < battlesPerCombination; i++)
                        {
                            var battleStartTime = DateTime.Now;

                            try
                            {
                                ScrollDebugLogger.Log($"WeaponTestRunner: Battle {i + 1}/{battlesPerCombination} starting: {weaponType} vs {enemyType}");

                                var battleResult = await BattleExecutor.RunSingleBattleWithWeapon(weaponType, enemyType, playerLevel, enemyLevel, i);

                                var battleDuration = (DateTime.Now - battleStartTime).TotalSeconds;
                                completedBattles++;

                                ScrollDebugLogger.Log($"WeaponTestRunner: Battle {i + 1}/{battlesPerCombination} completed in {battleDuration:F2}s: {weaponType} vs {enemyType} - {(battleResult.ErrorMessage != null ? "ERROR" : battleResult.PlayerWon ? "WIN" : "LOSS")}");

                                progress?.Report((completedBattles, totalBattles,
                                    $"{weaponType} vs {enemyType} ({completedBattles}/{totalBattles})"));

                                battleResults.Add(battleResult);
                            }
                            catch (Exception ex)
                            {
                                var battleDuration = (DateTime.Now - battleStartTime).TotalSeconds;
                                ScrollDebugLogger.Log($"WeaponTestRunner: Battle {i + 1}/{battlesPerCombination} EXCEPTION after {battleDuration:F2}s: {weaponType} vs {enemyType} - {ex.GetType().Name}: {ex.Message}");

                                var errorResult = new BattleResult
                                {
                                    ErrorMessage = $"Exception: {ex.GetType().Name}: {ex.Message}",
                                    PlayerWon = false,
                                    Turns = 0
                                };
                                completedBattles++;
                                progress?.Report((completedBattles, totalBattles,
                                    $"{weaponType} vs {enemyType} - Error ({completedBattles}/{totalBattles})"));
                                battleResults.Add(errorResult);
                            }
                        }

                        var sequentialDuration = (DateTime.Now - sequentialStartTime).TotalSeconds;
                        ScrollDebugLogger.Log($"WeaponTestRunner: All {battlesPerCombination} battles completed in {sequentialDuration:F2}s: {weaponType} vs {enemyType}");

                        combinationResult.BattleResults = battleResults;
                    
                        BattleStatisticsCalculator.CalculateWeaponEnemyStatistics(combinationResult);
                        result.CombinationResults.Add(combinationResult);
                    
                        ScrollDebugLogger.Log($"WeaponTestRunner: Combination {completedCombinations}/{totalCombinations} finished: {weaponType} vs {enemyType} - Win Rate: {combinationResult.WinRate:F1}%");
                    }
                }
            
                ScrollDebugLogger.Log($"WeaponTestRunner: All combinations complete, calculating overall statistics");
            
                BattleStatisticsCalculator.CalculateComprehensiveStatistics(result);
                
                ScrollDebugLogger.Log($"WeaponTestRunner: Comprehensive test complete - Overall Win Rate: {result.OverallWinRate:F1}%");
                
                return result;
            }
            finally
            {
                CombatManager.DisableCombatUIOutput = originalDisableFlag;
            }
        }
    }
}

