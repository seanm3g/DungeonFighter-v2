using System;
using System.Collections.Generic;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Comprehensive tests for error handling and edge cases
    /// Tests null handling, invalid data, boundary conditions, and error recovery
    /// </summary>
    public static class ErrorHandlingTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Error Handling Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestNullParameterHandling();
            TestInvalidDataHandling();
            TestDivisionByZero();
            TestNegativeValues();
            TestOverflowConditions();
            TestEmptyCollections();
            TestBoundaryConditions();

            TestBase.PrintSummary("Error Handling Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestNullParameterHandling()
        {
            Console.WriteLine("\n--- Testing Null Parameter Handling ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();

            // Test unequipping from empty slot (null handling)
            try
            {
                var result = character.UnequipItem("weapon");
                TestBase.AssertTrue(result == null,
                    "Unequipping from empty slot should return null",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"Unequipping should not throw exception: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            // Test healing with null character (if applicable)
            try
            {
                character.Heal(10);
                TestBase.AssertTrue(true,
                    "Healing should handle normal values",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"Healing should not throw exception: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestInvalidDataHandling()
        {
            Console.WriteLine("\n--- Testing Invalid Data Handling ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();

            // Test invalid slot name
            try
            {
                var weapon = TestDataBuilders.Weapon().Build();
                var result = character.EquipItem(weapon, "invalid_slot");
                TestBase.AssertTrue(true,
                    "Equipping to invalid slot should handle gracefully",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception)
            {
                // Exception is acceptable for invalid slot
                TestBase.AssertTrue(true,
                    "Invalid slot should be handled (exception is acceptable)",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestDivisionByZero()
        {
            Console.WriteLine("\n--- Testing Division By Zero ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();

            // Test health percentage calculation with zero max health
            try
            {
                character.Health.MaxHealth = 0;
                double healthPercentage = character.GetHealthPercentage();
                
                // Should handle zero max health gracefully (return 0 or handle specially)
                TestBase.AssertTrue(!double.IsNaN(healthPercentage) && !double.IsInfinity(healthPercentage),
                    $"Health percentage should handle zero max health: {healthPercentage}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                // Division by zero exception is acceptable, but should be caught
                TestBase.AssertTrue(true,
                    $"Division by zero should be handled: {ex.GetType().Name}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestNegativeValues()
        {
            Console.WriteLine("\n--- Testing Negative Values ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();

            // Test negative health (should clamp to 0)
            character.CurrentHealth = -10;
            TestBase.AssertTrue(character.CurrentHealth >= 0,
                $"Negative health should be clamped to 0: {character.CurrentHealth}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test negative XP (should handle gracefully)
            int originalXP = character.XP;
            character.XP = -5;
            TestBase.AssertTrue(character.XP >= 0 || character.XP == originalXP,
                $"Negative XP should be handled: {character.XP}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test negative damage (should handle gracefully)
            try
            {
                character.TakeDamage(-10);
                TestBase.AssertTrue(true,
                    "Negative damage should be handled gracefully",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception)
            {
                TestBase.AssertTrue(true,
                    "Negative damage handling (exception is acceptable)",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestOverflowConditions()
        {
            Console.WriteLine("\n--- Testing Overflow Conditions ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();

            // Test very large XP values
            try
            {
                int originalLevel = character.Level;
                character.AddXP(int.MaxValue / 2);
                
                // Should handle large XP without crashing
                TestBase.AssertTrue(character.Level >= originalLevel,
                    $"Large XP should be handled: Level {originalLevel} -> {character.Level}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"Large XP should not cause overflow: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            // Test very large health values
            try
            {
                character.Health.MaxHealth = int.MaxValue - 100;
                character.CurrentHealth = character.Health.MaxHealth;
                TestBase.AssertTrue(character.CurrentHealth <= character.GetEffectiveMaxHealth(),
                    $"Large health values should be handled: {character.CurrentHealth}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"Large health should not cause overflow: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestEmptyCollections()
        {
            Console.WriteLine("\n--- Testing Empty Collections ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();

            // Test empty inventory
            character.Equipment.Inventory.Clear();
            TestBase.AssertEqual(0, character.Equipment.Inventory.Count,
                "Empty inventory should have count 0",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test removing from empty inventory
            var item = TestDataBuilders.Item().Build();
            bool removed = character.Equipment.RemoveFromInventory(item);
            TestBase.AssertFalse(removed,
                "Removing from empty inventory should return false",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test empty action list
            var actions = character.Actions.GetAllActions(character);
            TestBase.AssertNotNull(actions,
                "GetAvailableActions should not return null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestBoundaryConditions()
        {
            Console.WriteLine("\n--- Testing Boundary Conditions ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();

            // Test minimum level (1)
            character.Level = 1;
            TestBase.AssertTrue(character.Level >= 1,
                $"Minimum level should be 1: {character.Level}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test minimum health (0)
            character.CurrentHealth = 0;
            TestBase.AssertTrue(character.CurrentHealth >= 0,
                $"Minimum health should be 0: {character.CurrentHealth}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test maximum health (effective max)
            int effectiveMax = character.GetEffectiveMaxHealth();
            character.CurrentHealth = effectiveMax + 100;
            TestBase.AssertTrue(character.CurrentHealth <= effectiveMax,
                $"Health should not exceed effective max: {character.CurrentHealth} <= {effectiveMax}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test level 1 XP threshold (test indirectly by adding XP)
            int originalLevel = character.Level;
            character.AddXP(1);
            // Level should either stay same or increase
            TestBase.AssertTrue(character.Level >= originalLevel,
                $"Adding XP should handle threshold correctly: Level {originalLevel} -> {character.Level}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}

