namespace RPGGame.Game.Testing.Commands
{
    using System.Threading.Tasks;
    using RPGGame.UI.Avalonia;

    /// <summary>
    /// Command to return to the settings menu.
    /// </summary>
    public class ReturnToSettingsCommand : TestCommandBase
    {
        private readonly System.Action? showMainMenuAction;

        public ReturnToSettingsCommand(
            CanvasUICoordinator canvasUI,
            TestExecutionCoordinator? testCoordinator,
            GameStateManager stateManager,
            System.Action? showMainMenuAction)
            : base(canvasUI, testCoordinator, stateManager)
        {
            this.showMainMenuAction = showMainMenuAction;
        }

        public override Task ExecuteAsync()
        {
            StateManager.TransitionToState(GameState.Settings);
            showMainMenuAction?.Invoke();
            return Task.CompletedTask;
        }
    }
}
