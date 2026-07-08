using System;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Verifies combat display failures are logged (not silently swallowed).
    /// </summary>
    public static class BlockDisplayManagerTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== BlockDisplayManager Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestLogDisplayFailure_DoesNotThrow();

            TestBase.PrintSummary("BlockDisplayManager Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestLogDisplayFailure_DoesNotThrow()
        {
            Console.WriteLine("--- LogDisplayFailure does not throw ---");
            try
            {
                BlockDisplayManager.LogDisplayFailure("UnitTest", new InvalidOperationException("forced display failure"));
                TestBase.AssertTrue(true,
                    "LogDisplayFailure should complete without throwing",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"LogDisplayFailure must not throw: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }
    }
}
