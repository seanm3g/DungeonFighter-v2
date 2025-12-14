namespace RPGGame.Game.Testing.Commands
{
    using System.Threading.Tasks;
    using RPGGame.UI.Avalonia;

    /// <summary>
    /// Command to run inventory and dungeon tests together.
    /// </summary>
    public class RunInventoryDungeonTestsCommand : TestCommandBase
    {
        public RunInventoryDungeonTestsCommand(
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
                await TestCoordinator.ExecuteInventoryAndDungeonTests(testRunner);
            }
        }
    }
}
