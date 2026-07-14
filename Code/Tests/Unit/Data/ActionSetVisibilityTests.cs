using System;
using System.Linq;
using RPGGame;
using RPGGame.Data;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    /// <summary>Active Action set visibility for gameplay / Action Lab.</summary>
    public static class ActionSetVisibilityTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== ActionSetVisibility Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestIsIncluded_NullMaxAllowsAll();
            TestIsIncluded_RespectsMaxTier();
            TestResolveInitialSelection_PrefersStoredWhenValid();
            TestResolveInitialSelection_FallsBackToDataMax();
            TestActionLoader_FiltersCatalogByActiveSet();

            TestBase.PrintSummary("ActionSetVisibility Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestIsIncluded_NullMaxAllowsAll()
        {
            Console.WriteLine("--- IsIncluded with null max allows all tiers ---");
            var previous = GameSettings.Instance.ActionsActiveSetMaxTier;
            try
            {
                GameSettings.Instance.ActionsActiveSetMaxTier = null;
                TestBase.AssertTrue(ActionSetVisibility.IsIncluded(0), "tier 0 included", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(ActionSetVisibility.IsIncluded(99), "tier 99 included when null", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                GameSettings.Instance.ActionsActiveSetMaxTier = previous;
            }
        }

        private static void TestIsIncluded_RespectsMaxTier()
        {
            Console.WriteLine("--- IsIncluded respects max tier ---");
            var previous = GameSettings.Instance.ActionsActiveSetMaxTier;
            try
            {
                GameSettings.Instance.ActionsActiveSetMaxTier = 1;
                TestBase.AssertTrue(ActionSetVisibility.IsIncluded(0), "tier 0 <= 1", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(ActionSetVisibility.IsIncluded(1), "tier 1 <= 1", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(!ActionSetVisibility.IsIncluded(2), "tier 2 > 1 excluded", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                GameSettings.Instance.ActionsActiveSetMaxTier = previous;
            }
        }

        private static void TestResolveInitialSelection_PrefersStoredWhenValid()
        {
            Console.WriteLine("--- ResolveInitialSelection prefers stored when valid ---");
            TestBase.AssertEqual(1, ActionSetVisibility.ResolveInitialSelection(3, 1), "stored 1 within 0..3", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0, ActionSetVisibility.ResolveInitialSelection(3, 0), "stored 0 kept", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestResolveInitialSelection_FallsBackToDataMax()
        {
            Console.WriteLine("--- ResolveInitialSelection falls back to data max ---");
            TestBase.AssertEqual(3, ActionSetVisibility.ResolveInitialSelection(3, null), "null -> data max", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(3, ActionSetVisibility.ResolveInitialSelection(3, 9), "too high -> data max", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0, ActionSetVisibility.ResolveInitialSelection(0, null), "empty catalog max 0", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestActionLoader_FiltersCatalogByActiveSet()
        {
            Console.WriteLine("--- ActionLoader catalog honors active set ---");
            var previous = GameSettings.Instance.ActionsActiveSetMaxTier;
            try
            {
                ActionLoader.LoadActions();
                GameSettings.Instance.ActionsActiveSetMaxTier = null;
                int allVisible = ActionLoader.GetAllActionNames().Count;
                int allLoaded = ActionLoader.GetAllLoadedActionNames().Count;
                TestBase.AssertTrue(allLoaded > 0, "actions loaded", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(allLoaded, allVisible, "null filter matches full load", ref _testsRun, ref _testsPassed, ref _testsFailed);

                GameSettings.Instance.ActionsActiveSetMaxTier = 0;
                int through0 = ActionLoader.GetAllActionNames().Count;
                TestBase.AssertTrue(through0 <= allLoaded, "through tier 0 ≤ full", ref _testsRun, ref _testsPassed, ref _testsFailed);

                var activeData = ActionLoader.GetActiveSetActionData();
                TestBase.AssertTrue(activeData.All(a => a.Tier <= 0), "active set data all tier ≤ 0", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(through0, activeData.Count, "names count matches active data", ref _testsRun, ref _testsPassed, ref _testsFailed);

                var highTier = ActionLoader.GetAllActionData().Where(a => a.Tier > 0).Take(1).ToList();
                if (highTier.Count > 0)
                {
                    string highName = highTier[0].Name;
                    TestBase.AssertTrue(!ActionLoader.HasAction(highName), "HasAction false for filtered-out tier", ref _testsRun, ref _testsPassed, ref _testsFailed);
                    TestBase.AssertTrue(ActionLoader.GetAction(highName) == null, "GetAction null for filtered-out tier", ref _testsRun, ref _testsPassed, ref _testsFailed);
                    TestBase.AssertTrue(ActionLoader.GetActionData(highName) != null, "GetActionData still returns definition", ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
                else
                {
                    TestBase.AssertTrue(true, "no higher-tier rows to probe (ok)", ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }
            finally
            {
                GameSettings.Instance.ActionsActiveSetMaxTier = previous;
            }
        }
    }
}
