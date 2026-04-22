using System;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Renderers.Validators;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Tests that combat rendering validation respects game state (menu/death must block stale combat repaints).
    /// </summary>
    public static class CombatRenderingValidatorTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== CombatRenderingValidator Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestBlocksCombatWhenDeathStateEvenIfContextMatches();
            TestAllowsCombatWhenDungeonStateAndContextMatches();

            TestBase.PrintSummary("CombatRenderingValidator Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestBlocksCombatWhenDeathStateEvenIfContextMatches()
        {
            Console.WriteLine("--- Death state blocks combat repaint ---");
            var ctx = new CanvasContextManager();
            var hero = new Character("Hero", 1);
            ctx.SetCurrentCharacter(hero);

            var gsm = new GameStateManager();
            gsm.SetCurrentPlayer(hero);
            gsm.TransitionToState(GameState.Death);

            var validator = new CombatRenderingValidator(ctx, gsm);
            TestBase.AssertTrue(!validator.ValidateCharacterActive(hero),
                "ValidateCharacterActive should be false in Death even when context holds the same character",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestAllowsCombatWhenDungeonStateAndContextMatches()
        {
            Console.WriteLine("--- Dungeon state allows combat when context matches ---");
            var ctx = new CanvasContextManager();
            var hero = new Character("Hero", 1);
            ctx.SetCurrentCharacter(hero);

            var gsm = new GameStateManager();
            gsm.SetCurrentPlayer(hero);
            gsm.TransitionToState(GameState.Dungeon);

            var validator = new CombatRenderingValidator(ctx, gsm);
            TestBase.AssertTrue(validator.ValidateCharacterActive(hero),
                "ValidateCharacterActive should be true in Dungeon when context matches",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
