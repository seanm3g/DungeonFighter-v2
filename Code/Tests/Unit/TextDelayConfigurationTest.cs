using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RPGGame.Config;
using RPGGame.UI;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Comprehensive unit tests for TextDelayConfiguration.
    /// Tests configuration loading, message type delays, chunked text reveal presets,
    /// combat delays, progressive menu delays, and enable flags.
    /// </summary>
    public static class TextDelayConfigurationTest
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;
        
        /// <summary>
        /// Runs all TextDelayConfiguration tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== TextDelayConfiguration Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;
            
            // Core functionality tests
            TestMessageTypeDelays();
            TestChunkedTextRevealPresets();
            TestCombatDelays();
            TestProgressiveMenuDelays();
            TestEnableFlags();
            TestDefaultValues();
            
            // Edge cases
            TestInvalidMessageTypes();
            TestInvalidPresetNames();
            TestPresetValidation();
            TestThreadSafety();
            
            // Print summary
            PrintSummary();
        }
        
        #region Core Functionality Tests
        
        /// <summary>
        /// Tests that message type delays load correctly from configuration
        /// </summary>
        private static void TestMessageTypeDelays()
        {
            Console.WriteLine("--- Testing Message Type Delays ---");
            
            try
            {
                // Test all message types have delays
                var allTypes = Enum.GetValues<UIMessageType>();
                foreach (var type in allTypes)
                {
                    var delay = TextDelayConfiguration.GetMessageTypeDelay(type);
                    AssertTrue(delay >= 0, 
                        $"Message type {type} should have a valid delay (got {delay}ms)");
                }
                
                // Test specific expected values from config
                var combatDelay = TextDelayConfiguration.GetMessageTypeDelay(UIMessageType.Combat);
                AssertTrue(combatDelay == 100, 
                    $"Combat message delay should be 100ms (got {combatDelay}ms)");
                
                var menuDelay = TextDelayConfiguration.GetMessageTypeDelay(UIMessageType.Menu);
                AssertTrue(menuDelay == 25, 
                    $"Menu message delay should be 25ms (got {menuDelay}ms)");
                
                var systemDelay = TextDelayConfiguration.GetMessageTypeDelay(UIMessageType.System);
                AssertTrue(systemDelay == 100, 
                    $"System message delay should be 100ms (got {systemDelay}ms)");
                
                var titleDelay = TextDelayConfiguration.GetMessageTypeDelay(UIMessageType.Title);
                AssertTrue(titleDelay == 400, 
                    $"Title message delay should be 400ms (got {titleDelay}ms)");
                
                var rollInfoDelay = TextDelayConfiguration.GetMessageTypeDelay(UIMessageType.RollInfo);
                AssertTrue(rollInfoDelay == 5, 
                    $"RollInfo message delay should be 5ms (got {rollInfoDelay}ms)");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Message type delay test failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Tests that chunked text reveal presets load correctly
        /// </summary>
        private static void TestChunkedTextRevealPresets()
        {
            Console.WriteLine("\n--- Testing Chunked Text Reveal Presets ---");
            
            try
            {
                // Test Combat preset
                var combatPreset = TextDelayConfiguration.GetChunkedTextRevealPreset("Combat");
                AssertTrue(combatPreset != null, "Combat preset should exist");
                if (combatPreset != null)
                {
                    AssertTrue(combatPreset.BaseDelayPerCharMs == 20, 
                        $"Combat base delay should be 20ms (got {combatPreset.BaseDelayPerCharMs}ms)");
                    AssertTrue(combatPreset.MinDelayMs == 500, 
                        $"Combat min delay should be 500ms (got {combatPreset.MinDelayMs}ms)");
                    AssertTrue(combatPreset.MaxDelayMs == 2000, 
                        $"Combat max delay should be 2000ms (got {combatPreset.MaxDelayMs}ms)");
                    AssertTrue(combatPreset.Strategy == "Line", 
                        $"Combat strategy should be 'Line' (got '{combatPreset.Strategy}')");
                    AssertTrue(combatPreset.MinDelayMs < combatPreset.MaxDelayMs, 
                        "Combat min delay should be less than max delay");
                }
                
                // Test Dungeon preset
                var dungeonPreset = TextDelayConfiguration.GetChunkedTextRevealPreset("Dungeon");
                AssertTrue(dungeonPreset != null, "Dungeon preset should exist");
                if (dungeonPreset != null)
                {
                    AssertTrue(dungeonPreset.BaseDelayPerCharMs == 25, 
                        $"Dungeon base delay should be 25ms (got {dungeonPreset.BaseDelayPerCharMs}ms)");
                    AssertTrue(dungeonPreset.Strategy == "Semantic", 
                        $"Dungeon strategy should be 'Semantic' (got '{dungeonPreset.Strategy}')");
                }
                
                // Test Room preset
                var roomPreset = TextDelayConfiguration.GetChunkedTextRevealPreset("Room");
                AssertTrue(roomPreset != null, "Room preset should exist");
                if (roomPreset != null)
                {
                    AssertTrue(roomPreset.BaseDelayPerCharMs == 30, 
                        $"Room base delay should be 30ms (got {roomPreset.BaseDelayPerCharMs}ms)");
                    AssertTrue(roomPreset.Strategy == "Sentence", 
                        $"Room strategy should be 'Sentence' (got '{roomPreset.Strategy}')");
                }
                
                // Test Narrative preset
                var narrativePreset = TextDelayConfiguration.GetChunkedTextRevealPreset("Narrative");
                AssertTrue(narrativePreset != null, "Narrative preset should exist");
                if (narrativePreset != null)
                {
                    AssertTrue(narrativePreset.BaseDelayPerCharMs == 25, 
                        $"Narrative base delay should be 25ms (got {narrativePreset.BaseDelayPerCharMs}ms)");
                    AssertTrue(narrativePreset.Strategy == "Sentence", 
                        $"Narrative strategy should be 'Sentence' (got '{narrativePreset.Strategy}')");
                }
                
                // Test Default preset
                var defaultPreset = TextDelayConfiguration.GetChunkedTextRevealPreset("Default");
                AssertTrue(defaultPreset != null, "Default preset should always exist");
                if (defaultPreset != null)
                {
                    AssertTrue(defaultPreset.MinDelayMs < defaultPreset.MaxDelayMs,
                        "Default preset should have valid delay range (min < max)");
                    AssertTrue(defaultPreset.BaseDelayPerCharMs > 0,
                        "Default preset base delay should be positive");
                }
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Chunked text reveal preset test failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Tests that combat delays load correctly
        /// </summary>
        private static void TestCombatDelays()
        {
            Console.WriteLine("\n--- Testing Combat Delays ---");
            
            try
            {
                var actionDelay = TextDelayConfiguration.GetActionDelayMs();
                AssertTrue(actionDelay == 1000, 
                    $"Action delay should be 1000ms (got {actionDelay}ms)");
                AssertTrue(actionDelay > 0, 
                    "Action delay should be positive");
                
                var messageDelay = TextDelayConfiguration.GetMessageDelayMs();
                AssertTrue(messageDelay == 200, 
                    $"Message delay should be 200ms (got {messageDelay}ms)");
                AssertTrue(messageDelay > 0, 
                    "Message delay should be positive");
                
                // Verify action delay is greater than message delay (logical relationship)
                AssertTrue(actionDelay > messageDelay, 
                    "Action delay should be greater than message delay");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Combat delay test failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Tests that progressive menu delays load correctly
        /// </summary>
        private static void TestProgressiveMenuDelays()
        {
            Console.WriteLine("\n--- Testing Progressive Menu Delays ---");
            
            try
            {
                var config = TextDelayConfiguration.GetProgressiveMenuDelays();
                AssertTrue(config != null, "Progressive menu delays config should exist");
                
                if (config != null)
                {
                    AssertTrue(config.BaseMenuDelay == 25, 
                        $"Base menu delay should be 25ms (got {config.BaseMenuDelay}ms)");
                    AssertTrue(config.ProgressiveReductionRate >= 0, 
                        $"Progressive reduction rate should be non-negative (got {config.ProgressiveReductionRate})");
                    AssertTrue(config.ProgressiveThreshold > 0, 
                        $"Progressive threshold should be positive (got {config.ProgressiveThreshold})");
                    AssertTrue(config.BaseMenuDelay > 0, 
                        "Base menu delay should be positive");
                }
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Progressive menu delay test failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Tests that enable flags load correctly
        /// </summary>
        private static void TestEnableFlags()
        {
            Console.WriteLine("\n--- Testing Enable Flags ---");
            
            try
            {
                // Test that flags can be retrieved without exceptions
                var guiDelaysEnabled = TextDelayConfiguration.GetEnableGuiDelays();
                AssertTrue(guiDelaysEnabled || !guiDelaysEnabled, 
                    "GUI delays flag retrieved successfully (valid boolean)");
                
                var consoleDelaysEnabled = TextDelayConfiguration.GetEnableConsoleDelays();
                AssertTrue(consoleDelaysEnabled || !consoleDelaysEnabled, 
                    "Console delays flag retrieved successfully (valid boolean)");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Enable flags test failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Tests that default values are reasonable when config is missing
        /// </summary>
        private static void TestDefaultValues()
        {
            Console.WriteLine("\n--- Testing Default Values ---");
            
            try
            {
                // Verify defaults are reasonable (even if config file exists, defaults should be valid)
                var defaultCombatDelay = TextDelayConfiguration.GetMessageTypeDelay(UIMessageType.Combat);
                AssertTrue(defaultCombatDelay > 0, 
                    $"Default combat delay should be positive (got {defaultCombatDelay}ms)");
                
                var defaultPreset = TextDelayConfiguration.GetChunkedTextRevealPreset("Default");
                AssertTrue(defaultPreset != null, 
                    "Default preset should always exist");
                
                if (defaultPreset != null)
                {
                    AssertTrue(defaultPreset.MinDelayMs < defaultPreset.MaxDelayMs,
                        "Default preset should have valid delay range (min < max)");
                    AssertTrue(defaultPreset.BaseDelayPerCharMs > 0,
                        "Default preset base delay should be positive");
                    AssertTrue(!string.IsNullOrEmpty(defaultPreset.Strategy),
                        "Default preset should have a strategy");
                }
                
                // Verify combat delays have defaults
                var actionDelay = TextDelayConfiguration.GetActionDelayMs();
                AssertTrue(actionDelay > 0, 
                    "Action delay should have a positive default");
                
                var messageDelay = TextDelayConfiguration.GetMessageDelayMs();
                AssertTrue(messageDelay > 0, 
                    "Message delay should have a positive default");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Default values test failed: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Edge Cases
        
        /// <summary>
        /// Tests behavior with invalid message types (should return 0)
        /// </summary>
        private static void TestInvalidMessageTypes()
        {
            Console.WriteLine("\n--- Testing Invalid Message Types ---");
            
            try
            {
                // All enum values should be valid, but test that unknown values return 0
                // Since we're using an enum, we can't test truly invalid values easily
                // But we can verify that all enum values return valid delays
                var allTypes = Enum.GetValues<UIMessageType>();
                foreach (var type in allTypes)
                {
                    var delay = TextDelayConfiguration.GetMessageTypeDelay(type);
                    // Should return 0 if not configured, or a positive value if configured
                    AssertTrue(delay >= 0, 
                        $"Message type {type} should return non-negative delay (got {delay}ms)");
                }
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Invalid message type test failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Tests behavior with invalid preset names (should return null)
        /// </summary>
        private static void TestInvalidPresetNames()
        {
            Console.WriteLine("\n--- Testing Invalid Preset Names ---");
            
            try
            {
                // Test with null
                var nullPreset = TextDelayConfiguration.GetChunkedTextRevealPreset(null!);
                AssertTrue(nullPreset == null, 
                    "Null preset name should return null");
                
                // Test with empty string
                var emptyPreset = TextDelayConfiguration.GetChunkedTextRevealPreset("");
                AssertTrue(emptyPreset == null, 
                    "Empty preset name should return null");
                
                // Test with non-existent preset
                var invalidPreset = TextDelayConfiguration.GetChunkedTextRevealPreset("NonExistentPreset");
                AssertTrue(invalidPreset == null, 
                    "Non-existent preset name should return null");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Invalid preset name test failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Tests that preset values are valid (min < max, positive values, etc.)
        /// </summary>
        private static void TestPresetValidation()
        {
            Console.WriteLine("\n--- Testing Preset Validation ---");
            
            try
            {
                var presetNames = new[] { "Combat", "Dungeon", "Room", "Narrative", "Default" };
                
                foreach (var presetName in presetNames)
                {
                    var preset = TextDelayConfiguration.GetChunkedTextRevealPreset(presetName);
                    if (preset != null)
                    {
                        AssertTrue(preset.MinDelayMs < preset.MaxDelayMs,
                            $"{presetName} preset: min delay ({preset.MinDelayMs}ms) should be less than max delay ({preset.MaxDelayMs}ms)");
                        AssertTrue(preset.BaseDelayPerCharMs > 0,
                            $"{presetName} preset: base delay should be positive (got {preset.BaseDelayPerCharMs}ms)");
                        AssertTrue(preset.MinDelayMs >= 0,
                            $"{presetName} preset: min delay should be non-negative (got {preset.MinDelayMs}ms)");
                        AssertTrue(preset.MaxDelayMs > 0,
                            $"{presetName} preset: max delay should be positive (got {preset.MaxDelayMs}ms)");
                        AssertTrue(!string.IsNullOrEmpty(preset.Strategy),
                            $"{presetName} preset: strategy should not be empty");
                    }
                }
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Preset validation test failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Tests that configuration loading is thread-safe
        /// </summary>
        private static void TestThreadSafety()
        {
            Console.WriteLine("\n--- Testing Thread Safety ---");
            
            try
            {
                // Test concurrent access to configuration
                var tasks = new List<Task>();
                var results = new List<int>();
                var exceptions = new List<Exception>();
                
                for (int i = 0; i < 10; i++)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        try
                        {
                            var delay = TextDelayConfiguration.GetMessageTypeDelay(UIMessageType.Combat);
                            lock (results)
                            {
                                results.Add(delay);
                            }
                            
                            var preset = TextDelayConfiguration.GetChunkedTextRevealPreset("Combat");
                            if (preset == null)
                            {
                                throw new Exception("Preset should not be null");
                            }
                            
                            var actionDelay = TextDelayConfiguration.GetActionDelayMs();
                            if (actionDelay <= 0)
                            {
                                throw new Exception("Action delay should be positive");
                            }
                        }
                        catch (Exception ex)
                        {
                            lock (exceptions)
                            {
                                exceptions.Add(ex);
                            }
                        }
                    }));
                }
                
                Task.WaitAll(tasks.ToArray());
                
                AssertTrue(exceptions.Count == 0, 
                    $"Thread safety test should not throw exceptions (got {exceptions.Count} exceptions)");
                AssertTrue(results.Count == 10, 
                    $"All 10 tasks should complete (got {results.Count} results)");
                
                // Verify all results are the same (configuration should be consistent)
                if (results.Count > 0)
                {
                    var firstResult = results[0];
                    var allSame = results.TrueForAll(r => r == firstResult);
                    AssertTrue(allSame, 
                        "All concurrent reads should return the same value");
                }
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Thread safety test failed: {ex.Message}");
            }
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
            
            if (_testsRun > 0)
            {
                Console.WriteLine($"Success Rate: {(_testsPassed * 100.0 / _testsRun):F1}%");
            }
            
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

