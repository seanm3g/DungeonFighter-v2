namespace RPGGame.Game.Testing.Commands
{
    using System;
    using System.Threading.Tasks;
    using RPGGame;
    using RPGGame.UI.Avalonia;

    /// <summary>
    /// Command to run the item generation analysis test.
    /// </summary>
    public class RunItemGenerationTestCommand : TestCommandBase
    {
        public RunItemGenerationTestCommand(
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
                new System.Action(() => RPGGame.TestManager.RunItemGenerationTest()),
                "ITEM GENERATION ANALYSIS TEST",
                "This will generate 100 items at each level from 1-20 and analyze the results.",
                "Test completed! Results saved to 'item_generation_test_results.txt'");

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
