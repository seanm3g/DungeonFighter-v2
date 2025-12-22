using System;
using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.UI.ColorSystem.Applications;

namespace RPGGame.Game.TestRunners.Helpers
{
    /// <summary>
    /// Helper class for text system tests
    /// </summary>
    public static class TextSystemTestHelpers
    {
        public static (int passed, int failed) TestWordSpacing(CanvasUICoordinator uiCoordinator)
        {
            var testCases = new[] { ("normal text", true), ("text  with  double  spaces", false), ("word1 word2", true) };
            int passed = 0, failed = 0;
            foreach (var (text, shouldPass) in testCases)
            {
                var result = TextSpacingValidator.ValidateWordSpacing(text);
                if (result.IsValid == shouldPass) passed++;
                else { failed++; uiCoordinator.WriteLine($"  ✗ '{text}' - Expected {(shouldPass ? "valid" : "invalid")}, got {(result.IsValid ? "valid" : "invalid")}"); }
            }
            return (passed, failed);
        }

        public static (int passed, int failed) TestBlankLineSpacing(CanvasUICoordinator uiCoordinator)
        {
            int spacing = TextSpacingSystem.GetSpacingBefore(TextSpacingSystem.BlockType.RoomHeader);
            uiCoordinator.WriteLine($"  RoomHeader spacing: {spacing} blank line(s)");
            var ruleIssues = TextSpacingSystem.ValidateSpacingRules();
            if (ruleIssues.Count == 0) { uiCoordinator.WriteLine($"  ✓ All spacing rules defined"); return (1, 0); }
            uiCoordinator.WriteLine($"  ✗ Missing rules: {ruleIssues.Count}");
            foreach (var issue in ruleIssues) uiCoordinator.WriteLine($"    - {issue}");
            return (0, 1);
        }

        public static (int passed, int failed) TestColorApplicationValidation(string testText, CanvasUICoordinator uiCoordinator)
        {
            var result = ColorApplicationValidator.ValidateNoDoubleColoring(testText);
            if (result.IsValid) { uiCoordinator.WriteLine($"  ✓ No double-coloring detected"); return (1, 0); }
            uiCoordinator.WriteLine($"  ✗ Double-coloring issues: {result.DoubleColoringCount}");
            foreach (var issue in result.Issues) uiCoordinator.WriteLine($"    - {issue}");
            return (0, 1);
        }
    }
}

