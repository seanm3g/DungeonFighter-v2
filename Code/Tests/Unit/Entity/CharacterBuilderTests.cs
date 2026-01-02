using System;
using System.Collections.Generic;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Entity
{
    /// <summary>
    /// Comprehensive tests for CharacterBuilder
    /// Tests builder pattern for character creation
    /// </summary>
    public static class CharacterBuilderTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all CharacterBuilder tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== CharacterBuilder Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestWithName();
            TestWithLevel();
            TestWithInventory();
            TestBuild();
            TestCreateDefault();
            TestCreateWithInventory();

            TestBase.PrintSummary("CharacterBuilder Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Builder Tests

        private static void TestWithName()
        {
            Console.WriteLine("--- Testing WithName ---");

            var builder = new CharacterBuilder();
            builder.WithName("TestCharacter");

            var character = builder.Build();
            TestBase.AssertNotNull(character,
                "Character should be built",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (character != null)
            {
                TestBase.AssertTrue(!string.IsNullOrEmpty(character.Name),
                    "Character should have a name",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestWithLevel()
        {
            Console.WriteLine("\n--- Testing WithLevel ---");

            var builder = new CharacterBuilder();
            builder.WithLevel(5);

            var character = builder.Build();
            TestBase.AssertNotNull(character,
                "Character should be built with level",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (character != null)
            {
                TestBase.AssertTrue(character.Level >= 1,
                    "Character should have valid level",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestWithInventory()
        {
            Console.WriteLine("\n--- Testing WithInventory ---");

            var inventory = new List<Item>
            {
                TestDataBuilders.Item().WithName("TestItem1").Build(),
                TestDataBuilders.Item().WithName("TestItem2").Build()
            };

            var builder = new CharacterBuilder();
            builder.WithInventory(inventory);

            var character = builder.Build();
            TestBase.AssertNotNull(character,
                "Character should be built with inventory",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestBuild()
        {
            Console.WriteLine("\n--- Testing Build ---");

            var builder = new CharacterBuilder()
                .WithName("TestChar")
                .WithLevel(3);

            var character = builder.Build();
            TestBase.AssertNotNull(character,
                "Character should be built using fluent interface",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCreateDefault()
        {
            Console.WriteLine("\n--- Testing CreateDefault ---");

            var character = CharacterBuilder.CreateDefault("DefaultChar", 1);
            TestBase.AssertNotNull(character,
                "Default character should be created",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var character2 = CharacterBuilder.CreateDefault();
            TestBase.AssertNotNull(character2,
                "Default character should be created without parameters",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCreateWithInventory()
        {
            Console.WriteLine("\n--- Testing CreateWithInventory ---");

            var inventory = new List<Item>
            {
                TestDataBuilders.Item().WithName("Item1").Build()
            };

            var character = CharacterBuilder.CreateWithInventory("InvChar", 2, inventory);
            TestBase.AssertNotNull(character,
                "Character with inventory should be created",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
