using System;
using RPGGame.Combat;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>
    /// Comprehensive tests for ThresholdManager
    /// Tests threshold management for critical hits, combos, and hit/miss
    /// </summary>
    public static class ThresholdManagerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all ThresholdManager tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== ThresholdManager Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestGetCriticalMissThreshold();
            TestGetCriticalHitThreshold();
            TestGetComboThreshold();
            TestGetHitThreshold();
            TestSetCriticalMissThreshold();
            TestSetCriticalHitThreshold();
            TestSetComboThreshold();
            TestSetHitThreshold();
            TestClearThresholds();
            TestDefaultThresholds();

            TestBase.PrintSummary("ThresholdManager Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Get Threshold Tests

        private static void TestGetCriticalMissThreshold()
        {
            Console.WriteLine("--- Testing GetCriticalMissThreshold ---");

            var manager = new ThresholdManager();
            var actor = TestDataBuilders.Character().WithName("Actor").Build();

            var threshold = manager.GetCriticalMissThreshold(actor);

            TestBase.AssertTrue(threshold >= 1,
                "Critical miss threshold should be at least 1",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(threshold <= 20,
                "Critical miss threshold should be reasonable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestGetCriticalHitThreshold()
        {
            Console.WriteLine("\n--- Testing GetCriticalHitThreshold ---");

            var manager = new ThresholdManager();
            var actor = TestDataBuilders.Character().WithName("Actor").Build();

            var threshold = manager.GetCriticalHitThreshold(actor);

            TestBase.AssertTrue(threshold >= 1,
                "Critical hit threshold should be at least 1",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(threshold <= 20,
                "Critical hit threshold should be at most 20",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestGetComboThreshold()
        {
            Console.WriteLine("\n--- Testing GetComboThreshold ---");

            var manager = new ThresholdManager();
            var actor = TestDataBuilders.Character().WithName("Actor").Build();

            var threshold = manager.GetComboThreshold(actor);

            TestBase.AssertTrue(threshold >= 1,
                "Combo threshold should be at least 1",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(threshold <= 20,
                "Combo threshold should be at most 20",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestGetHitThreshold()
        {
            Console.WriteLine("\n--- Testing GetHitThreshold ---");

            var manager = new ThresholdManager();
            var actor = TestDataBuilders.Character().WithName("Actor").Build();

            var threshold = manager.GetHitThreshold(actor);

            TestBase.AssertTrue(threshold >= 1,
                "Hit threshold should be at least 1",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(threshold <= 20,
                "Hit threshold should be at most 20",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Set Threshold Tests

        private static void TestSetCriticalMissThreshold()
        {
            Console.WriteLine("\n--- Testing SetCriticalMissThreshold ---");

            var manager = new ThresholdManager();
            var actor = TestDataBuilders.Character().WithName("Actor").Build();

            manager.SetCriticalMissThreshold(actor, 2);
            var threshold = manager.GetCriticalMissThreshold(actor);

            TestBase.AssertEqual(2, threshold,
                "Critical miss threshold should be set correctly",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestSetCriticalHitThreshold()
        {
            Console.WriteLine("\n--- Testing SetCriticalHitThreshold ---");

            var manager = new ThresholdManager();
            var actor = TestDataBuilders.Character().WithName("Actor").Build();

            manager.SetCriticalHitThreshold(actor, 18);
            var threshold = manager.GetCriticalHitThreshold(actor);

            TestBase.AssertEqual(18, threshold,
                "Critical hit threshold should be set correctly",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestSetComboThreshold()
        {
            Console.WriteLine("\n--- Testing SetComboThreshold ---");

            var manager = new ThresholdManager();
            var actor = TestDataBuilders.Character().WithName("Actor").Build();

            manager.SetComboThreshold(actor, 15);
            var threshold = manager.GetComboThreshold(actor);

            TestBase.AssertEqual(15, threshold,
                "Combo threshold should be set correctly",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestSetHitThreshold()
        {
            Console.WriteLine("\n--- Testing SetHitThreshold ---");

            var manager = new ThresholdManager();
            var actor = TestDataBuilders.Character().WithName("Actor").Build();

            manager.SetHitThreshold(actor, 10);
            var threshold = manager.GetHitThreshold(actor);

            TestBase.AssertEqual(10, threshold,
                "Hit threshold should be set correctly",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Clear Tests

        private static void TestClearThresholds()
        {
            Console.WriteLine("\n--- Testing ClearThresholds ---");

            var manager = new ThresholdManager();
            var actor = TestDataBuilders.Character().WithName("Actor").Build();

            manager.SetCriticalHitThreshold(actor, 18);
            manager.ResetThresholds(actor);

            // Should return to default after clearing
            var threshold = manager.GetCriticalHitThreshold(actor);
            TestBase.AssertTrue(threshold >= 1 && threshold <= 20,
                "Threshold should return to default after clearing",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Default Tests

        private static void TestDefaultThresholds()
        {
            Console.WriteLine("\n--- Testing DefaultThresholds ---");

            var manager = new ThresholdManager();
            var actor = TestDataBuilders.Character().WithName("Actor").Build();

            // Test that defaults are reasonable
            var criticalMiss = manager.GetCriticalMissThreshold(actor);
            var criticalHit = manager.GetCriticalHitThreshold(actor);
            var combo = manager.GetComboThreshold(actor);
            var hit = manager.GetHitThreshold(actor);

            TestBase.AssertTrue(criticalMiss >= 1,
                "Default critical miss threshold should be valid",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(criticalHit >= criticalMiss,
                "Critical hit threshold should be >= critical miss threshold",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(combo >= hit,
                "Combo threshold should be >= hit threshold",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
