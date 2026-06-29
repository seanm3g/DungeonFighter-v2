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
                if (wrapper?.Game == null)
                    throw new InvalidOperationException("Game wrapper not initialized");

                var dungeons = AgentContextBuilder.BuildDungeonList(wrapper.Game);
                return new
                {
                    count = dungeons.Count,
                    dungeons
                };
            }, writeIndented: true);
        }
    }
}
