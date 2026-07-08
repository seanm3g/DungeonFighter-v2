using System;
using System.Threading;
using System.Threading.Tasks;
using RPGGame;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Game
{
    /// <summary>
    /// Verifies background dungeon cancellation token plumbing.
    /// </summary>
    public static class BackgroundDungeonTaskManagerTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== BackgroundDungeonTaskManager Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestCancelDungeon_SignalsToken().GetAwaiter().GetResult();
            TestRestart_CancelsPreviousRun().GetAwaiter().GetResult();

            TestBase.PrintSummary("BackgroundDungeonTaskManager Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static async Task TestCancelDungeon_SignalsToken()
        {
            Console.WriteLine("--- CancelDungeon signals token ---");
            var manager = new BackgroundDungeonTaskManager();
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            manager.StartBackgroundDungeon("hero1", "Hero", "Crypt", async ct =>
            {
                entered.TrySetResult();
                try
                {
                    await Task.Delay(Timeout.Infinite, ct);
                }
                catch (OperationCanceledException)
                {
                    cancelled.TrySetResult();
                    throw;
                }
            });

            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            TestBase.AssertTrue(manager.HasActiveDungeon("hero1"),
                "Dungeon should be active after start",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            manager.CancelDungeon("hero1");
            await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(5));
            TestBase.AssertTrue(true, "Cancellation token was observed", ref _testsRun, ref _testsPassed, ref _testsFailed);

            await manager.WaitForDungeonCompletion("hero1", timeoutMs: 10000);
            var info = manager.GetDungeonRunInfo("hero1");
            // Info may already be cleaned up after delay; if present, should be cancelled
            if (info != null)
            {
                TestBase.AssertTrue(info.WasCancelled || info.IsComplete,
                    "Run info should mark cancelled/complete after CancelDungeon",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            else
            {
                TestBase.AssertTrue(true, "Run info cleaned up after cancel", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static async Task TestRestart_CancelsPreviousRun()
        {
            Console.WriteLine("\n--- Restart cancels previous run ---");
            var manager = new BackgroundDungeonTaskManager();
            var firstCancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var secondStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            manager.StartBackgroundDungeon("hero2", "Hero2", "Cave", async ct =>
            {
                try
                {
                    await Task.Delay(Timeout.Infinite, ct);
                }
                catch (OperationCanceledException)
                {
                    firstCancelled.TrySetResult();
                    throw;
                }
            });

            await Task.Delay(50);
            manager.StartBackgroundDungeon("hero2", "Hero2", "Cave2", async ct =>
            {
                secondStarted.TrySetResult();
                await Task.Delay(100, ct);
            });

            await firstCancelled.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await secondStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            TestBase.AssertTrue(true, "Restart cancelled first run and started second",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            await manager.WaitForDungeonCompletion("hero2", timeoutMs: 10000);
        }
    }
}
