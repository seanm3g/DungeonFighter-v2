using System;
using System.Linq;
using RPGGame;
using RPGGame.Combat.Events;
using RPGGame.Combat.Narrative;
using RPGGame.Tests;
using RPGGame.UI;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>NEW cards (D9.3): the board card for a first-seen status and the loot reveal's mark.</summary>
    public static class NewConceptCardsTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== NewConceptCards Tests ===\n");
            int run = 0, passed = 0, failed = 0;
            NewConceptCards.Dismiss();
            NewConceptCards.ClearMet();

            TestBase.AssertEqual("NEW · Rattle: your next rolls are lower.", NewConceptCards.CardText("rattle", onHero: true),
                "Rattle on you reads like the design's example", ref run, ref passed, ref failed);
            TestBase.AssertEqual("NEW · Stagger: its next swing comes later.", NewConceptCards.CardText("stagger", onHero: false),
                "a status on the enemy says 'its'", ref run, ref passed, ref failed);
            TestBase.AssertEqual("NEW · Guard: you take less damage.", NewConceptCards.CardText("harden", onHero: true),
                "the harden store shows as Guard", ref run, ref passed, ref failed);
            TestBase.AssertEqual("NEW · Bleed: it loses HP each time it acts.", NewConceptCards.CardText("poison", onHero: false),
                "a retired id resolves to its replacement", ref run, ref passed, ref failed);
            foreach (string id in CoreStatuses.Core.Concat(CoreStatuses.Kept))
                TestBase.AssertTrue(NewConceptCards.Sentence(id, true).Length > 0 && NewConceptCards.Sentence(id, false).Length > 0,
                    $"{id} has a sentence both ways", ref run, ref passed, ref failed);

            // The first status in a fight puts the card up (through Fernis's session), once.
            var hero = TestDataBuilders.CreateTestCharacter("Hero", 5);
            var enemy = MockFactories.CreateMockEnemy("Ghoul", 5, 100);
            var session = new FernisFightSession(hero, enemy, subscribe: false);
            session.OnEvent(new CombatEvent(CombatEventType.StatusEffectApplied, enemy) { Target = hero, StatusEffectType = "rollpenalty" });
            var card = NewConceptCards.Current;
            TestBase.AssertTrue(card != null && card.StatusId == "rattle" && card.Text == "NEW · Rattle: your next rolls are lower." && ReferenceEquals(card.Source, enemy),
                $"the enemy's first Rattle on you puts up its NEW card, got: {card?.Text}", ref run, ref passed, ref failed);
            NewConceptCards.Dismiss();
            session.OnEvent(new CombatEvent(CombatEventType.StatusEffectApplied, enemy) { Target = hero, StatusEffectType = "rattle" });
            TestBase.AssertTrue(NewConceptCards.Current == null, "a status already seen gets no second card", ref run, ref passed, ref failed);

            // Visible for a few seconds, fading at the end.
            var at = new DateTimeOffset(2026, 11, 22, 12, 0, 0, TimeSpan.Zero);
            NewConceptCards.Show("bleed", enemy, hero, hero, at);
            TestBase.AssertTrue(NewConceptCards.Visible(at.AddSeconds(1)) is { Alpha: 1.0 }, "shown at full strength", ref run, ref passed, ref failed);
            TestBase.AssertTrue(NewConceptCards.Visible(at.AddSeconds(NewConceptCards.ShowSeconds - 0.5)) is { Alpha: > 0 and < 1 }, "fades over its last second", ref run, ref passed, ref failed);
            TestBase.AssertTrue(NewConceptCards.Visible(at.AddSeconds(NewConceptCards.ShowSeconds + 1)) == null, "then leaves the board", ref run, ref passed, ref failed);
            TestBase.AssertTrue(NewConceptCards.Current!.Text.StartsWith("NEW · Bleed: it", StringComparison.Ordinal), "the hero's Bleed on the enemy says 'it'", ref run, ref passed, ref failed);

            // The loot that answers it.
            TestBase.AssertTrue(NewConceptCards.MetSinceLoot.Contains("rattle") && NewConceptCards.MetSinceLoot.Contains("bleed"), "met statuses are kept for the loot reveal", ref run, ref passed, ref failed);
            var bleeder = new WeaponItem("Test Knife", 1, 5, 1.0) { ImplicitRule = "ONCONNECT -> APPLY bleed 1" };
            TestBase.AssertTrue(NewConceptCards.StatusesApplied(bleeder).Contains("bleed"), "an item rule that applies Bleed is found", ref run, ref passed, ref failed);
            string? line = NewConceptCards.LootLine(bleeder);
            TestBase.AssertTrue(line != null && line.StartsWith("NEW · applies Bleed", StringComparison.Ordinal), $"the loot reveal marks it, got: {line}", ref run, ref passed, ref failed);
            var plain = new WeaponItem("Plain Stick", 1, 5, 1.0);
            TestBase.AssertTrue(NewConceptCards.LootLine(plain) == null, "an item that applies nothing met gets no mark", ref run, ref passed, ref failed);
            NewConceptCards.ClearMet();
            TestBase.AssertTrue(NewConceptCards.LootLine(bleeder) == null, "after the reveal the mark is spent", ref run, ref passed, ref failed);

            // Board text wrapping.
            var rows = BoardText.Wrap("NEW · Rattle: your next rolls are lower.", 16, 4);
            TestBase.AssertTrue(rows.Count >= 2 && rows.All(r => r.Length <= 16), $"wraps to the card width, got: {string.Join(" / ", rows)}", ref run, ref passed, ref failed);
            var cut = BoardText.Wrap("one two three four five six seven", 9, 2);
            TestBase.AssertTrue(cut.Count == 2 && cut[1].EndsWith("...", StringComparison.Ordinal) && cut.All(r => r.Length <= 9), $"too long ends with ..., got: {string.Join(" / ", cut)}", ref run, ref passed, ref failed);

            NewConceptCards.Dismiss();
            TestBase.PrintSummary("NewConceptCards Tests", run, passed, failed);
        }
    }
}
