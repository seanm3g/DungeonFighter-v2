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
            TestTitleType(ref run, ref passed, ref failed);
            TestLevelUpHeuristic(ref run, ref passed, ref failed);
            TestDungeonHeaderHeuristic(ref run, ref passed, ref failed);
            TestEnemyEncounterAppearanceHeuristic(ref run, ref passed, ref failed);
            TestEnemyDefeatedAndRemainingHealthHeuristic(ref run, ref passed, ref failed);
            TestCombatLineNotCenteredByDefault(ref run, ref passed, ref failed);
            TestOutcomeSummaryCentersNonEmptyLines(ref run, ref passed, ref failed);
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

        private static void TestTitleType(ref int run, ref int passed, ref int failed)
        {
            var segments = new List<ColoredText> { new ColoredText("A quiet corridor stretches ahead.", Colors.White) };
            TestBase.AssertTrue(
                CombatCenterPanelContentAlignment.ShouldCenterLine(segments, UIMessageType.Title),
                "UIMessageType.Title centers room-style narrative",
                ref run, ref passed, ref failed);
        }

        private static void TestLevelUpHeuristic(ref int run, ref int passed, ref int failed)
        {
            var segments = new List<ColoredText> { new ColoredText("You reached level 5!", Colors.White) };
            TestBase.AssertTrue(
                CombatCenterPanelContentAlignment.ShouldCenterLine(segments, UIMessageType.System),
                "level-up line centers by content",
                ref run, ref passed, ref failed);

            var actionSlotOne = new List<ColoredText>
            {
                new ColoredText("Gained ", Colors.White),
                new ColoredText("+1 action slot!", Colors.LimeGreen)
            };
            TestBase.AssertTrue(
                CombatCenterPanelContentAlignment.ShouldCenterLine(actionSlotOne, UIMessageType.System),
                "gained one action slot centers (multi-segment)",
                ref run, ref passed, ref failed);

            var actionSlotsPlural = new List<ColoredText> { new ColoredText("Gained +2 action slots!", Colors.White) };
            TestBase.AssertTrue(
                CombatCenterPanelContentAlignment.ShouldCenterLine(actionSlotsPlural, UIMessageType.System),
                "gained multiple action slots centers",
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

        private static void TestEnemyEncounterAppearanceHeuristic(ref int run, ref int passed, ref int failed)
        {
            var withWeapon = new List<ColoredText>
            {
                new ColoredText("A ", Colors.White),
                new ColoredText("Goblin", Colors.LimeGreen),
                new ColoredText(" with ", Colors.White),
                new ColoredText("Claw", Colors.OrangeRed),
                new ColoredText(" appears.", Colors.White)
            };
            TestBase.AssertTrue(
                CombatCenterPanelContentAlignment.ShouldCenterLine(withWeapon, UIMessageType.System),
                "enemy encounter with weapon centers by content (System)",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                CombatCenterPanelContentAlignment.ShouldCenterLine(withWeapon, UIMessageType.Combat),
                "enemy encounter line centers when logged as Combat",
                ref run, ref passed, ref failed);

            var noWeapon = new List<ColoredText>
            {
                new ColoredText("A ", Colors.White),
                new ColoredText("Slime", Colors.LimeGreen),
                new ColoredText(" appears.", Colors.White)
            };
            TestBase.AssertTrue(
                CombatCenterPanelContentAlignment.ShouldCenterLine(noWeapon, UIMessageType.System),
                "enemy encounter without weapon centers by content",
                ref run, ref passed, ref failed);
        }

        private static void TestEnemyDefeatedAndRemainingHealthHeuristic(ref int run, ref int passed, ref int failed)
        {
            var defeated = new List<ColoredText>
            {
                new ColoredText("Bat", Colors.Purple),
                new ColoredText(" has been defeated!", Colors.LimeGreen)
            };
            TestBase.AssertTrue(
                CombatCenterPanelContentAlignment.ShouldCenterLine(defeated, UIMessageType.System),
                "enemy defeated summary centers by content (System)",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                CombatCenterPanelContentAlignment.ShouldCenterLine(defeated, UIMessageType.Combat),
                "enemy defeated summary centers when logged as Combat",
                ref run, ref passed, ref failed);

            var health = new List<ColoredText> { new ColoredText("Remaining Health: 54/69", Colors.Gold) };
            TestBase.AssertTrue(
                CombatCenterPanelContentAlignment.ShouldCenterLine(health, UIMessageType.System),
                "remaining health line centers by content",
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

        private static void TestOutcomeSummaryCentersNonEmptyLines(ref int run, ref int passed, ref int failed)
        {
            var victoryHeader = new List<ColoredText> { new ColoredText("=== VICTORY! ===", Colors.White) };
            TestBase.AssertTrue(
                CombatCenterPanelContentAlignment.ShouldCenterLine(victoryHeader, UIMessageType.OutcomeSummary),
                "OutcomeSummary centers non-empty victory lines in the completion log",
                ref run, ref passed, ref failed);

            TestBase.AssertFalse(
                CombatCenterPanelContentAlignment.ShouldCenterLine(new List<ColoredText>(), UIMessageType.OutcomeSummary),
                "OutcomeSummary blank rows do not center-align",
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
