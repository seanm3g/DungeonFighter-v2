using System;
using System.Collections.Generic;
using RPGGame.Tests;
using RPGGame;
using RPGGame.Config;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>
    /// Tests for BattleEventAnalyzer
    /// Tests event analysis, narrative triggering, and health tracking
    /// </summary>
    public static class BattleEventAnalyzerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all BattleEventAnalyzer tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== BattleEventAnalyzer Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestConstructor();
            TestInitialize();
            TestUpdateFinalHealth();
            TestAnalyzeEvent();

            TestBase.PrintSummary("BattleEventAnalyzer Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Constructor Tests

        private static void TestConstructor()
        {
            Console.WriteLine("--- Testing Constructor ---");

            try
            {
                var textProvider = new NarrativeTextProvider();
                var stateManager = new NarrativeStateManager();
                var tauntSystem = new TauntSystem(textProvider);
                var analyzer = new BattleEventAnalyzer(textProvider, stateManager, tauntSystem);
                
                TestBase.AssertTrue(analyzer != null,
                    "BattleEventAnalyzer should be created successfully",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"BattleEventAnalyzer constructor failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestInitialize()
        {
            Console.WriteLine("\n--- Testing Initialize ---");

            try
            {
                var textProvider = new NarrativeTextProvider();
                var stateManager = new NarrativeStateManager();
                var tauntSystem = new TauntSystem(textProvider);
                var analyzer = new BattleEventAnalyzer(textProvider, stateManager, tauntSystem);
                
                analyzer.Initialize("Player", "Enemy", "Dungeon", 100, 50);
                
                TestBase.AssertTrue(true,
                    "Initialize should complete without errors",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"Initialize failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestUpdateFinalHealth()
        {
            Console.WriteLine("\n--- Testing UpdateFinalHealth ---");

            try
            {
                var textProvider = new NarrativeTextProvider();
                var stateManager = new NarrativeStateManager();
                var tauntSystem = new TauntSystem(textProvider);
                var analyzer = new BattleEventAnalyzer(textProvider, stateManager, tauntSystem);
                
                analyzer.UpdateFinalHealth(80, 30);
                
                TestBase.AssertTrue(true,
                    "UpdateFinalHealth should complete without errors",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"UpdateFinalHealth failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestAnalyzeEvent()
        {
            Console.WriteLine("\n--- Testing AnalyzeEvent ---");

            try
            {
                var textProvider = new NarrativeTextProvider();
                var stateManager = new NarrativeStateManager();
                var tauntSystem = new TauntSystem(textProvider);
                var analyzer = new BattleEventAnalyzer(textProvider, stateManager, tauntSystem);
                analyzer.Initialize("Player", "Enemy", "Dungeon", 100, 50);
                
                var evt = new BattleEvent
                {
                    Actor = "Player",
                    Target = "Enemy",
                    Damage = 10,
                    IsSuccess = true
                };
                var settings = new GameSettings();
                var narratives = analyzer.AnalyzeEvent(evt, settings);
                
                TestBase.AssertTrue(narratives != null,
                    "AnalyzeEvent should return non-null list",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"AnalyzeEvent failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion
    }
}
