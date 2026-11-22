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
            
            enemy.ApplyBleed(3, 2);
            
            AssertTrue(enemy.IsBleeding, "Enemy should be bleeding");
            AssertTrue(enemy.PendingBleedFromHits > 0, "Bleed pending should be set");
        }
        
        private static void TestStatusEffectDuration()
        {
            Console.WriteLine("\n--- Testing Status Effect Duration ---");
            
            var enemy = new Enemy("TestEnemy", 1, 100, 10, 10, 10, 10);
            
            enemy.ApplyBleed(5, 3);
            AssertTrue(enemy.PendingBleedFromHits > 0, "Bleed pending should be set");
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

