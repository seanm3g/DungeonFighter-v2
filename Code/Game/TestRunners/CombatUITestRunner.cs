using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Managers;

namespace RPGGame.Game.TestRunners
{
    /// <summary>
    /// Test runner for combat UI fix tests
    /// </summary>
    public class CombatUITestRunner
    {
        private readonly CanvasUICoordinator uiCoordinator;
        private readonly List<TestResult> testResults;

        public CombatUITestRunner(CanvasUICoordinator uiCoordinator, List<TestResult> testResults)
        {
            this.uiCoordinator = uiCoordinator;
            this.testResults = testResults;
        }

        public async Task<List<TestResult>> RunAllTests()
        {
            await RunTest("Combat Panel Containment", TestCombatPanelContainment);
            await RunTest("Combat Freezing Prevention", TestCombatFreezingPrevention);
            await RunTest("Combat Log Cleanup", TestCombatLogCleanup);
            
            return new List<TestResult>(testResults);
        }

        private async Task<TestResult> RunTest(string testName, Func<Task<TestResult>> testFunction)
        {
            uiCoordinator.WriteLine($"Running: {testName}...");
            
            try
            {
                var result = await Task.Run(async () => await testFunction()).ConfigureAwait(false);
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

        private Task<TestResult> TestCombatPanelContainment()
        {
            try
            {
                var character = new Character("TestHero", 1);
                var enemy = new Enemy("TestEnemy", 1, 10, 5, 5, 5, 5);
                
                uiCoordinator.SetCharacter(character);
                uiCoordinator.SetCurrentEnemy(enemy);
                uiCoordinator.SetDungeonName("Test Dungeon");
                uiCoordinator.SetRoomName("Test Room");
                
                uiCoordinator.WriteLine("Test combat message 1");
                uiCoordinator.WriteLine("Test combat message 2");
                uiCoordinator.WriteLine("Test combat message 3");
                
                var bufferCount = uiCoordinator.GetDisplayBufferCount();
                if (bufferCount < 3)
                {
                    return Task.FromResult(new TestResult("Combat Panel Containment", false, 
                        $"Expected at least 3 messages in buffer, got {bufferCount}"));
                }
                
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

        private async Task<TestResult> TestCombatFreezingPrevention()
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                
                await CombatDelayManager.DelayAfterActionAsync();
                var actionDelay = stopwatch.ElapsedMilliseconds;
                
                stopwatch.Restart();
                
                await CombatDelayManager.DelayAfterMessageAsync();
                var messageDelay = stopwatch.ElapsedMilliseconds;
                
                stopwatch.Stop();
                
                var totalDelay = actionDelay + messageDelay;
                
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

        private Task<TestResult> TestCombatLogCleanup()
        {
            try
            {
                uiCoordinator.WriteLine("Combat message 1");
                uiCoordinator.WriteLine("Combat message 2");
                uiCoordinator.WriteLine("Combat message 3");
                
                var initialCount = uiCoordinator.GetDisplayBufferCount();
                if (initialCount < 3)
                {
                    return Task.FromResult(new TestResult("Combat Log Cleanup", false, 
                        $"Failed to add test messages. Expected 3, got {initialCount}"));
                }
                
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
    }
}

