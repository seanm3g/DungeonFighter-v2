using System;
using System.Collections.Generic;
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
            TestBranchesWaitForTrunk();
            TestMultiRankSink();
            TestPrimaryPathGate();
            TestSharedRailSpend();
            TestTreeLayoutGrowsBottomUp();
            TestCoreForksAreEitherOr();
            TestSharedRailRequiresSecondaryAndTag();
            TestActionUnlockOnLearn();
            TestSkillTreeMenuHandlerBack();

            TestBase.PrintSummary("SkillTreeProgression Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void EnsureTreesLoaded()
        {
            var trees = SkillTreesConfig.TryLoadFromGameDataFile();
            TestBase.AssertTrue(trees != null && trees.Trees.Count(t => t.Parents.Count == 0) == 4
                    && trees.Trees.Count(t => t.Parents.Count == 2) == 6,
                "SkillTrees.json should load four class trees and six hybrid trees",
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
            var might = GameConfiguration.Instance.SkillTrees?.GetNode("b-might");
            int age1Cost = might?.Cost ?? 1;

            var result = p.TryLearnSkillNode("b-might", requirePrimaryPath: false);
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

            var missing = p.TryLearnSkillNode("b-scar", requirePrimaryPath: false);
            TestBase.AssertEqualEnum(CharacterProgression.LearnSkillResult.PrerequisitesMissing, missing,
                "trunk tip without prereq is blocked",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var might = p.TryLearnSkillNode("b-might", requirePrimaryPath: false);
            TestBase.AssertEqualEnum(CharacterProgression.LearnSkillResult.Success, might,
                "first trunk node learns",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Spent the only available point on Might — the next trunk node is unaffordable.
            var unaffordable = p.TryLearnSkillNode("b-age1", requirePrimaryPath: false);
            TestBase.AssertEqualEnum(CharacterProgression.LearnSkillResult.InsufficientPoints, unaffordable,
                "not enough remaining points for another skill",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestBranchesWaitForTrunk()
        {
            Console.WriteLine("--- Branches grow from the trunk tip ---");
            var tree = GameConfiguration.Instance.SkillTrees?.GetTreeForWeapon(WeaponType.Mace);
            var trunk = tree?.Nodes.Where(n => n.Tier > 0 && n.Branch == "Core").OrderBy(n => n.Tier).ToList()
                ?? new List<SkillTreeNodeDefinition>();
            TestBase.AssertEqual(10, trunk.Count, "mace tree opens with a 10-node trunk",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("b-might", trunk.FirstOrDefault()?.Id ?? "", "trunk starts with the stat action",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var p = new CharacterProgression { BarbarianPoints = 30 };
            p.EnsureSkillTreeRootsGranted();
            foreach (var node in trunk.Take(trunk.Count - 1))
                p.TryLearnSkillNode(node.Id, requirePrimaryPath: false);

            var early = p.TryLearnSkillNode("b-kin", requirePrimaryPath: false);
            TestBase.AssertEqualEnum(CharacterProgression.LearnSkillResult.PrerequisitesMissing, early,
                "branch locked until the trunk tip is learned",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var lean = new CharacterProgression { BarbarianPoints = 2 };
            lean.EnsureSkillTreeRootsGranted();
            lean.TryLearnSkillNode("b-might", requirePrimaryPath: false);
            lean.TryLearnSkillNode("b-age1", requirePrimaryPath: false);
            var freeLoot = lean.TryLearnSkillNode("b-loot", requirePrimaryPath: false);
            TestBase.AssertEqualEnum(CharacterProgression.LearnSkillResult.Success, freeLoot,
                "free trunk step learns with no points left",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(2, lean.GetSpentSkillPoints(WeaponType.Mace), "free trunk step costs no point",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            p.TryLearnSkillNode(trunk[^1].Id, requirePrimaryPath: false);
            var open = p.TryLearnSkillNode("b-kin", requirePrimaryPath: false);
            TestBase.AssertEqualEnum(CharacterProgression.LearnSkillResult.Success, open,
                "branch opens once the trunk is complete",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestMultiRankSink()
        {
            Console.WriteLine("--- Multi-rank sink up to maxRank ---");
            var age1 = GameConfiguration.Instance.SkillTrees?.GetNode("b-age1");
            int maxRank = age1?.MaxRank ?? 5;
            TestBase.AssertTrue(maxRank >= 2, "age1 should allow multi-rank sink",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var p = new CharacterProgression { BarbarianPoints = maxRank + 1 };
            p.EnsureSkillTreeRootsGranted();
            p.TryLearnSkillNode("b-might", requirePrimaryPath: false); // trunk step before age1

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
            // A path with no earned points stays closed.
            var denied = p.TryLearnSkillNode("z-age1", requirePrimaryPath: true);
            TestBase.AssertEqualEnum(CharacterProgression.LearnSkillResult.NotPrimaryPath, denied,
                "cannot spend into a wand tree with 0 wizard points",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Secondary class tree spends its own points (switch tree on the skill screen).
            var secondaryNode = p.TryLearnSkillNode("w-cunning", requirePrimaryPath: true);
            TestBase.AssertEqualEnum(CharacterProgression.LearnSkillResult.Success, secondaryNode,
                "secondary-class node spends warrior points",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Viewed tree cycles between paths with points.
            TestBase.AssertTrue(SkillTreeService.CycleViewedPath(p) && SkillTreeService.GetViewedPath(p) == WeaponType.Sword,
                "T cycles skill screen to the sword tree",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(SkillTreeService.BuildDisplayModel(p).PrimaryPath == WeaponType.Sword,
                "display model shows the viewed tree",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            p.SkillTreeViewPath = null;

            var ok = p.TryLearnSkillNode("b-might", requirePrimaryPath: true);
            TestBase.AssertEqualEnum(CharacterProgression.LearnSkillResult.Success, ok,
                "can spend into primary mace tree",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestSharedRailSpend()
        {
            Console.WriteLine("--- Hybrid rail: Spellblade tree for Sword primary + Wand secondary ---");
            var p = new CharacterProgression { WarriorPoints = 14, WizardPoints = 12 };
            p.EnsureSkillTreeRootsGranted();
            TestBase.AssertEqualEnum(WeaponType.Sword, p.GetPrimaryClassWeaponType()!.Value,
                "primary is Sword", ref _testsRun, ref _testsPassed, ref _testsFailed);

            var rail = SkillTreeService.GetSharedRailNodes(p);
            TestBase.AssertTrue(rail.Count > 0 && rail.All(n => n.Id.StartsWith("sb-", StringComparison.OrdinalIgnoreCase)),
                "rail shows the Spellblade hybrid tree",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(p.GetSkillRank("sb-root") >= 1, "hybrid root granted at 10 + 10 points",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            int swordBefore = p.GetAvailableSkillPoints(WeaponType.Sword);
            int wandBefore = p.GetAvailableSkillPoints(WeaponType.Wand);
            var learn = p.TryLearnSkillNode("sb-attr", requirePrimaryPath: true);
            TestBase.AssertEqualEnum(CharacterProgression.LearnSkillResult.Success, learn,
                "learn the Wits attribute node", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(swordBefore + wandBefore - 1,
                p.GetAvailableSkillPoints(WeaponType.Sword) + p.GetAvailableSkillPoints(WeaponType.Wand),
                "a hybrid rank costs one point split across the parents",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var gated = p.TryLearnSkillNode("sb-acc-attr", requirePrimaryPath: true);
            TestBase.AssertEqualEnum(CharacterProgression.LearnSkillResult.PrerequisitesMissing, gated,
                "deep hybrid tier gated on the lower parent's points",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var model = SkillTreeService.BuildDisplayModel(p);
            TestBase.AssertTrue(model.HasRail && (model.RailTitle ?? "").Contains("Spellblade", StringComparison.OrdinalIgnoreCase),
                "rail title names the hybrid", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestTreeLayoutGrowsBottomUp()
        {
            Console.WriteLine("--- Skill tree screen grows bottom-up ---");
            var p = new CharacterProgression { WizardPoints = 14, WarriorPoints = 12 };
            p.EnsureSkillTreeRootsGranted();
            var model = SkillTreeService.BuildDisplayModel(p);
            var layout = RPGGame.UI.Avalonia.Renderers.SkillTreeRenderer.GetLayout(model);
            var primary = model.PrimaryNodes;
            int TopOf(string branch, Func<IEnumerable<int>, int> pick) => pick(Enumerable.Range(0, primary.Count)
                .Where(i => string.Equals(primary[i].Branch, branch, StringComparison.OrdinalIgnoreCase))
                .Select(i => layout.Top[i]));

            var trunk = layout.Trunk;
            TestBase.AssertTrue(trunk.Count > 1 && Enumerable.Range(1, trunk.Count - 1).All(k =>
                    primary[trunk[k]].Tier == primary[trunk[k - 1]].Tier
                        ? layout.Top[trunk[k]] == layout.Top[trunk[k - 1]] && layout.ColPos[trunk[k]] > layout.ColPos[trunk[k - 1]]
                        : layout.Top[trunk[k]] < layout.Top[trunk[k - 1]]),
                "trunk climbs from tier 0, same-tier Core choices side by side", ref _testsRun, ref _testsPassed, ref _testsFailed);
            int tipTop = layout.Top[trunk[^1]];
            TestBase.AssertTrue(layout.BranchSeqs.SelectMany(s => s).All(i => layout.Top[i] < tipTop)
                    && layout.ForkRow > layout.TitleRow && layout.ForkRow < tipTop,
                "branches fork above the trunk tip", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(layout.Confluence.Count > 0
                    && layout.Confluence.All(c => layout.BranchSeqs.SelectMany(s => s).All(i => layout.Top[c] < layout.Top[i])),
                "Confluence crowns the top", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(model.HasRail && layout.RailBaseTop == layout.Top[trunk[0]]
                    && Enumerable.Range(model.PrimaryCount, model.RailNodes.Count).All(i => layout.Top[i] < layout.RailBaseTop),
                "rail base sits level with the roots, its skills above", ref _testsRun, ref _testsPassed, ref _testsFailed);

            bool overlap = false;
            for (int a = 0; a < model.AllNodes.Count; a++)
                for (int b = a + 1; b < model.AllNodes.Count; b++)
                    if (Math.Abs(layout.ColPos[a] - layout.ColPos[b]) < 1
                        && Math.Abs(layout.Top[a] - layout.Top[b]) < RPGGame.UI.Avalonia.Renderers.SkillTreeRenderer.NodeHeight)
                        overlap = true;
            TestBase.AssertTrue(!overlap && layout.Top.All(t => t >= 0 && t + 2 <= layout.ContentHeight),
                "boxes don't overlap and fit the content", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(TopOf("Core", Enumerable.Max) == layout.Top[trunk[0]],
                "lowest box is the tier 0 root", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCoreForksAreEitherOr()
        {
            Console.WriteLine("--- Core forks: any skill on the tier below opens the next ---");
            var p = new CharacterProgression { BarbarianPoints = 10 };
            p.EnsureSkillTreeRootsGranted();
            var tree = SkillTreePrerequisites.FindOwnerTree(SkillTreeService.Trees.GetNode("b-might")!)!;
            TestBase.AssertEqual(2, SkillTreePrerequisites.GetCorePredecessorOptions(tree, SkillTreeService.Trees.GetNode("b-loot")!).Count,
                "Blood Market joins the Might / First Steel Age fork", ref _testsRun, ref _testsPassed, ref _testsFailed);
            var afterCadence = SkillTreePrerequisites.GetCorePredecessorOptions(tree, SkillTreeService.Trees.GetNode("b-stun")!);
            TestBase.AssertTrue(afterCadence.Count == 1 && afterCadence[0].Id == "b-combo",
                "Cadence is a required step, not a fork option", ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqualEnum(CharacterProgression.LearnSkillResult.Success, p.TryLearnSkillNode("b-age1", requirePrimaryPath: true),
                "either fork skill opens straight off the root", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqualEnum(CharacterProgression.LearnSkillResult.Success, p.TryLearnSkillNode("b-loot", requirePrimaryPath: true),
                "one side of the fork is enough to move on", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqualEnum(CharacterProgression.LearnSkillResult.Success, p.TryLearnSkillNode("b-might", requirePrimaryPath: true),
                "the other side stays learnable later", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqualEnum(CharacterProgression.LearnSkillResult.PrerequisitesMissing, p.TryLearnSkillNode("b-bludgeon", requirePrimaryPath: true),
                "a fork can't be skipped entirely", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestSharedRailRequiresSecondaryAndTag()
        {
            Console.WriteLine("--- Hybrid tree locked below 10 points in a parent ---");
            var p = new CharacterProgression { BarbarianPoints = 20, WarriorPoints = 6 };
            p.EnsureSkillTreeRootsGranted();
            var locked = p.TryLearnSkillNode("wb-attr", requirePrimaryPath: true);
            TestBase.AssertEqualEnum(CharacterProgression.LearnSkillResult.PrerequisitesMissing, locked,
                "Warbrute stays locked with 6 Warrior points",
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

            // Might is the first step of the trunk.
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
