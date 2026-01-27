using System;
using RPGGame.UI.BlockDisplay;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.UI.BlockDisplay
{
    /// <summary>
    /// Comprehensive tests for EntityNameExtractor
    /// Tests entity name extraction from messages
    /// </summary>
    public static class EntityNameExtractorTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all EntityNameExtractor tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== EntityNameExtractor Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestExtractEntityNameFromMessage_BracketFormat();
            TestExtractEntityNameFromMessage_HitsFormat();
            TestExtractEntityNameFromMessage_MissesFormat();
            TestExtractEntityNameFromMessage_CriticalMissFormat();
            TestExtractEntityNameFromMessage_TakesFormat();
            TestExtractEntityNameFromMessage_IsAffectedFormat();
            TestExtractEntityNameFromMessage_EmptyMessage();
            TestExtractEntityNameFromMessage_NoEntity();

            TestBase.PrintSummary("EntityNameExtractor Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Extraction Tests

        private static void TestExtractEntityNameFromMessage_BracketFormat()
        {
            Console.WriteLine("--- Testing ExtractEntityNameFromMessage - Bracket Format ---");

            string message = "[Hero] hits Enemy for 10 damage";
            string? entityName = EntityNameExtractor.ExtractEntityNameFromMessage(message);
            
            TestBase.AssertEqual("Hero", entityName,
                "Should extract entity name from bracket format",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestExtractEntityNameFromMessage_HitsFormat()
        {
            Console.WriteLine("\n--- Testing ExtractEntityNameFromMessage - Hits Format ---");

            string message = "Hero hits Enemy for 10 damage";
            string? entityName = EntityNameExtractor.ExtractEntityNameFromMessage(message);
            
            TestBase.AssertEqual("Hero", entityName,
                "Should extract entity name from hits format",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestExtractEntityNameFromMessage_MissesFormat()
        {
            Console.WriteLine("\n--- Testing ExtractEntityNameFromMessage - Misses Format ---");

            string message = "Hero misses Enemy";
            string? entityName = EntityNameExtractor.ExtractEntityNameFromMessage(message);
            
            TestBase.AssertEqual("Hero", entityName,
                "Should extract entity name from misses format",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestExtractEntityNameFromMessage_CriticalMissFormat()
        {
            Console.WriteLine("\n--- Testing ExtractEntityNameFromMessage - CRITICAL MISS Format ---");

            string message = "Haldir Rockheart CRITICAL MISS Goblin";
            string? entityName = EntityNameExtractor.ExtractEntityNameFromMessage(message);
            
            TestBase.AssertEqual("Haldir Rockheart", entityName,
                "Should extract entity name from CRITICAL MISS format",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            
            // Test with different entity names
            string message2 = "Goblin CRITICAL MISS Haldir Rockheart";
            string? entityName2 = EntityNameExtractor.ExtractEntityNameFromMessage(message2);
            
            TestBase.AssertEqual("Goblin", entityName2,
                "Should extract entity name from CRITICAL MISS format (different entity)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestExtractEntityNameFromMessage_TakesFormat()
        {
            Console.WriteLine("\n--- Testing ExtractEntityNameFromMessage - Takes Format ---");

            string message = "Enemy takes 5 poison damage";
            string? entityName = EntityNameExtractor.ExtractEntityNameFromMessage(message);
            
            TestBase.AssertEqual("Enemy", entityName,
                "Should extract entity name from takes format",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestExtractEntityNameFromMessage_IsAffectedFormat()
        {
            Console.WriteLine("\n--- Testing ExtractEntityNameFromMessage - Is Affected Format ---");

            string message = "Hero is affected by Bleed";
            string? entityName = EntityNameExtractor.ExtractEntityNameFromMessage(message);
            
            TestBase.AssertEqual("Hero", entityName,
                "Should extract entity name from is affected format",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestExtractEntityNameFromMessage_EmptyMessage()
        {
            Console.WriteLine("\n--- Testing ExtractEntityNameFromMessage - Empty Message ---");

            string? entityName = EntityNameExtractor.ExtractEntityNameFromMessage("");
            
            TestBase.AssertNull(entityName,
                "Should return null for empty message",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestExtractEntityNameFromMessage_NoEntity()
        {
            Console.WriteLine("\n--- Testing ExtractEntityNameFromMessage - No Entity ---");

            string message = "This message has no entity name";
            string? entityName = EntityNameExtractor.ExtractEntityNameFromMessage(message);
            
            TestBase.AssertNull(entityName,
                "Should return null when no entity name is found",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
