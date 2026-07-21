using System;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Handlers;

namespace RPGGame.Tests.Unit.UI.TitleScreen
{
    /// <summary>
    /// Regression: title keypress must not rebuild the game if warmup already finished.
    /// </summary>
    public static class TitleToMenuBootstrapTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== TitleToMenu Bootstrap Tests ===\n");

            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestWarmupCompleteMeansInstantMenu();
            TestWarmupIncompleteMeansWait();
            TestTitleKeyOnlyAfterReady();

            TestBase.PrintSummary("TitleToMenu Bootstrap Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestWarmupCompleteMeansInstantMenu()
        {
            Console.WriteLine("--- Testing warmup-complete → instant menu ---");
            TestBase.AssertTrue(
                TitleToMenuBootstrap.CanShowMenuImmediately(warmupCompleted: true),
                "Warmup complete should allow immediate main menu",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestWarmupIncompleteMeansWait()
        {
            Console.WriteLine("--- Testing warmup-incomplete → wait ---");
            TestBase.AssertTrue(
                !TitleToMenuBootstrap.CanShowMenuImmediately(warmupCompleted: false),
                "Warmup incomplete should not claim instant menu",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestTitleKeyOnlyAfterReady()
        {
            Console.WriteLine("--- Testing title key gate ---");
            TestBase.AssertTrue(
                !TitleToMenuBootstrap.ShouldAcceptTitleKey(waitingForKeyAfterAnimation: false),
                "Keys before first idle frame should be ignored",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(
                TitleToMenuBootstrap.ShouldAcceptTitleKey(waitingForKeyAfterAnimation: true),
                "Keys after ready should be accepted",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
