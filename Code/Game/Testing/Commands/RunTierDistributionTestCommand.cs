namespace RPGGame.Game.Testing.Commands
{
    using System;
    using System.Threading.Tasks;
    using RPGGame;
    using RPGGame.UI.Avalonia;
    using RPGGame.Tests.Unit;

    /// <summary>
    /// Command to run the tier distribution verification test.
    /// </summary>
    public class RunTierDistributionTestCommand : TestCommandBase
    {
        public RunTierDistributionTestCommand(
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
                new System.Action(() => TierDistributionTest.TestTierDistribution()),
                "TIER DISTRIBUTION VERIFICATION TEST",
                "Testing tier distribution across various player/dungeon level scenarios.",
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
