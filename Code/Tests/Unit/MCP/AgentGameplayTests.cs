using System;
using System.Linq;
using System.Threading.Tasks;
using RPGGame.Data;
using RPGGame.MCP;
using RPGGame.MCP.Tools;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.MCP
{
    /// <summary>
    /// Tests for MCP agent gameplay interface (context, choices, enemy path resolution).
    /// </summary>
    public static class AgentGameplayTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Agent Gameplay Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestEnemiesJsonPathResolution();
            TestTrainingGroundOfferLabeledChoices();
            TestCustomLevelModeHints();

            TestBase.PrintSummary("Agent Gameplay Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestEnemiesJsonPathResolution()
        {
            Console.WriteLine("--- Testing Enemies.json path resolution ---");

            var path = JsonLoader.FindGameDataFile(GameConstants.EnemiesJson)
                ?? GameConstants.TryGetExistingGameDataFilePath(GameConstants.EnemiesJson);

            TestBase.AssertNotNull(path, "Enemies.json should resolve via JsonLoader/GameConstants", ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (path != null)
            {
                TestBase.AssertTrue(System.IO.File.Exists(path),
                    $"Resolved Enemies.json path should exist: {path}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestTrainingGroundOfferLabeledChoices()
        {
            Console.WriteLine("\n--- Testing TrainingGroundOffer labeled choices ---");

            try
            {
                var wrapper = new GameWrapper();
                McpToolState.GameWrapper = wrapper;
                wrapper.InitializeGame();
                wrapper.ShowMainMenu();

                var game = wrapper.Game!;
                game.HandleInput("1").GetAwaiter().GetResult();

                TestBase.AssertTrue(game.CurrentState == GameState.TrainingGroundOffer,
                    $"Expected TrainingGroundOffer after new game, got {game.CurrentState}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                var ctx = AgentContextBuilder.Build(game, wrapper.OutputCapture);
                TestBase.AssertTrue(ctx.Choices.Count >= 2,
                    "TrainingGroundOffer should expose at least 2 labeled choices",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                var skip = ctx.Choices.FirstOrDefault(c => c.Input == "2");
                TestBase.AssertNotNull(skip, "Choice '2' (skip training) should be present", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(!string.IsNullOrWhiteSpace(skip!.Label),
                    "Skip training choice should have a non-empty label",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                wrapper.DisposeGame();
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false, $"TrainingGroundOffer test failed: {ex.Message}", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestCustomLevelModeHints()
        {
            Console.WriteLine("\n--- Testing custom level mode hints ---");

            try
            {
                var wrapper = new GameWrapper();
                McpToolState.GameWrapper = wrapper;
                wrapper.InitializeGame();
                AdvanceToDungeonSelection(wrapper).GetAwaiter().GetResult();

                var game = wrapper.Game!;
                TestBase.AssertTrue(game.CurrentState == GameState.DungeonSelection,
                    $"Expected DungeonSelection, got {game.CurrentState}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                int customInput = FindCustomDungeonMenuInput(game);
                if (customInput < 0)
                {
                    TestBase.AssertTrue(true, "Custom level mode test skipped (no custom dungeon in list)", ref _testsRun, ref _testsPassed, ref _testsFailed);
                    wrapper.DisposeGame();
                    return;
                }

                game.HandleInput(customInput.ToString()).GetAwaiter().GetResult();

                var inputContext = AgentChoiceBuilder.ResolveInputContext(game);
                TestBase.AssertTrue(inputContext.PendingInputMode == AgentChoiceBuilder.ModeCustomDungeonLevel,
                    "Option 4 should enter CustomDungeonLevel pending input mode",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                var hints = AgentChoiceBuilder.BuildHints(inputContext, game.CurrentState);
                TestBase.AssertTrue(hints.Any(h => h.Contains("Custom dungeon level", StringComparison.OrdinalIgnoreCase)),
                    "Custom level mode should produce a warning hint",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                var choices = AgentChoiceBuilder.BuildChoices(game, inputContext);
                TestBase.AssertTrue(choices.Any(c => c.Input == "enter"),
                    "Custom level mode should list 'enter' as a choice",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertFalse(choices.Any(c => c.Label.Contains("Ancient Forest", StringComparison.OrdinalIgnoreCase)),
                    "Custom level mode should not list normal dungeon picks",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                wrapper.DisposeGame();
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false, $"Custom level mode test failed: {ex.Message}", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static async Task AdvanceToDungeonSelection(GameWrapper wrapper)
        {
            var game = wrapper.Game!;
            await game.HandleInput("1"); // new game
            await game.HandleInput("2"); // skip training
            await game.HandleInput("1"); // path intro
            await game.HandleInput("1"); // weapon
            await game.HandleInput("1"); // confirm character -> game loop
            await game.HandleInput("1"); // enter dungeons
        }

        private static int FindCustomDungeonMenuInput(GameCoordinator game)
        {
            var dungeons = game.AvailableDungeons;
            if (dungeons == null)
                return -1;
            for (int i = 0; i < dungeons.Count; i++)
            {
                if (dungeons[i].Name == GameConstants.DungeonCustomLevelMenuName)
                    return i + 1;
            }
            return -1;
        }
    }
}
