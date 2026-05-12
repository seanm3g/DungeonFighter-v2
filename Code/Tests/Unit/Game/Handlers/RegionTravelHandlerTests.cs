using System;
using System.Threading.Tasks;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Game.Handlers
{
    public static class RegionTravelHandlerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        public static void RunAllTests()
        {
            Console.WriteLine("=== RegionTravelHandler Tests ===\n");

            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestShowRegionTravelTransitionsState();
            TestReturnToGameLoop();

            TestBase.PrintSummary("RegionTravelHandler Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestShowRegionTravelTransitionsState()
        {
            var stateManager = new GameStateManager();
            stateManager.SetCurrentPlayer(TestDataBuilders.Character().WithName("Traveler").Build());
            var handler = new RegionTravelHandler(stateManager, null);

            handler.ShowRegionTravel();

            TestBase.AssertEqualEnum(GameState.RegionTravel, stateManager.CurrentState,
                "ShowRegionTravel should transition to RegionTravel state",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestReturnToGameLoop()
        {
            var stateManager = new GameStateManager();
            stateManager.SetCurrentPlayer(TestDataBuilders.Character().WithName("Traveler").Build());
            var handler = new RegionTravelHandler(stateManager, null);
            bool returnedToGameLoop = false;
            handler.ShowGameLoopEvent += () => returnedToGameLoop = true;

            handler.ShowRegionTravel();
            Task.Run(async () => await handler.HandleMenuInput("0")).Wait();

            TestBase.AssertEqualEnum(GameState.GameLoop, stateManager.CurrentState,
                "Region travel input 0 should transition to GameLoop",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(returnedToGameLoop,
                "Region travel input 0 should raise ShowGameLoopEvent",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
