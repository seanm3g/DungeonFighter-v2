using System;
using RPGGame.Combat.Events;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// maxRank-5 skill nodes scale their combat bonuses by learned rank.
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
            TestFirstFormationScalesWithRank();
            TestFirstCutScalesWithRank();
            TestFirstGlyphScalesWithRank();
            TestSwordHitAgiScalesWithRank();
            TestEmptyFuryScalesWithRank();
            TestScrapPreferScalesWithRank();
            TestParadeDressScalesWithRank();

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
            else if (weaponType == WeaponType.Dagger)
            {
                weapon.Material = "Glass";
                weapon.Tags.Add("glass");
            }
            else if (weaponType == WeaponType.Wand)
            {
                weapon.Material = "Willow";
                weapon.Tags.Add("willow");
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
            hero.Effects.ConsumedSpeedModPercent = 0;
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

        private static void TestFirstFormationScalesWithRank()
        {
            Console.WriteLine("--- First Formation scales with rank ---");
            var r1 = MakeHero("Form1", WeaponType.Sword);
            SetRank(r1, "w-age1", 1);
            PublishHit(r1);
            double s1 = r1.Effects.ConsumedSpeedModPercent;

            var r3 = MakeHero("Form3", WeaponType.Sword);
            SetRank(r3, "w-age1", 3);
            PublishHit(r3);
            double s3 = r3.Effects.ConsumedSpeedModPercent;

            TestBase.AssertTrue(s1 > 0, "rank 1 formation speed",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(s1 * 3, s3, "rank 3 triples formation speed",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestFirstCutScalesWithRank()
        {
            Console.WriteLine("--- First Cut scales with rank ---");
            var r1 = MakeHero("Cut1", WeaponType.Dagger);
            if (r1.Equipment.Weapon != null)
            {
                r1.Equipment.Weapon.Material = "Glass";
                r1.Equipment.Weapon.Tags.Add("glass");
            }
            SetRank(r1, "r-age1", 1);
            PublishHit(r1);
            double d1 = r1.Effects.ConsumedDamageModPercent;

            var r2 = MakeHero("Cut2", WeaponType.Dagger);
            if (r2.Equipment.Weapon != null)
            {
                r2.Equipment.Weapon.Material = "Glass";
                r2.Equipment.Weapon.Tags.Add("glass");
            }
            SetRank(r2, "r-age1", 2);
            PublishHit(r2);
            double d2 = r2.Effects.ConsumedDamageModPercent;

            TestBase.AssertEqual(d1 * 2, d2, "rank 2 doubles first_cut damage",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestFirstGlyphScalesWithRank()
        {
            Console.WriteLine("--- First Glyph scales with rank ---");
            var r1 = MakeHero("Glyph1", WeaponType.Wand);
            if (r1.Equipment.Weapon != null)
            {
                r1.Equipment.Weapon.Material = "Willow";
                r1.Equipment.Weapon.Tags.Add("willow");
            }
            SetRank(r1, "z-age1", 1);
            PublishHit(r1);
            double a1 = r1.Effects.ConsumedAmpModPercent;

            var r2 = MakeHero("Glyph2", WeaponType.Wand);
            if (r2.Equipment.Weapon != null)
            {
                r2.Equipment.Weapon.Material = "Willow";
                r2.Equipment.Weapon.Tags.Add("willow");
            }
            SetRank(r2, "z-age1", 2);
            PublishHit(r2);
            double a2 = r2.Effects.ConsumedAmpModPercent;

            TestBase.AssertEqual(a1 * 2, a2, "rank 2 doubles first_glyph AMP",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestParadeDressScalesWithRank()
        {
            Console.WriteLine("--- Parade Dress scales with rank ---");
            var r1 = MakeHero("Parade1", WeaponType.Sword);
            r1.Equipment.Weapon!.Modifications.Add(new Modification
            {
                Name = "Heirloom",
                PrefixCategory = "Quality"
            });
            SetRank(r1, "w-polish", 1);
            PublishHit(r1);
            double dmg1 = r1.Effects.ConsumedDamageModPercent;

            var r2 = MakeHero("Parade2", WeaponType.Sword);
            r2.Equipment.Weapon!.Modifications.Add(new Modification
            {
                Name = "Heirloom",
                PrefixCategory = "Quality"
            });
            SetRank(r2, "w-polish", 2);
            PublishHit(r2);
            double dmg2 = r2.Effects.ConsumedDamageModPercent;

            TestBase.AssertEqual(10.0, dmg1, "rank 1 Parade Dress +10%",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(20.0, dmg2, "rank 2 Parade Dress +20%",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
