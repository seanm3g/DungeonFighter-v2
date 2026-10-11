using System;
using System.Linq;
using RPGGame.Combat.Narrative;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>Tests for name-derived victory limerick epithets.</summary>
    public static class VictoryHeroEpithetTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== VictoryHeroEpithet Tests ===\n");
            _testsRun = _testsPassed = _testsFailed = 0;

            TestCamelCaseSplit();
            TestCompoundSurnameSplit();
            TestFromNameNeverEqualsFullName();
            TestFromNameOmitsForbiddenTokens();
            TestForbiddenTokensAreSpaceSplitOnly();
            TestScrubReplacesHeroTokens();

            TestBase.PrintSummary("VictoryHeroEpithet Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestCamelCaseSplit()
        {
            Console.WriteLine("--- CamelCase split ---");
            var bits = VictoryHeroEpithet.SplitCamelCase("FlameWeaver");
            TestBase.AssertEqual(2, bits.Count, "FlameWeaver splits in two",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("Flame", bits[0], "first bit", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("Weaver", bits[1], "second bit", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCompoundSurnameSplit()
        {
            Console.WriteLine("--- Compound surname split ---");
            var bits = VictoryHeroEpithet.SplitCamelCase("Flameweaver");
            TestBase.AssertEqual(2, bits.Count, "Flameweaver splits on weaver suffix",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("Flame", bits[0], "compound head", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("weaver", bits[1], "compound tail", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestFromNameNeverEqualsFullName()
        {
            Console.WriteLine("--- FromName never equals full name ---");
            for (int i = 0; i < 8; i++)
            {
                string epithet = VictoryHeroEpithet.FromName("Basil Flameweaver");
                TestBase.AssertFalse(
                    string.Equals(epithet, "Basil Flameweaver", StringComparison.OrdinalIgnoreCase),
                    $"epithet must not equal full name ({epithet})",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestFromNameOmitsForbiddenTokens()
        {
            Console.WriteLine("--- FromName omits forbidden tokens ---");
            for (int i = 0; i < 8; i++)
            {
                string epithet = VictoryHeroEpithet.FromName("Basil Flameweaver");
                TestBase.AssertFalse(
                    VictoryHeroEpithet.ContainsForbiddenName(epithet, "Basil Flameweaver"),
                    $"epithet must not contain name tokens ({epithet})",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            string single = VictoryHeroEpithet.FromName("Taran");
            TestBase.AssertFalse(
                VictoryHeroEpithet.ContainsForbiddenName(single, "Taran"),
                $"single-name epithet must not contain Taran ({single})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestForbiddenTokensAreSpaceSplitOnly()
        {
            Console.WriteLine("--- Forbidden tokens are space-split only ---");
            var tokens = VictoryHeroEpithet.ForbiddenNameTokens("Basil Flameweaver");
            TestBase.AssertTrue(tokens.Any(t => t.Equals("Basil", StringComparison.OrdinalIgnoreCase)),
                "includes first name", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(tokens.Any(t => t.Equals("Flameweaver", StringComparison.OrdinalIgnoreCase)),
                "includes last name", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertFalse(tokens.Any(t => t.Equals("Flame", StringComparison.OrdinalIgnoreCase)),
                "does not ban CamelCase fragment Flame",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertFalse(tokens.Any(t => t.Equals("Weaver", StringComparison.OrdinalIgnoreCase)),
                "does not ban CamelCase fragment Weaver",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestScrubReplacesHeroTokens()
        {
            Console.WriteLine("--- Scrub replaces hero tokens ---");
            string scrubbed = VictoryHeroEpithet.ScrubHeroName(
                "Basil landed a critical on the foe",
                "Basil Flameweaver",
                "the flame-weaving wanderer");
            TestBase.AssertFalse(scrubbed.Contains("Basil", StringComparison.OrdinalIgnoreCase),
                "first name scrubbed", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(scrubbed.Contains("the flame-weaving wanderer", StringComparison.Ordinal),
                "epithet inserted", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
