using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Data;
using RPGGame.Tests;
using RPGGame.UI;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>The NOW card's attached lines (D3.7, Phase 9 proposal 3): <see cref="NowCardContent"/>.</summary>
    public static class NowCardContentTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== NowCardContent Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            var hero = TestDataBuilders.Character().WithName("NowHero").WithStats(10, 10, 10, 0).Build();
            var foe = new Enemy(name: "Ghoul", level: 1, maxHealth: 30, strength: 6, agility: 6, technique: 4, intelligence: 4, armor: 0);
            var card = new Action { Name = "STAB", IsComboAction = true };

            var empty = NowCardContent.Build(hero, foe, card, 0);
            TestBase.AssertEqual(0, empty.Count, $"nothing attached: no lines, got: {Join(empty)}", ref run, ref passed, ref failed);

            // Statuses on the swinger and on its target.
            hero.FocusStacks = 2; hero.FocusTurns = 3;
            hero.ApplyRollPenalty(1, 2);
            hero.HasteStacks = 1;
            foe.ExposeStacks = 1; foe.ExposeTurns = 2;
            foe.IsMarked = true; foe.MarkTurns = 2;
            foe.HardenStacks = 1; foe.HardenTurns = 2;
            var statuses = NowCardContent.Build(hero, foe, card, 0);
            TestBase.AssertTrue(statuses.Any(l => l.Kind == NowCardLineKind.Status && l.Text.StartsWith("Focus 2: +", StringComparison.Ordinal) && l.Text.EndsWith("damage", StringComparison.Ordinal)),
                $"Focus on the swinger shows its damage bonus, got: {Join(statuses)}", ref run, ref passed, ref failed);
            TestBase.AssertTrue(statuses.Any(l => l.Text == "Rattle 1: -1 to the roll"), "Rattle shows the roll penalty", ref run, ref passed, ref failed);
            TestBase.AssertTrue(statuses.Any(l => l.Text.StartsWith("Haste 1: this action is", StringComparison.Ordinal)), "Haste shows on this action", ref run, ref passed, ref failed);
            TestBase.AssertTrue(statuses.Any(l => l.Text == "Mark on Ghoul: a hit crits"), "Mark on the target", ref run, ref passed, ref failed);
            TestBase.AssertTrue(statuses.Any(l => l.Text == "Expose 1 on Ghoul: it takes more damage"), "Expose on the target", ref run, ref passed, ref failed);
            TestBase.AssertTrue(statuses.Any(l => l.Text == "Guard 1 on Ghoul: it takes less damage"), "Guard on the target", ref run, ref passed, ref failed);
            TestBase.AssertTrue(!statuses.Any(l => l.Text.Contains("NowHero", StringComparison.Ordinal)), "the swinger's own statuses don't name it", ref run, ref passed, ref failed);

            // Queued grants: only this slot's bank, plus the next-action bank and TURN grants.
            var granted = TestDataBuilders.Character().WithName("GrantHero").WithStats(10, 10, 10, 0).Build();
            granted.Effects.AddPendingActionBonuses(1, new List<ActionAttackBonusItem> { new() { Type = "COMBO", Value = 1 } });
            granted.Effects.AddPendingActionBonuses(3, new List<ActionAttackBonusItem> { new() { Type = "HIT", Value = 2 } });
            granted.Effects.AddPendingActionBonusesNextHeroRoll(new List<ActionAttackBonusItem> { new() { Type = "ACCURACY", Value = 2 } });
            granted.Effects.TurnBonuses.Add(new ActionAttackBonusGroup
            {
                CadenceType = "TURN",
                Count = 2,
                Bonuses = new List<ActionAttackBonusItem> { new() { Type = "HIT", Value = 1 } }
            });
            granted.Effects.RerollCharges = 1;
            var slot2 = NowCardContent.Build(granted, foe, card, 1);
            TestBase.AssertTrue(slot2.Any(l => l.Kind == NowCardLineKind.Grant && l.Text.StartsWith("Queued for slot 2:", StringComparison.Ordinal)),
                $"slot 2's bank shows on slot 2, got: {Join(slot2)}", ref run, ref passed, ref failed);
            TestBase.AssertTrue(!slot2.Any(l => l.Text.StartsWith("Queued for slot 4", StringComparison.Ordinal)), "another slot's bank doesn't", ref run, ref passed, ref failed);
            TestBase.AssertTrue(slot2.Any(l => l.Text.StartsWith("Next action:", StringComparison.Ordinal)), "next-action bank shows", ref run, ref passed, ref failed);
            TestBase.AssertTrue(slot2.Any(l => l.Text.StartsWith("TURN:", StringComparison.Ordinal)), "TURN grants show", ref run, ref passed, ref failed);
            TestBase.AssertTrue(slot2.Any(l => l.Text == "Reroll x1"), "reroll charges show", ref run, ref passed, ref failed);
            var slot4 = NowCardContent.Build(granted, foe, card, 3);
            TestBase.AssertTrue(slot4.Any(l => l.Text.StartsWith("Queued for slot 4:", StringComparison.Ordinal)), "slot 4's bank on slot 4", ref run, ref passed, ref failed);

            // Stores: a CASH card names what it spends; held stores are listed.
            var cashHero = TestDataBuilders.Character().WithName("CashHero").WithStats(10, 10, 10, 0).Build();
            cashHero.Effects.AddMaterialKeyword("RAGE", 3);
            var cashCard = new Action { Name = "CASH IN", IsComboAction = true, Verb = "CASH FULLEST 75" };
            var cash = NowCardContent.Build(cashHero, foe, cashCard, 0);
            TestBase.AssertTrue(cash.Any(l => l.Kind == NowCardLineKind.Store && l.Text.StartsWith("Spends", StringComparison.Ordinal) && l.Text.Contains("3") && l.Text.EndsWith("at 75%", StringComparison.Ordinal)),
                $"a cash card shows the store it spends, got: {Join(cash)}", ref run, ref passed, ref failed);
            var plain = NowCardContent.Build(cashHero, foe, card, 0);
            TestBase.AssertTrue(plain.Any(l => l.Text == "Stores: RAGE 3"), $"held stores show on a plain card, got: {Join(plain)}", ref run, ref passed, ref failed);
            TestBase.AssertTrue(!plain.Any(l => l.Text.StartsWith("Spends", StringComparison.Ordinal)), "a plain card spends nothing", ref run, ref passed, ref failed);

            // Odds of the next link.
            var odds = NowCardContent.Build(cashHero, foe, card, 0, nextLinkFace: 14);
            TestBase.AssertTrue(odds.Count > 0 && odds[^1].Kind == NowCardLineKind.Odds && odds[^1].Text == "Next link: 35% (14+)",
                $"next-link odds come last, got: {Join(odds)}", ref run, ref passed, ref failed);
            TestBase.AssertEqual("Next link: 5% (20+)", NowCardContent.NextLinkLine(20), "only a 20 links", ref run, ref passed, ref failed);
            TestBase.AssertEqual("Next link: 100% (1+)", NowCardContent.NextLinkLine(1), "every face links", ref run, ref passed, ref failed);
            TestBase.AssertEqual("Next link: 0% (can't link)", NowCardContent.NextLinkLine(21), "no face links", ref run, ref passed, ref failed);

            TestBase.PrintSummary("NowCardContent Tests", run, passed, failed);
        }

        private static string Join(List<NowCardLine> lines) => string.Join(" | ", lines.Select(l => l.Text));
    }
}
