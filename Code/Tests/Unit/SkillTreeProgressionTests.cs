using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Skill Point spend / multi-rank learn API for class skill trees.
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
            TestSequentialSameBranchUnlock();
            TestPrimaryPathGate();
            TestActionUnlockOnLearn();
            TestMultiRankSameCostAndMax();
            TestLegacyIdListMigrationRanks();
            TestNewStackableNodesExist();
            TestMultiRankNodesHavePerRankBenefit();
            TestBinaryPassivesCapAtOneRank();
            TestSkillTreeMenuHandlerBack();
            TestScrollNormalizeKeepsBottomWhenMaxNotTierAligned();
            TestStackedBranchTierDoNotShareContentTop();

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
            TestBase.AssertTrue(p.HasLearnedSkill("b-root"), "mace path root auto-granted",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1, p.GetSkillRank("b-root"), "root at rank 1",
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

            var result = p.TryLearnSkillNode("b-venom", requirePrimaryPath: false);
            TestBase.AssertEqualEnum(CharacterProgression.LearnSkillResult.Success, result,
                "tier-1 node learns with enough points",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(lifetimeBefore, p.BarbarianPoints,
                "lifetime BarbarianPoints unchanged after learn",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(lifetimeBefore - 1, p.GetAvailableSkillPoints(WeaponType.Mace),
                "available reduced by node cost 1",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1, p.GetSpentSkillPoints(WeaponType.Mace),
                "spent equals learned costs",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestPrerequisitesAndCostGate()
        {
            Console.WriteLine("--- Prerequisites and cost gates ---");
            var p = new CharacterProgression { BarbarianPoints = 2 };
            p.EnsureSkillTreeRootsGranted();

            var missing = p.TryLearnSkillNode("b-age2", requirePrimaryPath: false);
            TestBase.AssertEqualEnum(CharacterProgression.LearnSkillResult.PrerequisitesMissing, missing,
                "tier-2 blocked until prior Alloy skill is ranked",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Layout order on Alloy T1: First Bronze Age (toward root) then Copper Vein.
            var age1 = p.TryLearnSkillNode("b-age1", requirePrimaryPath: false);
            TestBase.AssertEqualEnum(CharacterProgression.LearnSkillResult.Success, age1,
                "First Bronze Age learns after root",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqualEnum(CharacterProgression.LearnSkillResult.PrerequisitesMissing,
                p.TryLearnSkillNode("b-age2", requirePrimaryPath: false),
                "Second Bronze Age still needs Copper Vein (between age1 and age2 in branch order)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqualEnum(CharacterProgression.LearnSkillResult.Success,
                p.TryLearnSkillNode("b-pack4", requirePrimaryPath: false),
                "Copper Vein after First Bronze Age",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            p.BarbarianPoints = 3;
            TestBase.AssertEqualEnum(CharacterProgression.LearnSkillResult.Success,
                p.TryLearnSkillNode("b-age2", requirePrimaryPath: false),
                "Second Bronze Age after Copper Vein",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestSequentialSameBranchUnlock()
        {
            Console.WriteLine("--- Spatial branch sequence (no leapfrog) ---");
            // Impact T1 layout: Venom (toward root) then Overhand (toward tip).
            var p = new CharacterProgression { BarbarianPoints = 5 };
            p.EnsureSkillTreeRootsGranted();

            TestBase.AssertEqualEnum(CharacterProgression.LearnSkillResult.PrerequisitesMissing,
                p.TryLearnSkillNode("b-pack1", requirePrimaryPath: false),
                "Overhand blocked until Venom Mace (skill below it) has a rank",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqualEnum(CharacterProgression.LearnSkillResult.Success,
                p.TryLearnSkillNode("b-venom", requirePrimaryPath: false),
                "Venom Mace after root",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqualEnum(CharacterProgression.LearnSkillResult.Success,
                p.TryLearnSkillNode("b-pack1", requirePrimaryPath: false),
                "Overhand after Venom Mace",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqualEnum(CharacterProgression.LearnSkillResult.Success,
                p.TryLearnSkillNode("b-mighty", requirePrimaryPath: false),
                "Mighty Swing after Overhand (next on Impact)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqualEnum(CharacterProgression.LearnSkillResult.PrerequisitesMissing,
                p.TryLearnSkillNode("b-warcry", requirePrimaryPath: false),
                "Instinct branch still needs its own sequence",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestPrimaryPathGate()
        {
            Console.WriteLine("--- Primary path gate ---");
            var p = new CharacterProgression { BarbarianPoints = 10, WarriorPoints = 1 };
            p.EnsureSkillTreeRootsGranted();
            var denied = p.TryLearnSkillNode("w-riposte", requirePrimaryPath: true);
            TestBase.AssertEqualEnum(CharacterProgression.LearnSkillResult.NotPrimaryPath, denied,
                "cannot spend into non-primary sword tree",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var ok = p.TryLearnSkillNode("b-venom", requirePrimaryPath: true);
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
            // Impact sequence toward tip: Venom → Overhand → Mighty Swing
            character.Progression.TryLearnSkillNode("b-venom", requirePrimaryPath: false);
            character.Progression.TryLearnSkillNode("b-pack1", requirePrimaryPath: false);

            var result = SkillTreeService.TryLearn(character, "b-mighty", rebuildActions: true);
            TestBase.AssertEqualEnum(CharacterProgression.LearnSkillResult.Success, result,
                "learn Mighty Swing action node",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            bool has = character.ActionPool.Any(a =>
                string.Equals(a.action.Name, "MIGHTY SWING", StringComparison.OrdinalIgnoreCase));
            TestBase.AssertTrue(has, "MIGHTY SWING added to action pool after learn",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var again = character.Progression.TryLearnSkillNode("b-mighty", requirePrimaryPath: false);
            TestBase.AssertEqualEnum(CharacterProgression.LearnSkillResult.MaxRankReached, again,
                "action unlock stays max rank 1",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestMultiRankSameCostAndMax()
        {
            Console.WriteLine("--- Multi-rank same cost and max ---");
            var p = new CharacterProgression { BarbarianPoints = 40 };
            p.EnsureSkillTreeRootsGranted();
            // First Bronze Age is a flat-stack passive (+8% × Bronze × rank); Venom Mace is binary (maxRank 1).
            var node = SkillTreeService.Trees.GetNode("b-age1");
            TestBase.AssertTrue(node != null && node.MaxRank == 5, "first_bronze_age maxRank 5",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            for (int i = 1; i <= 5; i++)
            {
                var r = p.TryLearnSkillNode("b-age1", requirePrimaryPath: false);
                TestBase.AssertEqualEnum(CharacterProgression.LearnSkillResult.Success, r,
                    $"age1 rank {i} learns",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(i, p.GetSkillRank("b-age1"), $"rank is {i}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            TestBase.AssertEqual(5, p.GetSpentSkillPoints(WeaponType.Mace),
                "5 ranks × 1 SP = 5 spent",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var maxed = p.TryLearnSkillNode("b-age1", requirePrimaryPath: false);
            TestBase.AssertEqualEnum(CharacterProgression.LearnSkillResult.MaxRankReached, maxed,
                "cannot exceed max rank",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqualEnum(SkillTreeService.NodeViewState.Maxed,
                SkillTreeService.GetNodeState(p, node!),
                "view state Maxed at cap",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestMultiRankNodesHavePerRankBenefit()
        {
            Console.WriteLine("--- Multi-rank nodes declare per-rank benefit ---");
            // Runtime-scaled custom effects (SkillEffectRouter GetRank / pack fields).
            var scaledIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "first_bronze_age", "second_bronze_age", "third_bronze_age",
                "gut_instinct", "blood_price", "bone_temper", "mace_mastery", "pain_memory",
                "riposte", "cadence_keeper", "guard_duty", "steady_march", "second_wind",
                "unbroken_line", "ghost_step", "deep_cut", "quick_fingers", "toxic_ledger",
                "no_witnesses", "mobile_bulwark", "spell_memory", "mana_bleed", "focus_lens",
                "bronze_skin", "bronze_knuckle", "phalanx", "metronome", "field_brief",
                "back_channel", "toxin_pressure", "grease_palm", "sigil_burn", "soft_echo",
                "loaded_odds"
            };

            foreach (var tree in SkillTreeService.Trees.Trees)
            {
                foreach (var node in tree.Nodes)
                {
                    if (node.MaxRank <= 1) continue;
                    bool pack = node.DamageModPerRank > 0 || node.HealOnHitPerRank > 0;
                    string cid = string.IsNullOrWhiteSpace(node.CustomEffectId) ? node.Id : node.CustomEffectId;
                    bool known = scaledIds.Contains(cid);
                    TestBase.AssertTrue(pack || known,
                        $"{node.Id} maxRank {node.MaxRank} must be a pack or scaled customEffectId",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);

                    string effect = node.Effect ?? "";
                    TestBase.AssertTrue(
                        effect.IndexOf("per rank", StringComparison.OrdinalIgnoreCase) >= 0,
                        $"{node.Id} effect text should mention per rank",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }

            var age2 = SkillTreeService.Trees.GetNode("b-age2");
            TestBase.AssertTrue(age2 != null && age2.MaxRank == 5
                && (age2.Effect?.IndexOf("per rank", StringComparison.OrdinalIgnoreCase) ?? -1) >= 0,
                "Second Bronze Age is multi-rank and documents per-rank benefit",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestBinaryPassivesCapAtOneRank()
        {
            Console.WriteLine("--- Binary passives / actions stay maxRank 1 ---");
            string[] binary =
            {
                "b-venom", "b-alloy", "b-mighty", "b-warcry", "b-rage",
                "w-root", "w-geometry", "w-opening", "w-flow", "w-mastery",
                "r-root", "r-snake", "r-mastery", "z-prime", "z-equation", "z-catalyst"
            };
            foreach (string id in binary)
            {
                var n = SkillTreeService.Trees.GetNode(id);
                TestBase.AssertTrue(n != null && n.MaxRank == 1,
                    $"{id} is binary maxRank 1",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestLegacyIdListMigrationRanks()
        {
            Console.WriteLine("--- Legacy id list migrates to ranks ---");
            var save = new CharacterSaveData
            {
                Name = "Legacy",
                Level = 5,
                LearnedSkillNodeIds = new List<string> { "b-root", "b-venom" },
                BarbarianPoints = 10
            };
            var serializer = new RPGGame.Entity.Services.CharacterSerializer();
            var character = serializer.CreateCharacterFromSaveData(save);
            TestBase.AssertEqual(1, character.Progression.GetSkillRank("b-venom"),
                "legacy id becomes rank 1",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(character.Progression.HasLearnedSkill("b-root"),
                "root present after migrate",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestNewStackableNodesExist()
        {
            Console.WriteLine("--- New stackable nodes + 99 SP capacity ---");
            string[] ids =
            {
                "b-blood", "b-temper", "b-knuckle",
                "w-duty", "w-march", "w-phalanx", "w-metronome", "w-brief",
                "r-cut", "r-fingers", "r-channel", "r-toxin", "r-grease",
                "z-bleed", "z-lens", "z-sigil", "z-soft", "z-odds"
            };
            foreach (string id in ids)
            {
                var n = SkillTreeService.Trees.GetNode(id);
                TestBase.AssertTrue(n != null && n.MaxRank >= 5 && n.Cost == 1,
                    $"{id} exists maxRank>=5 cost 1",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            var pack = SkillTreeService.Trees.GetNode("b-pack4");
            TestBase.AssertTrue(pack != null && pack.MaxRank == 5,
                "Copper Vein pack stays maxRank 5 (new named skills cover sink)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            foreach (var tree in SkillTreeService.Trees.Trees)
            {
                int sink = 0;
                foreach (var node in tree.Nodes)
                {
                    int maxR = Math.Max(1, node.MaxRank);
                    sink += Math.Max(0, node.Cost) * maxR;
                }
                TestBase.AssertTrue(sink >= 99, $"{tree.ClassKey} max SP sink {sink} >= 99",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
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

        private static void TestScrollNormalizeKeepsBottomWhenMaxNotTierAligned()
        {
            Console.WriteLine("--- Scroll normalize preserves bottom pin ---");
            // content 28, viewport 24 ⇒ max scroll 4 (not a TierStride multiple).
            // Old Align(Clamp(Max)) snapped 4 → 0 and hid the roots / blocked scrolling.
            int normalized = RPGGame.UI.Avalonia.Renderers.SkillTreeRenderer.NormalizeScrollOffset(
                int.MaxValue, contentHeight: 28, viewportHeight: 24);
            TestBase.AssertEqual(4, normalized,
                "Bottom pin keeps max scroll when max is not tier-aligned",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            int mid = RPGGame.UI.Avalonia.Renderers.SkillTreeRenderer.NormalizeScrollOffset(
                7, contentHeight: 40, viewportHeight: 24);
            TestBase.AssertEqual(4, mid,
                "Mid-scroll snaps down to ScrollStep (NodeHeight)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            int top = RPGGame.UI.Avalonia.Renderers.SkillTreeRenderer.NormalizeScrollOffset(
                0, contentHeight: 28, viewportHeight: 24);
            TestBase.AssertEqual(0, top,
                "Top of tree stays at scroll 0",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Bottom pin 5 (not ScrollStep-aligned): selecting an on-screen node must not snap scroll to 4.
            int kept = RPGGame.UI.Avalonia.Renderers.SkillTreeRenderer.EnsureNodeVisible(
                scrollOffset: 5, nodeTop: 20, nodeBottom: 24, viewportHeight: 30);
            TestBase.AssertEqual(5, kept,
                "EnsureNodeVisible does not re-align when the node is already fully visible",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestStackedBranchTierDoNotShareContentTop()
        {
            Console.WriteLine("--- Stacked same-branch nodes get distinct Y ---");
            EnsureTreesLoaded();
            var tree = SkillTreeService.Trees.GetTreeForWeapon(WeaponType.Mace);
            TestBase.AssertTrue(tree != null, "Barbarian tree loaded",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            var nodes = RPGGame.UI.Avalonia.Renderers.SkillTreeRenderer.OrderNodes(tree!.Nodes);
            int mighty = RPGGame.UI.Avalonia.Renderers.SkillTreeRenderer.GetNodeTopCell(
                SkillTreeService.Trees.GetNode("b-mighty")!, nodes);
            int blood = RPGGame.UI.Avalonia.Renderers.SkillTreeRenderer.GetNodeTopCell(
                SkillTreeService.Trees.GetNode("b-blood")!, nodes);
            TestBase.AssertTrue(mighty != blood,
                "Mighty Swing and Blood Price must not share the same content Y",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            int contentH = RPGGame.UI.Avalonia.Renderers.SkillTreeRenderer.GetTreeContentHeight(nodes);
            int minTop = nodes.Min(n =>
                RPGGame.UI.Avalonia.Renderers.SkillTreeRenderer.GetNodeTopCell(n, nodes));
            TestBase.AssertEqual(RPGGame.UI.Avalonia.Renderers.SkillTreeRenderer.TopHeadroom, minTop,
                "Highest tier starts below TopHeadroom",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            // Flat layout was 28; stacking + headroom should make the tree taller so scroll is needed.
            TestBase.AssertTrue(contentH > 28,
                $"Stacked content height {contentH} exceeds flat 28",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
