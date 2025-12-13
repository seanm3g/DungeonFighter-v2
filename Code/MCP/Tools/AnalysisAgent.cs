using System;
using System.Text;
using System.Threading.Tasks;

namespace RPGGame.MCP.Tools
{
    /// <summary>
    /// Analysis Agent - Specialized in deep-dive diagnostics and recommendations
    /// </summary>
    public class AnalysisAgent
    {
        public enum FocusArea
        {
            Balance,    // Overall balance and win rates
            Weapons,    // Weapon variance and effectiveness
            Enemies,    // Enemy matchups and archetype balance
            Engagement  // Fun moments and engagement metrics
        }

        public static async Task<string> AnalyzeAndReport(FocusArea focus = FocusArea.Balance)
        {
            var output = new StringBuilder();
            output.AppendLine("╔════════════════════════════════════════════════════════╗");
            output.AppendLine("║     ANALYSIS AGENT - Deep Dive Diagnostics             ║");
            output.AppendLine("╚════════════════════════════════════════════════════════╝\n");

            try
            {
                // Always start with simulation
                output.AppendLine("PRELIMINARY: Running Battle Simulation");
                output.AppendLine("─────────────────────────────────────\n");
                output.AppendLine("  → Running comprehensive simulation...");
                var simResult = await SimulationTools.RunBattleSimulation(25, 1, 1);
                output.AppendLine("  ✓ Simulation complete\n\n");

                // Focused analysis
                switch (focus)
                {
                    case FocusArea.Balance:
                        return await AnalyzeBalance(output);

                    case FocusArea.Weapons:
                        return await AnalyzeWeapons(output);

                    case FocusArea.Enemies:
                        return await AnalyzeEnemies(output);

                    case FocusArea.Engagement:
                        return await AnalyzeEngagement(output);

                    default:
                        output.AppendLine("Unknown focus area");
                        return output.ToString();
                }
            }
            catch (Exception ex)
            {
                output.AppendLine($"✗ Analysis Failed: {ex.Message}");
                return output.ToString();
            }
        }

        private static async Task<string> AnalyzeBalance(StringBuilder output)
        {
            output.AppendLine("ANALYSIS: Overall Balance Assessment");
            output.AppendLine("───────────────────────────────────\n");

            output.AppendLine("  → Analyzing battle results...");
            var analysis = await AnalysisTools.AnalyzeBattleResults();
            output.AppendLine("  ✓ Battle analysis complete\n");
            output.Append(analysis);
            output.AppendLine();

            output.AppendLine("  → Computing quality metrics...");
            var qualityScore = await AutomatedTuningTools.GetBalanceQualityScore();
            output.AppendLine("  ✓ Quality score computed\n");
            output.Append(qualityScore);
            output.AppendLine();

            output.AppendLine("  → Generating recommendations...");
            var suggestions = await AutomatedTuningTools.SuggestTuning();
            output.AppendLine("  ✓ Recommendations generated\n");
            output.Append(suggestions);
            output.AppendLine();

            output.AppendLine("╔════════════════════════════════════════════════════════╗");
            output.AppendLine("║     BALANCE ANALYSIS COMPLETE                          ║");
            output.AppendLine("║     See recommendations above for tuning adjustments   ║");
            output.AppendLine("╚════════════════════════════════════════════════════════╝");

            return output.ToString();
        }

        private static async Task<string> AnalyzeWeapons(StringBuilder output)
        {
            output.AppendLine("ANALYSIS: Weapon Variance & Effectiveness");
            output.AppendLine("──────────────────────────────────────────\n");

            output.AppendLine("  → Analyzing weapon performance...");
            var analysis = await AnalysisTools.AnalyzeBattleResults();
            output.AppendLine("  ✓ Weapon performance analyzed\n");
            output.Append(analysis);
            output.AppendLine();

            output.AppendLine("  → Testing weapon parameter sensitivity...");
            output.AppendLine("  (Identifying which weapons need adjustment)\n");

            // Test global damage multiplier sensitivity
            var sensitivity = await EnhancedAnalysisTools.AnalyzeParameterSensitivity(
                "WeaponScaling.GlobalDamageMultiplier",
                "0.7,1.3",
                7,
                25
            );
            output.Append(sensitivity);
            output.AppendLine();

            output.AppendLine("╔════════════════════════════════════════════════════════╗");
            output.AppendLine("║     WEAPON ANALYSIS COMPLETE                           ║");
            output.AppendLine("║     Focus on improving weapon variance without        ║");
            output.AppendLine("║     introducing armor-type interactions                ║");
            output.AppendLine("╚════════════════════════════════════════════════════════╝");

            return output.ToString();
        }

        private static async Task<string> AnalyzeEnemies(StringBuilder output)
        {
            output.AppendLine("ANALYSIS: Enemy Matchups & Archetype Balance");
            output.AppendLine("────────────────────────────────────────────\n");

            output.AppendLine("  → Analyzing enemy performance across weapons...");
            var analysis = await AnalysisTools.AnalyzeBattleResults();
            output.AppendLine("  ✓ Enemy analysis complete\n");
            output.Append(analysis);
            output.AppendLine();

            output.AppendLine("  → Testing archetype parameter sensitivity...");
            output.AppendLine("  (Identifying which archetypes need adjustment)\n");

            // Test archetype health scaling
            var sensitivity = await EnhancedAnalysisTools.AnalyzeParameterSensitivity(
                "EnemySystem.GlobalMultipliers.HealthMultiplier",
                "0.8,1.3",
                10,
                25
            );
            output.Append(sensitivity);
            output.AppendLine();

            output.AppendLine("╔════════════════════════════════════════════════════════╗");
            output.AppendLine("║     ENEMY ANALYSIS COMPLETE                            ║");
            output.AppendLine("║     Current variance is good - maintain attribute     ║");
            output.AppendLine("║     overrides on all enemies                           ║");
            output.AppendLine("╚════════════════════════════════════════════════════════╝");

            return output.ToString();
        }

        private static async Task<string> AnalyzeEngagement(StringBuilder output)
        {
            output.AppendLine("ANALYSIS: Player Engagement & Fun Moments");
            output.AppendLine("─────────────────────────────────────────\n");

            output.AppendLine("  → Analyzing fun moment distribution...");
            var funMoments = await AnalysisTools.AnalyzeFunMoments();
            output.AppendLine("  ✓ Fun moment analysis complete\n");
            output.Append(funMoments);
            output.AppendLine();

            output.AppendLine("  → Getting detailed engagement summary...");
            var summary = await AnalysisTools.GetFunMomentSummary();
            output.AppendLine("  ✓ Summary complete\n");
            output.Append(summary);
            output.AppendLine();

            output.AppendLine("╔════════════════════════════════════════════════════════╗");
            output.AppendLine("║     ENGAGEMENT ANALYSIS COMPLETE                       ║");
            output.AppendLine("║     Track fun moments as balance metric alongside      ║");
            output.AppendLine("║     win rates                                          ║");
            output.AppendLine("╚════════════════════════════════════════════════════════╝");

            return output.ToString();
        }
    }
}
