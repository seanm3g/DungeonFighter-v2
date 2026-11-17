using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Simulation;

namespace RPGGame.SimulationTest
{
    /// <summary>
    /// Quick test harness to demonstrate combat simulation
    /// Run with: dotnet run --project Code -- test-simulation
    /// </summary>
    public class SimulationTestHarness
    {

        internal static Character CreateTestPlayer()
        {
            var player = new Character("TestWarrior", 1);
            // Initialize with basic stats
            return player;
        }

        internal static List<Enemy> CreateTestEnemies()
        {
            var enemies = new List<Enemy>();
            
            // Create a few test enemies with different stats
            // Explicitly use the constructor with individual stats by providing strength parameter
            var goblin = new Enemy("Goblin", 1, 50, 8, 6, 4, 4);
            var orc = new Enemy("Orc", 1, 60, 10, 5, 3, 3);
            var skeleton = new Enemy("Skeleton", 1, 45, 7, 7, 5, 4);
            
            enemies.Add(goblin);
            enemies.Add(orc);
            enemies.Add(skeleton);
            
            return enemies;
        }
    }
}
