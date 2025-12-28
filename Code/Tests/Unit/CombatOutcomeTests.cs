using System;
using RPGGame.Tests;
using RPGGame.Combat.Calculators;
using RPGGame.Combat.Formatting;
using RPGGame.Actions.RollModification;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Comprehensive tests for combat outcomes
    /// Tests hit/miss/critical detection and recording
    /// </summary>
    public static class CombatOutcomeTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Combat Outcome Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestHitDetection();
            TestMissRecording();
            TestHitRecording();
            TestCriticalHitDetection();
            TestCriticalMissDetection();
            TestCriticalHitRecording();
            TestCriticalMissRecording();
            TestThresholdOverrides();
            TestThresholdAdjustments();
            TestDynamicThresholds();

            TestBase.PrintSummary("Combat Outcome Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestHitDetection()
        {
            Console.WriteLine("--- Testing Hit Detection ---");

            var attacker = TestDataBuilders.Character().WithName("TestHero").Build();
            var target = TestDataBuilders.Enemy().WithName("TestEnemy").Build();

            // Test miss (roll 1-5)
            bool miss1 = HitCalculator.CalculateHit(attacker, target, 0, 1);
            TestBase.AssertFalse(miss1, 
                "Roll 1 should miss", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            bool miss5 = HitCalculator.CalculateHit(attacker, target, 0, 5);
            TestBase.AssertFalse(miss5, 
                "Roll 5 should miss", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test regular hit (roll 6-13)
            bool hit6 = HitCalculator.CalculateHit(attacker, target, 0, 6);
            TestBase.AssertTrue(hit6, 
                "Roll 6 should hit", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            bool hit13 = HitCalculator.CalculateHit(attacker, target, 0, 13);
            TestBase.AssertTrue(hit13, 
                "Roll 13 should hit", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test combo hit (roll 14-19)
            bool hit14 = HitCalculator.CalculateHit(attacker, target, 0, 14);
            TestBase.AssertTrue(hit14, 
                "Roll 14 should hit (combo threshold)", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            bool hit19 = HitCalculator.CalculateHit(attacker, target, 0, 19);
            TestBase.AssertTrue(hit19, 
                "Roll 19 should hit", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test critical hit (roll 20+)
            bool hit20 = HitCalculator.CalculateHit(attacker, target, 0, 20);
            TestBase.AssertTrue(hit20, 
                "Roll 20 should hit (critical)", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestMissRecording()
        {
            Console.WriteLine("\n--- Testing Miss Recording ---");

            var action = TestDataBuilders.CreateMockAction("TestAction");
            var missOutcome = CombatOutcome.CreateMiss(action, 3, 3);

            TestBase.AssertTrue(missOutcome.IsMiss, 
                "Miss outcome should record as miss", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertFalse(missOutcome.IsCritical, 
                "Miss should not be critical", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqual(3, missOutcome.TotalRoll, 
                "Miss outcome should record total roll", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHitRecording()
        {
            Console.WriteLine("\n--- Testing Hit Recording ---");

            var action = TestDataBuilders.CreateMockAction("TestAction");
            var hitOutcome = CombatOutcome.CreateHit(action, 15, 15, false);

            TestBase.AssertFalse(hitOutcome.IsMiss, 
                "Hit outcome should not be miss", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqual(15, hitOutcome.TotalRoll, 
                "Hit outcome should record total roll", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCriticalHitDetection()
        {
            Console.WriteLine("\n--- Testing Critical Hit Detection ---");

            var attacker = TestDataBuilders.Character().WithName("TestHero").Build();
            var thresholdManager = RollModificationManager.GetThresholdManager();

            // Test default critical hit threshold (20)
            int threshold = thresholdManager.GetCriticalHitThreshold(attacker);
            TestBase.AssertTrue(threshold >= 20, 
                $"Critical hit threshold should be 20+, got {threshold}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test critical hit detection
            bool isCritical20 = CombatCalculator.IsCriticalHit(attacker, 20);
            TestBase.AssertTrue(isCritical20, 
                "Roll 20 should be critical hit", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            bool isCritical25 = CombatCalculator.IsCriticalHit(attacker, 25);
            TestBase.AssertTrue(isCritical25, 
                "Roll 25 should be critical hit", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCriticalMissDetection()
        {
            Console.WriteLine("\n--- Testing Critical Miss Detection ---");

            var attacker = TestDataBuilders.Character().WithName("TestHero").Build();
            var thresholdManager = RollModificationManager.GetThresholdManager();

            // Test default critical miss threshold (1)
            int threshold = thresholdManager.GetCriticalMissThreshold(attacker);
            TestBase.AssertEqual(1, threshold, 
                $"Critical miss threshold should be 1, got {threshold}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test critical miss outcome
            var action = TestDataBuilders.CreateMockAction("TestAction");
            var criticalMissOutcome = CombatOutcome.CreateMiss(action, 1, 1);

            TestBase.AssertTrue(criticalMissOutcome.IsCriticalMiss, 
                "Natural 1 should be critical miss", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCriticalHitRecording()
        {
            Console.WriteLine("\n--- Testing Critical Hit Recording ---");

            var action = TestDataBuilders.CreateMockAction("TestAction");
            var criticalHitOutcome = CombatOutcome.CreateHit(action, 20, 20, true);

            TestBase.AssertTrue(criticalHitOutcome.IsCritical, 
                "Roll 20 should record as critical", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertFalse(criticalHitOutcome.IsMiss, 
                "Critical hit should not be miss", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCriticalMissRecording()
        {
            Console.WriteLine("\n--- Testing Critical Miss Recording ---");

            var action = TestDataBuilders.CreateMockAction("TestAction");
            var criticalMissOutcome = CombatOutcome.CreateMiss(action, 1, 1);

            TestBase.AssertTrue(criticalMissOutcome.IsCriticalMiss, 
                "Natural 1 should record as critical miss", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(criticalMissOutcome.IsMiss, 
                "Critical miss should also be miss", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestThresholdOverrides()
        {
            Console.WriteLine("\n--- Testing Threshold Overrides ---");

            var attacker = TestDataBuilders.Character().WithName("TestHero").Build();
            var thresholdManager = RollModificationManager.GetThresholdManager();

            // Test setting custom critical hit threshold
            thresholdManager.SetCriticalHitThreshold(attacker, 15);
            int customThreshold = thresholdManager.GetCriticalHitThreshold(attacker);
            TestBase.AssertEqual(15, customThreshold, 
                "Custom critical hit threshold should be set", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Reset
            thresholdManager.ResetThresholds(attacker);
        }

        private static void TestThresholdAdjustments()
        {
            Console.WriteLine("\n--- Testing Threshold Adjustments ---");

            var attacker = TestDataBuilders.Character().WithName("TestHero").Build();
            var thresholdManager = RollModificationManager.GetThresholdManager();

            int originalThreshold = thresholdManager.GetCriticalHitThreshold(attacker);
            
            // Test adjustment
            thresholdManager.AdjustCriticalHitThreshold(attacker, 3);
            int adjustedThreshold = thresholdManager.GetCriticalHitThreshold(attacker);
            TestBase.AssertEqual(originalThreshold + 3, adjustedThreshold, 
                "Threshold adjustment should add to current threshold", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Reset
            thresholdManager.ResetThresholds(attacker);
        }

        private static void TestDynamicThresholds()
        {
            Console.WriteLine("\n--- Testing Dynamic Thresholds ---");

            var attacker = TestDataBuilders.Character().WithName("TestHero").Build();
            var thresholdManager = RollModificationManager.GetThresholdManager();

            // Test all threshold types
            int hitThreshold = thresholdManager.GetHitThreshold(attacker);
            TestBase.AssertTrue(hitThreshold >= 0, 
                $"Hit threshold should be valid, got {hitThreshold}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            int comboThreshold = thresholdManager.GetComboThreshold(attacker);
            TestBase.AssertTrue(comboThreshold >= 0, 
                $"Combo threshold should be valid, got {comboThreshold}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            int criticalHitThreshold = thresholdManager.GetCriticalHitThreshold(attacker);
            TestBase.AssertTrue(criticalHitThreshold >= 0, 
                $"Critical hit threshold should be valid, got {criticalHitThreshold}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            int criticalMissThreshold = thresholdManager.GetCriticalMissThreshold(attacker);
            TestBase.AssertTrue(criticalMissThreshold >= 0, 
                $"Critical miss threshold should be valid, got {criticalMissThreshold}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}

