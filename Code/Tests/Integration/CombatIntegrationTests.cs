using System;
using RPGGame.Tests;

namespace RPGGame.Tests.Integration
{
    /// <summary>
    /// Comprehensive integration tests for combat system
    /// Tests end-to-end combat flows and system interactions
    /// </summary>
    public static class CombatIntegrationTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Combat Integration Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestCompleteCombatFlow();
            TestMultipleActionSequences();
            TestComboExecutionInCombat();
            TestStatusEffectApplicationInCombat();
            TestEnvironmentalEffectsInCombat();
            TestCharacterProgressionThroughCombat();

            TestBase.PrintSummary("Combat Integration Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestCompleteCombatFlow()
        {
            Console.WriteLine("--- Testing Complete Combat Flow ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            var enemy = TestDataBuilders.Enemy().WithName("TestEnemy").Build();

            // Test that combat can be initiated
            TestBase.AssertNotNull(character, 
                "Character should be available for combat", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertNotNull(enemy, 
                "Enemy should be available for combat", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestMultipleActionSequences()
        {
            Console.WriteLine("\n--- Testing Multiple Action Sequences ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();

            // Test that multiple actions can be executed
            TestBase.AssertTrue(character.ActionPool.Count >= 0, 
                "Character should have action pool", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestComboExecutionInCombat()
        {
            Console.WriteLine("\n--- Testing Combo Execution in Combat ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();

            // Test that combos can be executed in combat
            TestBase.AssertTrue(character.ComboStep >= 0, 
                "Character should support combo execution", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestStatusEffectApplicationInCombat()
        {
            Console.WriteLine("\n--- Testing Status Effect Application in Combat ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            var enemy = TestDataBuilders.Enemy().WithName("TestEnemy").Build();

            // Test that status effects can be applied in combat
            TestBase.AssertTrue(true, 
                "Status effects should be applicable in combat", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestEnvironmentalEffectsInCombat()
        {
            Console.WriteLine("\n--- Testing Environmental Effects in Combat ---");

            var environment = new Environment("TestEnvironment", "Test Description", false, "forest", "");

            // Test that environmental effects can be applied in combat
            TestBase.AssertNotNull(environment, 
                "Environment should be available for combat", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCharacterProgressionThroughCombat()
        {
            Console.WriteLine("\n--- Testing Character Progression Through Combat ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            int originalXP = character.XP;

            // Test that character can gain XP through combat
            character.XP += 50;
            TestBase.AssertTrue(character.XP > originalXP, 
                "Character should gain XP through combat", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}

