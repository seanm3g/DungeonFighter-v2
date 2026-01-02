using System;

namespace RPGGame.Tests
{
    /// <summary>
    /// Base class for test helpers - provides common assertion and summary functionality
    /// All methods are public static so they can be called from static test classes
    /// </summary>
    public static class TestBase
    {
        private static string? _currentTestName;

        /// <summary>
        /// Sets the current test name for result tracking
        /// </summary>
        public static void SetCurrentTestName(string testName)
        {
            _currentTestName = testName;
        }

        public static void AssertTrue(bool condition, string message, ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            testsRun++;
            if (condition)
            {
                testsPassed++;
                Console.WriteLine($"  ✓ {message}");
                TestResultCollector.RecordTest(_currentTestName ?? message, true, message);
            }
            else
            {
                testsFailed++;
                Console.WriteLine($"  ✗ FAILED: {message}");
                TestResultCollector.RecordTest(_currentTestName ?? message, false, message);
            }
        }
        
        public static void AssertFalse(bool condition, string message, ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            AssertTrue(!condition, message, ref testsRun, ref testsPassed, ref testsFailed);
        }
        
        public static void AssertEqual<T>(T expected, T actual, string message, ref int testsRun, ref int testsPassed, ref int testsFailed) where T : IEquatable<T>
        {
            testsRun++;
            if (expected.Equals(actual))
            {
                testsPassed++;
                Console.WriteLine($"  ✓ {message}");
                TestResultCollector.RecordTest(_currentTestName ?? message, true, message);
            }
            else
            {
                testsFailed++;
                string failureMessage = $"{message} (Expected: {expected}, Actual: {actual})";
                Console.WriteLine($"  ✗ FAILED: {failureMessage}");
                TestResultCollector.RecordTest(_currentTestName ?? message, false, failureMessage);
            }
        }
        
        public static void AssertEqual(int expected, int actual, string message, ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            testsRun++;
            if (expected == actual)
            {
                testsPassed++;
                Console.WriteLine($"  ✓ {message}");
                TestResultCollector.RecordTest(_currentTestName ?? message, true, message);
            }
            else
            {
                testsFailed++;
                string failureMessage = $"{message} (Expected: {expected}, Actual: {actual})";
                Console.WriteLine($"  ✗ FAILED: {failureMessage}");
                TestResultCollector.RecordTest(_currentTestName ?? message, false, failureMessage);
            }
        }
        
        // Overload for enums (which don't implement IEquatable<T>)
        // Using object parameters to avoid signature conflict with generic method
        public static void AssertEqualEnum(Enum expected, Enum actual, string message, ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            testsRun++;
            if (expected.Equals(actual))
            {
                testsPassed++;
                Console.WriteLine($"  ✓ {message}");
                TestResultCollector.RecordTest(_currentTestName ?? message, true, message);
            }
            else
            {
                testsFailed++;
                string failureMessage = $"{message} (Expected: {expected}, Actual: {actual})";
                Console.WriteLine($"  ✗ FAILED: {failureMessage}");
                TestResultCollector.RecordTest(_currentTestName ?? message, false, failureMessage);
            }
        }
        
        // Overload for nullable strings (to handle null comparisons properly)
        public static void AssertEqual(string? expected, string? actual, string message, ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            testsRun++;
            if (expected == actual || (expected != null && expected.Equals(actual)))
            {
                testsPassed++;
                Console.WriteLine($"  ✓ {message}");
                TestResultCollector.RecordTest(_currentTestName ?? message, true, message);
            }
            else
            {
                testsFailed++;
                string failureMessage = $"{message} (Expected: {expected ?? "null"}, Actual: {actual ?? "null"})";
                Console.WriteLine($"  ✗ FAILED: {failureMessage}");
                TestResultCollector.RecordTest(_currentTestName ?? message, false, failureMessage);
            }
        }
        
        public static void AssertNotNull(object? obj, string message, ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            AssertTrue(obj != null, message, ref testsRun, ref testsPassed, ref testsFailed);
        }
        
        public static void AssertNull(object? obj, string message, ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            AssertTrue(obj == null, message, ref testsRun, ref testsPassed, ref testsFailed);
        }
        
        public static void PrintSummary(string testSuiteName, int testsRun, int testsPassed, int testsFailed)
        {
            Console.WriteLine($"\n=== {testSuiteName} Summary ===");
            Console.WriteLine($"Total Tests: {testsRun}");
            Console.WriteLine($"Passed: {testsPassed}");
            Console.WriteLine($"Failed: {testsFailed}");
            
            if (testsRun > 0)
            {
                Console.WriteLine($"Success Rate: {(testsPassed * 100.0 / testsRun):F1}%");
            }
            
            if (testsFailed == 0)
            {
                Console.WriteLine("\n✅ All tests passed!");
            }
            else
            {
                Console.WriteLine($"\n❌ {testsFailed} test(s) failed");
            }
        }
    }
}

