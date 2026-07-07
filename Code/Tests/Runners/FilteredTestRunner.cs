using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Tests;

namespace RPGGame.Tests.Runners
{
    /// <summary>
    /// Runs named test suites matching a substring filter.
    /// Used by <c>dotnet run -- --run-test-filter &lt;pattern&gt;</c>.
    /// </summary>
    public static class FilteredTestRunner
    {
        public sealed class TestSuiteEntry
        {
            public TestSuiteEntry(string runner, string name, System.Action execute)
            {
                Runner = runner;
                Name = name;
                Execute = execute;
            }

            public string Runner { get; }
            public string Name { get; }
            public System.Action Execute { get; }
        }

        public static int Run(string? filter)
        {
            TestResultCollector.Clear();

            if (string.IsNullOrWhiteSpace(filter))
            {
                PrintUsage();
                return 1;
            }

            string? runnerPrefix = null;
            string pattern = filter.Trim();
            int colon = pattern.IndexOf(':');
            if (colon > 0)
            {
                runnerPrefix = pattern[..colon].Trim();
                pattern = pattern[(colon + 1)..].Trim();
            }

            if (pattern.Equals("list", StringComparison.OrdinalIgnoreCase)
                && runnerPrefix == null)
            {
                ListSuites();
                return 0;
            }

            var catalog = BuildCatalog();
            var matches = catalog
                .Where(entry => runnerPrefix == null
                                || entry.Runner.Equals(runnerPrefix, StringComparison.OrdinalIgnoreCase))
                .Where(entry => TestRunFilter.Matches(entry.Name, pattern))
                .ToList();

            if (matches.Count == 0)
            {
                Console.WriteLine($"No test suites matched filter '{filter}'.");
                Console.WriteLine();
                ListSuites(pattern);
                return 1;
            }

            Console.WriteLine(GameConstants.StandardSeparator);
            Console.WriteLine($"  FILTERED TEST RUN: {filter}");
            Console.WriteLine($"  Suites matched: {matches.Count}");
            Console.WriteLine($"{GameConstants.StandardSeparator}\n");

            foreach (var entry in matches)
            {
                Console.WriteLine($"=== {entry.Runner} / {entry.Name} ===\n");
                entry.Execute();
                Console.WriteLine();
            }

            var (total, passed, failed, successRate) = TestResultCollector.GetStatistics();
            Console.WriteLine("=== Filtered Test Run Summary ===");
            Console.WriteLine($"Total Tests: {total}");
            Console.WriteLine($"Passed: {passed}");
            Console.WriteLine($"Failed: {failed}");
            Console.WriteLine($"Success Rate: {successRate:F1}%");
            Console.WriteLine(failed == 0 ? "\n✅ All matched tests passed!" : $"\n❌ {failed} test(s) failed");

            return failed > 0 ? 1 : 0;
        }

        public static void ListSuites(string? highlight = null)
        {
            Console.WriteLine("Available test suites (use with --run-test-filter <pattern>):");
            Console.WriteLine("  Runners: game-system, data, mcp, comprehensive (prefix with runner:, e.g. game-system:Fundamentals)");
            Console.WriteLine();

            string? lastRunner = null;
            foreach (var entry in BuildCatalog().OrderBy(e => e.Runner).ThenBy(e => e.Name))
            {
                if (entry.Runner != lastRunner)
                {
                    Console.WriteLine($"[{entry.Runner}]");
                    lastRunner = entry.Runner;
                }

                string marker = highlight != null && TestRunFilter.Matches(entry.Name, highlight) ? " *" : "";
                Console.WriteLine($"  {entry.Name}{marker}");
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  dotnet run -- --run-test-filter <pattern>");
            Console.WriteLine("  dotnet run -- --list-test-suites");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  dotnet run -- --run-test-filter FundamentalsSimulation");
            Console.WriteLine("  dotnet run -- --run-test-filter EnemyProgression");
            Console.WriteLine("  dotnet run -- --run-test-filter game-system:Tuning");
            Console.WriteLine("  dotnet run -- --run-test-filter \"High WR\"   (matches suite names only)");
        }

        private static IReadOnlyList<TestSuiteEntry> BuildCatalog()
        {
            var list = new List<TestSuiteEntry>();
            list.AddRange(GameSystemTestRunner.GetSuiteEntries());
            list.AddRange(DataSystemTestRunner.GetSuiteEntries());
            list.AddRange(MCPSystemTestRunner.GetSuiteEntries());
            list.Add(new TestSuiteEntry("data", "ActionBonusMechanics", () => RPGGame.Tests.Unit.Data.ActionBonusMechanicsTests.RunAllTests()));
            list.Add(new TestSuiteEntry("data", "ActionCadenceEditorSync", () => RPGGame.Tests.Unit.Data.ActionCadenceEditorSyncTests.RunAllTests()));
            list.Add(new TestSuiteEntry("ui", "CadenceCardLineFormatter", () => RPGGame.Tests.Unit.UI.CadenceCardLineFormatterTests.RunAllTests()));
            list.Add(new TestSuiteEntry("ui", "CombatActionStripBuilder", () => RPGGame.Tests.Unit.UI.CombatActionStripBuilderTests.RunAllTests()));
            list.Add(new TestSuiteEntry("data", "ActionMechanicsRegistry", () => RPGGame.Tests.Unit.Data.ActionMechanicsRegistryTests.RunAll()));
            list.Add(new TestSuiteEntry("data", "ActionMechanicsCadenceMatrix", () => RPGGame.Tests.Unit.Data.ActionMechanicsCadenceMatrixTests.RunAll()));
            list.Add(new TestSuiteEntry("data", "ActionExecutionFlow", () => RPGGame.Tests.Unit.ActionExecutionFlowTests.RunAllTests()));
            list.Add(new TestSuiteEntry("comprehensive", "Comprehensive", () => ComprehensiveTestRunner.RunAllTests()));
            return list;
        }
    }
}
