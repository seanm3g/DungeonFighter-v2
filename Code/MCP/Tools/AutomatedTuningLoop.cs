using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using RPGGame.MCP.Models;
using RPGGame.Tuning;

namespace RPGGame.MCP.Tools
{
    /// <summary>
    /// Orchestrates automated multi-agent balance tuning loop.
    /// Coordinates Analysis Agent → Tuner Agent → Tester Agent → Game Tester Agent → Config Manager Agent
    /// </summary>
    public class AutomatedTuningLoop
    {
        public class LoopState
        {
            public string Phase { get; set; } = "INIT";
            public int Iteration { get; set; } = 0;
            public int MaxIterations { get; set; } = 5;
            public double TargetWinRate { get; set; } = 90.0;
            public bool MaximizeVariance { get; set; } = true;
            public string Status { get; set; } = "Initializing...";
            public List<string> Log { get; set; } = new();
            public Dictionary<string, object> Results { get; set; } = new();
            public bool Success { get; set; } = false;
            public DateTime StartTime { get; set; } = DateTime.Now;
        }

        private static LoopState _currentState = new();

        public static LoopState GetCurrentState() => _currentState;

        /// <summary>
        /// Run full automated balance cycle
        /// </summary>
        public static async Task<string> RunFullCycle(double targetWinRate = 90.0, int maxIterations = 5)
        {
            _currentState = new LoopState
            {
                TargetWinRate = targetWinRate,
                MaxIterations = maxIterations,
                StartTime = DateTime.Now
            };

            var output = new System.Text.StringBuilder();
            output.AppendLine("╔════════════════════════════════════════════════════════╗");
            output.AppendLine("║     AUTOMATED BALANCE TUNING LOOP - FULL CYCLE          ║");
            output.AppendLine("╠════════════════════════════════════════════════════════╣");
            output.AppendLine($"║ Target Win Rate: {targetWinRate}%");
            output.AppendLine($"║ Max Iterations: {maxIterations}");
            output.AppendLine($"║ Started: {_currentState.StartTime:yyyy-MM-dd HH:mm:ss}");
            output.AppendLine("╚════════════════════════════════════════════════════════╝\n");

            try
            {
                // PHASE 1: ANALYSIS
                output.AppendLine("═══════════════════════════════════════════════════════");
                output.AppendLine("PHASE 1: ANALYSIS AGENT - Diagnosing Current State");
                output.AppendLine("═══════════════════════════════════════════════════════\n");
                _currentState.Phase = "ANALYSIS";

                var analysisReport = await RunAnalysisPhase(output);
                _currentState.Results["analysisReport"] = analysisReport;

                // PHASE 2-N: ITERATIVE TUNING
                for (int i = 1; i <= maxIterations; i++)
                {
                    _currentState.Iteration = i;
                    output.AppendLine($"\n┌─────────────────────────────────────────────────────┐");
                    output.AppendLine($"│ ITERATION {i}/{maxIterations}");
                    output.AppendLine($"└─────────────────────────────────────────────────────┘\n");

                    // PHASE 2: TUNING
                    output.AppendLine($"PHASE 2.{i}: BALANCE TUNER AGENT - Applying Adjustments");
                    output.AppendLine("────────────────────────────────────────────────────\n");
                    _currentState.Phase = "TUNING";

                    var tuningResult = await RunTuningPhase(output, analysisReport, i);
                    _currentState.Results[$"tuningResult_{i}"] = tuningResult;

                    // PHASE 3: VERIFICATION
                    output.AppendLine($"\nPHASE 3.{i}: TESTER AGENT - Running Tests");
                    output.AppendLine("──────────────────────────────────────────\n");
                    _currentState.Phase = "TESTING";

                    var testResult = await RunTestingPhase(output, i);
                    _currentState.Results[$"testResult_{i}"] = testResult;

                    // Check if we've reached target
                    if (testResult.Contains("Win Rate:"))
                    {
                        var winRate = ExtractWinRate(testResult);
                        output.AppendLine($"\n✓ Iteration {i} Complete - Win Rate: {winRate:F1}%");

                        if (winRate >= targetWinRate - 2.0) // Within 2% of target
                        {
                            output.AppendLine($"✓ TARGET REACHED! (Target: {targetWinRate}%, Achieved: {winRate:F1}%)");
                            break;
                        }
                    }
                }

                // PHASE 4: GAMEPLAY VERIFICATION
                output.AppendLine($"\n┌─────────────────────────────────────────────────────┐");
                output.AppendLine($"│ PHASE 4: GAME TESTER AGENT - Gameplay Verification");
                output.AppendLine($"└─────────────────────────────────────────────────────┘\n");
                _currentState.Phase = "GAMEPLAY";

                var gameplayFeedback = await RunGameplayPhase(output);
                _currentState.Results["gameplayFeedback"] = gameplayFeedback;

                // PHASE 5: SAVE CONFIGURATION
                output.AppendLine($"\n┌─────────────────────────────────────────────────────┐");
                output.AppendLine($"│ PHASE 5: CONFIG MANAGER AGENT - Saving Patch");
                output.AppendLine($"└─────────────────────────────────────────────────────┘\n");
                _currentState.Phase = "SAVING";

                var saveResult = await RunSavePhase(output);
                _currentState.Results["saveResult"] = saveResult;

                // Summary
                _currentState.Success = true;
                output.AppendLine("\n╔════════════════════════════════════════════════════════╗");
                output.AppendLine("║     CYCLE COMPLETE - SUCCESS                          ║");
                output.AppendLine("╠════════════════════════════════════════════════════════╣");
                output.AppendLine($"║ Duration: {(DateTime.Now - _currentState.StartTime).TotalMinutes:F1} minutes");
                output.AppendLine($"║ Iterations: {_currentState.Iteration}");
                output.AppendLine($"║ Phases Completed: Analysis → Tuning → Testing → Gameplay → Save");
                output.AppendLine("╚════════════════════════════════════════════════════════╝");

                _currentState.Status = "COMPLETE";
            }
            catch (Exception ex)
            {
                output.AppendLine($"\n✗ CYCLE FAILED: {ex.Message}");
                _currentState.Status = $"FAILED: {ex.Message}";
                _currentState.Success = false;
            }

            return output.ToString();
        }

        private static async Task<string> RunAnalysisPhase(System.Text.StringBuilder output)
        {
            _currentState.Status = "Analysis Agent: Running diagnostics...";
            output.AppendLine("Analysis Agent: Running comprehensive diagnostics...\n");

            // Run battle simulation
            output.AppendLine("  → Running battle simulation (900 battles)...");
            var simResult = await SimulationTools.RunBattleSimulation(25, 1, 1);

            output.AppendLine("  ✓ Simulation complete\n");

            // Analyze results
            output.AppendLine("  → Analyzing results...");
            var analysis = await AnalysisTools.AnalyzeBattleResults();
            output.AppendLine("  ✓ Analysis complete\n");

            // Get quality score
            output.AppendLine("  → Computing balance quality score...");
            var qualityScore = await AutomatedTuningTools.GetBalanceQualityScore();
            output.AppendLine("  ✓ Quality score: " + ExtractQualityScore(qualityScore) + "\n");

            // Get suggestions
            output.AppendLine("  → Generating tuning suggestions...");
            var suggestions = await AutomatedTuningTools.SuggestTuning();
            output.AppendLine("  ✓ Suggestions generated\n");

            output.AppendLine(analysis + "\n");

            _currentState.Status = "Analysis Phase: Complete";
            return analysis + "\n" + suggestions;
        }

        private static async Task<string> RunTuningPhase(System.Text.StringBuilder output, string analysisReport, int iteration)
        {
            _currentState.Status = $"Tuning Agent: Iteration {iteration} adjustments...";
            output.AppendLine("Tuning Agent: Analyzing recommendations and applying adjustments...\n");

            // Get current config
            output.AppendLine("  → Retrieving current configuration...");
            var config = await BalanceAdjustmentTools.GetCurrentConfiguration();
            output.AppendLine("  ✓ Configuration loaded\n");

            // Apply iteration-specific adjustments
            output.AppendLine($"  → Applying adjustments for iteration {iteration}...");
            string adjustmentMsg = iteration switch
            {
                1 => "Boost global enemy health by 5%",
                2 => "Fine-tune archetype balancing",
                3 => "Adjust global damage multipliers",
                4 => "Enhance enemy variance",
                _ => "Apply precision tuning"
            };

            output.AppendLine($"     {adjustmentMsg}");

            switch (iteration)
            {
                case 1:
                    await BalanceAdjustmentTools.AdjustGlobalEnemyMultiplier("health", 1.05);
                    break;
                case 2:
                    await BalanceAdjustmentTools.AdjustArchetype("Assassin", "agility", 1.05);
                    break;
                case 3:
                    await BalanceAdjustmentTools.AdjustGlobalEnemyMultiplier("damage", 1.03);
                    break;
                case 4:
                    await BalanceAdjustmentTools.AdjustArchetype("Brute", "health", 1.05);
                    break;
            }

            output.AppendLine("  ✓ Adjustments applied\n");

            // Save configuration
            output.AppendLine("  → Saving configuration...");
            await BalanceAdjustmentTools.SaveConfiguration();
            output.AppendLine("  ✓ Configuration saved\n");

            _currentState.Status = $"Tuning Phase {iteration}: Complete";
            return $"Iteration {iteration}: {adjustmentMsg}";
        }

        private static async Task<string> RunTestingPhase(System.Text.StringBuilder output, int iteration)
        {
            _currentState.Status = $"Tester Agent: Testing iteration {iteration}...";
            output.AppendLine("Tester Agent: Running comprehensive test suite...\n");

            // Run simulation
            output.AppendLine("  → Running battle simulation...");
            var simResult = await SimulationTools.RunBattleSimulation(25, 1, 1);
            output.AppendLine("  ✓ Simulation complete\n");

            // Validate balance
            output.AppendLine("  → Validating balance constraints...");
            var validation = await AnalysisTools.ValidateBalance();
            output.AppendLine("  ✓ Validation complete\n");

            // Get quality score
            output.AppendLine("  → Computing balance quality score...");
            var qualityScore = await AutomatedTuningTools.GetBalanceQualityScore();
            var score = ExtractQualityScore(qualityScore);
            output.AppendLine($"  ✓ Quality Score: {score}\n");

            // Analyze fun moments
            output.AppendLine("  → Analyzing engagement metrics...");
            var funMoments = await AnalysisTools.AnalyzeFunMoments();
            output.AppendLine("  ✓ Engagement analysis complete\n");

            _currentState.Status = $"Testing Phase {iteration}: Complete";
            return simResult + "\n" + validation;
        }

        private static async Task<string> RunGameplayPhase(System.Text.StringBuilder output)
        {
            _currentState.Status = "Game Tester Agent: Running four-class playthrough batch...";
            output.AppendLine("Game Tester Agent: Verifying full-game balance across all four classes...\n");

            const int runsPerClass = 3;
            const int maxActionsPerRun = 400;

            output.AppendLine($"  → Running {runsPerClass} playthrough(s) per class (max {maxActionsPerRun} actions each)...\n");

            var batch = await ClassPlaythroughBatchRunner.RunAsync(
                runsPerClass,
                classesCsv: null,
                maxActionsPerRun);

            McpToolState.LastPlaythroughBatchResult = batch;
            _currentState.Results["playthroughBatch"] = batch;

            var report = ClassPlaythroughBatchRunner.FormatReport(batch);
            output.AppendLine(report);

            if (batch.HasParityWarnings)
            {
                output.AppendLine("  ⚠ Cross-class parity warnings detected — review before accepting tuning changes.\n");
                _currentState.Status = "Gameplay Phase: Complete with parity warnings";
                return report + "\nGameplay verification completed with parity warnings.";
            }

            output.AppendLine("  ✓ Playthrough batch complete — no cross-class parity warnings.\n");
            _currentState.Status = "Gameplay Phase: Complete";
            return report + "\nGameplay verification passed.";
        }

        private static async Task<string> RunSavePhase(System.Text.StringBuilder output)
        {
            _currentState.Status = "Config Manager Agent: Saving patch...";
            output.AppendLine("Config Manager Agent: Archiving successful configuration...\n");

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var patchName = $"AutoTuned_{timestamp}";

            output.AppendLine($"  → Creating patch: {patchName}...");
            var saveResult = await PatchManagementTools.SavePatch(
                patchName,
                "AutomatedTuningLoop",
                $"Automated balance tuning cycle - Target: {_currentState.TargetWinRate}% win rate, {_currentState.Iteration} iterations",
                $"1.{_currentState.Iteration}",
                "auto-tuned,cycle,automated"
            );
            output.AppendLine("  ✓ Patch saved\n");

            output.AppendLine($"  → Listing all patches...");
            var patches = await PatchManagementTools.ListPatches();
            output.AppendLine("  ✓ Patches available\n");

            _currentState.Status = "Save Phase: Complete";
            return $"Patch saved successfully: {patchName}";
        }

        private static double ExtractWinRate(string testResult)
        {
            var lines = testResult.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains("Win Rate") && line.Contains("%"))
                {
                    var parts = line.Split(new[] { "Win Rate:", "%", " " }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1 && double.TryParse(parts[parts.Length - 1], out var rate))
                    {
                        return rate;
                    }
                }
            }
            return 0.0;
        }

        private static string ExtractQualityScore(string scoreResult)
        {
            var lines = scoreResult.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains("Score") || line.Contains("score"))
                {
                    return line.Trim();
                }
            }
            return "N/A";
        }
    }
}
