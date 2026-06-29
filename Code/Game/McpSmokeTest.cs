using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RPGGame.MCP;
using RPGGame.MCP.Tools;

namespace RPGGame.Game
{
    /// <summary>
    /// Headless smoke test for the MCP gameplay path (GamePlaySession + handle_input).
    /// Run: dotnet run -- MCPSMOKE
    /// </summary>
    public static class McpSmokeTest
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("=== MCP Gameplay Smoke Test ===");
            Console.WriteLine("Exercises GamePlaySession (same path as MCP handle_input)\n");

            var session = new GamePlaySession();
            await session.Initialize();
            await session.StartNewGame();

            // Agent context: TrainingGroundOffer should expose labeled skip option
            var wrapper = McpToolState.GameWrapper;
            if (wrapper?.Game != null)
            {
                await session.ExecuteAction("1");
                var agentCtx = AgentContextBuilder.Build(wrapper.Game, wrapper.OutputCapture);
                if (agentCtx.CurrentState == "TrainingGroundOffer")
                {
                    var skip = agentCtx.Choices.FirstOrDefault(c => c.Input == "2");
                    if (skip == null || string.IsNullOrWhiteSpace(skip.Label))
                        throw new InvalidOperationException("Agent context missing labeled skip on TrainingGroundOffer.");
                    Console.WriteLine($"  Agent context OK: '{skip.Input}' = {skip.Label}");
                }
            }

            const int maxTurns = 40;
            var seenStates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool enteredDungeon = false;
            bool passed = false;

            for (int turn = 1; turn <= maxTurns; turn++)
            {
                var state = session.CurrentState;
                var current = state?.CurrentState ?? "(null)";
                seenStates.Add(current);

                Console.WriteLine($"Turn {turn}: {current}");
                if (state?.Player != null)
                {
                    var p = state.Player;
                    Console.WriteLine($"  Player: {p.Name} Lvl {p.Level} HP {p.CurrentHealth}/{p.MaxHealth}");
                }

                if (state?.CurrentDungeon != null)
                {
                    var d = state.CurrentDungeon;
                    Console.WriteLine($"  Dungeon: {d.Name} room {d.CurrentRoomNumber}/{d.TotalRooms}");
                }

                bool selectingDungeon = current == "DungeonSelection";
                if (selectingDungeon)
                {
                    Console.WriteLine("  Entering dungeon (runs all rooms in one handle_input call)...");
                }

                var actions = await session.GetAvailableActions();
                string choice = PickAction(current, actions);
                Console.WriteLine($"  Action: '{choice}' ({Math.Max(actions.Count, 1)} options)");
                await session.ExecuteAction(choice);

                // Re-check state after action (dungeon entry runs synchronously through all rooms)
                state = session.CurrentState;
                current = state?.CurrentState ?? "(null)";
                seenStates.Add(current);

                if (selectingDungeon)
                {
                    enteredDungeon = true;
                    passed = current is "DungeonCompletion" or "GameLoop" or "Death";
                    Console.WriteLine($"  After dungeon run: {current}");
                    if (passed)
                    {
                        Console.WriteLine($"\nPASS: Entered dungeon and completed without hang — final state: {current}");
                        break;
                    }
                }

                if (session.IsGameOver())
                {
                    Console.WriteLine("\nGame over.");
                    break;
                }
            }

            Console.WriteLine("\nStates visited: " + string.Join(" -> ", seenStates));
            session.Dispose();

            if (!enteredDungeon || !passed)
            {
                Console.WriteLine("\nFAIL: Did not complete a dungeon run.");
                throw new InvalidOperationException("MCP smoke test did not complete dungeon entry.");
            }
        }

        private static string PickAction(string currentState, List<string> actions)
        {
            // Known menus — GameStateSerializer omits actions for several states
            string preferred = currentState switch
            {
                "TrainingGroundOffer" => "2",
                "PreWeaponPathIntro" => "1",
                "WeaponSelection" => "1",
                "CharacterCreation" => "1",
                "GameLoop" => "1",
                "DungeonSelection" => "1",
                "Dungeon" or "DungeonExploration" => "1",
                _ => "1"
            };

            if (actions.Count > 0)
            {
                foreach (var action in actions)
                {
                    if (action == preferred)
                        return preferred;
                }

                foreach (var action in actions)
                {
                    if (action.Length == 1 && char.IsDigit(action[0]))
                        return action;
                }

                return actions[0];
            }

            return preferred;
        }
    }
}
