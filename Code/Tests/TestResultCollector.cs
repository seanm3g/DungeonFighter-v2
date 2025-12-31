using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Tests
{
    /// <summary>
    /// Collects test results across all test suites for comprehensive reporting
    /// </summary>
    public static class TestResultCollector
    {
        private static readonly List<TestResult> _results = new List<TestResult>();
        private static string? _currentCategory;

        /// <summary>
        /// Represents a single test result
        /// </summary>
        public class TestResult
        {
            public string Category { get; set; } = string.Empty;
            public string TestName { get; set; } = string.Empty;
            public bool Passed { get; set; }
            public string Message { get; set; } = string.Empty;
        }

        /// <summary>
        /// Sets the current test category (e.g., "PHASE 1: CORE MECHANICS")
        /// </summary>
        public static void SetCurrentCategory(string category)
        {
            _currentCategory = category;
        }

        /// <summary>
        /// Records a test result
        /// </summary>
        public static void RecordTest(string testName, bool passed, string message)
        {
            _results.Add(new TestResult
            {
                Category = _currentCategory ?? "Unknown",
                TestName = testName,
                Passed = passed,
                Message = message
            });
        }

        /// <summary>
        /// Clears all collected results
        /// </summary>
        public static void Clear()
        {
            _results.Clear();
            _currentCategory = null;
        }

        /// <summary>
        /// Gets overall statistics
        /// </summary>
        public static (int total, int passed, int failed, double successRate) GetStatistics()
        {
            int total = _results.Count;
            int passed = _results.Count(r => r.Passed);
            int failed = total - passed;
            double successRate = total > 0 ? (passed * 100.0 / total) : 0.0;

            return (total, passed, failed, successRate);
        }

        /// <summary>
        /// Gets all failed tests grouped by category
        /// </summary>
        public static Dictionary<string, List<TestResult>> GetFailedTestsByCategory()
        {
            var failedTests = _results.Where(r => !r.Passed).ToList();
            return failedTests
                .GroupBy(r => r.Category)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        /// <summary>
        /// Gets all test results grouped by category
        /// </summary>
        public static Dictionary<string, List<TestResult>> GetAllTestsByCategory()
        {
            return _results
                .GroupBy(r => r.Category)
                .ToDictionary(g => g.Key, g => g.ToList());
        }
    }
}
