namespace RPGGame.Game.Testing.Commands
{
    using System.Threading.Tasks;
    using RPGGame.UI.Avalonia;

    /// <summary>
    /// Command to run combat log filtering tests.
    /// Tests that combat logs are properly filtered by character and game state.
    /// </summary>
    public class RunCombatLogFilteringTestsCommand : TestCommandBase
    {
        public RunCombatLogFilteringTestsCommand(
            CanvasUICoordinator canvasUI,
            TestExecutionCoordinator? testCoordinator,
            GameStateManager stateManager)
            : base(canvasUI, testCoordinator, stateManager)
        {
        }

        public override async Task ExecuteAsync()
        {
            var testRunner = new CombatLogFilteringTestRunner(CanvasUI, StateManager);
            if (TestCoordinator != null)
            {
                await TestCoordinator.ExecuteTest(
                    async () => await testRunner.RunAllTests(),
                    "Combat Log Filtering Tests",
                    logToConsole: true);
            }
        }
    }
}

