using System;
using System.Linq;
using RPGGame.Utils;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Unit tests for PerformanceMonitor
    /// </summary>
    public static class PerformanceMonitorTest
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== PerformanceMonitor Tests ===\n");
            
            TestBasicMeasurement();
            TestStatistics();
            TestRegressionDetection();
            TestTimer();
            TestDisable();
            TestClear();
            
            Console.WriteLine("\n✓ All PerformanceMonitor tests passed!");
        }

        private static void TestBasicMeasurement()
        {
            PerformanceMonitor.Clear();
            PerformanceMonitor.RecordMeasurement("TestOperation", 100);
            PerformanceMonitor.RecordMeasurement("TestOperation", 200);
            
            var stats = PerformanceMonitor.GetStats("TestOperation");
            TestHarnessBase.AssertEqual(2, stats.Count, "Should record 2 measurements");
            TestHarnessBase.AssertEqual(150.0, stats.AverageMs, "Average should be 150ms");
        }

        private static void TestStatistics()
        {
            PerformanceMonitor.Clear();
            PerformanceMonitor.RecordMeasurement("StatsTest", 50);
            PerformanceMonitor.RecordMeasurement("StatsTest", 100);
            PerformanceMonitor.RecordMeasurement("StatsTest", 150);
            
            var stats = PerformanceMonitor.GetStats("StatsTest");
            TestHarnessBase.AssertEqual(3, stats.Count, "Should have 3 measurements");
            TestHarnessBase.AssertTrue(stats.MinMs == 50, "Min should be 50");
            TestHarnessBase.AssertTrue(stats.MaxMs == 150, "Max should be 150");
        }

        private static void TestRegressionDetection()
        {
            PerformanceMonitor.Clear();
            
            // Create baseline (fast measurements)
            for (int i = 0; i < 20; i++)
            {
                PerformanceMonitor.RecordMeasurement("RegressionTest", 50);
            }
            
            // Add slow measurements (regression)
            for (int i = 0; i < 10; i++)
            {
                PerformanceMonitor.RecordMeasurement("RegressionTest", 200);
            }
            
            bool hasRegression = PerformanceMonitor.CheckRegression("RegressionTest", 1.5);
            TestHarnessBase.AssertTrue(hasRegression, "Should detect regression");
        }

        private static void TestTimer()
        {
            PerformanceMonitor.Clear();
            
            using (PerformanceMonitor.StartTimer("TimerTest"))
            {
                System.Threading.Thread.Sleep(10);
            }
            
            var stats = PerformanceMonitor.GetStats("TimerTest");
            TestHarnessBase.AssertEqual(1, stats.Count, "Should record 1 measurement from timer");
        }

        private static void TestDisable()
        {
            PerformanceMonitor.Enabled = false;
            PerformanceMonitor.Clear();
            PerformanceMonitor.RecordMeasurement("DisabledTest", 100);
            
            var stats = PerformanceMonitor.GetStats("DisabledTest");
            TestHarnessBase.AssertEqual(0, stats.Count, "Should not record when disabled");
            
            PerformanceMonitor.Enabled = true;
        }

        private static void TestClear()
        {
            PerformanceMonitor.RecordMeasurement("ClearTest", 100);
            PerformanceMonitor.Clear();
            
            var stats = PerformanceMonitor.GetStats("ClearTest");
            TestHarnessBase.AssertEqual(0, stats.Count, "Should clear all measurements");
        }
    }
}
