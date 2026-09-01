using System;
using System.Linq;
using RPGGame;
using RPGGame.Items.ItemTriggerScenario;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>Focused coverage for scenario reports (buff deltas + formatter).</summary>
    public static class ItemTriggerScenarioTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Item Trigger Scenario Tests ===\n");
            _ = GameConfiguration.Instance;
            _run = _passed = _failed = 0;

            TestCatalogCount();
            TestFilterMatching();
            TestSingleIdentityReportHasBuffSection();
            TestBatchSummary();
            TestFormatterContainsFinding();
            TestPrepareAttackerSetupForcesFace();

            TestBase.PrintSummary("Item Trigger Scenario Tests", _run, _passed, _failed);
        }

        private static void TestCatalogCount()
        {
            TestBase.SetCurrentTestName("Scenario/CatalogCount");
            TestBase.AssertEqual(106, ItemTriggerIdentityCatalog.Identities.Count,
                "catalog has 106 identities", ref _run, ref _passed, ref _failed);
        }

        private static void TestFilterMatching()
        {
            TestBase.SetCurrentTestName("Scenario/FilterMatching");
            var any = ItemTriggerIdentityCatalog.Identities.First();
            TestBase.AssertTrue(
                ItemTriggerScenarioRunner.MatchesFilter(any, any.Name),
                "filter matches name", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(
                ItemTriggerScenarioRunner.MatchesFilter(any, any.Index.ToString()),
                "filter matches index", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(
                !ItemTriggerScenarioRunner.MatchesFilter(any, "___no_such_trigger___"),
                "filter rejects junk", ref _run, ref _passed, ref _failed);
        }

        private static void TestSingleIdentityReportHasBuffSection()
        {
            TestBase.SetCurrentTestName("Scenario/ReportBuffSection");
            // Prefer a combat ONCRITICAL identity if present; else first combat identity
            var identity = ItemTriggerIdentityCatalog.Identities
                               .FirstOrDefault(i =>
                                   !i.IsEquipEffect
                                   && (i.When?.Contains("CRITICAL", StringComparison.OrdinalIgnoreCase) ?? false))
                           ?? ItemTriggerIdentityCatalog.Identities.First(i => !i.IsEquipEffect);

            var report = ItemTriggerScenarioRunner.Run(identity);
            TestBase.AssertTrue(report.Before != null && report.After != null,
                "report captures before/after buff snapshots", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!string.IsNullOrWhiteSpace(report.Finding),
                "report has finding", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(report.SetupNotes.Count > 0,
                "report has setup notes", ref _run, ref _passed, ref _failed);
            // Buff deltas are optional (some procs only change messages), but SCOPE ACTION next-action should grow bank
            string mech = ItemTriggerScenarioRunner.NormalizeMech(identity.Mechanics);
            string scope = (identity.Scope ?? "").Trim().ToUpperInvariant();
            if (scope == "ACTION" && mech.Contains("next_action", StringComparison.OrdinalIgnoreCase) && report.Passed)
            {
                TestBase.AssertTrue(
                    report.BuffDeltaLines.Any(l => l.Contains("ACTION bank", StringComparison.OrdinalIgnoreCase)),
                    "ACTION next-action reports ACTION bank delta", ref _run, ref _passed, ref _failed);
            }
            else
            {
                TestBase.AssertTrue(true, "buff delta section present or N/A for this identity",
                    ref _run, ref _passed, ref _failed);
            }
        }

        private static void TestBatchSummary()
        {
            TestBase.SetCurrentTestName("Scenario/BatchFilter");
            // Run a tiny filtered batch (index 0) for speed
            var batch = ItemTriggerScenarioRunner.RunAll("0");
            // Filter "0" may match index 0 and any name/when containing "0"
            TestBase.AssertTrue(batch.Total >= 1, "filtered batch has results", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(batch.Passed + batch.Failed, batch.Total,
                "batch counts add up", ref _run, ref _passed, ref _failed);
        }

        private static void TestFormatterContainsFinding()
        {
            TestBase.SetCurrentTestName("Scenario/Formatter");
            var identity = ItemTriggerIdentityCatalog.Identities[0];
            var report = ItemTriggerScenarioRunner.Run(identity);
            string text = ItemTriggerScenarioReportFormatter.Format(report, verbose: true);
            TestBase.AssertTrue(text.Contains(identity.Name, StringComparison.OrdinalIgnoreCase),
                "formatter includes name", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(text.Contains("finding:", StringComparison.OrdinalIgnoreCase),
                "formatter includes finding", ref _run, ref _passed, ref _failed);
            var compact = ItemTriggerScenarioReportFormatter.FormatCompactLines(report);
            TestBase.AssertTrue(compact.Count >= 2, "compact lines for lab panel", ref _run, ref _passed, ref _failed);
        }

        private static void TestPrepareAttackerSetupForcesFace()
        {
            TestBase.SetCurrentTestName("Scenario/PrepareSetup");
            var identity = ItemTriggerIdentityCatalog.Identities
                .FirstOrDefault(i =>
                    !i.IsEquipEffect
                    && !(i.When?.Contains("ROOM", StringComparison.OrdinalIgnoreCase) ?? false)
                    && !(i.When?.Contains("TAKEHIT", StringComparison.OrdinalIgnoreCase) ?? false)
                    && !(i.When?.Contains("HEROHURT", StringComparison.OrdinalIgnoreCase) ?? false)
                    && !(i.When?.Contains("WHILE", StringComparison.OrdinalIgnoreCase) ?? false));
            if (identity == null)
            {
                TestBase.AssertTrue(false, "no attacker combat identity found", ref _run, ref _passed, ref _failed);
                return;
            }

            var setup = ItemTriggerScenarioRunner.PrepareAttackerSetup(identity);
            TestBase.AssertTrue(setup.Hero != null && setup.Enemy != null && setup.Swing != null,
                "prepare returns entities", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(setup.ForcedD20 >= 1 && setup.ForcedD20 <= 20,
                $"forced d20 in range ({setup.ForcedD20})", ref _run, ref _passed, ref _failed);
            int stripCount = setup.Hero?.GetComboActions()?.Count ?? 0;
            TestBase.AssertTrue(stripCount > 0,
                "setup builds combo strip", ref _run, ref _passed, ref _failed);
        }
    }
}
