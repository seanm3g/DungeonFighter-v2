using System;
using RPGGame.Combat.Calculators;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Tests for combat damage calculation
    /// </summary>
    public static class CombatDamageTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;
        
        public static void RunAllTests()
        {
            Console.WriteLine("=== Combat Damage Calculation Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;
            
            TestRawDamageCalculation();
            TestDamageWithMultiplier();
            TestDamageWithArmor();
            TestDamageWithWeakenedTarget();
            TestMinimumDamage();
            TestDamageWithActionMultiplier();
            
            PrintSummary();
        }
        
        private static void TestRawDamageCalculation()
        {
            Console.WriteLine("--- Testing Raw Damage Calculation ---");
            
            var character = new Character("TestHero", 1);
            character.Stats.Strength = 10;
            
            var damage = CombatCalculator.CalculateRawDamage(character);
            
            AssertTrue(damage > 0, $"Raw damage should be positive, got: {damage}");
        }
        
        private static void TestDamageWithMultiplier()
        {
            Console.WriteLine("\n--- Testing Damage with Multiplier ---");
            
            var character = new Character("TestHero", 1);
            character.Stats.Strength = 10;
            var action = new Action { DamageMultiplier = 2.0 };
            
            var damage = CombatCalculator.CalculateRawDamage(character, action);
            
            AssertTrue(damage > 0, $"Damage with multiplier should be positive, got: {damage}");
        }
        
        private static void TestDamageWithArmor()
        {
            Console.WriteLine("\n--- Testing Damage with Armor ---");
            
            var attacker = new Character("TestHero", 1);
            attacker.Stats.Strength = 20;
            
            var defender = new Enemy("TestEnemy", 1, 100, 10, 10, 10, 10);
            
            var damage = CombatCalculator.CalculateDamage(attacker, defender);
            
            AssertTrue(damage >= 0, $"Damage after armor should be non-negative, got: {damage}");
            var rawDamage = CombatCalculator.CalculateRawDamage(attacker);
            AssertTrue(damage <= rawDamage, $"Armored damage ({damage}) should be <= raw damage ({rawDamage})");
        }
        
        private static void TestDamageWithWeakenedTarget()
        {
            Console.WriteLine("\n--- Testing Damage with Weakened Target ---");
            
            var attacker = new Character("TestHero", 1);
            attacker.Stats.Strength = 10;
            
            var defender = new Enemy("TestEnemy", 1, 100, 10, 10, 10, 10);
            defender.IsWeakened = true;
            
            var normalDamage = CombatCalculator.CalculateDamage(attacker, defender, showWeakenedMessage: false);
            defender.IsWeakened = false;
            var weakenedDamage = CombatCalculator.CalculateDamage(attacker, defender, showWeakenedMessage: false);
            
            AssertTrue(weakenedDamage >= 0, $"Weakened damage should be non-negative, got: {weakenedDamage}");
        }
        
        private static void TestMinimumDamage()
        {
            Console.WriteLine("\n--- Testing Minimum Damage ---");
            
            var attacker = new Character("TestHero", 1);
            attacker.Stats.Strength = 1;
            
            var defender = new Enemy("TestEnemy", 1, 100, 10, 10, 100, 10);
            
            var damage = CombatCalculator.CalculateDamage(attacker, defender);
            
            var minDamage = GameConfiguration.Instance.Combat.MinimumDamage;
            AssertTrue(damage >= minDamage, $"Damage should be at least minimum ({minDamage}), got: {damage}");
        }
        
        private static void TestDamageWithActionMultiplier()
        {
            Console.WriteLine("\n--- Testing Damage with Action Multiplier ---");
            
            var character = new Character("TestHero", 1);
            character.Stats.Strength = 10;
            
            var action1 = new Action { DamageMultiplier = 1.0 };
            var action2 = new Action { DamageMultiplier = 2.0 };
            
            var damage1 = CombatCalculator.CalculateRawDamage(character, action1);
            var damage2 = CombatCalculator.CalculateRawDamage(character, action2);
            
            AssertTrue(damage2 >= damage1, $"Damage with 2.0x multiplier ({damage2}) should be >= damage with 1.0x ({damage1})");
        }
        
        private static void AssertTrue(bool condition, string message)
        {
            _testsRun++;
            if (condition)
            {
                _testsPassed++;
                Console.WriteLine($"  ✓ {message}");
            }
            else
            {
                _testsFailed++;
                Console.WriteLine($"  ✗ FAILED: {message}");
            }
        }
        
        private static void PrintSummary()
        {
            Console.WriteLine("\n=== Test Summary ===");
            Console.WriteLine($"Total Tests: {_testsRun}");
            Console.WriteLine($"Passed: {_testsPassed}");
            Console.WriteLine($"Failed: {_testsFailed}");
            Console.WriteLine($"Success Rate: {(_testsPassed * 100.0 / _testsRun):F1}%");
            
            if (_testsFailed == 0)
            {
                Console.WriteLine("\n✅ All tests passed!");
            }
            else
            {
                Console.WriteLine($"\n❌ {_testsFailed} test(s) failed");
            }
        }
    }
}

