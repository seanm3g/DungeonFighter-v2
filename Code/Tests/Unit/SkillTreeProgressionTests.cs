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
            TestMultiRankSink();
            TestPrimaryPathGate();
            TestSharedRailSpend();
            TestSharedRailRequiresSecondaryAndTag();
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
            int age1Cost = age1?.Cost ?? 1;

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
            var p = new CharacterProgression { BarbarianPoints = 1 };
            p.EnsureSkillTreeRootsGranted();

            var missing = p.TryLearnSkillNode("b-combo", requirePrimaryPath: false);
            TestBase.AssertEqualEnum(CharacterProgression.LearnSkillResult.PrerequisitesMissing, missing,
                "tier-3 without prereq is blocked",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var age1 = p.TryLearnSkillNode("b-age1", requirePrimaryPath: false);
            TestBase.AssertEqualEnum(CharacterProgression.LearnSkillResult.Success, age1,
                "tier-1 age1 learns",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Spent the only available point on age1 rank 1 — another T1 is unaffordable.
            var unaffordable = p.TryLearnSkillNode("b-loot", requirePrimaryPath: false);
            TestBase.AssertEqualEnum(CharacterProgression.LearnSkillResult.InsufficientPoints, unaffordable,
                "not enough remaining points for another skill",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestMultiRankSink()
        {
            Console.WriteLine("--- Multi-rank sink up to maxRank ---");
            var age1 = GameConfiguration.Instance.SkillTrees?.GetNode("b-age1");
            int maxRank = age1?.MaxRank ?? 5;
            TestBase.AssertTrue(maxRank >= 2, "age1 should allow multi-rank sink",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var p = new CharacterProgression { BarbarianPoints = maxRank };
            p.EnsureSkillTreeRootsGranted();

            for (int i = 1; i <= maxRank; i++)
            {
                var r = p.TryLearnSkillNode("b-age1", requirePrimaryPath: false);
                TestBase.AssertEqualEnum(CharacterProgression.LearnSkillResult.Success, r,
                    $"age1 rank {i} learns",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            TestBase.AssertEqual(maxRank, p.GetSkillRank("b-age1"),
                "age1 reached maxRank",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0, p.GetAvailableSkillPoints(WeaponType.Mace),
                "all points sunk into age1 ranks",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var capped = p.TryLearnSkillNode("b-age1", requirePrimaryPath: false);
            TestBase.AssertEqualEnum(CharacterProgression.LearnSkillResult.MaxRankReached, capped,
                "cannot exceed maxRank",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var might = GameConfiguration.Instance.SkillTrees?.GetNode("b-might");
            TestBase.AssertEqual(1, might?.Cost ?? -1, "action cost is 1",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1, might?.MaxRank ?? -1, "action maxRank is 1",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestPrimaryPathGate()
        {
            Console.WriteLine("--- Primary path gate ---");
            var p = new CharacterProgression { BarbarianPoints = 10, WarriorPoints = 1 };
            p.EnsureSkillTreeRootsGranted();
            // Primary should be Mace (higher points)
            var denied = p.TryLearnSkillNode("w-loot", requirePrimaryPath: true);
            TestBase.AssertEqualEnum(CharacterProgression.LearnSkillResult.NotPrimaryPath, denied,
                "cannot spend into non-shared non-primary sword node",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // w-cunning is sharedWith Mace and first on Tempo — secondary Sword rail should allow spend.
            var sharedOk = p.TryLearnSkillNode("w-cunning", requirePrimaryPath: true);
            TestBase.AssertEqualEnum(CharacterProgression.LearnSkillResult.Success, sharedOk,
                "shared secondary node spends into Warrior path from Mace primary",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var ok = p.TryLearnSkillNode("b-age1", requirePrimaryPath: true);
            TestBase.AssertEqualEnum(CharacterProgression.LearnSkillResult.Success, ok,
                "can spend into primary mace tree",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestSharedRailSpend()
        {
            Console.WriteLine("--- Shared rail spend (Concept A) ---");
            // Sword primary, Wand secondary → Spellblade rail from Arcane Weave
            var p = new CharacterProgression { WarriorPoints = 8, WizardPoints = 4 };
            p.EnsureSkillTreeRootsGranted();
            TestBase.AssertEqualEnum(WeaponType.Sword, p.GetPrimaryClassWeaponType()!.Value,
                "primary is Sword", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqualEnum(WeaponType.Wand, p.GetSecondaryClassWeaponType()!.Value,
                "secondary is Wand", ref _testsRun, ref _testsPassed, ref _testsFailed);

            var rail = SkillTreeService.GetSharedRailNodes(p);
            TestBase.AssertTrue(rail.Any(n => n.Id == "z-age1"),
                "rail includes First Glyph (sharedWith Sword)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(rail.Any(n => n.Id == "z-readbook"),
                "rail includes Read Book",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(rail.All(n => n.IsSharedWith(WeaponType.Sword)),
                "every rail node is tagged for Sword",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            int wandAvailBefore = p.GetAvailableSkillPoints(WeaponType.Wand);
            // Read Book is first on Echo (root prereq only) — avoids same-branch sibling layout gates.
            var learn = p.TryLearnSkillNode("z-readbook", requirePrimaryPath: true);
            TestBase.AssertEqualEnum(CharacterProgression.LearnSkillResult.Success, learn,
                "learn shared Wand node from Sword-primary view",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(wandAvailBefore - 1, p.GetAvailableSkillPoints(WeaponType.Wand),
                "Wand SP reduced; Sword SP unchanged for this purchase",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(8, p.GetAvailableSkillPoints(WeaponType.Sword),
                "Sword available unchanged after Wand rail spend",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var model = SkillTreeService.BuildDisplayModel(p);
            TestBase.AssertTrue(model.HasRail, "display model exposes rail",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(model.AllNodes.Count > model.PrimaryCount,
                "all nodes = primary + rail",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(
                (model.RailTitle ?? "").Contains("Spellblade", StringComparison.OrdinalIgnoreCase)
                || (model.RailTitle ?? "").Contains("SHARED", StringComparison.OrdinalIgnoreCase),
                "rail title names duo or SHARED",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestSharedRailRequiresSecondaryAndTag()
        {
            Console.WriteLine("--- Shared rail gate: non-shared secondary denied ---");
            var p = new CharacterProgression { BarbarianPoints = 10, WarriorPoints = 3 };
            p.EnsureSkillTreeRootsGranted();
            // w-loot is not marked sharedWith Mace
            var denied = p.TryLearnSkillNode("w-loot", requirePrimaryPath: true);
            TestBase.AssertEqualEnum(CharacterProgression.LearnSkillResult.NotPrimaryPath, denied,
                "non-shared secondary node still blocked",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var solo = new CharacterProgression { WarriorPoints = 5 };
            solo.EnsureSkillTreeRootsGranted();
            TestBase.AssertEqual(0, SkillTreeService.GetSharedRailNodes(solo).Count,
                "no rail without secondary path",
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
