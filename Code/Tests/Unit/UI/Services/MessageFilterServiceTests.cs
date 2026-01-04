using System;
using RPGGame.UI.Services;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.UI.Services
{
    /// <summary>
    /// Comprehensive tests for MessageFilterService
    /// Tests message filtering
    /// </summary>
    public static class MessageFilterServiceTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all MessageFilterService tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== MessageFilterService Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestConstructor();
            TestShouldDisplayMessage_MenuState();
            TestShouldDisplayMessage_CombatState();
            TestShouldDisplayMessage_NoStateManager();
            TestIsMenuState();

            TestBase.PrintSummary("MessageFilterService Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Constructor Tests

        private static void TestConstructor()
        {
            Console.WriteLine("--- Testing Constructor ---");

            var service = new MessageFilterService();
            TestBase.AssertNotNull(service,
                "MessageFilterService should be created",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Filtering Tests

        private static void TestShouldDisplayMessage_MenuState()
        {
            Console.WriteLine("\n--- Testing ShouldDisplayMessage - Menu State ---");

            var service = new MessageFilterService();
            var stateManager = new GameStateManager();
            stateManager.TransitionToState(GameState.MainMenu);
            
            bool shouldDisplay = service.ShouldDisplayMessage(null, UIMessageType.Combat, stateManager);
            
            TestBase.AssertFalse(shouldDisplay,
                "Combat messages should not display in menu states",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestShouldDisplayMessage_CombatState()
        {
            Console.WriteLine("\n--- Testing ShouldDisplayMessage - Combat State ---");

            var service = new MessageFilterService();
            var stateManager = new GameStateManager();
            stateManager.TransitionToState(GameState.Combat);
            
            bool shouldDisplay = service.ShouldDisplayMessage(null, UIMessageType.Combat, stateManager);
            
            TestBase.AssertTrue(shouldDisplay,
                "Combat messages should display in combat state",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestShouldDisplayMessage_NoStateManager()
        {
            Console.WriteLine("\n--- Testing ShouldDisplayMessage - No State Manager ---");

            var service = new MessageFilterService();
            
            bool shouldDisplay = service.ShouldDisplayMessage(null, UIMessageType.Combat, null);
            
            TestBase.AssertTrue(shouldDisplay,
                "Messages should display when no state manager (backward compatibility)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestIsMenuState()
        {
            Console.WriteLine("\n--- Testing IsMenuState ---");

            var service = new MessageFilterService();
            var stateManager = new GameStateManager();
            stateManager.TransitionToState(GameState.MainMenu);
            
            bool isMenu = service.IsMenuState(stateManager);
            
            TestBase.AssertTrue(isMenu,
                "IsMenuState should return true for menu states",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
