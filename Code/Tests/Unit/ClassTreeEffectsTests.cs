using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Combat;
using RPGGame.Combat.Events;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Level-100 class-tree effects: tier gate, Kinship, Synthesis feed, Titan, Perfect Aim, Archmage, Arcane Weave,
    /// convert masteries and Wand Mastery. Expected values read node power from SkillTrees.json.
    /// </summary>
    public static class ClassTreeEffectsTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== ClassTreeEffects Tests ===\n");
            _testsRun = _testsPassed = _testsFailed = 0;

            var trees = SkillTreesConfig.TryLoadFromGameDataFile();
            if (trees != null)
                GameConfiguration.Instance.SkillTrees = trees;

            TestTierGate();
            TestKinship();
            TestSynthesisFeed();
            TestTitanRaisesStrengthCap();
            TestPerfectAimAndArchmage();
            TestArcaneWeave();
            TestBoneWrathHeals();
            TestHookRules();

            TestBase.PrintSummary("ClassTreeEffects Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static double Power(string id) => GameConfiguration.Instance.SkillTrees.GetNode(id)?.Power ?? 1.0;

        private static Character MakeHero(string name, WeaponType weaponType, int points)
        {
            var hero = TestDataBuilders.Character().WithName(name).Build();
            switch (weaponType)
            {
                case WeaponType.Mace: hero.Progression.BarbarianPoints = points; break;
                case WeaponType.Sword: hero.Progression.WarriorPoints = points; break;
                case WeaponType.Dagger: hero.Progression.RoguePoints = points; break;
                case WeaponType.Wand: hero.Progression.WizardPoints = points; break;
            }
            hero.Progression.EnsureSkillTreeRootsGranted();
            hero.Equipment.Weapon = TestDataBuilders.Weapon().WithName(name + "Weapon").WithWeaponType(weaponType).Build();
            return hero;
        }

        private static void TestTierGate()
        {
            var node = GameConfiguration.Instance.SkillTrees.GetNode("b-titan")!;
            var tree = SkillTreePrerequisites.FindOwnerTree(node)!;
            var low = new CharacterProgression { BarbarianPoints = SkillTreesConfig.TierPointGate(node.Tier) - 1 };
            var high = new CharacterProgression { BarbarianPoints = SkillTreesConfig.TierPointGate(node.Tier) };
            TestBase.AssertTrue(!SkillTreePrerequisites.IsTierOpen(low, tree, node), "capstone tier closed below its point gate",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(SkillTreePrerequisites.IsTierOpen(high, tree, node), "capstone tier opens at its point gate",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestKinship()
        {
            var hero = MakeHero("Kin", WeaponType.Mace, 50);
            hero.Weapon!.Material = "Bone";
            hero.Progression.LearnedSkillRanks["b-kin"] = 1;
            var rule = CrossWires.LearnedNodeRules(hero).Where(x => x.Node.Id == "b-kin").Select(x => x.Rule).FirstOrDefault();
            TestBase.AssertTrue(rule != null && rule.When == "ONFIRSTHIT" && rule.Verb == "ADD" && rule.Store == "RAGE",
                "Tribal Kinship: the first hit each fight mints RAGE per Barbarian piece",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestSynthesisFeed()
        {
            var hero = MakeHero("Synth", WeaponType.Dagger, 50);
            TestBase.AssertEqual(0, CrossWires.MintBonus(hero, "GRAZE"), "no feed without Edge Synthesis",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            hero.Progression.LearnedSkillRanks["r-synth"] = 5;
            hero.Stats.Technique = 200;
            int tec = hero.GetEffectiveTechnique();
            int expected = (int)Math.Floor(Math.Floor(tec / 20.0) * 5 * Power("r-synth") + 1e-9);
            TestBase.AssertTrue(expected > 0, "test setup: enough TEC for a whole bonus unit",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(expected, CrossWires.MintBonus(hero, "GRAZE"),
                "Edge Synthesis (MINTBONUS rogue PER TEC 20) feeds Glass's GRAZE mints",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0, CrossWires.MintBonus(hero, "RAGE"), "Edge Synthesis does not feed Barbarian currencies",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestTitanRaisesStrengthCap()
        {
            var hero = MakeHero("Titan", WeaponType.Mace, 80);
            hero.Progression.LearnedSkillRanks["b-titan"] = 1;
            var rules = CrossWires.LearnedNodeRules(hero).Where(x => x.Node.Id == "b-titan").Select(x => x.Rule).ToList();
            TestBase.AssertTrue(rules.Any(r => r.Verb == "EXEMPT" && r.Store == "COMBO"),
                "Titan: misses against a Staggered enemy don't end the combo", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(rules.Any(r => r.When == "ONCRITICAL" && r.Verb == "APPLY" && r.Store == "stagger"),
                "Titan: crits apply Stagger", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestPerfectAimAndArchmage()
        {
            var rogue = MakeHero("Aim", WeaponType.Dagger, 80);
            double critBefore = CrossWires.Passive(rogue, "CRIT");
            rogue.Progression.LearnedSkillRanks["r-aim"] = 1;
            double expected = rogue.GetEffectiveTechnique() >= 100 ? 2 : 1;
            TestBase.AssertTrue(Math.Abs(CrossWires.Passive(rogue, "CRIT") - critBefore - expected) < 1e-6,
                "Perfect Aim: +1 crit range, +1 more at 100+ TEC",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(GameConfiguration.Instance.SkillTrees.GetNode("z-archmage") == null,
                "Archmage (a stat cap node) is cut (2026-11-18)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void Publish(Character hero, Action action, Actor? target)
        {
            SkillEffectRouter.Instance.ResetFightState();
            MaterialSetController.TryMintFromEvent(hero, new CombatEvent(CombatEventType.ActionHit, hero)
            {
                Target = target,
                Action = action,
                NaturalRollValue = 10,
                Damage = 10
            });
        }

        private static void TestArcaneWeave()
        {
            var hero = MakeHero("Weave", WeaponType.Wand, 50);
            hero.Progression.LearnedSkillRanks["z-weave"] = 1;
            var rule = CrossWires.LearnedNodeRules(hero).Where(x => x.Node.Id == "z-weave").Select(x => x.Rule).FirstOrDefault();
            TestBase.AssertTrue(rule != null && rule.Verb == "POTENCY" && rule.Store == "burn" && Math.Abs(rule.Amount - 25) < 1e-6,
                "Arcane Weave: with a Wand, Burn ticks 25% harder",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestBoneWrathHeals()
        {
            var hero = MakeHero("Wrath", WeaponType.Mace, 80);
            hero.Progression.LearnedSkillRanks["b-wrath"] = 1;
            // 2026-11-18: cashing pays damage, so Wrath Unbound is ONCASH:RAGE -> NEXT DAMAGE 3% PER SPENT (no heal).
            var rule = CrossWires.LearnedNodeRules(hero).Where(x => x.Node.Id == "b-wrath").Select(x => x.Rule).FirstOrDefault();
            TestBase.AssertTrue(rule != null && rule.When.StartsWith("ONCASH", StringComparison.OrdinalIgnoreCase) && rule.Verb == "NEXT" && rule.Store == "DAMAGE",
                "Wrath Unbound: spending RAGE adds damage to the next action",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>2026-11-23: the events and keys no rule used (ONSELFCOST, ONENEMYACT, ONEVEN, TURN, SLOTS, MISSINGHP, PER SELF).</summary>
        private static void TestHookRules()
        {
            var expect = new (string Node, int Rules, string Token)[]
            {
                ("b-blood", 3, "ONSELFCOST"), ("b-trance", 2, "MISSINGHP"), ("w-phalanx", 2, "ONENEMYACT"),
                ("w-cadence", 2, "TURN"), ("r-grease", 2, "ONEVEN"), ("z-slotInt", 2, "SLOTS"),
            };
            foreach (var (id, count, token) in expect)
            {
                var hero = MakeHero("Hook" + id, WeaponType.Mace, 100);
                hero.Progression.LearnedSkillRanks[id] = 1;
                int parsed = CrossWires.LearnedNodeRules(hero).Count(x => x.Node.Id == id);
                string text = GameConfiguration.Instance.SkillTrees.GetNode(id)?.CrossWire ?? "";
                TestBase.AssertTrue(parsed == count && text.Contains(token), $"{id}: all {count} rules parse ({token})",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            var phalanx = MakeHero("Phalanx", WeaponType.Sword, 100);
            phalanx.Progression.LearnedSkillRanks["w-phalanx"] = 5; // power 0.57: rank 1 rounds 0.57 Fortify at random
            int fortifyBefore = Stores.Get(phalanx, "fortify");
            for (int i = 0; i < 3; i++)
                HeroEventHub.Raise(phalanx, "ONENEMYACT");
            TestBase.AssertTrue(Stores.Get(phalanx, "fortify") > fortifyBefore, "Phalanx: every 3rd enemy action adds Fortify",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var trance = MakeHero("Trance", WeaponType.Mace, 100);
            trance.Progression.LearnedSkillRanks["b-trance"] = 1;
            double fullHp = CrossWires.Passive(trance, "DAMAGE");
            trance.CurrentHealth = trance.MaxHealth / 2;
            double halfHp = CrossWires.Passive(trance, "DAMAGE");
            TestBase.AssertTrue(halfHp - fullHp >= 4.5 * Power("b-trance"), $"Battle Trance: +1% damage per 10% HP missing ({fullHp} -> {halfHp})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var keeper = MakeHero("Keeper", WeaponType.Sword, 100);
            keeper.Progression.LearnedSkillRanks["w-cadence"] = 1;
            HeroEventHub.ResetFight(keeper);
            double early = CrossWires.Passive(keeper, "SPEED");
            for (int i = 0; i < 6; i++)
                HeroEventHub.OnHeroTurnStart(keeper);
            double late = CrossWires.Passive(keeper, "SPEED");
            TestBase.AssertTrue(late > early, $"Cadence Keeper: faster from turn 6 ({early} -> {late})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
