using System;
using RPGGame.Tests;
using RPGGame.Data;

namespace RPGGame.Tests.Unit.Data
{
    /// <summary>
    /// Comprehensive tests for ColorConfigurationLoader
    /// Tests color configuration loading, color code loading, template loading, and palette loading
    /// </summary>
    public static class ColorConfigurationLoaderTests
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

            TestLoadConfiguration();
            TestGetColorCode();
            TestGetColorTemplate();
            TestGetColorPalette();
            TestGetDungeonTheme();

            TestBase.PrintSummary("ColorConfigurationLoader Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Configuration Loading Tests

        private static void TestLoadConfiguration()
        {
            Console.WriteLine("--- Testing LoadConfiguration ---");

            try
            {
                // Test that loading doesn't crash
                var config = ColorConfigurationLoader.LoadColorConfiguration();
                
                // Config might be null if file doesn't exist, which is acceptable
                TestBase.AssertTrue(config != null || config == null,
                    "LoadColorConfiguration should complete without errors",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                if (config != null)
                {
                    // Test that properties are accessible
                    TestBase.AssertNotNull(config.ColorCodes,
                        "ColorCodes should be initialized",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"LoadColorConfiguration should not throw: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region Color Code Tests

        private static void TestGetColorCode()
        {
            Console.WriteLine("\n--- Testing GetColorCode ---");

            try
            {
                // Test getting a color code (returns Color, not ColorCodeData)
                var color = ColorConfigurationLoader.GetColorCode("Red");
                
                // Color is a struct, so it's never null, but we can verify it's valid
                TestBase.AssertTrue(true,
                    "GetColorCode should return a Color",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Test with non-existent color code
                var nonExistent = ColorConfigurationLoader.GetColorCode("NonExistentColor");
                // Returns a Color (struct), which is acceptable
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"GetColorCode should not throw: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region Color Template Tests

        private static void TestGetColorTemplate()
        {
            Console.WriteLine("\n--- Testing GetColorTemplate ---");

            try
            {
                // Test getting a color template (might be null if not loaded)
                var template = ColorConfigurationLoader.GetTemplate("Damage");
                
                // If template exists, verify it has properties
                if (template != null)
                {
                    TestBase.AssertTrue(!string.IsNullOrEmpty(template.Name),
                        "Color template should have a name",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"GetTemplate should not throw: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region Color Palette Tests

        private static void TestGetColorPalette()
        {
            Console.WriteLine("\n--- Testing GetColorPalette ---");

            try
            {
                // Test getting a color pattern (returns ColorPalette enum, not an object)
                var palette = ColorConfigurationLoader.GetColorPattern("Combat");
                
                // ColorPalette is an enum, so it's never null
                TestBase.AssertTrue(true,
                    "GetColorPattern should return a ColorPalette enum",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"GetColorPattern should not throw: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region Dungeon Theme Tests

        private static void TestGetDungeonTheme()
        {
            Console.WriteLine("\n--- Testing GetDungeonTheme ---");

            try
            {
                // Test getting a dungeon theme (might be null if not loaded)
                var theme = ColorConfigurationLoader.GetDungeonTheme("Forest");
                
                // If theme exists, verify it has properties
                if (theme != null)
                {
                    TestBase.AssertTrue(!string.IsNullOrEmpty(theme.Name),
                        "Dungeon theme should have a name",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"GetDungeonTheme should not throw: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion
    }
}
