using System;
using RPGGame;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Tests for ActionsTabManager. Verifies RefreshCurrentPlayerActionPool (Component 2)
    /// so that action pool refresh after Settings save is correct.
    /// </summary>
    public static class ActionsTabManagerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        public static void RunAllTests()
        {
            Console.WriteLine("=== ActionsTabManager Tests ===\n");

            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestRefreshCurrentPlayerActionPool_WhenPlayerNull_DoesNotThrow();
            TestRefreshCurrentPlayerActionPool_ResetsComboStep();
            TestRefreshCurrentPlayerActionPool_ActionPoolNotNull();

            TestBase.PrintSummary("ActionsTabManager Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestRefreshCurrentPlayerActionPool_WhenPlayerNull_DoesNotThrow()
        {
            Console.WriteLine("--- RefreshCurrentPlayerActionPool with null player does not throw ---");
            try
            {
                ActionsTabManager.RefreshCurrentPlayerActionPool(null);
                TestBase.AssertTrue(true, "RefreshCurrentPlayerActionPool(null) does not throw", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false, $"RefreshCurrentPlayerActionPool(null) threw: {ex.Message}", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestRefreshCurrentPlayerActionPool_ResetsComboStep()
        {
            Console.WriteLine("--- RefreshCurrentPlayerActionPool resets ComboStep to 0 ---");
            try
            {
                var character = new Character("RefreshTest", 1);
                character.ComboStep = 3;

                ActionsTabManager.RefreshCurrentPlayerActionPool(character);

                TestBase.AssertEqual(0, character.ComboStep, "ComboStep is 0 after refresh", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false, $"RefreshCurrentPlayerActionPool threw: {ex.Message}", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestRefreshCurrentPlayerActionPool_ActionPoolNotNull()
        {
            Console.WriteLine("--- RefreshCurrentPlayerActionPool leaves ActionPool non-null ---");
            try
            {
                var character = new Character("PoolTest", 1);
                ActionsTabManager.RefreshCurrentPlayerActionPool(character);

                TestBase.AssertTrue(character.ActionPool != null, "ActionPool is not null after refresh", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false, $"RefreshCurrentPlayerActionPool threw: {ex.Message}", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }
    }
}
