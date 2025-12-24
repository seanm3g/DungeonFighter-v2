using System;
using System.IO;
using System.Threading.Tasks;
using RPGGame;
using RPGGame.MCP;
using RPGGame.MCP.Tools;
using RPGGame.Config;

namespace RPGGame.Tuning
{
    /// <summary>
    /// Console application for running iterative balance tuning
    /// Usage: dotnet run -- TUNING [iteration_count]
    /// </summary>
    public class TuningRunner
    {
        public static async Task RunTuning(int iterations = 5)
        {
            Console.WriteLine("═════════════════════════════════════════");
            Console.WriteLine($"  DungeonFighter Balance Tuning Runner");
            Console.WriteLine($"  Running {iterations} Iterations");
            Console.WriteLine("  (Using sequential mode for stability)");
            Console.WriteLine("═════════════════════════════════════════\n");

            // Initialize game wrapper
            var gameWrapper = new GameWrapper();
            McpTools.SetGameWrapper(gameWrapper);

            // Pre-load all resources to avoid thread safety issues
            Console.WriteLine("Pre-loading game resources...\n");
            try
            {
                gameWrapper.InitializeGame();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Note: {ex.Message}\n");
            }

            try
            {
                for (int i = 1; i <= iterations; i++)
                {
                    Console.WriteLine($"\n{'='.ToString().PadRight(50, '=')}");
                    Console.WriteLine($"ITERATION {i}/{iterations}");
                    Console.WriteLine($"{'='.ToString().PadRight(50, '=')}\n");

                    // Phase 1: Baseline
                    Console.WriteLine("Phase 1: Establishing Baseline...");
                    var baselineResult = await RunBattleSimulation(25);  // Reduced from 50 for stability
                    await Task.Delay(500);
                    McpToolState.LastTestResult = baselineResult;
                    await AutomatedTuningTools.SetBaseline();
                    Console.WriteLine($"✓ Baseline set with {baselineResult.TotalBattles} battles\n");

                    // Phase 2: Analysis
                    Console.WriteLine("Phase 2: Analyzing Results...");
                    await PrintAnalysis();
                    await Task.Delay(500);

                    // Phase 3: Get Suggestions
                    Console.WriteLine("Phase 3: Getting Tuning Suggestions...");
                    var suggestions = await PrintSuggestions();
                    await Task.Delay(500);

                    // Phase 4: Apply First Suggestion (if available)
                    if (i < iterations) // Don't adjust on last iteration
                    {
                        Console.WriteLine($"\nPhase 4: Applying Adjustments...");
                        await ApplyIterationAdjustmentsAsync(i);
                        await Task.Delay(500);

                        // Phase 5: Test
                        Console.WriteLine($"Phase 5: Testing Changes...");
                        var testResult = await RunBattleSimulation(25);  // Reduced for stability
                        McpToolState.LastTestResult = testResult;

                        var qualityScore = await PrintBalanceQualityScore();
                        Console.WriteLine($"✓ Quality Score: {qualityScore}\n");

                        // Save patch
                        Console.WriteLine($"Phase 6: Saving Patch...");
                        await SaveIterationPatch(i);
                    }
                    else
                    {
                        Console.WriteLine($"\nFinal Iteration - Running comprehensive analysis...");
                        var finalResult = await RunBattleSimulation(50);  // Larger sample for final results
                        McpToolState.LastTestResult = finalResult;
                        var finalScore = await PrintBalanceQualityScore();
                        Console.WriteLine($"✓ Final Quality Score: {finalScore}\n");
                        await SaveIterationPatch(iterations);
                    }
                }

                Console.WriteLine("\n" + new string('=', 50));
                Console.WriteLine("✓ ALL ITERATIONS COMPLETE");
                Console.WriteLine(new string('=', 50));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n✗ Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private static async Task<BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult> RunBattleSimulation(int battlesPerCombination)
        {
            try
            {
                var progress = new System.Progress<(int completed, int total, string status)>(report =>
                {
                    // Calculate percentage complete
                    int percentage = report.total > 0 ? (int)((double)report.completed / report.total * 100) : 0;

                    // Create progress bar visualization
                    int barLength = 30;
                    int filledLength = report.total > 0 ? (int)((double)report.completed / report.total * barLength) : 0;
                    string bar = new string('█', filledLength) + new string('░', barLength - filledLength);

                    // Print progress bar with status
                    string progressLine = $"[{bar}] {percentage:D3}% - {report.status}";
                    Console.Write($"\r{progressLine,-100}");
                    Console.Out.Flush();

                    // Print newline when complete
                    if (report.completed >= report.total && report.total > 0)
                    {
                        Console.WriteLine();
                    }
                });

                var result = await BattleStatisticsRunner.RunComprehensiveWeaponEnemyTests(
                    battlesPerCombination, 1, 1, progress);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error running simulation: {ex.Message}");
                throw;
            }
        }

        private static async Task PrintAnalysis()
        {
            try
            {
                var result = await AnalysisTools.AnalyzeBattleResults();
                // Result is already printed by the tool
                Console.WriteLine(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error analyzing results: {ex.Message}");
            }
        }

        private static async Task<string> PrintSuggestions()
        {
            try
            {
                var result = await AutomatedTuningTools.SuggestTuning();
                Console.WriteLine(result);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting suggestions: {ex.Message}");
                return "";
            }
        }

        private static async Task<string> PrintBalanceQualityScore()
        {
            try
            {
                var result = await AutomatedTuningTools.GetBalanceQualityScore();
                Console.WriteLine(result);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting quality score: {ex.Message}");
                return "";
            }
        }

        private static async Task ApplyIterationAdjustmentsAsync(int iteration)
        {
            // Apply different adjustments for each iteration
            switch (iteration)
            {
                case 1:
                    Console.WriteLine("Adjusting: Global enemy health +10%");
                    await BalanceAdjustmentTools.AdjustGlobalEnemyMultiplier("health", 1.1);
                    break;
                case 2:
                    Console.WriteLine("Adjusting: Enemy damage +5%");
                    await BalanceAdjustmentTools.AdjustGlobalEnemyMultiplier("damage", 1.05);
                    break;
                case 3:
                    Console.WriteLine("Adjusting: Weapon scaling tuning");
                    await BalanceAdjustmentTools.AdjustWeaponScaling("global", "damage", 1.05);
                    break;
                case 4:
                    Console.WriteLine("Adjusting: Archetype rebalancing");
                    await BalanceAdjustmentTools.AdjustArchetype("Assassin", "agility", 1.1);
                    break;
            }

            // CRITICAL: Reset the GameConfiguration singleton to reload the updated config file
            // The config was written to disk, but the singleton cache still has the old values
            Console.WriteLine("Reloading configuration from disk...");
            GameConfiguration.ResetInstance();

            // Force the next access to reload the configuration
            _ = GameConfiguration.Instance;
            Console.WriteLine("✓ Configuration reloaded\n");
        }

        private static async Task SaveIterationPatch(int iteration)
        {
            try
            {
                var result = await PatchManagementTools.SavePatch(
                    name: $"Iteration_{iteration}_Balance",
                    author: "AutoTuner",
                    description: $"Iteration {iteration} balance adjustments",
                    version: $"1.{iteration}",
                    tags: "auto-tuned,iteration"
                );
                Console.WriteLine($"✓ Patch saved: Iteration_{iteration}_Balance");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not save patch: {ex.Message}");
            }
        }
    }
}
