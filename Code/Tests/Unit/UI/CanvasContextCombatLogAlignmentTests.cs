using RPGGame;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Managers;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Sticky enemy display name keeps center combat-log right-alignment after <see cref="ICanvasContextManager.ClearCurrentEnemy"/>.
    /// </summary>
    public static class CanvasContextCombatLogAlignmentTests
    {
        public static void RunAllTests()
        {
            int run = 0, passed = 0, failed = 0;
            System.Console.WriteLine("=== CanvasContextCombatLogAlignment Tests ===\n");

            TestStickySurvivesClearEnemy(ref run, ref passed, ref failed);
            TestStickyUpdatesOnNewEnemy(ref run, ref passed, ref failed);
            TestClearSticky(ref run, ref passed, ref failed);

            TestBase.PrintSummary("CanvasContextCombatLogAlignment Tests", run, passed, failed);
        }

        private static void TestStickySurvivesClearEnemy(ref int run, ref int passed, ref int failed)
        {
            var ctx = new CanvasContextManager();
            var lich = new Enemy("Lich", 1, 50, 8, 6, 4, 4);
            ctx.SetCurrentEnemy(lich);
            TestBase.AssertEqual("Lich", ctx.GetCombatLogEnemyAlignmentName(), "live enemy name", ref run, ref passed, ref failed);
            ctx.ClearCurrentEnemy();
            TestBase.AssertNull(ctx.GetCurrentEnemy(), "current enemy cleared", ref run, ref passed, ref failed);
            TestBase.AssertEqual("Lich", ctx.GetCombatLogEnemyAlignmentName(), "sticky name after clear", ref run, ref passed, ref failed);
        }

        private static void TestStickyUpdatesOnNewEnemy(ref int run, ref int passed, ref int failed)
        {
            var ctx = new CanvasContextManager();
            ctx.SetCurrentEnemy(new Enemy("Orc", 1, 50, 8, 6, 4, 4));
            ctx.ClearCurrentEnemy();
            ctx.SetCurrentEnemy(new Enemy("Dragon", 1, 50, 8, 6, 4, 4));
            TestBase.AssertEqual("Dragon", ctx.GetCombatLogEnemyAlignmentName(), "new fight updates alignment name", ref run, ref passed, ref failed);
        }

        private static void TestClearSticky(ref int run, ref int passed, ref int failed)
        {
            var ctx = new CanvasContextManager();
            ctx.SetCurrentEnemy(new Enemy("Wraith", 1, 50, 8, 6, 4, 4));
            ctx.ClearCurrentEnemy();
            ctx.ClearCombatLogEnemyAlignmentSticky();
            TestBase.AssertNull(ctx.GetCombatLogEnemyAlignmentName(), "sticky cleared", ref run, ref passed, ref failed);
        }
    }
}
