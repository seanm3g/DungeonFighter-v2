using System;
using System.Linq;

namespace RPGGame.Items.ItemTriggerScenario
{
    /// <summary>CLI entry for <c>--run-item-trigger-scenarios</c>.</summary>
    public static class ItemTriggerScenarioCli
    {
        /// <summary>
        /// Runs scenarios and prints reports. Returns process exit code (0 = all passed).
        /// Optional <paramref name="filter"/> matches name / WHEN / mechanics / index.
        /// </summary>
        public static int Run(string? filter = null, bool verbose = false)
        {
            Console.WriteLine("=== Item Trigger Scenarios ===");
            if (!string.IsNullOrWhiteSpace(filter))
                Console.WriteLine($"Filter: {filter}");
            Console.WriteLine();

            var batch = ItemTriggerScenarioRunner.RunAll(filter);
            Console.WriteLine(ItemTriggerScenarioReportFormatter.FormatBatch(batch, verbosePerReport: verbose));

            if (batch.Failed > 0)
            {
                Console.WriteLine();
                Console.WriteLine("Failed identities:");
                foreach (var r in batch.Reports.Where(x => !x.Passed))
                    Console.WriteLine($"  #{r.IdentityIndex} {r.IdentityName}: {r.Finding}");
            }

            return batch.Failed == 0 ? 0 : 1;
        }
    }
}
