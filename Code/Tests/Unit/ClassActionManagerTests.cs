using System;
using System.Linq;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// ClassActionManager unlocks: skill-tree Action nodes when SkillTrees.json is loaded.
    /// </summary>
    public static class ClassActionManagerTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== ClassActionManager Tests ===\n");
            _testsRun = _testsPassed = _testsFailed = 0;

            TestSkillTreeUnlockAddsNamedAction();
            TestUnlearnedTreeActionNotGranted();
            TestNullProgressionHandling();
            TestEmptyTreesFallsBackWithoutCrash();

            TestBase.PrintSummary("ClassActionManager Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestSkillTreeUnlockAddsNamedAction()
        {
            Console.WriteLine("--- Skill-tree unlock adds named action ---");
            var trees = SkillTreesConfig.TryLoadFromGameDataFile() ?? SkillTreesConfig.CreateEmpty();
            var backup = GameConfiguration.Instance.SkillTrees;
            try
            {
                GameConfiguration.Instance.SkillTrees = trees;
                var manager = new ClassActionManager();
                var character = TestDataBuilders.Character().WithName("TreeUnlock").Build();
                var progression = character.Progression;
                progression.BarbarianPoints = 20;
                progression.EnsureSkillTreeRootsGranted();
                progression.TryLearnSkillNode("b-might", requirePrimaryPath: false);

                manager.AddClassActions(character, progression, WeaponType.Mace);
                bool has = character.ActionPool.Any(a =>
                    string.Equals(a.action.Name, "MIGHT", StringComparison.OrdinalIgnoreCase));
                TestBase.AssertTrue(has, "Skill-tree MIGHT unlock appears in pool",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                GameConfiguration.Instance.SkillTrees = backup;
            }
        }

        private static void TestUnlearnedTreeActionNotGranted()
        {
            Console.WriteLine("--- Unlearned tree actions are not granted ---");
            var trees = SkillTreesConfig.TryLoadFromGameDataFile() ?? SkillTreesConfig.CreateEmpty();
            var backup = GameConfiguration.Instance.SkillTrees;
            try
            {
                GameConfiguration.Instance.SkillTrees = trees;
                var manager = new ClassActionManager();
                var character = TestDataBuilders.Character().WithName("NoLearn").Build();
                character.Progression.BarbarianPoints = 50;
                character.Progression.EnsureSkillTreeRootsGranted();

                manager.AddClassActions(character, character.Progression, WeaponType.Mace);
                bool hasMight = character.ActionPool.Any(a =>
                    string.Equals(a.action.Name, "MIGHT", StringComparison.OrdinalIgnoreCase));
                TestBase.AssertTrue(!hasMight,
                    "MIGHT not granted until Action node learned",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                GameConfiguration.Instance.SkillTrees = backup;
            }
        }

        private static void TestNullProgressionHandling()
        {
            Console.WriteLine("--- Null progression handling ---");
            var manager = new ClassActionManager();
            var character = TestDataBuilders.Character().WithName("NullProg").Build();
            try
            {
                manager.AddClassActions(character, null, null);
                TestBase.AssertTrue(true, "Should handle null progression without exception",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false, $"Null progression threw: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestEmptyTreesFallsBackWithoutCrash()
        {
            Console.WriteLine("--- Empty trees fall back without crash ---");
            var backup = GameConfiguration.Instance.SkillTrees;
            try
            {
                GameConfiguration.Instance.SkillTrees = SkillTreesConfig.CreateEmpty();
                var manager = new ClassActionManager();
                var character = TestDataBuilders.Character().WithName("LegacyFallback").Build();
                character.Progression.WarriorPoints = 5;
                manager.AddClassActions(character, character.Progression, WeaponType.Sword);
                TestBase.AssertTrue(true, "Empty SkillTrees falls back to ClassActions rules safely",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false, $"Fallback threw: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                GameConfiguration.Instance.SkillTrees = backup;
            }
        }
    }
}
