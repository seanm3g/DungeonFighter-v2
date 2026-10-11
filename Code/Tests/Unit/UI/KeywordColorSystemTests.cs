using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.Tests;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Tests for KeywordColorSystem
    /// Tests keyword matching, color application, and text processing
    /// </summary>
    public static class KeywordColorSystemTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all KeywordColorSystem tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== KeywordColorSystem Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestApplyKeywordColors();
            TestApplyKeywordColorsWithNullInput();
            TestApplyKeywordColorsWithEmptyInput();
            TestKeywordMatching();
            TestSparseKeywordPaintAndExactMatch();
            TestTypewriterNameAndKeywordPrefixes();

            TestBase.PrintSummary("KeywordColorSystem Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Keyword Color Application Tests

        private static void TestApplyKeywordColors()
        {
            Console.WriteLine("--- Testing Colorize ---");

            var result = KeywordColorSystem.Colorize("Player attacks with sword");
            
            TestBase.AssertTrue(result != null,
                "Colorize should return non-null result",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            
            if (result != null)
            {
                TestBase.AssertTrue(result.Count > 0,
                    "Colorize should return non-empty list",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestApplyKeywordColorsWithNullInput()
        {
            Console.WriteLine("\n--- Testing Colorize with null input ---");

            // Use null-forgiving operator since Colorize handles null internally
            var result = KeywordColorSystem.Colorize(null!);
            
            TestBase.AssertTrue(result != null,
                "Colorize should return non-null result for null input",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            
            if (result != null)
            {
                TestBase.AssertTrue(result.Count == 0,
                    "Colorize should return empty list for null input",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestApplyKeywordColorsWithEmptyInput()
        {
            Console.WriteLine("\n--- Testing Colorize with empty input ---");

            var result = KeywordColorSystem.Colorize("");
            
            TestBase.AssertTrue(result != null,
                "Colorize should handle empty input",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            
            if (result != null)
            {
                TestBase.AssertTrue(result.Count == 0,
                    "Colorize should return empty list for empty input",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestKeywordMatching()
        {
            Console.WriteLine("\n--- Testing Keyword Matching ---");

            // Test with common keywords
            var testCases = new[]
            {
                "Player attacks",
                "Enemy takes damage",
                "Critical hit!",
                "Health restored"
            };

            foreach (var testCase in testCases)
            {
                var result = KeywordColorSystem.Colorize(testCase);
                
                if (result != null)
                {
                    TestBase.AssertTrue(result.Count > 0,
                        $"Keyword matching should work for: {testCase}",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
                else
                {
                    TestBase.AssertTrue(false,
                        $"Colorize returned null for: {testCase}",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }
        }

        private static void TestSparseKeywordPaintAndExactMatch()
        {
            Console.WriteLine("\n--- Testing sparse exact-match keyword paint ---");

            var prose = KeywordColorSystem.Colorize(
                "leans toward uncertain fortune under a solid stance as heavy blow leaves a wound");

            TestBase.AssertTrue(prose != null && prose.Count > 0,
                "sparse paint returns segments",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            bool fortuneCyan = prose!.Exists(s =>
                s.Text.Contains("fortune", StringComparison.OrdinalIgnoreCase)
                && s.Color == ColorPalette.Cyan.GetColor());
            TestBase.AssertTrue(!fortuneCyan,
                "atmospheric 'fortune' is not cyan",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            bool stanceColored = prose.Exists(s =>
                s.Text.Contains("stance", StringComparison.OrdinalIgnoreCase)
                && s.Color != Colors.White);
            TestBase.AssertTrue(!stanceColored,
                "atmospheric 'stance' stays white",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            bool asColored = prose.Exists(s =>
                string.Equals(s.Text.TrimEnd('.', ',', '!', '?'), "as", StringComparison.OrdinalIgnoreCase)
                && s.Color != Colors.White);
            TestBase.AssertTrue(!asColored,
                "glue word 'as' is not substring-matched to damage verbs",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var damageColor = KeywordGroupManager.GetKeywordGroup("damage")?.Color
                ?? ColorPalette.Damage.GetColor();
            bool woundRed = prose.Exists(s =>
                s.Text.Contains("wound", StringComparison.OrdinalIgnoreCase)
                && s.Color == damageColor);
            TestBase.AssertTrue(woundRed,
                "severity 'wound' stays damage-colored",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Whole-word only: "slash" must not color "as", and "wounded" must not match "wound" via contains.
            var wounded = KeywordColorSystem.Colorize("the wounded hero");
            bool woundedFalse = wounded.Exists(s =>
                s.Text.Contains("wounded", StringComparison.OrdinalIgnoreCase)
                && s.Color == damageColor);
            TestBase.AssertTrue(!woundedFalse,
                "partial token 'wounded' does not match keyword 'wound'",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestTypewriterNameAndKeywordPrefixes()
        {
            Console.WriteLine("\n--- Typewriter prefixes color names and keywords ---");
            KeywordColorSystem.RegisterCharacterName("Marcus Serpentfang", ColorPalette.Gold.GetColor());
            try
            {
                var gold = ColorPalette.Gold.GetColor();
                var mar = KeywordColorSystem.Colorize("Mar");
                TestBase.AssertTrue(mar.Exists(s =>
                        s.Text.Contains("Mar", StringComparison.Ordinal)
                        && s.Color == gold),
                    "Mar prefix of Marcus Serpentfang is gold",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                var first = KeywordColorSystem.Colorize("Marcus gathers");
                TestBase.AssertTrue(first.Exists(s =>
                        string.Equals(s.Text, "Marcus", StringComparison.Ordinal)
                        && s.Color == gold),
                    "first name is gold before surname",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                var pair = KeywordColorSystem.Colorize("Marcus Ser");
                TestBase.AssertTrue(pair.Exists(s =>
                        s.Color == gold
                        && (s.Text.Contains("Marcus", StringComparison.Ordinal)
                            || s.Text.Contains("Ser", StringComparison.Ordinal))),
                    "Marcus Ser prefix pair is gold",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                var damageColor = KeywordGroupManager.GetKeywordGroup("damage")?.Color
                    ?? ColorPalette.Damage.GetColor();
                var wou = KeywordColorSystem.Colorize("a wou");
                TestBase.AssertTrue(wou.Exists(s =>
                        s.Text.Contains("wou", StringComparison.OrdinalIgnoreCase)
                        && s.Color == damageColor),
                    "wou prefix of wound is damage-colored",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                KeywordColorSystem.UnregisterCharacterName("Marcus Serpentfang");
            }
        }

        #endregion
    }
}
