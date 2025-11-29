using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.UI.ColorSystem.Applications.ItemFormatting;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Unit tests for ItemFormatting extracted components
    /// </summary>
    public static class ItemFormattingComponentsTest
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;
        
        /// <summary>
        /// Runs all ItemFormatting component tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== ItemFormatting Components Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;
            
            // ItemKeywordExtractor tests
            TestExtractKeywordOfThe();
            TestExtractKeywordOf();
            TestExtractKeywordNoPrefix();
            TestExtractKeywordEmpty();
            
            // ItemNameParser tests
            TestRemoveRarityPrefix();
            TestExtractPrefixModifications();
            
            PrintSummary();
        }
        
        #region ItemKeywordExtractor Tests
        
        private static void TestExtractKeywordOfThe()
        {
            Console.WriteLine("--- Testing ExtractKeywordOfThe ---");
            
            var (prefix, keyword) = ItemKeywordExtractor.ExtractKeyword("of the Fire");
            
            AssertTrue(prefix == "of the ", $"Should extract 'of the ' prefix, got: '{prefix}'");
            AssertTrue(keyword == "Fire", $"Should extract 'Fire' keyword, got: '{keyword}'");
        }
        
        private static void TestExtractKeywordOf()
        {
            Console.WriteLine("\n--- Testing ExtractKeywordOf ---");
            
            var (prefix, keyword) = ItemKeywordExtractor.ExtractKeyword("of Power");
            
            AssertTrue(prefix == "of ", $"Should extract 'of ' prefix, got: '{prefix}'");
            AssertTrue(keyword == "Power", $"Should extract 'Power' keyword, got: '{keyword}'");
        }
        
        private static void TestExtractKeywordNoPrefix()
        {
            Console.WriteLine("\n--- Testing ExtractKeywordNoPrefix ---");
            
            var (prefix, keyword) = ItemKeywordExtractor.ExtractKeyword("Flaming");
            
            AssertTrue(prefix == "", $"Should have empty prefix, got: '{prefix}'");
            AssertTrue(keyword == "Flaming", $"Should return full name as keyword, got: '{keyword}'");
        }
        
        private static void TestExtractKeywordEmpty()
        {
            Console.WriteLine("\n--- Testing ExtractKeywordEmpty ---");
            
            var (prefix1, keyword1) = ItemKeywordExtractor.ExtractKeyword(null!);
            var (prefix2, keyword2) = ItemKeywordExtractor.ExtractKeyword("");
            
            AssertTrue(prefix1 == "" && keyword1 == "", "Should return empty for null");
            AssertTrue(prefix2 == "" && keyword2 == "", "Should return empty for empty string");
        }
        
        #endregion
        
        #region ItemNameParser Tests
        
        private static void TestRemoveRarityPrefix()
        {
            Console.WriteLine("\n--- Testing RemoveRarityPrefix ---");
            
            string result1 = ItemNameParser.RemoveRarityPrefix("Legendary Sword");
            string result2 = ItemNameParser.RemoveRarityPrefix("Epic Dagger");
            string result3 = ItemNameParser.RemoveRarityPrefix("Common Mace");
            string result4 = ItemNameParser.RemoveRarityPrefix("Sword");
            
            AssertTrue(result1 == "Sword", $"Should remove 'Legendary', got: '{result1}'");
            AssertTrue(result2 == "Dagger", $"Should remove 'Epic', got: '{result2}'");
            AssertTrue(result3 == "Mace", $"Should remove 'Common', got: '{result3}'");
            AssertTrue(result4 == "Sword", $"Should return unchanged if no rarity, got: '{result4}'");
        }
        
        private static void TestExtractPrefixModifications()
        {
            Console.WriteLine("\n--- Testing ExtractPrefixModifications ---");
            
            // Create a mock item for testing
            var item = new Item(ItemType.Weapon, "Flaming Sharp Sword")
            {
                Modifications = new List<Modification>
                {
                    new Modification { Name = "Flaming" },
                    new Modification { Name = "Sharp" }
                }
            };
            
            var (remaining, prefixes) = ItemNameParser.ExtractPrefixModifications(item, "Flaming Sharp Sword");
            
            AssertTrue(prefixes.Count >= 2, $"Should extract 2 prefixes, got: {prefixes.Count}");
            AssertTrue(prefixes.Contains("Flaming"), "Should contain 'Flaming'");
            AssertTrue(prefixes.Contains("Sharp"), "Should contain 'Sharp'");
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

