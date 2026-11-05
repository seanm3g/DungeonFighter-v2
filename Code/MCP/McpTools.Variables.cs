using System;
using System.ComponentModel;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using Tools = RPGGame.MCP.Tools;

namespace RPGGame.MCP
{
    /// <summary>
    /// Variable Editor tools: read and edit single config values.
    /// (The single-fight win-rate simulation / analysis / adjustment / legacy patch tools were removed 2026-11-04;
    /// balance work uses get_balance_context and the start_* balance jobs in McpTools.BalanceSims.cs.)
    /// </summary>
    public static partial class McpTools
    {
        // ========== Variable Editor Tools ==========

        [McpServerTool(Name = "list_variable_categories", Title = "List Variable Categories")]
        [Description("Lists all available variable categories in the VariableEditor system.")]
        public static Task<string> ListVariableCategories()
        {
            return Tools.VariableEditorTools.ListVariableCategories();
        }

        [McpServerTool(Name = "list_variables", Title = "List Variables")]
        [Description("Lists all editable variables, optionally filtered by category.")]
        public static Task<string> ListVariables(
            [Description("Optional category name to filter variables")] string? category = null)
        {
            return Tools.VariableEditorTools.ListVariables(category);
        }

        [McpServerTool(Name = "get_variable", Title = "Get Variable Value")]
        [Description("Gets the current value of a specific variable by name.")]
        public static Task<string> GetVariable(
            [Description("Variable name (e.g., 'EnemySystem.GlobalMultipliers.HealthMultiplier')")] string variableName)
        {
            return Tools.VariableEditorTools.GetVariable(variableName);
        }

        [McpServerTool(Name = "set_variable", Title = "Set Variable Value")]
        [Description("Sets the value of a specific variable. The value will be converted to the appropriate type (int, double, bool, string).")]
        public static Task<string> SetVariable(
            [Description("Variable name (e.g., 'EnemySystem.GlobalMultipliers.HealthMultiplier')")] string variableName,
            [Description("New value (will be converted to appropriate type)")] string value)
        {
            return Tools.VariableEditorTools.SetVariable(variableName, value);
        }

        [McpServerTool(Name = "save_variable_changes", Title = "Save Variable Changes")]
        [Description("Blocked by default: would overwrite the active balance patch (PatchProfile.json) and gamesettings.json. Put balance changes in a new patch in GameData/Patches/Balance instead; DF_ALLOW_LEGACY_TUNING_WRITES=1 forces it.")]
        public static Task<string> SaveVariableChanges()
        {
            return Tools.VariableEditorTools.SaveVariableChanges();
        }

        [McpServerTool(Name = "get_variables_by_category", Title = "Get Variables by Category")]
        [Description("Gets all variables in a specific category.")]
        public static Task<string> GetVariablesByCategory(
            [Description("Category name (e.g., 'EnemySystem', 'PlayerAttributes', 'Combat')")] string category)
        {
            return Tools.VariableEditorTools.GetVariablesByCategory(category);
        }
    }
}

