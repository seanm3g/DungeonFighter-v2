using System;
using RPGGame.UI;
using RPGGame.UI.Chunking;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.UI.Chunking
{
    /// <summary>
    /// Comprehensive tests for ChunkStrategyFactory
    /// Tests chunk strategy selection
    /// </summary>
    public static class ChunkStrategyFactoryTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all ChunkStrategyFactory tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== ChunkStrategyFactory Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestGetStrategy_Sentence();
            TestGetStrategy_Paragraph();
            TestGetStrategy_Line();
            TestGetStrategy_Semantic();

            TestBase.PrintSummary("ChunkStrategyFactory Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Strategy Selection Tests

        private static void TestGetStrategy_Sentence()
        {
            Console.WriteLine("--- Testing GetStrategy - Sentence ---");

            var strategy = ChunkStrategyFactory.GetStrategy(ChunkedTextReveal.ChunkStrategy.Sentence);
            
            TestBase.AssertNotNull(strategy,
                "GetStrategy should return a strategy for Sentence",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestGetStrategy_Paragraph()
        {
            Console.WriteLine("\n--- Testing GetStrategy - Paragraph ---");

            var strategy = ChunkStrategyFactory.GetStrategy(ChunkedTextReveal.ChunkStrategy.Paragraph);
            
            TestBase.AssertNotNull(strategy,
                "GetStrategy should return a strategy for Paragraph",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestGetStrategy_Line()
        {
            Console.WriteLine("\n--- Testing GetStrategy - Line ---");

            var strategy = ChunkStrategyFactory.GetStrategy(ChunkedTextReveal.ChunkStrategy.Line);
            
            TestBase.AssertNotNull(strategy,
                "GetStrategy should return a strategy for Line",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestGetStrategy_Semantic()
        {
            Console.WriteLine("\n--- Testing GetStrategy - Semantic ---");

            var strategy = ChunkStrategyFactory.GetStrategy(ChunkedTextReveal.ChunkStrategy.Semantic);
            
            TestBase.AssertNotNull(strategy,
                "GetStrategy should return a strategy for Semantic",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
