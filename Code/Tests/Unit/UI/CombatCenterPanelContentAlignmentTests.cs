using System.Collections.Generic;
using Avalonia.Media;
using RPGGame;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Display;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests.Unit.UI
{
    public static class CombatCenterPanelContentAlignmentTests
    {
        public static void RunAllTests()
        {
            int run = 0, passed = 0, failed = 0;
            Console.WriteLine("=== CombatCenterPanelContentAlignment Tests ===\n");

            TestMenuType(ref run, ref passed, ref failed);
            TestEnvironmentalType(ref run, ref passed, ref failed);
            TestLevelUpHeuristic(ref run, ref passed, ref failed);
            TestDungeonHeaderHeuristic(ref run, ref passed, ref failed);
            TestCombatLineNotCenteredByDefault(ref run, ref passed, ref failed);
            TestResolveFlagsLength(ref run, ref passed, ref failed);

            TestBase.PrintSummary("CombatCenterPanelContentAlignment Tests", run, passed, failed);
        }

        private static void TestMenuType(ref int run, ref int passed, ref int failed)
        {
            var segments = new List<ColoredText> { new ColoredText("1. Option", Colors.White) };
            TestBase.AssertTrue(
                CombatCenterPanelContentAlignment.ShouldCenterLine(segments, UIMessageType.Menu),
                "UIMessageType.Menu centers",
                ref run, ref passed, ref failed);
        }

        private static void TestEnvironmentalType(ref int run, ref int passed, ref int failed)
        {
            var segments = new List<ColoredText> { new ColoredText("flavor", Colors.White) };
            TestBase.AssertTrue(
                CombatCenterPanelContentAlignment.ShouldCenterLine(segments, UIMessageType.Environmental),
                "UIMessageType.Environmental centers",
                ref run, ref passed, ref failed);
        }

        private static void TestLevelUpHeuristic(ref int run, ref int passed, ref int failed)
        {
            var segments = new List<ColoredText> { new ColoredText("You reached level 5!", Colors.White) };
            TestBase.AssertTrue(
                CombatCenterPanelContentAlignment.ShouldCenterLine(segments, UIMessageType.System),
                "level-up line centers by content",
                ref run, ref passed, ref failed);
        }

        private static void TestDungeonHeaderHeuristic(ref int run, ref int passed, ref int failed)
        {
            var segments = new List<ColoredText> { new ColoredText("=== ENTERING ROOM ===", Colors.White) };
            TestBase.AssertTrue(
                CombatCenterPanelContentAlignment.ShouldCenterLine(segments, UIMessageType.System),
                "room header centers by content",
                ref run, ref passed, ref failed);
        }

        private static void TestCombatLineNotCenteredByDefault(ref int run, ref int passed, ref int failed)
        {
            var segments = new List<ColoredText> { new ColoredText("Goblin hits Hero for 2", Colors.White) };
            TestBase.AssertFalse(
                CombatCenterPanelContentAlignment.ShouldCenterLine(segments, UIMessageType.System),
                "generic combat system line not centered",
                ref run, ref passed, ref failed);
        }

        private static void TestResolveFlagsLength(ref int run, ref int passed, ref int failed)
        {
            var lines = new List<List<ColoredText>>
            {
                new List<ColoredText> { new ColoredText("Menu", Colors.White) },
                new List<ColoredText> { new ColoredText("Fight", Colors.White) }
            };
            var types = new List<UIMessageType> { UIMessageType.Menu, UIMessageType.System };
            var flags = CombatCenterPanelContentAlignment.ResolveCenterAlignFlags(lines, types);
            TestBase.AssertTrue(flags.Length == 2 && flags[0] && !flags[1],
                "ResolveCenterAlignFlags matches per-row types",
                ref run, ref passed, ref failed);
        }
    }
}
