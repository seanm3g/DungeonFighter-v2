using System;
using RPGGame;
using RPGGame.Tests.Unit.Game;
using RPGGame.Tests.Unit.Game.Handlers;
using RPGGame.Tests.Unit.Game.Input;

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

            // Core Game System Tests
            GameCoordinatorTests.RunAllTests();
            Console.WriteLine();
            GameStateManagerTests.RunAllTests();
            Console.WriteLine();
            GameStateValidatorTests.RunAllTests();
            Console.WriteLine();
            GameInitializerTests.RunAllTests();
            Console.WriteLine();
            GameInitializationManagerTests.RunAllTests();
            Console.WriteLine();
            GameLoaderTests.RunAllTests();
            Console.WriteLine();
            FileManagerTests.RunAllTests();
            Console.WriteLine();
            DungeonRunnerManagerTests.RunAllTests();
            Console.WriteLine();
            DungeonDisplayManagerTests.RunAllTests();
            Console.WriteLine();
            
            // Handler Tests
            MainMenuHandlerTests.RunAllTests();
            Console.WriteLine();
            CharacterMenuHandlerTests.RunAllTests();
            Console.WriteLine();
            SettingsMenuHandlerTests.RunAllTests();
            Console.WriteLine();
            WeaponSelectionHandlerTests.RunAllTests();
            Console.WriteLine();
            CharacterCreationHandlerTests.RunAllTests();
            Console.WriteLine();
            DungeonSelectionHandlerTests.RunAllTests();
            Console.WriteLine();
            DungeonCompletionHandlerTests.RunAllTests();
            Console.WriteLine();
            DeathScreenHandlerTests.RunAllTests();
            Console.WriteLine();
            LoadCharacterSelectionHandlerTests.RunAllTests();
            Console.WriteLine();
            DungeonExitChoiceHandlerTests.RunAllTests();
            Console.WriteLine();
            HandlerInitializerTests.RunAllTests();
            Console.WriteLine();
            
            // Input Routing Tests
            GameInputRouterTests.RunAllTests();
            Console.WriteLine();
            EscapeKeyHandlerTests.RunAllTests();
            Console.WriteLine();
            
            // Other Game System Tests
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
