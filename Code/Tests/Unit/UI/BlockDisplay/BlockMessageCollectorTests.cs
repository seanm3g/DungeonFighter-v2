using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.BlockDisplay;
using RPGGame.Tests;
using Avalonia.Media;

namespace RPGGame.Tests.Unit.UI.BlockDisplay
{
    /// <summary>
    /// Comprehensive tests for BlockMessageCollector
    /// Tests message grouping and collection
    /// </summary>
    public static class BlockMessageCollectorTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all BlockMessageCollector tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== BlockMessageCollector Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestCollectActionBlockMessages_ActionTextOnly();
            TestCollectActionBlockMessages_WithRollInfo();
            TestCollectActionBlockMessages_WithStatusEffects();
            TestCollectActionBlockMessages_EmptyInput();
            TestCollectActionBlockMessages_EnvironmentalBlockUsesEnvironmentalLineTypes();
            TestCollectActionBlockMessages_EnvironmentalStatusEffectsSkipIndent();

            TestBase.PrintSummary("BlockMessageCollector Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Collection Tests

        private static void TestCollectActionBlockMessages_ActionTextOnly()
        {
            Console.WriteLine("--- Testing CollectActionBlockMessages - Action Text Only ---");

            var actionText = new List<ColoredText> { new ColoredText("Hero hits Enemy", Colors.White) };
            
            var result = BlockMessageCollector.CollectActionBlockMessages(actionText, null, null, null, null);
            
            TestBase.AssertNotNull(result,
                "CollectActionBlockMessages should return a list",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            
            TestBase.AssertTrue(result.Count > 0,
                "Result should contain at least one message group",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCollectActionBlockMessages_WithRollInfo()
        {
            Console.WriteLine("\n--- Testing CollectActionBlockMessages - With Roll Info ---");

            var actionText = new List<ColoredText> { new ColoredText("Hero hits Enemy", Colors.White) };
            var rollInfo = new List<ColoredText> { new ColoredText("(roll: 15)", Colors.Gray) };
            
            var result = BlockMessageCollector.CollectActionBlockMessages(actionText, rollInfo, null, null, null);
            
            TestBase.AssertNotNull(result,
                "CollectActionBlockMessages should return a list with roll info",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            
            TestBase.AssertTrue(result.Count > 1,
                "Result should contain multiple message groups (action + roll info)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCollectActionBlockMessages_WithStatusEffects()
        {
            Console.WriteLine("\n--- Testing CollectActionBlockMessages - With Status Effects ---");

            var actionText = new List<ColoredText> { new ColoredText("Hero hits Enemy", Colors.White) };
            var statusEffects = new List<List<ColoredText>>
            {
                new List<ColoredText> { new ColoredText("Enemy is bleeding", Colors.Red) }
            };
            
            var result = BlockMessageCollector.CollectActionBlockMessages(actionText, null, statusEffects, null, null);
            
            TestBase.AssertNotNull(result,
                "CollectActionBlockMessages should return a list with status effects",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCollectActionBlockMessages_EmptyInput()
        {
            Console.WriteLine("\n--- Testing CollectActionBlockMessages - Empty Input ---");

            var result = BlockMessageCollector.CollectActionBlockMessages(null, null, null, null, null);
            
            TestBase.AssertNotNull(result,
                "CollectActionBlockMessages should handle empty input",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCollectActionBlockMessages_EnvironmentalBlockUsesEnvironmentalLineTypes()
        {
            Console.WriteLine("\n--- Testing CollectActionBlockMessages - Environmental block line types ---");

            var actionText = new List<ColoredText> { new ColoredText("The room releases spores!", Colors.White) };
            var rollInfo = new List<ColoredText> { new ColoredText("(roll: 12)", Colors.Gray) };

            var result = BlockMessageCollector.CollectActionBlockMessages(
                actionText,
                rollInfo,
                null,
                null,
                null,
                TextSpacingSystem.BlockType.EnvironmentalAction);

            TestBase.AssertTrue(
                result.Count >= 2
                && result[0].messageType == UIMessageType.Environmental
                && result[1].messageType == UIMessageType.Environmental,
                "Environmental action block should tag action and roll lines as UIMessageType.Environmental",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCollectActionBlockMessages_EnvironmentalStatusEffectsSkipIndent()
        {
            Console.WriteLine("\n--- Testing CollectActionBlockMessages - Environmental status effects skip indent ---");

            var actionText = new List<ColoredText> { new ColoredText("Boss Chamber uses Room Collapse!", Colors.Green) };
            var effectBuilder = new ColoredTextBuilder();
            effectBuilder.Add("Zombie", ColorPalette.DarkGreen);
            effectBuilder.AddSpace();
            effectBuilder.Add("affected", Colors.White);
            effectBuilder.AddSpace();
            effectBuilder.Add("by STUN for 1 turn", Colors.White);
            var statusEffects = new List<List<ColoredText>> { effectBuilder.Build() };

            var result = BlockMessageCollector.CollectActionBlockMessages(
                actionText,
                null,
                statusEffects,
                null,
                null,
                TextSpacingSystem.BlockType.EnvironmentalAction);

            TestBase.AssertTrue(
                result.Count >= 2,
                "Environmental block should include action and effect lines",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            string effectPlain = ColoredTextRenderer.RenderAsPlainText(result[1].segments);
            TestBase.AssertFalse(
                effectPlain.StartsWith(BlockMessageCollector.ActionBlockSubsequentIndent, StringComparison.Ordinal),
                "Center-aligned environmental effect lines should not get combat-block indent",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertFalse(
                effectPlain.Contains("Zombie  affected", StringComparison.Ordinal),
                "Effect line should not contain double space after target name",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var stripped = BlockMessageCollector.StripLeadingActionBlockIndent(new List<ColoredText>
            {
                new ColoredText(BlockMessageCollector.ActionBlockSubsequentIndent + "Zombie affected", Colors.White)
            });
            string strippedPlain = ColoredTextRenderer.RenderAsPlainText(stripped);
            TestBase.AssertEqual(
                "Zombie affected",
                strippedPlain,
                "StripLeadingActionBlockIndent removes leading padding",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
