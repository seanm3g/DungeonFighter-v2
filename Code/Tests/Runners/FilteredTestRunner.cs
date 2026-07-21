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
            list.Add(new TestSuiteEntry("data", "ActionBonusMechanicsFlow", RunActionBonusMechanicsFlow));
            list.Add(new TestSuiteEntry("data", "JsonArraySheetConverterEnemies", RunJsonArraySheetConverterEnemies));
            list.Add(new TestSuiteEntry("data", "JsonArraySheetConverterWeapons", RunJsonArraySheetConverterWeapons));
            list.Add(new TestSuiteEntry("data", "ActionCadenceEditorSync", () => RPGGame.Tests.Unit.Data.ActionCadenceEditorSyncTests.RunAllTests()));
            list.Add(new TestSuiteEntry("ui", "CadenceCardLineFormatter", () => RPGGame.Tests.Unit.UI.CadenceCardLineFormatterTests.RunAllTests()));
            list.Add(new TestSuiteEntry("ui", "CombatActionStripBuilder", () => RPGGame.Tests.Unit.UI.CombatActionStripBuilderTests.RunAllTests()));
            list.Add(new TestSuiteEntry("ui", "TitleScreenAsciiSpacing", () => RPGGame.Tests.Unit.UI.TitleScreen.TitleScreenAsciiSpacingTests.RunAllTests()));
            list.Add(new TestSuiteEntry("ui", "TitleScreenAnimation", () => RPGGame.Tests.Unit.UI.TitleScreen.TitleScreenAnimationTests.RunAllTests()));
            list.Add(new TestSuiteEntry("ui", "TitleToMenuBootstrap", () => RPGGame.Tests.Unit.UI.TitleScreen.TitleToMenuBootstrapTests.RunAllTests()));
            list.Add(new TestSuiteEntry("ui", "ActionBonusBorderShimmer", () => RPGGame.Tests.Unit.UI.ActionBonusBorderShimmerTests.RunAllTests()));
            list.Add(new TestSuiteEntry("ui", "ActionsTabManager", () => RPGGame.Tests.Unit.UI.ActionsTabManagerTests.RunAllTests()));
            list.Add(new TestSuiteEntry("data", "ActionSetVisibility", () => RPGGame.Tests.Unit.Data.ActionSetVisibilityTests.RunAllTests()));
            list.Add(new TestSuiteEntry("data", "ActionMechanicsRegistry", () => RPGGame.Tests.Unit.Data.ActionMechanicsRegistryTests.RunAll()));
            list.Add(new TestSuiteEntry("data", "ActionMechanicsCadenceMatrix", () => RPGGame.Tests.Unit.Data.ActionMechanicsCadenceMatrixTests.RunAll()));
            list.Add(new TestSuiteEntry("data", "ActionExecutionFlow", () => RPGGame.Tests.Unit.ActionExecutionFlowTests.RunAllTests()));
            list.Add(new TestSuiteEntry("combat", "CombatDelayManager", () => RPGGame.Tests.Unit.Combat.CombatDelayManagerTests.RunAllTests()));
            list.Add(new TestSuiteEntry("combat", "CombatUiMuteScope", () => RPGGame.Tests.Unit.Combat.CombatUiMuteScopeTests.RunAllTests()));
            list.Add(new TestSuiteEntry("combat", "HealthBarDeltaDamageHint", () => RPGGame.Tests.Unit.Combat.HealthBarDeltaDamageHintTests.RunAllTests()));
            list.Add(new TestSuiteEntry("combat", "CombatEffectsSimplified", () => RPGGame.Tests.Unit.Combat.CombatEffectsSimplifiedTests.RunAllTests()));
            list.Add(new TestSuiteEntry("combat", "ActionTriggerGate", () => RPGGame.Tests.Unit.ActionTriggerGateTests.RunAllTests()));
            list.Add(new TestSuiteEntry("combat", "ActionTriggerBundleApplicator", () => RPGGame.Tests.Unit.ActionTriggerBundleApplicatorTests.RunAllTests()));
            list.Add(new TestSuiteEntry("combat", "StripMutation", () => RPGGame.Tests.Unit.StripMutationTests.RunAllTests()));
            list.Add(new TestSuiteEntry("combat", "Retrigger", () => RPGGame.Tests.Unit.RetriggerTests.RunAllTests()));
            list.Add(new TestSuiteEntry("combat", "RollProbabilityContent", () => RPGGame.Tests.Unit.RollProbabilityContentTests.RunAllTests()));
            list.Add(new TestSuiteEntry("combat", "WeaponModTriggerBridge", () => RPGGame.Tests.Unit.WeaponModTriggerBridgeTests.RunAllTests()));
            list.Add(new TestSuiteEntry("combat", "StunProcessor", () => RPGGame.Tests.Unit.Combat.StunProcessorTests.RunAllTests()));
            list.Add(new TestSuiteEntry("actions", "ActionSpeedSystem", () => RPGGame.Tests.Unit.Actions.ActionSpeedSystemTests.RunAllTests()));
            list.Add(new TestSuiteEntry("combat", "MultiHit", () => RPGGame.Tests.Unit.MultiHitTests.RunAllTests()));
            list.Add(new TestSuiteEntry("combat", "DamageCalculator", () => RPGGame.Tests.Unit.Combat.DamageCalculatorTests.RunAllTests()));
            list.Add(new TestSuiteEntry("combat", "WeaponBaseModCadence", () => RPGGame.Tests.Unit.Combat.WeaponBaseModCadenceTests.RunAllTests()));
            list.Add(new TestSuiteEntry("combat", "DamageFormatter", () => RPGGame.Tests.Unit.Combat.DamageFormatterTests.RunAllTests()));
            list.Add(new TestSuiteEntry("combat", "CombatResults", () => RPGGame.Tests.Unit.Combat.CombatResultsTests.RunAllTests()));
            list.Add(new TestSuiteEntry("combat", "StatusEffects", () => RPGGame.Tests.Unit.StatusEffectsTests.RunAllTests()));
            list.Add(new TestSuiteEntry("entity", "ActorClearTempEffects", () => RPGGame.Tests.Unit.Entity.ActorClearTempEffectsTests.RunAllTests()));
            list.Add(new TestSuiteEntry("game", "DeveloperSimMode", () => RPGGame.Tests.Unit.Tuning.DeveloperSimModeTests.RunAllTests()));
            list.Add(new TestSuiteEntry("game", "ActionInteractionLab", () => RPGGame.Tests.Unit.ActionInteractionLabTests.RunAllTests()));
            list.Add(new TestSuiteEntry("game", "ActionInteractionLabCatalog", RunActionInteractionLabCatalog));
            list.Add(new TestSuiteEntry("game", "ActionInteractionLabSession", RunActionInteractionLabSession));
            list.Add(new TestSuiteEntry("data", "FlavorText", () => RPGGame.Tests.Unit.FlavorTextBankCatalogTests.RunAllTests()));
            list.Add(new TestSuiteEntry("data", "FlavorTextSheetConverter", () => RPGGame.Tests.Unit.Data.FlavorTextSheetConverterTests.RunAllTests()));
            list.Add(new TestSuiteEntry("ui", "FlavorTextWindowPlacement", () => RPGGame.Tests.Unit.UI.FlavorTextWindowPlacementTests.RunAllTests()));
            list.Add(new TestSuiteEntry("ui", "ActionLabWindowPlacement", () => RPGGame.Tests.Unit.UI.ActionLabWindowPlacementTests.RunAllTests()));
            list.Add(new TestSuiteEntry("game", "SettingsMenuHandler", () => RPGGame.Tests.Unit.Game.Handlers.SettingsMenuHandlerTests.RunAllTests()));
            list.Add(new TestSuiteEntry("ui", "BlockDisplayManager", () => RPGGame.Tests.Unit.UI.BlockDisplayManagerTests.RunAllTests()));
            list.Add(new TestSuiteEntry("game", "GameStateManagerMultiCharacter", () => RPGGame.Tests.Unit.GameStateManagerMultiCharacterTests.RunAllTests()));
            list.Add(new TestSuiteEntry("persistence", "SaveLoad", () => RPGGame.Tests.Unit.SaveLoadSystemTests.RunAllTests()));
            list.Add(new TestSuiteEntry("comprehensive", "Comprehensive", () => ComprehensiveTestRunner.RunAllTests()));
            return list;
        }

        private static void RunActionBonusMechanicsFlow()
        {
            int run = 0, pass = 0, fail = 0;
            RPGGame.Tests.Unit.Data.ActionBonusMechanics.ActionBonusMechanicsFlowTests.RunAll(ref run, ref pass, ref fail);
        }

        private static void RunJsonArraySheetConverterEnemies()
        {
            int run = 0, pass = 0, fail = 0;
            RPGGame.Tests.Unit.Data.JsonArraySheetConverter.JsonArraySheetConverterEnemiesTests.RunAll(ref run, ref pass, ref fail);
        }

        private static void RunJsonArraySheetConverterWeapons()
        {
            int run = 0, pass = 0, fail = 0;
            RPGGame.Tests.Unit.Data.JsonArraySheetConverter.JsonArraySheetConverterWeaponsTests.RunAll(ref run, ref pass, ref fail);
        }

        private static void RunActionInteractionLabCatalog()
        {
            int run = 0, pass = 0, fail = 0;
            RPGGame.Tests.Unit.ActionInteractionLab.ActionInteractionLabCatalogTests.RunAll(ref run, ref pass, ref fail);
        }

        private static void RunActionInteractionLabSession()
        {
            int run = 0, pass = 0, fail = 0;
            RPGGame.Tests.Unit.ActionInteractionLab.ActionInteractionLabSessionTests.RunAll(ref run, ref pass, ref fail);
        }
    }
}
