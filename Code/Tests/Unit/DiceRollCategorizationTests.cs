using System;
using RPGGame;
using RPGGame.Actions.RollModification;
using RPGGame.Combat;
using RPGGame.Combat.Calculators;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Comprehensive tests for dice roll categorization
    /// Tests the actual categorization of dice rolls into:
    /// - Critical Miss (base roll <= 1)
    /// - Miss (base roll 1-5, natural 1 always misses)
    /// - Hit (attack roll 6-13)
    /// - Combo Action (attack roll >= 14)
    /// - Critical Hit (attack roll >= 20)
    /// - Critical Hit + Combo (attack roll >= 20 AND >= 14)
    /// </summary>
    public static class DiceRollCategorizationTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Dice Roll Categorization Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestCriticalMissCategorization();
            TestMissCategorization();
            TestHitCategorization();
            TestComboActionCategorization();
            TestCriticalHitCategorization();
            TestCriticalHitComboCategorization();
            TestEdgeCases();
            TestWithRollBonuses();
            TestThresholdBoundaries();

            TestBase.PrintSummary("Dice Roll Categorization Tests", _testsRun, _testsPassed, _testsFailed);
        }

        /// <summary>
        /// Helper method to categorize a dice roll result
        /// Simulates the logic from ActionExecutionFlow
        /// </summary>
        private static RollCategory CategorizeRoll(Actor source, Actor target, int baseRoll, int rollBonus)
        {
            var thresholdManager = RollModificationManager.GetThresholdManager();
            
            int criticalMissThreshold = thresholdManager.GetCriticalMissThreshold(source);
            int comboThreshold = thresholdManager.GetComboThreshold(source);
            int criticalHitThreshold = thresholdManager.GetCriticalHitThreshold(source);
            
            int attackRoll = baseRoll + rollBonus;
            
            // Critical miss is based on base roll (from ActionExecutionFlow line 65)
            bool isCriticalMiss = baseRoll <= criticalMissThreshold;
            
            // Natural 1 always misses (from ActionExecutionFlow line 86-89)
            bool isMiss;
            if (baseRoll == 1)
            {
                isMiss = true;
            }
            else
            {
                // Use actual HitCalculator to determine hit/miss
                isMiss = !HitCalculator.CalculateHit(source, target, rollBonus, attackRoll);
            }
            
            // If it's a miss, return appropriate category
            if (isMiss)
            {
                return isCriticalMiss ? RollCategory.CriticalMiss : RollCategory.Miss;
            }
            
            // Determine if it's a combo action (from ActionExecutionFlow line 77)
            bool isCombo = attackRoll >= comboThreshold;
            
            // Determine if it's a critical hit (from ActionExecutionFlow line 78)
            bool isCritical = attackRoll >= criticalHitThreshold;
            
            // Categorize based on combinations
            if (isCritical && isCombo)
            {
                return RollCategory.CriticalHitCombo;
            }
            else if (isCritical)
            {
                return RollCategory.CriticalHit;
            }
            else if (isCombo)
            {
                return RollCategory.ComboAction;
            }
            else
            {
                return RollCategory.Hit;
            }
        }

        private static void TestCriticalMissCategorization()
        {
            Console.WriteLine("--- Testing Critical Miss Categorization ---");

            var attacker = TestDataBuilders.Character().WithName("TestHero").Build();
            var target = TestDataBuilders.Enemy().WithName("TestEnemy").Build();
            var thresholdManager = RollModificationManager.GetThresholdManager();
            thresholdManager.ResetThresholds(attacker); // Ensure default thresholds

            // Test base roll 1 (default critical miss threshold)
            var category1 = CategorizeRoll(attacker, target, 1, 0);
            TestBase.AssertEqualEnum(RollCategory.CriticalMiss, category1,
                "Base roll 1 should be critical miss",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test base roll 1 with bonus (still critical miss based on base roll)
            var category1Bonus = CategorizeRoll(attacker, target, 1, 10);
            TestBase.AssertEqualEnum(RollCategory.CriticalMiss, category1Bonus,
                "Base roll 1 with bonus should still be critical miss",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test with custom critical miss threshold
            thresholdManager.SetCriticalMissThreshold(attacker, 2);
            var category2 = CategorizeRoll(attacker, target, 2, 0);
            TestBase.AssertEqualEnum(RollCategory.CriticalMiss, category2,
                "Base roll 2 with threshold 2 should be critical miss",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Reset
            thresholdManager.ResetThresholds(attacker);
        }

        private static void TestMissCategorization()
        {
            Console.WriteLine("\n--- Testing Miss Categorization ---");

            var attacker = TestDataBuilders.Character().WithName("TestHero").Build();
            var target = TestDataBuilders.Enemy().WithName("TestEnemy").Build();
            var thresholdManager = RollModificationManager.GetThresholdManager();
            thresholdManager.ResetThresholds(attacker);

            // Test base rolls 2-5 (miss range, but not critical miss)
            for (int roll = 2; roll <= 5; roll++)
            {
                var category = CategorizeRoll(attacker, target, roll, 0);
                TestBase.AssertEqualEnum(RollCategory.Miss, category,
                    $"Base roll {roll} should be miss",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            // Test that natural 1 always misses (even with bonus)
            var category1 = CategorizeRoll(attacker, target, 1, 100);
            TestBase.AssertEqualEnum(RollCategory.CriticalMiss, category1,
                "Natural 1 with large bonus should still be critical miss",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHitCategorization()
        {
            Console.WriteLine("\n--- Testing Hit Categorization ---");

            var attacker = TestDataBuilders.Character().WithName("TestHero").Build();
            var target = TestDataBuilders.Enemy().WithName("TestEnemy").Build();
            var thresholdManager = RollModificationManager.GetThresholdManager();
            thresholdManager.ResetThresholds(attacker);

            // Test base rolls 6-13 (hit range, not combo)
            for (int roll = 6; roll <= 13; roll++)
            {
                var category = CategorizeRoll(attacker, target, roll, 0);
                TestBase.AssertEqualEnum(RollCategory.Hit, category,
                    $"Base roll {roll} should be hit (not combo)",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            // Test edge case: roll 6 (minimum hit)
            var category6 = CategorizeRoll(attacker, target, 6, 0);
            TestBase.AssertEqualEnum(RollCategory.Hit, category6,
                "Base roll 6 should be hit",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test edge case: roll 13 (maximum hit before combo)
            var category13 = CategorizeRoll(attacker, target, 13, 0);
            TestBase.AssertEqualEnum(RollCategory.Hit, category13,
                "Base roll 13 should be hit (not combo)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestComboActionCategorization()
        {
            Console.WriteLine("\n--- Testing Combo Action Categorization ---");

            var attacker = TestDataBuilders.Character().WithName("TestHero").Build();
            var target = TestDataBuilders.Enemy().WithName("TestEnemy").Build();
            var thresholdManager = RollModificationManager.GetThresholdManager();
            thresholdManager.ResetThresholds(attacker);

            // Test base rolls 14-19 (combo range, not critical)
            for (int roll = 14; roll <= 19; roll++)
            {
                var category = CategorizeRoll(attacker, target, roll, 0);
                TestBase.AssertEqualEnum(RollCategory.ComboAction, category,
                    $"Base roll {roll} should be combo action",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            // Test edge case: roll 14 (minimum combo)
            var category14 = CategorizeRoll(attacker, target, 14, 0);
            TestBase.AssertEqualEnum(RollCategory.ComboAction, category14,
                "Base roll 14 should be combo action",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test edge case: roll 19 (maximum combo before critical)
            var category19 = CategorizeRoll(attacker, target, 19, 0);
            TestBase.AssertEqualEnum(RollCategory.ComboAction, category19,
                "Base roll 19 should be combo action (not critical)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCriticalHitCategorization()
        {
            Console.WriteLine("\n--- Testing Critical Hit Categorization ---");

            var attacker = TestDataBuilders.Character().WithName("TestHero").Build();
            var target = TestDataBuilders.Enemy().WithName("TestEnemy").Build();
            var thresholdManager = RollModificationManager.GetThresholdManager();
            thresholdManager.ResetThresholds(attacker);

            // Test base roll 20 (critical hit, also combo)
            var category20 = CategorizeRoll(attacker, target, 20, 0);
            TestBase.AssertEqualEnum(RollCategory.CriticalHitCombo, category20,
                "Base roll 20 should be critical hit + combo",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test base roll 20+ (critical hit, also combo)
            // Note: In practice, base roll can't exceed 20, but attack roll can with bonuses
            // Test with bonus to get attack roll > 20
            var category20Plus = CategorizeRoll(attacker, target, 20, 5);
            TestBase.AssertEqualEnum(RollCategory.CriticalHitCombo, category20Plus,
                "Base roll 20 with bonus should be critical hit + combo",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test with custom critical hit threshold
            thresholdManager.SetCriticalHitThreshold(attacker, 18);
            var category18 = CategorizeRoll(attacker, target, 18, 0);
            TestBase.AssertEqualEnum(RollCategory.CriticalHitCombo, category18,
                "Base roll 18 with threshold 18 should be critical hit + combo",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Reset
            thresholdManager.ResetThresholds(attacker);
        }

        private static void TestCriticalHitComboCategorization()
        {
            Console.WriteLine("\n--- Testing Critical Hit + Combo Categorization ---");

            var attacker = TestDataBuilders.Character().WithName("TestHero").Build();
            var target = TestDataBuilders.Enemy().WithName("TestEnemy").Build();
            var thresholdManager = RollModificationManager.GetThresholdManager();
            thresholdManager.ResetThresholds(attacker);

            // Test that roll 20+ is both critical hit AND combo
            var category20 = CategorizeRoll(attacker, target, 20, 0);
            TestBase.AssertEqualEnum(RollCategory.CriticalHitCombo, category20,
                "Base roll 20 should be critical hit + combo",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test with bonus that pushes attack roll to 20+
            var category15Bonus = CategorizeRoll(attacker, target, 15, 5);
            TestBase.AssertEqualEnum(RollCategory.CriticalHitCombo, category15Bonus,
                "Base roll 15 with +5 bonus (attack roll 20) should be critical hit + combo",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test with larger bonus
            var category10Bonus = CategorizeRoll(attacker, target, 10, 10);
            TestBase.AssertEqualEnum(RollCategory.CriticalHitCombo, category10Bonus,
                "Base roll 10 with +10 bonus (attack roll 20) should be critical hit + combo",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestEdgeCases()
        {
            Console.WriteLine("\n--- Testing Edge Cases ---");

            var attacker = TestDataBuilders.Character().WithName("TestHero").Build();
            var target = TestDataBuilders.Enemy().WithName("TestEnemy").Build();
            var thresholdManager = RollModificationManager.GetThresholdManager();
            thresholdManager.ResetThresholds(attacker);

            // Test boundary: roll 5 (miss) vs roll 6 (hit)
            var category5 = CategorizeRoll(attacker, target, 5, 0);
            TestBase.AssertEqualEnum(RollCategory.Miss, category5,
                "Base roll 5 should be miss",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var category6 = CategorizeRoll(attacker, target, 6, 0);
            TestBase.AssertEqualEnum(RollCategory.Hit, category6,
                "Base roll 6 should be hit",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test boundary: roll 13 (hit) vs roll 14 (combo)
            var category13 = CategorizeRoll(attacker, target, 13, 0);
            TestBase.AssertEqualEnum(RollCategory.Hit, category13,
                "Base roll 13 should be hit",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var category14 = CategorizeRoll(attacker, target, 14, 0);
            TestBase.AssertEqualEnum(RollCategory.ComboAction, category14,
                "Base roll 14 should be combo action",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test boundary: roll 19 (combo) vs roll 20 (critical + combo)
            var category19 = CategorizeRoll(attacker, target, 19, 0);
            TestBase.AssertEqualEnum(RollCategory.ComboAction, category19,
                "Base roll 19 should be combo action",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var category20 = CategorizeRoll(attacker, target, 20, 0);
            TestBase.AssertEqualEnum(RollCategory.CriticalHitCombo, category20,
                "Base roll 20 should be critical hit + combo",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestWithRollBonuses()
        {
            Console.WriteLine("\n--- Testing With Roll Bonuses ---");

            var attacker = TestDataBuilders.Character().WithName("TestHero").Build();
            var target = TestDataBuilders.Enemy().WithName("TestEnemy").Build();
            var thresholdManager = RollModificationManager.GetThresholdManager();
            thresholdManager.ResetThresholds(attacker);

            // Test that bonuses affect categorization correctly
            // Base roll 10 (hit) with +4 bonus = attack roll 14 (combo)
            var category10Plus4 = CategorizeRoll(attacker, target, 10, 4);
            TestBase.AssertEqualEnum(RollCategory.ComboAction, category10Plus4,
                "Base roll 10 with +4 bonus (attack roll 14) should be combo action",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Base roll 10 (hit) with +10 bonus = attack roll 20 (critical + combo)
            var category10Plus10 = CategorizeRoll(attacker, target, 10, 10);
            TestBase.AssertEqualEnum(RollCategory.CriticalHitCombo, category10Plus10,
                "Base roll 10 with +10 bonus (attack roll 20) should be critical hit + combo",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Base roll 4 (miss) with +2 bonus = attack roll 6 (hit)
            var category4Plus2 = CategorizeRoll(attacker, target, 4, 2);
            TestBase.AssertEqualEnum(RollCategory.Hit, category4Plus2,
                "Base roll 4 with +2 bonus (attack roll 6) should be hit",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Base roll 1 (critical miss) with any bonus = still critical miss
            var category1Plus20 = CategorizeRoll(attacker, target, 1, 20);
            TestBase.AssertEqualEnum(RollCategory.CriticalMiss, category1Plus20,
                "Base roll 1 with +20 bonus should still be critical miss",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestThresholdBoundaries()
        {
            Console.WriteLine("\n--- Testing Threshold Boundaries ---");

            var attacker = TestDataBuilders.Character().WithName("TestHero").Build();
            var target = TestDataBuilders.Enemy().WithName("TestEnemy").Build();
            var thresholdManager = RollModificationManager.GetThresholdManager();
            thresholdManager.ResetThresholds(attacker);

            // Test all threshold boundaries
            int hitThreshold = thresholdManager.GetHitThreshold(attacker);
            int comboThreshold = thresholdManager.GetComboThreshold(attacker);
            int criticalHitThreshold = thresholdManager.GetCriticalHitThreshold(attacker);
            int criticalMissThreshold = thresholdManager.GetCriticalMissThreshold(attacker);

            // Test just below hit threshold
            var categoryBelowHit = CategorizeRoll(attacker, target, hitThreshold, 0);
            TestBase.AssertTrue(categoryBelowHit == RollCategory.Miss || categoryBelowHit == RollCategory.CriticalMiss,
                $"Base roll {hitThreshold} (hit threshold) should be miss",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test at hit threshold + 1
            var categoryAtHit = CategorizeRoll(attacker, target, hitThreshold + 1, 0);
            TestBase.AssertEqualEnum(RollCategory.Hit, categoryAtHit,
                $"Base roll {hitThreshold + 1} (hit threshold + 1) should be hit",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test just below combo threshold
            var categoryBelowCombo = CategorizeRoll(attacker, target, comboThreshold - 1, 0);
            TestBase.AssertEqualEnum(RollCategory.Hit, categoryBelowCombo,
                $"Base roll {comboThreshold - 1} (combo threshold - 1) should be hit",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test at combo threshold
            var categoryAtCombo = CategorizeRoll(attacker, target, comboThreshold, 0);
            TestBase.AssertEqualEnum(RollCategory.ComboAction, categoryAtCombo,
                $"Base roll {comboThreshold} (combo threshold) should be combo action",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test just below critical hit threshold
            var categoryBelowCritical = CategorizeRoll(attacker, target, criticalHitThreshold - 1, 0);
            TestBase.AssertEqualEnum(RollCategory.ComboAction, categoryBelowCritical,
                $"Base roll {criticalHitThreshold - 1} (critical threshold - 1) should be combo action",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test at critical hit threshold
            var categoryAtCritical = CategorizeRoll(attacker, target, criticalHitThreshold, 0);
            TestBase.AssertEqualEnum(RollCategory.CriticalHitCombo, categoryAtCritical,
                $"Base roll {criticalHitThreshold} (critical threshold) should be critical hit + combo",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test at critical miss threshold
            var categoryAtCriticalMiss = CategorizeRoll(attacker, target, criticalMissThreshold, 0);
            TestBase.AssertEqualEnum(RollCategory.CriticalMiss, categoryAtCriticalMiss,
                $"Base roll {criticalMissThreshold} (critical miss threshold) should be critical miss",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }

    /// <summary>
    /// Enum representing the different categories a dice roll can fall into
    /// </summary>
    public enum RollCategory
    {
        CriticalMiss,      // Base roll <= critical miss threshold (default: 1)
        Miss,              // Base roll 1-5 (but natural 1 is critical miss)
        Hit,               // Attack roll 6-13 (normal attack range)
        ComboAction,       // Attack roll >= 14 (combo threshold)
        CriticalHit,       // Attack roll >= 20 (critical hit threshold) - should not occur without combo
        CriticalHitCombo   // Attack roll >= 20 AND >= 14 (both critical and combo)
    }
}

