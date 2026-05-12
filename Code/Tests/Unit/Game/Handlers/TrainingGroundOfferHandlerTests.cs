using System;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Game.Handlers
{
    /// <summary>
    /// Tests the pre-weapon Training Ground offer flow.
    /// </summary>
    public static class TrainingGroundOfferHandlerTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== TrainingGroundOfferHandler Tests ===\n");

            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestSkipShowsPathIntroBeforeWeaponSelection();

            TestBase.PrintSummary("TrainingGroundOfferHandler Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestSkipShowsPathIntroBeforeWeaponSelection()
        {
            TestBase.SetCurrentTestName(nameof(TestSkipShowsPathIntroBeforeWeaponSelection));

            var stateManager = new GameStateManager();
            var character = new Character("TestHero", 1)
            {
                PendingPreWeaponTrainingGround = true
            };
            stateManager.SetCurrentPlayer(character);

            var dungeonRunner = new DungeonRunnerManager(stateManager, new GameNarrativeManager(), null, null);
            var handler = new TrainingGroundOfferHandler(stateManager, null, dungeonRunner);

            handler.HandleMenuInput("2").GetAwaiter().GetResult();

            TestBase.AssertEqualEnum(
                GameState.PreWeaponPathIntro,
                stateManager.CurrentState,
                "Skipping Training Ground should show the path intro before weapon selection",
                ref _testsRun,
                ref _testsPassed,
                ref _testsFailed);

            TestBase.AssertFalse(
                character.PendingPreWeaponTrainingGround,
                "Skipping Training Ground should clear the pending tutorial flag",
                ref _testsRun,
                ref _testsPassed,
                ref _testsFailed);

            TestBase.ClearCurrentTestName();
        }
    }
}
