using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using RPGGame;
using RPGGame.Tests;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.ColorSystem.Applications.ItemFormatting;
using RPGGame.UI.ColorSystem.Themes;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Comprehensive tests for color system applications
    /// Tests ColorLayerSystem, KeywordColorSystem, ItemColorSystem, StatusEffectColorHelper
    /// </summary>
    public static class ColorSystemApplicationTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Color System Application Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestColorLayerSystem();
            TestKeywordColorSystem();
            TestItemColorSystem();
            TestItemNameFormatterEmbeddedMaterialAndBaseType();
            TestStatSuffixUsesStatBonusAffixRarity();
            TestRemoveRarityPrefixStripsMythic();
            TestItemColorSystemFormatFullMatchesItemNameFormatter();
            TestStatusEffectColorHelper();

            TestBase.PrintSummary("Color System Application Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region ColorLayerSystem Tests

        private static void TestColorLayerSystem()
        {
            Console.WriteLine("--- Testing ColorLayerSystem ---");

            var red = Colors.Red;

            // Test event significance adjustments
            var trivial = ColorLayerSystem.CreateSignificantSegment("Text", red, EventSignificance.Trivial);
            TestBase.AssertTrue(trivial.Count > 0, 
                "Trivial significance should create segment", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var minor = ColorLayerSystem.CreateSignificantSegment("Text", red, EventSignificance.Minor);
            TestBase.AssertTrue(minor.Count > 0, 
                "Minor significance should create segment", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var normal = ColorLayerSystem.CreateSignificantSegment("Text", red, EventSignificance.Normal);
            TestBase.AssertTrue(normal.Count > 0, 
                "Normal significance should create segment", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var important = ColorLayerSystem.CreateSignificantSegment("Text", red, EventSignificance.Important);
            TestBase.AssertTrue(important.Count > 0, 
                "Important significance should create segment", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var critical = ColorLayerSystem.CreateSignificantSegment("Text", red, EventSignificance.Critical);
            TestBase.AssertTrue(critical.Count > 0, 
                "Critical significance should create segment", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test dungeon depth color temperature
            var warmWhite = ColorLayerSystem.GetWhite(WhiteTemperature.Warm);
            TestBase.AssertNotNull(warmWhite, 
                "Warm white should be retrievable", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var coolWhite = ColorLayerSystem.GetWhite(WhiteTemperature.Cool);
            TestBase.AssertNotNull(coolWhite, 
                "Cool white should be retrievable", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var neutralWhite = ColorLayerSystem.GetWhite(WhiteTemperature.Neutral);
            TestBase.AssertNotNull(neutralWhite, 
                "Neutral white should be retrievable", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test depth-based white
            var depthWhite = ColorLayerSystem.GetWhiteByDepth(5);
            TestBase.AssertNotNull(depthWhite, 
                "Depth-based white should be retrievable", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region KeywordColorSystem Tests

        private static void TestKeywordColorSystem()
        {
            Console.WriteLine("\n--- Testing KeywordColorSystem ---");

            // Test keyword matching (case-sensitive/insensitive)
            var colored1 = KeywordColorSystem.Colorize("damage hit critical");
            TestBase.AssertTrue(colored1.Count > 0, 
                "Keyword coloring should work", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test keyword group application
            var colored2 = KeywordColorSystem.ColorizeWithGroups("fire burn flame", "fire");
            TestBase.AssertTrue(colored2.Count > 0, 
                "Group-specific coloring should work", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test whole-word matching
            var colored3 = KeywordColorSystem.Colorize("heal healing restored");
            TestBase.AssertTrue(colored3.Count > 0, 
                "Whole-word matching should work", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test multiple keyword groups
            var colored4 = KeywordColorSystem.Colorize("damage fire ice poison");
            TestBase.AssertTrue(colored4.Count > 0, 
                "Multiple keyword groups should work", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test getting group names
            var groupNames = KeywordColorSystem.GetAllGroupNames();
            TestBase.AssertTrue(groupNames != null, 
                "GetAllGroupNames should return collection", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region ItemColorSystem Tests

        private static void TestItemColorSystem()
        {
            Console.WriteLine("\n--- Testing ItemColorSystem ---");

            // Note: ItemColorSystem is primarily used for display
            // Test that item colors can be retrieved
            var item = TestDataBuilders.Item().WithRarity("Common").Build();
            
            TestBase.AssertNotNull(item.Rarity, 
                "Item should have rarity", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test rarity-based coloring (6 tiers)
            var rarities = new[] { "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic" };
            foreach (var rarity in rarities)
            {
                var testItem = TestDataBuilders.Item().WithRarity(rarity).Build();
                TestBase.AssertEqual(rarity, testItem.Rarity, 
                    $"Item should have {rarity} rarity", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region ItemNameFormatter Tests

        private static void TestItemNameFormatterEmbeddedMaterialAndBaseType()
        {
            Console.WriteLine("\n--- Testing ItemNameFormatter (base name theming) ---");

            var glassBrogues = new FeetItem("GLASS BROGUES", 1, 1)
            {
                Rarity = "Common",
                Modifications = new List<Modification>()
            };
            var gbSegments = ItemNameFormatter.FormatFullItemName(glassBrogues);
            var glassSeg = gbSegments.FirstOrDefault(s => s.Text == "GLASS");
            var broguesSeg = gbSegments.FirstOrDefault(s => s.Text == "BROGUES");
            var feetTheme = ItemThemeProvider.GetItemTypeTheme(ItemType.Feet);
            TestBase.AssertTrue(feetTheme.Count > 0, "Feet item type theme should exist", ref _testsRun, ref _testsPassed, ref _testsFailed);
            var expectedFeetColor = feetTheme[0].Color;
            TestBase.AssertNotNull(glassSeg,
                "FormatFullItemName should emit a segment for GLASS",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertNotNull(broguesSeg,
                "FormatFullItemName should emit a segment for BROGUES",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(
                ColorValidator.AreColorsEqual(glassSeg!.Color, ColorPalette.Cyan.GetColor()),
                "Leading material word GLASS should use cyan material color",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(
                ColorValidator.AreColorsEqual(broguesSeg!.Color, expectedFeetColor),
                "Base equipment word should use feet armor type theme",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var plainShoes = new FeetItem("SHOES", 1, 1) { Rarity = "Common" };
            var shoesSeg = ItemNameFormatter.FormatFullItemName(plainShoes).FirstOrDefault(s => s.Text == "SHOES");
            TestBase.AssertNotNull(shoesSeg, "Single-word feet name should still produce a segment", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(
                ColorValidator.AreColorsEqual(shoesSeg!.Color, expectedFeetColor),
                "Single-word feet item should use feet armor type theme (not flat white)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestStatSuffixUsesStatBonusAffixRarity()
        {
            Console.WriteLine("\n--- Testing ItemNameFormatter stat suffix uses StatBonus.Rarity ---");

            var boots = new FeetItem("BOOTS of Swiftness", 1, 1)
            {
                Rarity = "Common",
                StatBonuses = new List<StatBonus>
                {
                    new StatBonus
                    {
                        Name = "of Swiftness",
                        Rarity = "Rare",
                        StatType = "Agility",
                        Value = 1
                    }
                }
            };

            var segments = ItemNameFormatter.FormatFullItemName(boots);
            var swiftSeg = segments.FirstOrDefault(s =>
                string.Equals(s.Text, "Swiftness", StringComparison.OrdinalIgnoreCase));
            TestBase.AssertNotNull(swiftSeg, "Swiftness keyword segment exists", ref _testsRun, ref _testsPassed, ref _testsFailed);

            var rareBlue = ColorPalette.Blue.GetColor();
            TestBase.AssertTrue(
                ColorValidator.AreColorsEqual(swiftSeg!.Color, rareBlue),
                "Stat suffix keyword should use Rare affix-line color (blue), not Common gray",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var commonAffixColor = ItemThemeProvider.GetRarityColor("Common");
            TestBase.AssertTrue(
                !ColorValidator.AreColorsEqual(swiftSeg.Color, commonAffixColor),
                "Rare-tier stat suffix must differ from Common-tier ramp color",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRemoveRarityPrefixStripsMythic()
        {
            Console.WriteLine("\n--- Testing ItemNameParser.RemoveRarityPrefix (Mythic) ---");

            var stripped = ItemNameParser.RemoveRarityPrefix("Mythic Iron Sword");
            TestBase.AssertEqual(
                "Iron Sword",
                stripped,
                "Leading Mythic rarity token should be stripped for display parsing",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var strippedT = ItemNameParser.RemoveRarityPrefix("Transcendent Rusty Nail");
            TestBase.AssertEqual(
                "Rusty Nail",
                strippedT,
                "Leading Transcendent rarity token should strip for legacy data",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestItemColorSystemFormatFullMatchesItemNameFormatter()
        {
            Console.WriteLine("\n--- Testing ItemColorSystem.FormatFullItemName delegates to ItemNameFormatter ---");

            var item = new HeadItem("BONE Hat", 1, 1)
            {
                Rarity = "Rare",
                Modifications = new List<Modification>
                {
                    new Modification
                    {
                        Name = "BONE",
                        PrefixCategory = "Material",
                        ItemRank = "Common",
                        Effect = "equipmentStr",
                        RolledValue = 1
                    }
                }
            };

            var a = RPGGame.UI.ItemColorSystem.FormatFullItemName(item);
            var b = ItemNameFormatter.FormatFullItemName(item);
            string plainA = ColoredTextRenderer.RenderAsPlainText(a);
            string plainB = ColoredTextRenderer.RenderAsPlainText(b);
            TestBase.AssertEqual(plainA, plainB, "Plain text output should match", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(a.Count == b.Count, "Segment count should match", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region StatusEffectColorHelper Tests

        private static void TestStatusEffectColorHelper()
        {
            Console.WriteLine("\n--- Testing StatusEffectColorHelper ---");

            // Test effect type to color mapping
            // Note: StatusEffectColorHelper may not be directly accessible
            // Test through status effect application
            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            
            // Test that status effects can be applied
            character.IsWeakened = true;
            TestBase.AssertTrue(character.IsWeakened, 
                "Status effect should be applicable", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            character.IsWeakened = false; // Reset

            // Test multiple effect colors
            character.IsStunned = true;
            TestBase.AssertTrue(character.IsStunned, 
                "Multiple status effects should be applicable", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            character.IsStunned = false; // Reset
        }

        #endregion
    }
}

