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

            var hero = TestDataBuilders.Character().WithName("HudHero").WithLevel(1).Build();
            hero.StandingBlockPercent = 0;
            TestBase.AssertTrue(
                HeroDefenseHudFormatter.FormatStandingBlockLine(hero) == "BLOCK 0%",
                "standing 0 BLOCK line", ref run, ref passed, ref failed);

            hero.StandingBlockPercent = 0.25;
            TestBase.AssertTrue(
                HeroDefenseHudFormatter.FormatStandingBlockLine(hero) == "BLOCK 25%",
                "standing 25% BLOCK line", ref run, ref passed, ref failed);

            hero.StandingBlockPercent = 0.60;
            TestBase.AssertTrue(
                HeroDefenseHudFormatter.FormatStandingBlockLine(hero) == "BLOCK 60%",
                "standing 60% BLOCK line", ref run, ref passed, ref failed);

            hero.EquipItem(new ChestItem("Plate", 1, 8), "body");
            TestBase.AssertTrue(
                HeroDefenseHudFormatter.FormatClassLayerLine(hero).StartsWith("TEMPO +", StringComparison.Ordinal),
                "unarmed class layer is TEMPO", ref run, ref passed, ref failed);

            var daggerHero = TestDataBuilders.Character().WithName("HudRogue").WithLevel(1).Build();
            daggerHero.EquipItem(new WeaponItem("Stiletto", 1, 4, 0.05, WeaponType.Dagger), "weapon");
            daggerHero.EquipItem(new ChestItem("Leather", 1, 10), "body");
            TestBase.AssertTrue(
                HeroDefenseHudFormatter.FormatClassLayerLine(daggerHero).StartsWith("COUNTER +", StringComparison.Ordinal),
                "dagger class layer is COUNTER", ref run, ref passed, ref failed);

            var wandHero = TestDataBuilders.Character().WithName("HudWizard").WithLevel(1).Build();
            wandHero.EquipItem(new WeaponItem("Staff", 1, 4, 0.05, WeaponType.Wand), "weapon");
            wandHero.EquipItem(new ChestItem("Robe", 1, 5), "body");
            ClassDefenseCalculator.RefillWizardShield(wandHero);
            TestBase.AssertTrue(
                HeroDefenseHudFormatter.FormatClassLayerLine(wandHero).StartsWith("Shield ", StringComparison.Ordinal)
                    && HeroDefenseHudFormatter.FormatClassLayerLine(wandHero).Contains("10/10", StringComparison.Ordinal),
                "wand class layer is Shield current/max", ref run, ref passed, ref failed);

            var maceHero = TestDataBuilders.Character().WithName("HudBarb").WithLevel(1).Build();
            maceHero.EquipItem(new WeaponItem("Club", 1, 8, 0.05, WeaponType.Mace), "weapon");
            maceHero.EquipItem(new ChestItem("Hide", 1, 16), "body");
            TestBase.AssertTrue(
                HeroDefenseHudFormatter.FormatClassLayerLine(maceHero).StartsWith("GRIT ", StringComparison.Ordinal),
                "mace class layer is GRIT", ref run, ref passed, ref failed);

            var action = TestDataBuilders.CreateMockAction("JAB");
            action.BlockPercent = 0.25;
            TestBase.AssertTrue(
                HeroDefenseHudFormatter.FormatActionBlockSuffix(action) == "Block 25%",
                "action block suffix", ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                HeroDefenseHudFormatter.FormatActionBlockTooltip(action) == "Block 25%",
                "action block tooltip", ref run, ref passed, ref failed);

            action.BlockPercent = 0;
            TestBase.AssertTrue(
                HeroDefenseHudFormatter.FormatActionBlockTooltip(action) == "Block 0% (DEFENSE only)",
                "zero block tooltip", ref run, ref passed, ref failed);

            action.BlockPercent = 0.10;
            TestBase.AssertTrue(
                HeroDefenseHudFormatter.FormatActionBlockTooltip(action) == "Block 10%",
                "10% block tooltip", ref run, ref passed, ref failed);

            TestBase.PrintSummary("HeroDefenseHudFormatter Tests", run, passed, failed);
        }
    }
}
