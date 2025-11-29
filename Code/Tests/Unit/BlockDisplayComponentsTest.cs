using System;
using System.Collections.Generic;
using RPGGame.UI.BlockDisplay;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Unit tests for BlockDisplay extracted components
    /// </summary>
    public static class BlockDisplayComponentsTest
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;
        
        /// <summary>
        /// Runs all BlockDisplay component tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== BlockDisplay Components Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;
            
            // EntityNameExtractor tests
            TestExtractEntityNameFromBrackets();
            TestExtractEntityNameFromHits();
            TestExtractEntityNameFromMisses();
            TestExtractEntityNameFromUses();
            TestExtractEntityNameNull();
            
            // BlockMessageCollector tests
            TestCollectActionBlockMessages();
            TestCollectActionBlockMessagesWithNulls();
            
            PrintSummary();
        }
        
        #region EntityNameExtractor Tests
        
        private static void TestExtractEntityNameFromBrackets()
        {
            Console.WriteLine("--- Testing ExtractEntityNameFromBrackets ---");
            
            string message = "[Player] hits enemy for 10 damage";
            string? result = EntityNameExtractor.ExtractEntityNameFromMessage(message);
            
            AssertTrue(result == "Player", $"Should extract 'Player' from brackets, got: {result}");
        }
        
        private static void TestExtractEntityNameFromHits()
        {
            Console.WriteLine("\n--- Testing ExtractEntityNameFromHits ---");
            
            string message = "Player hits enemy for 10 damage";
            string? result = EntityNameExtractor.ExtractEntityNameFromMessage(message);
            
            AssertTrue(result == "Player", $"Should extract 'Player' from 'hits', got: {result}");
        }
        
        private static void TestExtractEntityNameFromMisses()
        {
            Console.WriteLine("\n--- Testing ExtractEntityNameFromMisses ---");
            
            string message = "Enemy misses Player";
            string? result = EntityNameExtractor.ExtractEntityNameFromMessage(message);
            
            AssertTrue(result == "Enemy", $"Should extract 'Enemy' from 'misses', got: {result}");
        }
        
        private static void TestExtractEntityNameFromUses()
        {
            Console.WriteLine("\n--- Testing ExtractEntityNameFromUses ---");
            
            string message = "Player uses Fireball on Enemy";
            string? result = EntityNameExtractor.ExtractEntityNameFromMessage(message);
            
            AssertTrue(result == "Player", $"Should extract 'Player' from 'uses', got: {result}");
        }
        
        private static void TestExtractEntityNameNull()
        {
            Console.WriteLine("\n--- Testing ExtractEntityNameNull ---");
            
            string? result1 = EntityNameExtractor.ExtractEntityNameFromMessage(null!);
            string? result2 = EntityNameExtractor.ExtractEntityNameFromMessage("");
            string? result3 = EntityNameExtractor.ExtractEntityNameFromMessage("Some random text");
            
            AssertTrue(result1 == null, "Should return null for null input");
            AssertTrue(result2 == null, "Should return null for empty input");
            AssertTrue(result3 == null, "Should return null for unrecognized format");
        }
        
        #endregion
        
        #region BlockMessageCollector Tests
        
        private static void TestCollectActionBlockMessages()
        {
            Console.WriteLine("\n--- Testing CollectActionBlockMessages ---");
            
            var actionText = new List<ColoredText> { new ColoredText("Player hits", Avalonia.Media.Colors.White) };
            var rollInfo = new List<ColoredText> { new ColoredText("roll: 15", Avalonia.Media.Colors.White) };
            var statusEffects = new List<List<ColoredText>> 
            { 
                new List<ColoredText> { new ColoredText("Bleed applied", Avalonia.Media.Colors.Red) }
            };
            
            var result = BlockMessageCollector.CollectActionBlockMessages(actionText, rollInfo, statusEffects, null, null);
            
            AssertTrue(result.Count >= 3, $"Should have at least 3 message groups, got: {result.Count}");
        }
        
        private static void TestCollectActionBlockMessagesWithNulls()
        {
            Console.WriteLine("\n--- Testing CollectActionBlockMessagesWithNulls ---");
            
            var result = BlockMessageCollector.CollectActionBlockMessages(null, null, null, null, null);
            
            AssertTrue(result.Count == 0, $"Should return empty list for all nulls, got: {result.Count}");
        }
        
        #endregion
        
        #region Helper Methods
        
        private static void AssertTrue(bool condition, string message)
        {
            _testsRun++;
            if (condition)
            {
                _testsPassed++;
                Console.WriteLine($"  ✓ {message}");
            }
            else
            {
                _testsFailed++;
                Console.WriteLine($"  ✗ FAILED: {message}");
            }
        }
        
        private static void PrintSummary()
        {
            Console.WriteLine("\n=== Test Summary ===");
            Console.WriteLine($"Total Tests: {_testsRun}");
            Console.WriteLine($"Passed: {_testsPassed}");
            Console.WriteLine($"Failed: {_testsFailed}");
            Console.WriteLine($"Success Rate: {(_testsPassed * 100.0 / _testsRun):F1}%");
            
            if (_testsFailed == 0)
            {
                Console.WriteLine("\n✅ All tests passed!");
            }
            else
            {
                Console.WriteLine($"\n❌ {_testsFailed} test(s) failed");
            }
        }
        
        #endregion
    }
}

