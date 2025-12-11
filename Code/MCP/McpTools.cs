using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using RPGGame.MCP.Models;

namespace RPGGame.MCP
{
    /// <summary>
    /// MCP Tools for DungeonFighter game
    /// Each tool is registered with the MCP server using attributes
    /// </summary>
    [McpServerToolType]
    public static class McpTools
    {
        private static GameWrapper? _gameWrapper;

        /// <summary>
        /// Sets the game wrapper instance (called by MCP server)
        /// </summary>
        public static void SetGameWrapper(GameWrapper wrapper)
        {
            _gameWrapper = wrapper;
        }

        // ========== Game Control Tools ==========

        [McpServerTool(Name = "start_new_game", Title = "Start New Game")]
        [Description("Starts a new game instance. Returns the initial game state.")]
        public static Task<string> StartNewGame()
        {
            try
            {
                if (_gameWrapper == null)
                    throw new InvalidOperationException("Game wrapper not initialized");

                _gameWrapper.InitializeGame();
                _gameWrapper.ShowMainMenu();
                var state = _gameWrapper.GetGameState();
                return Task.FromResult(JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = false }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message }));
            }
        }

        [McpServerTool(Name = "save_game", Title = "Save Game")]
        [Description("Saves the current game state.")]
        public static Task<string> SaveGame()
        {
            try
            {
                if (_gameWrapper == null)
                    throw new InvalidOperationException("Game wrapper not initialized");

                _gameWrapper.SaveGame();
                return Task.FromResult(JsonSerializer.Serialize(new { success = true, message = "Game saved successfully" }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message }));
            }
        }

        // ========== Navigation Tools ==========

        [McpServerTool(Name = "handle_input", Title = "Handle Game Input")]
        [Description("Handles game input (menu selection, combat actions, etc.). Returns updated game state.")]
        public static async Task<string> HandleInput(
            [Description("The input to send to the game (e.g., '1', '2', 'attack')")] string input)
        {
            try
            {
                if (_gameWrapper == null)
                    throw new InvalidOperationException("Game wrapper not initialized");

                var state = await _gameWrapper.HandleInput(input);
                return JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { error = ex.Message });
            }
        }

        [McpServerTool(Name = "get_available_actions", Title = "Get Available Actions")]
        [Description("Gets the list of available actions for the current game state.")]
        public static Task<string> GetAvailableActions()
        {
            try
            {
                if (_gameWrapper == null)
                    throw new InvalidOperationException("Game wrapper not initialized");

                var actions = _gameWrapper.GetAvailableActions();
                return Task.FromResult(JsonSerializer.Serialize(actions));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message }));
            }
        }

        // ========== Information Tools ==========

        [McpServerTool(Name = "get_game_state", Title = "Get Game State")]
        [Description("Gets comprehensive game state snapshot including player, dungeon, room, and combat status.")]
        public static Task<string> GetGameState()
        {
            try
            {
                if (_gameWrapper == null)
                    throw new InvalidOperationException("Game wrapper not initialized");

                var state = _gameWrapper.GetGameState();
                return Task.FromResult(JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = false }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message }));
            }
        }

        [McpServerTool(Name = "get_player_stats", Title = "Get Player Stats")]
        [Description("Gets player character statistics including health, level, XP, attributes, and equipment.")]
        public static Task<string> GetPlayerStats()
        {
            try
            {
                if (_gameWrapper == null)
                    throw new InvalidOperationException("Game wrapper not initialized");

                var state = _gameWrapper.GetGameState();
                return Task.FromResult(JsonSerializer.Serialize(state.Player, new JsonSerializerOptions { WriteIndented = false }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message }));
            }
        }

        [McpServerTool(Name = "get_current_dungeon", Title = "Get Current Dungeon")]
        [Description("Gets information about the current dungeon (name, level, theme, rooms).")]
        public static Task<string> GetCurrentDungeon()
        {
            try
            {
                if (_gameWrapper == null)
                    throw new InvalidOperationException("Game wrapper not initialized");

                var state = _gameWrapper.GetGameState();
                return Task.FromResult(JsonSerializer.Serialize(state.CurrentDungeon, new JsonSerializerOptions { WriteIndented = false }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message }));
            }
        }

        [McpServerTool(Name = "get_inventory", Title = "Get Inventory")]
        [Description("Gets the player's inventory items.")]
        public static Task<string> GetInventory()
        {
            try
            {
                if (_gameWrapper == null)
                    throw new InvalidOperationException("Game wrapper not initialized");

                var state = _gameWrapper.GetGameState();
                return Task.FromResult(JsonSerializer.Serialize(state.Player?.Inventory ?? new List<ItemSnapshot>(), new JsonSerializerOptions { WriteIndented = false }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message }));
            }
        }

        [McpServerTool(Name = "get_combat_state", Title = "Get Combat State")]
        [Description("Gets current combat information if in combat (current enemy, available actions, turn status).")]
        public static Task<string> GetCombatState()
        {
            try
            {
                if (_gameWrapper == null)
                    throw new InvalidOperationException("Game wrapper not initialized");

                var state = _gameWrapper.GetGameState();
                return Task.FromResult(JsonSerializer.Serialize(state.Combat, new JsonSerializerOptions { WriteIndented = false }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message }));
            }
        }

        [McpServerTool(Name = "get_recent_output", Title = "Get Recent Output")]
        [Description("Gets recent game output/messages for AI context.")]
        public static Task<string> GetRecentOutput(
            [Description("Number of messages to retrieve (default: 10, max: 100)")] int count = 10)
        {
            try
            {
                if (_gameWrapper == null)
                    throw new InvalidOperationException("Game wrapper not initialized");

                // Limit to max 100 messages to prevent excessive token usage
                var limitedCount = Math.Min(count, 100);
                var output = _gameWrapper.GetRecentOutput(limitedCount);
                return Task.FromResult(JsonSerializer.Serialize(output));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message }));
            }
        }

        // ========== Dungeon Tools ==========

        [McpServerTool(Name = "get_available_dungeons", Title = "Get Available Dungeons")]
        [Description("Lists all available dungeons that can be entered.")]
        public static Task<string> GetAvailableDungeons()
        {
            try
            {
                if (_gameWrapper == null)
                    throw new InvalidOperationException("Game wrapper not initialized");

                var state = _gameWrapper.GetGameState();
                // TODO: Get available dungeons from game state
                return Task.FromResult(JsonSerializer.Serialize(new { message = "Available dungeons feature not yet implemented" }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message }));
            }
        }
    }
}

