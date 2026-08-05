using System;
using System.Linq;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Skill Point spend / learn API for class skill trees.
    /// </summary>
    public static class SkillTreeProgressionTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== SkillTreeProgression Tests ===\n");
            _testsRun = _testsPassed = _testsFailed = 0;

            EnsureTreesLoaded();
            TestRootAutoGrantOnAward();
            TestSpendDoesNotDropLifetimeRank();
            TestPrerequisitesAndCostGate();
            TestPrimaryPathGate();
            TestActionUnlockOnLearn();
            TestSkillTreeMenuHandlerBack();

            TestBase.PrintSummary("SkillTreeProgression Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void EnsureTreesLoaded()
        {
            var trees = SkillTreesConfig.TryLoadFromGameDataFile();
            TestBase.AssertTrue(trees != null && trees.Trees.Count == 4,
                "SkillTrees.json should load four class trees",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            if (trees != null)
                GameConfiguration.Instance.SkillTrees = trees;
        }

        private static void TestRootAutoGrantOnAward()
        {
            Console.WriteLine("--- Root auto-grant on AwardClassPoint ---");
            var p = new CharacterProgression();
            TestBase.AssertEqual(0, p.LearnedSkillRanks.Count, "no learned nodes before award",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            p.AwardClassPoint(WeaponType.Mace);
            TestBase.AssertTrue(p.HasLearnedSkill("b-tribe"), "mace Level 1 root auto-granted",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1, p.BarbarianPoints, "lifetime points still 1",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1, p.GetAvailableSkillPoints(WeaponType.Mace),
                "root costs 0 so available remains 1",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestSpendDoesNotDropLifetimeRank()
        {
            Console.WriteLine("--- Spend does not drop lifetime rank ---");
            var p = new CharacterProgression { BarbarianPoints = 20 };
            p.EnsureSkillTreeRootsGranted();
            int lifetimeBefore = p.BarbarianPoints;
            var age1 = GameConfiguration.Instance.SkillTrees?.GetNode("b-age1");
            int age1Cost = age1?.Cost ?? 4;

            var result = p.TryLearnSkillNode("b-age1", requirePrimaryPath: false);
            TestBase.AssertEqualEnum(CharacterProgression.LearnSkillResult.Success, result,
                "tier-1 node learns with enough points",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(lifetimeBefore, p.BarbarianPoints,
                "lifetime BarbarianPoints unchanged after learn",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(lifetimeBefore - age1Cost, p.GetAvailableSkillPoints(WeaponType.Mace),
                "available reduced by node cost",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(age1Cost, p.GetSpentSkillPoints(WeaponType.Mace),
                "spent equals learned costs",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestPrerequisitesAndCostGate()
        {
            Console.WriteLine("--- Prerequisites and cost gates ---");
            var p = new CharacterProgression { BarbarianPoints = 5 };
            p.EnsureSkillTreeRootsGranted();

            var missing = p.TryLearnSkillNode("b-combo", requirePrimaryPath: false);
            TestBase.AssertEqualEnum(CharacterProgression.LearnSkillResult.PrerequisitesMissing, missing,
                "tier-3 without prereq is blocked",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var age1 = p.TryLearnSkillNode("b-age1", requirePrimaryPath: false);
            TestBase.AssertEqualEnum(CharacterProgression.LearnSkillResult.Success, age1,
                "tier-1 age1 learns",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Another T1 (or further Alloy node) still gated by remaining SP after spending 4.
            var unaffordable = p.TryLearnSkillNode("b-loot", requirePrimaryPath: false);
            TestBase.AssertEqualEnum(CharacterProgression.LearnSkillResult.InsufficientPoints, unaffordable,
                "not enough remaining points for another tier-1",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestPrimaryPathGate()
        {
            Console.WriteLine("--- Primary path gate ---");
            var p = new CharacterProgression { BarbarianPoints = 10, WarriorPoints = 1 };
            p.EnsureSkillTreeRootsGranted();
            // Primary should be Mace (higher points)
            var denied = p.TryLearnSkillNode("w-age1", requirePrimaryPath: true);
            TestBase.AssertEqualEnum(CharacterProgression.LearnSkillResult.NotPrimaryPath, denied,
                "cannot spend into non-primary sword tree",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var ok = p.TryLearnSkillNode("b-age1", requirePrimaryPath: true);
            TestBase.AssertEqualEnum(CharacterProgression.LearnSkillResult.Success, ok,
                "can spend into primary mace tree",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestActionUnlockOnLearn()
        {
            Console.WriteLine("--- Action unlock on learn ---");
            var character = TestDataBuilders.Character().WithName("SkillHero").Build();
            character.Progression.BarbarianPoints = 30;
            character.Progression.EnsureSkillTreeRootsGranted();

            var result = SkillTreeService.TryLearn(character, "b-might", rebuildActions: true);
            TestBase.AssertEqualEnum(CharacterProgression.LearnSkillResult.Success, result,
                "learn Might action node",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            bool has = character.ActionPool.Any(a =>
                string.Equals(a.action.Name, "MIGHT", StringComparison.OrdinalIgnoreCase));
            TestBase.AssertTrue(has, "MIGHT added to action pool after learn",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestSkillTreeMenuHandlerBack()
        {
            Console.WriteLine("--- SkillTreeMenuHandler back to GameLoop ---");
            var stateManager = new GameStateManager();
            var character = TestDataBuilders.Character().WithName("TreeHero").Build();
            character.Progression.BarbarianPoints = 5;
            character.Progression.EnsureSkillTreeRootsGranted();
            stateManager.SetCurrentPlayer(character);

            bool back = false;
            var handler = new SkillTreeMenuHandler(stateManager, null);
            handler.ShowGameLoopEvent += () => back = true;
            handler.ShowSkillTree();
            TestBase.AssertEqualEnum(GameState.SkillTree, stateManager.CurrentState,
                "ShowSkillTree transitions to SkillTree",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            handler.HandleMenuInput("0");
            TestBase.AssertEqualEnum(GameState.GameLoop, stateManager.CurrentState,
                "0 returns to GameLoop",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(back, "ShowGameLoopEvent raised",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
