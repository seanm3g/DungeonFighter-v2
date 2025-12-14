namespace RPGGame.Game.Testing.Commands
{
    using System.Threading.Tasks;
    using RPGGame.UI.Avalonia;

    /// <summary>
    /// Command to run comprehensive color system tests.
    /// </summary>
    public class RunColorSystemTestsCommand : TestCommandBase
    {
        public RunColorSystemTestsCommand(
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
                await TestCoordinator.ExecuteColorSystemTests(testRunner);
            }
        }
    }
}
