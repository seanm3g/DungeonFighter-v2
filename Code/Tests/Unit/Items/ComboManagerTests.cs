using System;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Items
{
    /// <summary>
    /// Comprehensive tests for ComboManager
    /// Tests combo management functionality
    /// Note: Some methods require user input, so we test what we can
    /// </summary>
    public static class ComboManagerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all ComboManager tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== ComboManager Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestConstructor();

            // Note: ManageComboActions and other methods require user input
            // so we can't easily test them in unit tests without mocking
            // These would be better suited for integration tests

            TestBase.PrintSummary("ComboManager Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Constructor Tests

        private static void TestConstructor()
        {
            Console.WriteLine("--- Testing Constructor ---");

            var character = TestDataBuilders.Character()
                .WithName("TestPlayer")
                .WithLevel(1)
                .Build();

            var inventory = new List<Item>();
            var displayManager = new GameDisplayManager(character, inventory);

            // Test normal construction
            var comboManager = new ComboManager(character, displayManager);
            TestBase.AssertNotNull(comboManager,
                "ComboManager should be created",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
