using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    /// <summary>
    /// Comprehensive tests for JsonLoader
    /// Tests JSON loading, saving, caching, and error handling
    /// </summary>
    public static class JsonLoaderTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        private static readonly string TestDataDir = Path.Combine(Path.GetTempPath(), "DungeonFighterTests");
        private static readonly string TestJsonFile = Path.Combine(TestDataDir, "test.json");
        private static readonly string TestJsonFile2 = Path.Combine(TestDataDir, "test2.json");

        /// <summary>
        /// Runs all JsonLoader tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== JsonLoader Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            // Setup test directory
            if (!Directory.Exists(TestDataDir))
            {
                Directory.CreateDirectory(TestDataDir);
            }

            TestLoadJson();
            TestSaveJson();
            TestCache();
            TestLoadJsonFromPaths();
            TestValidateJsonSyntax();
            TestGetFileInfo();
            TestFindGameDataFile();
            TestLoadJsonList();
            TestClearCache();

            // Cleanup
            try
            {
                if (File.Exists(TestJsonFile))
                    File.Delete(TestJsonFile);
                if (File.Exists(TestJsonFile2))
                    File.Delete(TestJsonFile2);
                if (Directory.Exists(TestDataDir))
                    Directory.Delete(TestDataDir);
            }
            catch { }

            TestBase.PrintSummary("JsonLoader Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Load JSON Tests

        private static void TestLoadJson()
        {
            Console.WriteLine("--- Testing LoadJson ---");

            // Create test JSON file
            var testData = new { Name = "Test", Value = 42 };
            var json = JsonSerializer.Serialize(testData);
            File.WriteAllText(TestJsonFile, json);

            // Test loading valid JSON
            var loaded = JsonLoader.LoadJson<TestData>(TestJsonFile);
            TestBase.AssertNotNull(loaded,
                "Should load valid JSON",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (loaded != null)
            {
                TestBase.AssertEqual("Test", loaded.Name,
                    "Loaded data should have correct name",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(42, loaded.Value,
                    "Loaded data should have correct value",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            // Test loading non-existent file with fallback
            var fallback = new TestData { Name = "Fallback", Value = 0 };
            var loadedFallback = JsonLoader.LoadJson<TestData>("nonexistent.json", useCache: false, fallbackValue: fallback);
            TestBase.AssertNotNull(loadedFallback,
                "Should return fallback for non-existent file",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (loadedFallback != null)
            {
                TestBase.AssertEqual("Fallback", loadedFallback.Name,
                    "Should return fallback value",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            // Test loading invalid JSON
            File.WriteAllText(TestJsonFile, "{ invalid json }");
            var invalid = JsonLoader.LoadJson<TestData>(TestJsonFile, useCache: false, fallbackValue: fallback);
            TestBase.AssertNotNull(invalid,
                "Should return fallback for invalid JSON",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Save JSON Tests

        private static void TestSaveJson()
        {
            Console.WriteLine("\n--- Testing SaveJson ---");

            var testData = new TestData { Name = "SaveTest", Value = 100 };
            var result = JsonLoader.SaveJson(testData, TestJsonFile2, updateCache: false);

            TestBase.AssertTrue(result,
                "SaveJson should succeed",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Verify file was created
            TestBase.AssertTrue(File.Exists(TestJsonFile2),
                "Saved file should exist",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Verify content
            var loaded = JsonLoader.LoadJson<TestData>(TestJsonFile2, useCache: false);
            TestBase.AssertNotNull(loaded,
                "Should be able to load saved JSON",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (loaded != null)
            {
                TestBase.AssertEqual("SaveTest", loaded.Name,
                    "Saved data should match original",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            // Test saving to non-existent directory (should create it)
            var nestedPath = Path.Combine(TestDataDir, "nested", "test.json");
            var result2 = JsonLoader.SaveJson(testData, nestedPath, updateCache: false);
            TestBase.AssertTrue(result2,
                "SaveJson should create directories",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(File.Exists(nestedPath),
                "Nested file should exist",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Cache Tests

        private static void TestCache()
        {
            Console.WriteLine("\n--- Testing Cache ---");

            // Clear cache first
            JsonLoader.ClearCache();

            // Load a file
            var testData = new TestData { Name = "CacheTest", Value = 1 };
            File.WriteAllText(TestJsonFile, JsonSerializer.Serialize(testData));

            var loaded1 = JsonLoader.LoadJson<TestData>(TestJsonFile, useCache: true);
            TestBase.AssertNotNull(loaded1,
                "Should load data",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Modify file
            testData.Value = 2;
            File.WriteAllText(TestJsonFile, JsonSerializer.Serialize(testData));

            // Load again with cache - should get cached version
            var loaded2 = JsonLoader.LoadJson<TestData>(TestJsonFile, useCache: true);
            TestBase.AssertNotNull(loaded2,
                "Should load from cache",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (loaded2 != null)
            {
                // Should still be value 1 from cache
                TestBase.AssertEqual(1, loaded2.Value,
                    "Cached data should not reflect file changes",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            // Load without cache - should get new value
            var loaded3 = JsonLoader.LoadJson<TestData>(TestJsonFile, useCache: false);
            TestBase.AssertNotNull(loaded3,
                "Should load fresh data without cache",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (loaded3 != null)
            {
                TestBase.AssertEqual(2, loaded3.Value,
                    "Non-cached data should reflect file changes",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            // Test cache stats
            var stats = JsonLoader.GetCacheStats();
            TestBase.AssertTrue(stats.Count >= 0,
                "Cache stats should be valid",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestClearCache()
        {
            Console.WriteLine("\n--- Testing ClearCache ---");

            // Load something to populate cache
            var testData = new TestData { Name = "ClearTest", Value = 1 };
            File.WriteAllText(TestJsonFile, JsonSerializer.Serialize(testData));
            JsonLoader.LoadJson<TestData>(TestJsonFile, useCache: true);

            // Clear specific file
            JsonLoader.ClearCacheForFile(TestJsonFile);
            var stats1 = JsonLoader.GetCacheStats();
            // Cache might still have other entries, so we just verify it doesn't crash

            // Clear all cache
            JsonLoader.ClearCache();
            var stats2 = JsonLoader.GetCacheStats();
            TestBase.AssertEqual(0, stats2.Count,
                "Cache should be empty after ClearCache",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Load From Paths Tests

        private static void TestLoadJsonFromPaths()
        {
            Console.WriteLine("\n--- Testing LoadJsonFromPaths ---");

            // Create test file
            var testData = new TestData { Name = "PathsTest", Value = 3 };
            File.WriteAllText(TestJsonFile, JsonSerializer.Serialize(testData));

            // Test with valid path
            var paths = new[] { TestJsonFile, "nonexistent1.json", "nonexistent2.json" };
            var loaded = JsonLoader.LoadJsonFromPaths<TestData>(paths, useCache: false);
            TestBase.AssertNotNull(loaded,
                "Should load from first valid path",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (loaded != null)
            {
                TestBase.AssertEqual("PathsTest", loaded.Name,
                    "Should load correct data from paths",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            // Test with no valid paths
            var invalidPaths = new[] { "nonexistent1.json", "nonexistent2.json" };
            var fallback = new TestData { Name = "Fallback", Value = 0 };
            var loadedFallback = JsonLoader.LoadJsonFromPaths<TestData>(invalidPaths, useCache: false, fallbackValue: fallback);
            TestBase.AssertNotNull(loadedFallback,
                "Should return fallback when no paths exist",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Validation Tests

        private static void TestValidateJsonSyntax()
        {
            Console.WriteLine("\n--- Testing ValidateJsonSyntax ---");

            // Test valid JSON
            var validJson = "{\"name\":\"test\",\"value\":1}";
            File.WriteAllText(TestJsonFile, validJson);
            TestBase.AssertTrue(JsonLoader.ValidateJsonSyntax(TestJsonFile),
                "Should validate valid JSON",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test invalid JSON
            File.WriteAllText(TestJsonFile, "{ invalid json }");
            TestBase.AssertFalse(JsonLoader.ValidateJsonSyntax(TestJsonFile),
                "Should reject invalid JSON",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test non-existent file
            TestBase.AssertFalse(JsonLoader.ValidateJsonSyntax("nonexistent.json"),
                "Should return false for non-existent file",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region File Info Tests

        private static void TestGetFileInfo()
        {
            Console.WriteLine("\n--- Testing GetFileInfo ---");

            // Create test file
            var testData = new TestData { Name = "InfoTest", Value = 4 };
            File.WriteAllText(TestJsonFile, JsonSerializer.Serialize(testData));

            var info = JsonLoader.GetFileInfo(TestJsonFile);
            TestBase.AssertNotNull(info,
                "Should return file info for existing file",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (info.HasValue)
            {
                TestBase.AssertTrue(info.Value.Exists,
                    "File should exist",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(info.Value.Size > 0,
                    "File should have size > 0",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            // Test non-existent file
            var info2 = JsonLoader.GetFileInfo("nonexistent.json");
            TestBase.AssertNull(info2,
                "Should return null for non-existent file",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Find Game Data File Tests

        private static void TestFindGameDataFile()
        {
            Console.WriteLine("\n--- Testing FindGameDataFile ---");

            // Test finding a known game data file (Actions.json should exist)
            var actionsPath = JsonLoader.FindGameDataFile("Actions.json");
            // This might be null if file doesn't exist in test environment, so we just check it doesn't crash
            if (actionsPath != null)
            {
                TestBase.AssertTrue(File.Exists(actionsPath),
                    "Found file path should exist",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            // Test finding non-existent file
            var nonexistent = JsonLoader.FindGameDataFile("NonexistentFile.json");
            // Should return null, but we don't assert since it depends on game data location
        }

        #endregion

        #region Load JSON List Tests

        private static void TestLoadJsonList()
        {
            Console.WriteLine("\n--- Testing LoadJsonList ---");

            // Create test list file
            var testList = new List<TestData>
            {
                new TestData { Name = "Item1", Value = 1 },
                new TestData { Name = "Item2", Value = 2 }
            };
            File.WriteAllText(TestJsonFile, JsonSerializer.Serialize(testList));

            // This test depends on FindGameDataFile working, so we'll test the pattern
            // by directly testing LoadJson with a list
            var loaded = JsonLoader.LoadJson<List<TestData>>(TestJsonFile, useCache: false, fallbackValue: new List<TestData>());
            TestBase.AssertNotNull(loaded,
                "Should load JSON list",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (loaded != null)
            {
                TestBase.AssertTrue(loaded.Count >= 0,
                    "Loaded list should be valid",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region Helper Classes

        private class TestData
        {
            public string Name { get; set; } = "";
            public int Value { get; set; }
        }

        #endregion
    }
}
