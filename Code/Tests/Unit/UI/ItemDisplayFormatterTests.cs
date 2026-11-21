using System;
using System.Linq;
using RPGGame.Data;
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

            TestModLinesUseWordCase();
        }

        /// <summary>Playtest 2026-11-21: "STRENGTH (2)", "CROSSWIRE: 1", "On CRISIS spent: … DRAG" read as words.</summary>
        private static void TestModLinesUseWordCase()
        {
            MaterialBuildsLoader.Reload();
            Stores.Reload();
            var bone = new Modification { Name = "Bone", PrefixCategory = "MATERIAL", Effect = "STRENGTH", RolledValue = 2 };
            string boneText = ItemDisplayFormatter.GetModificationDisplayText(bone);
            TestBase.AssertEqual("Bone — +2 Strength", boneText, "material line: '+2 Strength', not 'STRENGTH (2)'",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var dour = new Modification
            {
                Name = "Dour", PrefixCategory = "ADJECTIVE", Effect = CrossWires.AffixEffect,
                CrossWire = "ONCASH:CRISIS -> MINT DRAG 50%", MinValue = 1, MaxValue = 1, RolledValue = 1
            };
            string dourText = ItemDisplayFormatter.GetModificationDisplayText(dour);
            TestBase.AssertTrue(dourText.Contains("Crisis") && dourText.Contains("Drag") && !dourText.Contains("CRISIS") && !dourText.Contains("DRAG"),
                $"cross-wire stores in word case ({dourText})", ref _testsRun, ref _testsPassed, ref _testsFailed);

            var item = new WeaponItem("Club", 1, 3, 1.3, WeaponType.Mace) { Rarity = "Uncommon", Material = "Bone" };
            item.Modifications.Add(bone);
            item.Modifications.Add(dour);
            var lines = RPGGame.UI.Avalonia.Renderers.Helpers.ItemStatFormatter.GetStatContributions(item)
                .Select(c => c.ToPlainLine()).ToList();
            string? dourLine = lines.FirstOrDefault(l => l.StartsWith("Dour", StringComparison.Ordinal));
            TestBase.AssertTrue(dourLine != null && !dourLine.Contains("CROSSWIRE") && dourLine.Contains("Crisis"),
                $"detail line shows the rule, not 'CROSSWIRE: 1' ({dourLine})", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(lines.Contains("Bone — Strength: +2"), $"material stat line ({string.Join(" | ", lines)})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqual("BARBARIAN RAGE grants +2 Rage", RPGGame.UI.RuleText.WordCase("BARBARIAN RAGE grants +2 RAGE"),
                "action names stay upper case, a lone store name is a word", ref _testsRun, ref _testsPassed, ref _testsFailed);
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
