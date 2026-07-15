using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.Tests.Unit;
using RPGGame.Tests.Unit.Game;
using RPGGame.Tests.Unit.Game.Handlers;
using RPGGame.Tests.Unit.Game.Input;
using RPGGame.Tests.Unit.UI;
using RPGGame.Tests.Unit.Combat;

namespace RPGGame.Tests.Runners
{
    /// <summary>
    /// Test runner for Game system tests
    /// </summary>
    public static class GameSystemTestRunner
    {
        private static readonly (string Name, System.Action Execute)[] Suites =
        {
            ("GameCoordinator", () => GameCoordinatorTests.RunAllTests()),
            ("BackgroundDungeonTaskManager", () => BackgroundDungeonTaskManagerTests.RunAllTests()),
            ("GameStateManager", () => GameStateManagerTests.RunAllTests()),
            ("GameStateValidator", () => GameStateValidatorTests.RunAllTests()),
            ("GameInitializer", () => GameInitializerTests.RunAllTests()),
            ("GameInitializationManager", () => GameInitializationManagerTests.RunAllTests()),
            ("GameLoader", () => GameLoaderTests.RunAllTests()),
            ("FileManager", () => FileManagerTests.RunAllTests()),
            ("DungeonRunnerManager", () => DungeonRunnerManagerTests.RunAllTests()),
            ("DungeonDisplayManager", () => DungeonDisplayManagerTests.RunAllTests()),
            ("CharacterCloneService", () => CharacterCloneServiceTests.RunAllTests()),
            ("CharacterResurrectionService", () => CharacterResurrectionServiceTests.RunAllTests()),
            ("MainMenuHandler", () => MainMenuHandlerTests.RunAllTests()),
            ("CharacterMenuHandler", () => CharacterMenuHandlerTests.RunAllTests()),
            ("SettingsMenuHandler", () => SettingsMenuHandlerTests.RunAllTests()),
            ("WeaponSelectionHandler", () => WeaponSelectionHandlerTests.RunAllTests()),
            ("TrainingGroundOfferHandler", () => TrainingGroundOfferHandlerTests.RunAllTests()),
            ("CharacterCreationHandler", () => CharacterCreationHandlerTests.RunAllTests()),
            ("DungeonSelectionHandler", () => DungeonSelectionHandlerTests.RunAllTests()),
            ("RegionTravelHandler", () => RegionTravelHandlerTests.RunAllTests()),
            ("TravelRouteColoredTextFormatter", () => TravelRouteColoredTextFormatterTests.RunAllTests()),
            ("DungeonCompletionHandler", () => DungeonCompletionHandlerTests.RunAllTests()),
            ("GameLoopInputHandler", () => GameLoopInputHandlerTests.RunAllTests()),
            ("DeathScreenHandler", () => DeathScreenHandlerTests.RunAllTests()),
            ("LoadCharacterSelectionHandler", () => LoadCharacterSelectionHandlerTests.RunAllTests()),
            ("DungeonExitChoiceHandler", () => DungeonExitChoiceHandlerTests.RunAllTests()),
            ("HandlerInitializer", () => HandlerInitializerTests.RunAllTests()),
            ("GameInputRouter", () => GameInputRouterTests.RunAllTests()),
            ("EscapeKeyHandler", () => EscapeKeyHandlerTests.RunAllTests()),
            ("ActionEditorHandler", () => ActionEditorHandlerTests.RunAllTests()),
            ("CharacterManagementHandler", () => CharacterManagementHandlerTests.RunAllTests()),
            ("AdjustmentExecutor", () => AdjustmentExecutorTests.RunAllTests()),
            ("PlayerTuningApplier", () => RPGGame.Tests.Unit.Tuning.PlayerTuningApplierTests.RunAllTests()),
            ("CombatTuningParameterRegistry", () => RPGGame.Tests.Unit.Tuning.CombatTuningParameterRegistryTests.RunAllTests()),
            ("LevelWinRateCurve", () => RPGGame.Tests.Unit.Tuning.LevelWinRateCurveTests.RunAllTests()),
            ("LevelTuningSessionStore", () => RPGGame.Tests.Unit.Tuning.LevelTuningSessionStoreTests.RunAllTests()),
            ("BalanceTuningProfile", () => RPGGame.Tests.Unit.Tuning.BalanceTuningProfileTests.RunAllTests()),
            ("BalanceDialClassifier", () => RPGGame.Tests.Unit.Tuning.BalanceDialClassifierTests.RunAllTests()),
            ("DeveloperSimMode", () => RPGGame.Tests.Unit.Tuning.DeveloperSimModeTests.RunAllTests()),
            ("FundamentalsSimulation", () => RPGGame.Tests.Unit.Tuning.FundamentalsSimulationTests.RunAllTests()),
            ("ClassPlaythrough", () => RPGGame.Tests.Unit.Tuning.ClassPlaythroughBatchTests.RunAllTests()),
            ("PlaythroughTuning", () => RPGGame.Tests.Unit.Tuning.PlaythroughTuningTests.RunAllTests()),
            ("EnemyProgressionCurveEvaluator", () => RPGGame.Tests.Unit.Tuning.EnemyProgressionCurveEvaluatorTests.RunAllTests()),
            ("RollFeelVarianceCompression", () => RPGGame.Tests.Unit.Tuning.RollFeelVarianceCompressionTests.RunAllTests()),
            ("CombatTuningPanelHandler", () => CombatTuningPanelHandlerTests.RunAllTests()),
            ("TuningValueFormatter", () => TuningValueFormatterTests.RunAllTests()),
            ("CombatTuningParameterViewModel", () => CombatTuningParameterViewModelTests.RunAllTests()),
            ("CombatTuningPanelViewModel", () => CombatTuningPanelViewModelTests.RunAllTests()),
            ("ArchetypeTuningViewModel", () => ArchetypeTuningViewModelTests.RunAllTests()),
            ("StatusEffectTuningViewModel", () => StatusEffectTuningViewModelTests.RunAllTests()),
            ("SliderWithTextBoxLogic", () => SliderWithTextBoxLogicTests.RunAllTests()),
            ("EnemyTuningPanelHandler", () => EnemyTuningPanelHandlerTests.RunAllTests()),
            ("BattleStatisticsHandler", () => BattleStatisticsHandlerTests.RunAllTests()),
            ("MatchupAnalyzer", () => MatchupAnalyzerTests.RunAllTests()),
            ("GameScreenCoordinator", () => GameScreenCoordinatorTests.RunAllTests()),
            ("CombatRenderingValidator", () => CombatRenderingValidatorTests.RunAllTests()),
            ("ClaudeAIGamePlayer", () => ClaudeAIGamePlayerTests.RunAllTests()),
            ("NaiveteThresholdBonuses", () => NaiveteThresholdBonusesTests.RunAllTests()),
            ("ActionEffectTarget", () => ActionEffectTargetTests.RunAllTests()),
        };

        public static IReadOnlyList<FilteredTestRunner.TestSuiteEntry> GetSuiteEntries()
        {
            var entries = new List<FilteredTestRunner.TestSuiteEntry>(Suites.Length);
            foreach (var (name, execute) in Suites)
                entries.Add(new FilteredTestRunner.TestSuiteEntry("game-system", name, execute));
            return entries;
        }

        public static void RunAllTests() => RunFiltered(null);

        public static void RunFiltered(string? filter)
        {
            Console.WriteLine(GameConstants.StandardSeparator);
            Console.WriteLine("  GAME SYSTEM TEST SUITE");
            if (!string.IsNullOrWhiteSpace(filter))
                Console.WriteLine($"  Filter: {filter}");
            Console.WriteLine($"{GameConstants.StandardSeparator}\n");

            foreach (var (name, execute) in Suites)
            {
                if (!TestRunFilter.Matches(name, filter))
                    continue;
                execute();
                Console.WriteLine();
            }
        }
    }
}
