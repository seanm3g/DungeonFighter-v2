using System;
using RPGGame;
using RPGGame.World.Tags;
using RPGGame.Entity.Actions.ComboRouting;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Phase 3: Tag System & Combo Routing Tests
    /// </summary>
    public static class AdvancedMechanicsTest_Phase3
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;
        
        public static void RunAllTests()
        {
            Console.WriteLine("=== Phase 3: Tag System & Combo Routing ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;
            
            TestTagRegistry();
            TestTagMatcher();
            TestTagAggregator();
            TestTagModifier();
            TestComboRouter();
            
            PrintSummary();
        }
        
        private static void TestTagRegistry()
        {
            Console.WriteLine("Testing TagRegistry...");
            try
            {
                var registry = TagRegistry.Instance;
                registry.RegisterTag("TEST_TAG");
                AssertTrue(registry.IsTagRegistered("TEST_TAG"), "Tag registered and found");
                AssertTrue(registry.IsTagRegistered("test_tag"), "Tag matching is case-insensitive");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Tag registry test failed: {ex.Message}");
            }
        }

        private static void TestTagMatcher()
        {
            Console.WriteLine("Testing TagMatcher...");
            try
            {
                var sourceTags = new[] { "FIRE", "WIZARD", "EPIC" };
                var requiredTags = new[] { "FIRE", "WIZARD" };

                bool hasAll = TagMatcher.HasAllTags(sourceTags, requiredTags);
                AssertTrue(hasAll, "TagMatcher correctly identified all required tags");

                int matchCount = TagMatcher.CountMatchingTags(sourceTags, requiredTags);
                AssertTrue(matchCount == 2, $"Matching tag count: {matchCount} (expected 2)");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Tag matcher test failed: {ex.Message}");
            }
        }

        private static void TestTagAggregator()
        {
            Console.WriteLine("Testing TagAggregator...");
            try
            {
                var character = new Character("Test", 1);
                var tags = TagAggregator.AggregateCharacterTags(character);
                AssertTrue(tags != null, "Tag aggregator returned tag list");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Tag aggregator test failed: {ex.Message}");
            }
        }

        private static void TestTagModifier()
        {
            Console.WriteLine("Testing TagModifier...");
            try
            {
                var modifier = new TagModifier();
                var actor = new Character("Test", 1);

                modifier.AddTemporaryTag(actor, "TEMPORARY_TAG", 3);
                var tags = modifier.GetTemporaryTags(actor);
                AssertTrue(tags.Contains("TEMPORARY_TAG"), "Temporary tag added");

                modifier.UpdateTagDurations(1.0);
                modifier.RemoveTemporaryTag(actor, "TEMPORARY_TAG");
                var tagsAfter = modifier.GetTemporaryTags(actor);
                AssertTrue(!tagsAfter.Contains("TEMPORARY_TAG"), "Temporary tag removed");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Tag modifier test failed: {ex.Message}");
            }
        }

        private static void TestComboRouter()
        {
            Console.WriteLine("Testing ComboRouter...");
            try
            {
                var character = new Character("Test", 1);
                var action = new Action
                {
                    Name = "Test Action"
                };
                action.ComboRouting.JumpToSlot = 2;
                var comboSequence = new System.Collections.Generic.List<Action>
                {
                    new Action { Name = "Action1" },
                    new Action { Name = "Action2" },
                    new Action { Name = "Action3" }
                };

                var result = ComboRouter.RouteCombo(character, action, 0, comboSequence);
                AssertTrue(result.RoutingAction == ComboRouter.RoutingAction.JumpToSlot, "Combo routing identified jump action");
                AssertTrue(result.NextSlotIndex == 1, $"Next slot index: {result.NextSlotIndex} (expected 1)");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Combo router test failed: {ex.Message}");
            }
        }
        
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
            Console.WriteLine("\n=== Phase 3 Test Summary ===");
            Console.WriteLine($"Total Tests: {_testsRun}");
            Console.WriteLine($"Passed: {_testsPassed}");
            Console.WriteLine($"Failed: {_testsFailed}");
            
            if (_testsRun > 0)
            {
                Console.WriteLine($"Success Rate: {(_testsPassed * 100.0 / _testsRun):F1}%");
            }
            
            if (_testsFailed == 0)
            {
                Console.WriteLine("\n✅ All Phase 3 tests passed!");
            }
            else
            {
                Console.WriteLine($"\n❌ {_testsFailed} test(s) failed");
            }
        }
    }
}

