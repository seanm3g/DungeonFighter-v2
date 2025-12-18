using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RPGGame.Game
{
    /// <summary>
    /// Automated demo that plays through the game with AI-driven decisions
    /// Shows how to use the GamePlaySession and MCP tools programmatically
    /// </summary>
    public class AutomatedGameplayDemo
    {
        private GamePlaySession? _session;
        private Random _random = new Random();

        public async Task RunDemo()
        {
            try
            {
                DisplayDemoTitle();
                Console.WriteLine();

                // Initialize session
                Console.WriteLine("📍 Initializing game session...");
                _session = new GamePlaySession();
                await _session.Initialize();
                Console.WriteLine("✓ Session initialized\n");

                // Start new game
                Console.WriteLine("📍 Starting new game...");
                await _session.StartNewGame();
                Console.WriteLine("✓ Game started\n");

                // Play game
                await PlayGame();

                // Display final summary
                DisplayGameSummary();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                if (_session != null)
                {
                    _session.Dispose();
                    _session = null;
                }
            }
        }

        private async Task PlayGame()
        {
            const int maxTurns = 100;
            int turnCount = 0;

            while (turnCount < maxTurns && _session != null)
            {
                turnCount++;

                // Display current state
                DisplayGameState(turnCount);

                // Get available actions
                var actions = await _session.GetAvailableActions();

                // Some UI screens don't populate actions in state, but we can still send input
                // Only break if we've genuinely reached game over
                if (actions.Count == 0 && _session.CurrentState?.CurrentState == "GameOver")
                {
                    Console.WriteLine("No actions available. Game over.");
                    break;
                }

                // Choose action (simple: always pick first action for demo)
                string action = ChooseAction(actions, turnCount);
                Console.WriteLine($"Executing: {action}\n");

                // Execute action
                await _session.ExecuteAction(action);

                // Check for game over
                if (_session.IsGameOver())
                {
                    Console.WriteLine("\n" + new string('═', 60));
                    if (_session.IsPlayerVictory())
                    {
                        Console.WriteLine("🎉 VICTORY! Game completed successfully!");
                    }
                    else
                    {
                        Console.WriteLine("💀 DEFEAT! Character was defeated.");
                    }
                    Console.WriteLine(new string('═', 60) + "\n");
                    break;
                }

                // Small delay for readability
                await Task.Delay(200);
            }

            if (turnCount >= maxTurns)
            {
                Console.WriteLine($"\n⏱️  Demo reached max turn limit ({maxTurns} turns)");
            }
        }

        private string ChooseAction(List<string> actions, int turnNumber)
        {
            // If no actions in list, try common menu options
            if (actions.Count == 0)
            {
                // Common progression: 1 usually means "continue" or "confirm"
                return "1";
            }

            // Try to find first numeric action
            foreach (var action in actions)
            {
                if (action.Length == 1 && char.IsDigit(action[0]))
                {
                    return action;
                }
            }

            // Default to first action
            return "1";
        }

        private void DisplayGameState(int turnNumber)
        {
            if (_session?.CurrentState == null)
                return;

            var state = _session.CurrentState;

            Console.WriteLine(new string('─', 60));
            Console.WriteLine($"Turn {turnNumber} | Status: {state.CurrentState}");

            if (state.Player != null)
            {
                var player = state.Player;
                var healthPercent = (double)player.CurrentHealth / player.MaxHealth * 100;
                Console.WriteLine($"  👤 {player.Name} (Lvl {player.Level}) | ❤️  {player.CurrentHealth}/{player.MaxHealth} ({healthPercent:F0}%)");

                if (state.CurrentDungeon != null)
                {
                    Console.WriteLine($"  📍 {state.CurrentDungeon.Name} - Room {state.CurrentDungeon.CurrentRoomNumber}/{state.CurrentDungeon.TotalRooms}");
                }

                if (state.Combat != null && state.Combat.CurrentEnemy != null)
                {
                    var enemy = state.Combat.CurrentEnemy;
                    var enemyHealthPercent = (double)enemy.CurrentHealth / enemy.MaxHealth * 100;
                    Console.WriteLine($"  ⚔️  Fighting {enemy.Name} (Lvl {enemy.Level}) | ❤️  {enemy.CurrentHealth}/{enemy.MaxHealth} ({enemyHealthPercent:F0}%)");
                }
            }

            Console.WriteLine(new string('─', 60));
        }

        private void DisplayDemoTitle()
        {
            Console.WriteLine("╔══════════════════════════════════════════════════╗");
            Console.WriteLine("║  DUNGEON FIGHTER v2 - AUTOMATED GAMEPLAY DEMO   ║");
            Console.WriteLine("║              MCP Tool Integration                ║");
            Console.WriteLine("╚══════════════════════════════════════════════════╝");
        }

        private void DisplayGameSummary()
        {
            if (_session == null)
                return;

            Console.WriteLine("\n" + new string('═', 60));
            Console.WriteLine("📊 GAME SUMMARY");
            Console.WriteLine(new string('═', 60));

            Console.WriteLine($"Total Turns Played: {_session.TurnCount}");
            Console.WriteLine($"Final Status: {_session.CurrentState?.CurrentState ?? "Unknown"}");

            if (_session.CurrentState?.Player != null)
            {
                var player = _session.CurrentState.Player;
                Console.WriteLine($"Final Level: {player.Level}");
                Console.WriteLine($"Final Health: {player.CurrentHealth}/{player.MaxHealth}");
                Console.WriteLine($"Experience: {player.XP} XP");
            }

            Console.WriteLine("\nMCP Tools Used Successfully:");
            Console.WriteLine("  ✓ GameControlTools.StartNewGame()");
            Console.WriteLine("  ✓ NavigationTools.HandleInput()");
            Console.WriteLine("  ✓ NavigationTools.GetAvailableActions()");
            Console.WriteLine("  ✓ InformationTools.GetGameState()");
            Console.WriteLine("\n✓ Demo completed successfully!");
            Console.WriteLine(new string('═', 60));
        }

        public static async Task Main(string[] args)
        {
            var demo = new AutomatedGameplayDemo();
            await demo.RunDemo();
        }
    }
}
