using System;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Managers;

namespace RPGGame.Tests.Unit.UI
{
    public static class ItemSuffixesTabManagerTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== ItemSuffixesTabManager Tests ===\n");
            _testsRun = _testsPassed = _testsFailed = 0;

            TestConstructor();

            TestBase.PrintSummary("ItemSuffixesTabManager Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestConstructor()
        {
            Console.WriteLine("--- Testing Constructor ---");
            try
            {
                var manager = new ItemSuffixesTabManager(null);
                TestBase.AssertTrue(manager != null,
                    "ItemSuffixesTabManager should be created successfully",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"ItemSuffixesTabManager constructor failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }
    }
}
