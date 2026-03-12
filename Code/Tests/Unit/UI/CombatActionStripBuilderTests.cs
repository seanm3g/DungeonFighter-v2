using RPGGame;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Tests for CombatActionStripBuilder (abilities list with dynamic info for the action strip).
    /// </summary>
    public static class CombatActionStripBuilderTests
    {
        /// <summary>
        /// Runs all CombatActionStripBuilder tests.
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== CombatActionStripBuilder Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            var nullLines = CombatActionStripBuilder.BuildLines(null!, 80, 8);
            TestBase.AssertTrue(nullLines != null && nullLines.Count == 0,
                "BuildLines(null) returns empty list",
                ref run, ref passed, ref failed);

            var characterNoActions = new Character("Test", 1);
            var noActionLines = CombatActionStripBuilder.BuildLines(characterNoActions, 80, 8);
            TestBase.AssertTrue(noActionLines != null && noActionLines.Count >= 1 && noActionLines[0] == "(No abilities)",
                "BuildLines(character with no combo actions) returns (No abilities)",
                ref run, ref passed, ref failed);

            var limitedLines = CombatActionStripBuilder.BuildLines(characterNoActions, 40, 3);
            TestBase.AssertTrue(limitedLines != null && limitedLines.Count <= 3,
                "BuildLines respects maxLines",
                ref run, ref passed, ref failed);

            var nullPanelData = CombatActionStripBuilder.BuildPanelData(null);
            TestBase.AssertTrue(nullPanelData != null && nullPanelData.Count == 0,
                "BuildPanelData(null) returns empty list",
                ref run, ref passed, ref failed);

            var noActionPanelData = CombatActionStripBuilder.BuildPanelData(characterNoActions);
            TestBase.AssertTrue(noActionPanelData != null && noActionPanelData.Count == 0,
                "BuildPanelData(character with no combo actions) returns empty",
                ref run, ref passed, ref failed);

            int selectedIndex = characterNoActions.ComboStep % 1;
            TestBase.AssertTrue(selectedIndex == 0,
                "Selected index ComboStep % count is 0 when count is 0 (no div by zero)",
                ref run, ref passed, ref failed);

            TestBase.PrintSummary("CombatActionStripBuilder Tests", run, passed, failed);
        }
    }
}
