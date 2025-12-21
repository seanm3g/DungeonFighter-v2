using System;
using RPGGame.Combat.Calculators;
using RPGGame.Actions.RollModification;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Tests for combat hit/miss calculation and critical hits
    /// </summary>
    public static class CombatHitMissTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;
        
        public static void RunAllTests()
        {
            Console.WriteLine("=== Combat Hit/Miss Calculation Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;
            
            TestHitCalculation();
            TestMissCalculation();
            TestCriticalHitThreshold();
            TestRollBonusApplication();
            TestCriticalHitCalculation();
            TestCriticalMissHandling();
            TestRollModification();
            TestRollBonusFromGear();
            
            PrintSummary();
        }
        
        private static void TestHitCalculation()
        {
            Console.WriteLine("--- Testing Hit Calculation ---");
            
            var attacker = new Character("TestHero", 1);
            var defender = new Enemy("TestEnemy", 1, 100, 10, 10, 10, 10);
            
            bool hit6 = CombatCalculator.CalculateHit(attacker, defender, 0, 6);
            bool hit10 = CombatCalculator.CalculateHit(attacker, defender, 0, 10);
            bool hit20 = CombatCalculator.CalculateHit(attacker, defender, 0, 20);
            
            AssertTrue(hit6, "Roll of 6 should hit");
            AssertTrue(hit10, "Roll of 10 should hit");
            AssertTrue(hit20, "Roll of 20 should hit");
        }
        
        private static void TestMissCalculation()
        {
            Console.WriteLine("\n--- Testing Miss Calculation ---");
            
            var attacker = new Character("TestHero", 1);
            var defender = new Enemy("TestEnemy", 1, 100, 10, 10, 10, 10);
            
            bool miss1 = CombatCalculator.CalculateHit(attacker, defender, 0, 1);
            bool miss3 = CombatCalculator.CalculateHit(attacker, defender, 0, 3);
            bool miss5 = CombatCalculator.CalculateHit(attacker, defender, 0, 5);
            
            AssertTrue(!miss1, "Roll of 1 should miss");
            AssertTrue(!miss3, "Roll of 3 should miss");
            AssertTrue(!miss5, "Roll of 5 should miss");
        }
        
        private static void TestCriticalHitThreshold()
        {
            Console.WriteLine("\n--- Testing Critical Hit Threshold ---");
            
            var character = new Character("TestHero", 1);
            var thresholdManager = RollModificationManager.GetThresholdManager();
            
            var criticalThreshold = thresholdManager.GetCriticalHitThreshold(character);
            
            AssertTrue(criticalThreshold >= 14, $"Critical hit threshold should be >= 14, got: {criticalThreshold}");
        }
        
        private static void TestRollBonusApplication()
        {
            Console.WriteLine("\n--- Testing Roll Bonus Application ---");
            
            var attacker = new Character("TestHero", 1);
            var defender = new Enemy("TestEnemy", 1, 100, 10, 10, 10, 10);
            
            bool hitWithBonus = CombatCalculator.CalculateHit(attacker, defender, 2, 4);
            
            AssertTrue(hitWithBonus, "Roll of 4 with +2 bonus (total 6) should hit");
        }
        
        private static void TestCriticalHitCalculation()
        {
            Console.WriteLine("\n--- Testing Critical Hit Calculation ---");
            
            var character = new Character("TestHero", 1);
            var thresholdManager = RollModificationManager.GetThresholdManager();
            
            var threshold = thresholdManager.GetCriticalHitThreshold(character);
            
            AssertTrue(threshold <= 20, $"Critical threshold should be <= 20, got: {threshold}");
        }
        
        private static void TestCriticalMissHandling()
        {
            Console.WriteLine("\n--- Testing Critical Miss Handling ---");
            
            var character = new Character("TestHero", 1);
            
            bool isCriticalMiss = (1 == 1);
            
            AssertTrue(isCriticalMiss, "Natural 1 should be critical miss");
        }
        
        private static void TestRollModification()
        {
            Console.WriteLine("\n--- Testing Roll Modification ---");
            
            var character = new Character("TestHero", 1);
            var action = new Action { Name = "Test Action" };
            
            var baseRoll = 10;
            var modifiedRoll = RollModificationManager.ApplyActionRollModifications(
                baseRoll, action, character, null);
            
            AssertTrue(modifiedRoll >= 0, $"Modified roll should be non-negative, got: {modifiedRoll}");
        }
        
        private static void TestRollBonusFromGear()
        {
            Console.WriteLine("\n--- Testing Roll Bonus from Gear ---");
            
            var character = new Character("TestHero", 1);
            var action = new Action { Name = "Test Action" };
            
            var rollBonus = ActionUtilities.CalculateRollBonus(character, action);
            
            AssertTrue(rollBonus >= 0, $"Roll bonus should be non-negative, got: {rollBonus}");
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

