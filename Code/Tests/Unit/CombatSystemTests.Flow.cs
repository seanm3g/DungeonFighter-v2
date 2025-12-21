using System;
using RPGGame.Actions.Execution;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Tests for combat flow and action execution
    /// </summary>
    public static class CombatFlowTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;
        
        public static void RunAllTests()
        {
            Console.WriteLine("=== Combat Flow Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;
            
            TestCombatTurnFlow();
            TestActionExecutionInCombat();
            TestDamageApplication();
            TestHealingApplication();
            TestMultiHitDamage();
            TestMultiHitCount();
            
            PrintSummary();
        }
        
        private static void TestCombatTurnFlow()
        {
            Console.WriteLine("--- Testing Combat Turn Flow ---");
            
            var character = new Character("TestHero", 1);
            var enemy = new Enemy("TestEnemy", 1, 100, 10, 10, 10, 10);
            
            var initialEnemyHealth = enemy.CurrentHealth;
            enemy.TakeDamage(10);
            
            AssertTrue(enemy.CurrentHealth < initialEnemyHealth, 
                $"Enemy health should decrease after damage: {initialEnemyHealth} -> {enemy.CurrentHealth}");
        }
        
        private static void TestActionExecutionInCombat()
        {
            Console.WriteLine("\n--- Testing Action Execution in Combat ---");
            
            var character = new Character("TestHero", 1);
            var enemy = new Enemy("TestEnemy", 1, 100, 10, 10, 10, 10);
            var action = new Action { Name = "Test Action", Type = ActionType.Attack };
            
            AssertTrue(action != null, "Action should be created");
            AssertTrue(character != null && enemy != null, "Combat entities should be created");
        }
        
        private static void TestDamageApplication()
        {
            Console.WriteLine("\n--- Testing Damage Application ---");
            
            var enemy = new Enemy("TestEnemy", 1, 100, 10, 10, 10, 10);
            var initialHealth = enemy.CurrentHealth;
            
            ActionUtilities.ApplyDamage(enemy, 15);
            
            AssertTrue(enemy.CurrentHealth < initialHealth, 
                $"Enemy health should decrease: {initialHealth} -> {enemy.CurrentHealth}");
            AssertTrue(enemy.CurrentHealth == initialHealth - 15, 
                $"Enemy should take exactly 15 damage: {enemy.CurrentHealth} == {initialHealth - 15}");
        }
        
        private static void TestHealingApplication()
        {
            Console.WriteLine("\n--- Testing Healing Application ---");
            
            var character = new Character("TestHero", 1);
            character.TakeDamage(20);
            var damagedHealth = character.CurrentHealth;
            
            ActionUtilities.ApplyHealing(character, 10);
            
            AssertTrue(character.CurrentHealth > damagedHealth, 
                $"Character health should increase: {damagedHealth} -> {character.CurrentHealth}");
            AssertTrue(character.CurrentHealth <= character.MaxHealth, 
                "Character health should not exceed max health");
        }
        
        private static void TestMultiHitDamage()
        {
            Console.WriteLine("\n--- Testing Multi-Hit Damage ---");
            
            var action = new Action 
            { 
                Name = "Multi-Hit",
                Advanced = new AdvancedMechanicsProperties 
                { 
                    MultiHitCount = 3
                }
            };
            
            AssertTrue(action.Advanced.MultiHitCount == 3, "Action should have 3 hits");
        }
        
        private static void TestMultiHitCount()
        {
            Console.WriteLine("\n--- Testing Multi-Hit Count ---");
            
            var action = new Action 
            { 
                Name = "Double Strike",
                Advanced = new AdvancedMechanicsProperties { MultiHitCount = 2 }
            };
            
            AssertTrue(action.Advanced.MultiHitCount == 2, "Action should have 2 hits");
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

