using System;
using Avalonia.Input;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Utils;

namespace RPGGame.Tests.Unit.UI
{
    public static class KeyInputConverterTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== KeyInputConverter Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            TestBase.AssertTrue(
                KeyInputConverter.IsCombatLogCopyChord(Key.C, KeyModifiers.Control),
                "Ctrl+C is copy chord",
                ref run, ref passed, ref failed);

            TestBase.AssertTrue(
                KeyInputConverter.IsCombatLogCopyChord(Key.C, KeyModifiers.Meta),
                "Cmd+C (Meta) is copy chord",
                ref run, ref passed, ref failed);

            TestBase.AssertTrue(
                KeyInputConverter.IsCombatLogCopyChord(Key.C, KeyModifiers.Control | KeyModifiers.Shift),
                "Ctrl+Shift+C counts as copy chord",
                ref run, ref passed, ref failed);

            TestBase.AssertTrue(
                !KeyInputConverter.IsCombatLogCopyChord(Key.C, KeyModifiers.None),
                "C alone is not copy chord",
                ref run, ref passed, ref failed);

            TestBase.AssertTrue(
                !KeyInputConverter.IsCombatLogCopyChord(Key.C, KeyModifiers.Shift),
                "Shift+C is not copy chord",
                ref run, ref passed, ref failed);

            TestBase.AssertTrue(
                !KeyInputConverter.IsCombatLogCopyChord(Key.V, KeyModifiers.Control),
                "Ctrl+V is not copy chord",
                ref run, ref passed, ref failed);

            Console.WriteLine($"\nKeyInputConverter: {passed}/{run} passed, {failed} failed\n");
        }
    }
}
