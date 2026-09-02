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
            Console.WriteLine("=== ClassDefenseCalculator / LeftoverEnergy Tests ===\n");
            _run = _pass = _fail = 0;

            TestLeftoverFromCost();
            TestApplyFromActionHeroOnly();
            TestResetClearsLeftoverAndShield();
            TestWarriorPercentCurve();
            TestLeftoverZeroWarriorOnly();
            TestLeftoverTwoBlockDominant();
            TestPierceIgnoresBlockAndWarrior();
            TestRogueDodgeAvoidsEvenOnPierce();
            TestWizardShieldAbsorbsAfterBlock();
            TestBarbarianMintsRageWithoutBlockAtLeftoverZero();
            TestUnnamedSyntheticEnergyCost();

            TestBase.PrintSummary("ClassDefenseCalculator / LeftoverEnergy Tests", _run, _pass, _fail);
        }

        private static void TestLeftoverFromCost()
        {
            Console.WriteLine("--- leftover from cost ---");
            TestBase.AssertEqual(2, LeftoverEnergy.LeftoverFromCost(1), "cost 1 leftover 2", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(1, LeftoverEnergy.LeftoverFromCost(2), "cost 2 leftover 1", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(0, LeftoverEnergy.LeftoverFromCost(3), "cost 3 leftover 0", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(1, LeftoverEnergy.LeftoverFromCost(0), "invalid cost defaults leftover 1", ref _run, ref _pass, ref _fail);
        }

        private static void TestApplyFromActionHeroOnly()
        {
            Console.WriteLine("--- ApplyFromAction hero only ---");
            var hero = TestDataBuilders.Character().WithName("EHero").WithLevel(1).Build();
            var enemy = TestDataBuilders.Enemy().WithName("EFoe").Build();
            var swing = TestDataBuilders.CreateMockAction("JAB");
            swing.EnergyCost = 1;
            LeftoverEnergy.ApplyFromAction(hero, swing);
            LeftoverEnergy.ApplyFromAction(enemy, swing);
            TestBase.AssertEqual(2, hero.LeftoverEnergy, "hero leftover 2 after cost 1", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(0, enemy.LeftoverEnergy, "enemy leftover unused", ref _run, ref _pass, ref _fail);
        }

        private static void TestResetClearsLeftoverAndShield()
        {
            Console.WriteLine("--- Reset leftover + shield ---");
            var hero = TestDataBuilders.Character().WithName("ResetHero").WithLevel(1).Build();
            hero.LeftoverEnergy = 2;
            hero.EnergyShieldCurrent = 9;
            hero.EnergyShieldMax = 9;
            LeftoverEnergy.Reset(hero);
            TestBase.AssertEqual(0, hero.LeftoverEnergy, "reset leftover 0", ref _run, ref _pass, ref _fail);
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

        private static void TestLeftoverZeroWarriorOnly()
        {
            Console.WriteLine("--- leftover 0 warrior only ---");
            var hero = UnarmedHeroWithDefense(8);
            hero.LeftoverEnergy = 0;
            var mit = ClassDefenseCalculator.ApplyIncoming(hero, 100, pierce: false);
            int expected = (int)Math.Round(100 * (1.0 - ClassDefenseCalculator.GetWarriorArmorPercent(8)), MidpointRounding.AwayFromZero);
            TestBase.AssertEqual(0.0, mit.BlockPercent, "no BLOCK", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(expected, mit.Remaining, "DEFENSE % only", ref _run, ref _pass, ref _fail);
        }

        private static void TestLeftoverTwoBlockDominant()
        {
            Console.WriteLine("--- leftover 2 BLOCK then warrior ---");
            var hero = UnarmedHeroWithDefense(8);
            hero.LeftoverEnergy = 2;
            var mit = ClassDefenseCalculator.ApplyIncoming(hero, 100, pierce: false);
            TestBase.AssertTrue(mit.BlockPercent > 0.4, $"BLOCK ~45%, got {mit.BlockPercent}", ref _run, ref _pass, ref _fail);
            int afterBlock = (int)Math.Round(100 * (1.0 - mit.BlockPercent), MidpointRounding.AwayFromZero);
            int expected = (int)Math.Round(afterBlock * (1.0 - mit.WarriorArmorPercent), MidpointRounding.AwayFromZero);
            TestBase.AssertEqual(expected, mit.Remaining, "multiplicative BLOCK then DEFENSE", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(mit.Remaining < 90, "BLOCK is dominant vs leftover 0", ref _run, ref _pass, ref _fail);
        }

        private static void TestPierceIgnoresBlockAndWarrior()
        {
            Console.WriteLine("--- pierce ignores BLOCK and warrior % ---");
            var hero = UnarmedHeroWithDefense(8);
            hero.LeftoverEnergy = 2;
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
            hero.LeftoverEnergy = 2;
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
            hero.LeftoverEnergy = 0;
            var mit = ClassDefenseCalculator.ApplyIncoming(hero, 6, pierce: false);
            TestBase.AssertEqual(0.0, mit.BlockPercent, "leftover 0 no BLOCK", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(6, mit.ShieldAbsorbed, "shield eats leftover-0 hit", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(0, mit.Remaining, "pad absorbs", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(4, hero.EnergyShieldCurrent, "shield remainder", ref _run, ref _pass, ref _fail);
        }

        private static void TestBarbarianMintsRageWithoutBlockAtLeftoverZero()
        {
            Console.WriteLine("--- barbarian leftover 0 rage ---");
            var hero = TestDataBuilders.Character().WithName("Barb").WithLevel(1).Build();
            hero.EquipItem(new WeaponItem("Club", 1, 8, 0.05, WeaponType.Mace), "weapon");
            hero.EquipItem(new ChestItem("Hide", 1, 16), "body");
            hero.LeftoverEnergy = 0;
            var mit = ClassDefenseCalculator.ApplyIncoming(hero, 50, pierce: false);
            TestBase.AssertEqual(0.0, mit.BlockPercent, "no BLOCK", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(50, mit.Remaining, "Rage is not DR", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(2, mit.RageMinted, "16/8 = 2 rage", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(2, hero.Effects.GetMaterialKeyword("RAGE"), "banked RAGE", ref _run, ref _pass, ref _fail);
        }

        private static void TestUnnamedSyntheticEnergyCost()
        {
            Console.WriteLine("--- unnamed synthetic energy 2 ---");
            var unnamed = TestDataBuilders.CreateMockAction("");
            unnamed.IsComboAction = false;
            unnamed.EnergyCost = LeftoverEnergy.DefaultCost;
            TestBase.AssertEqual(2, unnamed.EnergyCost, "unnamed cost 2", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(1, LeftoverEnergy.LeftoverFromCost(unnamed.EnergyCost), "unnamed leftover 1", ref _run, ref _pass, ref _fail);
        }

        private static Character UnarmedHeroWithDefense(int rating)
        {
            var hero = TestDataBuilders.Character().WithName("Unarmed").WithLevel(1).Build();
            hero.EquipItem(new ChestItem("Plate", 1, rating), "body");
            return hero;
        }
    }
}
