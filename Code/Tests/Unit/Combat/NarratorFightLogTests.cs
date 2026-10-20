using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Combat.Sequence;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Display;
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
            PlacementComesFromTheSwingNotTheWording();
            SetPiecesNeverLandOnConsecutiveSwings();
            EveryEnemyGetsAnOpeningLine();
            OpeningLinePrefersTheMostSpecificBank();

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

        /// <summary>
        /// Horizontal placement must be a property of WHOSE SWING IT WAS, never of how the line
        /// happens to be worded.
        /// </summary>
        private static void PlacementComesFromTheSwingNotTheWording()
        {
            Console.WriteLine("\n--- Placement follows the swing, not the first word ---");
            NarrativePacing.Invalidate();

            const string Hero = "Zarek Nebulaheart";
            const string Foe = "Ember Spirit";

            // The centre panel right-aligns a row when it is a continuation of an enemy block,
            // or when it is a primary row whose text starts with the enemy's name. The second
            // rule is the trap: these two lines describe the same enemy, and only one of them
            // opens with its name.
            var openingWithEnemyName = Line($"{Foe} put the whole fight through a gap.");
            var openingWithAnythingElse = Line($"Half of {Foe} is gone, and the guard did not leave with it.");

            foreach (var (label, line) in new[]
                     {
                         ("opens with the enemy name", openingWithEnemyName),
                         ("opens with another word  ", openingWithAnythingElse)
                     })
            {
                string plain = ColoredTextRenderer.RenderAsPlainText(line);
                bool treatedAsContinuation = plain.StartsWith(
                    BlockMessageCollector.ActionBlockSubsequentIndent, StringComparison.Ordinal);

                Console.WriteLine($"    [{label}] continuation={treatedAsContinuation}");

                TestBase.AssertTrue(treatedAsContinuation,
                    $"A narrator line ({label.Trim()}) must carry the continuation indent, or the "
                    + "centre panel falls back to sniffing its first word for the enemy's name",
                    ref _run, ref _passed, ref _failed);
            }

            // Both lines must land on the SAME side, because both describe the same swing. If
            // the name sniff is still reachable they diverge, which is the bug from the
            // screenshot: a set piece pinned right, a beat about the same enemy pinned left.
            var flags = CombatCenterPanelEnemyLineAlignment.ResolveRightAlignFlags(
                new List<List<ColoredText>>
                {
                    Line($"{Foe} Attacks {Hero}... and hits for 18 damage", indent: false),
                    openingWithEnemyName,
                    openingWithAnythingElse
                },
                new[] { Foe },
                Hero);

            Console.WriteLine($"    [flags] enemyBlock={flags[0]} line1={flags[1]} line2={flags[2]}");

            TestBase.AssertTrue(flags[0],
                "The enemy's own action line should right-align",
                ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(flags[1] == flags[2],
                $"Two narrator lines on the same enemy swing must land on the same side, "
                + $"got {flags[1]} and {flags[2]} - placement is still following the wording",
                ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(flags[1] && flags[2],
                "A narrator line on an enemy swing belongs on the enemy's side",
                ref _run, ref _passed, ref _failed);

            // And the mirror case: the same wording on a PLAYER swing must go left.
            var playerFlags = CombatCenterPanelEnemyLineAlignment.ResolveRightAlignFlags(
                new List<List<ColoredText>>
                {
                    Line($"{Hero} Attacks {Foe}... and hits for 25 damage", indent: false),
                    openingWithEnemyName
                },
                new[] { Foe },
                Hero);

            Console.WriteLine($"    [flags] playerBlock={playerFlags[0]} line={playerFlags[1]}");

            TestBase.AssertTrue(!playerFlags[0] && !playerFlags[1],
                "The identical line on a player swing must stay on the player's side",
                ref _run, ref _passed, ref _failed);
        }

        /// <summary>
        /// The ending that exposed this: the overseer promising to withhold, then resolving it
        /// one line later.
        ///
        ///   This is where it is decided for Skeleton, and I will not say which way.
        ///   The cold left the room slower than Skeleton did.
        ///
        /// below10Percent fires when the enemy crosses 10% and enemyDefeated fires when it dies,
        /// and those are nearly always a swing or two apart - so this is the NORMAL ending, not
        /// an edge case. Two adjacent purple lines are also a block of purple rather than the
        /// rare highlight decision 13 is built on.
        /// </summary>
        private static void SetPiecesNeverLandOnConsecutiveSwings()
        {
            Console.WriteLine("\n--- Set pieces never land on consecutive swings ---");
            NarrativePacing.Invalidate();

            const string Hero = "Alaric Mistwalker";
            const string Foe = "Skeleton";

            // Walk the enemy down through 50%, then through 10%, then kill it - the exact shape
            // of the fight in the recording.
            var narrative = new BattleNarrative(Hero, Foe, "Armory", 250, 150);
            var swings = new List<BattleEvent>
            {
                Swing(Hero, Foe, "STRIKE", 20, roll: 12),
                Swing(Foe, Hero, "BONE STRIKE", 14, roll: 11),
                Swing(Hero, Foe, "SLASH", 55, roll: 20, critical: true),
                Swing(Hero, Foe, "STRIKE", 45, roll: 15),
                Swing(Hero, Foe, "SLASH", 18, roll: 17),
                Swing(Hero, Foe, "STRIKE", 12, roll: 14),
            };

            var tiers = new List<NarrativeTier>();

            for (int i = 0; i < swings.Count; i++)
            {
                narrative.AddEvent(swings[i]);

                // Ask the way the live path asks: twice per swing, for the two display
                // surfaces. The second question must not change the answer, and must not count
                // as another swing against the cooldown.
                var lines = NarrativeTierPresenter.BuildFlavorLines(narrative);
                bool spoke = NarrativeTierPresenter.TryGetFlavorLine(narrative, out string text, out var tier);
                NarrativeTierPresenter.TryGetFlavorLine(narrative, out string text2, out var tier2);

                TestBase.AssertTrue(text == text2 && tier == tier2,
                    $"Asking twice in one swing must give the same answer, got '{text}' then '{text2}'",
                    ref _run, ref _passed, ref _failed);

                bool renderedSomething = lines != null && lines.Count > 0;
                TestBase.AssertTrue(renderedSomething == spoke,
                    "The block path and the prose path must agree on whether this swing speaks",
                    ref _run, ref _passed, ref _failed);

                var effective = spoke ? tier : NarrativeTier.Ledger;
                tiers.Add(effective);
                Console.WriteLine($"    swing {i + 1}: {effective,-8} {(spoke ? text : "(silent)")}");
            }

            for (int i = 1; i < tiers.Count; i++)
            {
                TestBase.AssertTrue(
                    !(tiers[i] == NarrativeTier.SetPiece && tiers[i - 1] == NarrativeTier.SetPiece),
                    $"Set pieces landed on consecutive swings {i} and {i + 1} - the overseer "
                    + "stops being rare and starts contradicting himself",
                    ref _run, ref _passed, ref _failed);
            }

            TestBase.AssertTrue(tiers.Contains(NarrativeTier.SetPiece),
                "The cooldown must not silence the overseer entirely - this fight has a crit and a death",
                ref _run, ref _passed, ref _failed);

            // The cooldown's first draft passed the adjacency check by muting the DEATH and
            // keeping "Skeleton is nearly out", which trades one bug for a worse one: the
            // payoff of the whole fight renders as nothing. The ending is exempt, and this is
            // the assertion that keeps it exempt.
            TestBase.AssertTrue(tiers[^1] == NarrativeTier.SetPiece,
                $"The killing blow must always speak, got {tiers[^1]} on the last swing",
                ref _run, ref _passed, ref _failed);
        }

        /// <summary>
        /// Ground rule 2, checked against the real roster rather than a sample. There are 129
        /// enemies in Enemies.json and only a handful have a bank of their own; every one of
        /// the rest has to reach a line through a tag or the generic pool. A creature that
        /// silently gets nothing is the failure this guards - it would look exactly like the
        /// narrator being quiet on purpose.
        /// </summary>
        private static void EveryEnemyGetsAnOpeningLine()
        {
            Console.WriteLine("\n--- Every enemy in the roster resolves to an opening line ---");
            FlavorText.Reload();
            var data = FlavorText.GetData();
            var provider = new NarrativeTextProvider();

            var roster = LoadEnemyRoster();
            TestBase.AssertTrue(roster.Count > 0,
                "Enemies.json should load - without it this test proves nothing",
                ref _run, ref _passed, ref _failed);

            int specific = 0, viaTag = 0, generic = 0;
            var unresolved = new List<string>();

            foreach (var (name, tags) in roster)
            {
                string? hit = null;
                foreach (string bank in CandidateBanksFor(name, tags))
                {
                    if (data.CombatNarratives.TryGetValue(bank, out var lines) && lines is { Length: > 0 })
                    {
                        hit = bank;
                        break;
                    }
                }

                if (hit == null)
                {
                    unresolved.Add(name);
                }
                else if (hit == "encounterOpening")
                {
                    generic++;
                }
                else if (hit == $"encounterOpening_{EncounterNarrator.NormalizeKey(name)}")
                {
                    specific++;
                }
                else
                {
                    viaTag++;
                }
            }

            Console.WriteLine($"    [coverage] {roster.Count} enemies: {specific} own bank, "
                              + $"{viaTag} via tag, {generic} generic, {unresolved.Count} unresolved");

            TestBase.AssertTrue(unresolved.Count == 0,
                $"Every enemy must resolve to a line; unresolved: {string.Join(", ", unresolved)}",
                ref _run, ref _passed, ref _failed);

            // The generic pool is the safety net, not the plan. If most of the roster lands
            // there the tag banks have stopped matching and the lines have gone shapeless.
            TestBase.AssertTrue(generic < roster.Count / 2,
                $"Most enemies should reach a tagged or specific bank, {generic}/{roster.Count} fell to generic",
                ref _run, ref _passed, ref _failed);

            // Voice rules, checked on every opening bank at once.
            foreach (var (bank, lines) in data.CombatNarratives)
            {
                if (!bank.StartsWith("encounterOpening", StringComparison.OrdinalIgnoreCase) || lines == null)
                    continue;

                foreach (string line in lines)
                {
                    TestBase.AssertTrue(!line.Contains('!'),
                        $"{bank} must not use exclamation marks, got: {line}",
                        ref _run, ref _passed, ref _failed);
                    TestBase.AssertTrue(!line.Contains('{') || line.Contains("{enemy}", StringComparison.Ordinal),
                        $"{bank} may only use the {{enemy}} token, got: {line}",
                        ref _run, ref _passed, ref _failed);
                }
            }

            _ = provider;
        }

        /// <summary>
        /// A Ghoul must get the Ghoul line, not the generic undead one. The whole reason for
        /// doing this at the encounter is that the canon is creature-specific - a Ghoul goes
        /// for what a corpse carries - and that is lost if the tag bank wins.
        /// </summary>
        private static void OpeningLinePrefersTheMostSpecificBank()
        {
            Console.WriteLine("\n--- The most specific bank wins ---");
            FlavorText.Reload();

            // Ghoul carries the "undead" tag, so both banks exist and the specific one must win.
            var banks = new List<string>(CandidateBanksFor("Ghoul", new List<string> { "undead" }));
            Console.WriteLine($"    [chain] {string.Join(" -> ", banks)}");

            TestBase.AssertTrue(banks.Count >= 3
                    && banks[0] == "encounterOpening_ghoul"
                    && banks[^1] == "encounterOpening",
                $"Chain must run specific -> tag -> generic, got: {string.Join(" -> ", banks)}",
                ref _run, ref _passed, ref _failed);

            TestBase.AssertTrue(banks.IndexOf("encounterOpening_undead") > 0
                    && banks.IndexOf("encounterOpening_undead") < banks.IndexOf("encounterOpening"),
                "The tag bank must sit between the specific bank and the generic pool",
                ref _run, ref _passed, ref _failed);

            // Names normalize the same way whatever the spelling in Enemies.json.
            TestBase.AssertTrue(
                EncounterNarrator.NormalizeKey("Magma Beast") == "magmabeast"
                && EncounterNarrator.NormalizeKey("magma-beast") == "magmabeast",
                "Enemy names must normalize to one bank key regardless of spacing or case",
                ref _run, ref _passed, ref _failed);

            // And the whole way to painted segments, in the overseer's colour. The block path
            // and the prose path disagreed about his colour for a session; this is a third
            // surface and it has to match them.
            var ghoul = MockFactories.CreateMockEnemy("Ghoul");
            var provider = new NarrativeTextProvider();

            NarrativePacing.OverrideForTesting(
                new NarrativePacing.Profile { NarratorEnabled = true });
            try
            {
                var segments = EncounterNarrator.BuildOpeningSegments(ghoul, provider);
                string painted = segments == null
                    ? "(nothing)"
                    : ColoredTextRenderer.RenderAsPlainText(segments);
                Console.WriteLine($"    [encounter] {painted}");

                TestBase.AssertTrue(segments != null && segments.Count > 0,
                    "A Ghoul encounter must produce an opening line",
                    ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(
                    segments != null && segments.Exists(s =>
                        !string.IsNullOrWhiteSpace(s.Text)
                        && s.Color.Equals(ColorPalette.Purple.GetColor())),
                    "The encounter line must carry the narrator colour",
                    ref _run, ref _passed, ref _failed);
            }
            finally
            {
                NarrativePacing.OverrideForTesting(null);
                NarrativePacing.Invalidate();
            }

            // And it stays silent when the narrator is switched off, like every other path.
            NarrativePacing.OverrideForTesting(NarrativePacing.Profile.Full);
            try
            {
                TestBase.AssertTrue(
                    EncounterNarrator.BuildOpeningSegments(ghoul, provider) == null,
                    "With the narrator disabled the encounter must stay quiet",
                    ref _run, ref _passed, ref _failed);
            }
            finally
            {
                NarrativePacing.OverrideForTesting(null);
                NarrativePacing.Invalidate();
            }
        }

        /// <summary>Mirrors EncounterNarrator.CandidateBanks without needing a live Enemy.</summary>
        private static IEnumerable<string> CandidateBanksFor(string name, IReadOnlyList<string> tags)
        {
            string key = EncounterNarrator.NormalizeKey(name);
            if (key.Length > 0)
                yield return $"encounterOpening_{key}";

            foreach (string tag in tags)
            {
                string t = EncounterNarrator.NormalizeKey(tag);
                if (t.Length > 0)
                    yield return $"encounterOpening_{t}";
            }

            yield return "encounterOpening";
        }

        private static List<(string Name, IReadOnlyList<string> Tags)> LoadEnemyRoster()
        {
            var result = new List<(string, IReadOnlyList<string>)>();
            string? path = GameConstants.TryGetExistingGameDataFilePath("Enemies.json");
            if (path == null)
                return result;

            using var doc = System.Text.Json.JsonDocument.Parse(System.IO.File.ReadAllText(path));
            foreach (var entry in doc.RootElement.EnumerateArray())
            {
                if (!entry.TryGetProperty("name", out var nameEl))
                    continue;

                string name = nameEl.GetString() ?? string.Empty;
                var tags = new List<string>();
                if (entry.TryGetProperty("tags", out var tagsEl)
                    && tagsEl.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var t in tagsEl.EnumerateArray())
                    {
                        string? v = t.GetString();
                        if (!string.IsNullOrWhiteSpace(v))
                            tags.Add(v);
                    }
                }

                result.Add((name, tags));
            }

            return result;
        }

        private static List<ColoredText> Line(string text, bool indent = true)
        {
            // The indent must ride inside the text segment. Build() runs
            // SpacingHelper.ProcessSegments, which collapses a whitespace-only segment to a
            // single space - the exact trap that produced the bug this test pins.
            string body = indent
                ? BlockMessageCollector.ActionBlockSubsequentIndent + text
                : text;
            return new ColoredTextBuilder().Add(body, ColorPalette.Gray).Build();
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
