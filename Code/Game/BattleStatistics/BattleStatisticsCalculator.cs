using System.Linq;

namespace RPGGame.BattleStatistics
{
    /// <summary>
    /// Calculates statistics from battle results
    /// </summary>
    public static class BattleStatisticsCalculator
    {
        /// <summary>
        /// Calculates statistics from battle results
        /// </summary>
        public static void CalculateStatistics(StatisticsResult result)
        {
            var validResults = result.BattleResults.Where(r => r.ErrorMessage == null).ToList();
            
            if (validResults.Count == 0)
            {
                return;
            }

            result.PlayerWins = validResults.Count(r => r.PlayerWon);
            result.EnemyWins = validResults.Count(r => !r.PlayerWon);
            result.WinRate = (double)result.PlayerWins / validResults.Count * 100.0;
            
            result.AverageTurns = validResults.Average(r => r.Turns);
            result.AveragePlayerDamageDealt = validResults.Average(r => r.PlayerDamageDealt);
            result.AverageEnemyDamageDealt = validResults.Average(r => r.EnemyDamageDealt);
            
            result.MinTurns = validResults.Min(r => r.Turns);
            result.MaxTurns = validResults.Max(r => r.Turns);
        }

        /// <summary>
        /// Calculates statistics for a weapon test result
        /// </summary>
        public static void CalculateWeaponStatistics(WeaponTestResult result)
        {
            var validResults = result.BattleResults.Where(r => r.ErrorMessage == null).ToList();
            
            if (validResults.Count == 0)
            {
                return;
            }
            
            result.PlayerWins = validResults.Count(r => r.PlayerWon);
            result.EnemyWins = validResults.Count(r => !r.PlayerWon);
            result.WinRate = (double)result.PlayerWins / validResults.Count * 100.0;
            
            result.AverageTurns = validResults.Average(r => r.Turns);
            result.AveragePlayerDamageDealt = validResults.Average(r => r.PlayerDamageDealt);
            result.AverageEnemyDamageDealt = validResults.Average(r => r.EnemyDamageDealt);
            
            result.MinTurns = validResults.Min(r => r.Turns);
            result.MaxTurns = validResults.Max(r => r.Turns);
        }

        /// <summary>
        /// Calculates statistics for a weapon-enemy combination
        /// </summary>
        public static void CalculateWeaponEnemyStatistics(WeaponEnemyCombinationResult result)
        {
            var validResults = result.BattleResults.Where(r => r.ErrorMessage == null).ToList();
            
            if (validResults.Count == 0)
            {
                return;
            }
            
            result.PlayerWins = validResults.Count(r => r.PlayerWon);
            result.EnemyWins = validResults.Count(r => !r.PlayerWon);
            result.WinRate = (double)result.PlayerWins / validResults.Count * 100.0;
            
            result.AverageTurns = validResults.Average(r => r.Turns);
            result.AveragePlayerDamageDealt = validResults.Average(r => r.PlayerDamageDealt);
            result.AverageEnemyDamageDealt = validResults.Average(r => r.EnemyDamageDealt);
            
            result.MinTurns = validResults.Min(r => r.Turns);
            result.MaxTurns = validResults.Max(r => r.Turns);
        }

        /// <summary>
        /// Calculates overall statistics for comprehensive test
        /// </summary>
        public static void CalculateComprehensiveStatistics(ComprehensiveWeaponEnemyTestResult result)
        {
            var allValidResults = result.CombinationResults
                .SelectMany(c => c.BattleResults)
                .Where(r => r.ErrorMessage == null)
                .ToList();
            
            if (allValidResults.Count == 0)
            {
                return;
            }
            
            result.TotalBattles = allValidResults.Count;
            result.TotalPlayerWins = allValidResults.Count(r => r.PlayerWon);
            result.TotalEnemyWins = allValidResults.Count(r => !r.PlayerWon);
            result.OverallWinRate = (double)result.TotalPlayerWins / allValidResults.Count * 100.0;
            result.OverallAverageTurns = allValidResults.Average(r => r.Turns);
            result.OverallAveragePlayerDamage = allValidResults.Average(r => r.PlayerDamageDealt);
            result.OverallAverageEnemyDamage = allValidResults.Average(r => r.EnemyDamageDealt);
            
            var funResults = allValidResults.Where(r => r.FunMomentSummary != null).ToList();
            if (funResults.Count > 0)
            {
                result.OverallAverageFunScore = funResults.Average(r => r.FunMomentSummary!.FunScore);
                result.OverallAverageActionVariety = funResults.Average(r => r.FunMomentSummary!.ActionVarietyScore);
                result.OverallAverageTurnVariance = funResults.Average(r => r.FunMomentSummary!.TurnVariance);
                result.TotalHealthLeadChanges = funResults.Sum(r => r.FunMomentSummary!.HealthLeadChanges);
                result.TotalComebacks = funResults.Sum(r => r.FunMomentSummary!.Comebacks);
                result.TotalCloseCalls = funResults.Sum(r => r.FunMomentSummary!.CloseCalls);
            }
            
            foreach (var weaponType in result.WeaponTypes)
            {
                var weaponResults = result.CombinationResults
                    .Where(c => c.WeaponType == weaponType)
                    .SelectMany(c => c.BattleResults)
                    .Where(r => r.ErrorMessage == null)
                    .ToList();
                
                if (weaponResults.Count > 0)
                {
                    var funWeaponResults = weaponResults.Where(r => r.FunMomentSummary != null).ToList();
                    result.WeaponStatistics[weaponType] = new WeaponOverallStats
                    {
                        TotalBattles = weaponResults.Count,
                        Wins = weaponResults.Count(r => r.PlayerWon),
                        WinRate = (double)weaponResults.Count(r => r.PlayerWon) / weaponResults.Count * 100.0,
                        AverageTurns = weaponResults.Average(r => r.Turns),
                        AverageDamage = weaponResults.Average(r => r.PlayerDamageDealt),
                        AverageFunScore = funWeaponResults.Count > 0 ? funWeaponResults.Average(r => r.FunMomentSummary!.FunScore) : 0.0,
                        AverageActionVariety = funWeaponResults.Count > 0 ? funWeaponResults.Average(r => r.FunMomentSummary!.ActionVarietyScore) : 0.0,
                        AverageHealthLeadChanges = funWeaponResults.Count > 0 ? funWeaponResults.Average(r => r.FunMomentSummary!.HealthLeadChanges) : 0.0
                    };
                }
            }
            
            foreach (var enemyType in result.EnemyTypes)
            {
                var enemyResults = result.CombinationResults
                    .Where(c => c.EnemyType == enemyType)
                    .SelectMany(c => c.BattleResults)
                    .Where(r => r.ErrorMessage == null)
                    .ToList();
                
                if (enemyResults.Count > 0)
                {
                    var funEnemyResults = enemyResults.Where(r => r.FunMomentSummary != null).ToList();
                    result.EnemyStatistics[enemyType] = new EnemyOverallStats
                    {
                        TotalBattles = enemyResults.Count,
                        Wins = enemyResults.Count(r => r.PlayerWon),
                        WinRate = (double)enemyResults.Count(r => r.PlayerWon) / enemyResults.Count * 100.0,
                        AverageTurns = enemyResults.Average(r => r.Turns),
                        AverageDamageReceived = enemyResults.Average(r => r.PlayerDamageDealt),
                        AverageFunScore = funEnemyResults.Count > 0 ? funEnemyResults.Average(r => r.FunMomentSummary!.FunScore) : 0.0,
                        AverageActionVariety = funEnemyResults.Count > 0 ? funEnemyResults.Average(r => r.FunMomentSummary!.ActionVarietyScore) : 0.0,
                        AverageHealthLeadChanges = funEnemyResults.Count > 0 ? funEnemyResults.Average(r => r.FunMomentSummary!.HealthLeadChanges) : 0.0
                    };
                }
            }
        }
    }
}

