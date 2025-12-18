using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RPGGame.MCP.Models;

namespace RPGGame.Game
{
    /// <summary>
    /// Interactive game player that allows real-time user interaction with the game
    /// Displays game state and accepts user input for actions
    /// </summary>
    public class InteractiveMCPGamePlayer
    {
        private GamePlaySession? _session;
        private bool _running = false;

        /// <summary>
        /// Run an interactive game session
        /// </summary>
        public async Task Play()
        {
            try
            {
                DisplayTitle();
                Console.WriteLine();

                // Initialize session
                Console.WriteLine("Initializing game session...");
                _session = new GamePlaySession();
                await _session.Initialize();
                Console.WriteLine("✓ Session initialized\n");

                // Start new game
                Console.WriteLine("Starting new game...");
                await _session.StartNewGame();
                Console.WriteLine("✓ Game started\n");

                _running = true;

                // Main game loop
                while (_running)
                {
                    try
                    {
                        // Display current state
                        DisplayGameState();

                        // Get available actions
                        var actions = await _session.GetAvailableActions();

                        if (actions.Count == 0)
                        {
                            Console.WriteLine("No actions available. Game may be over.");
                            break;
                        }

                        // Display actions
                        DisplayActions(actions);

                        // Get user input
                        string input = GetUserInput();

                        if (input.Equals("quit", StringComparison.OrdinalIgnoreCase) ||
                            input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                        {
                            _running = false;
                            break;
                        }

                        if (input.Equals("help", StringComparison.OrdinalIgnoreCase))
                        {
                            DisplayHelp();
                            continue;
                        }

                        if (input.Equals("status", StringComparison.OrdinalIgnoreCase))
                        {
                            await DisplayDetailedStatus();
                            continue;
                        }

                        // Execute action
                        Console.WriteLine($"\nExecuting action: {input}");
                        await _session.ExecuteAction(input);

                        // Check for game over
                        if (_session.IsGameOver())
                        {
                            DisplayGameState();
                            if (_session.IsPlayerVictory())
                            {
                                Console.WriteLine("\n🎉 Victory! You completed the dungeon!");
                            }
                            else
                            {
                                Console.WriteLine("\n💀 Defeat! Your character was defeated.");
                            }
                            break;
                        }

                        // Get recent output for feedback
                        var output = await _session.GetRecentOutput(5);
                        if (output.Count > 0)
                        {
                            Console.WriteLine("\nRecent Events:");
                            foreach (var message in output.TakeLast(3))
                            {
                                Console.WriteLine($"  • {message}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error executing action: {ex.Message}");
                        Console.WriteLine("Try entering a valid action number or 'help' for commands.\n");
                    }
                }

                // Game ended
                DisplayGameSummary();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Fatal error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                // Cleanup
                if (_session != null)
                {
                    _session.Dispose();
                    _session = null;
                }

                Console.WriteLine("\nGame session ended. Thanks for playing!");
            }
        }

        private void DisplayTitle()
        {
            Console.WriteLine("╔══════════════════════════════════════════════════╗");
            Console.WriteLine("║     DUNGEON FIGHTER v2 - INTERACTIVE PLAYER      ║");
            Console.WriteLine("║              MCP Tool Integration                ║");
            Console.WriteLine("╚══════════════════════════════════════════════════╝");
        }

        private void DisplayGameState()
        {
            Console.WriteLine("\n" + new string('═', 60));

            if (_session?.CurrentState == null)
            {
                Console.WriteLine("Game state unavailable");
                return;
            }

            var state = _session.CurrentState;

            Console.WriteLine($"Turn: {_session.TurnCount} | Status: {state.CurrentState}");

            if (state.Player != null)
            {
                var player = state.Player;
                var healthBar = GetHealthBar(player.CurrentHealth, player.MaxHealth);
                Console.WriteLine($"Player: {player.Name} (Level {player.Level}) | Health: {healthBar} {player.CurrentHealth}/{player.MaxHealth}");
                if (player.XP > 0)
                {
                    Console.WriteLine($"Experience: {player.XP} XP");
                }
            }

            if (state.CurrentDungeon != null)
            {
                var dungeon = state.CurrentDungeon;
                Console.WriteLine($"Location: {dungeon.Name} - Room {dungeon.CurrentRoomNumber}/{dungeon.TotalRooms}");
            }

            if (state.Combat != null)
            {
                var combat = state.Combat;
                Console.WriteLine($"⚔️  Combat Active!");
                if (combat.CurrentEnemy != null)
                {
                    var enemyHealth = GetHealthBar(combat.CurrentEnemy.CurrentHealth, combat.CurrentEnemy.MaxHealth);
                    Console.WriteLine($"   Enemy: {combat.CurrentEnemy.Name} (Level {combat.CurrentEnemy.Level}) | Health: {enemyHealth} {combat.CurrentEnemy.CurrentHealth}/{combat.CurrentEnemy.MaxHealth}");
                }
            }

            Console.WriteLine(new string('═', 60));
        }

        private void DisplayActions(List<string> actions)
        {
            Console.WriteLine("\n📋 Available Actions:");
            for (int i = 0; i < actions.Count; i++)
            {
                var action = actions[i];
                Console.WriteLine($"   [{i + 1}] {action}");
            }
        }

        private string GetUserInput()
        {
            Console.Write("\n➤ Enter action (number, 'help', 'status', or 'quit'): ");
            var input = Console.ReadLine() ?? "";
            return input.Trim();
        }

        private void DisplayHelp()
        {
            Console.WriteLine("\n📚 Command Help:");
            Console.WriteLine("   • Enter action number: Execute that action (1-9)");
            Console.WriteLine("   • Type the action name: Execute by name if available");
            Console.WriteLine("   • 'status': View detailed character status");
            Console.WriteLine("   • 'help': Display this help message");
            Console.WriteLine("   • 'quit' or 'exit': End the game session");
            Console.WriteLine();
        }

        private async Task DisplayDetailedStatus()
        {
            if (_session == null)
                return;

            Console.WriteLine("\n" + new string('═', 60));
            Console.WriteLine("DETAILED STATUS");
            Console.WriteLine(new string('═', 60));

            try
            {
                var playerStats = await _session.GetPlayerStats();
                if (playerStats != null)
                {
                    Console.WriteLine($"Player Stats: {playerStats}");
                }

                var dungeonInfo = await _session.GetCurrentDungeon();
                if (dungeonInfo != null)
                {
                    Console.WriteLine($"Dungeon Info: {dungeonInfo}");
                }

                var recentOutput = await _session.GetRecentOutput(15);
                Console.WriteLine("\nRecent Game Events:");
                foreach (var message in recentOutput)
                {
                    Console.WriteLine($"  • {message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting status: {ex.Message}");
            }

            Console.WriteLine();
        }

        private void DisplayGameSummary()
        {
            if (_session == null)
                return;

            Console.WriteLine("\n" + new string('═', 60));
            Console.WriteLine("GAME SUMMARY");
            Console.WriteLine(new string('═', 60));

            var result = new GamePlaySessionResult
            {
                TurnsPlayed = _session.TurnCount,
                ActionSequence = _session.ActionHistory.ToList(),
                Success = _session.IsPlayerVictory()
            };

            Console.WriteLine($"Outcome: {(result.Success ? "Victory" : "Defeat")}");
            Console.WriteLine($"Turns Played: {result.TurnsPlayed}");
            Console.WriteLine($"Actions Taken: {result.ActionSequence.Count}");

            if (_session.CurrentState?.Player != null)
            {
                Console.WriteLine($"Final Level: {_session.CurrentState.Player.Level}");
                Console.WriteLine($"Final Health: {_session.CurrentState.Player.CurrentHealth}/{_session.CurrentState.Player.MaxHealth}");
            }

            Console.WriteLine();
        }

        private string GetHealthBar(int current, int max)
        {
            const int barLength = 20;
            int filled = (int)((double)current / max * barLength);
            filled = Math.Max(0, Math.Min(barLength, filled));
            var bar = "[" + new string('█', filled) + new string('░', barLength - filled) + "]";
            return bar;
        }

        /// <summary>
        /// Static entry point for running the interactive player
        /// </summary>
        public static async Task Main(string[] args)
        {
            var player = new InteractiveMCPGamePlayer();
            await player.Play();
        }
    }
}
