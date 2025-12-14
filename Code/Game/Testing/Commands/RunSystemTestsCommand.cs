namespace RPGGame.Game.Testing.Commands
{
    using System.Threading.Tasks;
    using RPGGame.UI.Avalonia;

    /// <summary>
    /// Command to run system-specific tests.
    /// </summary>
    public class RunSystemTestsCommand : TestCommandBase
    {
        private readonly string systemName;

        public RunSystemTestsCommand(
            CanvasUICoordinator canvasUI,
            TestExecutionCoordinator? testCoordinator,
            GameStateManager stateManager,
            string systemName)
            : base(canvasUI, testCoordinator, stateManager)
        {
            this.systemName = systemName;
        }

        public override async Task ExecuteAsync()
        {
            var testRunner = new GameSystemTestRunner(CanvasUI);
            if (TestCoordinator != null)
            {
                await TestCoordinator.ExecuteSystemTest(testRunner, systemName);
            }
        }
    }
}
