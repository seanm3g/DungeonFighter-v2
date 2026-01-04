using System;
using RPGGame.Combat.Calculators;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>
    /// Comprehensive tests for DamageCalculator
    /// Tests damage calculations, caching, and edge cases
    /// </summary>
    public static class DamageCalculatorTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all DamageCalculator tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== DamageCalculator Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            // Clear cache before tests
            DamageCalculator.ClearAllCaches();

            TestCalculateRawDamage();
            TestCalculateDamage();
            TestCacheInvalidation();
            TestCacheClearing();
            TestCacheStats();
            TestEdgeCases();
            TestDamageWithArmor();
            TestDamageWithMultipliers();

            TestBase.PrintSummary("DamageCalculator Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Raw Damage Tests

        private static void TestCalculateRawDamage()
        {
            Console.WriteLine("--- Testing CalculateRawDamage ---");

            var attacker = TestDataBuilders.Character()
                .WithName("Attacker")
                .WithStats(10, 10, 10, 10)
                .Build();

            var action = TestDataBuilders.CreateMockAction("JAB");
            action.DamageMultiplier = 1.0;

            // Equip a weapon (required for damage in real game)
            var weapon = new WeaponItem("TestSword", 1, 10);
            attacker.EquipItem(weapon, "weapon");
            
            // Test basic raw damage calculation
            var damage = DamageCalculator.CalculateRawDamage(attacker, action, 1.0, 1.0, 10);
            
            // CRITICAL: Raw damage should ALWAYS be positive, not just non-negative
            TestBase.AssertTrue(damage > 0,
                $"Raw damage should be positive, got: {damage}. This indicates a critical bug!",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test with combo amplifier
            var comboDamage = DamageCalculator.CalculateRawDamage(attacker, action, 2.0, 1.0, 10);
            TestBase.AssertTrue(comboDamage >= damage,
                "Combo amplifier should increase damage",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test with damage multiplier
            var multiplierDamage = DamageCalculator.CalculateRawDamage(attacker, action, 1.0, 2.0, 10);
            TestBase.AssertTrue(multiplierDamage >= damage,
                "Damage multiplier should increase damage",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Final Damage Tests

        private static void TestCalculateDamage()
        {
            Console.WriteLine("\n--- Testing CalculateDamage ---");

            var attacker = TestDataBuilders.Character()
                .WithName("Attacker")
                .WithStats(10, 10, 10, 10)
                .Build();

            var target = TestDataBuilders.Enemy()
                .WithName("Target")
                .WithHealth(100)
                .Build();

            // Equip a weapon (required for damage in real game)
            var weapon = new WeaponItem("TestSword", 1, 10);
            attacker.EquipItem(weapon, "weapon");

            var action = TestDataBuilders.CreateMockAction("JAB");
            action.DamageMultiplier = 1.0;

            // Test basic damage calculation
            var damage = DamageCalculator.CalculateDamage(attacker, target, action, 1.0, 1.0, 0, 10);
            
            // CRITICAL: Damage should ALWAYS be positive, not just non-negative
            TestBase.AssertTrue(damage > 0,
                $"Damage should be positive, got: {damage}. This indicates a critical bug!",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Damage should be less than or equal to raw damage (due to armor)
            var rawDamage = DamageCalculator.CalculateRawDamage(attacker, action, 1.0, 1.0, 10);
            TestBase.AssertTrue(damage <= rawDamage,
                "Final damage should be less than or equal to raw damage",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestDamageWithArmor()
        {
            Console.WriteLine("\n--- Testing DamageWithArmor ---");

            var attacker = TestDataBuilders.Character()
                .WithName("Attacker")
                .WithStats(10, 10, 10, 10)
                .Build();

            var lowArmorTarget = TestDataBuilders.Enemy()
                .WithName("LowArmor")
                .WithHealth(100)
                .Build();

            var action = TestDataBuilders.CreateMockAction("JAB");
            action.DamageMultiplier = 1.0;

            var damageLowArmor = DamageCalculator.CalculateDamage(attacker, lowArmorTarget, action, 1.0, 1.0, 0, 10);
            
            TestBase.AssertTrue(damageLowArmor >= 0,
                "Damage against low armor target should be calculated",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestDamageWithMultipliers()
        {
            Console.WriteLine("\n--- Testing DamageWithMultipliers ---");

            var attacker = TestDataBuilders.Character()
                .WithName("Attacker")
                .WithStats(10, 10, 10, 10)
                .Build();

            var target = TestDataBuilders.Enemy()
                .WithName("Target")
                .WithHealth(100)
                .Build();

            var action = TestDataBuilders.CreateMockAction("JAB");
            action.DamageMultiplier = 1.0;

            // Test with different multipliers
            var baseDamage = DamageCalculator.CalculateDamage(attacker, target, action, 1.0, 1.0, 0, 10);
            var doubleDamage = DamageCalculator.CalculateDamage(attacker, target, action, 2.0, 1.0, 0, 10);
            var tripleDamage = DamageCalculator.CalculateDamage(attacker, target, action, 1.0, 3.0, 0, 10);

            TestBase.AssertTrue(doubleDamage >= baseDamage,
                "Double combo amplifier should increase damage",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(tripleDamage >= baseDamage,
                "Triple damage multiplier should increase damage",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Cache Tests

        private static void TestCacheInvalidation()
        {
            Console.WriteLine("\n--- Testing CacheInvalidation ---");

            var actor = TestDataBuilders.Character()
                .WithName("Actor")
                .WithStats(10, 10, 10, 10)
                .Build();

            var action = TestDataBuilders.CreateMockAction("JAB");

            // Calculate damage to populate cache
            DamageCalculator.CalculateRawDamage(actor, action, 1.0, 1.0, 10);

            // Invalidate cache
            DamageCalculator.InvalidateCache(actor);

            // Should not throw exception
            TestBase.AssertTrue(true,
                "Cache invalidation should complete without errors",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCacheClearing()
        {
            Console.WriteLine("\n--- Testing CacheClearing ---");

            var actor = TestDataBuilders.Character()
                .WithName("Actor")
                .WithStats(10, 10, 10, 10)
                .Build();

            var action = TestDataBuilders.CreateMockAction("JAB");

            // Calculate damage to populate cache
            DamageCalculator.CalculateRawDamage(actor, action, 1.0, 1.0, 10);

            // Clear all caches
            DamageCalculator.ClearAllCaches();

            // Should not throw exception
            TestBase.AssertTrue(true,
                "Cache clearing should complete without errors",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCacheStats()
        {
            Console.WriteLine("\n--- Testing CacheStats ---");

            // Get cache stats
            var stats = DamageCalculator.GetCacheStats();

            TestBase.AssertTrue(stats.rawHits >= 0,
                "Raw cache hits should be non-negative",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(stats.rawMisses >= 0,
                "Raw cache misses should be non-negative",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(stats.finalHits >= 0,
                "Final cache hits should be non-negative",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(stats.finalMisses >= 0,
                "Final cache misses should be non-negative",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(stats.rawHitRate >= 0 && stats.rawHitRate <= 1,
                "Raw hit rate should be between 0 and 1",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(stats.finalHitRate >= 0 && stats.finalHitRate <= 1,
                "Final hit rate should be between 0 and 1",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Edge Cases

        private static void TestEdgeCases()
        {
            Console.WriteLine("\n--- Testing EdgeCases ---");

            var attacker = TestDataBuilders.Character()
                .WithName("Attacker")
                .WithStats(1, 1, 1, 1)
                .Build();

            var target = TestDataBuilders.Enemy()
                .WithName("Target")
                .WithHealth(1)
                .Build();

            var action = TestDataBuilders.CreateMockAction("JAB");
            action.DamageMultiplier = 0.1;

            // Test with very low stats
            var lowDamage = DamageCalculator.CalculateDamage(attacker, target, action, 1.0, 1.0, 0, 1);
            TestBase.AssertTrue(lowDamage >= 0,
                "Damage with low stats should be non-negative",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test with zero multiplier
            var zeroDamage = DamageCalculator.CalculateDamage(attacker, target, action, 0.0, 1.0, 0, 10);
            TestBase.AssertTrue(zeroDamage >= 0,
                "Damage with zero multiplier should be non-negative",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test with null action
            var nullActionDamage = DamageCalculator.CalculateRawDamage(attacker, null, 1.0, 1.0, 10);
            TestBase.AssertTrue(nullActionDamage >= 0,
                "Damage with null action should be non-negative",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test with very high roll
            var highRollDamage = DamageCalculator.CalculateDamage(attacker, target, action, 1.0, 1.0, 0, 20);
            TestBase.AssertTrue(highRollDamage >= 0,
                "Damage with high roll should be non-negative",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
