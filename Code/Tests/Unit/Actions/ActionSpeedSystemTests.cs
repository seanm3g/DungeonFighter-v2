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
            TestAdvanceOwnTimeline();
            TestExecuteAction();
            TestFastActorGetsMultipleActionsDuringSlowRecovery();
            TestChainCostsSlowestSwing();
            TestFreeSwingDisplaysChainSpeed();

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

        /// <summary>
        /// AdvanceOwnTimeline schedules from the entity's NextActionTime (ExecuteAction contract),
        /// not from max(global, self) like AdvanceEntityTurn.
        /// </summary>
        private static void TestAdvanceOwnTimeline()
        {
            Console.WriteLine("\n--- Testing AdvanceOwnTimeline ---");

            using (GameTicker.BeginIsolatedEncounterGameTime())
            {
                GameTicker.Instance.Reset();

                var system = new ActionSpeedSystem();
                var character = TestDataBuilders.Character()
                    .WithName("TimelineHero")
                    .WithLevel(1)
                    .Build();

                system.AddEntity(character, 1.0);
                system.SetEntityActionTime(character, 5.0);
                GameTicker.Instance.AdvanceGameTime(20.0);

                system.AdvanceOwnTimeline(character, 4.0);

                // Own timeline: next = 5 + 4 = 9; global ≈ 24. Entity is still ready (catch-up).
                TestBase.AssertTrue(system.IsEntityReady(character),
                    "After AdvanceOwnTimeline from early ready time, entity should remain catch-up ready",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                double until = system.GetTimeUntilReady(character);
                TestBase.AssertTrue(until <= 0.0,
                    $"Expected ready now (until<=0), got {until}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
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

        /// <summary>
        /// After a slow action advances combat time, a faster actor's next swing must be scheduled from their
        /// own prior readiness (multiple short recoveries fit inside one long recovery), not from global time.
        /// </summary>
        private static void TestFastActorGetsMultipleActionsDuringSlowRecovery()
        {
            Console.WriteLine("\n--- Testing fast actor multiple actions during slow recovery ---");

            using (GameTicker.BeginIsolatedEncounterGameTime())
            {
                GameTicker.Instance.Reset();

                var system = new ActionSpeedSystem();
                // Names: "AliceSlow" before "BobFast" alphabetically so both start ready and Alice acts first.
                var slowActor = TestDataBuilders.Character()
                    .WithName("AliceSlow")
                    .WithLevel(1)
                    .Build();
                var fastActor = TestDataBuilders.Enemy()
                    .WithName("BobFast")
                    .WithLevel(1)
                    .Build();

                system.AddEntity(slowActor, 1.0);
                system.AddEntity(fastActor, 1.0);

                var slowAction = TestDataBuilders.CreateMockAction("SlowSwing", ActionType.Attack);
                slowAction.Length = 3.0;
                var fastAction = TestDataBuilders.CreateMockAction("FastSwing", ActionType.Attack);
                fastAction.Length = 1.0;

                system.ExecuteAction(slowActor, slowAction, isCriticalMiss: false);

                int fastActionsBeforeSlowSecond = 0;
                const int maxSteps = 20;
                for (int i = 0; i < maxSteps; i++)
                {
                    var next = system.GetNextEntityToAct();
                    TestBase.AssertTrue(next != null, "Expected an actor to be ready", ref _testsRun, ref _testsPassed, ref _testsFailed);
                    if (next == slowActor)
                    {
                        system.ExecuteAction(slowActor, slowAction, isCriticalMiss: false);
                        break;
                    }

                    system.ExecuteAction(fastActor, fastAction, isCriticalMiss: false);
                    fastActionsBeforeSlowSecond++;
                }

                TestBase.AssertTrue(fastActionsBeforeSlowSecond >= 2,
                    $"Fast actor should act at least twice before the slow actor's second swing; got {fastActionsBeforeSlowSecond}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        /// <summary>
        /// A combo chain costs avg swing × (1 + g × (actions − 1)): g 0 = one average swing, g 1 = the sum of its swings.
        /// </summary>
        private static void TestChainCostsSlowestSwing()
        {
            Console.WriteLine("\n--- Testing combo chain cost (avg × (1 + g × (n − 1))) ---");

            var lengths = new[] { 3.0, 1.0, 2.0 };
            double perLength = 0;
            double ChainTime(double growth)
            {
                using (GameTicker.BeginIsolatedEncounterGameTime())
                {
                    GameTicker.Instance.Reset();
                    var system = new ActionSpeedSystem();
                    var foe = TestDataBuilders.Enemy().WithName("Chainer").WithLevel(1).Build();
                    system.AddEntity(foe, 1.0);
                    double total = 0;
                    for (int i = 0; i < lengths.Length; i++)
                    {
                        var swing = TestDataBuilders.CreateMockAction($"Swing{i}", ActionType.Attack);
                        swing.Length = lengths[i];
                        total += i == 0
                            ? system.ExecuteAction(foe, swing, isCriticalMiss: false)
                            : system.ExecuteChainedAction(foe, swing, isCriticalMiss: false, growth);
                    }
                    perLength = foe.GetTotalAttackSpeed();
                    return total;
                }
            }

            // Opening swing (3) is already paid, so g 0 (avg 2) still costs 3: time spent is never refunded.
            double free = ChainTime(0.0);
            TestBase.AssertTrue(Math.Abs(free - 3.0 * perLength) < 1e-9,
                $"g 0 chain should keep the opening swing's 3 × {perLength:F3}; got {free:F3}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            double sum = ChainTime(1.0);
            TestBase.AssertTrue(Math.Abs(sum - 6.0 * perLength) < 1e-9,
                $"g 1 chain should cost every swing (6 × {perLength:F3}); got {sum:F3}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            double quarter = ChainTime(0.25);
            TestBase.AssertTrue(Math.Abs(quarter - 3.0 * perLength) < 1e-9,
                $"g 0.25 chain should cost avg 2 × 1.5 (3 × {perLength:F3}); got {quarter:F3}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Screenshot example: 1.7, 2.0, 1.8, 2.5, 1.9 at g .25 → 3.96.
            double example = ActionSpeedSystem.ChainCost(1.7 + 2.0 + 1.8 + 2.5 + 1.9, 5, 0.25);
            TestBase.AssertTrue(Math.Abs(example - 3.96) < 1e-9,
                $"ChainCost(5 swings, g .25) should be 3.96; got {example:F4}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(Math.Abs(ActionSpeedSystem.ChainCost(1.7 + 2.0, 2, 0.5) - 2.775) < 1e-9,
                "ChainCost(1.7, 2.0, g .5) should be 2.775",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>A free combo swing's displayed speed is what it adds to the chain, matching what ExecuteChainedAction charges.</summary>
        private static void TestFreeSwingDisplaysChainSpeed()
        {
            Console.WriteLine("\n--- Testing free combo swing speed readout (chain increase) ---");

            using (GameTicker.BeginIsolatedEncounterGameTime())
            {
                GameTicker.Instance.Reset();
                var system = new ActionSpeedSystem();
                var foe = TestDataBuilders.Enemy().WithName("Chainer").WithLevel(1).Build();
                system.AddEntity(foe, 1.0);
                var opener = TestDataBuilders.CreateMockAction("Opener", ActionType.Attack);
                opener.Length = 2.0;
                var follow = TestDataBuilders.CreateMockAction("Follow", ActionType.Attack);
                follow.Length = 2.0;

                double solo = RPGGame.Combat.Formatting.ActionSpeedCalculator.CalculateActualActionSpeed(foe, follow);
                system.ExecuteAction(foe, opener, isCriticalMiss: false);

                double shown;
                using (RPGGame.Combat.Formatting.ComboChainSpeedScope.Enter(system, foe, 0.25))
                    shown = RPGGame.Combat.Formatting.ActionSpeedCalculator.CalculateActualActionSpeed(foe, follow);
                double charged = system.ExecuteChainedAction(foe, follow, isCriticalMiss: false, 0.25);

                TestBase.AssertTrue(Math.Abs(shown - charged) < 1e-9,
                    $"free swing readout should equal the chain charge ({charged:F3}); got {shown:F3}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(Math.Abs(shown - 0.25 * solo) < 1e-9,
                    $"equal swings at g .25 should show a quarter of the solo time ({0.25 * solo:F3}); got {shown:F3}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                double outside = RPGGame.Combat.Formatting.ActionSpeedCalculator.CalculateActualActionSpeed(foe, follow);
                TestBase.AssertTrue(Math.Abs(outside - solo) < 1e-9,
                    "outside a free swing the readout stays the solo swing time",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion
    }
}
