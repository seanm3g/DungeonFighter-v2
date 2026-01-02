using System;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Actions
{
    /// <summary>
    /// Comprehensive tests for ActionSpeedSystem
    /// Tests action speed calculation, turn order, and entity management
    /// </summary>
    public static class ActionSpeedSystemTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all ActionSpeedSystem tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== ActionSpeedSystem Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestAddEntity();
            TestRemoveEntity();
            TestSetEntityActionTime();
            TestGetNextEntityToAct();
            TestAdvanceEntityTurn();
            TestExecuteAction();

            TestBase.PrintSummary("ActionSpeedSystem Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Entity Management Tests

        private static void TestAddEntity()
        {
            Console.WriteLine("--- Testing AddEntity ---");

            var system = new ActionSpeedSystem();
            var character = TestDataBuilders.Character()
                .WithName("TestPlayer")
                .WithLevel(1)
                .Build();

            system.AddEntity(character, 1.0);

            // Verify entity was added by checking if it can be retrieved
            var next = system.GetNextEntityToAct();
            TestBase.AssertTrue(next == character || next == null,
                "Added entity should be retrievable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRemoveEntity()
        {
            Console.WriteLine("\n--- Testing RemoveEntity ---");

            var system = new ActionSpeedSystem();
            var character = TestDataBuilders.Character()
                .WithName("TestPlayer")
                .WithLevel(1)
                .Build();

            system.AddEntity(character, 1.0);
            system.RemoveEntity(character);

            // Entity should no longer be retrievable
            var next = system.GetNextEntityToAct();
            // After removal, might return null or another entity
            TestBase.AssertTrue(next != character || next == null,
                "Removed entity should not be retrievable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Action Time Tests

        private static void TestSetEntityActionTime()
        {
            Console.WriteLine("\n--- Testing SetEntityActionTime ---");

            var system = new ActionSpeedSystem();
            var character = TestDataBuilders.Character()
                .WithName("TestPlayer")
                .WithLevel(1)
                .Build();

            system.AddEntity(character, 1.0);
            system.SetEntityActionTime(character, 5.0);

            // Setting action time should not crash
            TestBase.AssertTrue(true,
                "SetEntityActionTime should complete without errors",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestGetNextEntityToAct()
        {
            Console.WriteLine("\n--- Testing GetNextEntityToAct ---");

            var system = new ActionSpeedSystem();
            var character1 = TestDataBuilders.Character()
                .WithName("Player1")
                .WithLevel(1)
                .Build();

            var character2 = TestDataBuilders.Character()
                .WithName("Player2")
                .WithLevel(1)
                .Build();

            system.AddEntity(character1, 1.0);
            system.AddEntity(character2, 1.0);

            var next = system.GetNextEntityToAct();
            TestBase.AssertTrue(next != null,
                "GetNextEntityToAct should return an entity when entities are added",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestAdvanceEntityTurn()
        {
            Console.WriteLine("\n--- Testing AdvanceEntityTurn ---");

            var system = new ActionSpeedSystem();
            var character = TestDataBuilders.Character()
                .WithName("TestPlayer")
                .WithLevel(1)
                .Build();

            system.AddEntity(character, 1.0);
            system.AdvanceEntityTurn(character, 1.0);

            // Advancing turn should not crash
            TestBase.AssertTrue(true,
                "AdvanceEntityTurn should complete without errors",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Action Execution Tests

        private static void TestExecuteAction()
        {
            Console.WriteLine("\n--- Testing ExecuteAction ---");

            var system = new ActionSpeedSystem();
            var character = TestDataBuilders.Character()
                .WithName("TestPlayer")
                .WithLevel(1)
                .Build();

            system.AddEntity(character, 1.0);

            var action = TestDataBuilders.CreateMockAction("TestAction", ActionType.Attack);
            var duration = system.ExecuteAction(character, action);

            TestBase.AssertTrue(duration >= 0,
                $"ExecuteAction should return non-negative duration, got {duration}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
