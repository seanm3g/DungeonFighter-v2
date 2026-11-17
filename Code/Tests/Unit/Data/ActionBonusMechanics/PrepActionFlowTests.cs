using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.Actions;
using RPGGame.Actions.Execution;
using RPGGame.Actions.RollModification;
using RPGGame.Tests;
using RPGGame.Utils;

namespace RPGGame.Tests.Unit.Data.ActionBonusMechanics
{
    /// <summary>
    /// Real-data PREP actions through <see cref="ActionExecutionFlow.Execute"/>: BLOOD OATH (pay 5% max HP, next action
    /// +60% damage) and PRELUDE (next 3 actions +15% AMP), each followed by SLAM.
    /// </summary>
    public static class PrepActionFlowTests
    {
        private const int ComboRoll = 19;

        public static void RunAll(ref int run, ref int passed, ref int failed)
        {
            TestBloodOathBuffsNextAction(ref run, ref passed, ref failed);
            TestBloodOathPaysHp(ref run, ref passed, ref failed);
            TestPreludeAmpsNextThreeSlots(ref run, ref passed, ref failed);
        }

        private sealed record Swing(int Damage, bool Hit, bool Combo, double TracedMod, double Amp);

        /// <summary>Strip [prep, SLAM, SLAM, SLAM]; optionally swings the prep on a combo roll, then swings SLAM at slot 2.</summary>
        private static Swing? SlamAfter(string prepName, bool prepFirst, out Character hero, out int hpPaidByPrep)
        {
            hero = new Character("PrepHero", 10);
            hpPaidByPrep = 0;
            var prep = ActionLoader.GetAction(prepName);
            if (prep == null || ActionLoader.GetAction("SLAM") == null)
                return null;
            hero.Stats.Strength = 60;
            prep.IsComboAction = true;
            prep.ComboOrder = 1;
            hero.AddAction(prep, 1.0);
            hero.Actions.AddToCombo(prep);
            Action slam = null!;
            for (int i = 0; i < 3; i++)
            {
                var s = ActionLoader.GetAction("SLAM")!;
                s.IsComboAction = true;
                s.ComboOrder = i + 2;
                hero.AddAction(s, 1.0);
                hero.Actions.AddToCombo(s);
                if (i == 0)
                    slam = s;
            }
            hero.ComboStep = 0;

            var lastUsed = new Dictionary<Actor, Action>();
            var lastCritMiss = new Dictionary<Actor, bool>();
            var enemy = new Enemy("PrepDummy", 1, 50000, 0, 0, 0, 0);
            try
            {
                if (prepFirst)
                {
                    hero.CurrentHealth = hero.GetEffectiveMaxHealth();
                    int hpBefore = hero.CurrentHealth;
                    Dice.SetTestRoll(ComboRoll);
                    ActionSelector.SetStoredActionRoll(hero, ComboRoll);
                    ActionExecutionFlow.Execute(hero, enemy, null, null, prep, null, lastUsed, lastCritMiss);
                    hpPaidByPrep = hpBefore - hero.CurrentHealth;
                }
                else
                {
                    hero.ComboStep = 1;
                }
                Dice.SetTestRoll(ComboRoll);
                ActionSelector.SetStoredActionRoll(hero, ComboRoll);
                var r = ActionExecutionFlow.Execute(hero, enemy, null, null, slam, null, lastUsed, lastCritMiss);
                return new Swing(r.Damage, r.Hit, r.IsCombo, r.DamageTrace?.NextActionDamageModPercent ?? 0, r.DamageTrace?.Amp ?? 1.0);
            }
            finally
            {
                Dice.ClearTestRoll();
                ActionSelector.ClearStoredRolls();
            }
        }

        private static void TestBloodOathBuffsNextAction(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(TestBloodOathBuffsNextAction));
            Console.WriteLine("\n--- BLOOD OATH -> SLAM: +60% damage lands on SLAM ---");
            var plain = SlamAfter("BLOOD OATH", false, out _, out _);
            var buffed = SlamAfter("BLOOD OATH", true, out _, out _);
            if (plain == null || buffed == null)
            {
                TestBase.AssertTrue(false, "BLOOD OATH and SLAM load", ref run, ref passed, ref failed);
                return;
            }
            Console.WriteLine($"  plain SLAM {plain.Damage}, after BLOOD OATH {buffed.Damage} (traced +{buffed.TracedMod}%)");
            TestBase.AssertTrue(buffed.Hit && buffed.Combo, "SLAM after BLOOD OATH lands its combo roll", ref run, ref passed, ref failed);
            TestBase.AssertTrue(Math.Abs(buffed.TracedMod - 60) < 0.001,
                $"damage trace shows the banked +60% ({buffed.TracedMod})", ref run, ref passed, ref failed);
            TestBase.AssertTrue(buffed.Damage > plain.Damage,
                $"SLAM after BLOOD OATH deals more damage ({buffed.Damage} vs {plain.Damage})", ref run, ref passed, ref failed);
        }

        /// <summary>The HP cost is paid by the combat effects pass inside a real swing, not only by calling ActionCost.</summary>
        private static void TestBloodOathPaysHp(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(TestBloodOathPaysHp));
            var swing = SlamAfter("BLOOD OATH", true, out var hero, out int paid);
            if (swing == null)
            {
                TestBase.AssertTrue(false, "BLOOD OATH loads", ref run, ref passed, ref failed);
                return;
            }
            int expected = (int)Math.Round(hero.GetEffectiveMaxHealth() * 5 / 100.0);
            Console.WriteLine($"  BLOOD OATH swing paid {paid} HP (expected {expected})");
            TestBase.AssertEqual(expected, paid, "BLOOD OATH swing pays 5% max HP", ref run, ref passed, ref failed);
            var plain = SlamAfter("PRELUDE", true, out _, out int preludePaid);
            TestBase.AssertEqual(0, preludePaid, "PRELUDE (no cost) pays no HP", ref run, ref passed, ref failed);
        }

        private static void TestPreludeAmpsNextThreeSlots(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(TestPreludeAmpsNextThreeSlots));
            Console.WriteLine("\n--- PRELUDE -> SLAM: +15% AMP on the next 3 actions ---");
            var plain = SlamAfter("PRELUDE", false, out _, out _);
            var buffed = SlamAfter("PRELUDE", true, out var hero, out _);
            if (plain == null || buffed == null)
            {
                TestBase.AssertTrue(false, "PRELUDE and SLAM load", ref run, ref passed, ref failed);
                return;
            }
            Console.WriteLine($"  plain SLAM {plain.Damage} (amp {plain.Amp:F3}), after PRELUDE {buffed.Damage} (amp {buffed.Amp:F3})");
            TestBase.AssertTrue(buffed.Hit && buffed.Combo, "SLAM after PRELUDE lands its combo roll", ref run, ref passed, ref failed);
            TestBase.AssertTrue(buffed.Amp > plain.Amp,
                $"PRELUDE raises SLAM's AMP ({buffed.Amp:F3} vs {plain.Amp:F3})", ref run, ref passed, ref failed);
            TestBase.AssertTrue(buffed.Damage > plain.Damage,
                $"SLAM after PRELUDE deals more damage ({buffed.Damage} vs {plain.Damage})", ref run, ref passed, ref failed);
            TestBase.AssertTrue(hero.Effects.GetPendingActionBonusesForSlot(2).Count > 0 && hero.Effects.GetPendingActionBonusesForSlot(3).Count > 0,
                "PRELUDE still holds AMP for strip slots 3 and 4", ref run, ref passed, ref failed);
        }
    }
}
