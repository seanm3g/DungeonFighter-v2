using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Data;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.World
{
    /// <summary>EnemyPrefixes.json: data shape, eligibility rules, room roll and the final boss's Demon prefix.</summary>
    public static class EnemyPrefixTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Enemy Prefix Tests ===\n");
            _run = _passed = _failed = 0;
            try
            {
                EnemyPrefixLoader.SetForTests(null);
                TestDataFileHasDemonBossAndIdeas();
                EnemyPrefixLoader.SetForTests(SyntheticFile(chance: 1.0));
                TestEligibilityRules();
                TestRoomRollRenamesAndTagsApart();
                TestBossPrefixReplacesRoomPrefix();
                EnemyPrefixLoader.SetForTests(SyntheticFile(chance: 0.0));
                TestZeroChanceRollsNothing();
            }
            finally
            {
                EnemyPrefixLoader.SetForTests(null);
            }
            TestBase.PrintSummary("Enemy Prefix Tests", _run, _passed, _failed);
        }

        private static EnemyPrefixFile SyntheticFile(double chance) => new()
        {
            RoomRoll = new EnemyPrefixRoomRoll { Chance = chance, MaxChance = 1.0 },
            BossPrefix = "Demon",
            Prefixes = new List<EnemyPrefixData>
            {
                new() { Name = "Demon", BossOnly = true, Tags = new() { "demon" } },
                new() { Name = "Undead", Weight = 1, RequiresTags = new() { "living" }, ExcludesTags = new() { "undead" }, Tags = new() { "undead" } },
                new() { Name = "Ancient", Weight = 1, MinLevel = 50, Tags = new() { "ancient" } }
            }
        };

        private static Enemy Make(string name, int level)
        {
            var enemy = EnemyLoader.CreateEnemy(name, level);
            if (enemy == null)
                throw new InvalidOperationException($"test enemy {name} missing from Enemies.json");
            return enemy;
        }

        private static void TestDataFileHasDemonBossAndIdeas()
        {
            TestBase.SetCurrentTestName(nameof(TestDataFileHasDemonBossAndIdeas));
            var file = EnemyPrefixLoader.Get();
            TestBase.AssertEqual("Demon", file.BossPrefix, "boss prefix is Demon", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(EnemyPrefixLoader.Find("Demon")?.BossOnly == true, "Demon is boss-only", ref _run, ref _passed, ref _failed);
            foreach (var name in new[] { "Undead", "Skeleton", "Ghost", "Giant", "Tiny" })
                TestBase.AssertNotNull(EnemyPrefixLoader.Find(name), $"{name} prefix exists", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(file.Prefixes.All(p => p.Tags.Count > 0 && !string.IsNullOrWhiteSpace(p.Idea)),
                "every prefix has tags and an idea", ref _run, ref _passed, ref _failed);
        }

        private static void TestEligibilityRules()
        {
            TestBase.SetCurrentTestName(nameof(TestEligibilityRules));
            var prefixes = EnemyPrefixLoader.Get().Prefixes;
            var wolf = Make("Wolf", 10);
            var names = EnemyPrefixRoller.Eligible(wolf, prefixes).Select(p => p.Name).ToList();
            TestBase.AssertTrue(names.Contains("Undead"), "living wolf can turn Undead", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!names.Contains("Demon"), "boss-only Demon never rolls for rooms", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!names.Contains("Ancient"), "Ancient needs level 50", ref _run, ref _passed, ref _failed);
            var skeleton = Make("Skeleton", 10);
            TestBase.AssertTrue(!EnemyPrefixRoller.Eligible(skeleton, prefixes).Any(p => p.Name == "Undead"),
                "an undead enemy cannot turn Undead", ref _run, ref _passed, ref _failed);
        }

        private static void TestRoomRollRenamesAndTagsApart()
        {
            TestBase.SetCurrentTestName(nameof(TestRoomRollRenamesAndTagsApart));
            var wolf = Make("Wolf", 10);
            int ownTags = wolf.Tags.Count;
            var applied = EnemyPrefixRoller.TryApplyRoomPrefix(wolf, new Random(1));
            TestBase.AssertEqual("Undead", applied?.Name, "only eligible prefix is applied", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("Undead Wolf", wolf.Name, "name carries the prefix", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("Wolf", wolf.BaseName, "base name kept", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(wolf.PrefixTags.Contains("undead"), "prefix tag stored", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(ownTags, wolf.Tags.Count, "enemy's own tags untouched (no mechanics yet)", ref _run, ref _passed, ref _failed);
            TestBase.AssertNull(EnemyPrefixRoller.TryApplyRoomPrefix(wolf, new Random(2)), "never re-prefixed", ref _run, ref _passed, ref _failed);
        }

        private static void TestBossPrefixReplacesRoomPrefix()
        {
            TestBase.SetCurrentTestName(nameof(TestBossPrefixReplacesRoomPrefix));
            var wolf = Make("Wolf", 10);
            EnemyPrefixRoller.TryApplyRoomPrefix(wolf, new Random(1));
            EnemyPrefixRoller.ApplyBossPrefix(wolf);
            TestBase.AssertEqual("Demon Wolf", wolf.Name, "boss name", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("Demon", wolf.Prefix, "boss prefix replaces room prefix", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(wolf.PrefixTags.SequenceEqual(new[] { "demon" }), "boss tags replace room tags", ref _run, ref _passed, ref _failed);
        }

        private static void TestZeroChanceRollsNothing()
        {
            TestBase.SetCurrentTestName(nameof(TestZeroChanceRollsNothing));
            var wolf = Make("Wolf", 10);
            TestBase.AssertNull(EnemyPrefixRoller.TryApplyRoomPrefix(wolf, new Random(1)), "0% chance: no prefix", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("Wolf", wolf.Name, "name unchanged", ref _run, ref _passed, ref _failed);
        }
    }
}
