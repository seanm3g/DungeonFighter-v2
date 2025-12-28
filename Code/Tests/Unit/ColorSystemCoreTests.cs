using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.Tests;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Comprehensive tests for color system core functions
    /// Tests ColoredTextParser, ColorTemplateLibrary, ColoredTextBuilder, ColoredTextMerger, ColorUtils, ColorValidator
    /// </summary>
    public static class ColorSystemCoreTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Color System Core Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestColoredTextParser();
            TestColorTemplateLibrary();
            TestColoredTextBuilder();
            TestColoredTextMerger();
            TestColorUtils();
            TestColorValidator();

            TestBase.PrintSummary("Color System Core Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region ColoredTextParser Tests

        private static void TestColoredTextParser()
        {
            Console.WriteLine("--- Testing ColoredTextParser ---");

            // Test parsing color markup
            var parsed1 = ColoredTextParser.Parse("{{damage|15}} damage");
            TestBase.AssertTrue(parsed1.Count > 0, 
                "Parser should parse template syntax", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test parsing template syntax
            var parsed2 = ColoredTextParser.Parse("{{fiery|Blazing Sword}}");
            TestBase.AssertTrue(parsed2.Count > 0, 
                "Parser should parse template syntax", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test parsing character-specific markup
            var parsed3 = ColoredTextParser.Parse("[char:TestHero:damage]15[/char] damage");
            TestBase.AssertTrue(parsed3.Count > 0, 
                "Parser should parse character-specific markup", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test handling invalid markup
            var parsed4 = ColoredTextParser.Parse("plain text without markup");
            TestBase.AssertTrue(parsed4.Count > 0, 
                "Parser should handle plain text", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test parsing nested structures
            var parsed5 = ColoredTextParser.Parse("{{damage|{{critical|15}}}}");
            TestBase.AssertTrue(parsed5.Count > 0, 
                "Parser should handle nested structures", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test empty string
            var parsed6 = ColoredTextParser.Parse("");
            TestBase.AssertTrue(parsed6.Count == 0, 
                "Parser should return empty list for empty string", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region ColorTemplateLibrary Tests

        private static void TestColorTemplateLibrary()
        {
            Console.WriteLine("\n--- Testing ColorTemplateLibrary ---");

            // Test template retrieval
            bool hasFiery = ColorTemplateLibrary.HasTemplate("fiery");
            TestBase.AssertTrue(hasFiery, 
                "ColorTemplateLibrary should have fiery template", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test template shader types
            if (hasFiery)
            {
                var fierySegments = ColorTemplateLibrary.GetTemplate("fiery", "Test");
                TestBase.AssertTrue(fierySegments.Count > 0, 
                    "Fiery template should return segments", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            // Test invalid template handling
            bool hasInvalid = ColorTemplateLibrary.HasTemplate("INVALID_TEMPLATE_XYZ");
            TestBase.AssertFalse(hasInvalid, 
                "ColorTemplateLibrary should not have invalid template", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region ColoredTextBuilder Tests

        private static void TestColoredTextBuilder()
        {
            Console.WriteLine("\n--- Testing ColoredTextBuilder ---");

            // Test text building with colors
            var builder = new ColoredTextBuilder();
            builder.Add("Test", Colors.Red);
            var segments = builder.Build();
            
            TestBase.AssertTrue(segments.Count > 0, 
                "Builder should create segments", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (segments.Count > 0)
            {
                TestBase.AssertEqual("Test", segments[0].Text, 
                    "Builder should preserve text", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            // Test color method chaining
            var chainedBuilder = new ColoredTextBuilder()
                .Add("Red", Colors.Red)
                .Add("Green", Colors.Green)
                .Add("Blue", Colors.Blue);
            var chainedSegments = chainedBuilder.Build();
            
            TestBase.AssertTrue(chainedSegments.Count >= 3, 
                "Chained builder should create multiple segments", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test segment creation
            var segmentBuilder = new ColoredTextBuilder();
            segmentBuilder.Add(new ColoredText("Segment", Colors.White));
            var segmentResult = segmentBuilder.Build();
            
            TestBase.AssertTrue(segmentResult.Count > 0, 
                "Builder should accept ColoredText segments", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test builder reset (by creating new builder)
            var newBuilder = new ColoredTextBuilder();
            TestBase.AssertTrue(newBuilder.Build().Count == 0, 
                "New builder should start empty", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region ColoredTextMerger Tests

        private static void TestColoredTextMerger()
        {
            Console.WriteLine("\n--- Testing ColoredTextMerger ---");

            // Test merging adjacent segments
            var segments = new List<ColoredText> 
            { 
                new ColoredText("Hello", Colors.Red),
                new ColoredText("World", Colors.Green)
            };
            var merged = ColoredTextMerger.MergeAdjacentSegments(segments);
            
            TestBase.AssertTrue(merged.Count >= 2, 
                "Merger should combine multiple lists", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test preserving colors during merge
            if (merged.Count >= 2)
            {
                TestBase.AssertTrue(merged[0].Color == Colors.Red || merged[1].Color == Colors.Red, 
                    "Merger should preserve colors", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            // Test handling empty lists
            var emptyList = new List<ColoredText>();
            var mergedWithEmpty = ColoredTextMerger.MergeAdjacentSegments(emptyList);
            
            TestBase.AssertTrue(mergedWithEmpty.Count == 0, 
                "Merger should handle empty lists", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region ColorUtils Tests

        private static void TestColorUtils()
        {
            Console.WriteLine("\n--- Testing ColorUtils ---");

            // Test color conversions
            var red = Colors.Red;
            var systemColor = ColorUtils.ToSystemColor(red);
            TestBase.AssertTrue(systemColor.R == red.R && systemColor.G == red.G && systemColor.B == red.B, 
                "Color conversion should preserve RGB values", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test Avalonia color conversion
            var avaloniaColor = ColorUtils.ToAvaloniaColor(red);
            TestBase.AssertTrue(avaloniaColor.R == red.R && avaloniaColor.G == red.G && avaloniaColor.B == red.B, 
                "Avalonia color conversion should preserve RGB values", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test hex string conversion
            var hexColor = ColorUtils.ToAvaloniaColor("#FF0000");
            TestBase.AssertNotNull(hexColor, 
                "Hex string conversion should return color", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test RGB value conversion
            var rgbColor = ColorUtils.ToAvaloniaColor(255, 0, 0);
            TestBase.AssertTrue(rgbColor.R == 255 && rgbColor.G == 0 && rgbColor.B == 0, 
                "RGB value conversion should create correct color", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region ColorValidator Tests

        private static void TestColorValidator()
        {
            Console.WriteLine("\n--- Testing ColorValidator ---");

            // Test visibility checks on black background
            var white = Colors.White;
            var whiteBrightness = ColorValidator.GetBrightness(white);
            TestBase.AssertTrue(whiteBrightness > 0, 
                $"White should have brightness > 0, got {whiteBrightness}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var black = Colors.Black;
            var blackBrightness = ColorValidator.GetBrightness(black);
            TestBase.AssertTrue(blackBrightness < whiteBrightness, 
                "Black should have lower brightness than white", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test color equality
            var areEqual = ColorValidator.AreColorsEqual(white, white);
            TestBase.AssertTrue(areEqual, 
                "Same colors should be equal", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var notEqual = ColorValidator.AreColorsEqual(white, black);
            TestBase.AssertFalse(notEqual, 
                "Different colors should not be equal", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test invalid color handling
            var ensured = ColorValidator.EnsureVisible(white);
            TestBase.AssertNotNull(ensured, 
                "ColorValidator should ensure visibility", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}

