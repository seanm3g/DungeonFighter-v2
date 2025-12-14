using System;
using System.ComponentModel;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using Tools = RPGGame.MCP.Tools;

namespace RPGGame.MCP
{
    /// <summary>
    /// Partial class for Game Control, Navigation, Information, and Dungeon tools
    /// </summary>
    public static partial class McpTools
    {
        // ========== Game Control Tools ==========

        [McpServerTool(Name = "start_new_game", Title = "Start New Game")]
        [Description("Starts a new game instance. Returns the initial game state.")]
        public static Task<string> StartNewGame()
        {
            return Tools.GameControlTools.StartNewGame();
        }

        [McpServerTool(Name = "save_game", Title = "Save Game")]
        [Description("Saves the current game state.")]
        public static Task<string> SaveGame()
        {
            return Tools.GameControlTools.SaveGame();
        }

        // ========== Navigation Tools ==========

        [McpServerTool(Name = "handle_input", Title = "Handle Game Input")]
        [Description("Handles game input (menu selection, combat actions, etc.). Returns updated game state.")]
        public static Task<string> HandleInput(
            [Description("The input to send to the game (e.g., '1', '2', 'attack')")] string input)
        {
            return Tools.NavigationTools.HandleInput(input);
        }

        [McpServerTool(Name = "get_available_actions", Title = "Get Available Actions")]
        [Description("Gets the list of available actions for the current game state.")]
        public static Task<string> GetAvailableActions()
        {
            return Tools.NavigationTools.GetAvailableActions();
        }

        // ========== Information Tools ==========

        [McpServerTool(Name = "get_game_state", Title = "Get Game State")]
        [Description("Gets comprehensive game state snapshot including player, dungeon, room, and combat status.")]
        public static Task<string> GetGameState()
        {
            return Tools.InformationTools.GetGameState();
        }

        [McpServerTool(Name = "get_player_stats", Title = "Get Player Stats")]
        [Description("Gets player character statistics including health, level, XP, attributes, and equipment.")]
        public static Task<string> GetPlayerStats()
        {
            return Tools.InformationTools.GetPlayerStats();
        }

        [McpServerTool(Name = "get_current_dungeon", Title = "Get Current Dungeon")]
        [Description("Gets information about the current dungeon (name, level, theme, rooms).")]
        public static Task<string> GetCurrentDungeon()
        {
            return Tools.InformationTools.GetCurrentDungeon();
        }

        [McpServerTool(Name = "get_inventory", Title = "Get Inventory")]
        [Description("Gets the player's inventory items.")]
        public static Task<string> GetInventory()
        {
            return Tools.InformationTools.GetInventory();
        }

        [McpServerTool(Name = "get_combat_state", Title = "Get Combat State")]
        [Description("Gets current combat information if in combat (current enemy, available actions, turn status).")]
        public static Task<string> GetCombatState()
        {
            return Tools.InformationTools.GetCombatState();
        }

        [McpServerTool(Name = "get_recent_output", Title = "Get Recent Output")]
        [Description("Gets recent game output/messages for AI context.")]
        public static Task<string> GetRecentOutput(
            [Description("Number of messages to retrieve (default: 10, max: 100)")] int count = 10)
        {
            return Tools.InformationTools.GetRecentOutput(count);
        }

        // ========== Dungeon Tools ==========

        [McpServerTool(Name = "get_available_dungeons", Title = "Get Available Dungeons")]
        [Description("Lists all available dungeons that can be entered.")]
        public static Task<string> GetAvailableDungeons()
        {
            return Tools.DungeonTools.GetAvailableDungeons();
        }
    }
}

