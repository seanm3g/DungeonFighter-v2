using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RPGGame.Game
{
    /// <summary>
    /// Fast automated player that plays the game without delays
    /// </summary>
    public class FastAutomatedPlayer
    {
        private GamePlaySession? _session;
        private Random _random = new Random();

        public async Task Run()
        {
            try
            {
                Console.WriteLine("⚡ DUNGEON FIGHTER v2 - FAST AUTOMATED PLAYER");
                Console.WriteLine("Playing at maximum speed with all delays disabled...\n");

                // Initialize session
                _session = new GamePlaySession();
                await _session.Initialize();

                // Start new game
                await _session.StartNewGame();

                // Play game
                await PlayGameFast();

                // Display summary
                DisplaySummary();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                _session?.Dispose();
            }
        }

        private async Task PlayGameFast()
        {
            const int maxTurns = 500;
            int turnCount = 0;

            while (turnCount < maxTurns && _session != null)
            {
                turnCount++;

                var state = _session.CurrentState;
                if (state?.Player != null)
                {
                    Console.WriteLine($"[Turn {turnCount}] {state.CurrentState} - {state.Player.Name} Lvl {state.Player.Level} HP: {state.Player.CurrentHealth}/{state.Player.MaxHealth}");

                    if (state.Combat?.CurrentEnemy != null)
                    {
                        var enemy = state.Combat.CurrentEnemy;
                        Console.WriteLine($"  → Battling {enemy.Name} (Lvl {enemy.Level}) HP: {enemy.CurrentHealth}/{enemy.MaxHealth}");
                    }
                }

                // Get available actions
                var actions = await _session.GetAvailableActions();

                // Check for game over
                if (state?.CurrentState == "GameOver")
                {
                    Console.WriteLine("\n💀 GAME OVER!");
                    break;
                }

                // Choose action
                string action = ChooseAction(actions);

                // Execute immediately (no delay)
                await _session.ExecuteAction(action);

                // Check if character died
                if (_session.IsGameOver())
                {
                    if (_session.IsPlayerVictory())
                    {
                        Console.WriteLine("\n🎉 VICTORY!");
                    }
                    else
                    {
                        Console.WriteLine("\n💀 DEFEAT - Character died!");
                    }
                    break;
                }

                // No delay - play as fast as possible
            }

            if (turnCount >= maxTurns)
            {
                Console.WriteLine($"\n⏱️ Reached turn limit: {maxTurns}");
            }
        }

        private string ChooseAction(List<string> actions)
        {
            if (actions.Count == 0)
                return "1";

            // Find first numeric action
            foreach (var action in actions)
            {
                if (action.Length == 1 && char.IsDigit(action[0]))
                    return action;
            }

            return "1";
        }

        private void DisplaySummary()
        {
            if (_session?.CurrentState?.Player == null)
                return;

            var player = _session.CurrentState.Player;
            Console.WriteLine("\n" + new string('═', 60));
            Console.WriteLine("📊 FINAL STATS");
            Console.WriteLine(new string('═', 60));
            Console.WriteLine($"Turns: {_session.TurnCount}");
            Console.WriteLine($"Character: {player.Name}");
            Console.WriteLine($"Final Level: {player.Level}");
            Console.WriteLine($"Final Health: {player.CurrentHealth}/{player.MaxHealth}");
            Console.WriteLine($"Total XP: {player.XP}");
            Console.WriteLine(new string('═', 60));
        }

        public static async Task Main(string[] args)
        {
            var player = new FastAutomatedPlayer();
            await player.Run();
        }
    }
}
