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

            hero.StandingBlockPercent = 1.0;
            TestBase.AssertTrue(
                HeroDefenseHudFormatter.FormatStandingBlockLine(hero) == "BLOCK 100%",
                "standing 100% BLOCK line", ref run, ref passed, ref failed);

            hero.StandingBlockPercent = 1.80;
            TestBase.AssertTrue(
                HeroDefenseHudFormatter.FormatStandingBlockLine(hero) == "BLOCK 180%",
                "standing 180% BLOCK line", ref run, ref passed, ref failed);

            hero.EquipItem(new ChestItem("Plate", 1, 8), "body");
            TestBase.AssertTrue(
                HeroDefenseHudFormatter.FormatClassLayerLine(hero).StartsWith("DR ", StringComparison.Ordinal),
                "class layer is live DR %", ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                HeroDefenseHudFormatter.FormatDamageReductionPercent(hero).EndsWith("%", StringComparison.Ordinal)
                    && !HeroDefenseHudFormatter.FormatDamageReductionPercent(hero).StartsWith("DR ", StringComparison.Ordinal),
                "hover DR is percent only", ref run, ref passed, ref failed);
            int rating = 8 + hero.Level + Math.Max(0, GameConfiguration.Instance.Combat.PlayerBaseArmor);
            int defensive = (int)Math.Round(rating * 1.80, MidpointRounding.AwayFromZero);
            TestBase.AssertEqual(defensive, ClassDefenseCalculator.GetDisplayedDefense(hero),
                "panel Defense is (gear + level + character base) × 180%", ref run, ref passed, ref failed);

            var action = TestDataBuilders.CreateMockAction("JAB");
            action.BlockPercent = 1.0;
            TestBase.AssertTrue(
                HeroDefenseHudFormatter.FormatActionBlockSuffix(action) == "",
                "neutral stance is omitted from the card", ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                HeroDefenseHudFormatter.FormatActionBlockTooltip(action) == "",
                "neutral stance is omitted from the tooltip", ref run, ref passed, ref failed);

            action.BlockPercent = 0;
            TestBase.AssertTrue(
                HeroDefenseHudFormatter.FormatActionBlockTooltip(action) == "result: aggressive stance",
                "zero block tooltip", ref run, ref passed, ref failed);

            action.BlockPercent = 1.80;
            TestBase.AssertTrue(
                HeroDefenseHudFormatter.FormatActionBlockTooltip(action) == "result: defensive stance",
                "180% block tooltip", ref run, ref passed, ref failed);

            TestBase.PrintSummary("HeroDefenseHudFormatter Tests", run, passed, failed);
        }
    }
}
