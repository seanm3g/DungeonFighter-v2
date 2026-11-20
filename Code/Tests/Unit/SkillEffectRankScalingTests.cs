using System.Linq;
using System.Collections.Generic;
using System;
using RPGGame.Combat.Events;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// maxRank-5 skill nodes scale their combat bonuses by learned rank x the node's <c>power</c> from SkillTrees.json
    /// (rule nodes since 2026-11-16: <see cref="CrossWires"/> scales a node's rule amounts by rank x power).
    /// Puberty adds a flat 10 x power to Strength. Commission / Initiation / Apprenticeship add 10 x power to AGI / TEC / INT.
    /// </summary>
    public static class SkillEffectRankScalingTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== SkillEffectRankScaling Tests ===\n");
            _testsRun = _testsPassed = _testsFailed = 0;
            TestBase.ClearBalancePacingForTests(); // strips built on fresh L1 heroes; a config Reload brings back the slot cap

            EnsureTreesLoaded();
            TestFirstBronzeAgeScalesWithRank();
            TestSwordHitAgiScalesWithRank();
            TestEmptyFuryScalesWithRank();
            TestScrapPreferScalesWithRank();
            TestPubertyGrantsStandingStrength();
            TestRitePassivesGrantStandingAttributes();

            TestBase.PrintSummary("SkillEffectRankScaling Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void EnsureTreesLoaded()
        {
            var trees = SkillTreesConfig.TryLoadFromGameDataFile();
            TestBase.AssertTrue(trees != null && trees.Trees.Count(t => t.Parents.Count == 0) == 4,
                "SkillTrees.json loads",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            if (trees != null)
                GameConfiguration.Instance.SkillTrees = trees;
        }

        private static Character MakeHero(string name, WeaponType weaponType)
        {
            var character = TestDataBuilders.Character().WithName(name).Build();
            character.Progression.BarbarianPoints = 50;
            character.Progression.WarriorPoints = 50;
            character.Progression.RoguePoints = 50;
            character.Progression.WizardPoints = 50;
            character.Progression.EnsureSkillTreeRootsGranted();

            var weapon = TestDataBuilders.Weapon()
                .WithName(name + "Weapon")
                .WithWeaponType(weaponType)
                .Build();
            if (weaponType == WeaponType.Mace)
            {
                weapon.Material = "Bone";
                weapon.Tags.Add("bone");
            }
            else if (weaponType == WeaponType.Sword)
            {
                weapon.Material = "Bronze";
                weapon.Tags.Add("bronze");
            }
            character.Equipment.Weapon = weapon;
            return character;
        }

        private static double Power(string nodeId) => GameConfiguration.Instance.SkillTrees.GetNode(nodeId)?.Power ?? 1.0;

        private static void SetRank(Character character, string nodeId, int rank)
        {
            character.Progression.LearnedSkillRanks[nodeId] = rank;
        }

        private static void PublishHit(Character hero)
        {
            SkillEffectRouter.Instance.ResetFightState();
            hero.Effects.ConsumedDamageModPercent = 0;
            hero.Effects.ConsumedAmpModPercent = 0;
            hero.Effects.ConsumedSkillAmpPercent = 0;
            hero.Stats.TempStrengthBonus = 0;
            hero.Stats.TempAgilityBonus = 0;
            hero.Stats.TempTechniqueBonus = 0;
            hero.Stats.TempIntelligenceBonus = 0;
            MaterialSetController.TryMintFromEvent(hero, new CombatEvent(CombatEventType.ActionHit, hero)
            {
                Action = new Action { Name = "TEST_SWING", Type = ActionType.Attack },
                Damage = 10
            });
        }

        private static void TestFirstBronzeAgeScalesWithRank()
        {
            // 2026-11-18: First Steel Age is a rank-1 rule (Mace actions count as Barbarian); rank scaling is checked on
            // Steady March (WHILE IF WEAPON sword -> AGI 1, 5 ranks).
            Console.WriteLine("--- First Steel Age tags Mace actions Barbarian; Steady March scales with rank ---");
            var swing = new Action { Name = "TEST_SWING", Type = ActionType.Attack };
            var mace = MakeHero("AgeTag", WeaponType.Mace);
            TestBase.AssertFalse(CrossWires.HasTag(mace, swing, "barbarian"), "unlearned: a Mace swing is not Barbarian",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            SetRank(mace, "b-age1", 1);
            TestBase.AssertTrue(CrossWires.HasTag(mace, swing, "barbarian"), "First Steel Age: a Mace swing counts as Barbarian",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            double Agi(int rank, string name)
            {
                var hero = MakeHero(name, WeaponType.Sword);
                double before = CrossWires.Passive(hero, "AGI");
                SetRank(hero, "w-march", rank);
                return CrossWires.Passive(hero, "AGI") - before;
            }
            double a1 = Agi(1, "March1"), a3 = Agi(3, "March3");
            TestBase.AssertTrue(Math.Abs(a1 - 1.0 * Power("w-march")) < 1e-6, "rank 1: 1 x power AGI",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(Math.Abs(a3 - a1 * 3) < 1e-6, "rank 3 triples it",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestSwordHitAgiScalesWithRank()
        {
            Console.WriteLine("--- Loaded Odds (rule ONCONNECT IF ROLL>=15 -> NEXT INT 1 FOR 2) scales with rank ---");
            double Amount(Character hero) => CrossWires.LearnedNodeRules(hero)
                .Where(x => x.Node.Id == "z-odds").Select(x => x.Rule.Amount).DefaultIfEmpty(0).Sum();
            var r1 = MakeHero("OddsRank1", WeaponType.Wand);
            SetRank(r1, "z-odds", 1);
            var r2 = MakeHero("OddsRank2", WeaponType.Wand);
            SetRank(r2, "z-odds", 2);
            TestBase.AssertTrue(Math.Abs(Amount(r1) - 1 * Power("z-odds")) < 1e-6, "rank 1 rule grants 1 x power INT",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(Math.Abs(Amount(r2) - 2 * Power("z-odds")) < 1e-6, "rank 2 rule grants 2 x power INT",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestEmptyFuryScalesWithRank()
        {
            Console.WriteLine("--- Empty Fury mints RAGE per extra slot; Concentrated Chaos (WHILE -> AMP 7% PER EXTRASLOTS) reads filled slots ---");
            var fury = MakeHero("Fury1", WeaponType.Mace);
            SetRank(fury, "b-empty", 1);
            var rule = CrossWires.LearnedNodeRules(fury).Where(x => x.Node.Id == "b-empty").Select(x => x.Rule).FirstOrDefault();
            TestBase.AssertTrue(rule != null && rule.When == "ONCOMBOEND" && rule.Verb == "MINT" && rule.Store == "RAGE",
                "Empty Fury: a combo end mints RAGE", ref _testsRun, ref _testsPassed, ref _testsFailed);

            var r1 = MakeHero("Chaos1", WeaponType.Wand);
            int filled = FillStrip(r1);
            double before = CrossWires.Passive(r1, "AMP");
            SetRank(r1, "z-chaos", 1);
            double amp = CrossWires.Passive(r1, "AMP") - before;
            var bare = MakeHero("Chaos0", WeaponType.Wand);
            while (bare.GetComboActions().Count > 1)
                bare.RemoveFromCombo(bare.GetComboActions()[^1]);
            double bareBefore = CrossWires.Passive(bare, "AMP");
            SetRank(bare, "z-chaos", 1);

            TestBase.AssertTrue(filled >= 2, $"test strip holds 2+ actions (got {filled})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(Math.Abs(amp - 7.0 * (filled - 1) * Power("z-chaos")) < 1e-6,
                $"Concentrated Chaos: 7% x extra slots x power (got {amp})", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(Math.Abs(CrossWires.Passive(bare, "AMP") - bareBefore) < 1e-6, "a one-action strip grants no Chaos AMP",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>Raises the hero so the strip has room, then puts three catalog actions in it.</summary>
        private static int FillStrip(Character hero)
        {
            hero.Level = 40;
            foreach (string name in new[] { "SLAM", "BLUDGEON", "STRIKE" })
            {
                var action = ActionLoader.GetAction(name);
                if (action != null)
                    hero.AddToCombo(action);
            }
            return hero.GetComboActions().Count;
        }

        private static void TestScrapPreferScalesWithRank()
        {
            Console.WriteLine("--- Scrap Prefer (rule ONCONNECT EVERY 4 -> APPLY stagger 1 PER QUALITY ...) ---");
            var hero = MakeHero("Scrap1", WeaponType.Mace);
            SetRank(hero, "b-scar", 1);
            var rule = CrossWires.LearnedNodeRules(hero).Where(x => x.Node.Id == "b-scar").Select(x => x.Rule).FirstOrDefault();
            TestBase.AssertTrue(rule != null && rule.Verb == "APPLY" && rule.Store == "stagger" && rule.Every == 4,
                "Scrap Prefer applies Stagger every 4th connect", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1, GameConfiguration.Instance.SkillTrees.GetNode("b-scar")?.MaxRank ?? 0, "rank 1 node",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestPubertyGrantsStandingStrength()
        {
            Console.WriteLine("--- Puberty grants a flat 10 x power standing STR ---");
            var hero = MakeHero("PubertyStr", WeaponType.Mace);
            hero.Level = 20;
            int before = hero.GetEffectiveStrength();
            TestBase.AssertEqual(0, SkillEffectRouter.Instance.GetSkillAttributeBonus(hero, "STR"),
                "no Puberty rank -> no skill STR",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            SetRank(hero, "b-puberty", 1);
            int flat = (int)Math.Round(10 * Power("b-puberty"));
            int after = hero.GetEffectiveStrength();
            TestBase.AssertTrue(flat > 0, "Puberty grants STR",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(flat, SkillEffectRouter.Instance.GetSkillAttributeBonus(hero, "STR"),
                "Puberty is 10 x power STR",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0, SkillEffectRouter.Instance.GetSkillAttributeBonus(hero, "AGI"),
                "Puberty does not add Agility",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(before + flat, after,
                "GetEffectiveStrength includes Puberty",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(hero.MeetsStatThreshold("STR", before + flat),
                "STR threshold uses Puberty",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            hero.Level = 40;
            TestBase.AssertEqual(flat, SkillEffectRouter.Instance.GetSkillAttributeBonus(hero, "STR"),
                "Puberty does not scale with level",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRitePassivesGrantStandingAttributes()
        {
            Console.WriteLine("--- Sister rite passives grant standing attributes ---");
            var warrior = MakeHero("CommissionAgi", WeaponType.Sword);
            int agiBefore = warrior.GetEffectiveAgility();
            SetRank(warrior, "w-puberty", 1);
            TestBase.AssertEqual(agiBefore + (int)Math.Round(10 * Power("w-puberty")), warrior.GetEffectiveAgility(),
                "Commission 10 x power AGI",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var rogue = MakeHero("InitiationTec", WeaponType.Dagger);
            int tecBefore = rogue.GetEffectiveTechnique();
            SetRank(rogue, "r-puberty", 1);
            TestBase.AssertEqual(tecBefore + (int)Math.Round(10 * Power("r-puberty")), rogue.GetEffectiveTechnique(),
                "Initiation 10 x power TEC",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var wizard = MakeHero("ApprenticeInt", WeaponType.Wand);
            int intBefore = wizard.GetEffectiveIntelligence();
            SetRank(wizard, "z-puberty", 1);
            TestBase.AssertEqual(intBefore + (int)Math.Round(10 * Power("z-puberty")), wizard.GetEffectiveIntelligence(),
                "Apprenticeship 10 x power INT",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
