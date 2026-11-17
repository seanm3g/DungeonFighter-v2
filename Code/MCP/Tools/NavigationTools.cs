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
        [Description("Sends one input (a choice's Input from get_agent_context). Returns the new screen, its labeled choices, and NewEvents: the lines this input produced.")]
        public static async Task<string> HandleInput(
            [Description("The input to send, exactly as a choice's Input (e.g. '1', '0', 'l', 'hub:tab:actions', 'cpi:pool:2')")] string input)
        {
            return await McpToolExecutor.ExecuteAsync(async () =>
            {
                var wrapper = McpToolState.GameWrapper;
                if (wrapper?.Game == null)
                    throw new InvalidOperationException("No game running. Call start_new_game first.");

                return await wrapper.HandleInputForAgent(input);
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
