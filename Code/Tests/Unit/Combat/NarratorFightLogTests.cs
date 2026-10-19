using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Combat.Sequence;
using RPGGame.Tests;
using RPGGame.UI.BlockDisplay;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>
    /// End-to-end proof that the overseer reaches the F7-OFF combat log.
    ///
    /// Every previous session verified the narrator by asserting on a FUNCTION - does
    /// AnalyzeEvent return a line, does BuildFlavorLines return a list. Four times that was
    /// declared working and four times nothing appeared on Joey's screen, because the gap was
    /// never in the function. It was in whether anything downstream rendered what the function
    /// returned, and in whether the shipped config even opened the gate.
    ///
    /// So this suite does not stop at BuildFlavorLines. It runs a realistic multi-swing fight
    /// and drives the exact call the live display path makes -
    /// <see cref="BlockMessageCollector.CollectActionBlockMessages"/> with the flavor lines in
    /// the narratives slot - which is the list BlockDisplayManager hands straight to the
    /// renderer. It then prints every group as plain text, so the log in the test output is
    /// the log on screen.
    ///
    /// It also reads the REAL GameData/NarrativePacing.json rather than an override, because
    /// the thing that actually kept the narrator off screen was a committed config value, and
    /// a test that overrides the config cannot catch that.
    /// </summary>
    public static class NarratorFightLogTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Narrator Fight Log Tests ===\n");
            _run = _passed = _failed = 0;

            ShippedConfigOpensTheNarratorGate();
            FullFightLogShowsBothVoices();
            EnvironmentalBeatSurvivesToTheBlock();

            TestBase.PrintSummary("Narrator Fight Log Tests", _run, _passed, _failed);
        }

        /// <summary>
        /// The committed config, not an override. If this fails, the narrator is off for every
        /// player regardless of how good the banks are.
        /// </summary>
        private static void ShippedConfigOpensTheNarratorGate()
        {
            Console.WriteLine("--- The shipped config lets the narrator speak ---");
            NarrativePacing.Invalidate();
            var profile = NarrativePacing.Active;

            Console.WriteLine($"    [config] narratorEnabled={profile.NarratorEnabled} "
                              + $"swingChance={profile.SwingChance} "
                              + $"maxSentences={profile.MaxSentencesPerSwing} "
                              + $"paragraphTargetMs={profile.ParagraphTargetMs}");

            TestBase.AssertTrue(profile.NarratorEnabled,
                "GameData/NarrativePacing.json must resolve to a profile with narratorEnabled "
                + "- otherwise TryGetFlavorLine returns at its first check and no bank is ever read",
                ref _run, ref _passed, ref _failed);
        }

        /// <summary>
        /// A fight that lasts long enough to cross the thresholds, rendered the way the overview
        /// view renders it.
        /// </summary>
        private static void FullFightLogShowsBothVoices()
        {
            Console.WriteLine("\n--- A whole fight, as the overview log paints it ---");
            NarrativePacing.Invalidate();

            const string Hero = "Michael Ironthorn";
            const string Foe = "Magma Beast";
            var narrative = new BattleNarrative(Hero, Foe, "Echo Chamber", 250, 150);

            // A plausible fight: trade blows, a crit, the enemy drops past both thresholds,
            // then dies. Health is driven by the damage numbers, the way AddEvent does it live.
            var swings = new List<BattleEvent>
            {
                Swing(Hero, Foe, "STRIKE", 13, roll: 10),
                Swing(Foe, Hero, "SLAM", 25, roll: 17),
                Swing(Hero, Foe, "SLASH", 18, roll: 20, critical: true),
                Swing(Foe, Hero, "SLAM", 26, roll: 14),
                Swing(Hero, Foe, "STRIKE", 17, roll: 19, combo: true, comboStep: 2),
                Swing(Foe, Hero, "SLAM", 15, roll: 12),
                Swing(Hero, Foe, "SLASH", 25, roll: 19),
                Swing(Hero, Foe, "STRIKE", 30, roll: 18),
                Swing(Hero, Foe, "SLASH", 40, roll: 20, critical: true),
                Swing(Hero, Foe, "STRIKE", 60, roll: 16),
            };

            var narratorLines = new List<string>();
            var purpleLines = new List<string>();
            int swingNumber = 0;

            foreach (var swing in swings)
            {
                swingNumber++;
                narrative.AddEvent(swing);

                // Exactly what CombatTurnHandlerSimplified does for this swing.
                var flavorLines = NarrativeTierPresenter.BuildFlavorLines(narrative);

                // Exactly what BlockDisplayManager hands the renderer.
                var action = new ColoredTextBuilder()
                    .Add($"{swing.Actor} Attacks {swing.Target}... and hits for {swing.Damage} damage",
                         ColorPalette.Info)
                    .Build();
                var roll = new ColoredTextBuilder()
                    .Add($"    (roll: {swing.NaturalRoll})", ColorPalette.Gray)
                    .Build();

                var groups = BlockMessageCollector.CollectActionBlockMessages(
                    action, roll, null, null, flavorLines);

                Console.WriteLine($"\n  swing {swingNumber}:");
                foreach (var group in groups)
                {
                    string plain = ColoredTextRenderer.RenderAsPlainText(group.segments);
                    bool isPurple = group.segments.Any(s =>
                        !string.IsNullOrWhiteSpace(s.Text)
                        && s.Color.Equals(ColorPalette.Purple.GetColor()));
                    Console.WriteLine($"    {(isPurple ? "[OVERSEER] " : "           ")}{plain}");
                }

                if (flavorLines == null)
                {
                    continue;
                }

                foreach (var line in flavorLines)
                {
                    string plain = ColoredTextRenderer.RenderAsPlainText(line).Trim();
                    if (string.IsNullOrWhiteSpace(plain))
                        continue;

                    narratorLines.Add(plain);
                    if (line.Any(s => !string.IsNullOrWhiteSpace(s.Text)
                                      && s.Color.Equals(ColorPalette.Purple.GetColor())))
                    {
                        purpleLines.Add(plain);
                    }
                }
            }

            Console.WriteLine();

            TestBase.AssertTrue(narratorLines.Count > 0,
                "A ten-swing fight must put at least one narrator line in the log",
                ref _run, ref _passed, ref _failed);

            TestBase.AssertTrue(purpleLines.Count > 0,
                "A fight with two crits and a death must produce at least one purple set piece",
                ref _run, ref _passed, ref _failed);

            // Decision 4: the log is a ledger with highlights, not a line every swing. If the
            // narrator spoke on all ten, the contrast the whole tier model exists for is gone.
            TestBase.AssertTrue(narratorLines.Count < swings.Count,
                $"The narrator must stay quiet on some swings, spoke on {narratorLines.Count}/{swings.Count}",
                ref _run, ref _passed, ref _failed);

            // Rule 6 and decision 14, checked against what actually rendered rather than
            // against the bank - a token that failed to substitute would slip past a bank read.
            foreach (string line in narratorLines)
            {
                TestBase.AssertTrue(!line.Contains('{') && !line.Contains('}'),
                    $"No raw token may reach the screen, got: {line}",
                    ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(!line.Contains('!'),
                    $"No exclamation marks reach the screen, got: {line}",
                    ref _run, ref _passed, ref _failed);
            }

            Console.WriteLine($"    [summary] {narratorLines.Count} narrator lines across "
                              + $"{swings.Count} swings, {purpleLines.Count} of them set pieces");
        }

        /// <summary>
        /// The environmental beat has one swing to be collected, and it used to get none.
        /// </summary>
        private static void EnvironmentalBeatSurvivesToTheBlock()
        {
            Console.WriteLine("\n--- The environmental beat reaches the block it fires on ---");
            NarrativePacing.Invalidate();

            const string Hero = "Michael Ironthorn";
            const string Foe = "Magma Beast";
            var narrative = new BattleNarrative(Hero, Foe, "Echo Chamber", 250, 150);

            narrative.AddEvent(Swing(Hero, Foe, "STRIKE", 13, roll: 10));

            // What CombatTurnHandlerSimplified passes: the rendered mechanical block as one
            // newline-joined string. This is also why no bank line may contain {effect}.
            narrative.AddEnvironmentalAction(
                "The ground gives way beneath you! You take 2 damage!\n    (roll: 14)");

            var flavorLines = NarrativeTierPresenter.BuildFlavorLines(narrative);
            TestBase.AssertTrue(flavorLines != null && flavorLines.Count > 0,
                "The environmental swing must produce a flavor line to pass to the block",
                ref _run, ref _passed, ref _failed);

            if (flavorLines == null)
            {
                return;
            }

            var action = new ColoredTextBuilder()
                .Add("The ground gives way beneath you! You take 2 damage!", ColorPalette.Cyan)
                .Build();

            var groups = BlockMessageCollector.CollectActionBlockMessages(
                action, new List<ColoredText>(), null, null, flavorLines,
                TextSpacingSystem.BlockType.EnvironmentalAction);

            string all = string.Join(" | ",
                groups.Select(g => ColoredTextRenderer.RenderAsPlainText(g.segments)));
            Console.WriteLine($"    [block] {all}");

            foreach (var line in flavorLines)
            {
                string plain = ColoredTextRenderer.RenderAsPlainText(line).Trim();
                if (string.IsNullOrWhiteSpace(plain))
                    continue;

                Console.WriteLine($"    [beat ] {plain}");

                TestBase.AssertTrue(
                    all.Contains(plain, StringComparison.Ordinal),
                    $"The environmental beat must survive into the rendered block, got: {plain}",
                    ref _run, ref _passed, ref _failed);

                // The regression that made this bank unusable: {effect} is the whole mechanical
                // readout, so a line carrying it reprints the block inside itself.
                TestBase.AssertTrue(
                    !plain.Contains("(roll:", StringComparison.Ordinal)
                        && !plain.Contains("damage!", StringComparison.Ordinal),
                    $"The beat must not carry the mechanical readout inside it, got: {plain}",
                    ref _run, ref _passed, ref _failed);
            }
        }

        private static BattleEvent Swing(
            string actor, string target, string action, int damage, int roll,
            bool critical = false, bool combo = false, int comboStep = 0)
        {
            return new BattleEvent
            {
                Actor = actor,
                Target = target,
                Action = action,
                Damage = damage,
                IsSuccess = true,
                IsCritical = critical,
                NaturalRoll = roll,
                Roll = roll,
                IsCombo = combo,
                ComboStep = comboStep
            };
        }
    }
}
