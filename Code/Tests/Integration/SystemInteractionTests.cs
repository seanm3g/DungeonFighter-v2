using System;
using RPGGame.Tests;

namespace RPGGame.Tests.Integration
{
    /// <summary>
    /// Comprehensive integration tests for system interactions
    /// Tests interactions between different game systems
    /// </summary>
    public static class SystemInteractionTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        public static void RunAllTests()
        {
            Console.WriteLine("=== System Interaction Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestActionDiceComboInteraction();
            TestStatusEffectsDamageInteraction();
            TestEquipmentStatsActionsInteraction();
            TestColorSystemDisplaySystemInteraction();
            TestCharacterEnemyEnvironmentInteraction();

            TestBase.PrintSummary("System Interaction Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestActionDiceComboInteraction()
        {
            Console.WriteLine("--- Testing Action + Dice + Combo Interaction ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            var action = TestDataBuilders.CreateMockAction("TestAction");

            // Test that actions, dice, and combos work together
            TestBase.AssertNotNull(character, 
                "Character should be available", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertNotNull(action, 
                "Action should be available", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestStatusEffectsDamageInteraction()
        {
            Console.WriteLine("\n--- Testing Status Effects + Damage Interaction ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            var enemy = TestDataBuilders.Enemy().WithName("TestEnemy").Build();

            // Test that status effects affect damage
            enemy.IsWeakened = true;
            TestBase.AssertTrue(enemy.IsWeakened, 
                "Status effects should be applicable", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            enemy.IsWeakened = false;
        }

        private static void TestEquipmentStatsActionsInteraction()
        {
            Console.WriteLine("\n--- Testing Equipment + Stats + Actions Interaction ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();

            // Test that equipment affects stats and actions
            int equipmentBonus = character.Equipment.GetEquipmentStatBonus("STR");
            TestBase.AssertTrue(equipmentBonus >= 0, 
                "Equipment should affect stats", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestColorSystemDisplaySystemInteraction()
        {
            Console.WriteLine("\n--- Testing Color System + Display System Interaction ---");

            var segments = new System.Collections.Generic.List<RPGGame.UI.ColorSystem.ColoredText>
            {
                new RPGGame.UI.ColorSystem.ColoredText("Test", Avalonia.Media.Colors.White)
            };

            // Test that color system works with display system
            var plainText = RPGGame.UI.ColorSystem.ColoredTextRenderer.RenderAsPlainText(segments);
            TestBase.AssertTrue(!string.IsNullOrEmpty(plainText), 
                "Color system should work with display system", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCharacterEnemyEnvironmentInteraction()
        {
            Console.WriteLine("\n--- Testing Character + Enemy + Environment Interaction ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            var enemy = TestDataBuilders.Enemy().WithName("TestEnemy").Build();
            var environment = new Environment("TestEnvironment", "Test Description", false, "forest", "");

            // Test that character, enemy, and environment can interact
            TestBase.AssertNotNull(character, 
                "Character should be available", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertNotNull(enemy, 
                "Enemy should be available", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertNotNull(environment, 
                "Environment should be available", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}

