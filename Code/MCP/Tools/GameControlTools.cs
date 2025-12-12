using System;
using System.ComponentModel;
using System.Text.Json;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using RPGGame.MCP.Tools;

namespace RPGGame.MCP.Tools
{
    /// <summary>
    /// Game control tools (start game, save game)
    /// </summary>
    public static class GameControlTools
    {
        [McpServerTool(Name = "start_new_game", Title = "Start New Game")]
        [Description("Starts a new game instance. Returns the initial game state.")]
        public static Task<string> StartNewGame()
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var wrapper = McpToolState.GameWrapper;
                if (wrapper == null)
                    throw new InvalidOperationException("Game wrapper not initialized");

                wrapper.InitializeGame();
                wrapper.ShowMainMenu();
                return wrapper.GetGameState();
            }, writeIndented: false);
        }

        [McpServerTool(Name = "save_game", Title = "Save Game")]
        [Description("Saves the current game state.")]
        public static Task<string> SaveGame()
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var wrapper = McpToolState.GameWrapper;
                if (wrapper == null)
                    throw new InvalidOperationException("Game wrapper not initialized");

                wrapper.SaveGame();
                return new { success = true, message = "Game saved successfully" };
            });
        }
    }
}
