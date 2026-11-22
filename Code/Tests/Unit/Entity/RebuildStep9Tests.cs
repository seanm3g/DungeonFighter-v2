using System;
using System.Linq;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Entity
{
    /// <summary>Rebuild step 9: buyable Core slot nodes (D5.3, D7.8) and material branches opening on 2 pieces.</summary>
    public static class RebuildStep9Tests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Rebuild Step 9 Tests ===\n");
            _run = _passed = _failed = 0;

            var trees = GameConfiguration.Instance.SkillTrees;
            int slotNodes = trees?.Trees.Sum(t => t.Nodes.Count(n => n.CustomEffectId == "core_slot")) ?? 0;
            TestBase.AssertEqual(8, slotNodes, "two Core slot nodes per core class", ref _run, ref _passed, ref _failed);

            var hero = TestDataBuilders.CreateTestCharacter("Slotter", 20);
            int before = ComboSequenceMaxHelper.GetEffectiveMax(hero);
            hero.Progression.LearnedSkillRanks["b-slot1"] = 1;
            int after = ComboSequenceMaxHelper.GetEffectiveMax(hero);
            TestBase.AssertTrue(after == before + 1 || after == (GameConfiguration.Instance.LootSystem?.ComboSequenceAbsoluteMax ?? 8),
                $"a learned Core slot node adds a strip slot ({before} -> {after})", ref _run, ref _passed, ref _failed);

            var boneNode = trees?.Trees.SelectMany(t => t.Nodes).FirstOrDefault(n => n.Branch == "Bone");
            TestBase.AssertTrue(boneNode != null && !SkillTreeService.MaterialBranchOpen(hero, boneNode),
                "a material branch stays shut without 2 pieces of its material", ref _run, ref _passed, ref _failed);
            var coreNode = trees?.Trees.SelectMany(t => t.Nodes).FirstOrDefault(n => n.Branch == "Core");
            TestBase.AssertTrue(SkillTreeService.MaterialBranchOpen(hero, coreNode), "Core nodes are always open", ref _run, ref _passed, ref _failed);

            TestBase.PrintSummary("Rebuild Step 9 Tests", _run, _passed, _failed);
        }
    }
}
