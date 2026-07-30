using System;
using RPGGame;
using RPGGame.Items.ItemTriggerScenario;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Combat-path coverage for every item trigger identity via
    /// <see cref="ItemTriggerScenarioRunner"/> (shared with Lab + CLI).
    /// </summary>
    public static class ItemTriggerCombatIntegrationTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Item Trigger Combat Integration Tests ===\n");
            _ = GameConfiguration.Instance;
            _run = _passed = _failed = 0;

            var identities = ItemTriggerIdentityCatalog.Identities;
            TestBase.AssertTrue(identities.Count >= 106, "catalog at least wave-2 size", ref _run, ref _passed, ref _failed);

            // Full combat-path coverage for demo/wave identities; animal/taxon suffix procs
            // are covered by StatBonusAnimalSuffixTriggerTests + scenario CLI when needed.
            foreach (var identity in identities)
            {
                if (IsAnimalOrTaxonSuffixIdentity(identity.Name))
                    continue;
                TestBase.SetCurrentTestName($"Combat/{identity.Index}:{identity.Name}");
                var report = ItemTriggerScenarioRunner.Run(identity);
                TestBase.AssertTrue(report.Passed,
                    report.Passed
                        ? $"{identity.Name}: {report.Finding}"
                        : $"{identity.Name}: {report.Finding}" +
                          (string.IsNullOrEmpty(report.Error) ? "" : $" ({report.Error.Split('\n')[0]})"),
                    ref _run, ref _passed, ref _failed);
            }

            TestBase.PrintSummary("Item Trigger Combat Integration Tests", _run, _passed, _failed);
        }

        private static bool IsAnimalOrTaxonSuffixIdentity(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;
            return name.EndsWith("Suffix", StringComparison.OrdinalIgnoreCase)
                   || name.EndsWith("SetFrom", StringComparison.OrdinalIgnoreCase)
                   || name.EndsWith("AmpTo", StringComparison.OrdinalIgnoreCase);
        }
    }
}
