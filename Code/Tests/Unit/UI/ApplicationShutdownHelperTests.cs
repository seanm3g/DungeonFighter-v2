using System;
using RPGGame.Tests;
using RPGGame.UI.Avalonia;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Tests for GUI shutdown cleanup (window X / Exit Game must release DF.exe).
    /// </summary>
    public static class ApplicationShutdownHelperTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== ApplicationShutdownHelper Tests ===\n");

            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestPerformShutdown_WithoutForceExit_IsIdempotent();
            TestPerformShutdown_MarksShutdownStarted();
            TestForceExitWatchdogMs_IsPositiveAndBounded();

            TestBase.PrintSummary("ApplicationShutdownHelper Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestPerformShutdown_WithoutForceExit_IsIdempotent()
        {
            Console.WriteLine("\n--- Testing PerformShutdown idempotency (no process exit) ---");

            ApplicationShutdownHelper.ResetForTests();
            try
            {
                ApplicationShutdownHelper.PerformShutdown(forceProcessExit: false);
                ApplicationShutdownHelper.PerformShutdown(forceProcessExit: false);

                TestBase.AssertTrue(
                    ApplicationShutdownHelper.HasShutdownStarted,
                    "First PerformShutdown should mark shutdown started",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                ApplicationShutdownHelper.ResetForTests();
            }
        }

        private static void TestPerformShutdown_MarksShutdownStarted()
        {
            Console.WriteLine("\n--- Testing HasShutdownStarted after cleanup ---");

            ApplicationShutdownHelper.ResetForTests();
            try
            {
                TestBase.AssertTrue(
                    !ApplicationShutdownHelper.HasShutdownStarted,
                    "ResetForTests should clear shutdown flag",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                ApplicationShutdownHelper.PerformShutdown(forceProcessExit: false);

                TestBase.AssertTrue(
                    ApplicationShutdownHelper.HasShutdownStarted,
                    "PerformShutdown should set HasShutdownStarted",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                ApplicationShutdownHelper.ResetForTests();
            }
        }

        private static void TestForceExitWatchdogMs_IsPositiveAndBounded()
        {
            Console.WriteLine("\n--- Testing ForceExitWatchdogMs bound ---");

            TestBase.AssertTrue(
                ApplicationShutdownHelper.ForceExitWatchdogMs > 0
                && ApplicationShutdownHelper.ForceExitWatchdogMs <= 5000,
                "Force-exit watchdog should be a short positive timeout (≤5s)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
