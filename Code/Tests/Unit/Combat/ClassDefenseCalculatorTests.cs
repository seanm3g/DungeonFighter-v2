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
            TestApplyFromActionHeroOnly();
            TestUnnamedSwingClearsStandingBlock();
            TestNamedSwingReplacesPriorStandingBlock();
            TestResetClearsStandingShieldAndPending();
            TestDefenseScaleCurve();
            TestStandingZeroUnarmedMintsTempoNoDr();
            TestFreeBlockDominantThenTempo();
            TestPierceStillMintsTempo();
            TestRogueCounterMintOnPierce();
            TestWizardShieldAbsorbsAfterBlock();
            TestBarbarianGritAfterBlock();
            TestBarbarianGritSkippedOnPierce();
            TestTempoConsumeShortensActionLength();
            TestCounterConsumeBoostsDamage();
            TestUnnamedSyntheticBlockPercent();

            TestBase.PrintSummary("ClassDefenseCalculator / StandingBlock Tests", _run, _pass, _fail);
        }

        private static void TestParsePercentPoints()
        {
            Console.WriteLine("--- parse percent points ---");
            TestBase.AssertEqual(0.45, StandingBlock.ParsePercentPoints("45"), "45 → 0.45", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(0.25, StandingBlock.ParsePercentPoints("25"), "25 → 0.25", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(0.0, StandingBlock.ParsePercentPoints("0"), "0 → 0", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(0.10, StandingBlock.ParsePercentPoints("10"), "10 → 0.10", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(0.60, StandingBlock.ParsePercentPoints("60"), "60 → 0.60", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(StandingBlock.DefaultBlockPercent, StandingBlock.ParsePercentPoints(""), "empty → default", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(0.45, StandingBlock.FromLegacyEnergyCost(1), "legacy energy 1 → 45%", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(0.0, StandingBlock.FromLegacyEnergyCost(3), "legacy energy 3 → 0%", ref _run, ref _pass, ref _fail);
        }

        private static void TestApplyFromActionHeroOnly()
        {
            Console.WriteLine("--- ApplyFromAction hero only ---");
            var hero = TestDataBuilders.Character().WithName("EHero").WithLevel(1).Build();
            var enemy = TestDataBuilders.Enemy().WithName("EFoe").Build();
            var swing = TestDataBuilders.CreateMockAction("JAB");
            swing.BlockPercent = 0.45;
            StandingBlock.ApplyFromAction(hero, swing);
            StandingBlock.ApplyFromAction(enemy, swing);
            TestBase.AssertEqual(0.45, hero.StandingBlockPercent, "hero standing 45%", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(0.0, enemy.StandingBlockPercent, "enemy standing unused", ref _run, ref _pass, ref _fail);
        }

        private static void TestUnnamedSwingClearsStandingBlock()
        {
            Console.WriteLine("--- unnamed hit/miss clears standing BLOCK ---");
            var hero = TestDataBuilders.Character().WithName("ClearHero").WithLevel(1).Build();
            var named = TestDataBuilders.CreateMockAction("SLAM");
            named.IsComboAction = true;
            named.BlockPercent = 0.45;
            StandingBlock.ApplyFromAction(hero, named);
            TestBase.AssertEqual(0.45, hero.StandingBlockPercent, "named slam sets 45%", ref _run, ref _pass, ref _fail);

            var unnamed = TestDataBuilders.CreateMockAction("");
            unnamed.IsComboAction = false;
            unnamed.BlockPercent = 0.25; // ignored for unnamed
            StandingBlock.ApplyFromAction(hero, unnamed);
            TestBase.AssertEqual(0.0, hero.StandingBlockPercent, "unnamed clears to DEFENSE only", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(StandingBlock.IsUnnamedSwing(unnamed), "unnamed helper", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(0.0, StandingBlock.ResolveFromAction(unnamed), "resolve unnamed 0", ref _run, ref _pass, ref _fail);
        }

        private static void TestNamedSwingReplacesPriorStandingBlock()
        {
            Console.WriteLine("--- last named action wins standing BLOCK ---");
            var hero = TestDataBuilders.Character().WithName("LastNamed").WithLevel(1).Build();
            var first = TestDataBuilders.CreateMockAction("JAB");
            first.IsComboAction = true;
            first.BlockPercent = 0.10;
            var second = TestDataBuilders.CreateMockAction("SLAM");
            second.IsComboAction = true;
            second.BlockPercent = 0.60;
            StandingBlock.ApplyFromAction(hero, first);
            StandingBlock.ApplyFromAction(hero, second);
            TestBase.AssertEqual(0.60, hero.StandingBlockPercent, "second named overwrites first", ref _run, ref _pass, ref _fail);
        }

        private static void TestResetClearsStandingShieldAndPending()
        {
            Console.WriteLine("--- Reset standing + shield + pending ---");
            var hero = TestDataBuilders.Character().WithName("ResetHero").WithLevel(1).Build();
            hero.StandingBlockPercent = 0.45;
            hero.EnergyShieldCurrent = 9;
            hero.EnergyShieldMax = 9;
            hero.Effects.PendingDefenseTempoSpeedPct = 12;
            hero.Effects.PendingDefenseCounterDamagePct = 15;
            StandingBlock.Reset(hero);
            TestBase.AssertEqual(0.0, hero.StandingBlockPercent, "reset standing 0", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(0, hero.EnergyShieldCurrent, "reset shield 0", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(0.0, hero.Effects.PendingDefenseTempoSpeedPct, "reset tempo 0", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(0.0, hero.Effects.PendingDefenseCounterDamagePct, "reset counter 0", ref _run, ref _pass, ref _fail);
        }

        private static void TestDefenseScaleCurve()
        {
            Console.WriteLine("--- DEFENSE scale curve ---");
            TestBase.AssertEqual(0, ClassDefenseCalculator.GetWarriorTempoSpeedPct(0), "rating 0 tempo 0", ref _run, ref _pass, ref _fail);
            int at20 = ClassDefenseCalculator.GetWarriorTempoSpeedPct(20);
            TestBase.AssertEqual(13, at20, "rating 20 tempo ≈ 13%", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(ClassDefenseCalculator.GetWarriorTempoSpeedPct(10000) <= ClassDefenseCalculator.WarriorTempoCap,
                "tempo cap 25", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(15, ClassDefenseCalculator.GetRogueCounterDamagePct(20), "rating 20 counter 15%", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(13, ClassDefenseCalculator.GetBarbarianGrit(20), "rating 20 grit 13", ref _run, ref _pass, ref _fail);
        }

        private static void TestStandingZeroUnarmedMintsTempoNoDr()
        {
            Console.WriteLine("--- standing 0 unarmed Tempo mint, no DR ---");
            var hero = UnarmedHeroWithDefense(8);
            hero.StandingBlockPercent = 0;
            var mit = ClassDefenseCalculator.ApplyIncoming(hero, 100, pierce: false);
            int expectedTempo = ClassDefenseCalculator.GetWarriorTempoSpeedPct(8);
            TestBase.AssertEqual(0.0, mit.BlockPercent, "no BLOCK", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(100, mit.Remaining, "Tempo is not DR", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(expectedTempo, mit.TempoPct, "tempo minted", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual((double)expectedTempo, hero.Effects.PendingDefenseTempoSpeedPct, "pending tempo", ref _run, ref _pass, ref _fail);
        }

        private static void TestFreeBlockDominantThenTempo()
        {
            Console.WriteLine("--- free BLOCK then Tempo mint ---");
            var hero = UnarmedHeroWithDefense(8);
            hero.StandingBlockPercent = 0.60;
            var mit = ClassDefenseCalculator.ApplyIncoming(hero, 100, pierce: false);
            TestBase.AssertEqual(0.60, mit.BlockPercent, "BLOCK 60%", ref _run, ref _pass, ref _fail);
            int afterBlock = (int)Math.Round(100 * (1.0 - mit.BlockPercent), MidpointRounding.AwayFromZero);
            TestBase.AssertEqual(afterBlock, mit.Remaining, "BLOCK only (Tempo not DR)", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(mit.Remaining < 50, "60% BLOCK is dominant", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(mit.TempoPct > 0, "tempo still minted", ref _run, ref _pass, ref _fail);
        }

        private static void TestPierceStillMintsTempo()
        {
            Console.WriteLine("--- pierce skips BLOCK, still mints Tempo ---");
            var hero = UnarmedHeroWithDefense(8);
            hero.StandingBlockPercent = 0.45;
            var mit = ClassDefenseCalculator.ApplyIncoming(hero, 100, pierce: true);
            TestBase.AssertEqual(0.0, mit.BlockPercent, "pierce BLOCK 0", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(100, mit.Remaining, "pierce full hit", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(ClassDefenseCalculator.GetWarriorTempoSpeedPct(8), mit.TempoPct, "pierce still Tempo", ref _run, ref _pass, ref _fail);
        }

        private static void TestRogueCounterMintOnPierce()
        {
            Console.WriteLine("--- rogue Counter mint on pierce ---");
            var hero = TestDataBuilders.Character().WithName("Rogue").WithLevel(1).Build();
            hero.EquipItem(new WeaponItem("Stiletto", 1, 4, 0.05, WeaponType.Dagger), "weapon");
            hero.EquipItem(new ChestItem("Leather", 1, 10), "body");
            hero.StandingBlockPercent = 0.45;
            var mit = ClassDefenseCalculator.ApplyIncoming(hero, 40, pierce: true);
            int expected = ClassDefenseCalculator.GetRogueCounterDamagePct(10);
            TestBase.AssertEqual(40, mit.Remaining, "no dodge — full pierce hit", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(expected, mit.CounterPct, "counter minted", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual((double)expected, hero.Effects.PendingDefenseCounterDamagePct, "pending counter", ref _run, ref _pass, ref _fail);
        }

        private static void TestWizardShieldAbsorbsAfterBlock()
        {
            Console.WriteLine("--- wizard shield after BLOCK ---");
            var hero = TestDataBuilders.Character().WithName("Wizard").WithLevel(1).Build();
            hero.EquipItem(new WeaponItem("Staff", 1, 4, 0.05, WeaponType.Wand), "weapon");
            hero.EquipItem(new ChestItem("Robe", 1, 5), "body");
            ClassDefenseCalculator.RefillWizardShield(hero);
            TestBase.AssertEqual(10, hero.EnergyShieldMax, "pool = rating * 2", ref _run, ref _pass, ref _fail);
            hero.StandingBlockPercent = 0;
            var mit = ClassDefenseCalculator.ApplyIncoming(hero, 6, pierce: false);
            TestBase.AssertEqual(0.0, mit.BlockPercent, "standing 0 no BLOCK", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(6, mit.ShieldAbsorbed, "shield eats standing-0 hit", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(0, mit.Remaining, "pad absorbs", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(4, hero.EnergyShieldCurrent, "shield remainder", ref _run, ref _pass, ref _fail);
        }

        private static void TestBarbarianGritAfterBlock()
        {
            Console.WriteLine("--- barbarian Grit after BLOCK ---");
            var hero = TestDataBuilders.Character().WithName("Barb").WithLevel(1).Build();
            hero.EquipItem(new WeaponItem("Club", 1, 8, 0.05, WeaponType.Mace), "weapon");
            hero.EquipItem(new ChestItem("Hide", 1, 16), "body");
            hero.StandingBlockPercent = 0;
            int grit = ClassDefenseCalculator.GetBarbarianGrit(16);
            var mit = ClassDefenseCalculator.ApplyIncoming(hero, 50, pierce: false);
            TestBase.AssertEqual(0.0, mit.BlockPercent, "no BLOCK", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(grit, mit.GritIgnored, "grit applied", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(50 - grit, mit.Remaining, "flat ignore", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(0, hero.Effects.GetMaterialKeyword("RAGE"), "no armor RAGE mint", ref _run, ref _pass, ref _fail);
        }

        private static void TestBarbarianGritSkippedOnPierce()
        {
            Console.WriteLine("--- barbarian Grit skipped on pierce ---");
            var hero = TestDataBuilders.Character().WithName("BarbPierce").WithLevel(1).Build();
            hero.EquipItem(new WeaponItem("Club", 1, 8, 0.05, WeaponType.Mace), "weapon");
            hero.EquipItem(new ChestItem("Hide", 1, 16), "body");
            hero.StandingBlockPercent = 0.45;
            var mit = ClassDefenseCalculator.ApplyIncoming(hero, 50, pierce: true);
            TestBase.AssertEqual(0, mit.GritIgnored, "pierce no grit", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(50, mit.Remaining, "pierce full", ref _run, ref _pass, ref _fail);
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

        private static void TestUnnamedSyntheticBlockPercent()
        {
            Console.WriteLine("--- unnamed synthetic block 0% ---");
            var unnamed = TestDataBuilders.CreateMockAction("");
            unnamed.IsComboAction = false;
            unnamed.BlockPercent = 0;
            TestBase.AssertEqual(0.0, unnamed.BlockPercent, "unnamed block 0%", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(0.0, StandingBlock.ResolveFromAction(unnamed), "resolve unnamed 0%", ref _run, ref _pass, ref _fail);
        }

        private static Character UnarmedHeroWithDefense(int rating)
        {
            var hero = TestDataBuilders.Character().WithName("Unarmed").WithLevel(1).Build();
            hero.EquipItem(new ChestItem("Plate", 1, rating), "body");
            return hero;
        }
    }
}
