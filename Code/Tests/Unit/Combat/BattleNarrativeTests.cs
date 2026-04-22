using System;
using RPGGame.Tests;
using RPGGame;
using RPGGame.Combat;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>
    /// Tests for BattleNarrative
    /// Tests narrative generation, event tracking, and text generation
    /// </summary>
    public static class BattleNarrativeTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all BattleNarrative tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== BattleNarrative Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestConstructor();
            TestAddEvent();
            TestGetNarratives();
            TestInformationalSummaryExcludesComboCounts();

            TestBase.PrintSummary("BattleNarrative Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Constructor Tests

        private static void TestConstructor()
        {
            Console.WriteLine("--- Testing Constructor ---");

            try
            {
                var narrative = new BattleNarrative("Player", "Enemy", "Dungeon", 100, 50);
                
                TestBase.AssertTrue(narrative != null,
                    "BattleNarrative should be created successfully",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"BattleNarrative constructor failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region Event Tests

        private static void TestAddEvent()
        {
            Console.WriteLine("\n--- Testing AddEvent ---");

            try
            {
                var narrative = new BattleNarrative("Player", "Enemy");
                var evt = new BattleEvent
                {
                    Actor = "Player",
                    Target = "Enemy",
                    Action = "Attack",
                    Damage = 10,
                    IsSuccess = true
                };
                
                narrative.AddEvent(evt);
                
                TestBase.AssertTrue(true,
                    "AddEvent should complete without errors",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"AddEvent failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestGetNarratives()
        {
            Console.WriteLine("\n--- Testing GetTriggeredNarratives ---");

            try
            {
                var narrative = new BattleNarrative("Player", "Enemy");
                var narratives = narrative.GetTriggeredNarratives();
                
                TestBase.AssertTrue(narratives != null,
                    "GetTriggeredNarratives should return non-null list",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"GetTriggeredNarratives failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestInformationalSummaryExcludesComboCounts()
        {
            Console.WriteLine("\n--- Testing informational summary (no combo line) ---");

            string playerWin = BattleNarrativeGenerator.GenerateInformationalSummary(
                40, 5, playerWon: true, enemyWon: false, "Hero", "Goblin");
            TestBase.AssertTrue(string.IsNullOrEmpty(playerWin),
                "Player victory summary should be empty (no combo or damage line)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            string enemyWin = BattleNarrativeGenerator.GenerateInformationalSummary(
                10, 50, playerWon: false, enemyWon: true, "Hero", "Goblin");
            TestBase.AssertTrue(
                enemyWin.Contains("Total damage dealt", StringComparison.Ordinal)
                && enemyWin.Contains("Goblin defeats Hero", StringComparison.Ordinal)
                && !enemyWin.Contains("Combos", StringComparison.OrdinalIgnoreCase),
                "Enemy victory summary should include damage totals but not combo counts",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            string stalemate = BattleNarrativeGenerator.GenerateInformationalSummary(
                25, 25, playerWon: false, enemyWon: false, "Hero", "Goblin");
            TestBase.AssertTrue(
                stalemate.Contains("stalemate", StringComparison.OrdinalIgnoreCase)
                && !stalemate.Contains("Combos", StringComparison.OrdinalIgnoreCase),
                "Stalemate summary should not include combo counts",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
