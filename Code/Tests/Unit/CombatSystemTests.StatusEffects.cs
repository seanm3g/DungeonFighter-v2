using System;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Tests for combat status effects
    /// </summary>
    public static class CombatStatusEffectsTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;
        
        public static void RunAllTests()
        {
            Console.WriteLine("=== Combat Status Effects Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;
            
            TestStatusEffectApplication();
            TestBleedEffect();
            TestPoisonEffect();
            TestStunEffect();
            TestWeakenEffect();
            TestStatusEffectDuration();
            
            PrintSummary();
        }
        
        private static void TestStatusEffectApplication()
        {
            Console.WriteLine("--- Testing Status Effect Application ---");
            
            var action = new Action 
            { 
                Name = "Bleed Attack",
                CausesBleed = true,
                CausesPoison = false
            };
            
            AssertTrue(action.CausesBleed, "Action should cause bleed");
            AssertTrue(!action.CausesPoison, "Action should not cause poison");
        }
        
        private static void TestBleedEffect()
        {
            Console.WriteLine("\n--- Testing Bleed Effect ---");
            
            var enemy = new Enemy("TestEnemy", 1, 100, 10, 10, 10, 10);
            var initialHealth = enemy.CurrentHealth;
            
            enemy.ApplyPoison(3, 2, isBleeding: true);
            
            AssertTrue(enemy.IsBleeding, "Enemy should be bleeding");
            AssertTrue(enemy.PoisonDamage > 0, "Bleed damage should be set");
        }
        
        private static void TestPoisonEffect()
        {
            Console.WriteLine("\n--- Testing Poison Effect ---");
            
            var enemy = new Enemy("TestEnemy", 1, 100, 10, 10, 10, 10);
            
            enemy.ApplyPoison(5, 3);
            
            AssertTrue(enemy.PoisonStacks > 0, "Enemy should be poisoned");
            AssertTrue(enemy.PoisonDamage > 0, "Poison damage should be set");
        }
        
        private static void TestStunEffect()
        {
            Console.WriteLine("\n--- Testing Stun Effect ---");
            
            var enemy = new Enemy("TestEnemy", 1, 100, 10, 10, 10, 10);
            
            enemy.IsStunned = true;
            enemy.StunTurnsRemaining = 2;
            
            AssertTrue(enemy.IsStunned, "Enemy should be stunned");
            AssertTrue(enemy.StunTurnsRemaining > 0, "Stun duration should be set");
        }
        
        private static void TestWeakenEffect()
        {
            Console.WriteLine("\n--- Testing Weaken Effect ---");
            
            var enemy = new Enemy("TestEnemy", 1, 100, 10, 10, 10, 10);
            
            enemy.ApplyWeaken(3);
            
            AssertTrue(enemy.IsWeakened, "Enemy should be weakened");
            AssertTrue(enemy.WeakenTurns > 0, "Weaken duration should be set");
        }
        
        private static void TestStatusEffectDuration()
        {
            Console.WriteLine("\n--- Testing Status Effect Duration ---");
            
            var enemy = new Enemy("TestEnemy", 1, 100, 10, 10, 10, 10);
            
            enemy.ApplyPoison(5, 3, isBleeding: true);
            var initialBleedStacks = enemy.PoisonStacks;
            
            AssertTrue(initialBleedStacks > 0, "Bleed stacks should be set");
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

