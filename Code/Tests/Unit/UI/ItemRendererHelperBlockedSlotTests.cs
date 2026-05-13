using System;
using Avalonia.Media;
using RPGGame.Tests;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Renderers.Helpers;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Tests <see cref="ItemRendererHelper.IsEquipBlockedForCharacter"/> and inventory name-line coloring:
    /// when the item's <c>attributeRequirements</c> exceed the character's effective STR/AGI/TEC/INT,
    /// the full name row (index, rarity, slot, and item name) is drawn in red.
    /// </summary>
    public static class ItemRendererHelperBlockedSlotTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== ItemRendererHelper Blocked Slot Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            TestNullCharacter_NotBlocked(ref run, ref passed, ref failed);
            TestItemWithoutRequirements_NotBlocked(ref run, ref passed, ref failed);
            TestItemRequirementMet_NotBlocked(ref run, ref passed, ref failed);
            TestItemRequirementUnmet_Blocked(ref run, ref passed, ref failed);
            TestItemRequirementUnmet_NameLineAllSegmentsRed(ref run, ref passed, ref failed);
            TestNormalizedTechniqueKey_NotBlocked(ref run, ref passed, ref failed);

            TestBase.PrintSummary("ItemRendererHelper Blocked Slot Tests", run, passed, failed);
        }

        private static Character BuildHero(int strength, int agility, int technique, int intelligence) =>
            TestDataBuilders.Character()
                .WithName("Hero")
                .WithLevel(1)
                .WithStats(strength, agility, technique, intelligence)
                .Build();

        private static Item BuildHeadItem(string requirementKey, int requirementValue)
        {
            var head = new HeadItem("Test Helm", 1, 0);
            head.Rarity = "Common";
            if (!string.IsNullOrEmpty(requirementKey))
                head.AttributeRequirements.Add(requirementKey, requirementValue);
            return head;
        }

        private static void TestNullCharacter_NotBlocked(ref int run, ref int passed, ref int failed)
        {
            var item = BuildHeadItem("strength", 5);
            TestBase.AssertFalse(
                ItemRendererHelper.IsEquipBlockedForCharacter(item, null),
                "Null character should never report blocked (avoids crashes during preview rendering)",
                ref run, ref passed, ref failed);
        }

        private static void TestItemWithoutRequirements_NotBlocked(ref int run, ref int passed, ref int failed)
        {
            var hero = BuildHero(3, 3, 3, 3);
            var item = BuildHeadItem(string.Empty, 0);
            TestBase.AssertFalse(
                ItemRendererHelper.IsEquipBlockedForCharacter(item, hero),
                "Items without attribute requirements should not be flagged as blocked",
                ref run, ref passed, ref failed);
        }

        private static void TestItemRequirementMet_NotBlocked(ref int run, ref int passed, ref int failed)
        {
            var hero = BuildHero(10, 10, 10, 10);
            var item = BuildHeadItem("strength", 5);
            TestBase.AssertFalse(
                ItemRendererHelper.IsEquipBlockedForCharacter(item, hero),
                "Hero with STR 10 should not be blocked from STR 5 item",
                ref run, ref passed, ref failed);
        }

        private static void TestItemRequirementUnmet_Blocked(ref int run, ref int passed, ref int failed)
        {
            var hero = BuildHero(3, 3, 3, 3);
            var item = BuildHeadItem("strength", 5);
            TestBase.AssertTrue(
                ItemRendererHelper.IsEquipBlockedForCharacter(item, hero),
                "Level-1 starter (STR 3) should be flagged as blocked from STR 5 item",
                ref run, ref passed, ref failed);
        }

        private static void TestItemRequirementUnmet_NameLineAllSegmentsRed(ref int run, ref int passed, ref int failed)
        {
            var hero = BuildHero(3, 3, 3, 3);
            var item = BuildHeadItem("strength", 5);
            var line = ItemRendererHelper.BuildItemNameSegments(itemIndex: 0, item: item, character: hero);
            TestBase.AssertTrue(line.Count > 0, "blocked name line should have colored segments", ref run, ref passed, ref failed);
            var expectedRed = new ColoredText("x", Colors.Red).Color;
            foreach (var seg in line)
            {
                if (seg == null || string.IsNullOrEmpty(seg.Text))
                    continue;
                TestBase.AssertTrue(
                    ColorValidator.AreColorsEqual(seg.Color, expectedRed),
                    $"Blocked item segment '{seg.Text}' should use equip-block red",
                    ref run, ref passed, ref failed);
            }
        }

        /// <summary>
        /// Regression: items whose JSON requirement keys go through <see cref="Data.GameDataJsonNormalizer"/>
        /// arrive with the canonical key (e.g. typo'd <c>techinque</c> normalized to <c>technique</c>).
        /// This simulates the post-normalization state: with the canonical key in place, the helper
        /// uses the character's actual TEC and returns the correct verdict (not blocked when met).
        /// </summary>
        private static void TestNormalizedTechniqueKey_NotBlocked(ref int run, ref int passed, ref int failed)
        {
            var hero = BuildHero(3, 3, 5, 3);
            var item = BuildHeadItem("technique", 5);
            TestBase.AssertFalse(
                ItemRendererHelper.IsEquipBlockedForCharacter(item, hero),
                "Hero with TEC 5 meets TEC 5 requirement (post-normalization typo path)",
                ref run, ref passed, ref failed);
        }
    }
}
