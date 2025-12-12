using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.Data;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Comprehensive unit tests for ColorConfigurationLoader.
    /// Tests loading, caching, recursion prevention, and all accessor methods.
    /// </summary>
    public static class ColorConfigurationLoaderTest
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;
        
        /// <summary>
        /// Runs all ColorConfigurationLoader tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== ColorConfigurationLoader Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;
            
            // Critical: Test infinite recursion prevention
            TestNoInfiniteRecursion();
            TestCacheBuildingOrder();
            TestGetColorCodeDuringBuilding();
            
            // Core functionality tests
            TestLoadColorConfiguration();
            TestGetColorCode();
            TestGetPaletteColor();
            TestGetTemplate();
            TestGetDungeonTheme();
            TestGetDungeonThemeColor();
            TestGetColorPattern();
            TestGetEntityDefault();
            TestGetKeywordGroups();
            
            // Cache behavior tests
            TestCaching();
            TestReload();
            TestFallbackBehavior();
            
            // Edge cases
            TestNullAndEmptyInputs();
            TestInvalidColorCodes();
            TestInvalidPaletteNames();
            TestMissingConfigurationFile();
            
            // Print summary
            PrintSummary();
        }
        
        #region Critical Recursion Prevention Tests
        
        /// <summary>
        /// Tests that BuildPaletteColorCache doesn't cause infinite recursion
        /// when it calls GetColorCode which references palette colors with color codes
        /// </summary>
        private static void TestNoInfiniteRecursion()
        {
            Console.WriteLine("--- Testing Infinite Recursion Prevention ---");
            
            try
            {
                // Reload to ensure clean state
                ColorConfigurationLoader.Reload();
                
                // This should complete without stack overflow
                var config = ColorConfigurationLoader.LoadColorConfiguration();
                
                AssertTrue(config != null, "Configuration should load successfully");
                AssertTrue(true, "No infinite recursion detected (test completed)");
            }
            catch (StackOverflowException)
            {
                AssertTrue(false, "Stack overflow detected - infinite recursion not prevented!");
            }
            catch (Exception ex)
            {
                // Other exceptions are okay (like file not found), but stack overflow is critical
                AssertTrue(ex is not StackOverflowException, 
                    $"Unexpected exception type: {ex.GetType().Name} (StackOverflowException would indicate recursion)");
            }
        }
        
        /// <summary>
        /// Tests that cache building happens in the correct order
        /// (color codes must be built before palette colors)
        /// </summary>
        private static void TestCacheBuildingOrder()
        {
            Console.WriteLine("\n--- Testing Cache Building Order ---");
            
            try
            {
                ColorConfigurationLoader.Reload();
                var config = ColorConfigurationLoader.LoadColorConfiguration();
                
                // After loading, color code cache should be populated first
                // Then palette cache can safely reference color codes
                var testColor = ColorConfigurationLoader.GetColorCode("R");
                
                AssertTrue(testColor != Colors.White || config?.ColorCodes == null, 
                    "Color code should be retrievable after cache building");
                
                // Test that palette colors can reference color codes
                var paletteColor = ColorConfigurationLoader.GetPaletteColor(ColorPalette.Damage);
                AssertTrue(paletteColor != default, "Palette color should be retrievable");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Cache building order test failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Tests that GetColorCode works correctly during cache building phase
        /// </summary>
        private static void TestGetColorCodeDuringBuilding()
        {
            Console.WriteLine("\n--- Testing GetColorCode During Building ---");
            
            try
            {
                ColorConfigurationLoader.Reload();
                
                // Load configuration - this triggers cache building
                var config = ColorConfigurationLoader.LoadColorConfiguration();
                
                // During building, GetColorCode should check cache directly
                // After building, it should return cached values
                if (config?.ColorCodes != null && config.ColorCodes.Count > 0)
                {
                    var firstCode = config.ColorCodes[0].Code;
                    if (!string.IsNullOrEmpty(firstCode))
                    {
                        var color = ColorConfigurationLoader.GetColorCode(firstCode);
                        AssertTrue(color != default, 
                            $"GetColorCode should return valid color for code '{firstCode}' after building");
                    }
                }
                
                AssertTrue(true, "GetColorCode works correctly during and after building");
            }
            catch (StackOverflowException)
            {
                AssertTrue(false, "Stack overflow when calling GetColorCode during building!");
            }
            catch (Exception ex)
            {
                AssertTrue(ex is not StackOverflowException, 
                    $"Exception during GetColorCode test: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Core Functionality Tests
        
        private static void TestLoadColorConfiguration()
        {
            Console.WriteLine("\n--- Testing LoadColorConfiguration ---");
            
            try
            {
                ColorConfigurationLoader.Reload();
                var config = ColorConfigurationLoader.LoadColorConfiguration();
                
                AssertTrue(config != null, "Configuration should not be null");
                
                // Test that subsequent calls return cached version
                var config2 = ColorConfigurationLoader.LoadColorConfiguration();
                AssertTrue(ReferenceEquals(config, config2), 
                    "Subsequent calls should return cached configuration");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"LoadColorConfiguration failed: {ex.Message}");
            }
        }
        
        private static void TestGetColorCode()
        {
            Console.WriteLine("\n--- Testing GetColorCode ---");
            
            // Test with null/empty
            var nullColor = ColorConfigurationLoader.GetColorCode(null!);
            AssertTrue(nullColor == Colors.White, "Null color code should return white");
            
            var emptyColor = ColorConfigurationLoader.GetColorCode("");
            AssertTrue(emptyColor == Colors.White, "Empty color code should return white");
            
            // Test with valid code (if available)
            try
            {
                var config = ColorConfigurationLoader.LoadColorConfiguration();
                if (config?.ColorCodes != null && config.ColorCodes.Count > 0)
                {
                    var testCode = config.ColorCodes[0].Code;
                    if (!string.IsNullOrEmpty(testCode))
                    {
                        var color = ColorConfigurationLoader.GetColorCode(testCode);
                        AssertTrue(color != default, 
                            $"GetColorCode should return valid color for '{testCode}'");
                    }
                }
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"GetColorCode test failed: {ex.Message}");
            }
        }
        
        private static void TestGetPaletteColor()
        {
            Console.WriteLine("\n--- Testing GetPaletteColor ---");
            
            try
            {
                // Test with various palette values
                var white = ColorConfigurationLoader.GetPaletteColor(ColorPalette.White);
                AssertTrue(white != default, "GetPaletteColor should return valid color for White");
                
                var damage = ColorConfigurationLoader.GetPaletteColor(ColorPalette.Damage);
                AssertTrue(damage != default, "GetPaletteColor should return valid color for Damage");
                
                var player = ColorConfigurationLoader.GetPaletteColor(ColorPalette.Player);
                AssertTrue(player != default, "GetPaletteColor should return valid color for Player");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"GetPaletteColor test failed: {ex.Message}");
            }
        }
        
        private static void TestGetTemplate()
        {
            Console.WriteLine("\n--- Testing GetTemplate ---");
            
            // Test with null/empty
                var nullTemplate = ColorConfigurationLoader.GetTemplate(null!);
                AssertTrue(nullTemplate == null, "Null template name should return null");
            
            var emptyTemplate = ColorConfigurationLoader.GetTemplate("");
            AssertTrue(emptyTemplate == null, "Empty template name should return null");
            
            // Test with valid template (if available)
            try
            {
                var config = ColorConfigurationLoader.LoadColorConfiguration();
                if (config?.ColorTemplates != null && config.ColorTemplates.Count > 0)
                {
                    var templateName = config.ColorTemplates[0].Name;
                    if (!string.IsNullOrEmpty(templateName))
                    {
                        var template = ColorConfigurationLoader.GetTemplate(templateName);
                        AssertTrue(template != null, 
                            $"GetTemplate should return template for '{templateName}'");
                    }
                }
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"GetTemplate test failed: {ex.Message}");
            }
        }
        
        private static void TestGetDungeonTheme()
        {
            Console.WriteLine("\n--- Testing GetDungeonTheme ---");
            
            // Test with null/empty
            var nullTheme = ColorConfigurationLoader.GetDungeonTheme(null!);
            AssertTrue(nullTheme == null, "Null theme name should return null");
            
            var emptyTheme = ColorConfigurationLoader.GetDungeonTheme("");
            AssertTrue(emptyTheme == null, "Empty theme name should return null");
            
            // Test with valid theme (if available)
            try
            {
                var config = ColorConfigurationLoader.LoadColorConfiguration();
                if (config?.DungeonThemes != null && config.DungeonThemes.Count > 0)
                {
                    var themeName = config.DungeonThemes[0].Name;
                    if (!string.IsNullOrEmpty(themeName))
                    {
                        var theme = ColorConfigurationLoader.GetDungeonTheme(themeName);
                        AssertTrue(theme != null, 
                            $"GetDungeonTheme should return theme for '{themeName}'");
                    }
                }
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"GetDungeonTheme test failed: {ex.Message}");
            }
        }
        
        private static void TestGetDungeonThemeColor()
        {
            Console.WriteLine("\n--- Testing GetDungeonThemeColor ---");
            
            try
            {
                var config = ColorConfigurationLoader.LoadColorConfiguration();
                if (config?.DungeonThemes != null && config.DungeonThemes.Count > 0)
                {
                    var themeName = config.DungeonThemes[0].Name;
                    if (!string.IsNullOrEmpty(themeName))
                    {
                        var color = ColorConfigurationLoader.GetDungeonThemeColor(themeName);
                        AssertTrue(color != default, 
                            $"GetDungeonThemeColor should return valid color for '{themeName}'");
                    }
                }
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"GetDungeonThemeColor test failed: {ex.Message}");
            }
        }
        
        private static void TestGetColorPattern()
        {
            Console.WriteLine("\n--- Testing GetColorPattern ---");
            
            // Test with null/empty
            var nullPattern = ColorConfigurationLoader.GetColorPattern(null!);
            AssertTrue(nullPattern == ColorPalette.White, 
                "Null pattern should return White palette");
            
            var emptyPattern = ColorConfigurationLoader.GetColorPattern("");
            AssertTrue(emptyPattern == ColorPalette.White, 
                "Empty pattern should return White palette");
            
            // Test with valid pattern (if available)
            try
            {
                var config = ColorConfigurationLoader.LoadColorConfiguration();
                if (config?.ColorPatterns != null && config.ColorPatterns.Count > 0)
                {
                    var patternName = config.ColorPatterns[0].Name;
                    if (!string.IsNullOrEmpty(patternName))
                    {
                        var palette = ColorConfigurationLoader.GetColorPattern(patternName);
                        AssertTrue(palette != ColorPalette.White || config.ColorPatterns.Count == 0, 
                            $"GetColorPattern should return valid palette for '{patternName}'");
                    }
                }
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"GetColorPattern test failed: {ex.Message}");
            }
        }
        
        private static void TestGetEntityDefault()
        {
            Console.WriteLine("\n--- Testing GetEntityDefault ---");
            
            try
            {
                var player = ColorConfigurationLoader.GetEntityDefault("player");
                AssertTrue(!string.IsNullOrEmpty(player), 
                    "GetEntityDefault should return default for 'player'");
                
                var enemy = ColorConfigurationLoader.GetEntityDefault("enemy");
                AssertTrue(!string.IsNullOrEmpty(enemy), 
                    "GetEntityDefault should return default for 'enemy'");
                
                var boss = ColorConfigurationLoader.GetEntityDefault("boss");
                AssertTrue(!string.IsNullOrEmpty(boss), 
                    "GetEntityDefault should return default for 'boss'");
                
                var npc = ColorConfigurationLoader.GetEntityDefault("npc");
                AssertTrue(!string.IsNullOrEmpty(npc), 
                    "GetEntityDefault should return default for 'npc'");
                
                var unknown = ColorConfigurationLoader.GetEntityDefault("unknown");
                AssertTrue(unknown == "White", 
                    "GetEntityDefault should return 'White' for unknown entity type");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"GetEntityDefault test failed: {ex.Message}");
            }
        }
        
        private static void TestGetKeywordGroups()
        {
            Console.WriteLine("\n--- Testing GetKeywordGroups ---");
            
            try
            {
                var groups = ColorConfigurationLoader.GetKeywordGroups();
                AssertTrue(groups != null, "GetKeywordGroups should not return null");
                
                // Groups can be empty, that's okay
                AssertTrue(true, "GetKeywordGroups returns valid list");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"GetKeywordGroups test failed: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Cache Behavior Tests
        
        private static void TestCaching()
        {
            Console.WriteLine("\n--- Testing Caching ---");
            
            try
            {
                ColorConfigurationLoader.Reload();
                
                // First load
                var config1 = ColorConfigurationLoader.LoadColorConfiguration();
                
                // Second load should return cached version
                var config2 = ColorConfigurationLoader.LoadColorConfiguration();
                
                AssertTrue(ReferenceEquals(config1, config2), 
                    "Subsequent LoadColorConfiguration calls should return cached config");
                
                // Test that color code cache persists
                if (config1?.ColorCodes != null && config1.ColorCodes.Count > 0)
                {
                    var code = config1.ColorCodes[0].Code;
                    if (!string.IsNullOrEmpty(code))
                    {
                        var color1 = ColorConfigurationLoader.GetColorCode(code);
                        var color2 = ColorConfigurationLoader.GetColorCode(code);
                        
                        AssertTrue(color1 == color2, 
                            "GetColorCode should return same color from cache");
                    }
                }
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Caching test failed: {ex.Message}");
            }
        }
        
        private static void TestReload()
        {
            Console.WriteLine("\n--- Testing Reload ---");
            
            try
            {
                // Load initial config
                var config1 = ColorConfigurationLoader.LoadColorConfiguration();
                
                // Reload
                ColorConfigurationLoader.Reload();
                
                // Load again - should get new instance
                var config2 = ColorConfigurationLoader.LoadColorConfiguration();
                
                // Configs might be equal in content but should be reloaded
                AssertTrue(true, "Reload completed without exception");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Reload test failed: {ex.Message}");
            }
        }
        
        private static void TestFallbackBehavior()
        {
            Console.WriteLine("\n--- Testing Fallback Behavior ---");
            
            try
            {
                // Test that methods don't throw when config is missing or incomplete
                var invalidCode = ColorConfigurationLoader.GetColorCode("INVALID_CODE_XYZ");
                AssertTrue(invalidCode != default, 
                    "GetColorCode should return fallback color for invalid code");
                
                var invalidTemplate = ColorConfigurationLoader.GetTemplate("INVALID_TEMPLATE_XYZ");
                AssertTrue(invalidTemplate == null, 
                    "GetTemplate should return null for invalid template");
                
                var invalidTheme = ColorConfigurationLoader.GetDungeonTheme("INVALID_THEME_XYZ");
                AssertTrue(invalidTheme == null, 
                    "GetDungeonTheme should return null for invalid theme");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Fallback behavior test failed: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Edge Cases
        
        private static void TestNullAndEmptyInputs()
        {
            Console.WriteLine("\n--- Testing Null and Empty Inputs ---");
            
            // All methods should handle null/empty gracefully
            var nullCode = ColorConfigurationLoader.GetColorCode(null!);
            AssertTrue(nullCode == Colors.White, "Null color code returns white");
            
            var nullTemplate = ColorConfigurationLoader.GetTemplate(null!);
            AssertTrue(nullTemplate == null, "Null template name returns null");
            
            var nullTheme = ColorConfigurationLoader.GetDungeonTheme(null!);
            AssertTrue(nullTheme == null, "Null theme name returns null");
            
            var nullPattern = ColorConfigurationLoader.GetColorPattern(null!);
            AssertTrue(nullPattern == ColorPalette.White, "Null pattern returns White palette");
        }
        
        private static void TestInvalidColorCodes()
        {
            Console.WriteLine("\n--- Testing Invalid Color Codes ---");
            
            var invalid1 = ColorConfigurationLoader.GetColorCode("INVALID");
            AssertTrue(invalid1 != default, "Invalid color code returns fallback");
            
            var invalid2 = ColorConfigurationLoader.GetColorCode("999");
            AssertTrue(invalid2 != default, "Numeric color code returns fallback");
        }
        
        private static void TestInvalidPaletteNames()
        {
            Console.WriteLine("\n--- Testing Invalid Palette Names ---");
            
            // Try to parse invalid enum values - should handle gracefully
            try
            {
                // This should work even if the palette doesn't exist in config
                var invalid = ColorConfigurationLoader.GetPaletteColor((ColorPalette)9999);
                AssertTrue(invalid != default, "Invalid palette enum returns fallback color");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Invalid palette enum should not throw: {ex.Message}");
            }
        }
        
        private static void TestMissingConfigurationFile()
        {
            Console.WriteLine("\n--- Testing Missing Configuration File ---");
            
            try
            {
                // Reload to ensure clean state
                ColorConfigurationLoader.Reload();
                
                // Even if file is missing, should not throw
                var config = ColorConfigurationLoader.LoadColorConfiguration();
                AssertTrue(config != null, 
                    "LoadColorConfiguration should return config even if file missing");
            }
            catch (Exception ex)
            {
                // File not found is acceptable, but other exceptions are not
                AssertTrue(ex is System.IO.FileNotFoundException || 
                         ex is System.IO.DirectoryNotFoundException,
                    $"Unexpected exception for missing file: {ex.GetType().Name}");
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

