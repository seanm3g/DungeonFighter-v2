using System;
using System.Collections.Generic;
using RPGGame.Combat.Events;

namespace RPGGame.Tests.Unit.Rebuild
{
    /// <summary>Rebuild 1.5 (D2.5): a crit amplifies the action's statuses; a crit miss is a miss with 2× recovery.</summary>
    public static class CritAmplifiesEffectsTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== CritAmplifiesEffects Tests ===\n");
            _run = _passed = _failed = 0;
            var combat = GameConfiguration.Instance.Combat;
            bool old = combat.CritAmplifiesEffects;
            try
            {
                combat.CritAmplifiesEffects = true;
                TestBase.AssertTrue(combat.CritEffectApplications >= 2, $"A crit applies statuses at least twice ({combat.CritEffectApplications})", ref _run, ref _passed, ref _failed);
                int normal = ExposeAfter(critical: false);
                int crit = ExposeAfter(critical: true);
                TestBase.AssertEqual(1, normal, "A normal hit applies 1 Expose", ref _run, ref _passed, ref _failed);
                TestBase.AssertEqual(Math.Min(combat.CritEffectApplications, Math.Max(1, combat.StatusMaxStacks)), crit,
                    "A crit applies Expose once per crit application", ref _run, ref _passed, ref _failed);

                combat.CritAmplifiesEffects = false;
                TestBase.AssertEqual(1, ExposeAfter(critical: true), "Switch off: a crit applies 1 Expose", ref _run, ref _passed, ref _failed);
            }
            finally
            {
                combat.CritAmplifiesEffects = old;
            }

            // Crit miss: miss + 2× recovery (already built: Actor.HasCriticalMissPenalty doubles the action length).
            var hero = TestDataBuilders.Character().WithName("Fumbler").Build();
            var swing = new Action("FUMBLE", comboOrder: 0, damageMultiplier: 1.0, length: 1.0, isComboAction: false);
            double plain = ActionSpeedSystemLength(hero, swing, critMiss: false);
            double fumble = ActionSpeedSystemLength(hero, swing, critMiss: true);
            TestBase.AssertTrue(plain > 0 && Math.Abs(fumble - 2 * plain) < 1e-6, $"A crit miss takes 2× recovery ({plain:0.###} → {fumble:0.###})", ref _run, ref _passed, ref _failed);

            TestBase.PrintSummary("CritAmplifiesEffects Tests", _run, _passed, _failed);
        }

        private static int ExposeAfter(bool critical)
        {
            var hero = TestDataBuilders.Character().WithName("Critter").Build();
            var foe = TestDataBuilders.Enemy().WithName("Dummy").Build();
            var action = new Action("EXPOSE TEST", comboOrder: 0, damageMultiplier: 1.0, length: 1.0, isComboAction: true) { CausesExpose = true };
            var ev = new CombatEvent(CombatEventType.ActionHit, hero) { Target = foe, Action = action, IsCritical = critical, IsMiss = false, NaturalRollValue = critical ? 20 : 15 };
            CombatEffectsSimplified.ApplyStatusEffects(action, hero, foe, new List<string>(), ev);
            return foe.ExposeStacks ?? 0;
        }

        private static double ActionSpeedSystemLength(Character hero, Action action, bool critMiss) =>
            RPGGame.Combat.Formatting.ActionSpeedCalculator.CalculateActualActionSpeed(hero, action, critMiss);
    }
}
