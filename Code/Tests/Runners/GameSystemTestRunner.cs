using System;
using RPGGame;
using RPGGame.Tests.Unit.Game;

namespace RPGGame.Tests.Runners
{
    /// <summary>
    /// Test runner for Game system tests
    /// </summary>
    public static class GameSystemTestRunner
    {
        /// <summary>
        /// Runs all Game system tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine(GameConstants.StandardSeparator);
            Console.WriteLine("  GAME SYSTEM TEST SUITE");
            Console.WriteLine($"{GameConstants.StandardSeparator}\n");

            GameCoordinatorTests.RunAllTests();
            Console.WriteLine();
            GameStateManagerTests.RunAllTests();
            Console.WriteLine();
            GameInitializerTests.RunAllTests();
            Console.WriteLine();
            DungeonRunnerManagerTests.RunAllTests();
            Console.WriteLine();
            DungeonDisplayManagerTests.RunAllTests();
            Console.WriteLine();
            ActionEditorHandlerTests.RunAllTests();
            Console.WriteLine();
            CharacterManagementHandlerTests.RunAllTests();
            Console.WriteLine();
            AdjustmentExecutorTests.RunAllTests();
            Console.WriteLine();
            BattleStatisticsHandlerTests.RunAllTests();
            Console.WriteLine();
            MatchupAnalyzerTests.RunAllTests();
            Console.WriteLine();
            GameScreenCoordinatorTests.RunAllTests();
            Console.WriteLine();
            ClaudeAIGamePlayerTests.RunAllTests();
        }
    }
}
