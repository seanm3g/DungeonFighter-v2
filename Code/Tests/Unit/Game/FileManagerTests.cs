using System;
using System.IO;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Game
{
    /// <summary>
    /// Comprehensive tests for FileManager
    /// Tests file operations (save/load)
    /// </summary>
    public static class FileManagerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all FileManager tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== FileManager Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestGetGameDataFilePath();
            TestSafeWriteJsonFile();
            TestCreateBackup();

            TestBase.PrintSummary("FileManager Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region File Path Tests

        private static void TestGetGameDataFilePath()
        {
            Console.WriteLine("--- Testing GetGameDataFilePath ---");

            string filePath = FileManager.GetGameDataFilePath("test.json");
            
            TestBase.AssertNotNull(filePath,
                "GetGameDataFilePath should return a file path",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            
            TestBase.AssertTrue(!string.IsNullOrEmpty(filePath),
                "File path should not be empty",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region File Operations Tests

        private static void TestSafeWriteJsonFile()
        {
            Console.WriteLine("\n--- Testing SafeWriteJsonFile ---");

            // Create a temporary file path
            string tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.json");
            
            try
            {
                var testData = new { Name = "Test", Value = 123 };
                
                // Test that SafeWriteJsonFile doesn't crash
                FileManager.SafeWriteJsonFile(tempFile, testData, createBackup: false);
                
                TestBase.AssertTrue(File.Exists(tempFile),
                    "SafeWriteJsonFile should create the file",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                // Clean up
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        private static void TestCreateBackup()
        {
            Console.WriteLine("\n--- Testing CreateBackup ---");

            // Create a temporary file path
            string tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.json");
            string backupFile = tempFile + ".backup";
            
            try
            {
                // Create initial file
                File.WriteAllText(tempFile, "test content");
                
                var testData = new { Name = "Test", Value = 123 };
                
                // Test that backup is created
                FileManager.SafeWriteJsonFile(tempFile, testData, createBackup: true);
                
                TestBase.AssertTrue(File.Exists(backupFile),
                    "CreateBackup should create backup file",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                // Clean up
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
                if (File.Exists(backupFile))
                {
                    File.Delete(backupFile);
                }
            }
        }

        #endregion
    }
}
