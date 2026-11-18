using System;
using System.Linq;
using RPGGame.Combat.Sequence;
using RPGGame.UI;

namespace RPGGame.Tests.Unit.Rebuild
{
    /// <summary>Rebuild 1.1: the board's odds (D2.7), escalation (D0.1, D0.11, D9.6) and roll ruler (D2.2).</summary>
    public static class ComboBoardModelTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== ComboBoardModel Tests ===\n");
            _run = _passed = _failed = 0;

            // Odds on a bare d20: 14+ is 7 faces (35%), 15+ 30%, a natural 20 always lands, a natural 1 never does.
            Close(0.35, ComboBoardModel.Chance(14), "needs 14+ on a bare d20 = 35%");
            Close(0.30, ComboBoardModel.Chance(15), "needs 15+ = 30%");
            Close(0.05, ComboBoardModel.Chance(25), "a natural 20 always lands (5%)");
            Close(0.95, ComboBoardModel.Chance(1, rollBonus: 10), "a natural 1 never lands (95% cap)");
            Close(0.45, ComboBoardModel.Chance(14, rollBonus: 2), "+2 nudge: 14+ = 45%");
            Close(1 - 0.65 * 0.65, ComboBoardModel.Chance(14, advantage: true), "2d20 keep high: 1 − 0.65²");
            TestBase.AssertEqual("35%", ComboBoardModel.FormatPercent(0.35), "Percent format", ref _run, ref _passed, ref _failed);

            var hero = TestDataBuilders.Character().WithName("Odds").Build();
            int s1 = ComboBoardModel.NeededForSlot(hero, 0), s2 = ComboBoardModel.NeededForSlot(hero, 1), s3 = ComboBoardModel.NeededForSlot(hero, 2);
            TestBase.AssertTrue(s2 == s1 + 1 && s3 == s1 + 2, $"Each link needs +1 (D2.9): {s1}, {s2}, {s3}", ref _run, ref _passed, ref _failed);
            string line = ComboBoardModel.OddsLine(hero, 1, 0);
            TestBase.AssertTrue(line.StartsWith($"needs {s2}+ · ") && line.EndsWith("%"), $"Card line reads \"{line}\"", ref _run, ref _passed, ref _failed);

            // Escalation: the hero's lit cards grow 1 → 4; the enemy's only 1 → 2 and take less gold.
            TestBase.AssertEqual(1, ComboBoardModel.LitBorderThickness(0, 5, enemy: false), "Hero link 1 is thin", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(4, ComboBoardModel.LitBorderThickness(4, 5, enemy: false), "Hero link 5 is thickest", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(2, ComboBoardModel.LitBorderThickness(4, 5, enemy: true), "Enemy escalation is quieter", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(ComboBoardModel.LitGoldShare(4, 5, enemy: true) < ComboBoardModel.LitGoldShare(4, 5, enemy: false), "Enemy cards take less gold", ref _run, ref _passed, ref _failed);

            // Roll ruler: five bands, the landed one marked.
            var t = new CombatSequenceThresholds(Crit: 20, Combo: 14, MinHit: 6, CritMiss: 1);
            var bands = ComboBoardModel.Ruler(t, 16);
            TestBase.AssertEqual("1|2–5|6–13|14+|20", string.Join("|", bands.Select(b => b.Label)), "Five bands with labels", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(bands.Single(b => b.Marked).Band == ComboBoardModel.RulerBand.Combo, "16 lands in the action band", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(ComboBoardModel.Ruler(t, 20).Single(b => b.Marked).Band == ComboBoardModel.RulerBand.Crit, "20 is a crit", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(ComboBoardModel.Ruler(t, 1).Single(b => b.Marked).Band == ComboBoardModel.RulerBand.CritMiss, "1 is a crit miss", ref _run, ref _passed, ref _failed);
            var midChain = ComboBoardModel.Ruler(new CombatSequenceThresholds(20, 15, 15, 1), 12);
            TestBase.AssertFalse(midChain.Any(b => b.Band == ComboBoardModel.RulerBand.Hit), "Mid-chain the HIT band is gone (D2.8)", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(midChain.Single(b => b.Marked).Band == ComboBoardModel.RulerBand.Miss, "12 mid-chain is a miss", ref _run, ref _passed, ref _failed);
            string text = ComboBoardModel.RulerText(t, 16, nudge: -2);
            TestBase.AssertTrue(text.Contains("[14+: 16]") && text.Contains("−2 nudge"), $"Ruler text: {text}", ref _run, ref _passed, ref _failed);

            TestBase.PrintSummary("ComboBoardModel Tests", _run, _passed, _failed);
        }

        private static void Close(double expected, double actual, string message) =>
            TestBase.AssertTrue(Math.Abs(expected - actual) < 1e-9, $"{message} (got {actual:0.####})", ref _run, ref _passed, ref _failed);
    }
}
