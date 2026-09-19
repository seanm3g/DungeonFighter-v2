using System;
using RPGGame.Combat.Events;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// maxRank-5 skill nodes scale their combat bonuses by learned rank.
    /// Standing rite passives (Puberty / Commission / Initiation / Apprenticeship) add +15 to the class stat.
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
            TestBase.AssertTrue(trees != null && trees.Trees.Count == 4,
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

        private static void SetRank(Character character, string nodeId, int rank)
        {
            character.Progression.LearnedSkillRanks[nodeId] = rank;
        }

        private static void PublishHit(Character hero)
        {
            SkillEffectRouter.Instance.RefreshForCharacter(hero);
            SkillEffectRouter.Instance.ResetFightState();
            hero.Effects.ConsumedDamageModPercent = 0;
            hero.Effects.ConsumedAmpModPercent = 0;
            hero.Stats.TempStrengthBonus = 0;
            hero.Stats.TempAgilityBonus = 0;
            hero.Stats.TempTechniqueBonus = 0;
            hero.Stats.TempIntelligenceBonus = 0;
            CombatEventBus.Instance.Publish(new CombatEvent(CombatEventType.ActionHit, hero)
            {
                Action = new Action { Name = "TEST_SWING", Type = ActionType.Attack },
                Damage = 10
            });
        }

        private static void TestFirstBronzeAgeScalesWithRank()
        {
            Console.WriteLine("--- First Bronze Age scales with rank ---");
            var r1 = MakeHero("AgeRank1", WeaponType.Mace);
            SetRank(r1, "b-age1", 1);
            PublishHit(r1);
            double dmg1 = r1.Effects.ConsumedDamageModPercent;

            var r3 = MakeHero("AgeRank3", WeaponType.Mace);
            SetRank(r3, "b-age1", 3);
            PublishHit(r3);
            double dmg3 = r3.Effects.ConsumedDamageModPercent;

            TestBase.AssertTrue(dmg1 > 0, "rank 1 applies damage mod",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(dmg1 * 3, dmg3, "rank 3 triples age1 damage mod",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestSwordHitAgiScalesWithRank()
        {
            Console.WriteLine("--- Sword Hit AGI scales with rank ---");
            var r1 = MakeHero("AgiRank1", WeaponType.Sword);
            SetRank(r1, "w-hitagi", 1);
            PublishHit(r1);
            int agi1 = r1.Stats.TempAgilityBonus;

            var r2 = MakeHero("AgiRank2", WeaponType.Sword);
            SetRank(r2, "w-hitagi", 2);
            PublishHit(r2);
            int agi2 = r2.Stats.TempAgilityBonus;

            TestBase.AssertEqual(5, agi1, "rank 1 grants +5 AGI",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(10, agi2, "rank 2 grants +10 AGI",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestEmptyFuryScalesWithRank()
        {
            Console.WriteLine("--- Empty Fury scales with rank ---");
            var r1 = MakeHero("Fury1", WeaponType.Mace);
            SetRank(r1, "b-empty", 1);
            while (r1.GetComboActions().Count > 0)
                r1.RemoveFromCombo(r1.GetComboActions()[^1]);
            PublishHit(r1);
            int str1 = r1.Stats.TempStrengthBonus;

            var r2 = MakeHero("Fury2", WeaponType.Mace);
            SetRank(r2, "b-empty", 2);
            while (r2.GetComboActions().Count > 0)
                r2.RemoveFromCombo(r2.GetComboActions()[^1]);
            PublishHit(r2);
            int str2 = r2.Stats.TempStrengthBonus;

            TestBase.AssertTrue(str1 > 0, "rank 1 Empty Fury grants STR",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(str1 * 2, str2, "rank 2 doubles Empty Fury STR",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestScrapPreferScalesWithRank()
        {
            Console.WriteLine("--- Scrap Prefer scales with rank ---");
            var r1 = MakeHero("Scrap1", WeaponType.Mace);
            r1.Equipment.Weapon!.Modifications.Add(new Modification
            {
                Name = "Masterwork",
                PrefixCategory = "Quality"
            });
            SetRank(r1, "b-scar", 1);
            PublishHit(r1);
            double dmg1 = r1.Effects.ConsumedDamageModPercent;

            var r2 = MakeHero("Scrap2", WeaponType.Mace);
            r2.Equipment.Weapon!.Modifications.Add(new Modification
            {
                Name = "Masterwork",
                PrefixCategory = "Quality"
            });
            SetRank(r2, "b-scar", 2);
            PublishHit(r2);
            double dmg2 = r2.Effects.ConsumedDamageModPercent;

            TestBase.AssertEqual(10.0, dmg1, "rank 1 Scrap Prefer +10% damage",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(20.0, dmg2, "rank 2 Scrap Prefer +20% damage",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestPubertyGrantsStandingStrength()
        {
            Console.WriteLine("--- Puberty grants standing +15 STR ---");
            var hero = MakeHero("PubertyStr", WeaponType.Mace);
            int before = hero.GetEffectiveStrength();
            TestBase.AssertEqual(0, SkillEffectRouter.Instance.GetSkillAttributeBonus(hero, "STR"),
                "no Puberty rank -> no skill STR",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            SetRank(hero, "b-puberty", 1);
            int after = hero.GetEffectiveStrength();
            TestBase.AssertEqual(15, SkillEffectRouter.Instance.GetSkillAttributeBonus(hero, "STR"),
                "Puberty rank 1 is +15 STR",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(before + 15, after,
                "GetEffectiveStrength includes Puberty",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(hero.MeetsStatThreshold("STR", before + 15),
                "STR threshold uses Puberty",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRitePassivesGrantStandingAttributes()
        {
            Console.WriteLine("--- Sister rite passives grant standing attributes ---");
            var warrior = MakeHero("CommissionAgi", WeaponType.Sword);
            int agiBefore = warrior.GetEffectiveAgility();
            SetRank(warrior, "w-puberty", 1);
            TestBase.AssertEqual(agiBefore + 15, warrior.GetEffectiveAgility(),
                "Commission +15 AGI",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var rogue = MakeHero("InitiationTec", WeaponType.Dagger);
            int tecBefore = rogue.GetEffectiveTechnique();
            SetRank(rogue, "r-puberty", 1);
            TestBase.AssertEqual(tecBefore + 15, rogue.GetEffectiveTechnique(),
                "Initiation +15 TEC",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var wizard = MakeHero("ApprenticeInt", WeaponType.Wand);
            int intBefore = wizard.GetEffectiveIntelligence();
            SetRank(wizard, "z-puberty", 1);
            TestBase.AssertEqual(intBefore + 15, wizard.GetEffectiveIntelligence(),
                "Apprenticeship +15 INT",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
