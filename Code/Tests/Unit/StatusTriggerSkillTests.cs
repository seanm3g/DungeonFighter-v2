using System;
using System.Collections.Generic;
using RPGGame.Combat;
using RPGGame.Combat.Events;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Status-applied triggers: <see cref="StatusApplySnapshot"/> sees new and stacked debuffs and publishes
    /// StatusEffectApplied; Blood Frenzy / Pinning Drill / Toxic Insight / Kindled Mind grant class stats for the rest of the dungeon.
    /// Expected values read node power from SkillTrees.json.
    /// </summary>
    public static class StatusTriggerSkillTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== StatusTriggerSkill Tests ===\n");
            _run = _passed = _failed = 0;
            var trees = SkillTreesConfig.TryLoadFromGameDataFile();
            if (trees != null)
                GameConfiguration.Instance.SkillTrees = trees;

            TestSnapshotSeesNewAndStackedStatuses();
            TestSnapshotPublishesOnePerStatus();
            TestNodesExistInTrees();
            TestEachNodeGrantsItsStat();
            TestWrongStatusOrSourceGrantsNothing();
            TestFractionsCarryAndResetPerFight();

            TestBase.PrintSummary("StatusTriggerSkill Tests", _run, _passed, _failed);
        }

        private static double Power(string id) => GameConfiguration.Instance.SkillTrees.GetNode(id)?.Power ?? 1.0;

        private static Character MakeHero(string name)
        {
            var hero = TestDataBuilders.Character().WithName(name).Build();
            hero.Progression.EnsureSkillTreeRootsGranted();
            SkillEffectRouter.Instance.RefreshForCharacter(hero);
            SkillEffectRouter.Instance.ResetFightState();
            hero.FightCadenceBuffs.Clear();
            hero.DungeonCadenceBuffs.Clear();
            return hero;
        }

        private static void Apply(Actor source, Actor target, string status) =>
            CombatEventBus.Instance.Publish(new CombatEvent(CombatEventType.StatusEffectApplied, source)
            {
                Target = target,
                StatusEffectType = status
            });

        private static void TestSnapshotSeesNewAndStackedStatuses()
        {
            TestBase.SetCurrentTestName(nameof(TestSnapshotSeesNewAndStackedStatuses));
            var enemy = MockFactories.CreateMockEnemy("Dummy");
            var snap = StatusApplySnapshot.Take(enemy);
            TestBase.AssertEqual(0, snap.Gained(enemy).Count, "nothing applied → nothing gained", ref _run, ref _passed, ref _failed);
            enemy.BleedIntensity = 2;
            enemy.IsStunned = true;
            enemy.StunTurnsRemaining = 1;
            var gained = snap.Gained(enemy);
            TestBase.AssertTrue(gained.Contains("bleed") && gained.Contains("stun") && gained.Count == 2,
                "new Bleed and Stun are seen", ref _run, ref _passed, ref _failed);
            var stacked = StatusApplySnapshot.Take(enemy);
            enemy.BleedIntensity = 3;
            TestBase.AssertTrue(stacked.Gained(enemy).Contains("bleed"), "a higher Bleed stack counts as applied",
                ref _run, ref _passed, ref _failed);
            var queued = StatusApplySnapshot.Take(enemy);
            enemy.PendingBleedFromHits += 1;
            TestBase.AssertTrue(queued.Gained(enemy).Contains("bleed"), "Bleed queued from a hit counts as applied",
                ref _run, ref _passed, ref _failed);
            var slowed = StatusApplySnapshot.Take(enemy);
            enemy.Effects.SlowTurns = 2;
            enemy.Effects.SlowMultiplier = 0.8;
            TestBase.AssertTrue(slowed.Gained(enemy).Contains("slow"), "Slow on an enemy is seen", ref _run, ref _passed, ref _failed);
        }

        private static void TestSnapshotPublishesOnePerStatus()
        {
            TestBase.SetCurrentTestName(nameof(TestSnapshotPublishesOnePerStatus));
            var hero = MakeHero("Pub");
            var enemy = MockFactories.CreateMockEnemy("Dummy");
            var seen = new List<string>();
            void Handler(CombatEvent e) { if (ReferenceEquals(e.Source, hero)) seen.Add(e.StatusEffectType ?? ""); }
            CombatEventBus.Instance.Subscribe(CombatEventType.StatusEffectApplied, Handler);
            try
            {
                var snap = StatusApplySnapshot.Take(enemy);
                enemy.PoisonPercentOfMaxHealth = 2;
                enemy.ExposeStacks = 1;
                snap.PublishGained(hero, enemy, null);
                TestBase.AssertTrue(seen.Count == 2 && seen.Contains("poison") && seen.Contains("expose"),
                    "one StatusEffectApplied per gained status", ref _run, ref _passed, ref _failed);
                seen.Clear();
                var self = StatusApplySnapshot.Take(hero);
                hero.BleedIntensity = 1;
                self.PublishGained(hero, hero, null);
                TestBase.AssertEqual(0, seen.Count, "statuses on yourself publish nothing", ref _run, ref _passed, ref _failed);
                hero.BleedIntensity = 0;
            }
            finally
            {
                CombatEventBus.Instance.Unsubscribe(CombatEventType.StatusEffectApplied, Handler);
            }
        }

        private static void TestNodesExistInTrees()
        {
            TestBase.SetCurrentTestName(nameof(TestNodesExistInTrees));
            foreach (var (id, effect) in new[] { ("b-frenzy", "blood_frenzy"), ("w-pin", "pinning_drill"), ("r-insight", "toxic_insight"), ("z-kindle", "kindled_mind") })
            {
                var node = GameConfiguration.Instance.SkillTrees.GetNode(id);
                TestBase.AssertTrue(node != null && node.CustomEffectId == effect && node.MaxRank == 5,
                    $"{id} is in its tree with effect {effect}", ref _run, ref _passed, ref _failed);
            }
        }

        private static void TestEachNodeGrantsItsStat()
        {
            TestBase.SetCurrentTestName(nameof(TestEachNodeGrantsItsStat));
            var cases = new (string Node, string Status, string Code, double PerRank)[]
            {
                ("b-frenzy", "stun", "STR", 6),
                ("w-pin", "slow", "AGI", 8),
                ("r-insight", "poison", "TECH", 1),
                ("z-kindle", "burn", "INT", 1),
            };
            foreach (var (node, status, code, perRank) in cases)
            {
                var hero = MakeHero("Grant" + node);
                var enemy = MockFactories.CreateMockEnemy("Dummy");
                hero.Progression.LearnedSkillRanks[node] = 5;
                Apply(hero, enemy, status);
                Apply(hero, enemy, status);
                int expected = (int)Math.Floor(2 * perRank * 5 * Power(node) + 1e-9);
                TestBase.AssertEqual(expected, hero.DungeonCadenceBuffs.GetStatBonus(code),
                    $"{node}: two {status} applications at rank 5 grant {code}", ref _run, ref _passed, ref _failed);
            }
        }

        private static void TestWrongStatusOrSourceGrantsNothing()
        {
            TestBase.SetCurrentTestName(nameof(TestWrongStatusOrSourceGrantsNothing));
            var hero = MakeHero("Wrong");
            var enemy = MockFactories.CreateMockEnemy("Dummy");
            hero.Progression.LearnedSkillRanks["b-frenzy"] = 5;
            Apply(hero, enemy, "burn");
            TestBase.AssertEqual(0, hero.DungeonCadenceBuffs.GetStatBonus("STR"), "Blood Frenzy ignores Burn", ref _run, ref _passed, ref _failed);
            Apply(enemy, hero, "bleed");
            TestBase.AssertEqual(0, hero.DungeonCadenceBuffs.GetStatBonus("STR"), "an enemy bleeding you grants nothing", ref _run, ref _passed, ref _failed);
            var unlearned = MakeHero("Unlearned");
            Apply(unlearned, enemy, "bleed");
            TestBase.AssertEqual(0, unlearned.DungeonCadenceBuffs.GetStatBonus("STR"), "no node, no STR", ref _run, ref _passed, ref _failed);
        }

        private static void TestFractionsCarryAndResetPerFight()
        {
            TestBase.SetCurrentTestName(nameof(TestFractionsCarryAndResetPerFight));
            var hero = MakeHero("Carry");
            var enemy = MockFactories.CreateMockEnemy("Dummy");
            hero.Progression.LearnedSkillRanks["r-insight"] = 1;
            double per = 1 * Power("r-insight");
            int n = per < 1 ? (int)Math.Ceiling(1 / per) + 1 : 3;
            for (int i = 0; i < n; i++) Apply(hero, enemy, "expose");
            TestBase.AssertEqual((int)Math.Floor(n * per + 1e-9), hero.DungeonCadenceBuffs.GetStatBonus("TECH"),
                "Toxic Insight: fractions add up across applications", ref _run, ref _passed, ref _failed);
            int beforeFight = hero.DungeonCadenceBuffs.GetStatBonus("TECH");
            SkillEffectRouter.Instance.ResetFightState();
            hero.FightCadenceBuffs.Clear();
            Apply(hero, enemy, "expose");
            TestBase.AssertEqual(beforeFight + (int)Math.Floor(per + 1e-9), hero.DungeonCadenceBuffs.GetStatBonus("TECH"),
                "TEC carries into the next fight; the leftover fraction does not", ref _run, ref _passed, ref _failed);
        }
    }
}
