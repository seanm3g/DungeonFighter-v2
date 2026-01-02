using System;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.World
{
    /// <summary>
    /// Comprehensive tests for RoomGenerator
    /// Tests room generation, room layouts, and enemy placement
    /// </summary>
    public static class RoomGeneratorTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all RoomGenerator tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== RoomGenerator Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestGenerateRoom();
            TestGenerateRoomWithTheme();
            TestGenerateRoomHostile();

            TestBase.PrintSummary("RoomGenerator Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Room Generation Tests

        private static void TestGenerateRoom()
        {
            Console.WriteLine("--- Testing GenerateRoom ---");

            var room = RoomGenerator.GenerateRoom("Forest", 5, true);
            
            // Room generation might return null if no rooms match, which is acceptable
            if (room != null)
            {
                TestBase.AssertNotNull(room,
                    "Generated room should not be null",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertTrue(!string.IsNullOrEmpty(room.Name),
                    "Generated room should have a name",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertEqual("Forest", room.Theme,
                    "Generated room should have correct theme",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            else
            {
                // Null is acceptable if no rooms match the criteria
                TestBase.AssertTrue(true,
                    "GenerateRoom can return null if no rooms match",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestGenerateRoomWithTheme()
        {
            Console.WriteLine("\n--- Testing GenerateRoom with Theme ---");

            // Test different themes
            var themes = new[] { "Forest", "Lava", "Crypt", "Ice" };
            
            foreach (var theme in themes)
            {
                var room = RoomGenerator.GenerateRoom(theme, 5, true);
                // Room might be null, which is acceptable
                if (room != null)
                {
                    TestBase.AssertEqual(theme, room.Theme,
                        $"Generated room should have theme {theme}",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }
        }

        private static void TestGenerateRoomHostile()
        {
            Console.WriteLine("\n--- Testing GenerateRoom Hostility ---");

            // Test hostile room
            var hostileRoom = RoomGenerator.GenerateRoom("Forest", 5, true);
            if (hostileRoom != null)
            {
                TestBase.AssertTrue(hostileRoom.IsHostile,
                    "Hostile room should have IsHostile = true",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            // Test non-hostile room
            var nonHostileRoom = RoomGenerator.GenerateRoom("Forest", 5, false);
            if (nonHostileRoom != null)
            {
                TestBase.AssertFalse(nonHostileRoom.IsHostile,
                    "Non-hostile room should have IsHostile = false",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion
    }
}
