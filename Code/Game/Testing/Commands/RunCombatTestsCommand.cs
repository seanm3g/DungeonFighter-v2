namespace RPGGame.Game.Testing.Commands
{
    using System.Threading.Tasks;
    using RPGGame.UI.Avalonia;

    /// <summary>
    /// Command to run combat tests including UI fixes.
    /// </summary>
    public class RunCombatTestsCommand : TestCommandBase
    {
        public RunCombatTestsCommand(
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
                await TestCoordinator.ExecuteCombatTests(testRunner);
            }
        }
    }
}
