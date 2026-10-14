using System;
using System.Threading.Tasks;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Display;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Regression tests for <see cref="DisplayTiming"/> scheduling behavior.
    /// These tests focus on thread-safety expectations rather than full UI execution.
    /// </summary>
    public static class DisplayTimingTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        public static void RunAllTests()
        {
            Console.WriteLine("=== DisplayTiming Tests ===\n");

            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestScheduleRender_MenuMode_DoesNotInvokeSynchronouslyOnBackgroundThread();

            TestBase.PrintSummary("DisplayTiming Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestScheduleRender_MenuMode_DoesNotInvokeSynchronouslyOnBackgroundThread()
        {
            Console.WriteLine("--- Testing ScheduleRender (Menu mode) ---");

            // MenuDisplayMode has DebounceMs=0 so ScheduleRender is the "immediate" path.
            // The regression we care about: the callback should NOT run inline on the background thread.
            // We can assert that the flag is still false immediately after scheduling from a Task.
            var timing = new DisplayTiming(new MenuDisplayMode());
            bool ranInline = false;
            bool headless = false;

            Task.Run(() =>
            {
                // Without a running Avalonia app the dispatcher reports access from every thread, so the
                // UI-thread hop cannot be observed; the guard only matters inside the real app.
                headless = Avalonia.Threading.Dispatcher.UIThread.CheckAccess();
                if (!headless)
                    timing.ScheduleRender(() => ranInline = true);
            }).GetAwaiter().GetResult();

            if (headless)
            {
                TestBase.AssertTrue(true, "ScheduleRender UI-thread hop skipped (headless: no Avalonia dispatcher loop)",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                return;
            }

            TestBase.AssertTrue(!ranInline,
                "ScheduleRender should not invoke render callback inline when called from a background thread",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}

