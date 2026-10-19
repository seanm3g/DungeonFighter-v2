using System;
using System.Linq;
using RPGGame.Tests;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>
    /// Tests for the PROTOTYPE tier model: which moments earn prose, and in which voice.
    ///
    /// The load-bearing property is that tier comes from the bank, never from the prose. The
    /// old crit filter read the text to work out what kind of line it had, and silently changed
    /// behaviour the moment anyone rewrote a bank. These tests exist to keep that from coming
    /// back.
    /// </summary>
    public static class NarrativeTierTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Narrative Tier Tests ===\n");
            _testsRun = _testsPassed = _testsFailed = 0;

            TurnsAreSetPieces();
            OrdinaryMomentsAreBeats();
            NoiseIsLedger();
            UnknownBanksStillSpeak();
            BiomeSuffixedBanksInheritTheirFamily();
            SetPieceBanksAreOverseerVoice();
            BeatBanksAreWorldVoice();
            FlavorLinesReachTheBlock();
            CollectorKeepsFlavorLines();
            SetPieceCarriesNarratorColorInBlockPath();
            EvaluatorBanksSubstituteNames();

            TestBase.PrintSummary("Narrative Tier Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TurnsAreSetPieces()
        {
            Console.WriteLine("--- The turns are set pieces ---");
            foreach (var bank in new[] { "criticalHit", "enemyDefeated", "playerDefeated", "below10Percent" })
            {
                TestBase.AssertTrue(
                    NarrativeTiers.For(bank) == NarrativeTier.SetPiece,
                    $"{bank} should be a set piece",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void OrdinaryMomentsAreBeats()
        {
            Console.WriteLine("\n--- Ordinary significant moments are beats ---");
            foreach (var bank in new[] { "firstBlood", "below50Percent", "healthLeadChange", "intenseBattle" })
            {
                TestBase.AssertTrue(
                    NarrativeTiers.For(bank) == NarrativeTier.Beat,
                    $"{bank} should be a beat",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void NoiseIsLedger()
        {
            Console.WriteLine("\n--- Noise stays silent ---");
            foreach (var bank in new[] { "healthRecovery", "playerTaunt", "enemyTaunt" })
            {
                TestBase.AssertTrue(
                    !NarrativeTiers.Speaks(bank),
                    $"{bank} should not reach the log",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void UnknownBanksStillSpeak()
        {
            Console.WriteLine("\n--- Unclassified banks are heard, not silenced ---");
            // Silence by omission is the harder failure to notice, so an unknown bank falls to
            // Beat rather than Ledger.
            TestBase.AssertTrue(
                NarrativeTiers.For("someBankNobodyHasClassifiedYet") == NarrativeTier.Beat,
                "An unknown bank should fall to Beat",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(
                NarrativeTiers.For(null) == NarrativeTier.Beat && NarrativeTiers.For("") == NarrativeTier.Beat,
                "Null/empty bank should fall to Beat without throwing",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void BiomeSuffixedBanksInheritTheirFamily()
        {
            Console.WriteLine("\n--- Biome taunt banks inherit the family tier ---");
            foreach (var bank in new[] { "playerTaunt_lava", "enemyTaunt_crypt", "playerTaunt_library" })
            {
                TestBase.AssertTrue(
                    NarrativeTiers.For(bank) == NarrativeTier.Ledger,
                    $"{bank} should inherit the taunt family's tier",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void SetPieceBanksAreOverseerVoice()
        {
            Console.WriteLine("\n--- Set-piece banks are written in the overseer's voice ---");
            FlavorText.Reload();
            var data = FlavorText.GetData();

            // The overseer recounts: first person, past tense. Present-tense "I watch" or a
            // missing narrator would put him back in the commentator's chair.
            foreach (var bank in new[] { "criticalHit", "enemyDefeated", "below10Percent" })
            {
                if (!data.CombatNarratives.TryGetValue(bank, out var lines) || lines == null)
                    continue;

                int firstPerson = lines.Count(l =>
                    l.Contains(" I ", StringComparison.Ordinal) ||
                    l.StartsWith("I ", StringComparison.Ordinal));

                TestBase.AssertTrue(
                    firstPerson > 0,
                    $"{bank} should carry the narrator's first person in at least one line",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertTrue(
                    lines.All(l => !l.Contains('!')),
                    $"{bank} must not use exclamation marks - the overseer states, he does not exclaim",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void FlavorLinesReachTheBlock()
        {
            Console.WriteLine("\n--- Tiered flavor reaches the action block ---");

            RPGGame.Combat.Sequence.NarrativePacing.OverrideForTesting(
                new RPGGame.Combat.Sequence.NarrativePacing.Profile { NarratorEnabled = true });
            try
            {
                var narrative = new BattleNarrative("Hero", "Goblin", "Test", 100, 100);
                narrative.AddEvent(new BattleEvent
                {
                    Actor = "Hero", Target = "Goblin", Action = "Strike",
                    IsSuccess = true, NaturalRoll = 12, Damage = 7
                });

                var tagged = narrative.GetTriggeredNarrativesTagged();
                Console.WriteLine($"    [probe] tagged={tagged.Count} banks=[{string.Join(",", tagged.Select(t => t.Bank))}]");

                var lines = NarrativeTierPresenter.BuildFlavorLines(narrative);
                Console.WriteLine($"    [probe] flavorLines={(lines == null ? "null" : lines.Count.ToString())}");

                TestBase.AssertTrue(tagged.Count > 0,
                    "First damaging hit should produce tagged narratives",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(lines != null && lines.Count > 0,
                    "A first-blood beat should produce a flavor line for the action block",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                RPGGame.Combat.Sequence.NarrativePacing.OverrideForTesting(null);
                RPGGame.Combat.Sequence.NarrativePacing.Invalidate();
            }
        }

        private static void CollectorKeepsFlavorLines()
        {
            Console.WriteLine("\n--- Collector keeps the flavor line in the block ---");

            var action = new ColoredTextBuilder().Add("Hero attacks", ColorPalette.Info).Build();
            var roll = new ColoredTextBuilder().Add("(roll: 12)", ColorPalette.Info).Build();
            var flavor = new System.Collections.Generic.List<System.Collections.Generic.List<ColoredText>>
            {
                new ColoredTextBuilder().Add("  First blood hits the stone.", ColorPalette.Gray).Build()
            };

            var groups = RPGGame.UI.BlockDisplay.BlockMessageCollector.CollectActionBlockMessages(
                action, roll, null, null, flavor);

            string all = string.Join(" | ", groups.Select(g => ColoredTextRenderer.RenderAsPlainText(g.segments)));
            Console.WriteLine($"    [probe] groups={groups.Count} :: {all}");

            TestBase.AssertTrue(
                all.Contains("First blood hits the stone", StringComparison.Ordinal),
                "Collector must keep the tiered flavor line in the block",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void SetPieceCarriesNarratorColorInBlockPath()
        {
            Console.WriteLine("\n--- Set piece uses the narrator color in the block path too ---");
            // Two display surfaces render the overseer: the prose presenter (F7 on) and the
            // mechanical block (F7 off). They must agree on his color, or he changes identity
            // when the player toggles views.
            RPGGame.Combat.Sequence.NarrativePacing.OverrideForTesting(
                new RPGGame.Combat.Sequence.NarrativePacing.Profile { NarratorEnabled = true });
            try
            {
                var narrative = new BattleNarrative("Hero", "Goblin", "Test", 100, 100);
                narrative.AddEvent(new BattleEvent
                {
                    Actor = "Hero", Target = "Goblin", Action = "Strike",
                    IsSuccess = true, IsCritical = true, NaturalRoll = 19, Roll = 19, Damage = 30
                });

                var tagged = narrative.GetTriggeredNarrativesTagged();
                bool isSetPiece = tagged.Exists(t => NarrativeTiers.For(t.Bank) == NarrativeTier.SetPiece);
                var lines = NarrativeTierPresenter.BuildFlavorLines(narrative);

                bool purple = lines != null && lines.Exists(l =>
                    l.Exists(seg => !string.IsNullOrWhiteSpace(seg.Text)
                                    && seg.Color.Equals(ColorPalette.Purple.GetColor())));

                TestBase.AssertTrue(isSetPiece,
                    "A crit should tag a set-piece bank",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(purple,
                    "Set-piece line in the block path must use the narrator color",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                RPGGame.Combat.Sequence.NarrativePacing.OverrideForTesting(null);
                RPGGame.Combat.Sequence.NarrativePacing.Invalidate();
            }
        }

        private static void EvaluatorBanksSubstituteNames()
        {
            Console.WriteLine("\n--- Evaluator-owned banks fill in {name} ---");
            // Regression: NarrativeTriggerEvaluator was built in the analyzer's constructor,
            // before Initialize supplied the combatant names, so every line it owns rendered
            // with an empty {name} - "Pressure changes hands. is the one walking forward."
            var textProvider = new NarrativeTextProvider();
            var analyzer = new BattleEventAnalyzer(
                textProvider, new NarrativeStateManager(), new TauntSystem(textProvider));
            analyzer.Initialize("Hero", "Goblin", "Test", 100, 100);
            analyzer.UpdateFinalHealth(100, 60);

            var tagged = analyzer.AnalyzeEventTagged(new BattleEvent
            {
                Actor = "Hero", Target = "Goblin", Action = "Strike",
                IsSuccess = true, NaturalRoll = 12, Damage = 40
            }, GameSettings.Instance);

            var lead = tagged.FindAll(t => t.Bank == "healthLeadChange");
            TestBase.AssertTrue(lead.Count > 0,
                "A big swing should trigger a health-lead narrative",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            foreach (var entry in lead)
            {
                Console.WriteLine($"    [lead] {entry.Text}");
                TestBase.AssertTrue(
                    entry.Text.Contains("Hero", StringComparison.Ordinal)
                        || entry.Text.Contains("Goblin", StringComparison.Ordinal),
                    $"Health-lead line must name a combatant, got: {entry.Text}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(
                    !entry.Text.Contains("{name}", StringComparison.Ordinal),
                    "No raw token may survive into a displayed line",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void BeatBanksAreWorldVoice()
        {
            Console.WriteLine("\n--- Beat banks stay in the world's voice ---");
            FlavorText.Reload();
            var data = FlavorText.GetData();

            // The world's voice has no narrator in it. A stray "I" here would blur the two
            // voices the tense/person split exists to keep apart.
            foreach (var bank in new[] { "firstBlood", "below50Percent", "healthLeadChange" })
            {
                if (!data.CombatNarratives.TryGetValue(bank, out var lines) || lines == null)
                    continue;

                TestBase.AssertTrue(
                    lines.All(l => !l.StartsWith("I ", StringComparison.Ordinal)
                                   && !l.Contains(" I ", StringComparison.Ordinal)),
                    $"{bank} should not speak in the narrator's first person",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }
    }
}
