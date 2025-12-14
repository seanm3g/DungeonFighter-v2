using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RPGGame.MCP.Tools.Refactoring
{
    /// <summary>
    /// Formats refactoring analysis output
    /// </summary>
    public static class RefactoringFormatter
    {
        /// <summary>
        /// Formats a list of refactoring opportunities
        /// </summary>
        public static string FormatRefactoringOpportunities(List<RefactoringOpportunity> opportunities)
        {
            var output = new StringBuilder();

            output.AppendLine("REFACTORING OPPORTUNITIES RANKED BY IMPACT/EFFORT:\n");

            int priority = 1;
            foreach (var opp in opportunities)
            {
                output.AppendLine($"PRIORITY {priority}: {opp.Type.ToUpper()} - {opp.Description}");
                output.AppendLine($"═════════════════════════════════════════════════════");
                output.AppendLine($"Location: {opp.Location}");
                output.AppendLine($"Impact Score: {opp.ImpactScore:F0}/100");
                output.AppendLine($"Effort Score: {opp.EffortScore:F0}/100");
                output.AppendLine($"ROI: {(opp.ImpactScore / (opp.EffortScore + 1)):F2}");
                output.AppendLine($"Risk Level: {(opp.RiskLevel < 0.2 ? "Very Low" : opp.RiskLevel < 0.4 ? "Low" : opp.RiskLevel < 0.6 ? "Medium" : "High")}\n");

                output.AppendLine("BEFORE:");
                output.AppendLine($"  {opp.BeforeCode}\n");

                output.AppendLine("AFTER:");
                output.AppendLine($"  {opp.AfterCode}\n");

                if (opp.Benefits.Count > 0)
                {
                    output.AppendLine("BENEFITS:");
                    foreach (var benefit in opp.Benefits)
                        output.AppendLine($"  ✓ {benefit}");
                    output.AppendLine();
                }

                if (opp.Risks.Count > 0)
                {
                    output.AppendLine("RISKS:");
                    foreach (var risk in opp.Risks)
                        output.AppendLine($"  ⚠ {risk}");
                    output.AppendLine();
                }

                if (opp.AffectedTests.Count > 0)
                {
                    output.AppendLine("TESTS TO RUN:");
                    foreach (var test in opp.AffectedTests)
                        output.AppendLine($"  • {test}");
                    output.AppendLine();
                }

                output.AppendLine();
                priority++;
            }

            output.AppendLine("╔════════════════════════════════════════════════════════╗");
            output.AppendLine("║     Total Identified: " + opportunities.Count + " opportunities");
            output.AppendLine("║     Estimated Total Effort: " + (opportunities.Sum(o => o.EffortScore) / 5).ToString("F0") + " minutes");
            output.AppendLine("║     Expected Code Reduction: 15-25%");
            output.AppendLine("║     Risk: Low to Medium (if properly tested)");
            output.AppendLine("╚════════════════════════════════════════════════════════╝");

            return output.ToString();
        }
        
        /// <summary>
        /// Formats a refactoring plan
        /// </summary>
        public static string FormatRefactoringPlan(string type, string target)
        {
            var output = new StringBuilder();

            output.AppendLine("REFACTORING PLAN:\n");
            output.AppendLine($"Type: {type}");
            output.AppendLine($"Target: {target}\n");

            output.AppendLine("STEPS:\n");
            output.AppendLine("1. Create backup of current code");
            output.AppendLine("2. Run full test suite to establish baseline");
            output.AppendLine("3. Apply refactoring changes");
            output.AppendLine("4. Run full test suite to verify no regressions");
            output.AppendLine("5. Review code quality metrics");
            output.AppendLine("6. Create commit with changes\n");

            output.AppendLine("DETAILED CHANGES:\n");

            if (type.Equals("extract", StringComparison.OrdinalIgnoreCase))
            {
                output.AppendLine("Extract Method Refactoring:");
                output.AppendLine("  • Identify the code block to extract");
                output.AppendLine("  • Determine required parameters");
                output.AppendLine("  • Create new method with parameters");
                output.AppendLine("  • Return values from extracted method");
                output.AppendLine("  • Replace original code with method call");
                output.AppendLine("  • Update all call sites\n");
            }
            else if (type.Equals("simplify", StringComparison.OrdinalIgnoreCase))
            {
                output.AppendLine("Simplification Refactoring:");
                output.AppendLine("  • Replace nested conditionals with early returns");
                output.AppendLine("  • Combine related conditions");
                output.AppendLine("  • Remove unnecessary temporary variables");
                output.AppendLine("  • Use standard library utilities where available");
                output.AppendLine("  • Inline single-use methods\n");
            }
            else if (type.Equals("consolidate", StringComparison.OrdinalIgnoreCase))
            {
                output.AppendLine("Consolidation Refactoring:");
                output.AppendLine("  • Identify similar code blocks");
                output.AppendLine("  • Extract common pattern");
                output.AppendLine("  • Parameterize differences");
                output.AppendLine("  • Replace all instances with consolidated method");
                output.AppendLine("  • Remove redundant code\n");
            }

            output.AppendLine("ESTIMATED EFFORT: 30-60 minutes");
            output.AppendLine("RISK LEVEL: Low to Medium\n");

            output.AppendLine("VERIFICATION:\n");
            output.AppendLine("  ✓ All tests pass");
            output.AppendLine("  ✓ Code metrics improve");
            output.AppendLine("  ✓ No new warnings");
            output.AppendLine("  ✓ Performance unchanged or improved");
            output.AppendLine("  ✓ Code review approval\n");

            output.AppendLine("Next: Run tests with `/test-engineer run [category]`");

            return output.ToString();
        }
    }
}

