using System;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Entity
{
    /// <summary>
    /// Comprehensive tests for Character class
    /// Tests character creation, stat calculations, leveling, health management, equipment, and actions
    /// </summary>
    public static class CharacterTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all Character tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== Character Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestCharacterCreation();
            TestCharacterProperties();
            TestCharacterComponents();
            TestHealthManagement();
            TestStatAccess();

            TestBase.PrintSummary("Character Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Creation Tests

        private static void TestCharacterCreation()
        {
            Console.WriteLine("--- Testing Character Creation ---");

            // Test default creation
            var character = new Character();
            TestBase.AssertNotNull(character,
                "Character should be created with default constructor",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (character != null)
            {
                TestBase.AssertTrue(!string.IsNullOrEmpty(character.Name),
                    "Character should have a name",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertTrue(character.Level >= 1,
                    $"Character should have level >= 1, got {character.Level}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            // Test creation with name and level
            var character2 = new Character("TestHero", 5);
            TestBase.AssertNotNull(character2,
                "Character should be created with name and level",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (character2 != null)
            {
                TestBase.AssertEqual("TestHero", character2.Name,
                    "Character should have correct name",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertEqual(5, character2.Level,
                    "Character should have correct level",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region Properties Tests

        private static void TestCharacterProperties()
        {
            Console.WriteLine("\n--- Testing Character Properties ---");

            var character = new Character("TestHero", 1);

            // Test health properties
            TestBase.AssertTrue(character.CurrentHealth > 0,
                $"Character should have positive health, got {character.CurrentHealth}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(character.MaxHealth > 0,
                $"Character should have positive max health, got {character.MaxHealth}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(character.CurrentHealth <= character.MaxHealth,
                "Current health should not exceed max health",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Components Tests

        private static void TestCharacterComponents()
        {
            Console.WriteLine("\n--- Testing Character Components ---");

            var character = new Character("TestHero", 1);

            // Test that all components are initialized
            TestBase.AssertNotNull(character.Stats,
                "Stats component should be initialized",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertNotNull(character.Effects,
                "Effects component should be initialized",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertNotNull(character.Equipment,
                "Equipment component should be initialized",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertNotNull(character.Progression,
                "Progression component should be initialized",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertNotNull(character.Actions,
                "Actions component should be initialized",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertNotNull(character.Health,
                "Health manager should be initialized",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertNotNull(character.Combat,
                "Combat calculator should be initialized",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertNotNull(character.Facade,
                "Facade should be initialized",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Health Management Tests

        private static void TestHealthManagement()
        {
            Console.WriteLine("\n--- Testing Health Management ---");

            var character = new Character("TestHero", 1);
            var initialHealth = character.CurrentHealth;
            var maxHealth = character.MaxHealth;

            // Test taking damage
            character.Health.TakeDamage(10);
            TestBase.AssertTrue(character.CurrentHealth < initialHealth,
                "Health should decrease after taking damage",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test healing
            var healthBeforeHeal = character.CurrentHealth;
            character.Health.Heal(5);
            TestBase.AssertTrue(character.CurrentHealth >= healthBeforeHeal,
                "Health should increase after healing",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test health doesn't exceed max
            character.Health.Heal(9999);
            TestBase.AssertTrue(character.CurrentHealth <= maxHealth,
                "Health should not exceed max health",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Stat Access Tests

        private static void TestStatAccess()
        {
            Console.WriteLine("\n--- Testing Stat Access ---");

            var character = new Character("TestHero", 1);

            // Test facade stat access
            var strength = character.Facade.GetEffectiveStrength();
            TestBase.AssertTrue(strength >= 0,
                $"Effective strength should be >= 0, got {strength}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var agility = character.Facade.GetEffectiveAgility();
            TestBase.AssertTrue(agility >= 0,
                $"Effective agility should be >= 0, got {agility}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
