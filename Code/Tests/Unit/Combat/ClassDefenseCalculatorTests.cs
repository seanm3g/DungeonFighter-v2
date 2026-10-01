using System;
using RPGGame;
using RPGGame.Combat.Calculators;
using RPGGame.Combat.Formatting;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Combat
{
    public static class ClassDefenseCalculatorTests
    {
        private static int _run, _pass, _fail;

        public static void RunAllTests()
        {
            Console.WriteLine("=== ClassDefenseCalculator / StandingBlock Tests ===\n");
            _run = _pass = _fail = 0;

            TestParsePercentPoints();
            TestApplyFromActionHeroAndEnemy();
            TestUnnamedSwingSetsBaseDefense();
            TestResolvedSwingSetsStanceFromHit();
            TestNamedSwingReplacesPriorStandingBlock();
            TestResetOpensAtOneHundred();
            TestHitConsumesComboBonusToBase();
            TestDisplayedDefenseScalesWithStandingBlock();
            TestEnemyUsesWowDr();
            TestDefenseScaleCurveHelpersRemain();
            TestWowDrTableAtK100();
            TestRatingZeroAlwaysZeroDr();
            TestPierceSkipsDr();
            TestNoTempoGritShieldOnLivePath();
            TestMultiHitIndexUsesBaseAfterFirstTick();
            TestTempoConsumeShortensActionLength();
            TestCounterConsumeBoostsDamage();
            TestUnnamedSyntheticResolvesToOne();

            TestBase.PrintSummary("ClassDefenseCalculator / StandingBlock Tests", _run, _pass, _fail);
        }

        private static void TestParsePercentPoints()
        {
            Console.WriteLine("--- parse percent points ---");
            TestBase.AssertEqual(1.80, StandingBlock.ParsePercentPoints("180"), "180 → 1.80", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(1.0, StandingBlock.ParsePercentPoints("100"), "100 → 1.0", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(0.0, StandingBlock.ParsePercentPoints("0"), "0 → 0", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(5.0, StandingBlock.ParsePercentPoints("500"), "500 → 5.0", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(5.0, StandingBlock.ParsePercentPoints("999"), "over 500 clamps to 5", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(StandingBlock.DefaultBlockPercent, StandingBlock.ParsePercentPoints(""), "empty → default", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(1.80, StandingBlock.FromLegacyEnergyCost(1), "legacy energy 1 → 180%", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(1.0, StandingBlock.FromLegacyEnergyCost(2), "legacy energy 2 → 100%", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(0.0, StandingBlock.FromLegacyEnergyCost(3), "legacy energy 3 → 0%", ref _run, ref _pass, ref _fail);
        }

        private static void TestApplyFromActionHeroAndEnemy()
        {
            Console.WriteLine("--- ApplyFromAction hero and enemy ---");
            var hero = TestDataBuilders.Character().WithName("EHero").WithLevel(1).Build();
            var enemy = TestDataBuilders.Enemy().WithName("EFoe").Build();
            var swing = TestDataBuilders.CreateMockAction("JAB");
            swing.BlockPercent = 1.80;
            StandingBlock.ApplyFromAction(hero, swing);
            StandingBlock.ApplyFromAction(enemy, swing);
            TestBase.AssertEqual(1.80, hero.StandingBlockPercent, "hero standing 180%", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(1.80, enemy.StandingBlockPercent, "enemy standing 180%", ref _run, ref _pass, ref _fail);
        }

        private static void TestUnnamedSwingSetsBaseDefense()
        {
            Console.WriteLine("--- unnamed hit/miss sets 100% ---");
            var hero = TestDataBuilders.Character().WithName("ClearHero").WithLevel(1).Build();
            var named = TestDataBuilders.CreateMockAction("SLAM");
            named.IsComboAction = true;
            named.BlockPercent = 1.80;
            StandingBlock.ApplyFromAction(hero, named);
            TestBase.AssertEqual(1.80, hero.StandingBlockPercent, "named slam sets 180%", ref _run, ref _pass, ref _fail);

            var unnamed = TestDataBuilders.CreateMockAction("");
            unnamed.IsComboAction = false;
            unnamed.BlockPercent = 0;
            StandingBlock.ApplyFromAction(hero, unnamed);
            TestBase.AssertEqual(1.0, hero.StandingBlockPercent, "unnamed sets 100%", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(StandingBlock.IsUnnamedSwing(unnamed), "unnamed helper", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(1.0, StandingBlock.ResolveFromAction(unnamed), "resolve unnamed 1.0", ref _run, ref _pass, ref _fail);
        }

        private static void TestResolvedSwingSetsStanceFromHit()
        {
            Console.WriteLine("--- resolved swing: named hit sets stance; miss and normal hit are neutral ---");
            var hero = TestDataBuilders.Character().WithName("StanceHero").WithLevel(1).Build();
            var named = TestDataBuilders.CreateMockAction("SLAM");
            named.IsComboAction = true;
            named.BlockPercent = 1.80;
            StandingBlock.ApplyFromResolvedSwing(hero, named, hit: true);
            TestBase.AssertEqual(1.80, hero.StandingBlockPercent, "named hit sets defensive", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual("defensive", StandingBlock.GetStanceName(hero.StandingBlockPercent), "180% is defensive", ref _run, ref _pass, ref _fail);

            StandingBlock.ApplyFromResolvedSwing(hero, named, hit: false);
            TestBase.AssertEqual(1.0, hero.StandingBlockPercent, "named miss sets neutral", ref _run, ref _pass, ref _fail);

            var aggressive = TestDataBuilders.CreateMockAction("RUSH");
            aggressive.IsComboAction = true;
            aggressive.BlockPercent = 0;
            StandingBlock.ApplyFromResolvedSwing(hero, aggressive, hit: true);
            TestBase.AssertEqual(0.0, hero.StandingBlockPercent, "named hit sets aggressive", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual("result: aggressive stance", StandingBlock.FormatCardLine(aggressive.BlockPercent), "card line", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual("", StandingBlock.FormatCardLine(1.0), "neutral card line is blank", ref _run, ref _pass, ref _fail);

            var unnamed = TestDataBuilders.CreateMockAction("");
            unnamed.IsComboAction = false;
            unnamed.BlockPercent = 1.80;
            StandingBlock.ApplyFromResolvedSwing(hero, unnamed, hit: true);
            TestBase.AssertEqual(1.0, hero.StandingBlockPercent, "normal hit sets neutral", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual("neutral", StandingBlock.GetStanceName(1.0), "100% is neutral", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual("Defensive", StandingBlock.FormatStanceLabel(1.80), "editor label", ref _run, ref _pass, ref _fail);
        }

        private static void TestNamedSwingReplacesPriorStandingBlock()
        {
            Console.WriteLine("--- last named action wins standing BLOCK ---");
            var hero = TestDataBuilders.Character().WithName("LastNamed").WithLevel(1).Build();
            var first = TestDataBuilders.CreateMockAction("JAB");
            first.IsComboAction = true;
            first.BlockPercent = 1.0;
            var second = TestDataBuilders.CreateMockAction("SLAM");
            second.IsComboAction = true;
            second.BlockPercent = 1.80;
            StandingBlock.ApplyFromAction(hero, first);
            StandingBlock.ApplyFromAction(hero, second);
            TestBase.AssertEqual(1.80, hero.StandingBlockPercent, "second named overwrites first", ref _run, ref _pass, ref _fail);
        }

        private static void TestResetOpensAtOneHundred()
        {
            Console.WriteLine("--- Reset standing to 100% + shield + pending ---");
            var hero = TestDataBuilders.Character().WithName("ResetHero").WithLevel(1).Build();
            hero.StandingBlockPercent = 1.80;
            hero.EnergyShieldCurrent = 9;
            hero.EnergyShieldMax = 9;
            hero.Effects.PendingDefenseTempoSpeedPct = 12;
            hero.Effects.PendingDefenseCounterDamagePct = 15;
            StandingBlock.Reset(hero);
            TestBase.AssertEqual(1.0, hero.StandingBlockPercent, "reset standing 1.0", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(0, hero.EnergyShieldCurrent, "reset shield 0", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(0.0, hero.Effects.PendingDefenseTempoSpeedPct, "reset tempo 0", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(0.0, hero.Effects.PendingDefenseCounterDamagePct, "reset counter 0", ref _run, ref _pass, ref _fail);
        }

        private static void TestHitConsumesComboBonusToBase()
        {
            Console.WriteLine("--- incoming hit resets standing BLOCK to 100% ---");
            var attacker = TestDataBuilders.Enemy().WithName("HitAtk").WithHealth(100).Build();
            var hero = UnarmedHeroWithDefense(8);
            hero.StandingBlockPercent = 1.80;
            var action = TestDataBuilders.CreateMockAction("JAB");
            action.DamageMultiplier = 1.0;
            DamageCalculator.CalculateDamage(attacker, hero, action, 1.0, 1.0, 0, 10);
            TestBase.AssertEqual(1.80, hero.StandingBlockPercent, "calc does not consume (HUD still reads stance)", ref _run, ref _pass, ref _fail);
            StandingBlock.ConsumeAfterHit(hero);
            TestBase.AssertEqual(1.0, hero.StandingBlockPercent, "after hit standing is base 100%", ref _run, ref _pass, ref _fail);
        }

        private static void TestDisplayedDefenseScalesWithStandingBlock()
        {
            Console.WriteLine("--- displayed Defense is rating × BLOCK ---");
            var hero = UnarmedHeroWithDefense(8);
            int rating = 8 + hero.Level + Math.Max(0, GameConfiguration.Instance.Combat.PlayerBaseArmor);
            hero.StandingBlockPercent = 1.0;
            TestBase.AssertEqual(rating, ClassDefenseCalculator.GetDisplayedDefense(hero), "100% = gear + level + character base", ref _run, ref _pass, ref _fail);
            hero.StandingBlockPercent = 1.80;
            int defensive = (int)Math.Round(rating * 1.80, MidpointRounding.AwayFromZero);
            TestBase.AssertEqual(defensive, ClassDefenseCalculator.GetDisplayedDefense(hero), "180% of gear + level + character base", ref _run, ref _pass, ref _fail);
            hero.StandingBlockPercent = 0;
            TestBase.AssertEqual(0, ClassDefenseCalculator.GetDisplayedDefense(hero), "0% = 0", ref _run, ref _pass, ref _fail);
        }

        private static void TestEnemyUsesWowDr()
        {
            Console.WriteLine("--- enemy incoming uses WoW DR ---");
            double kSnap = GameConfiguration.Instance.Combat.ArmorReductionFactor;
            GameConfiguration.Instance.Combat.ArmorReductionFactor = 100;
            try
            {
                var attacker = TestDataBuilders.Character().WithName("VsFoe").WithLevel(1).Build();
                attacker.EquipItem(new WeaponItem("TestSword", 1, 20), "weapon");
                var foe = new Enemy("ArmoredFoe", 1, 200, 5, 5, 5, 5, armor: 100, isLiving: true);
                foe.StandingBlockPercent = 1.0;
                var action = TestDataBuilders.CreateMockAction("JAB");
                action.DamageMultiplier = 1.0;
                int raw = DamageCalculator.CalculateRawDamage(attacker, action, 1.0, 1.0, 10);
                int dmg = DamageCalculator.CalculateDamage(attacker, foe, action, 1.0, 1.0, 0, 10);
                double dr = ClassDefenseCalculator.ComputeDr(100, 100, pierce: false);
                int expected = (int)Math.Round(raw * (1.0 - dr), MidpointRounding.AwayFromZero);
                int min = Math.Max(1, GameConfiguration.Instance.Combat.MinimumDamage);
                if (expected > 0)
                    expected = Math.Max(min, expected);
                TestBase.AssertEqual(expected, dmg, "enemy 100 defense at 100% uses WoW DR", ref _run, ref _pass, ref _fail);
                StandingBlock.ConsumeAfterHit(foe);
                TestBase.AssertEqual(1.0, foe.StandingBlockPercent, "enemy standing reset after hit", ref _run, ref _pass, ref _fail);

                foe.StandingBlockPercent = 1.80;
                TestBase.AssertEqual(180, ClassDefenseCalculator.GetDisplayedDefense(foe), "enemy displayed 180", ref _run, ref _pass, ref _fail);
            }
            finally
            {
                GameConfiguration.Instance.Combat.ArmorReductionFactor = kSnap;
            }
        }

        private static void TestDefenseScaleCurveHelpersRemain()
        {
            Console.WriteLine("--- unused class-layer helpers still scale ---");
            TestBase.AssertEqual(0, ClassDefenseCalculator.GetWarriorTempoSpeedPct(0), "rating 0 tempo 0", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(13, ClassDefenseCalculator.GetWarriorTempoSpeedPct(20), "rating 20 tempo ≈ 13%", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(ClassDefenseCalculator.GetWarriorTempoSpeedPct(10000) <= ClassDefenseCalculator.WarriorTempoCap,
                "tempo cap 25", ref _run, ref _pass, ref _fail);
        }

        private static void TestWowDrTableAtK100()
        {
            Console.WriteLine("--- WoW DR table K=100 rating=100 ---");
            double kSnap = GameConfiguration.Instance.Combat.ArmorReductionFactor;
            int baseSnap = GameConfiguration.Instance.Combat.PlayerBaseArmor;
            GameConfiguration.Instance.Combat.ArmorReductionFactor = 100;
            GameConfiguration.Instance.Combat.PlayerBaseArmor = 0;
            try
            {
                var hero = UnarmedHeroWithDefense(100);
                hero.StandingBlockPercent = 0;
                var glass = ClassDefenseCalculator.ApplyIncoming(hero, 100, pierce: false);
                TestBase.AssertEqual(100, glass.Remaining, "0% → 0 DR", ref _run, ref _pass, ref _fail);

                hero.StandingBlockPercent = 1.0;
                var mid = ClassDefenseCalculator.ApplyIncoming(hero, 100, pierce: false);
                TestBase.AssertEqual(50, mid.Remaining, "100% → 50% DR", ref _run, ref _pass, ref _fail);

                hero.StandingBlockPercent = 1.80;
                var high = ClassDefenseCalculator.ApplyIncoming(hero, 100, pierce: false);
                TestBase.AssertEqual(35, high.Remaining, "180% of gear 100 + level 1 → remaining 35", ref _run, ref _pass, ref _fail);

                hero.StandingBlockPercent = 2.0;
                var twice = ClassDefenseCalculator.ApplyIncoming(hero, 100, pierce: false);
                TestBase.AssertEqual(33, twice.Remaining, "200% → remaining 33", ref _run, ref _pass, ref _fail);

                hero.StandingBlockPercent = 5.0;
                var cap = ClassDefenseCalculator.ApplyIncoming(hero, 100, pierce: false);
                TestBase.AssertEqual(17, cap.Remaining, "500% → remaining 17", ref _run, ref _pass, ref _fail);
            }
            finally
            {
                GameConfiguration.Instance.Combat.ArmorReductionFactor = kSnap;
                GameConfiguration.Instance.Combat.PlayerBaseArmor = baseSnap;
            }
        }

        private static void TestRatingZeroAlwaysZeroDr()
        {
            Console.WriteLine("--- rating 0 always 0 DR ---");
            double kSnap = GameConfiguration.Instance.Combat.ArmorReductionFactor;
            int baseSnap = GameConfiguration.Instance.Combat.PlayerBaseArmor;
            GameConfiguration.Instance.Combat.ArmorReductionFactor = 100;
            GameConfiguration.Instance.Combat.PlayerBaseArmor = 0;
            try
            {
                var hero = TestDataBuilders.Character().WithName("Naked").WithLevel(1).Build();
                hero.StandingBlockPercent = 5.0;
                var mit = ClassDefenseCalculator.ApplyIncoming(hero, 40, pierce: false);
                TestBase.AssertEqual(1, mit.Rating, "no gear still includes level 1", ref _run, ref _pass, ref _fail);
                TestBase.AssertEqual(38, mit.Remaining, "level 1 at 500% still reduces a little", ref _run, ref _pass, ref _fail);
            }
            finally
            {
                GameConfiguration.Instance.Combat.ArmorReductionFactor = kSnap;
                GameConfiguration.Instance.Combat.PlayerBaseArmor = baseSnap;
            }
        }

        private static void TestPierceSkipsDr()
        {
            Console.WriteLine("--- pierce skips DR ---");
            var hero = UnarmedHeroWithDefense(100);
            hero.StandingBlockPercent = 1.80;
            var mit = ClassDefenseCalculator.ApplyIncoming(hero, 100, pierce: true);
            TestBase.AssertEqual(100, mit.Remaining, "pierce full hit", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(0.0, mit.DrPercent, "pierce DR 0", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(0.0, hero.Effects.PendingDefenseTempoSpeedPct, "no Tempo mint", ref _run, ref _pass, ref _fail);
        }

        private static void TestNoTempoGritShieldOnLivePath()
        {
            Console.WriteLine("--- live path does not mint Tempo / apply Grit / eat shield ---");
            var hero = UnarmedHeroWithDefense(8);
            hero.StandingBlockPercent = 1.0;
            hero.Effects.PendingDefenseTempoSpeedPct = 0;
            var mit = ClassDefenseCalculator.ApplyIncoming(hero, 100, pierce: false);
            TestBase.AssertEqual(0, mit.TempoPct, "no Tempo field", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(0.0, hero.Effects.PendingDefenseTempoSpeedPct, "no Tempo pending", ref _run, ref _pass, ref _fail);

            var barb = TestDataBuilders.Character().WithName("Barb").WithLevel(1).Build();
            barb.EquipItem(new WeaponItem("Club", 1, 8, 0.05, WeaponType.Mace), "weapon");
            barb.EquipItem(new ChestItem("Hide", 1, 16), "body");
            barb.StandingBlockPercent = 0;
            var grit = ClassDefenseCalculator.ApplyIncoming(barb, 50, pierce: false);
            TestBase.AssertEqual(0, grit.GritIgnored, "no Grit on live path", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(50, grit.Remaining, "glass 0% = full", ref _run, ref _pass, ref _fail);

            var wiz = TestDataBuilders.Character().WithName("Wizard").WithLevel(1).Build();
            wiz.EquipItem(new WeaponItem("Staff", 1, 4, 0.05, WeaponType.Wand), "weapon");
            wiz.EquipItem(new ChestItem("Robe", 1, 5), "body");
            ClassDefenseCalculator.RefillWizardShield(wiz);
            wiz.StandingBlockPercent = 0;
            int shieldBefore = wiz.EnergyShieldCurrent;
            var pad = ClassDefenseCalculator.ApplyIncoming(wiz, 6, pierce: false);
            TestBase.AssertEqual(0, pad.ShieldAbsorbed, "no shield absorb on live path", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(shieldBefore, wiz.EnergyShieldCurrent, "shield unchanged", ref _run, ref _pass, ref _fail);
        }

        private static void TestMultiHitIndexUsesBaseAfterFirstTick()
        {
            Console.WriteLine("--- multi-hit tick 1 uses 1.0× even if standing is 0 or 1.8 ---");
            double kSnap = GameConfiguration.Instance.Combat.ArmorReductionFactor;
            int baseSnap = GameConfiguration.Instance.Combat.PlayerBaseArmor;
            GameConfiguration.Instance.Combat.ArmorReductionFactor = 100;
            GameConfiguration.Instance.Combat.PlayerBaseArmor = 0;
            try
            {
                var hero = UnarmedHeroWithDefense(100);
                hero.StandingBlockPercent = 0;
                var tick0 = ClassDefenseCalculator.ApplyIncoming(hero, 100, pierce: false, mitigationHitIndex: 0);
                var tick1 = ClassDefenseCalculator.ApplyIncoming(hero, 100, pierce: false, mitigationHitIndex: 1);
                TestBase.AssertEqual(100, tick0.Remaining, "tick 0 glass", ref _run, ref _pass, ref _fail);
                TestBase.AssertEqual(50, tick1.Remaining, "tick 1 base 100%", ref _run, ref _pass, ref _fail);

                hero.StandingBlockPercent = 1.80;
                var first = ClassDefenseCalculator.ApplyIncoming(hero, 100, pierce: false, 0);
                var later = ClassDefenseCalculator.ApplyIncoming(hero, 100, pierce: false, 1);
                TestBase.AssertEqual(35, first.Remaining, "tick 0 at 180% of gear 100 + level 1", ref _run, ref _pass, ref _fail);
                TestBase.AssertEqual(50, later.Remaining, "tick 1 at 100%", ref _run, ref _pass, ref _fail);
            }
            finally
            {
                GameConfiguration.Instance.Combat.ArmorReductionFactor = kSnap;
                GameConfiguration.Instance.Combat.PlayerBaseArmor = baseSnap;
            }
        }

        private static void TestTempoConsumeShortensActionLength()
        {
            Console.WriteLine("--- Tempo consume shortens display length ---");
            var hero = UnarmedHeroWithDefense(20);
            var action = TestDataBuilders.CreateMockAction("JAB");
            action.Length = 1.0;
            double baseSpeed = ActionSpeedCalculator.CalculateActualActionSpeed(hero, action);
            hero.Effects.PendingDefenseTempoSpeedPct = 25;
            double withTempo = ActionSpeedCalculator.CalculateActualActionSpeed(hero, action);
            TestBase.AssertTrue(withTempo < baseSpeed, "tempo shortens duration", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(25.0, hero.Effects.PendingDefenseTempoSpeedPct, "display does not consume", ref _run, ref _pass, ref _fail);
            double consumed = hero.Effects.ConsumePendingDefenseTempoSpeedPct();
            TestBase.AssertEqual(25.0, consumed, "consume returns pct", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(0.0, hero.Effects.PendingDefenseTempoSpeedPct, "cleared after consume", ref _run, ref _pass, ref _fail);
        }

        private static void TestCounterConsumeBoostsDamage()
        {
            Console.WriteLine("--- Counter consume boosts hero damage ---");
            var hero = TestDataBuilders.Character().WithName("CounterHero").WithLevel(1).Build();
            hero.EquipItem(new WeaponItem("Stiletto", 1, 10, 0.05, WeaponType.Dagger), "weapon");
            var foe = TestDataBuilders.Enemy().WithName("Foe").WithHealth(500).Build();
            var action = TestDataBuilders.CreateMockAction("STAB");
            action.DamageMultiplier = 1.0;

            int baseline = DamageCalculator.CalculateDamage(hero, foe, action, 1.0, 1.0, 0, 10);
            hero.Effects.PendingDefenseCounterDamagePct = 30;
            int boosted = DamageCalculator.CalculateDamage(hero, foe, action, 1.0, 1.0, 0, 10);
            TestBase.AssertTrue(boosted > baseline, "counter boosts damage", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(0.0, hero.Effects.PendingDefenseCounterDamagePct, "counter consumed", ref _run, ref _pass, ref _fail);
        }

        private static void TestUnnamedSyntheticResolvesToOne()
        {
            Console.WriteLine("--- unnamed synthetic resolves to 100% ---");
            var unnamed = TestDataBuilders.CreateMockAction("");
            unnamed.IsComboAction = false;
            unnamed.BlockPercent = 0;
            TestBase.AssertEqual(1.0, StandingBlock.ResolveFromAction(unnamed), "resolve unnamed 1.0", ref _run, ref _pass, ref _fail);
        }

        private static Character UnarmedHeroWithDefense(int rating)
        {
            var hero = TestDataBuilders.Character().WithName("Unarmed").WithLevel(1).Build();
            hero.EquipItem(new ChestItem("Plate", 1, rating), "body");
            return hero;
        }
    }
}
