using System;
using System.Collections.Generic;
using RPGGame.Tests;
using RPGGame.World.DungeonMap;

namespace RPGGame.Tests.Unit.World
{
    public static class DungeonMapGeneratorTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Dungeon Map Generator Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestTrainingGroundMap();
            TestSingleRoomMapHasStartContentExit();
            TestMultiRoomAttachesAllRoomsAndIsConnected();
            TestDungeonGenerateAttachesTraversalState();

            TestBase.PrintSummary("Dungeon Map Generator Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestTrainingGroundMap()
        {
            var room = new Environment("TG", "desc", true, "Generic");
            var state = DungeonMapGenerator.GenerateTrainingGround(room);

            TestBase.AssertEqual(DungeonMapGenerator.TrainingGroundSize, state.Width,
                "Training ground width", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1, state.ContentPositions.Count,
                "Training ground one content cell", ref _testsRun, ref _testsPassed, ref _testsFailed);

            var (cx, cy) = state.ContentPositions[0];
            TestBase.AssertEqualEnum(DungeonMapCellKind.Content, state.GetCell(cx, cy).Kind,
                "Center should be content", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(ReferenceEquals(room, state.GetCell(cx, cy).Room),
                "Content cell holds room", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestSingleRoomMapHasStartContentExit()
        {
            var room = new Environment("Alone", "desc", true, "Forest");
            var state = DungeonMapGenerator.Generate(new[] { room }, new Random(42));

            TestBase.AssertEqual(1, state.ContentPositions.Count, "One content", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(state.GetCell(state.StartX, state.StartY).IsWalkable,
                "Start walkable", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(state.GetCell(state.ExitX, state.ExitY).IsWalkable,
                "Exit walkable", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(CanReach(state, state.StartX, state.StartY, state.ContentPositions[0].X, state.ContentPositions[0].Y),
                "Start reaches content", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestMultiRoomAttachesAllRoomsAndIsConnected()
        {
            var rooms = new List<Environment>
            {
                new Environment("R1", "d", true, "Crypt"),
                new Environment("R2", "d", false, "Crypt"),
                new Environment("R3", "d", true, "Crypt"),
                new Environment("R4", "d", true, "Crypt")
            };
            var state = DungeonMapGenerator.Generate(rooms, new Random(7));

            TestBase.AssertEqual(rooms.Count, state.ContentPositions.Count,
                "Content count matches rooms", ref _testsRun, ref _testsPassed, ref _testsFailed);

            for (int i = 0; i < rooms.Count; i++)
            {
                var (x, y) = state.ContentPositions[i];
                TestBase.AssertEqual(i, state.GetCell(x, y).ContentRoomIndex,
                    $"Content index {i}", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(ReferenceEquals(rooms[i], state.GetCell(x, y).Room),
                    $"Room {i} attached", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(CanReach(state, state.StartX, state.StartY, x, y),
                    $"Start reaches room {i}", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            TestBase.AssertTrue(CanReach(state, state.StartX, state.StartY, state.ExitX, state.ExitY),
                "Start reaches exit", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(state.Width <= DungeonMapGenerator.MaxSize,
                "Width capped", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestDungeonGenerateAttachesTraversalState()
        {
            var dungeon = new Dungeon("MapAttach", 1, 3, "Forest", generationSeed: 99);
            dungeon.Generate();

            TestBase.AssertTrue(dungeon.Rooms.Count > 0, "Rooms generated", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertNotNull(dungeon.TraversalState, "TraversalState attached", ref _testsRun, ref _testsPassed, ref _testsFailed);
            if (dungeon.TraversalState != null)
            {
                TestBase.AssertEqual(dungeon.Rooms.Count, dungeon.TraversalState.ContentPositions.Count,
                    "Traversal content matches room count", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            var tg = PreWeaponTrainingFlow.CreateTrainingGroundDungeon();
            tg.Generate();
            TestBase.AssertNotNull(tg.TraversalState, "TG TraversalState", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(DungeonMapGenerator.TrainingGroundSize, tg.TraversalState!.Width,
                "TG map size", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static bool CanReach(DungeonTraversalState state, int sx, int sy, int tx, int ty)
        {
            var dist = SmartPathStepper.BfsDistances(state, tx, ty);
            return dist.ContainsKey(SmartPathStepper.Pack(sx, sy, state.Width));
        }
    }
}
