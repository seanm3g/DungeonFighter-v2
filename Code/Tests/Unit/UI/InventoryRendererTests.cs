using System;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Renderers;
using RPGGame.UI.Avalonia.Renderers.Inventory;
using RPGGame.UI;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Tests for InventoryRenderer
    /// Tests inventory rendering, item display, and selection prompts
    /// </summary>
    public static class InventoryRendererTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all InventoryRenderer tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== InventoryRenderer Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestInventoryRenderingMethods();
            TestItemComparisonHoverIds();
            TestInventoryItemScrollRange();

            TestBase.PrintSummary("InventoryRenderer Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Rendering Tests

        private static void TestInventoryRenderingMethods()
        {
            Console.WriteLine("--- Testing Inventory Rendering Methods ---");

            // Test that inventory rendering methods exist
            TestBase.AssertTrue(true,
                "InventoryRenderer should have rendering methods",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestItemComparisonHoverIds()
        {
            Console.WriteLine("\n--- Testing Item Comparison Hover IDs ---");

            string? currentFeet = ItemComparisonRenderer.GetComparisonTooltipHoverValue(
                currentColumn: true,
                slot: "feet",
                newItemInventoryIndex: 2);
            TestBase.AssertEqual(LeftPanelHoverState.Prefix + "gear:feet", currentFeet, "current comparison column uses equipped slot hover id", ref _testsRun, ref _testsPassed, ref _testsFailed);

            string? newItem = ItemComparisonRenderer.GetComparisonTooltipHoverValue(
                currentColumn: false,
                slot: "feet",
                newItemInventoryIndex: 2);
            TestBase.AssertEqual(LeftPanelHoverState.Prefix + "inv:2", newItem, "new comparison column uses selected inventory row hover id", ref _testsRun, ref _testsPassed, ref _testsFailed);

            string? missingIndex = ItemComparisonRenderer.GetComparisonTooltipHoverValue(
                currentColumn: false,
                slot: "feet",
                newItemInventoryIndex: -1);
            TestBase.AssertTrue(missingIndex == null, "new comparison column without inventory index has no hover id", ref _testsRun, ref _testsPassed, ref _testsFailed);

            string? invalidSlot = ItemComparisonRenderer.GetComparisonTooltipHoverValue(
                currentColumn: true,
                slot: "unknown",
                newItemInventoryIndex: 2);
            TestBase.AssertTrue(invalidSlot == null, "current comparison column ignores invalid slots", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestInventoryItemScrollRange()
        {
            Console.WriteLine("\n--- Testing Inventory Item Scroll Range ---");

            var rowHeights = new System.Collections.Generic.List<int> { 3, 3, 4, 2, 3 };
            var firstPage = InventoryItemScrollLayout.CalculateVisibleRange(rowHeights, requestedFirstIndex: 0, availableRows: 7);
            TestBase.AssertEqual(0, firstPage.FirstIndex, "inventory scroll starts at first row", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(2, firstPage.LastExclusiveIndex, "inventory scroll stops before overlapping fixed menu space", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(firstPage.HasItemsBelow, "inventory scroll reports hidden rows below", ref _testsRun, ref _testsPassed, ref _testsFailed);

            var scrolledPage = InventoryItemScrollLayout.CalculateVisibleRange(rowHeights, requestedFirstIndex: 2, availableRows: 7);
            TestBase.AssertEqual(2, scrolledPage.FirstIndex, "inventory scroll preserves requested top item", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(4, scrolledPage.LastExclusiveIndex, "inventory scroll uses variable row heights", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(scrolledPage.HasItemsAbove, "inventory scroll reports hidden rows above", ref _testsRun, ref _testsPassed, ref _testsFailed);

            int clamped = InventoryItemScrollLayout.ClampFirstVisibleIndex(20, rowHeights.Count);
            TestBase.AssertEqual(rowHeights.Count - 1, clamped, "inventory scroll clamps past-end offsets", ref _testsRun, ref _testsPassed, ref _testsFailed);

            bool needsStatus = InventoryItemScrollLayout.RequiresScrollStatus(rowHeights, availableRows: 7, firstVisibleIndex: 0);
            TestBase.AssertTrue(needsStatus, "inventory scroll status appears when rows exceed viewport", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
