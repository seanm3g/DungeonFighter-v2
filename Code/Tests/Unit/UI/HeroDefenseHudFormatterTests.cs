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
            hero.StandingBlockPercent = 0;
            TestBase.AssertTrue(
                HeroDefenseHudFormatter.FormatStandingBlockLine(hero) == "BLOCK 0%",
                "standing 0 shows BLOCK 0%",
                ref run, ref passed, ref failed);

            hero.StandingBlockPercent = 0.25;
            TestBase.AssertTrue(
                HeroDefenseHudFormatter.FormatStandingBlockLine(hero) == "BLOCK 25%",
                "standing 25%",
                ref run, ref passed, ref failed);

            hero.StandingBlockPercent = 0.60;
            TestBase.AssertTrue(
                HeroDefenseHudFormatter.FormatStandingBlockLine(hero) == "BLOCK 60%",
                "standing free 60%",
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

            var action = new RPGGame.Action { Name = "Strike", BlockPercent = 0.25 };
            TestBase.AssertTrue(
                HeroDefenseHudFormatter.FormatActionBlockSuffix(action) == "Block 25%",
                "strip block suffix Block 25%",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                HeroDefenseHudFormatter.FormatActionBlockTooltip(action) == "Block 25%",
                "block tooltip 25%",
                ref run, ref passed, ref failed);

            action.BlockPercent = 0;
            TestBase.AssertTrue(
                HeroDefenseHudFormatter.FormatActionBlockTooltip(action) == "Block 0% (DEFENSE only)",
                "block 0 is DEFENSE only",
                ref run, ref passed, ref failed);

            action.BlockPercent = 0.10;
            TestBase.AssertTrue(
                HeroDefenseHudFormatter.FormatActionBlockTooltip(action) == "Block 10%",
                "free block 10%",
                ref run, ref passed, ref failed);

            TestBase.PrintSummary("HeroDefenseHudFormatter Tests", run, passed, failed);
        }
    }
}
