using System;
using System.ComponentModel;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using RPGGame.MCP.Tools;

namespace RPGGame.MCP.Tools
{
    /// <summary>
    /// Navigation tools (handle input, get available actions)
    /// </summary>
    public static class NavigationTools
    {
        [McpServerTool(Name = "handle_input", Title = "Handle Game Input")]
        [Description("Handles game input (menu selection, combat actions, etc.). Returns updated game state.")]
        public static async Task<string> HandleInput(
            [Description("The input to send to the game (e.g., '1', '2', 'attack')")] string input)
        {
            return await McpToolExecutor.ExecuteAsync(async () =>
            {
                var wrapper = McpToolState.GameWrapper;
                if (wrapper == null)
                    throw new InvalidOperationException("Game wrapper not initialized");

                return await wrapper.HandleInput(input);
            }, writeIndented: true);
        }

        [McpServerTool(Name = "get_available_actions", Title = "Get Available Actions")]
        [Description("Gets the list of available actions for the current game state.")]
        public static Task<string> GetAvailableActions()
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var wrapper = McpToolState.GameWrapper;
                if (wrapper == null)
                    throw new InvalidOperationException("Game wrapper not initialized");

                return wrapper.GetAvailableActions();
            });
        }
    }
}
