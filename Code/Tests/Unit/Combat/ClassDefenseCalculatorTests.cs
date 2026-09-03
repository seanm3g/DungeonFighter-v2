using System;
using RPGGame;
using RPGGame.Combat.Calculators;
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
            TestResetClearsStandingAndShield();
            TestWarriorPercentCurve();
            TestStandingZeroWarriorOnly();
            TestFreeBlockDominant();
            TestPierceIgnoresBlockAndWarrior();
            TestRogueDodgeAvoidsEvenOnPierce();
            TestWizardShieldAbsorbsAfterBlock();
            TestBarbarianMintsRageWithoutBlockAtZero();
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

        private static void TestResetClearsStandingAndShield()
        {
            Console.WriteLine("--- Reset standing + shield ---");
            var hero = TestDataBuilders.Character().WithName("ResetHero").WithLevel(1).Build();
            hero.StandingBlockPercent = 0.45;
            hero.EnergyShieldCurrent = 9;
            hero.EnergyShieldMax = 9;
            StandingBlock.Reset(hero);
            TestBase.AssertEqual(0.0, hero.StandingBlockPercent, "reset standing 0", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(0, hero.EnergyShieldCurrent, "reset shield 0", ref _run, ref _pass, ref _fail);
        }

        private static void TestWarriorPercentCurve()
        {
            Console.WriteLine("--- Warrior DEFENSE % curve ---");
            TestBase.AssertEqual(0.0, ClassDefenseCalculator.GetWarriorArmorPercent(0), "rating 0 = 0%", ref _run, ref _pass, ref _fail);
            double at20 = ClassDefenseCalculator.GetWarriorArmorPercent(20);
            TestBase.AssertTrue(at20 > 0.12 && at20 < 0.13, $"rating 20 ≈ 12.5%, got {at20}", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(ClassDefenseCalculator.GetWarriorArmorPercent(10000) < ClassDefenseCalculator.WarriorArmorPercentCap + 0.0001,
                "cap 25%", ref _run, ref _pass, ref _fail);
        }

        private static void TestStandingZeroWarriorOnly()
        {
            Console.WriteLine("--- standing 0 warrior only ---");
            var hero = UnarmedHeroWithDefense(8);
            hero.StandingBlockPercent = 0;
            var mit = ClassDefenseCalculator.ApplyIncoming(hero, 100, pierce: false);
            int expected = (int)Math.Round(100 * (1.0 - ClassDefenseCalculator.GetWarriorArmorPercent(8)), MidpointRounding.AwayFromZero);
            TestBase.AssertEqual(0.0, mit.BlockPercent, "no BLOCK", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(expected, mit.Remaining, "DEFENSE % only", ref _run, ref _pass, ref _fail);
        }

        private static void TestFreeBlockDominant()
        {
            Console.WriteLine("--- free BLOCK then warrior ---");
            var hero = UnarmedHeroWithDefense(8);
            hero.StandingBlockPercent = 0.60;
            var mit = ClassDefenseCalculator.ApplyIncoming(hero, 100, pierce: false);
            TestBase.AssertEqual(0.60, mit.BlockPercent, "BLOCK 60%", ref _run, ref _pass, ref _fail);
            int afterBlock = (int)Math.Round(100 * (1.0 - mit.BlockPercent), MidpointRounding.AwayFromZero);
            int expected = (int)Math.Round(afterBlock * (1.0 - mit.WarriorArmorPercent), MidpointRounding.AwayFromZero);
            TestBase.AssertEqual(expected, mit.Remaining, "multiplicative BLOCK then DEFENSE", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(mit.Remaining < 50, "60% BLOCK is dominant", ref _run, ref _pass, ref _fail);
        }

        private static void TestPierceIgnoresBlockAndWarrior()
        {
            Console.WriteLine("--- pierce ignores BLOCK and warrior % ---");
            var hero = UnarmedHeroWithDefense(8);
            hero.StandingBlockPercent = 0.45;
            var mit = ClassDefenseCalculator.ApplyIncoming(hero, 100, pierce: true);
            TestBase.AssertEqual(0.0, mit.BlockPercent, "pierce BLOCK 0", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(0.0, mit.WarriorArmorPercent, "pierce warrior 0", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(100, mit.Remaining, "pierce full hit", ref _run, ref _pass, ref _fail);
        }

        private static void TestRogueDodgeAvoidsEvenOnPierce()
        {
            Console.WriteLine("--- rogue dodge on pierce ---");
            var hero = TestDataBuilders.Character().WithName("Rogue").WithLevel(1).Build();
            hero.EquipItem(new WeaponItem("Stiletto", 1, 4, 0.05, WeaponType.Dagger), "weapon");
            hero.EquipItem(new ChestItem("Leather", 1, 10), "body");
            hero.StandingBlockPercent = 0.45;
            Dice.QueueUnforcedTestRolls(1);
            var mit = ClassDefenseCalculator.ApplyIncoming(hero, 40, pierce: true);
            Dice.QueueUnforcedTestRolls();
            TestBase.AssertTrue(mit.Dodged, "dodge succeeds", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(0, mit.Remaining, "true avoid", ref _run, ref _pass, ref _fail);
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

        private static void TestBarbarianMintsRageWithoutBlockAtZero()
        {
            Console.WriteLine("--- barbarian standing 0 rage ---");
            var hero = TestDataBuilders.Character().WithName("Barb").WithLevel(1).Build();
            hero.EquipItem(new WeaponItem("Club", 1, 8, 0.05, WeaponType.Mace), "weapon");
            hero.EquipItem(new ChestItem("Hide", 1, 16), "body");
            hero.StandingBlockPercent = 0;
            var mit = ClassDefenseCalculator.ApplyIncoming(hero, 50, pierce: false);
            TestBase.AssertEqual(0.0, mit.BlockPercent, "no BLOCK", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(50, mit.Remaining, "Rage is not DR", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(2, mit.RageMinted, "16/8 = 2 rage", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(2, hero.Effects.GetMaterialKeyword("RAGE"), "banked RAGE", ref _run, ref _pass, ref _fail);
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
