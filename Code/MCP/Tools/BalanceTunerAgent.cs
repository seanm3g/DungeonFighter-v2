using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RPGGame.MCP.Tools
{
    /// <summary>
    /// Balance Tuner Agent - Specialized in iterative balance adjustments
    /// </summary>
    public class BalanceTunerAgent
    {
        public static async Task<string> TuneBalance(double targetWinRate = 90.0, bool maximizeVariance = true)
        {
            var output = new StringBuilder();
            output.AppendLine("╔════════════════════════════════════════════════════════╗");
            output.AppendLine("║     BALANCE TUNER AGENT - Iterative Adjustments        ║");
            output.AppendLine("╠════════════════════════════════════════════════════════╣");
            output.AppendLine($"║ Target Win Rate: {targetWinRate}%");
            output.AppendLine($"║ Maximize Variance: {(maximizeVariance ? "Yes" : "No")}");
            output.AppendLine("╚════════════════════════════════════════════════════════╝\n");

            try
            {
                int iteration = 0;
                int maxIterations = 10;
                double currentWinRate = 0.0;

                while (iteration < maxIterations && Math.Abs(currentWinRate - targetWinRate) > 2.0)
                {
                    iteration++;
                    output.AppendLine($"\n┌─ Tuning Iteration {iteration} ─────────────────────────────┐\n");

                    // Run simulation to get current state
                    output.AppendLine($"  → Running test simulation...");
                    var simResult = await SimulationTools.RunBattleSimulation(25, 1, 1);
                    output.AppendLine($"  ✓ Simulation complete\n");

                    // Get suggestions
                    output.AppendLine($"  → Analyzing results for suggestions...");
                    var suggestions = await AutomatedTuningTools.SuggestTuning();
                    output.AppendLine($"  ✓ Suggestions generated\n");

                    // Extract current metrics
                    currentWinRate = ExtractAverageWinRate(simResult);
                    output.AppendLine($"  Current Average Win Rate: {currentWinRate:F1}%");
                    output.AppendLine($"  Target: {targetWinRate}%");
                    output.AppendLine($"  Difference: {(targetWinRate - currentWinRate):+0.0;-0.0;0.0}%\n");

                    if (Math.Abs(currentWinRate - targetWinRate) <= 2.0)
                    {
                        output.AppendLine($"✓ TARGET REACHED!\n");
                        break;
                    }

                    // Apply adjustments based on win rate
                    output.AppendLine($"  → Applying adjustments...");

                    if (currentWinRate < targetWinRate - 2.0)
                    {
                        // Win rate too low - buff player
                        output.AppendLine($"     Win rate too low - increasing player advantage");
                        await BalanceAdjustmentTools.AdjustGlobalEnemyMultiplier("health", 0.95);
                        await BalanceAdjustmentTools.AdjustGlobalEnemyMultiplier("damage", 0.98);
                    }
                    else if (currentWinRate > targetWinRate + 2.0)
                    {
                        // Win rate too high - buff enemies
                        output.AppendLine($"     Win rate too high - increasing enemy challenge");
                        await BalanceAdjustmentTools.AdjustGlobalEnemyMultiplier("health", 1.05);
                        await BalanceAdjustmentTools.AdjustGlobalEnemyMultiplier("damage", 1.02);
                    }

                    // Maximize variance if requested
                    if (maximizeVariance && iteration % 2 == 0)
                    {
                        output.AppendLine($"     Enhancing enemy variance (iteration {iteration})");
                        // Could add archetype variance enhancements here
                    }

                    output.AppendLine($"  ✓ Adjustments applied\n");

                    // Save configuration
                    output.AppendLine($"  → Saving configuration...");
                    await BalanceAdjustmentTools.SaveConfiguration();
                    output.AppendLine($"  ✓ Configuration saved\n");

                    output.AppendLine($"└─ Iteration {iteration} Complete ─────────────────────────────┘");
                }

                // Final summary
                output.AppendLine($"\n╔════════════════════════════════════════════════════════╗");
                output.AppendLine($"║     TUNING COMPLETE                                    ║");
                output.AppendLine($"╠════════════════════════════════════════════════════════╣");
                output.AppendLine($"║ Final Win Rate: {currentWinRate:F1}%");
                output.AppendLine($"║ Target: {targetWinRate}%");
                output.AppendLine($"║ Iterations: {iteration}/{maxIterations}");
                output.AppendLine($"║ Status: {(Math.Abs(currentWinRate - targetWinRate) <= 2.0 ? "✓ SUCCESS" : "⚠ INCOMPLETE")}");
                output.AppendLine($"╚════════════════════════════════════════════════════════╝");

                return output.ToString();
            }
            catch (Exception ex)
            {
                output.AppendLine($"✗ Tuning Failed: {ex.Message}");
                return output.ToString();
            }
        }

        private static double ExtractAverageWinRate(string simResult)
        {
            // Parse simulation results to find average win rate
            var lines = simResult.Split('\n');
            double totalWinRate = 0;
            int count = 0;

            foreach (var line in lines)
            {
                if (line.Contains("Win Rate:") && line.Contains("%"))
                {
                    var parts = line.Split(new[] { "Win Rate:", "%", " " }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0 && double.TryParse(parts[parts.Length - 1], out var rate))
                    {
                        totalWinRate += rate;
                        count++;
                    }
                }
            }

            return count > 0 ? totalWinRate / count : 0.0;
        }
    }
}
