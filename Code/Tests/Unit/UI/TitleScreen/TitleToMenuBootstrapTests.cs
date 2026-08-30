using System;
using Avalonia.Controls;
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
            TestHiddenUntilFirstFrameThenReveal();

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

        private static void TestHiddenUntilFirstFrameThenReveal()
        {
            Console.WriteLine("--- Testing window stays hidden until first title frame ---");
            TestBase.AssertEqual(0.0, TitleToMenuBootstrap.GetStartupWindowOpacity(titleFirstFrameReady: false),
                "Window opacity should be 0 until the first title frame is painted",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1.0, TitleToMenuBootstrap.GetStartupWindowOpacity(titleFirstFrameReady: true),
                "Window opacity should be 1 after the first title frame is painted",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(
                !TitleToMenuBootstrap.GetStartupShowInTaskbar(titleFirstFrameReady: false),
                "Taskbar button should stay hidden until the title frame is ready",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(
                TitleToMenuBootstrap.GetStartupShowInTaskbar(titleFirstFrameReady: true),
                "Taskbar button should appear with the title frame",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqualEnum(WindowState.Minimized,
                TitleToMenuBootstrap.GetStartupWindowState(titleFirstFrameReady: false),
                "Window should start minimized so a black frame cannot flash on Windows",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqualEnum(WindowState.Normal,
                TitleToMenuBootstrap.GetStartupWindowState(titleFirstFrameReady: true),
                "Window should restore to normal after the first title frame is painted",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
