using System;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Comprehensive tests for ItemDisplayFormatter
    /// Tests item formatting, comparison display, and stat display
    /// </summary>
    public static class ItemDisplayFormatterTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all ItemDisplayFormatter tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== ItemDisplayFormatter Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestFormatStatBonus();
            TestCleanStatBonusName();
            TestGetModificationDisplayText();
            TestGetDisplayType();
            TestGetWeaponClassDisplay();

            TestBase.PrintSummary("ItemDisplayFormatter Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Stat Bonus Tests

        private static void TestFormatStatBonus()
        {
            Console.WriteLine("--- Testing FormatStatBonus ---");

            var statBonus = new StatBonus
            {
                StatType = "Strength",
                Value = 5.0
            };

            var formatted = ItemDisplayFormatter.FormatStatBonus(statBonus);
            TestBase.AssertTrue(!string.IsNullOrEmpty(formatted),
                "FormatStatBonus should return formatted string",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(formatted.Contains("Strength"),
                "Formatted stat bonus should contain stat type",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test AttackSpeed formatting
            var attackSpeedBonus = new StatBonus
            {
                StatType = "AttackSpeed",
                Value = 0.05
            };

            var formattedSpeed = ItemDisplayFormatter.FormatStatBonus(attackSpeedBonus);
            TestBase.AssertTrue(formattedSpeed.Contains("AttackSpeed"),
                "AttackSpeed should be formatted correctly",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Name Cleaning Tests

        private static void TestCleanStatBonusName()
        {
            Console.WriteLine("\n--- Testing CleanStatBonusName ---");

            // Test with "of " prefix
            var cleaned = ItemDisplayFormatter.CleanStatBonusName("of Strength");
            TestBase.AssertEqual("Strength", cleaned,
                "Should remove 'of ' prefix",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test without prefix
            var notCleaned = ItemDisplayFormatter.CleanStatBonusName("Strength");
            TestBase.AssertEqual("Strength", notCleaned,
                "Should not modify name without prefix",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Modification Display Tests

        private static void TestGetModificationDisplayText()
        {
            Console.WriteLine("\n--- Testing GetModificationDisplayText ---");

            var modification = new Modification
            {
                Name = "Sharp",
                Effect = "damage",
                RolledValue = 5.0
            };

            var displayText = ItemDisplayFormatter.GetModificationDisplayText(modification);
            TestBase.AssertTrue(!string.IsNullOrEmpty(displayText),
                "GetModificationDisplayText should return display text",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(displayText.Contains("Sharp"),
                "Display text should contain modification name",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Display Type Tests

        private static void TestGetDisplayType()
        {
            Console.WriteLine("\n--- Testing GetDisplayType ---");

            // Test weapon
            var weapon = TestDataBuilders.Weapon()
                .WithName("Test Sword")
                .WithTier(1)
                .Build();

            var weaponType = ItemDisplayFormatter.GetDisplayType(weapon);
            TestBase.AssertTrue(!string.IsNullOrEmpty(weaponType),
                "GetDisplayType should return type for weapon",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test armor
            var armor = TestDataBuilders.Armor()
                .WithType(ItemType.Head)
                .WithName("Test Helmet")
                .WithTier(1)
                .Build();

            var armorType = ItemDisplayFormatter.GetDisplayType(armor);
            TestBase.AssertEqual("Head", armorType,
                "GetDisplayType should return 'Head' for head armor",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestGetWeaponClassDisplay()
        {
            Console.WriteLine("\n--- Testing GetWeaponClassDisplay ---");

            var sword = TestDataBuilders.Weapon()
                .WithName("Test Sword")
                .WithWeaponType(WeaponType.Sword)
                .WithTier(1)
                .Build();

            var display = ItemDisplayFormatter.GetWeaponClassDisplay(sword);
            TestBase.AssertTrue(!string.IsNullOrEmpty(display),
                "GetWeaponClassDisplay should return display string",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test with null
            var nullDisplay = ItemDisplayFormatter.GetWeaponClassDisplay(null);
            TestBase.AssertEqual("Weapon", nullDisplay,
                "GetWeaponClassDisplay should return 'Weapon' for null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
