using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Renderers.Inventory;
using RPGGame.UI.Avalonia.Renderers.Text;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Equip comparison column wrapping (mods/stats must stay inside column width).
    /// </summary>
    public static class ItemComparisonRendererTests
    {
        private static int _run;
        private static int _passed;
        private static int _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== ItemComparisonRenderer Tests ===\n");
            _run = 0;
            _passed = 0;
            _failed = 0;

            TestModsLineWrapsWithinComparisonColumnWidth();

            TestBase.PrintSummary("ItemComparisonRenderer Tests", _run, _passed, _failed);
        }

        private static void TestModsLineWrapsWithinComparisonColumnWidth()
        {
            Console.WriteLine("--- Mods line wraps within comparison column width ---");

            // Mirrors the overflowing NEW ITEM mods line from equip comparison:
            // "Mods: Acrobatic — +1 extra hit(s) on attacks, Bone — STRENGTH (2)"
            var modifications = new List<Modification>
            {
                new Modification
                {
                    Name = "Acrobatic",
                    Effect = "MULTI-HIT",
                    RolledValue = 1
                },
                new Modification
                {
                    Name = "Bone",
                    Effect = "STRENGTH",
                    RolledValue = 2
                }
            };

            var segments = ItemComparisonRenderer.BuildModsLineSegments(
                modifications,
                Colors.Cyan,
                Colors.White);

            string plain = string.Concat(segments.Select(s => s.Text));
            TestBase.AssertTrue(
                plain.Contains("Mods:", StringComparison.Ordinal)
                    && plain.Contains("Acrobatic", StringComparison.Ordinal)
                    && plain.Contains("Bone", StringComparison.Ordinal),
                "Mods line should include label and both modification names",
                ref _run, ref _passed, ref _failed);

            // Typical half-column budget on the equip comparison screen ((centerWidth - 6) / 2).
            const int columnWidth = 40;
            TestBase.AssertTrue(
                plain.Length > columnWidth,
                "Fixture mods line should be longer than the comparison column so wrap is required",
                ref _run, ref _passed, ref _failed);

            var wrapped = TextWrappingHelper.WrapColoredSegments(segments, columnWidth);
            TestBase.AssertTrue(
                wrapped.Count >= 2,
                "Long mods line should wrap onto multiple rows",
                ref _run, ref _passed, ref _failed);

            for (int i = 0; i < wrapped.Count; i++)
            {
                int lineLen = ColoredTextRenderer.GetDisplayLength(wrapped[i]);
                TestBase.AssertTrue(
                    lineLen <= columnWidth,
                    $"Wrapped mods row {i} length {lineLen} must be <= column width {columnWidth}",
                    ref _run, ref _passed, ref _failed);
            }
        }
    }
}
