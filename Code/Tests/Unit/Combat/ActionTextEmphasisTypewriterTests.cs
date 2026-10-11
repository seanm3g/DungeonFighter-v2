using System;
using RPGGame.Combat.Sequence;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Combat
{
    public static class ActionTextEmphasisTypewriterTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== ActionTextEmphasisTypewriter Tests ===\n");
            _run = _passed = _failed = 0;

            TestSlamDumpsWholeWord();
            TestNonCatalogStaysPerGlyph();
            TestCompletesDistortionPulseWord();
            TestWordBoundaryIgnoresSubstring();

            TestBase.PrintSummary("ActionTextEmphasisTypewriter Tests", _run, _passed, _failed);
        }

        private static void TestSlamDumpsWholeWord()
        {
            Console.WriteLine("--- SLAM dumps whole word ---");
            const string plain = "answers with SLAM.";
            int start = plain.IndexOf("SLAM", StringComparison.Ordinal);
            int next = ActionTextEmphasisTypewriter.NextVisibleLength(plain, start, plain.Length, "SLAM");
            TestBase.AssertEqual(start + 4, next, "jump to end of SLAM", ref _run, ref _passed, ref _failed);
        }

        private static void TestNonCatalogStaysPerGlyph()
        {
            Console.WriteLine("\n--- Non-catalog stays per glyph ---");
            const string plain = "answers with STRIKE.";
            int start = plain.IndexOf("STRIKE", StringComparison.Ordinal);
            int next = ActionTextEmphasisTypewriter.NextVisibleLength(plain, start, plain.Length, "STRIKE");
            TestBase.AssertEqual(start + 1, next, "STRIKE advances one glyph", ref _run, ref _passed, ref _failed);
        }

        private static void TestCompletesDistortionPulseWord()
        {
            Console.WriteLine("\n--- CompletesDistortionPulseWord ---");
            const string plain = "with SLAM.";
            int start = plain.IndexOf("SLAM", StringComparison.Ordinal);
            TestBase.AssertTrue(
                ActionTextEmphasisTypewriter.CompletesDistortionPulseWord(plain, start, start + 4, "SLAM"),
                "dump completes pulse word",
                ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(
                !ActionTextEmphasisTypewriter.CompletesDistortionPulseWord(plain, start, start + 1, "SLAM"),
                "single glyph does not complete",
                ref _run, ref _passed, ref _failed);
        }

        private static void TestWordBoundaryIgnoresSubstring()
        {
            Console.WriteLine("\n--- Word boundary ignores substring ---");
            const string plain = "SLAMMING hard.";
            int next = ActionTextEmphasisTypewriter.NextVisibleLength(plain, 0, plain.Length, "SLAM");
            TestBase.AssertEqual(1, next, "SLAMMING is not a SLAM word dump", ref _run, ref _passed, ref _failed);
        }
    }
}
