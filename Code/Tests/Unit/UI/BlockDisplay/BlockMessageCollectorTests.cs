using System;
using System.Collections.Generic;
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

        #endregion
    }
}
