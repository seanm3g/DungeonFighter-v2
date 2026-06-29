using System;
using System.ComponentModel;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using RPGGame.MCP.Models;

namespace RPGGame.MCP.Tools
{
    /// <summary>
    /// Primary MCP tools for agent-driven gameplay (rich context + user directives).
    /// </summary>
    public static class AgentGameplayTools
    {
        [McpServerTool(Name = "get_agent_context", Title = "[Gameplay] Get Agent Context")]
        [Description("Returns labeled choices, hints, player brief, and recent events for the current screen. Prefer this over get_game_state during play.")]
        public static Task<string> GetAgentContext(
            [Description("Number of recent output lines to include (default 15, max 50)")] int recentEventCount = 15)
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var wrapper = McpToolState.GameWrapper;
                if (wrapper?.Game == null)
                    throw new InvalidOperationException("Game wrapper not initialized");

                int count = Math.Clamp(recentEventCount, 1, 50);
                return AgentContextBuilder.Build(wrapper.Game, wrapper.OutputCapture, count);
            }, writeIndented: true);
        }

        [McpServerTool(Name = "set_agent_directive", Title = "[Gameplay] Set Agent Directive")]
        [Description("Stores a session-scoped strategy string echoed in get_agent_context (e.g. equip upgrades, pick safe dungeons).")]
        public static Task<string> SetAgentDirective(
            [Description("Natural-language strategy for the agent to follow this session")] string directive)
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                McpToolState.AgentDirective = string.IsNullOrWhiteSpace(directive) ? null : directive.Trim();
                return new
                {
                    success = true,
                    userDirective = McpToolState.AgentDirective
                };
            });
        }
    }
}
