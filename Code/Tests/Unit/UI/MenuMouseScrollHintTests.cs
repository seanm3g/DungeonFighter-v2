using System;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Help;
using RPGGame.UI.Avalonia.Renderers.Inventory;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Soft-gray mouse (animal) + ↕ affordance for scrollable menus.
    /// </summary>
    public static class MenuMouseScrollHintTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("\n=== Menu Mouse Scroll Hint Tests ===");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestGlyphIsMouseAnimalWithUpDownArrow();
            TestColorIsSoftGray();
            TestInventoryScrollHintsStillDescribeDirection();

            Console.WriteLine($"\nMenuMouseScrollHint: {_testsPassed}/{_testsRun} passed, {_testsFailed} failed");
        }

        private static void TestGlyphIsMouseAnimalWithUpDownArrow()
        {
            Console.WriteLine("\n--- Glyph is mouse animal + ↕ ---");

            TestBase.AssertTrue(
                MenuMouseScrollHint.Glyph.Contains(AsciiArtAssets.UIElements.MouseAnimal, StringComparison.Ordinal),
                "scroll hint includes the animal mouse glyph",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(
                MenuMouseScrollHint.Glyph.Contains(AsciiArtAssets.UIElements.ArrowUpDown, StringComparison.Ordinal),
                "scroll hint includes the up/down arrow",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(
                AsciiArtAssets.UIElements.MouseScrollHint,
                MenuMouseScrollHint.Glyph,
                "hint glyph matches AsciiArtAssets.MouseScrollHint",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(
                MenuMouseScrollHint.GlyphColumns >= 2,
                "glyph column budget leaves room for emoji width",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestColorIsSoftGray()
        {
            Console.WriteLine("\n--- Color is soft gray ---");

            var expected = AsciiArtAssets.Colors.DarkGray;
            var actual = MenuMouseScrollHint.Color;
            TestBase.AssertTrue(
                actual.R == expected.R && actual.G == expected.G && actual.B == expected.B,
                "scroll affordance uses DarkGray to stay deemphasized",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestInventoryScrollHintsStillDescribeDirection()
        {
            Console.WriteLine("\n--- Inventory text hints keep directional copy ---");

            string top = InventoryItemScrollLayout.BuildTopScrollHint(hiddenItemCount: 2);
            TestBase.AssertTrue(
                top.Contains("▲", StringComparison.Ordinal) && top.Contains("scroll up", StringComparison.Ordinal),
                "top inventory hint still names scroll-up",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var range = new InventoryItemVisibleRange(firstIndex: 0, lastExclusiveIndex: 4, usedRows: 6, itemCount: 5);
            string bottom = InventoryItemScrollLayout.BuildBottomScrollHint(range, firstDisplay: 1, lastDisplay: 4);
            TestBase.AssertTrue(
                bottom.Contains("▼", StringComparison.Ordinal) && bottom.Contains("scroll down", StringComparison.Ordinal),
                "bottom inventory hint still names scroll-down",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
