using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using RPGGame.MCP.Models;
using RPGGame.MCP.Tools;

namespace RPGGame.MCP.Tools
{
    /// <summary>
    /// Information retrieval tools (get game state, player stats, etc.)
    /// </summary>
    public static class InformationTools
    {
        [McpServerTool(Name = "get_game_state", Title = "Get Game State")]
        [Description("Gets comprehensive game state snapshot including player, dungeon, room, and combat status.")]
        public static Task<string> GetGameState()
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var wrapper = McpToolState.GameWrapper;
                if (wrapper == null)
                    throw new InvalidOperationException("Game wrapper not initialized");

                return wrapper.GetGameState();
            }, writeIndented: false);
        }

        [McpServerTool(Name = "get_player_stats", Title = "Get Player Stats")]
        [Description("Gets player character statistics including health, level, XP, attributes, and equipment.")]
        public static Task<string> GetPlayerStats()
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var wrapper = McpToolState.GameWrapper;
                if (wrapper == null)
                    throw new InvalidOperationException("Game wrapper not initialized");

                var state = wrapper.GetGameState();
                return state.Player ?? (object)new { error = "Player not found" };
            }, writeIndented: false);
        }

        [McpServerTool(Name = "get_current_dungeon", Title = "Get Current Dungeon")]
        [Description("Gets information about the current dungeon (name, level, theme, rooms).")]
        public static Task<string> GetCurrentDungeon()
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var wrapper = McpToolState.GameWrapper;
                if (wrapper == null)
                    throw new InvalidOperationException("Game wrapper not initialized");

                var state = wrapper.GetGameState();
                return state.CurrentDungeon ?? (object)new { error = "No current dungeon" };
            }, writeIndented: false);
        }

        [McpServerTool(Name = "get_inventory", Title = "Get Inventory")]
        [Description("Gets the player's inventory items.")]
        public static Task<string> GetInventory()
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var wrapper = McpToolState.GameWrapper;
                if (wrapper == null)
                    throw new InvalidOperationException("Game wrapper not initialized");

                var state = wrapper.GetGameState();
                return state.Player?.Inventory ?? new List<ItemSnapshot>();
            }, writeIndented: false);
        }

        [McpServerTool(Name = "get_combat_state", Title = "Get Combat State")]
        [Description("Gets current combat information if in combat (current enemy, available actions, turn status).")]
        public static Task<string> GetCombatState()
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var wrapper = McpToolState.GameWrapper;
                if (wrapper == null)
                    throw new InvalidOperationException("Game wrapper not initialized");

                var state = wrapper.GetGameState();
                return state.Combat ?? (object)new { error = "Not in combat" };
            }, writeIndented: false);
        }

        [McpServerTool(Name = "get_recent_output", Title = "Get Recent Output")]
        [Description("Gets recent game output/messages for AI context.")]
        public static Task<string> GetRecentOutput(
            [Description("Number of messages to retrieve (default: 10, max: 100)")] int count = 10)
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var wrapper = McpToolState.GameWrapper;
                if (wrapper == null)
                    throw new InvalidOperationException("Game wrapper not initialized");

                var limitedCount = Math.Min(count, 100);
                return wrapper.GetRecentOutput(limitedCount);
            });
        }
    }
}
