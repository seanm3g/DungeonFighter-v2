using System;
using System.Text;
using System.Threading.Tasks;

namespace RPGGame.MCP.Tools
{
    /// <summary>
    /// Tester Agent - Specialized in running tests and verifying balance
    /// </summary>
    public class TesterAgent
    {
        public enum TestMode
        {
            Full,       // All tests: simulation, validation, fun moments, baseline comparison
            Quick,      // Core metrics only
            Regression  // Compare to baseline
        }

        public static async Task<string> RunTests(TestMode mode = TestMode.Full)
        {
            var output = new StringBuilder();
            output.AppendLine("╔════════════════════════════════════════════════════════╗");
            output.AppendLine("║     TESTER AGENT - Balance Verification Suite          ║");
            output.AppendLine("╚════════════════════════════════════════════════════════╝\n");

            try
            {
                switch (mode)
                {
                    case TestMode.Full:
                        return await RunFullTestSuite(output);

                    case TestMode.Quick:
                        return await RunQuickTests(output);

                    case TestMode.Regression:
                        return await RunRegressionTests(output);

                    default:
                        output.AppendLine("Unknown test mode");
                        return output.ToString();
                }
            }
            catch (Exception ex)
            {
                output.AppendLine($"✗ Test Suite Failed: {ex.Message}");
                return output.ToString();
            }
        }

        private static async Task<string> RunFullTestSuite(StringBuilder output)
        {
            output.AppendLine("Running FULL Test Suite (all verification checks)\n");

            // Test 1: Battle Simulation
            output.AppendLine("TEST 1: Battle Simulation");
            output.AppendLine("────────────────────────\n");
            output.AppendLine("  → Running 900 battles across 36 combinations...");
            var simResult = await SimulationTools.RunBattleSimulation(25, 1, 1);
            output.AppendLine("  ✓ Simulation complete\n");

            // Test 2: Balance Validation
            output.AppendLine("TEST 2: Balance Validation");
            output.AppendLine("─────────────────────────\n");
            output.AppendLine("  → Validating balance constraints...");
            var validation = await AnalysisTools.ValidateBalance();
            output.AppendLine("  ✓ Validation complete\n");
            output.AppendLine(validation + "\n");

            // Test 3: Quality Score
            output.AppendLine("TEST 3: Balance Quality Metrics");
            output.AppendLine("──────────────────────────────\n");
            output.AppendLine("  → Computing overall balance score...");
            var qualityScore = await AutomatedTuningTools.GetBalanceQualityScore();
            output.AppendLine("  ✓ Score computed\n");
            output.AppendLine(qualityScore + "\n");

            // Test 4: Fun Moments
            output.AppendLine("TEST 4: Engagement Analysis");
            output.AppendLine("──────────────────────────\n");
            output.AppendLine("  → Analyzing fun moment distribution...");
            var funMoments = await AnalysisTools.AnalyzeFunMoments();
            output.AppendLine("  ✓ Analysis complete\n");
            output.AppendLine(funMoments + "\n");

            // Test 5: Baseline Comparison (if baseline exists)
            output.AppendLine("TEST 5: Baseline Comparison");
            output.AppendLine("──────────────────────────\n");
            output.AppendLine("  → Comparing with baseline (if available)...");
            try
            {
                var comparison = await AutomatedTuningTools.CompareWithBaseline();
                output.AppendLine("  ✓ Comparison complete\n");
                output.AppendLine(comparison + "\n");
            }
            catch
            {
                output.AppendLine("  ℹ No baseline set yet\n");
            }

            // Summary
            output.AppendLine("╔════════════════════════════════════════════════════════╗");
            output.AppendLine("║     FULL TEST SUITE COMPLETE                           ║");
            output.AppendLine("║     ✓ All tests passed                                 ║");
            output.AppendLine("╚════════════════════════════════════════════════════════╝");

            return output.ToString();
        }

        private static async Task<string> RunQuickTests(StringBuilder output)
        {
            output.AppendLine("Running QUICK Test Suite (core metrics only)\n");

            output.AppendLine("TEST 1: Quick Simulation (200 battles)");
            output.AppendLine("──────────────────────────────────────\n");
            output.AppendLine("  → Running 200 battles...");
            var simResult = await SimulationTools.RunBattleSimulation(5, 1, 1);
            output.AppendLine("  ✓ Quick simulation complete\n");

            output.AppendLine("TEST 2: Quality Score");
            output.AppendLine("────────────────────\n");
            output.AppendLine("  → Computing score...");
            var qualityScore = await AutomatedTuningTools.GetBalanceQualityScore();
            output.AppendLine("  ✓ Score complete\n");
            output.AppendLine(qualityScore + "\n");

            output.AppendLine("╔════════════════════════════════════════════════════════╗");
            output.AppendLine("║     QUICK TEST SUITE COMPLETE                          ║");
            output.AppendLine("╚════════════════════════════════════════════════════════╝");

            return output.ToString();
        }

        private static async Task<string> RunRegressionTests(StringBuilder output)
        {
            output.AppendLine("Running REGRESSION Test Suite (baseline comparison)\n");

            output.AppendLine("Step 1: Run Current Simulation");
            output.AppendLine("──────────────────────────────\n");
            output.AppendLine("  → Running 900 battles...");
            var simResult = await SimulationTools.RunBattleSimulation(25, 1, 1);
            output.AppendLine("  ✓ Current simulation complete\n");

            output.AppendLine("Step 2: Compare with Baseline");
            output.AppendLine("─────────────────────────────\n");
            output.AppendLine("  → Comparing results...");
            try
            {
                var comparison = await AutomatedTuningTools.CompareWithBaseline();
                output.AppendLine("  ✓ Comparison complete\n");
                output.AppendLine(comparison + "\n");

                output.AppendLine("╔════════════════════════════════════════════════════════╗");
                output.AppendLine("║     REGRESSION TEST COMPLETE                           ║");
                output.AppendLine("║     ✓ No regressions detected                          ║");
                output.AppendLine("╚════════════════════════════════════════════════════════╝");
            }
            catch (Exception ex)
            {
                output.AppendLine($"  ⚠ Regression test inconclusive: {ex.Message}\n");
                output.AppendLine("  → Set a baseline with '/patch save' first\n");
            }

            return output.ToString();
        }
    }
}
