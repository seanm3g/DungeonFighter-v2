using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Media;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Renderers.Text;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Tests word-boundary colored wrapping used by the combat log (F7 prose).
    /// </summary>
    public static class TextWrappingHelperTests
    {
        private static int _run;
        private static int _passed;
        private static int _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== TextWrappingHelper Tests ===\n");
            _run = 0;
            _passed = 0;
            _failed = 0;

            TestMultiWordColoredPhraseSoftWraps();
            TestRemainderPunctuationDoesNotLeadLine();
            TestTrailingPeriodStaysWithPreviousWord();
            TestShortSubjectNotOrphanedBeforePredicate();
            TestShortConnectorDoesNotLeaveEarlyStub();
            TestPerCharacterGlyphsDoNotGainInterLetterSpaces();
            TestPerCharacterStatusWordStaysCompact();
            TestFirstLineIndentSurvivesOnFirstLineOnly();
            TestApplySoftWrapsStartsOverflowWordOnNextLine();
            TestTypewriterPrefixKeepsOverflowWordOnNextLine();

            TestBase.PrintSummary("TextWrappingHelper Tests", _run, _passed, _failed);
        }

        /// <summary>
        /// A multi-word cyan phrase must soft-wrap mid-phrase when the remainder of the line
        /// cannot fit the whole span — not jump wholly to the next line leaving a large gap.
        /// </summary>
        private static void TestMultiWordColoredPhraseSoftWraps()
        {
            Console.WriteLine("--- Multi-word colored phrase soft-wraps at word boundaries ---");

            // "answers with " = 13 chars; "keen, decisive fortune" = 22 chars.
            // Width 20: first line should take "answers with keen," (18) then wrap "decisive fortune".
            var segments = new List<ColoredText>
            {
                new ColoredText("answers with ", Colors.White),
                new ColoredText("keen, decisive fortune", Colors.Cyan)
            };

            var lines = TextWrappingHelper.WrapColoredSegments(segments, maxWidth: 20);
            TestBase.AssertTrue(lines.Count >= 2,
                "wraps onto at least two lines", ref _run, ref _passed, ref _failed);

            string first = Flatten(lines[0]);
            string second = Flatten(lines[1]);
            TestBase.AssertTrue(first.Contains("answers with", StringComparison.Ordinal),
                "first line keeps lead-in", ref _run, ref _passed, ref _failed);
            // "keen," is short enough to orphan-prevent onto the next line with "decisive".
            TestBase.AssertTrue(
                first.Contains("keen", StringComparison.OrdinalIgnoreCase)
                    || second.StartsWith("keen", StringComparison.OrdinalIgnoreCase),
                "cyan phrase soft-wraps (keen stays with its clause)", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(second.Contains("decisive", StringComparison.OrdinalIgnoreCase)
                    || second.Contains("fortune", StringComparison.OrdinalIgnoreCase),
                "remainder of phrase continues on next line", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!second.StartsWith(" ", StringComparison.Ordinal),
                "continuation does not lead with a space", ref _run, ref _passed, ref _failed);
            string joined = first + " " + second;
            TestBase.AssertTrue(!joined.Contains("keen, decisive fortune", StringComparison.Ordinal)
                    || lines.Count >= 2,
                "multi-word cyan span is not forced onto one line as an atomic block",
                ref _run, ref _passed, ref _failed);
        }

        /// <summary>
        /// Keyword coloring often leaves "," / " " as separate segments after a colored phrase.
        /// Wrapping must not start a line as " , a thread…".
        /// </summary>
        private static void TestRemainderPunctuationDoesNotLeadLine()
        {
            Console.WriteLine("--- Remainder punctuation does not lead a wrapped line ---");

            // Fill almost exactly: "toward " (7) + "uncertain fortune" (17) = 24.
            // Width 24 forces the following ", a thread" onto the next line; comma must glue back.
            var segments = new List<ColoredText>
            {
                new ColoredText("toward ", Colors.White),
                new ColoredText("uncertain fortune", Colors.Cyan),
                new ColoredText(",", Colors.White),
                new ColoredText(" ", Colors.White),
                new ColoredText("a thread drawn taut.", Colors.White)
            };

            var lines = TextWrappingHelper.WrapColoredSegments(segments, maxWidth: 24);
            TestBase.AssertTrue(lines.Count >= 2,
                "wraps remainder after fortune", ref _run, ref _passed, ref _failed);

            string first = Flatten(lines[0]);
            string second = Flatten(lines[1]);
            TestBase.AssertTrue(first.EndsWith(",", StringComparison.Ordinal)
                    || first.Contains("fortune,", StringComparison.Ordinal),
                "comma glues to previous word on first line", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!second.StartsWith(" ", StringComparison.Ordinal) && !second.StartsWith(",", StringComparison.Ordinal),
                "second line does not start with space or comma", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(second.StartsWith("a thread", StringComparison.Ordinal),
                "second line starts with the following words", ref _run, ref _passed, ref _failed);
        }

        /// <summary>
        /// A trailing period left outside a full-width colored phrase must stay on that line.
        /// </summary>
        private static void TestTrailingPeriodStaysWithPreviousWord()
        {
            Console.WriteLine("--- Trailing period stays with previous word ---");

            // Exact fill of the cyan phrase; period as its own segment must glue, not orphan.
            const string phrase = "uncertain fortune";
            var segments = new List<ColoredText>
            {
                new ColoredText(phrase, Colors.Cyan),
                new ColoredText(".", Colors.White)
            };

            var lines = TextWrappingHelper.WrapColoredSegments(segments, maxWidth: phrase.Length);
            TestBase.AssertEqual(1, lines.Count,
                "period does not create a second line", ref _run, ref _passed, ref _failed);
            string plain = Flatten(lines[0]);
            TestBase.AssertEqual(phrase + ".", plain,
                "period appended to fortune", ref _run, ref _passed, ref _failed);
        }

        /// <summary>
        /// "Wolf answers with miss." must not wrap after the short subject name alone.
        /// </summary>
        private static void TestShortSubjectNotOrphanedBeforePredicate()
        {
            Console.WriteLine("--- Short subject stays with its predicate ---");

            // Fill a line so "Wolf" would fit at EOL but "answers" would not.
            // "leans toward thin and shaky fortune. " = 36 chars; width 41 leaves room for "Wolf" (4)
            // but not "Wolf answers" (4+1+7=12).
            var segments = new List<ColoredText>
            {
                new ColoredText("leans toward thin and shaky fortune. ", Colors.White),
                new ColoredText("Wolf", Colors.Gold),
                new ColoredText(" answers with miss.", Colors.White)
            };

            var lines = TextWrappingHelper.WrapColoredSegments(segments, maxWidth: 41);
            TestBase.AssertTrue(lines.Count >= 2,
                "wraps onto a second line", ref _run, ref _passed, ref _failed);

            string first = Flatten(lines[0]);
            string second = Flatten(lines[1]);
            TestBase.AssertTrue(!first.TrimEnd().EndsWith("Wolf", StringComparison.Ordinal),
                "first line does not end with orphaned Wolf", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(second.StartsWith("Wolf answers", StringComparison.Ordinal),
                "Wolf stays with answers on the next line", ref _run, ref _passed, ref _failed);
        }

        /// <summary>
        /// Mid-line short connectors ("and"/"the") must not orphan-pull when the line still has
        /// room beyond an orphan-sized slot — otherwise "Skill" sits alone before "and luck…".
        /// </summary>
        private static void TestShortConnectorDoesNotLeaveEarlyStub()
        {
            Console.WriteLine("--- Short connector does not leave early stub ---");

            // Width 16: indent(4)+Skill(5)=9; " and"(4) fits leaving 3; "luck" needs 5.
            // Old orphan logic flushed before "and" and left "    Skill" alone.
            const string indent = "    ";
            var segments = new List<ColoredText>
            {
                new ColoredText(indent + "Skill and luck conspire toward uncertain fortune.", Colors.White)
            };

            var lines = TextWrappingHelper.WrapColoredSegments(segments, maxWidth: 16);
            TestBase.AssertTrue(lines.Count >= 2,
                "narrow prose wraps onto multiple lines", ref _run, ref _passed, ref _failed);

            string first = Flatten(lines[0]);
            TestBase.AssertTrue(first.StartsWith(indent + "Skill", StringComparison.Ordinal),
                "first line still starts with indented Skill", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!string.Equals(first, indent + "Skill", StringComparison.Ordinal),
                "first line is not only indented Skill", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(first.Contains("and", StringComparison.Ordinal),
                "short connector 'and' stays with Skill on the first line", ref _run, ref _passed, ref _failed);
        }

        /// <summary>
        /// Undulating dungeon/room names are one glyph per segment. Wrap must not insert spaces
        /// between those glyphs (regression: "A n c i e n t F o r e s t").
        /// </summary>
        private static void TestPerCharacterGlyphsDoNotGainInterLetterSpaces()
        {
            Console.WriteLine("--- Per-character name glyphs stay compacted ---");

            var segments = new List<ColoredText>
            {
                new ColoredText("Dungeon: ", Colors.Gold)
            };
            foreach (char c in "Ancient Forest")
                segments.Add(new ColoredText(c.ToString(), Colors.Lime));

            var lines = TextWrappingHelper.WrapColoredSegments(segments, maxWidth: 80);
            TestBase.AssertEqual(1, lines.Count,
                "fits on one line", ref _run, ref _passed, ref _failed);
            string plain = Flatten(lines[0]);
            TestBase.AssertEqual("Dungeon: Ancient Forest", plain,
                "wrap does not space between undulating name glyphs", ref _run, ref _passed, ref _failed);
        }

        /// <summary>
        /// Multi-color status templates (STUN / stunned) are also one glyph per segment.
        /// </summary>
        private static void TestPerCharacterStatusWordStaysCompact()
        {
            Console.WriteLine("--- Per-character status word stays compact ---");

            var segments = new List<ColoredText>
            {
                new ColoredText("affected by ", Colors.White)
            };
            foreach (char c in "STUN")
                segments.Add(new ColoredText(c.ToString(), Colors.Yellow));
            segments.Add(new ColoredText(" for 1 turn", Colors.White));

            var lines = TextWrappingHelper.WrapColoredSegments(segments, maxWidth: 80);
            TestBase.AssertEqual(1, lines.Count,
                "fits on one line", ref _run, ref _passed, ref _failed);
            string plain = Flatten(lines[0]);
            TestBase.AssertEqual("affected by STUN for 1 turn", plain,
                "wrap does not space between status-effect glyphs", ref _run, ref _passed, ref _failed);
        }

        /// <summary>
        /// F7 narrative paragraphs prefix four spaces. Indent must survive colored wrap on line 0
        /// only, count toward width, and not reappear on continuation lines.
        /// </summary>
        private static void TestFirstLineIndentSurvivesOnFirstLineOnly()
        {
            Console.WriteLine("--- First-line paragraph indent survives on line 0 only ---");

            const string indent = "    ";
            // indent(4) + "Fate leans toward steady resolve. A decisive hit" = 4+45 = 49.
            // Width 40 forces a wrap after the indent-aware first line.
            var segments = new List<ColoredText>
            {
                new ColoredText(indent, Colors.White),
                new ColoredText("Fate leans toward steady resolve. A decisive hit cuts through the din.", Colors.White)
            };

            var lines = TextWrappingHelper.WrapColoredSegments(segments, maxWidth: 40);
            TestBase.AssertTrue(lines.Count >= 2,
                "indented prose wraps onto at least two lines", ref _run, ref _passed, ref _failed);

            string first = Flatten(lines[0]);
            string second = Flatten(lines[1]);
            TestBase.AssertTrue(first.StartsWith(indent, StringComparison.Ordinal),
                "first line keeps book-style indent", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!second.StartsWith(" ", StringComparison.Ordinal),
                "continuation line has no leading spaces", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(first.Length <= 40,
                "first line including indent stays within maxWidth", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(first.Contains("Fate", StringComparison.Ordinal),
                "first line still carries prose after indent", ref _run, ref _passed, ref _failed);
        }

        /// <summary>
        /// ApplySoftWraps must place a word that will not fit entirely on the current line onto
        /// the next line (with a \n), matching final wrap — so typewriter reveal never parks a
        /// partial word at EOL that later jumps.
        /// </summary>
        private static void TestApplySoftWrapsStartsOverflowWordOnNextLine()
        {
            Console.WriteLine("--- ApplySoftWraps starts overflow word on next line ---");

            // "Skill and chance " = 17; "conspire" = 8; width 20 cannot fit "conspire" on line 1.
            // "chance" is longer than MaxOrphanWordLength so it is not pulled onto the next line.
            var segments = new List<ColoredText>
            {
                new ColoredText("Skill and chance conspire toward fate.", Colors.White)
            };

            var soft = TextWrappingHelper.ApplySoftWraps(segments, maxWidth: 20);
            string plain = Flatten(soft);
            TestBase.AssertTrue(plain.Contains('\n'),
                "soft wrap inserts a newline", ref _run, ref _passed, ref _failed);

            string[] physical = plain.Split('\n');
            TestBase.AssertTrue(physical.Length >= 2,
                "at least two physical lines", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!physical[0].Contains("conspire", StringComparison.Ordinal),
                "first line does not hold conspire", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(physical[1].StartsWith("conspire", StringComparison.Ordinal),
                "conspire starts the continuation line", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(physical[0].Length <= 20,
                "first physical line within width", ref _run, ref _passed, ref _failed);
        }

        /// <summary>
        /// Truncating a soft-wrapped paragraph mid-word must still show that word's prefix on the
        /// continuation line — never as a dangling stump on the previous line.
        /// </summary>
        private static void TestTypewriterPrefixKeepsOverflowWordOnNextLine()
        {
            Console.WriteLine("--- Typewriter prefix keeps overflow word on next line ---");

            var segments = new List<ColoredText>
            {
                new ColoredText("Skill and chance conspire toward fate.", Colors.White)
            };
            var soft = TextWrappingHelper.ApplySoftWraps(segments, maxWidth: 20);
            string full = Flatten(soft);
            int wordStart = full.IndexOf("conspire", StringComparison.Ordinal);
            TestBase.AssertTrue(wordStart > 0 && full[wordStart - 1] == '\n',
                "conspire is preceded by soft newline", ref _run, ref _passed, ref _failed);

            // Reveal only the first three letters of the wrapped word.
            var slice = ColoredTextRenderer.Truncate(soft, wordStart + 3);
            string partial = Flatten(slice);
            string[] physical = partial.Split('\n');
            TestBase.AssertTrue(physical.Length >= 2,
                "mid-word reveal already has two lines", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!physical[0].EndsWith("con", StringComparison.Ordinal),
                "first line does not end with partial conspire", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("con", physical[physical.Length - 1],
                "continuation shows the typed prefix of conspire", ref _run, ref _passed, ref _failed);
        }

        private static string Flatten(List<ColoredText> line)
        {
            var sb = new StringBuilder();
            foreach (var s in line)
                sb.Append(s.Text);
            return sb.ToString();
        }
    }
}
