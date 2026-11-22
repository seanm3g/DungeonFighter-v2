using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Data;
using RPGGame.Tests;
using RPGGame.UI;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>Strip-editor arrows (Phase 5 proposal 7, step 3.1): <see cref="StripLinks"/>.</summary>
    public static class StripLinksTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== StripLinks Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            static Action Plain(string name) => new Action { Name = name, IsComboAction = true };
            static Action Granting(string name, string cadence = "ACTION", int count = 1, CadenceTiming? timing = null) => new Action
            {
                Name = name,
                IsComboAction = true,
                ActionAttackBonuses = new ActionAttackBonuses
                {
                    BonusGroups = new List<ActionAttackBonusGroup>
                    {
                        new()
                        {
                            CadenceType = cadence, Count = count, Timing = timing,
                            Bonuses = new List<ActionAttackBonusItem> { new() { Type = "DAMAGE_MOD", Value = 25 } }
                        }
                    }
                }
            };
            string Show(List<StripLink> links) => string.Join(",", links.Select(l => $"{l.From + 1}>{l.To + 1}"));

            var next = StripLinks.Build(new[] { Granting("ZZ Link A"), Plain("ZZ B"), Plain("ZZ C") });
            TestBase.AssertEqual("1>2", Show(next), "next action: an arrow to the next card", ref run, ref passed, ref failed);
            TestBase.AssertTrue(next.Count == 1 && next[0].Label.Length > 0, "the arrow carries the grant's text", ref run, ref passed, ref failed);

            TestBase.AssertEqual("1>2,1>3", Show(StripLinks.Build(new[] { Granting("ZZ Link N", count: 2), Plain("ZZ B"), Plain("ZZ C") })),
                "next 2 actions: one arrow each", ref run, ref passed, ref failed);
            TestBase.AssertEqual("2>3", Show(StripLinks.Build(new[] { Plain("ZZ A"), Granting("ZZ Link Clip", count: 3), Plain("ZZ C") })),
                "next N is clipped at the strip end", ref run, ref passed, ref failed);
            TestBase.AssertEqual("3>1", Show(StripLinks.Build(new[] { Plain("ZZ A"), Plain("ZZ B"), Granting("ZZ Link Fin") })),
                "a finisher's next-action grant carries to the opener", ref run, ref passed, ref failed);
            TestBase.AssertEqual("", Show(StripLinks.Build(new[] { Plain("ZZ A"), Plain("ZZ B"), Granting("ZZ Link Fin2", count: 2) })),
                "a finisher's next-2 grant lands nowhere", ref run, ref passed, ref failed);
            TestBase.AssertEqual("1>3", Show(StripLinks.Build(new[] { Granting("ZZ Link Last", timing: new CadenceTiming { Slot = "last" }), Plain("ZZ B"), Plain("ZZ C") })),
                "slot: last points at the last card", ref run, ref passed, ref failed);
            TestBase.AssertEqual("1>3", Show(StripLinks.Build(new[] { Granting("ZZ Link Delay", timing: new CadenceTiming { Delay = 1 }), Plain("ZZ B"), Plain("ZZ C") })),
                "delay 1 skips a card", ref run, ref passed, ref failed);
            TestBase.AssertEqual("1>2", Show(StripLinks.Build(new[] { Granting("ZZ Link Charge", count: 3, timing: new CadenceTiming { Shape = "charge" }), Plain("ZZ B"), Plain("ZZ C") })),
                "charge puts every copy on the next card", ref run, ref passed, ref failed);
            TestBase.AssertEqual("", Show(StripLinks.Build(new[] { Granting("ZZ Link If", timing: new CadenceTiming { SlotIf = "last" }), Plain("ZZ B"), Plain("ZZ C") })),
                "slotIf that doesn't hold: no arrow", ref run, ref passed, ref failed);
            TestBase.AssertEqual("", Show(StripLinks.Build(new[] { Granting("ZZ Link Turn", cadence: "TURN", count: 2), Plain("ZZ B") })),
                "TURN grants follow the rolls, not a slot: no arrow", ref run, ref passed, ref failed);
            TestBase.AssertEqual("", Show(StripLinks.Build(new[] { Granting("ZZ Link Solo") })), "a one-card strip has no arrows", ref run, ref passed, ref failed);

            TestBase.PrintSummary("StripLinks Tests", run, passed, failed);
        }
    }
}
