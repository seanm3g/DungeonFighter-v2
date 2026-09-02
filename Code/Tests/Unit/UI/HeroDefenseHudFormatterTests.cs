using System;
using RPGGame;
using RPGGame.Combat.Calculators;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.UI
{
    public static class HeroDefenseHudFormatterTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== HeroDefenseHudFormatter Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            var hero = new Character("Hud", 1);
            hero.LeftoverEnergy = 0;
            TestBase.AssertTrue(
                HeroDefenseHudFormatter.FormatLeftoverBlockLine(hero) == "Leftover 0  BLOCK 0%",
                "leftover 0 shows BLOCK 0%",
                ref run, ref passed, ref failed);

            hero.LeftoverEnergy = 1;
            TestBase.AssertTrue(
                HeroDefenseHudFormatter.FormatLeftoverBlockLine(hero) == "Leftover 1  BLOCK 25%",
                "leftover 1 shows BLOCK 25%",
                ref run, ref passed, ref failed);

            hero.LeftoverEnergy = 2;
            TestBase.AssertTrue(
                HeroDefenseHudFormatter.FormatLeftoverBlockLine(hero) == "Leftover 2  BLOCK 45%",
                "leftover 2 shows BLOCK 45%",
                ref run, ref passed, ref failed);

            TestBase.AssertTrue(
                HeroDefenseHudFormatter.FormatClassLayerLine(hero).StartsWith("DEFENSE ", StringComparison.Ordinal),
                "unarmed class layer is warrior DEFENSE %",
                ref run, ref passed, ref failed);

            hero.EquipItem(new WeaponItem("Stiletto", 1, 4, 0.05, WeaponType.Dagger), "weapon");
            TestBase.AssertTrue(
                HeroDefenseHudFormatter.FormatClassLayerLine(hero).StartsWith("Dodge ", StringComparison.Ordinal),
                "dagger class layer is Dodge",
                ref run, ref passed, ref failed);

            var wandHero = new Character("Mage", 1);
            wandHero.EquipItem(new WeaponItem("Staff", 1, 4, 0.05, WeaponType.Wand), "weapon");
            wandHero.EquipItem(new ChestItem("Robe", 1, 5), "body");
            ClassDefenseCalculator.RefillWizardShield(wandHero);
            TestBase.AssertTrue(
                HeroDefenseHudFormatter.FormatClassLayerLine(wandHero).StartsWith("Shield ", StringComparison.Ordinal)
                    && HeroDefenseHudFormatter.FormatClassLayerLine(wandHero).Contains("10/10", StringComparison.Ordinal),
                "wand class layer is Shield current/max",
                ref run, ref passed, ref failed);

            var maceHero = new Character("Barb", 1);
            maceHero.EquipItem(new WeaponItem("Club", 1, 8, 0.05, WeaponType.Mace), "weapon");
            maceHero.EquipItem(new ChestItem("Hide", 1, 16), "body");
            TestBase.AssertTrue(
                HeroDefenseHudFormatter.FormatClassLayerLine(maceHero).StartsWith("RAGE +", StringComparison.Ordinal),
                "mace class layer is RAGE mint",
                ref run, ref passed, ref failed);

            var action = new RPGGame.Action { Name = "Strike", EnergyCost = 2 };
            TestBase.AssertTrue(
                HeroDefenseHudFormatter.FormatActionEnergySuffix(action) == "E2",
                "strip energy suffix E2",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                HeroDefenseHudFormatter.FormatActionEnergyTooltip(action) == "Energy 2 (leftover 1 → BLOCK 25%)",
                "energy tooltip leftover 1 BLOCK 25%",
                ref run, ref passed, ref failed);

            action.EnergyCost = 3;
            TestBase.AssertTrue(
                HeroDefenseHudFormatter.FormatActionEnergyTooltip(action) == "Energy 3 (leftover 0, DEFENSE only)",
                "energy 3 is leftover 0 DEFENSE only",
                ref run, ref passed, ref failed);

            action.EnergyCost = 1;
            TestBase.AssertTrue(
                HeroDefenseHudFormatter.FormatActionEnergyTooltip(action) == "Energy 1 (leftover 2 → BLOCK 45%)",
                "energy 1 leftover 2 BLOCK 45%",
                ref run, ref passed, ref failed);

            TestBase.PrintSummary("HeroDefenseHudFormatter Tests", run, passed, failed);
        }
    }
}
