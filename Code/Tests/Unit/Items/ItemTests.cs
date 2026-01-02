using System;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Items
{
    /// <summary>
    /// Comprehensive tests for Item base class
    /// Tests item creation, tier scaling, stat bonuses, modifications, and requirements
    /// </summary>
    public static class ItemTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all Item tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== Item Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestItemCreation();
            TestItemProperties();
            TestStatBonuses();
            TestModifications();
            TestMeetsRequirements();
            TestIsStarterItem();

            TestBase.PrintSummary("Item Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Creation Tests

        private static void TestItemCreation()
        {
            Console.WriteLine("--- Testing Item Creation ---");

            // Test creating item with all parameters
            var item = new Item(ItemType.Weapon, "Test Sword", 2, 5);
            TestBase.AssertEqualEnum(ItemType.Weapon, item.Type,
                "Item should have correct type",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("Test Sword", item.Name,
                "Item should have correct name",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(2, item.Tier,
                "Item should have correct tier",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(5, item.ComboBonus,
                "Item should have correct combo bonus",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test creating item with default name
            var item2 = new Item(ItemType.Head);
            TestBase.AssertTrue(!string.IsNullOrEmpty(item2.Name),
                "Item should have default name when not specified",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test creating armor item
            var armor = new Item(ItemType.Chest, "Test Armor", 1);
            TestBase.AssertEqualEnum(ItemType.Chest, armor.Type,
                "Armor item should have correct type",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Properties Tests

        private static void TestItemProperties()
        {
            Console.WriteLine("\n--- Testing Item Properties ---");

            var item = new Item(ItemType.Weapon, "Test Item", 1);

            // Test default values
            TestBase.AssertEqual(1, item.Level,
                "Item should have default level 1",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("Common", item.Rarity,
                "Item should have default rarity Common",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0, item.BonusDamage,
                "Item should have default bonus damage 0",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test setting properties
            item.Level = 5;
            item.Rarity = "Rare";
            item.BonusDamage = 10;

            TestBase.AssertEqual(5, item.Level,
                "Item level should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("Rare", item.Rarity,
                "Item rarity should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(10, item.BonusDamage,
                "Item bonus damage should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test collections are initialized
            TestBase.AssertNotNull(item.StatBonuses,
                "StatBonuses should be initialized",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertNotNull(item.ActionBonuses,
                "ActionBonuses should be initialized",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertNotNull(item.Modifications,
                "Modifications should be initialized",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Stat Bonuses Tests

        private static void TestStatBonuses()
        {
            Console.WriteLine("\n--- Testing Stat Bonuses ---");

            var item = new Item(ItemType.Weapon, "Test Item", 1);

            var statBonus = new StatBonus
            {
                StatType = "Strength",
                Value = 5.0,
                Name = "Strength Bonus"
            };

            item.StatBonuses.Add(statBonus);

            TestBase.AssertEqual(1, item.StatBonuses.Count,
                "Item should have stat bonus added",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqual("Strength", item.StatBonuses[0].StatType,
                "Stat bonus should have correct stat type",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Modifications Tests

        private static void TestModifications()
        {
            Console.WriteLine("\n--- Testing Modifications ---");

            var item = new Item(ItemType.Weapon, "Test Item", 1);

            var modification = new Modification
            {
                Name = "Sharp",
                DiceResult = 10,
                RolledValue = 5.5
            };

            item.Modifications.Add(modification);

            TestBase.AssertEqual(1, item.Modifications.Count,
                "Item should have modification added",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqual("Sharp", item.Modifications[0].Name,
                "Modification should have correct name",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Requirements Tests

        private static void TestMeetsRequirements()
        {
            Console.WriteLine("\n--- Testing MeetsRequirements ---");

            var character = TestDataBuilders.Character()
                .WithName("TestPlayer")
                .WithLevel(1)
                .Build();

            // Set stats explicitly after building
            character.Stats.Strength = 10;
            character.Stats.Agility = 10;
            character.Stats.Technique = 10;
            character.Stats.Intelligence = 10;

            var item = new Item(ItemType.Weapon, "Test Item", 1);
            
            // Item with no requirements should always pass
            TestBase.AssertTrue(item.MeetsRequirements(character),
                "Item with no requirements should meet requirements",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test with null character
            TestBase.AssertTrue(item.MeetsRequirements(null!),
                "Item should meet requirements with null character",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test with requirements
            item.AttributeRequirements.Add("Strength", 5);
            TestBase.AssertTrue(item.MeetsRequirements(character),
                "Item should meet requirements when character has enough stats",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test with requirements that aren't met
            item.AttributeRequirements["Strength"] = 20;
            TestBase.AssertFalse(item.MeetsRequirements(character),
                "Item should not meet requirements when character lacks stats",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Starter Item Tests

        private static void TestIsStarterItem()
        {
            Console.WriteLine("\n--- Testing IsStarterItem ---");

            // Test non-starter item
            var item = new Item(ItemType.Weapon, "Regular Sword", 1);
            TestBase.AssertFalse(item.IsStarterItem,
                "Regular item should not be starter item",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test starter item
            var starterItem = new Item(ItemType.Weapon, "Starter Sword", 1);
            TestBase.AssertTrue(starterItem.IsStarterItem,
                "Item with 'Starter' in name should be starter item",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
