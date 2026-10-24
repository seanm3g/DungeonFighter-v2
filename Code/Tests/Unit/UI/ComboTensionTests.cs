using System;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Feedback;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Tests for <see cref="ComboTension"/> charge math (card states, pulse speed).
    /// </summary>
    public static class ComboTensionTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== ComboTension Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            TestChargeZeroOnOpener(ref run, ref passed, ref failed);
            TestChargeRisesToOneAtFinisher(ref run, ref passed, ref failed);
            TestNoChargeForSingleSlotCombo(ref run, ref passed, ref failed);
            TestFinisherNextOnlyOnLastSlot(ref run, ref passed, ref failed);
            TestSpentSlotsAreBeforeNext(ref run, ref passed, ref failed);
            TestPulseSpeedsUpWithCharge(ref run, ref passed, ref failed);
            TestPulseInUnitInterval(ref run, ref passed, ref failed);
            TestLowHealthVignetteOnlyForHero(ref run, ref passed, ref failed);

            TestBase.PrintSummary("ComboTension Tests", run, passed, failed);
        }

        private static void TestChargeZeroOnOpener(ref int run, ref int passed, ref int failed)
        {
            TestBase.AssertTrue(ComboTension.GetCharge(0, 4) == 0.0,
                "Opener has no charge", ref run, ref passed, ref failed);
        }

        private static void TestChargeRisesToOneAtFinisher(ref int run, ref int passed, ref int failed)
        {
            double one = ComboTension.GetCharge(1, 4);
            double two = ComboTension.GetCharge(2, 4);
            double three = ComboTension.GetCharge(3, 4);
            TestBase.AssertTrue(one > 0 && one < two && two < three,
                "Charge rises step by step", ref run, ref passed, ref failed);
            TestBase.AssertTrue(Math.Abs(three - 1.0) < 1e-9,
                "Charge is 1 when the finisher is next", ref run, ref passed, ref failed);
        }

        private static void TestNoChargeForSingleSlotCombo(ref int run, ref int passed, ref int failed)
        {
            TestBase.AssertTrue(ComboTension.GetCharge(0, 1) == 0.0 && !ComboTension.IsFinisherNext(0, 1),
                "One-slot combo has no build-up", ref run, ref passed, ref failed);
        }

        private static void TestFinisherNextOnlyOnLastSlot(ref int run, ref int passed, ref int failed)
        {
            TestBase.AssertTrue(ComboTension.IsFinisherNext(3, 4) && !ComboTension.IsFinisherNext(2, 4),
                "Finisher flag only on the last filled slot", ref run, ref passed, ref failed);
        }

        private static void TestSpentSlotsAreBeforeNext(ref int run, ref int passed, ref int failed)
        {
            TestBase.AssertTrue(ComboTension.IsSpent(0, 2) && ComboTension.IsSpent(1, 2)
                    && !ComboTension.IsSpent(2, 2) && !ComboTension.IsSpent(3, 2),
                "Slots before next are spent", ref run, ref passed, ref failed);
            TestBase.AssertTrue(!ComboTension.IsSpent(0, 0),
                "Nothing is spent on the opener (miss reset)", ref run, ref passed, ref failed);
        }

        private static void TestPulseSpeedsUpWithCharge(ref int run, ref int passed, ref int failed)
        {
            TestBase.AssertTrue(ComboTension.GetPulsePeriodMs(1.0) < ComboTension.GetPulsePeriodMs(0.3),
                "Pulse period shortens as charge rises", ref run, ref passed, ref failed);
        }

        private static void TestPulseInUnitInterval(ref int run, ref int passed, ref int failed)
        {
            bool ok = true;
            for (int ms = 0; ms < 3000; ms += 37)
            {
                double t = ComboTension.GetPulseT(0.6, DateTimeOffset.FromUnixTimeMilliseconds(ms));
                ok &= t >= 0.0 && t <= 1.0;
            }
            TestBase.AssertTrue(ok, "Pulse wave stays in 0..1", ref run, ref passed, ref failed);
        }

        private static void TestLowHealthVignetteOnlyForHero(ref int run, ref int passed, ref int failed)
        {
            var hero = new Character("Hero", 1) { MaxHealth = 100, CurrentHealth = 15 };
            TestBase.AssertTrue(ComboTension.IsHeroLowHealth(hero),
                "Low-health vignette shows for a hurt hero", ref run, ref passed, ref failed);
            hero.CurrentHealth = 60;
            TestBase.AssertTrue(!ComboTension.IsHeroLowHealth(hero),
                "No low-health vignette for a healthy hero", ref run, ref passed, ref failed);
            hero.CurrentHealth = 0;
            TestBase.AssertTrue(!ComboTension.IsHeroLowHealth(hero),
                "No low-health vignette for a dead hero", ref run, ref passed, ref failed);

            var enemy = new Enemy(name: "Goblin", level: 1, maxHealth: 100, strength: 8, agility: 6, technique: 4, intelligence: 4, armor: 0)
            {
                CurrentHealth = 10
            };
            TestBase.AssertTrue(!ComboTension.IsHeroLowHealth(enemy),
                "Low enemy health never triggers the vignette", ref run, ref passed, ref failed);
        }
    }
}
