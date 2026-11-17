using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;

namespace RPGGame.Simulation
{
    /// <summary>
    /// Runs batches of combat simulations for balance analysis and tuning
    /// Collects statistics across multiple scenarios
    /// </summary>
    public class BatchSimulationRunner
    {
        public class SimulationBatch
        {
            public string Name { get; set; } = string.Empty;
            public int TotalSimulations { get; set; }
            public int PlayerWins { get; set; }
            public int PlayerLosses { get; set; }
            
            // Turn statistics
            public double AverageTurns { get; set; }
            public int MinTurns { get; set; }
            public int MaxTurns { get; set; }
            public double StdDevTurns { get; set; }
            
            // Phase statistics
            public double AveragePhase1Turns { get; set; }
            public double AveragePhase2Turns { get; set; }
            public double AveragePhase3Turns { get; set; }
            
            // Damage statistics
            public double AveragePlayerDamagePerTurn { get; set; }
            public double AverageEnemyDamagePerTurn { get; set; }
            public double AverageFinalPlayerHealth { get; set; }
            
            // Win conditions
            public int WinsIn6OrFewer { get; set; }    // Good builds
            public int WinsIn14OrMore { get; set; }    // Struggling
            public int WinsInTargetRange { get; set; } // 6-14 turns
            
            public List<CombatSimulator.CombatSimulationResult> DetailedResults { get; set; }
            = new();
        }

        /// <summary>
        /// Simulates combat between a player and various enemies
        /// </summary>
        public static SimulationBatch RunSimulationBatch(
            Character player,
            List<Enemy> enemies,
            int repetitionsPerEnemy = 5,
            string batchName = "Test Batch")
        {
            var batch = new SimulationBatch { Name = batchName };
            var results = new List<CombatSimulator.CombatSimulationResult>();

            // Run simulations
            foreach (var enemy in enemies)
            {
                for (int i = 0; i < repetitionsPerEnemy; i++)
                {
                    // Reset states for clean simulation
                    player.CurrentHealth = player.GetEffectiveMaxHealth();
                    enemy.CurrentHealth = enemy.MaxHealth;

                    var result = CombatSimulator.SimulateCombat(player, enemy);
                    results.Add(result);
                    batch.TotalSimulations++;

                    if (result.PlayerWon)
                        batch.PlayerWins++;
                    else
                        batch.PlayerLosses++;
                }
            }

            batch.DetailedResults = results;

            // Calculate statistics
            if (results.Count > 0)
            {
                // Turn statistics
                batch.AverageTurns = results.Average(r => r.TurnsToComplete);
                batch.MinTurns = results.Min(r => r.TurnsToComplete);
                batch.MaxTurns = results.Max(r => r.TurnsToComplete);
                batch.StdDevTurns = CalculateStandardDeviation(results.Select(r => (double)r.TurnsToComplete));

                // Phase statistics (only from wins)
                var wins = results.Where(r => r.PlayerWon).ToList();
                if (wins.Count > 0)
                {
                    batch.AveragePhase1Turns = wins.Average(r => r.Phase1Turns);
                    batch.AveragePhase2Turns = wins.Average(r => r.Phase2Turns);
                    batch.AveragePhase3Turns = wins.Average(r => r.Phase3Turns);
                }

                // Damage statistics
                batch.AveragePlayerDamagePerTurn = results.Average(r => 
                    r.TurnsToComplete > 0 ? (double)r.TotalPlayerDamageDealt / r.TurnsToComplete : 0);
                batch.AverageEnemyDamagePerTurn = results.Average(r => 
                    r.TurnsToComplete > 0 ? (double)r.TotalEnemyDamageDealt / r.TurnsToComplete : 0);
                batch.AverageFinalPlayerHealth = results.Average(r => r.PlayerFinalHealth);

                // Win conditions
                batch.WinsIn6OrFewer = wins.Count(r => r.TurnsToComplete <= 6);
                batch.WinsInTargetRange = wins.Count(r => r.TurnsToComplete >= 6 && r.TurnsToComplete <= 14);
                batch.WinsIn14OrMore = wins.Count(r => r.TurnsToComplete >= 14);
            }

            return batch;
        }

        private static double CalculateStandardDeviation(IEnumerable<double> values)
        {
            var list = values.ToList();
            if (list.Count < 2)
                return 0;

            double mean = list.Average();
            double variance = list.Average(x => Math.Pow(x - mean, 2));
            return Math.Sqrt(variance);
        }
    }
}
