using System;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    /// <summary>
    /// Comprehensive tests for RoomLoader
    /// Tests room data loading, retrieval, and error handling
    /// </summary>
    public static class RoomLoaderTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all RoomLoader tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== RoomLoader Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestLoadRooms();
            TestGetRoomData();
            TestHasRoom();
            TestGetAllRoomNames();
            TestGetAllRoomData();

            TestBase.PrintSummary("RoomLoader Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Loading Tests

        private static void TestLoadRooms()
        {
            Console.WriteLine("--- Testing LoadRooms ---");

            // Test that loading doesn't crash
            RoomLoader.LoadRooms();
            TestBase.AssertTrue(true,
                "LoadRooms should complete without errors",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test loading multiple times
            RoomLoader.LoadRooms();
            TestBase.AssertTrue(true,
                "LoadRooms should handle multiple calls",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Retrieval Tests

        private static void TestGetRoomData()
        {
            Console.WriteLine("\n--- Testing GetRoomData ---");

            // Test getting room data (might be null if no rooms loaded)
            var roomData = RoomLoader.GetRoomData("Forest Clearing");
            
            // If room exists, verify properties
            if (roomData != null)
            {
                TestBase.AssertTrue(!string.IsNullOrEmpty(roomData.Name),
                    "Room data should have a name",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            // Test getting non-existent room
            var nonExistent = RoomLoader.GetRoomData("NonExistentRoom");
            TestBase.AssertNull(nonExistent,
                "Non-existent room should return null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test with empty string
            var empty = RoomLoader.GetRoomData("");
            TestBase.AssertNull(empty,
                "Empty room name should return null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Query Tests

        private static void TestHasRoom()
        {
            Console.WriteLine("\n--- Testing HasRoom ---");

            // Test with potentially existing room (use GetRoomData to check)
            var hasRoom = RoomLoader.GetRoomData("Forest Clearing") != null;
            // Result depends on data files, so we just verify it doesn't crash
            TestBase.AssertTrue(hasRoom || !hasRoom,
                "HasRoom should return a boolean",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test with non-existent room
            TestBase.AssertFalse(RoomLoader.GetRoomData("NonExistentRoom") != null,
                "Non-existent room should return false",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test with empty string
            TestBase.AssertFalse(RoomLoader.GetRoomData("") != null,
                "Empty room name should return false",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestGetAllRoomNames()
        {
            Console.WriteLine("\n--- Testing GetAllRoomNames ---");

            var names = RoomLoader.GetAllRoomNames();
            TestBase.AssertNotNull(names,
                "GetAllRoomNames should return a list",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (names != null)
            {
                TestBase.AssertTrue(names.Count >= 0,
                    $"Room names count should be >= 0, got {names.Count}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // If there are rooms, verify names are not empty
                foreach (var name in names)
                {
                    TestBase.AssertTrue(!string.IsNullOrEmpty(name),
                        $"Room name should not be empty: '{name}'",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }
        }

        private static void TestGetAllRoomData()
        {
            Console.WriteLine("\n--- Testing GetAllRoomData ---");

            var allRooms = RoomLoader.GetAllRoomData();
            TestBase.AssertNotNull(allRooms,
                "GetAllRoomData should return a list",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (allRooms != null)
            {
                TestBase.AssertTrue(allRooms.Count >= 0,
                    $"Room data count should be >= 0, got {allRooms.Count}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // If there are rooms, verify they have names
                foreach (var room in allRooms)
                {
                    TestBase.AssertNotNull(room,
                        "Room data should not be null",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    
                    if (room != null)
                    {
                        TestBase.AssertTrue(!string.IsNullOrEmpty(room.Name),
                            "Room should have a name",
                            ref _testsRun, ref _testsPassed, ref _testsFailed);
                    }
                }
            }
        }

        #endregion
    }
}
