namespace RPGGame.Game.Testing.Commands
{
    using System.Threading.Tasks;
    using RPGGame.UI.Avalonia;

    /// <summary>
    /// Command to run advanced mechanics and integration tests together.
    /// </summary>
    public class RunAdvancedTestsCommand : TestCommandBase
    {
        public RunAdvancedTestsCommand(
            CanvasUICoordinator canvasUI,
            TestExecutionCoordinator? testCoordinator,
            GameStateManager stateManager)
            : base(canvasUI, testCoordinator, stateManager)
        {
        }

        public override async Task ExecuteAsync()
        {
            var testRunner = new GameSystemTestRunner(CanvasUI);
            if (TestCoordinator != null)
            {
                await TestCoordinator.ExecuteAdvancedAndIntegrationTests(testRunner);
            }
        }
    }
}
