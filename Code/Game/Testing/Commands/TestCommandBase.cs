namespace RPGGame.Game.Testing.Commands
{
    using System.Threading.Tasks;
    using RPGGame.UI.Avalonia;

    /// <summary>
    /// Base class for test commands that provides common dependencies.
    /// </summary>
    public abstract class TestCommandBase : ITestCommand
    {
        protected readonly CanvasUICoordinator CanvasUI;
        protected readonly TestExecutionCoordinator? TestCoordinator;
        protected readonly GameStateManager StateManager;

        protected TestCommandBase(
            CanvasUICoordinator canvasUI,
            TestExecutionCoordinator? testCoordinator,
            GameStateManager stateManager)
        {
            CanvasUI = canvasUI;
            TestCoordinator = testCoordinator;
            StateManager = stateManager;
        }

        public abstract Task ExecuteAsync();
    }
}
