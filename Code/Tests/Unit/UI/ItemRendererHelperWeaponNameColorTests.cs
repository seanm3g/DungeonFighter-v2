using System.Collections.Generic;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Renderers.Helpers;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.ColorSystem.Applications;

namespace RPGGame.Tests.Unit.UI
{
    public static class ItemRendererHelperWeaponNameColorTests
    {
        public static void RunAllTests()
        {
            int run = 0, passed = 0, failed = 0;

            TestWeaponInventoryLine_AppendsSameSegmentsAsFormatFullItemName(ref run, ref passed, ref failed);

            TestBase.PrintSummary(nameof(ItemRendererHelperWeaponNameColorTests), run, passed, failed);
        }

        /// <summary>
        /// Inventory rows must use <see cref="ItemDisplayColoredText.FormatFullItemName"/> for the name
        /// (prefix/base/suffix colors), not a single rarity tint for the whole weapon name.
        /// </summary>
        private static void TestWeaponInventoryLine_AppendsSameSegmentsAsFormatFullItemName(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(TestWeaponInventoryLine_AppendsSameSegmentsAsFormatFullItemName));

            var weapon = new WeaponItem("Stick", tier: 1, baseDamage: 1, baseAttackSpeed: 1.0, weaponType: WeaponType.Wand)
            {
                Rarity = "Common"
            };

            var line = ItemRendererHelper.BuildItemNameSegments(itemIndex: 0, item: weapon, character: null);
            var expectedName = ItemDisplayColoredText.FormatFullItemName(weapon);
            AssertTailMatches(line, expectedName, ref run, ref passed, ref failed);

            TestBase.ClearCurrentTestName();
        }

        private static void AssertTailMatches(List<ColoredText> fullLine, List<ColoredText> expectedNameTail,
            ref int run, ref int passed, ref int failed)
        {
            TestBase.AssertTrue(expectedNameTail.Count > 0, "expected name has segments", ref run, ref passed, ref failed);
            TestBase.AssertTrue(fullLine.Count >= expectedNameTail.Count, "line includes full name", ref run, ref passed, ref failed);
            if (fullLine.Count < expectedNameTail.Count || expectedNameTail.Count == 0)
                return;

            int start = fullLine.Count - expectedNameTail.Count;
            for (int i = 0; i < expectedNameTail.Count; i++)
            {
                var a = fullLine[start + i];
                var e = expectedNameTail[i];
                TestBase.AssertEqual(e.Text, a.Text, $"name segment text @{i}", ref run, ref passed, ref failed);
                TestBase.AssertEqual(e.Color, a.Color, $"name segment color @{i}", ref run, ref passed, ref failed);
            }
        }
    }
}

