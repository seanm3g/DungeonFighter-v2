using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using RPGGame.MCP.Models;
using RPGGame;
using RPGGame.Config;
using RPGGame.Editors;
using System.Text;
using Tools = RPGGame.MCP.Tools;

namespace RPGGame.MCP
{
    /// <summary>
    /// MCP Tools for DungeonFighter game
    /// Each tool is registered with the MCP server using attributes
    /// Split into partial classes by category: GameControl, Simulation, Agents
    /// </summary>
    [McpServerToolType]
    public static partial class McpTools
    {
        /// <summary>
        /// Sets the game wrapper instance (called by MCP server)
        /// </summary>
        public static void SetGameWrapper(GameWrapper wrapper)
        {
            Tools.McpToolState.GameWrapper = wrapper;
        }

        /// <summary>
        /// Gets the variable editor (for backward compatibility)
        /// </summary>
        internal static Editors.VariableEditor GetVariableEditor()
        {
            return Tools.McpToolState.GetVariableEditor();
        }

    }
}

