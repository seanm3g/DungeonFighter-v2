using System.ComponentModel;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using Tools = RPGGame.MCP.Tools;

namespace RPGGame.MCP
{
    /// <summary>
    /// Agent gameplay context tools (get_agent_context, set_agent_directive).
    /// </summary>
    public static partial class McpTools
    {
        [McpServerTool(Name = "get_agent_context", Title = "[Gameplay] Get Agent Context")]
        [Description("Returns labeled choices, hints, player brief, and recent events. Prefer over get_game_state during play.")]
        public static Task<string> GetAgentContext(
            [Description("Number of recent output lines (default 15, max 50)")] int recentEventCount = 15)
        {
            return Tools.AgentGameplayTools.GetAgentContext(recentEventCount);
        }

        [McpServerTool(Name = "set_agent_directive", Title = "[Gameplay] Set Agent Directive")]
        [Description("Stores session-scoped strategy echoed in get_agent_context.")]
        public static Task<string> SetAgentDirective(
            [Description("Natural-language strategy for the agent")] string directive)
        {
            return Tools.AgentGameplayTools.SetAgentDirective(directive);
        }
    }
}
