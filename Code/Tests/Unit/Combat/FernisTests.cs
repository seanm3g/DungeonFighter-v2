using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Combat.Events;
using RPGGame.Combat.Narrative;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>Fernis D Man and the swing moments (Rebuild step 3.2 / 3.3; D9.1, D9.5-D9.9).</summary>
    public static class FernisTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Fernis Tests ===\n");
            _run = _passed = _failed = 0;

            var bank = FernisLineBank.Load();
            var expected = new[]
            {
                FernisMoments.FightWon, FernisMoments.Comeback, FernisMoments.Death, FernisMoments.ChainComplete,
                FernisMoments.ChainBroken, FernisMoments.Overkill, FernisMoments.CloseCall, FernisMoments.Threat,
                FernisMoments.NewConcept, FernisMoments.DungeonSummary
            };
            var missing = expected.Where(m => bank.Pick(m, null, new Random(1)) == null).ToList();
            TestBase.AssertTrue(missing.Count == 0, $"the line bank has every moment (missing: {string.Join(", ", missing)})", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(bank.IsBig(FernisMoments.Death) && !bank.IsBig(FernisMoments.Overkill), "death is big, overkill is small", ref _run, ref _passed, ref _failed);

            // Every line fills with no brace left (tokens: hero enemy turns links cause hp status).
            var tokens = new Dictionary<string, string> { ["hero"] = "H", ["enemy"] = "E", ["turns"] = "3", ["links"] = "4", ["cause"] = "Rattle", ["hp"] = "9", ["status"] = "Bleed" };
            var leaks = bank.Moments.SelectMany(m => m.Value.Lines.Values.SelectMany(v => v))
                .Select(l => FernisLineBank.Fill(l, tokens)).Where(l => l.Contains('{') || l.Contains('}')).ToList();
            TestBase.AssertTrue(leaks.Count == 0, $"every line fills without braces ({leaks.Count} leak)", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("A b", FernisLineBank.Fill("a {nope} b", null), "unknown tokens drop; the line starts capitalized", ref _run, ref _passed, ref _failed);

            // Class lines join the pool: over many picks a Rogue hears a Rogue-only line.
            var rng = new Random(7);
            bool heardRogue = Enumerable.Range(0, 200).Select(_ => bank.Pick(FernisMoments.FightWon, "rogue", rng))
                .Any(l => l != null && l.Contains("thousand cuts", StringComparison.Ordinal));
            TestBase.AssertTrue(heardRogue, "class lines are in the class's pool", ref _run, ref _passed, ref _failed);

            // No repeats: a moment skips its recent lines while the pool has others.
            var fresh = FernisLineBank.Load();
            var picks = Enumerable.Range(0, 3).Select(_ => fresh.Pick(FernisMoments.Death, null, new Random(11))).ToList();
            TestBase.AssertTrue(picks.Distinct().Count() == 3, $"three picks of a 3-line moment are all different ({string.Join(" / ", picks)})", ref _run, ref _passed, ref _failed);

            // Budget: one line per swing; big moments always speak.
            var narrator = new FernisNarrator(bank, new Random(3));
            narrator.NewSwing();
            string? first = narrator.TrySay(FernisMoments.CloseCall, null);
            string? second = narrator.TrySay(FernisMoments.ChainComplete, null);
            TestBase.AssertTrue(first != null && second == null, "at most one line per swing; a big moment speaks", ref _run, ref _passed, ref _failed);
            narrator.NewSwing();
            TestBase.AssertNotNull(narrator.TrySay(FernisMoments.Death, null), "the next swing has budget again", ref _run, ref _passed, ref _failed);

            // Swing moments from bus events (no subscription; events fed directly).
            var hero = TestDataBuilders.CreateTestCharacter("Hero", 5);
            var enemy = MockFactories.CreateMockEnemy("Wolf", 5, 100);
            var session = new FernisFightSession(hero, enemy, new FernisNarrator(bank, new Random(5)), subscribe: false);
            session.OnEvent(new CombatEvent(CombatEventType.ActionExecuted, hero));
            session.OnEvent(new CombatEvent(CombatEventType.EnemyDied, hero) { Target = enemy, Damage = 1 });
            int afterSmallKill = session.Pending.Count;
            session.OnEvent(new CombatEvent(CombatEventType.ActionExecuted, hero));
            hero.CurrentHealth = Math.Max(1, hero.GetEffectiveMaxHealth() / 10);
            session.OnEvent(new CombatEvent(CombatEventType.HeroLowHealth, hero));
            TestBase.AssertTrue(afterSmallKill == 0 && session.Pending.Count == 1, "a small killing blow is no overkill; a close call speaks",
                ref _run, ref _passed, ref _failed);
            enemy.CurrentHealth = 0;
            session.FinishFight();
            var pending = session.Pending;
            // The result line takes the last swing's slot: the close-call line still queued is replaced, not doubled.
            TestBase.AssertTrue(pending.Count == 1 && bank.Moments[FernisMoments.Comeback].Lines["any"]
                    .Any(t => FernisLineBank.Fill(t, new Dictionary<string, string> { ["hp"] = "10" }).Length > 0),
                "a win after a close call ends on a comeback line", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(2, session.HeroSwings, "hero swings are counted for {turns}", ref _run, ref _passed, ref _failed);

            // Other fights' events are ignored.
            var stranger = TestDataBuilders.CreateTestCharacter("Stranger", 5);
            var quiet = new FernisFightSession(hero, enemy, new FernisNarrator(bank, new Random(5)), subscribe: false);
            quiet.OnEvent(new CombatEvent(CombatEventType.HeroLowHealth, stranger));
            TestBase.AssertEqual(0, quiet.HeroSwings, "events from another fight are ignored", ref _run, ref _passed, ref _failed);

            TestBase.AssertTrue(!FernisFightSession.Enabled || !RPGGame.Combat.CombatUiMuteScope.IsMuted, "Fernis is off in muted fights", ref _run, ref _passed, ref _failed);

            // Statuses these sessions met would otherwise mark items in later loot-reveal tests (D9.3).
            NewConceptCards.Dismiss();
            NewConceptCards.ClearMet();
            TestBase.PrintSummary("Fernis Tests", _run, _passed, _failed);
        }
    }
}
