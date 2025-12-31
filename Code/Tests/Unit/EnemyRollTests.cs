using System;
using RPGGame.Tests;
using RPGGame.Combat.Calculators;
using RPGGame.Actions.RollModification;
using RPGGame.Actions.Execution;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Comprehensive tests for enemy roll outcomes
    /// Tests critical miss, miss, hit, combo, and crit combo detection for enemies
    /// </summary>
    public static class EnemyRollTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Enemy Roll Outcome Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestEnemyCriticalMiss();
            TestEnemyMiss();
            TestEnemyHit();
            TestEnemyCombo();
            TestEnemyCritCombo();
            TestEnemyRollThresholds();
            TestEnemyRollWithBonuses();
            TestEnemyRollWithPenalties();

            TestBase.PrintSummary("Enemy Roll Outcome Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestEnemyCriticalMiss()
        {
            Console.WriteLine("--- Testing Enemy Critical Miss ---");

            var enemy = TestDataBuilders.Enemy().WithName("TestEnemy").Build();
            var target = TestDataBuilders.Character().WithName("TestTarget").Build();
            var thresholdManager = RollModificationManager.GetThresholdManager();

            // Critical miss threshold should be 1 (natural 1)
            int criticalMissThreshold = thresholdManager.GetCriticalMissThreshold(enemy);
            TestBase.AssertEqual(1, criticalMissThreshold,
                $"Enemy critical miss threshold should be 1, got {criticalMissThreshold}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test base roll of 1 (critical miss)
            // Simulate action execution flow for enemy
            var action = TestDataBuilders.CreateMockAction("BITE");
            int baseRoll = 1;
            int rollBonus = 0;
            int attackRoll = baseRoll + rollBonus;

            // Check critical miss detection (base roll <= critical miss threshold)
            bool isCriticalMiss = baseRoll <= criticalMissThreshold;
            TestBase.AssertTrue(isCriticalMiss,
                $"Enemy base roll of {baseRoll} should be critical miss",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Critical miss should also miss
            bool hits = HitCalculator.CalculateHit(enemy, target, rollBonus, attackRoll);
            TestBase.AssertFalse(hits,
                $"Enemy critical miss (roll {attackRoll}) should not hit",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test base roll of 0 (edge case, should also be critical miss)
            baseRoll = 0;
            attackRoll = baseRoll + rollBonus;
            isCriticalMiss = baseRoll <= criticalMissThreshold;
            TestBase.AssertTrue(isCriticalMiss,
                $"Enemy base roll of {baseRoll} should be critical miss",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestEnemyMiss()
        {
            Console.WriteLine("\n--- Testing Enemy Miss ---");

            var enemy = TestDataBuilders.Enemy().WithName("TestEnemy").Build();
            var target = TestDataBuilders.Character().WithName("TestTarget").Build();
            var thresholdManager = RollModificationManager.GetThresholdManager();

            int hitThreshold = thresholdManager.GetHitThreshold(enemy);
            TestBase.AssertTrue(hitThreshold >= 5,
                $"Enemy hit threshold should be >= 5, got {hitThreshold}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test rolls 1-5 should miss (assuming hit threshold is 5)
            for (int roll = 1; roll <= 5; roll++)
            {
                int rollBonus = 0;
                int attackRoll = roll + rollBonus;
                bool hits = HitCalculator.CalculateHit(enemy, target, rollBonus, attackRoll);
                TestBase.AssertFalse(hits,
                    $"Enemy roll of {attackRoll} (base {roll}) should miss (threshold: {hitThreshold})",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            // Test roll of 2 (miss, but not critical miss)
            int baseRoll = 2;
            int bonus = 0;
            int totalRoll = baseRoll + bonus;
            bool isCriticalMiss = baseRoll <= thresholdManager.GetCriticalMissThreshold(enemy);
            TestBase.AssertFalse(isCriticalMiss,
                $"Enemy base roll of {baseRoll} should not be critical miss",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestEnemyHit()
        {
            Console.WriteLine("\n--- Testing Enemy Hit (Basic Attack) ---");

            var enemy = TestDataBuilders.Enemy().WithName("TestEnemy").Build();
            var target = TestDataBuilders.Character().WithName("TestTarget").Build();
            var thresholdManager = RollModificationManager.GetThresholdManager();

            int hitThreshold = thresholdManager.GetHitThreshold(enemy);
            int comboThreshold = thresholdManager.GetComboThreshold(enemy);

            // Test rolls 6-13 should hit (basic attack range)
            for (int roll = 6; roll <= 13; roll++)
            {
                int rollBonus = 0;
                int attackRoll = roll + rollBonus;
                bool hits = HitCalculator.CalculateHit(enemy, target, rollBonus, attackRoll);
                TestBase.AssertTrue(hits,
                    $"Enemy roll of {attackRoll} (base {roll}) should hit",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Should not be combo (below combo threshold)
                bool isCombo = attackRoll >= comboThreshold;
                TestBase.AssertFalse(isCombo,
                    $"Enemy roll of {attackRoll} should not be combo (combo threshold: {comboThreshold})",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Should not be critical (below critical threshold)
                bool isCritical = attackRoll >= thresholdManager.GetCriticalHitThreshold(enemy);
                TestBase.AssertFalse(isCritical,
                    $"Enemy roll of {attackRoll} should not be critical",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestEnemyCombo()
        {
            Console.WriteLine("\n--- Testing Enemy Combo ---");

            var enemy = TestDataBuilders.Enemy().WithName("TestEnemy").Build();
            var target = TestDataBuilders.Character().WithName("TestTarget").Build();
            var thresholdManager = RollModificationManager.GetThresholdManager();

            int comboThreshold = thresholdManager.GetComboThreshold(enemy);
            int criticalThreshold = thresholdManager.GetCriticalHitThreshold(enemy);

            TestBase.AssertTrue(comboThreshold >= 14,
                $"Enemy combo threshold should be >= 14, got {comboThreshold}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test rolls 14-19 should be combo hits
            for (int roll = 14; roll <= 19; roll++)
            {
                int rollBonus = 0;
                int attackRoll = roll + rollBonus;
                bool hits = HitCalculator.CalculateHit(enemy, target, rollBonus, attackRoll);
                TestBase.AssertTrue(hits,
                    $"Enemy roll of {attackRoll} (base {roll}) should hit",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Should be combo
                bool isCombo = attackRoll >= comboThreshold;
                TestBase.AssertTrue(isCombo,
                    $"Enemy roll of {attackRoll} should be combo (combo threshold: {comboThreshold})",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Should not be critical (below critical threshold)
                bool isCritical = attackRoll >= criticalThreshold;
                TestBase.AssertFalse(isCritical,
                    $"Enemy roll of {attackRoll} should not be critical (critical threshold: {criticalThreshold})",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestEnemyCritCombo()
        {
            Console.WriteLine("\n--- Testing Enemy Crit Combo ---");

            var enemy = TestDataBuilders.Enemy().WithName("TestEnemy").Build();
            var target = TestDataBuilders.Character().WithName("TestTarget").Build();
            var thresholdManager = RollModificationManager.GetThresholdManager();

            int comboThreshold = thresholdManager.GetComboThreshold(enemy);
            int criticalThreshold = thresholdManager.GetCriticalHitThreshold(enemy);

            TestBase.AssertTrue(criticalThreshold >= 20,
                $"Enemy critical hit threshold should be >= 20, got {criticalThreshold}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test roll of 20 (critical combo)
            int baseRoll = 20;
            int rollBonus = 0;
            int attackRoll = baseRoll + rollBonus;
            bool hits = HitCalculator.CalculateHit(enemy, target, rollBonus, attackRoll);
            TestBase.AssertTrue(hits,
                $"Enemy roll of {attackRoll} (base {baseRoll}) should hit",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Should be combo
            bool isCombo = attackRoll >= comboThreshold;
            TestBase.AssertTrue(isCombo,
                $"Enemy roll of {attackRoll} should be combo (combo threshold: {comboThreshold})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Should be critical
            bool isCritical = attackRoll >= criticalThreshold;
            TestBase.AssertTrue(isCritical,
                $"Enemy roll of {attackRoll} should be critical (critical threshold: {criticalThreshold})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test roll of 25 (critical combo above 20)
            baseRoll = 25;
            attackRoll = baseRoll + rollBonus;
            hits = HitCalculator.CalculateHit(enemy, target, rollBonus, attackRoll);
            TestBase.AssertTrue(hits,
                $"Enemy roll of {attackRoll} (base {baseRoll}) should hit",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            isCombo = attackRoll >= comboThreshold;
            TestBase.AssertTrue(isCombo,
                $"Enemy roll of {attackRoll} should be combo",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            isCritical = attackRoll >= criticalThreshold;
            TestBase.AssertTrue(isCritical,
                $"Enemy roll of {attackRoll} should be critical",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestEnemyRollThresholds()
        {
            Console.WriteLine("\n--- Testing Enemy Roll Thresholds ---");

            var enemy = TestDataBuilders.Enemy().WithName("TestEnemy").Build();
            var thresholdManager = RollModificationManager.GetThresholdManager();

            // Verify all thresholds are set correctly
            int criticalMissThreshold = thresholdManager.GetCriticalMissThreshold(enemy);
            int hitThreshold = thresholdManager.GetHitThreshold(enemy);
            int comboThreshold = thresholdManager.GetComboThreshold(enemy);
            int criticalThreshold = thresholdManager.GetCriticalHitThreshold(enemy);

            TestBase.AssertEqual(1, criticalMissThreshold,
                $"Enemy critical miss threshold should be 1, got {criticalMissThreshold}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(hitThreshold >= 5,
                $"Enemy hit threshold should be >= 5, got {hitThreshold}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(comboThreshold >= 14,
                $"Enemy combo threshold should be >= 14, got {comboThreshold}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(criticalThreshold >= 20,
                $"Enemy critical hit threshold should be >= 20, got {criticalThreshold}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Verify threshold ordering
            TestBase.AssertTrue(criticalMissThreshold < hitThreshold,
                $"Critical miss threshold ({criticalMissThreshold}) should be < hit threshold ({hitThreshold})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(hitThreshold < comboThreshold,
                $"Hit threshold ({hitThreshold}) should be < combo threshold ({comboThreshold})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(comboThreshold < criticalThreshold,
                $"Combo threshold ({comboThreshold}) should be < critical threshold ({criticalThreshold})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestEnemyRollWithBonuses()
        {
            Console.WriteLine("\n--- Testing Enemy Roll With Bonuses ---");

            var enemy = TestDataBuilders.Enemy().WithName("TestEnemy").Build();
            var target = TestDataBuilders.Character().WithName("TestTarget").Build();
            var thresholdManager = RollModificationManager.GetThresholdManager();

            int comboThreshold = thresholdManager.GetComboThreshold(enemy);
            int criticalThreshold = thresholdManager.GetCriticalHitThreshold(enemy);

            // Test base roll 10 with +5 bonus = 15 (should be combo)
            int baseRoll = 10;
            int rollBonus = 5;
            int attackRoll = baseRoll + rollBonus;
            bool hits = HitCalculator.CalculateHit(enemy, target, rollBonus, attackRoll);
            TestBase.AssertTrue(hits,
                $"Enemy roll of {attackRoll} (base {baseRoll} + {rollBonus} bonus) should hit",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            bool isCombo = attackRoll >= comboThreshold;
            TestBase.AssertTrue(isCombo,
                $"Enemy roll of {attackRoll} should be combo with bonus",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test base roll 15 with +5 bonus = 20 (should be crit combo)
            baseRoll = 15;
            rollBonus = 5;
            attackRoll = baseRoll + rollBonus;
            hits = HitCalculator.CalculateHit(enemy, target, rollBonus, attackRoll);
            TestBase.AssertTrue(hits,
                $"Enemy roll of {attackRoll} (base {baseRoll} + {rollBonus} bonus) should hit",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            isCombo = attackRoll >= comboThreshold;
            TestBase.AssertTrue(isCombo,
                $"Enemy roll of {attackRoll} should be combo with bonus",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            bool isCritical = attackRoll >= criticalThreshold;
            TestBase.AssertTrue(isCritical,
                $"Enemy roll of {attackRoll} should be critical with bonus",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestEnemyRollWithPenalties()
        {
            Console.WriteLine("\n--- Testing Enemy Roll With Penalties ---");

            var enemy = TestDataBuilders.Enemy().WithName("TestEnemy").Build();
            var target = TestDataBuilders.Character().WithName("TestTarget").Build();
            var thresholdManager = RollModificationManager.GetThresholdManager();

            int hitThreshold = thresholdManager.GetHitThreshold(enemy);
            int comboThreshold = thresholdManager.GetComboThreshold(enemy);

            // Test base roll 10 with -5 penalty = 5 (should miss)
            int baseRoll = 10;
            int rollBonus = -5; // Penalty
            int attackRoll = baseRoll + rollBonus;
            bool hits = HitCalculator.CalculateHit(enemy, target, rollBonus, attackRoll);
            TestBase.AssertFalse(hits,
                $"Enemy roll of {attackRoll} (base {baseRoll} + {rollBonus} penalty) should miss",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test base roll 15 with -5 penalty = 10 (should hit but not combo)
            baseRoll = 15;
            rollBonus = -5;
            attackRoll = baseRoll + rollBonus;
            hits = HitCalculator.CalculateHit(enemy, target, rollBonus, attackRoll);
            TestBase.AssertTrue(hits,
                $"Enemy roll of {attackRoll} (base {baseRoll} + {rollBonus} penalty) should hit",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            bool isCombo = attackRoll >= comboThreshold;
            TestBase.AssertFalse(isCombo,
                $"Enemy roll of {attackRoll} should not be combo with penalty",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test base roll 20 with -5 penalty = 15 (should be combo but not critical)
            baseRoll = 20;
            rollBonus = -5;
            attackRoll = baseRoll + rollBonus;
            hits = HitCalculator.CalculateHit(enemy, target, rollBonus, attackRoll);
            TestBase.AssertTrue(hits,
                $"Enemy roll of {attackRoll} (base {baseRoll} + {rollBonus} penalty) should hit",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            isCombo = attackRoll >= comboThreshold;
            TestBase.AssertTrue(isCombo,
                $"Enemy roll of {attackRoll} should be combo even with penalty",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            bool isCritical = attackRoll >= thresholdManager.GetCriticalHitThreshold(enemy);
            TestBase.AssertFalse(isCritical,
                $"Enemy roll of {attackRoll} should not be critical with penalty",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
