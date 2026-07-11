using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Tests for ActionsTabManager. Verifies RefreshCurrentPlayerActionPool (Component 2)
    /// so that action pool refresh after Settings save is correct.
    /// </summary>
    public static class ActionsTabManagerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        public static void RunAllTests()
        {
            Console.WriteLine("=== ActionsTabManager Tests ===\n");

            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestRefreshCurrentPlayerActionPool_WhenPlayerNull_DoesNotThrow();
            TestRefreshCurrentPlayerActionPool_ResetsComboStep();
            TestRefreshCurrentPlayerActionPool_ActionPoolNotNull();

            TestFindFirstActionNamePrefixIndex_FindsCaseInsensitivePrefix();
            TestFindFirstActionNamePrefixIndex_ReturnsFirstOfOrderedList();
            TestFindFirstActionNamePrefixIndex_EmptyOrMissing_ReturnsMinusOne();

            TestMatchesTierSetFilter_InclusiveThroughMax();
            TestTierSetOption_FormatAndParse();

            TestBase.PrintSummary("ActionsTabManager Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestRefreshCurrentPlayerActionPool_WhenPlayerNull_DoesNotThrow()
        {
            Console.WriteLine("--- RefreshCurrentPlayerActionPool with null player does not throw ---");
            try
            {
                ActionsTabManager.RefreshCurrentPlayerActionPool(null);
                TestBase.AssertTrue(true, "RefreshCurrentPlayerActionPool(null) does not throw", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false, $"RefreshCurrentPlayerActionPool(null) threw: {ex.Message}", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestRefreshCurrentPlayerActionPool_ResetsComboStep()
        {
            Console.WriteLine("--- RefreshCurrentPlayerActionPool resets ComboStep to 0 ---");
            try
            {
                var character = new Character("RefreshTest", 1);
                character.ComboStep = 3;

                ActionsTabManager.RefreshCurrentPlayerActionPool(character);

                TestBase.AssertEqual(0, character.ComboStep, "ComboStep is 0 after refresh", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false, $"RefreshCurrentPlayerActionPool threw: {ex.Message}", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestRefreshCurrentPlayerActionPool_ActionPoolNotNull()
        {
            Console.WriteLine("--- RefreshCurrentPlayerActionPool leaves ActionPool non-null ---");
            try
            {
                var character = new Character("PoolTest", 1);
                ActionsTabManager.RefreshCurrentPlayerActionPool(character);

                TestBase.AssertTrue(character.ActionPool != null, "ActionPool is not null after refresh", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false, $"RefreshCurrentPlayerActionPool threw: {ex.Message}", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestFindFirstActionNamePrefixIndex_FindsCaseInsensitivePrefix()
        {
            Console.WriteLine("--- FindFirstActionNamePrefixIndex is case-insensitive ---");
            var names = new List<string> { "Alpha", "Strike", "strike force" };
            int idx = ActionsTabManager.FindFirstActionNamePrefixIndex(names, "stri");
            TestBase.AssertEqual(1, idx, "Prefix 'stri' matches Strike at index 1", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestFindFirstActionNamePrefixIndex_ReturnsFirstOfOrderedList()
        {
            Console.WriteLine("--- FindFirstActionNamePrefixIndex returns first alphabetical match ---");
            var names = new List<string> { "apple", "apricot", "banana" };
            int idx = ActionsTabManager.FindFirstActionNamePrefixIndex(names, "ap");
            TestBase.AssertEqual(0, idx, "First 'ap*' is apple", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestFindFirstActionNamePrefixIndex_EmptyOrMissing_ReturnsMinusOne()
        {
            Console.WriteLine("--- FindFirstActionNamePrefixIndex returns -1 for empty prefix or no match ---");
            var names = new List<string> { "Zed" };
            TestBase.AssertEqual(-1, ActionsTabManager.FindFirstActionNamePrefixIndex(names, ""), "empty prefix", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(-1, ActionsTabManager.FindFirstActionNamePrefixIndex(names, "x"), "no match", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestMatchesTierSetFilter_InclusiveThroughMax()
        {
            Console.WriteLine("--- MatchesTierSetFilter keeps tiers <= selected max ---");
            var tiers = new[] { 0, 1, 2 };
            var kept = new List<int>();
            foreach (int t in tiers)
            {
                if (ActionsTabManager.MatchesTierSetFilter(t, maxTierInclusive: 1))
                    kept.Add(t);
            }
            TestBase.AssertEqual(2, kept.Count, "two tiers through max 1", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0, kept[0], "includes tier 0", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1, kept[1], "includes tier 1", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(ActionsTabManager.MatchesTierSetFilter(2, 2), "tier equals max kept", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!ActionsTabManager.MatchesTierSetFilter(3, 2), "tier above max excluded", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestTierSetOption_FormatAndParse()
        {
            Console.WriteLine("--- Tier set option format/parse ---");
            string label = ActionsTabManager.FormatTierSetOption(2);
            TestBase.AssertEqual("Through Tier 2", label, "format label", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(ActionsTabManager.TryParseTierSetOption(label, out int max), "parse label", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(2, max, "parsed max", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!ActionsTabManager.TryParseTierSetOption("(All)", out _), "non-set option fails", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
