using System;
using System.Threading;
using System.Threading.Tasks;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Game.Handlers
{
    /// <summary>
    /// Comprehensive tests for DungeonExitChoiceHandler
    /// Tests dungeon exit choice handling
    /// </summary>
    public static class DungeonExitChoiceHandlerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all DungeonExitChoiceHandler tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== DungeonExitChoiceHandler Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            try
            {
                TestConstructor();
                TestShowExitChoiceMenu();
                TestIsWaitingForChoice();
                TestHandleMenuInput_Continue();
                TestHandleMenuInput_Leave();
                TestHandleMenuInput_InvalidChoice();
                TestHandleMenuInput_NotWaiting();
            }
            catch (Exception ex)
            {
                _testsFailed++;
                Console.WriteLine($"  ✗ FAILED: Unhandled exception in test execution: {ex.Message}");
                Console.WriteLine($"  Stack trace: {ex.StackTrace}");
            }
            finally
            {
                TestBase.PrintSummary("DungeonExitChoiceHandler Tests", _testsRun, _testsPassed, _testsFailed);
            }
        }

        #region Constructor Tests

        private static void TestConstructor()
        {
            Console.WriteLine("--- Testing Constructor ---");

            var stateManager = new GameStateManager();
            var narrativeManager = new GameNarrativeManager();
            var displayManager = new DungeonDisplayManager(narrativeManager, null, stateManager);
            
            var handler = new DungeonExitChoiceHandler(stateManager, null, displayManager);
            TestBase.AssertNotNull(handler,
                "DungeonExitChoiceHandler should be created",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Display Tests

        private static void TestShowExitChoiceMenu()
        {
            Console.WriteLine("\n--- Testing ShowExitChoiceMenu ---");

            try
            {
                var stateManager = new GameStateManager();
                var narrativeManager = new GameNarrativeManager();
                var displayManager = new DungeonDisplayManager(narrativeManager, null, stateManager);
                var handler = new DungeonExitChoiceHandler(stateManager, null, displayManager);
                
                // Start the exit choice menu
                var menuTask = Task.Run(async () => await handler.ShowExitChoiceMenu(1, 5));
                
                // Give it a moment to start
                System.Threading.Thread.Sleep(100);
                
                // Provide input to complete the menu (choose to continue)
                handler.HandleMenuInput("1");
                
                // Wait for menu to complete with timeout (5 seconds)
                if (!menuTask.Wait(TimeSpan.FromSeconds(5)))
                {
                    throw new TimeoutException("ShowExitChoiceMenu did not complete within timeout");
                }
                
                TestBase.AssertTrue(true,
                    "ShowExitChoiceMenu should complete without errors",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"ShowExitChoiceMenu threw exception: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region State Tests

        private static void TestIsWaitingForChoice()
        {
            Console.WriteLine("\n--- Testing IsWaitingForChoice ---");

            var stateManager = new GameStateManager();
            var narrativeManager = new GameNarrativeManager();
            var displayManager = new DungeonDisplayManager(narrativeManager, null, stateManager);
            var handler = new DungeonExitChoiceHandler(stateManager, null, displayManager);
            
            // Initially should not be waiting
            TestBase.AssertFalse(handler.IsWaitingForChoice,
                "Initially should not be waiting for choice",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Input Handling Tests

        private static void TestHandleMenuInput_Continue()
        {
            Console.WriteLine("\n--- Testing HandleMenuInput - Continue ---");

            try
            {
                var stateManager = new GameStateManager();
                var narrativeManager = new GameNarrativeManager();
                var displayManager = new DungeonDisplayManager(narrativeManager, null, stateManager);
                var handler = new DungeonExitChoiceHandler(stateManager, null, displayManager);
                
                // Start the exit choice menu
                var menuTask = Task.Run(async () => await handler.ShowExitChoiceMenu(1, 5));
                
                // Give it a moment to start
                System.Threading.Thread.Sleep(100);
                
                // Test continue option
                handler.HandleMenuInput("1");
                
                // Wait for menu to complete with timeout (5 seconds)
                if (!menuTask.Wait(TimeSpan.FromSeconds(5)))
                {
                    throw new TimeoutException("HandleMenuInput for continue did not complete within timeout");
                }
                
                TestBase.AssertTrue(true,
                    "HandleMenuInput for continue should complete",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"HandleMenuInput for continue threw exception: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestHandleMenuInput_Leave()
        {
            Console.WriteLine("\n--- Testing HandleMenuInput - Leave ---");

            try
            {
                var stateManager = new GameStateManager();
                var narrativeManager = new GameNarrativeManager();
                var displayManager = new DungeonDisplayManager(narrativeManager, null, stateManager);
                var handler = new DungeonExitChoiceHandler(stateManager, null, displayManager);
                
                // Start the exit choice menu
                var menuTask = Task.Run(async () => await handler.ShowExitChoiceMenu(1, 5));
                
                // Give it a moment to start
                System.Threading.Thread.Sleep(100);
                
                // Test leave option
                handler.HandleMenuInput("2");
                
                // Wait for menu to complete with timeout (5 seconds)
                if (!menuTask.Wait(TimeSpan.FromSeconds(5)))
                {
                    throw new TimeoutException("HandleMenuInput for leave did not complete within timeout");
                }
                
                TestBase.AssertTrue(true,
                    "HandleMenuInput for leave should complete",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"HandleMenuInput for leave threw exception: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestHandleMenuInput_InvalidChoice()
        {
            Console.WriteLine("\n--- Testing HandleMenuInput - Invalid Choice ---");

            try
            {
                var stateManager = new GameStateManager();
                var narrativeManager = new GameNarrativeManager();
                var displayManager = new DungeonDisplayManager(narrativeManager, null, stateManager);
                string? messageReceived = null;
                var handler = new DungeonExitChoiceHandler(stateManager, null, displayManager);
                handler.ShowMessageEvent += (msg) => { messageReceived = msg; };
                
                // Start the exit choice menu
                var menuTask = Task.Run(async () => await handler.ShowExitChoiceMenu(1, 5));
                
                // Give it a moment to start
                System.Threading.Thread.Sleep(100);
                
                // Test invalid choice
                handler.HandleMenuInput("99");
                
                // Complete the menu with a valid choice
                handler.HandleMenuInput("1");
                
                // Wait for menu to complete with timeout (5 seconds)
                if (!menuTask.Wait(TimeSpan.FromSeconds(5)))
                {
                    throw new TimeoutException("HandleMenuInput for invalid choice did not complete within timeout");
                }
                
                TestBase.AssertTrue(messageReceived != null && messageReceived.Contains("Invalid"),
                    "HandleMenuInput should show error for invalid choice",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"HandleMenuInput for invalid choice threw exception: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestHandleMenuInput_NotWaiting()
        {
            Console.WriteLine("\n--- Testing HandleMenuInput - Not Waiting ---");

            var stateManager = new GameStateManager();
            var narrativeManager = new GameNarrativeManager();
            var displayManager = new DungeonDisplayManager(narrativeManager, null, stateManager);
            var handler = new DungeonExitChoiceHandler(stateManager, null, displayManager);
            
            // Test input when not waiting (should be ignored)
            handler.HandleMenuInput("1");
            
            TestBase.AssertTrue(true,
                "HandleMenuInput should handle input when not waiting gracefully",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
