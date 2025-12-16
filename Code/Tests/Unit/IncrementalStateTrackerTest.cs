using System;
using RPGGame.MCP;
using RPGGame.MCP.Models;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Unit tests for IncrementalStateTracker
    /// </summary>
    public static class IncrementalStateTrackerTest
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== IncrementalStateTracker Tests ===\n");
            
            TestBasicTracking();
            TestPlayerHealthTracking();
            TestEnemyHealthTracking();
            TestStateChanges();
            TestReset();
            
            Console.WriteLine("\n✓ All IncrementalStateTracker tests passed!");
        }

        private static void TestBasicTracking()
        {
            var tracker = new IncrementalStateTracker();
            var snapshot = new GameStateSnapshot
            {
                CurrentState = "GameLoop",
                Player = new PlayerSnapshot { CurrentHealth = 100, MaxHealth = 100 }
            };
            
            var result = tracker.CreateFullSnapshot(snapshot);
            TestHarnessBase.AssertNotNull(result, "Should create full snapshot");
            TestHarnessBase.AssertEqual("GameLoop", result.CurrentState, "Should preserve state");
        }

        private static void TestPlayerHealthTracking()
        {
            var tracker = new IncrementalStateTracker();
            var snapshot = new GameStateSnapshot
            {
                CurrentState = "Combat",
                Player = new PlayerSnapshot { CurrentHealth = 100, MaxHealth = 100, HealthPercentage = 100.0 }
            };
            
            tracker.CreateFullSnapshot(snapshot);
            tracker.MarkPlayerHealthChanged(80, 100, 80.0);
            
            var newSnapshot = new GameStateSnapshot
            {
                CurrentState = "Combat",
                Player = new PlayerSnapshot { CurrentHealth = 80, MaxHealth = 100, HealthPercentage = 80.0 }
            };
            
            var incremental = tracker.CreateIncrementalSnapshot(newSnapshot);
            TestHarnessBase.AssertNotNull(incremental.Player, "Player should exist");
            TestHarnessBase.AssertEqual(80, incremental.Player!.CurrentHealth, "Should update health");
        }

        private static void TestEnemyHealthTracking()
        {
            var tracker = new IncrementalStateTracker();
            var snapshot = new GameStateSnapshot
            {
                CurrentState = "Combat",
                Combat = new CombatSnapshot
                {
                    CurrentEnemy = new EnemySnapshot { CurrentHealth = 50, MaxHealth = 50, HealthPercentage = 100.0 }
                }
            };
            
            tracker.CreateFullSnapshot(snapshot);
            tracker.MarkEnemyHealthChanged(30, 50, 60.0);
            
            var newSnapshot = new GameStateSnapshot
            {
                CurrentState = "Combat",
                Combat = new CombatSnapshot
                {
                    CurrentEnemy = new EnemySnapshot { CurrentHealth = 30, MaxHealth = 50, HealthPercentage = 60.0 }
                }
            };
            
            var incremental = tracker.CreateIncrementalSnapshot(newSnapshot);
            TestHarnessBase.AssertNotNull(incremental.Combat, "Combat should exist");
            TestHarnessBase.AssertNotNull(incremental.Combat!.CurrentEnemy, "Enemy should exist");
            TestHarnessBase.AssertEqual(30, incremental.Combat.CurrentEnemy!.CurrentHealth, "Should update enemy health");
        }

        private static void TestStateChanges()
        {
            var tracker = new IncrementalStateTracker();
            var snapshot = new GameStateSnapshot { CurrentState = "GameLoop" };
            tracker.CreateFullSnapshot(snapshot);
            
            tracker.MarkStateChanged("Combat");
            var newSnapshot = new GameStateSnapshot { CurrentState = "Combat" };
            var incremental = tracker.CreateIncrementalSnapshot(newSnapshot);
            
            TestHarnessBase.AssertEqual("Combat", incremental.CurrentState, "Should update state");
        }

        private static void TestReset()
        {
            var tracker = new IncrementalStateTracker();
            tracker.MarkPlayerHealthChanged(50, 100, 50.0);
            tracker.Reset();
            
            TestHarnessBase.AssertEqual(0, tracker.ChangeCount, "Reset should clear changes");
            TestHarnessBase.AssertTrue(!tracker.HasChanges, "Should have no changes after reset");
        }
    }
}
