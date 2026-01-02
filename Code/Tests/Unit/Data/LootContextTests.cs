using System;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    /// <summary>
    /// Comprehensive tests for LootContext
    /// Tests context creation and property setting
    /// </summary>
    public static class LootContextTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all LootContext tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== LootContext Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestCreateWithCharacter();
            TestCreateWithDungeonTheme();
            TestCreateWithEnemyArchetype();
            TestCreateWithAllProperties();
            TestCreateWithNullCharacter();
            TestPropertySetting();

            TestBase.PrintSummary("LootContext Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Creation Tests

        private static void TestCreateWithCharacter()
        {
            Console.WriteLine("--- Testing Create with Character ---");

            var character = TestDataBuilders.CreateTestCharacter();
            var context = LootContext.Create(character);

            TestBase.AssertNotNull(context,
                "Context should be created",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (context != null)
            {
                TestBase.AssertNotNull(context.PlayerClass,
                    "Player class should be set from character",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestCreateWithDungeonTheme()
        {
            Console.WriteLine("\n--- Testing Create with Dungeon Theme ---");

            var context = LootContext.Create(null, "Forest");

            TestBase.AssertNotNull(context,
                "Context should be created",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (context != null)
            {
                TestBase.AssertEqual("Forest", context.DungeonTheme,
                    "Dungeon theme should be set",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestCreateWithEnemyArchetype()
        {
            Console.WriteLine("\n--- Testing Create with Enemy Archetype ---");

            var context = LootContext.Create(null, null, "Berserker");

            TestBase.AssertNotNull(context,
                "Context should be created",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (context != null)
            {
                TestBase.AssertEqual("Berserker", context.EnemyArchetype,
                    "Enemy archetype should be set",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestCreateWithAllProperties()
        {
            Console.WriteLine("\n--- Testing Create with All Properties ---");

            var character = TestDataBuilders.CreateTestCharacter();
            var context = LootContext.Create(character, "Lava", "Guardian");

            TestBase.AssertNotNull(context,
                "Context should be created",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (context != null)
            {
                TestBase.AssertNotNull(context.PlayerClass,
                    "Player class should be set",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual("Lava", context.DungeonTheme,
                    "Dungeon theme should be set",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual("Guardian", context.EnemyArchetype,
                    "Enemy archetype should be set",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestCreateWithNullCharacter()
        {
            Console.WriteLine("\n--- Testing Create with Null Character ---");

            var context = LootContext.Create(null);

            TestBase.AssertNotNull(context,
                "Context should be created even with null character",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (context != null)
            {
                TestBase.AssertNull(context.PlayerClass,
                    "Player class should be null when character is null",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region Property Tests

        private static void TestPropertySetting()
        {
            Console.WriteLine("\n--- Testing Property Setting ---");

            var context = new LootContext();

            // Test setting all properties
            context.PlayerClass = "Warrior";
            context.DungeonTheme = "Crypt";
            context.EnemyArchetype = "Assassin";
            context.WeaponType = "Sword";

            TestBase.AssertEqual("Warrior", context.PlayerClass,
                "Player class should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("Crypt", context.DungeonTheme,
                "Dungeon theme should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("Assassin", context.EnemyArchetype,
                "Enemy archetype should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("Sword", context.WeaponType,
                "Weapon type should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
