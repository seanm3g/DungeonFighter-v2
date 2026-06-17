using System;
using Avalonia.Controls;
using Avalonia.Media;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Resources;

namespace RPGGame.Tests.Unit.UI
{
    public static class SettingsInputApplierTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== SettingsInputApplier Tests ===\n");
            _testsRun = _testsPassed = _testsFailed = 0;

            ApplyTextBlock_UsesPrimaryAndMutedBrushes();
            ApplyCheckBox_UsesPrimaryBrush();
            ApplyTextBox_UsesThemeInputChrome();

            TestBase.PrintSummary("SettingsInputApplier Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void ApplyTextBlock_UsesPrimaryAndMutedBrushes()
        {
            Console.WriteLine("--- ApplyTextBlock ---");

            var primary = new TextBlock();
            SettingsInputApplier.ApplyTextBlock(primary);
            TestBase.AssertEqual(
                ((SolidColorBrush)SettingsThemeBrushes.TextPrimary).Color,
                ((SolidColorBrush)primary.Foreground!).Color,
                "Primary text block should use SettingsTextPrimary",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var muted = new TextBlock();
            SettingsInputApplier.ApplyTextBlock(muted, muted: true);
            TestBase.AssertEqual(
                ((SolidColorBrush)SettingsThemeBrushes.TextMuted).Color,
                ((SolidColorBrush)muted.Foreground!).Color,
                "Muted text block should use SettingsTextMuted",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void ApplyCheckBox_UsesPrimaryBrush()
        {
            Console.WriteLine("--- ApplyCheckBox ---");

            var checkBox = new CheckBox();
            SettingsInputApplier.ApplyCheckBox(checkBox);
            TestBase.AssertEqual(
                ((SolidColorBrush)SettingsThemeBrushes.TextPrimary).Color,
                ((SolidColorBrush)checkBox.Foreground!).Color,
                "CheckBox should use SettingsTextPrimary",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void ApplyTextBox_UsesThemeInputChrome()
        {
            Console.WriteLine("--- ApplyTextBox ---");

            var textBox = new TextBox();
            SettingsInputApplier.ApplyTextBox(textBox);
            TestBase.AssertEqual(
                ((SolidColorBrush)SettingsThemeBrushes.TextPrimary).Color,
                ((SolidColorBrush)textBox.Foreground!).Color,
                "TextBox foreground should use SettingsTextPrimary",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(
                ((SolidColorBrush)SettingsThemeBrushes.InputBackground).Color,
                ((SolidColorBrush)textBox.Background!).Color,
                "TextBox background should use SettingsInputBackground",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
