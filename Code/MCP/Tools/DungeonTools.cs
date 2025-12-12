using System;
using System.ComponentModel;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using RPGGame.MCP.Tools;

namespace RPGGame.MCP.Tools
{
    /// <summary>
    /// Dungeon-related tools
    /// </summary>
    public static class DungeonTools
    {
        [McpServerTool(Name = "get_available_dungeons", Title = "Get Available Dungeons")]
        [Description("Lists all available dungeons that can be entered.")]
        public static Task<string> GetAvailableDungeons()
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var wrapper = McpToolState.GameWrapper;
                if (wrapper == null)
                    throw new InvalidOperationException("Game wrapper not initialized");

                // TODO: Get available dungeons from game state
                return new { message = "Available dungeons feature not yet implemented" };
            });
        }
    }
}
