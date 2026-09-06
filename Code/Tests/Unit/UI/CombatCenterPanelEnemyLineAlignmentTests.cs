using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Display;
using RPGGame.UI.BlockDisplay;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests.Unit.UI
{
    public static class CombatCenterPanelEnemyLineAlignmentTests
    {
        public static void RunAllTests()
        {
            int run = 0, passed = 0, failed = 0;
            Console.WriteLine("=== CombatCenterPanelEnemyLineAlignment Tests ===\n");

            TestEnemyPrimaryLine(ref run, ref passed, ref failed);
            TestIndentedRollLineNotAlignedForPrimaryOnly(ref run, ref passed, ref failed);
            TestNullEnemy(ref run, ref passed, ref failed);
            TestHeroLine(ref run, ref passed, ref failed);
            TestResolveEnemyRollRightAlignedAfterPrimary(ref run, ref passed, ref failed);
            TestResolveHeroRollStaysLeft(ref run, ref passed, ref failed);
            TestResolveMultiEnemyEncounterNames(ref run, ref passed, ref failed);
            TestResolveDoesNotThrowWhenLiveCountGrowsDuringRead(ref run, ref passed, ref failed);

            TestBase.PrintSummary("CombatCenterPanelEnemyLineAlignment Tests", run, passed, failed);
        }

        private static void TestEnemyPrimaryLine(ref int run, ref int passed, ref int failed)
        {
            var segments = new List<ColoredText>
            {
                new ColoredText("Goblin", Colors.Orange),
                new ColoredText(" hits Hero for 3 damage", Colors.White)
            };
            TestBase.AssertTrue(
                CombatCenterPanelEnemyLineAlignment.ShouldRightAlignEnemyPrimaryCombatLine(segments, "Goblin"),
                "enemy primary line starting with enemy name",
                ref run, ref passed, ref failed);
        }

        private static void TestIndentedRollLineNotAlignedForPrimaryOnly(ref int run, ref int passed, ref int failed)
        {
            var segments = new List<ColoredText>
            {
                new ColoredText(BlockMessageCollector.ActionBlockSubsequentIndent + "Roll: 12", Colors.Gray)
            };
            TestBase.AssertFalse(
                CombatCenterPanelEnemyLineAlignment.ShouldRightAlignEnemyPrimaryCombatLine(segments, "Goblin"),
                "indented roll line alone is not treated as enemy primary",
                ref run, ref passed, ref failed);
        }

        private static void TestNullEnemy(ref int run, ref int passed, ref int failed)
        {
            var segments = new List<ColoredText> { new ColoredText("Goblin hits", Colors.White) };
            TestBase.AssertFalse(
                CombatCenterPanelEnemyLineAlignment.ShouldRightAlignEnemyPrimaryCombatLine(segments, null),
                "null enemy name disables alignment",
                ref run, ref passed, ref failed);
        }

        private static void TestHeroLine(ref int run, ref int passed, ref int failed)
        {
            var segments = new List<ColoredText>
            {
                new ColoredText("Hero", Colors.Green),
                new ColoredText(" hits Goblin", Colors.White)
            };
            TestBase.AssertFalse(
                CombatCenterPanelEnemyLineAlignment.ShouldRightAlignEnemyPrimaryCombatLine(segments, "Goblin"),
                "hero attacker line must not match goblin prefix",
                ref run, ref passed, ref failed);
        }

        private static void TestResolveEnemyRollRightAlignedAfterPrimary(ref int run, ref int passed, ref int failed)
        {
            var lines = new List<List<ColoredText>>
            {
                new List<ColoredText>
                {
                    new ColoredText("Skeleton", Colors.White),
                    new ColoredText(" hits Hero", Colors.White)
                },
                new List<ColoredText> { new ColoredText(BlockMessageCollector.ActionBlockSubsequentIndent + "(roll: 10)", Colors.Gray) }
            };
            var flags = CombatCenterPanelEnemyLineAlignment.ResolveRightAlignFlags(lines, "Skeleton", "Roran Dawnblade");
            TestBase.AssertTrue(flags[0] && flags[1], "enemy primary and indented roll both right-align", ref run, ref passed, ref failed);
        }

        private static void TestResolveHeroRollStaysLeft(ref int run, ref int passed, ref int failed)
        {
            var lines = new List<List<ColoredText>>
            {
                new List<ColoredText>
                {
                    new ColoredText("Skeleton", Colors.White),
                    new ColoredText(" hits", Colors.White)
                },
                new List<ColoredText> { new ColoredText(BlockMessageCollector.ActionBlockSubsequentIndent + "enemy roll", Colors.Gray) },
                new List<ColoredText>
                {
                    new ColoredText("Roran Dawnblade", Colors.Yellow),
                    new ColoredText(" CRITICAL MISS", Colors.Red)
                },
                new List<ColoredText> { new ColoredText(BlockMessageCollector.ActionBlockSubsequentIndent + "(roll: 1)", Colors.Gray) }
            };
            var flags = CombatCenterPanelEnemyLineAlignment.ResolveRightAlignFlags(lines, "Skeleton", "Roran Dawnblade");
            TestBase.AssertTrue(flags[0] && flags[1] && !flags[2] && !flags[3],
                "after hero primary, hero indented line stays left",
                ref run, ref passed, ref failed);
        }

        private static void TestResolveMultiEnemyEncounterNames(ref int run, ref int passed, ref int failed)
        {
            var lines = new List<List<ColoredText>>
            {
                new List<ColoredText>
                {
                    new ColoredText("Wight", Colors.White),
                    new ColoredText(" misses Hero", Colors.White)
                },
                new List<ColoredText> { new ColoredText(BlockMessageCollector.ActionBlockSubsequentIndent + "(roll: 5)", Colors.Gray) },
                new List<ColoredText>
                {
                    new ColoredText("Wraith", Colors.White),
                    new ColoredText(" hits Hero", Colors.White)
                },
                new List<ColoredText> { new ColoredText(BlockMessageCollector.ActionBlockSubsequentIndent + "(roll: 10)", Colors.Gray) }
            };
            IReadOnlyList<string> names = new[] { "Wraith", "Wight" };
            var flags = CombatCenterPanelEnemyLineAlignment.ResolveRightAlignFlags(lines, names, "Hero");
            TestBase.AssertTrue(flags[0] && flags[1] && flags[2] && flags[3],
                "both enemy encounters right-align when multiple enemy names are supplied",
                ref run, ref passed, ref failed);
        }

        private static void TestResolveDoesNotThrowWhenLiveCountGrowsDuringRead(ref int run, ref int passed, ref int failed)
        {
            var inner = new List<List<ColoredText>>
            {
                new List<ColoredText>
                {
                    new ColoredText("Skeleton", Colors.White),
                    new ColoredText(" Attacks Tyler Quickstrike...", Colors.White)
                },
                new List<ColoredText> { new ColoredText(BlockMessageCollector.ActionBlockSubsequentIndent + "(roll: 17)", Colors.Gray) }
            };
            var growing = new CountGrowsAfterIndexerReadList(inner);
            bool[]? flags = null;
            Exception? thrown = null;
            try
            {
                flags = CombatCenterPanelEnemyLineAlignment.ResolveRightAlignFlags(growing, "Skeleton", "Tyler Quickstrike");
            }
            catch (Exception ex)
            {
                thrown = ex;
            }

            TestBase.AssertTrue(thrown == null, "live buffer Count growth must not throw", ref run, ref passed, ref failed);
            TestBase.AssertTrue(flags != null && flags.Length == 2 && flags[0] && flags[1],
                "alignment flags stay sized to the original snapshot",
                ref run, ref passed, ref failed);
        }

        /// <summary>
        /// Mimics the combat-thread race: after the first row is read, Count inflates so a loop
        /// bound on Count walks past a flags array allocated from the earlier Count.
        /// </summary>
        private sealed class CountGrowsAfterIndexerReadList : IReadOnlyList<List<ColoredText>>
        {
            private readonly List<List<ColoredText>> _items;
            private int _reportedCount;

            public CountGrowsAfterIndexerReadList(List<List<ColoredText>> items)
            {
                _items = items;
                _reportedCount = items.Count;
            }

            public int Count => _reportedCount;

            public List<ColoredText> this[int index]
            {
                get
                {
                    _reportedCount = _items.Count + 8;
                    if (index >= 0 && index < _items.Count)
                        return _items[index];
                    return new List<ColoredText> { new ColoredText("late", Colors.White) };
                }
            }

            public IEnumerator<List<ColoredText>> GetEnumerator() => _items.GetEnumerator();

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
