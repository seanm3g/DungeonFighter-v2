using System;
using RPGGame.Actions.Execution;
using RPGGame.Combat.Formatting;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Unit tests for ActionExecution extracted components
    /// </summary>
    public static class ActionExecutionComponentsTest
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;
        
        /// <summary>
        /// Runs all ActionExecution component tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== ActionExecution Components Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;
            
            // ActionSpeedCalculator tests
            TestCalculateActualActionSpeed();
            TestCalculateActualActionSpeedNullAction();
            
            PrintSummary();
        }
        
        #region ActionSpeedCalculator Tests
        
        private static void TestCalculateActualActionSpeed()
        {
            Console.WriteLine("--- Testing CalculateActualActionSpeed ---");
            
            // Create mock actor and action
            var character = new Character("Test", 1);
            var action = new Action
            {
                Name = "Test Action",
                Length = 1.5
            };
            
            double speed = ActionSpeedCalculator.CalculateActualActionSpeed(character, action);
            
            AssertTrue(speed > 0, $"Should calculate positive speed, got: {speed}");
        }
        
        private static void TestCalculateActualActionSpeedNullAction()
        {
            Console.WriteLine("\n--- Testing CalculateActualActionSpeedNullAction ---");
            
            var character = new Character("Test", 1);
            double speed = ActionSpeedCalculator.CalculateActualActionSpeed(character, null!);
            
            AssertTrue(speed == 0, $"Should return 0 for null action, got: {speed}");
        }
        
        #endregion
        
        #region Helper Methods
        
        private static void AssertTrue(bool condition, string message)
        {
            _testsRun++;
            if (condition)
            {
                _testsPassed++;
                Console.WriteLine($"  ✓ {message}");
            }
            else
            {
                _testsFailed++;
                Console.WriteLine($"  ✗ FAILED: {message}");
            }
        }
        
        private static void PrintSummary()
        {
            Console.WriteLine("\n=== Test Summary ===");
            Console.WriteLine($"Total Tests: {_testsRun}");
            Console.WriteLine($"Passed: {_testsPassed}");
            Console.WriteLine($"Failed: {_testsFailed}");
            Console.WriteLine($"Success Rate: {(_testsPassed * 100.0 / _testsRun):F1}%");
            
            if (_testsFailed == 0)
            {
                Console.WriteLine("\n✅ All tests passed!");
            }
            else
            {
                Console.WriteLine($"\n❌ {_testsFailed} test(s) failed");
            }
        }
        
        #endregion
    }
}

