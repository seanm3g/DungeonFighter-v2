using System;
using System.Collections.Generic;
using RPGGame.Combat.Events;
using RPGGame.Data;

namespace RPGGame.Tests.Unit.Rebuild
{
    /// <summary>Rebuild 1.5 + Q7 (D2.5): a crit amplifies the action's statuses and its other effects; a crit miss is a miss with 2× recovery.</summary>
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

            OtherEffects();

            // Crit miss: miss + 2× recovery (already built: Actor.HasCriticalMissPenalty doubles the action length).
            var hero = TestDataBuilders.Character().WithName("Fumbler").Build();
            var swing = new Action("FUMBLE", comboOrder: 0, damageMultiplier: 1.0, length: 1.0, isComboAction: false);
            double plain = ActionSpeedSystemLength(hero, swing, critMiss: false);
            double fumble = ActionSpeedSystemLength(hero, swing, critMiss: true);
            TestBase.AssertTrue(plain > 0 && Math.Abs(fumble - 2 * plain) < 1e-6, $"A crit miss takes 2× recovery ({plain:0.###} → {fumble:0.###})", ref _run, ref _passed, ref _failed);

            TestBase.PrintSummary("CritAmplifiesEffects Tests", _run, _passed, _failed);
        }

        /// <summary>Q7: inside a crit, heals, cadence grants, trigger bundles and rule amounts are amplified too.</summary>
        private static void OtherEffects()
        {
            var hero = TestDataBuilders.Character().WithName("Critter2").Build();
            var foe = TestDataBuilders.Enemy().WithName("Dummy2").Build();
            int apps = GameConfiguration.Instance.Combat.CritEffectApplications;
            TestBase.AssertEqual(1, CritAmplifier.For(hero), "Outside a crit: x1", ref _run, ref _passed, ref _failed);
            using (CritAmplifier.Begin(hero, isCritical: false))
                TestBase.AssertEqual(1, CritAmplifier.For(hero), "A normal hit: x1", ref _run, ref _passed, ref _failed);
            using (CritAmplifier.Begin(hero, isCritical: true))
            {
                TestBase.AssertEqual(apps, CritAmplifier.For(hero), $"Inside the hero's crit: x{apps}", ref _run, ref _passed, ref _failed);
                TestBase.AssertEqual(1, CritAmplifier.For(foe), "The defender's own effects aren't amplified", ref _run, ref _passed, ref _failed);
            }
            TestBase.AssertEqual(1, CritAmplifier.For(hero), "The scope ends with the swing", ref _run, ref _passed, ref _failed);

            // The action's own TURN grant (ACCURACY +2) lands doubled on a crit.
            var action = new Action("GRANT TEST", comboOrder: 0, damageMultiplier: 1.0, length: 1.0, isComboAction: false)
            {
                ActionAttackBonuses = new ActionAttackBonuses
                {
                    BonusGroups = new List<ActionAttackBonusGroup>
                    {
                        new ActionAttackBonusGroup { Keyword = "TURN", CadenceType = "TURN", Count = 1, DurationType = "TURN",
                            Bonuses = new List<ActionAttackBonusItem> { new ActionAttackBonusItem { Type = "ACCURACY", Value = 2 } } }
                    }
                }
            };
            double plain = GrantedAccuracy(action, critical: false);
            double crit = GrantedAccuracy(action, critical: true);
            TestBase.AssertTrue(Math.Abs(plain - 2) < 1e-9 && Math.Abs(crit - 2 * apps) < 1e-9, $"A crit amplifies cadence grants ({plain} -> {crit})", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(2.0, action.ActionAttackBonuses.BonusGroups[0].Bonuses[0].Value, "The action's data isn't changed", ref _run, ref _passed, ref _failed);

            CritAmplifier.AmplifiesOtherEffects = false;
            try
            {
                using (CritAmplifier.Begin(hero, isCritical: true))
                    TestBase.AssertEqual(1, CritAmplifier.For(hero), "Switch off: only damage and statuses (1.5)", ref _run, ref _passed, ref _failed);
            }
            finally { CritAmplifier.AmplifiesOtherEffects = true; }
        }

        private static double GrantedAccuracy(Action action, bool critical)
        {
            var hero = TestDataBuilders.Character().WithName(critical ? "GrantCrit" : "GrantPlain").Build();
            using (CritAmplifier.Begin(hero, critical))
                hero.Effects.AddActionAttackBonuses(action.ActionAttackBonuses, action, hero);
            double sum = 0;
            foreach (var b in hero.Effects.PeekTurnBonuses(action))
                if (b.Type == "ACCURACY") sum += b.Value;
            return sum;
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
