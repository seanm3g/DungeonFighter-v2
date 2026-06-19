using System;
using RPGGame;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Components;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>Display formatting for SliderWithTextBox (no Avalonia headless required).</summary>
    public static class SliderWithTextBoxLogicTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== SliderWithTextBox Logic Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestFormat_IntegerRange();
            TestFormat_DecimalRange();
            TestFormat_LargeMaximumUsesInteger();
            TestClampAndFormat_NeverEmptyForValidInput();
            TestClampAndFormat_ClampsOutOfRangeValue();
            TestClampAndFormat_InvalidRangeReturnsEmpty();

            TestBase.PrintSummary("SliderWithTextBox Logic Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestFormat_IntegerRange()
        {
            Console.WriteLine("--- Integer tick frequency formats as whole number ---");
            string text = SliderWithTextBox.FormatValueForDisplay(100.4, 20, 200, 1.0);
            TestBase.AssertEqual("100", text,
                "Integer-range slider shows rounded whole number",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestFormat_DecimalRange()
        {
            Console.WriteLine("--- Decimal range formats with two places ---");
            string text = SliderWithTextBox.FormatValueForDisplay(1.25, 0.25, 3.0, 0.05);
            TestBase.AssertEqual("1.25", text,
                "Decimal slider shows F2 format",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestFormat_LargeMaximumUsesInteger()
        {
            Console.WriteLine("--- Maximum >= 100 uses integer display ---");
            string text = SliderWithTextBox.FormatValueForDisplay(99.6, 0, 150, 0.1);
            TestBase.AssertEqual("100", text,
                "Large max range uses integer display even with fractional tick",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestClampAndFormat_NeverEmptyForValidInput()
        {
            Console.WriteLine("--- ClampAndFormat never returns empty for valid input ---");
            string text = SliderWithTextBox.ClampAndFormatForDisplay(200, 20, 200, 1.0);
            TestBase.AssertTrue(!string.IsNullOrEmpty(text),
                "ClampAndFormat produces non-empty display text",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("200", text,
                "ClampAndFormat shows clamped integer value",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestClampAndFormat_ClampsOutOfRangeValue()
        {
            Console.WriteLine("--- ClampAndFormat recovers from out-of-range value ---");
            string text = SliderWithTextBox.ClampAndFormatForDisplay(0, 20, 200, 1.0);
            TestBase.AssertEqual("20", text,
                "Out-of-range low value clamps to minimum for display",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestClampAndFormat_InvalidRangeReturnsEmpty()
        {
            Console.WriteLine("--- ClampAndFormat returns empty when min exceeds max ---");
            string text = SliderWithTextBox.ClampAndFormatForDisplay(100, 20, 1, 1.0);
            TestBase.AssertEqual(string.Empty, text,
                "Incomplete range during init does not throw",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
