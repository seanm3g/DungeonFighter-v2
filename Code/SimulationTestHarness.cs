using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Simulation;

namespace RPGGame.SimulationTest
{
    /// <summary>
    /// Quick test harness to demonstrate combat simulation
    /// Run with: dotnet run --project Code -- test-simulation
    /// </summary>
    public class SimulationTestHarness
    {
        public static void RunSimulationDemo()
        {
            Console.WriteLine("=".PadRight(60, '='));
            Console.WriteLine("DUNGEONF IGHTER COMBAT SIMULATION DEMO");
            Console.WriteLine("=".PadRight(60, '='));
            Console.WriteLine();

            // Step 1: Create test characters
            Console.WriteLine("STEP 1: Setting up test scenario");
            Console.WriteLine("-".PadRight(60, '-'));
            
            var player = CreateTestPlayer();
            var enemies = CreateTestEnemies();
            
            Console.WriteLine($"Player: {player.Name} (Level {player.Level}, Health: {player.CurrentHealth})");
            Console.WriteLine($"Enemies to test against:");
            foreach (var enemy in enemies)
            {
                Console.WriteLine($"  - {enemy.Name} (Level {enemy.Level}, Health: {enemy.CurrentHealth})");
            }
            Console.WriteLine();

            // Step 2: Run simulations
            Console.WriteLine("STEP 2: Running battle simulations (50 per enemy)");
            Console.WriteLine("-".PadRight(60, '-'));
            
            var batch = BatchSimulationRunner.RunSimulationBatch(player, enemies, 5, "Demo Test");
            
            Console.WriteLine($"Total simulations: {batch.TotalSimulations}");
            Console.WriteLine($"Player wins: {batch.PlayerWins}");
            Console.WriteLine($"Player losses: {batch.PlayerLosses}");
            Console.WriteLine($"Win rate: {(double)batch.PlayerWins / batch.TotalSimulations:P1}");
            Console.WriteLine();
            
            Console.WriteLine("TURN STATISTICS:");
            Console.WriteLine($"  Average: {batch.AverageTurns:F1} turns");
            Console.WriteLine($"  Min: {batch.MinTurns} turns");
            Console.WriteLine($"  Max: {batch.MaxTurns} turns");
            Console.WriteLine($"  Std Dev: {batch.StdDevTurns:F2}");
            Console.WriteLine();
            
            Console.WriteLine("BUILD CATEGORIZATION:");
            Console.WriteLine($"  Good Builds (≤6 turns): {batch.WinsIn6OrFewer}");
            Console.WriteLine($"  Target Range (6-14 turns): {batch.WinsInTargetRange}");
            Console.WriteLine($"  Struggling (≥14 turns): {batch.WinsIn14OrMore}");
            Console.WriteLine();
            
            Console.WriteLine("PHASE DISTRIBUTION:");
            Console.WriteLine($"  Phase 1 (100%→66%): {batch.AveragePhase1Turns:F1} turns avg");
            Console.WriteLine($"  Phase 2 (66%→33%): {batch.AveragePhase2Turns:F1} turns avg");
            Console.WriteLine($"  Phase 3 (33%→0%): {batch.AveragePhase3Turns:F1} turns avg");
            Console.WriteLine();

            // Step 3: Analyze results
            Console.WriteLine("STEP 3: Analyzing results");
            Console.WriteLine("-".PadRight(60, '-'));
            
            var analysis = SimulationAnalyzer.AnalyzeBatch(batch);
            
            Console.WriteLine(SimulationAnalyzer.FormatAnalysisReport(analysis));
            Console.WriteLine();

            // Summary
            Console.WriteLine("=".PadRight(60, '='));
            Console.WriteLine("SUMMARY");
            Console.WriteLine("=".PadRight(60, '='));
            
            double winRate = (double)batch.PlayerWins / batch.TotalSimulations;
            string status = winRate < 0.5 ? "TOO HARD (need stronger player)" :
                           winRate > 0.7 ? "TOO EASY (need stronger enemies)" :
                           "BALANCED ✓";
            
            string durationStatus = batch.AverageTurns < 6 ? "TOO SHORT (increase enemy health)" :
                                   batch.AverageTurns > 14 ? "TOO LONG (increase player damage)" :
                                   "PERFECT ✓";
            
            Console.WriteLine($"Win Rate Status: {status}");
            Console.WriteLine($"Duration Status: {durationStatus}");
            Console.WriteLine($"Overall Assessment: {analysis.Summary}");
            Console.WriteLine();
            
            if (analysis.Issues.Count > 0)
            {
                Console.WriteLine("IDENTIFIED ISSUES:");
                foreach (var issue in analysis.Issues)
                {
                    Console.WriteLine($"  ⚠ {issue}");
                }
                Console.WriteLine();
            }
            
            if (analysis.Suggestions.Count > 0)
            {
                Console.WriteLine("RECOMMENDATIONS:");
                foreach (var suggestion in analysis.Suggestions.Take(3))
                {
                    Console.WriteLine($"  → {suggestion}");
                }
                Console.WriteLine();
            }

            Console.WriteLine("=".PadRight(60, '='));
        }

        private static Character CreateTestPlayer()
        {
            var player = new Character("TestWarrior", 1);
            // Initialize with basic stats
            return player;
        }

        private static List<Enemy> CreateTestEnemies()
        {
            var enemies = new List<Enemy>();
            
            // Create a few test enemies with different stats
            // Explicitly use the constructor with individual stats by providing strength parameter
            var goblin = new Enemy("Goblin", 1, 50, 8, 6, 4, 4);
            var orc = new Enemy("Orc", 1, 60, 10, 5, 3, 3);
            var skeleton = new Enemy("Skeleton", 1, 45, 7, 7, 5, 4);
            
            enemies.Add(goblin);
            enemies.Add(orc);
            enemies.Add(skeleton);
            
            return enemies;
        }
    }
}
