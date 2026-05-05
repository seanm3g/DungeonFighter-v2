using System;
using RPGGame;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Managers;

namespace RPGGame.Tests.Unit.UI
{
    public static class ItemSuffixesDataCoordinatorTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== ItemSuffixesDataCoordinator Tests ===\n");
            _testsRun = _testsPassed = _testsFailed = 0;

            ParseMechanics_LineFormat();
            ParseMechanics_BracketFormat();
            ParseMechanics_FallbackLegacy();
            ParseMechanics_EmptyNoFallback();
            FormatMechanics_RoundTripDisplay();

            TestBase.PrintSummary("ItemSuffixesDataCoordinator Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void ParseMechanics_LineFormat()
        {
            var list = ItemSuffixesDataCoordinator.ParseMechanicsText("Armor: 1\r\nHealth: 10", "", 0);
            TestBase.AssertTrue(list != null && list.Count == 2 && list[0].StatType == "Armor" && list[0].Value == 1 && list[1].Value == 10,
                "multiline StatType: value should parse",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void ParseMechanics_BracketFormat()
        {
            var list = ItemSuffixesDataCoordinator.ParseMechanicsText("[Armor:1, Health:10]", "", 0);
            TestBase.AssertTrue(list != null && list.Count >= 2,
                "bracket mechanics should parse",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void ParseMechanics_FallbackLegacy()
        {
            var list = ItemSuffixesDataCoordinator.ParseMechanicsText("  ", "STR", 3);
            TestBase.AssertTrue(list != null && list.Count == 1 && list[0].StatType == "STR" && list[0].Value == 3,
                "empty mechanics with legacy fallback",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void ParseMechanics_EmptyNoFallback()
        {
            var list = ItemSuffixesDataCoordinator.ParseMechanicsText("", "", 0);
            TestBase.AssertTrue(list == null,
                "empty mechanics without stat type yields null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void FormatMechanics_RoundTripDisplay()
        {
            var bonus = new StatBonus
            {
                Mechanics = new System.Collections.Generic.List<StatBonusMechanic>
                {
                    new StatBonusMechanic { StatType = "Armor", Value = 2 }
                }
            };
            string s = ItemSuffixesDataCoordinator.FormatMechanicsForDisplay(bonus);
            TestBase.AssertTrue(s.Contains("Armor", StringComparison.Ordinal) && s.Contains("2", StringComparison.Ordinal),
                "format display includes stat and value",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
