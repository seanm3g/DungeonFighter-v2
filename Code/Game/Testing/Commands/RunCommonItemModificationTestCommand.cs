namespace RPGGame.Game.Testing.Commands
{
    using System;
    using System.Threading.Tasks;
    using RPGGame;
    using RPGGame.UI.Avalonia;

    /// <summary>
    /// Command to run the common item modification test.
    /// </summary>
    public class RunCommonItemModificationTestCommand : TestCommandBase
    {
        public RunCommonItemModificationTestCommand(
            CanvasUICoordinator canvasUI,
            TestExecutionCoordinator? testCoordinator,
            GameStateManager stateManager)
            : base(canvasUI, testCoordinator, stateManager)
        {
        }

        public override async Task ExecuteAsync()
        {
            bool success = await ConsoleTestRunnerHelper.RunConsoleTestWithUI(
                CanvasUI,
                new System.Action(() => RPGGame.TestManager.RunCommonItemModificationTest()),
                "COMMON ITEM MODIFICATION TEST",
                "This will generate 1000 Common items and verify the 25% chance for modifications.",
                "Test completed!");

            if (success)
            {
                TestUICoordinator.MarkWaitingForReturn(TestCoordinator);
            }
            else
            {
                TestUICoordinator.MarkWaitingForReturn(TestCoordinator);
            }
        }
    }
}
