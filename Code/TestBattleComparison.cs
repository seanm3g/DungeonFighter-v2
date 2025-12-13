using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RPGGame;
using RPGGame.Entity;
using RPGGame.World;

namespace RPGGame.Tests
{
    /// <summary>
    /// Runs a single battle and logs all turn/action details for comparison with tuner
    /// </summary>
    public class TestBattleComparison
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== BATTLE COMPARISON TEST ===\n");
            Console.WriteLine("This test runs a single battle and logs all actions to compare");
            Console.WriteLine("the actual game simulation with the tuner's measurements.\n");

            // Create a player character
            var player = new Character("TestHero", 1);

            // Give them a weapon matching config
            var weapon = new WeaponItem(
                name: "Test Mace",
                tier: 1,
                baseDamage: 1,  // From config
                baseAttackSpeed: 1.0,
                weaponType: WeaponType.Mace
            );
            player.EquipItem(weapon, "weapon");
            player.Actions.AddClassActions(player, player.Progression, WeaponType.Mace);

            Console.WriteLine($"Player: {player.Name} (Level {player.Level})");
            Console.WriteLine($"  Health: {player.CurrentHealth}");
            Console.WriteLine($"  Weapon: {weapon.Name} (Base Damage: {weapon.BaseDamage})\n");

            // Create an enemy
            var enemy = EnemyLoader.CreateEnemy("Goblin", 1);
            if (enemy == null)
            {
                enemy = new Enemy("TestGoblin", 1, 30, 3, 2, 1, 3);
            }

            Console.WriteLine($"Enemy: {enemy.Name} (Level {enemy.Level})");
            Console.WriteLine($"  Health: {enemy.CurrentHealth}\n");

            // Create environment
            var environment = new Environment(
                name: "Test Room",
                description: "Testing environment",
                isHostile: false,
                theme: "neutral"
            );

            // Run the battle
            Console.WriteLine("=== BATTLE START ===\n");

            var combatManager = new CombatManager();
            CombatManager.DisableCombatUIOutput = true;  // Disable UI output

            // Run the full combat using the standard combat system
            bool playerWon = await combatManager.RunCombat(player, enemy, environment);

            Console.WriteLine("=== BATTLE COMPLETE ===\n");

            // Get final stats
            var finalTurn = combatManager.GetCurrentTurn();
            var finalActionCount = combatManager.GetTotalActionCount();
            var narrative = combatManager.GetCurrentBattleNarrative();
            var events = narrative?.GetAllEvents() ?? new List<BattleEvent>();

            Console.WriteLine("=== RESULTS ===\n");
            Console.WriteLine($"Narrative object is null: {narrative == null}");
            Console.WriteLine($"TurnManager.GetCurrentTurn(): {finalTurn}");
            Console.WriteLine($"TurnManager.GetTotalActionCount(): {finalActionCount}");
            Console.WriteLine($"Battle events logged: {events.Count}");
            Console.WriteLine($"Player won: {playerWon}");
            Console.WriteLine($"Final player health: {player.CurrentHealth}");
            Console.WriteLine($"Final enemy health: {enemy.CurrentHealth}\n");

            // Compare with tuner expectations
            Console.WriteLine("=== COMPARISON WITH TUNER ===");
            Console.WriteLine($"Tuner reports: 1 turn (100% win rate)");
            Console.WriteLine($"Actual game: {finalTurn} turns (player won: {playerWon})\n");

            if (finalTurn == 1)
            {
                Console.WriteLine("✓ MATCH: Game and tuner agree - battle is 1 turn");
                Console.WriteLine("The turn system is working correctly!");
            }
            else if (finalTurn > 1)
            {
                Console.WriteLine($"✗ MISMATCH: Game ran {finalTurn} turns, but tuner reported 1");
                Console.WriteLine("ISSUE: Different turn counting between game and tuner!");
            }
            else
            {
                Console.WriteLine($"? UNEXPECTED: Turn count is {finalTurn}");
            }
        }
    }
}
