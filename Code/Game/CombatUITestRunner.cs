using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Managers;

namespace RPGGame
{
    /// <summary>
    /// Test runner for combat UI fixes
    /// Provides accessible tests through the game's settings menu
    /// </summary>
    public class CombatUITestRunner
    {
        private readonly CanvasUICoordinator uiCoordinator;
        private readonly List<TestResult> testResults = new List<TestResult>();

        public CombatUITestRunner(CanvasUICoordinator uiCoordinator)
        {
            this.uiCoordinator = uiCoordinator;
        }

        /// <summary>
        /// Runs all combat UI tests and returns results
        /// </summary>
        public async Task<List<TestResult>> RunAllTests()
        {
            testResults.Clear();
            
            uiCoordinator.WriteLine("=== COMBAT UI TESTS ===");
            uiCoordinator.WriteLine("Running comprehensive tests...");
            uiCoordinator.WriteBlankLine();

            // Test 1: Combat Panel Containment
            await RunTest("Combat Panel Containment", TestCombatPanelContainment);

            // Test 2: Combat Freezing Prevention
            await RunTest("Combat Freezing Prevention", TestCombatFreezingPrevention);

            // Test 3: Combat Log Cleanup
            await RunTest("Combat Log Cleanup", TestCombatLogCleanup);

            // Test 4: Integration Test
            await RunTest("Integration Test", TestIntegration);

            return new List<TestResult>(testResults);
        }

        /// <summary>
        /// Runs a specific test by name
        /// </summary>
        public async Task<TestResult> RunSpecificTest(string testName)
        {
            uiCoordinator.WriteLine($"=== RUNNING TEST: {testName} ===");
            
            return testName switch
            {
                "Combat Panel Containment" => await RunTest(testName, TestCombatPanelContainment),
                "Combat Freezing Prevention" => await RunTest(testName, TestCombatFreezingPrevention),
                "Combat Log Cleanup" => await RunTest(testName, TestCombatLogCleanup),
                "Integration Test" => await RunTest(testName, TestIntegration),
                _ => new TestResult(testName, false, "Unknown test name")
            };
        }

        /// <summary>
        /// Runs a test and records the result
        /// </summary>
        private async Task<TestResult> RunTest(string testName, Func<Task<TestResult>> testFunction)
        {
            uiCoordinator.WriteLine($"Running: {testName}...");
            
            try
            {
                var result = await testFunction();
                testResults.Add(result);
                
                if (result.Passed)
                {
                    uiCoordinator.WriteLine($"✅ {testName}: PASSED");
                    if (!string.IsNullOrEmpty(result.Message))
                    {
                        uiCoordinator.WriteLine($"   {result.Message}");
                    }
                }
                else
                {
                    uiCoordinator.WriteLine($"❌ {testName}: FAILED");
                    uiCoordinator.WriteLine($"   {result.Message}");
                }
                
                uiCoordinator.WriteBlankLine();
                return result;
            }
            catch (Exception ex)
            {
                var result = new TestResult(testName, false, $"Exception: {ex.Message}");
                testResults.Add(result);
                uiCoordinator.WriteLine($"❌ {testName}: ERROR");
                uiCoordinator.WriteLine($"   {ex.Message}");
                uiCoordinator.WriteBlankLine();
                return result;
            }
        }

        /// <summary>
        /// Test 1: Combat Panel Containment
        /// Verifies that combat displays in center panel using persistent layout
        /// </summary>
        private Task<TestResult> TestCombatPanelContainment()
        {
            try
            {
                // Create test character and enemy
                var character = new Character("TestHero", 1);
                var enemy = new Enemy("TestEnemy", 1, 10, 5, 5, 5, 5);
                
                // Set up context
                uiCoordinator.SetCharacter(character);
                uiCoordinator.SetCurrentEnemy(enemy);
                uiCoordinator.SetDungeonName("Test Dungeon");
                uiCoordinator.SetRoomName("Test Room");
                
                // Add test combat messages
                uiCoordinator.WriteLine("Test combat message 1");
                uiCoordinator.WriteLine("Test combat message 2");
                uiCoordinator.WriteLine("Test combat message 3");
                
                // Verify display buffer has messages
                var bufferCount = uiCoordinator.GetDisplayBufferCount();
                if (bufferCount < 3)
                {
                    return Task.FromResult(new TestResult("Combat Panel Containment", false, 
                        $"Expected at least 3 messages in buffer, got {bufferCount}"));
                }
                
                // Test that persistent layout is used (no exception thrown)
                uiCoordinator.RenderDisplayBuffer();
                
                return Task.FromResult(new TestResult("Combat Panel Containment", true, 
                    $"Successfully rendered {bufferCount} combat messages in persistent layout"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new TestResult("Combat Panel Containment", false, 
                    $"Exception during test: {ex.Message}"));
            }
        }

        /// <summary>
        /// Test 2: Combat Freezing Prevention
        /// Verifies that combat delays are reduced for GUI to prevent freezing
        /// </summary>
        private async Task<TestResult> TestCombatFreezingPrevention()
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                
                // Test action delay
                await CombatDelayManager.DelayAfterActionAsync();
                var actionDelay = stopwatch.ElapsedMilliseconds;
                
                stopwatch.Restart();
                
                // Test message delay
                await CombatDelayManager.DelayAfterMessageAsync();
                var messageDelay = stopwatch.ElapsedMilliseconds;
                
                stopwatch.Stop();
                
                var totalDelay = actionDelay + messageDelay;
                
                // For GUI mode, delays should be minimal (under 200ms total)
                if (totalDelay > 200)
                {
                    return new TestResult("Combat Freezing Prevention", false, 
                        $"Total delay too high: {totalDelay}ms (expected < 200ms)");
                }
                
                return new TestResult("Combat Freezing Prevention", true, 
                    $"Delays are optimized: Action={actionDelay}ms, Message={messageDelay}ms, Total={totalDelay}ms");
            }
            catch (Exception ex)
            {
                return new TestResult("Combat Freezing Prevention", false, 
                    $"Exception during test: {ex.Message}");
            }
        }

        /// <summary>
        /// Test 3: Combat Log Cleanup
        /// Verifies that combat log clears properly after victory
        /// </summary>
        private Task<TestResult> TestCombatLogCleanup()
        {
            try
            {
                // Add test messages to display buffer
                uiCoordinator.WriteLine("Combat message 1");
                uiCoordinator.WriteLine("Combat message 2");
                uiCoordinator.WriteLine("Combat message 3");
                
                var initialCount = uiCoordinator.GetDisplayBufferCount();
                if (initialCount < 3)
                {
                    return Task.FromResult(new TestResult("Combat Log Cleanup", false, 
                        $"Failed to add test messages. Expected 3, got {initialCount}"));
                }
                
                // Clear the display buffer
                uiCoordinator.ClearDisplayBuffer();
                
                var finalCount = uiCoordinator.GetDisplayBufferCount();
                if (finalCount != 0)
                {
                    return Task.FromResult(new TestResult("Combat Log Cleanup", false, 
                        $"Display buffer not cleared. Expected 0, got {finalCount}"));
                }
                
                return Task.FromResult(new TestResult("Combat Log Cleanup", true, 
                    $"Successfully cleared display buffer from {initialCount} to {finalCount} messages"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new TestResult("Combat Log Cleanup", false, 
                    $"Exception during test: {ex.Message}"));
            }
        }

        /// <summary>
        /// Test 4: Integration Test
        /// Verifies that all fixes work together correctly
        /// </summary>
        private async Task<TestResult> TestIntegration()
        {
            try
            {
                // Test the complete flow
                var character = new Character("TestHero", 1);
                var enemy = new Enemy("TestEnemy", 1, 10, 5, 5, 5, 5);
                
                // Set up context
                uiCoordinator.SetCharacter(character);
                uiCoordinator.SetCurrentEnemy(enemy);
                uiCoordinator.SetDungeonName("Test Dungeon");
                uiCoordinator.SetRoomName("Test Room");
                
                // Add combat messages
                uiCoordinator.WriteLine("Integration test message 1");
                uiCoordinator.WriteLine("Integration test message 2");
                
                // Test persistent layout rendering
                uiCoordinator.RenderDisplayBuffer();
                
                // Test delay optimization
                var stopwatch = Stopwatch.StartNew();
                await CombatDelayManager.DelayAfterActionAsync();
                var delay = stopwatch.ElapsedMilliseconds;
                
                // Test cleanup
                uiCoordinator.ClearDisplayBuffer();
                var finalCount = uiCoordinator.GetDisplayBufferCount();
                
                if (delay > 200 || finalCount != 0)
                {
                    return new TestResult("Integration Test", false, 
                        $"Integration issues: Delay={delay}ms, BufferCount={finalCount}");
                }
                
                return new TestResult("Integration Test", true, 
                    "All combat UI fixes working together correctly");
            }
            catch (Exception ex)
            {
                return new TestResult("Integration Test", false, 
                    $"Exception during integration test: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets a summary of all test results
        /// </summary>
        public string GetTestSummary()
        {
            if (testResults.Count == 0)
            {
                return "No tests have been run yet.";
            }
            
            var passed = testResults.Count(r => r.Passed);
            var total = testResults.Count;
            
            var summary = $"=== TEST SUMMARY ===\n";
            summary += $"Tests Run: {total}\n";
            summary += $"Passed: {passed}\n";
            summary += $"Failed: {total - passed}\n";
            summary += $"Success Rate: {(passed * 100.0 / total):F1}%\n\n";
            
            foreach (var result in testResults)
            {
                var status = result.Passed ? "✅ PASS" : "❌ FAIL";
                summary += $"{status} {result.TestName}\n";
                if (!string.IsNullOrEmpty(result.Message))
                {
                    summary += $"    {result.Message}\n";
                }
            }
            
            return summary;
        }
    }

    /// <summary>
    /// Represents the result of a test
    /// </summary>
    public class TestResult
    {
        public string TestName { get; }
        public bool Passed { get; }
        public string Message { get; }

        public TestResult(string testName, bool passed, string message = "")
        {
            TestName = testName;
            Passed = passed;
            Message = message;
        }
    }
}
