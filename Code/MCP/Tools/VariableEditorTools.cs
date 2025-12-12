using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using RPGGame.Editors;
using RPGGame.MCP.Tools;

namespace RPGGame.MCP.Tools
{
    /// <summary>
    /// Variable editor tools
    /// </summary>
    public static class VariableEditorTools
    {
        private static string GetCategoryForVariable(VariableEditor editor, string variableName)
        {
            foreach (var category in editor.GetCategories())
            {
                var variables = editor.GetVariablesByCategory(category);
                if (variables.Any(v => v.Name == variableName))
                {
                    return category;
                }
            }
            return "unknown";
        }

        [McpServerTool(Name = "list_variable_categories", Title = "List Variable Categories")]
        [Description("Lists all available variable categories in the VariableEditor system.")]
        public static Task<string> ListVariableCategories()
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var editor = McpToolState.GetVariableEditor();
                var categories = editor.GetCategories();

                return new
                {
                    categories = categories,
                    count = categories.Count
                };
            }, writeIndented: true);
        }

        [McpServerTool(Name = "list_variables", Title = "List Variables")]
        [Description("Lists all editable variables, optionally filtered by category.")]
        public static Task<string> ListVariables(
            [Description("Optional category name to filter variables")] string? category = null)
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var editor = McpToolState.GetVariableEditor();
                var variables = string.IsNullOrEmpty(category)
                    ? editor.GetVariables()
                    : editor.GetVariablesByCategory(category);

                var variableList = variables.Select(v => new
                {
                    name = v.Name,
                    description = v.Description,
                    value = v.GetValue(),
                    valueType = v.GetValueType().Name,
                    category = GetCategoryForVariable(editor, v.Name)
                }).ToList();

                return new
                {
                    category = category ?? "all",
                    variables = variableList,
                    count = variableList.Count
                };
            }, writeIndented: true);
        }

        [McpServerTool(Name = "get_variable", Title = "Get Variable Value")]
        [Description("Gets the current value of a specific variable by name.")]
        public static Task<string> GetVariable(
            [Description("Variable name (e.g., 'EnemySystem.GlobalMultipliers.HealthMultiplier')")] string variableName)
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var editor = McpToolState.GetVariableEditor();
                var variable = editor.GetVariable(variableName);

                if (variable == null)
                {
                    throw new InvalidOperationException($"Variable '{variableName}' not found");
                }

                return new
                {
                    name = variable.Name,
                    description = variable.Description,
                    value = variable.GetValue(),
                    valueType = variable.GetValueType().Name,
                    category = GetCategoryForVariable(editor, variable.Name)
                };
            }, writeIndented: true);
        }

        [McpServerTool(Name = "set_variable", Title = "Set Variable Value")]
        [Description("Sets the value of a specific variable. The value will be converted to the appropriate type (int, double, bool, string).")]
        public static Task<string> SetVariable(
            [Description("Variable name (e.g., 'EnemySystem.GlobalMultipliers.HealthMultiplier')")] string variableName,
            [Description("New value (will be converted to appropriate type)")] string value)
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var editor = McpToolState.GetVariableEditor();
                var variable = editor.GetVariable(variableName);

                if (variable == null)
                {
                    throw new InvalidOperationException($"Variable '{variableName}' not found");
                }

                var valueType = variable.GetValueType();
                object? convertedValue = null;
                string? error = null;

                try
                {
                    if (valueType == typeof(int))
                    {
                        if (int.TryParse(value, out int intVal))
                        {
                            convertedValue = intVal;
                        }
                        else
                        {
                            error = $"Invalid integer value: {value}";
                        }
                    }
                    else if (valueType == typeof(double))
                    {
                        if (double.TryParse(value, out double doubleVal))
                        {
                            convertedValue = doubleVal;
                        }
                        else
                        {
                            error = $"Invalid number value: {value}";
                        }
                    }
                    else if (valueType == typeof(bool))
                    {
                        string trimmed = value.Trim().ToLower();
                        if (bool.TryParse(trimmed, out bool boolVal))
                        {
                            convertedValue = boolVal;
                        }
                        else if (trimmed == "1" || trimmed == "true" || trimmed == "t")
                        {
                            convertedValue = true;
                        }
                        else if (trimmed == "0" || trimmed == "false" || trimmed == "f")
                        {
                            convertedValue = false;
                        }
                        else
                        {
                            error = $"Invalid boolean value: {value}. Use true/false or 1/0";
                        }
                    }
                    else
                    {
                        // String or other types
                        convertedValue = value;
                    }
                }
                catch (Exception ex)
                {
                    error = $"Error converting value: {ex.Message}";
                }

                if (error != null)
                {
                    throw new InvalidOperationException(error);
                }

                var oldValue = variable.GetValue();
                variable.SetValue(convertedValue!);
                var newValue = variable.GetValue();

                return new
                {
                    success = true,
                    name = variable.Name,
                    oldValue = oldValue,
                    newValue = newValue,
                    valueType = valueType.Name,
                    message = $"Set {variable.Name} from {oldValue} to {newValue}"
                };
            }, writeIndented: true);
        }

        [McpServerTool(Name = "save_variable_changes", Title = "Save Variable Changes")]
        [Description("Saves all variable changes to TuningConfig.json and gamesettings.json files.")]
        public static Task<string> SaveVariableChanges()
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var editor = McpToolState.GetVariableEditor();
                var success = editor.SaveChanges();

                return new
                {
                    success = success,
                    message = success ? "Variable changes saved successfully to TuningConfig.json and gamesettings.json" : "Failed to save variable changes"
                };
            }, writeIndented: true);
        }

        [McpServerTool(Name = "get_variables_by_category", Title = "Get Variables by Category")]
        [Description("Gets all variables in a specific category.")]
        public static Task<string> GetVariablesByCategory(
            [Description("Category name (e.g., 'EnemySystem', 'PlayerAttributes', 'Combat')")] string category)
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var editor = McpToolState.GetVariableEditor();
                var variables = editor.GetVariablesByCategory(category);

                var variableList = variables.Select(v => new
                {
                    name = v.Name,
                    description = v.Description,
                    value = v.GetValue(),
                    valueType = v.GetValueType().Name
                }).ToList();

                return new
                {
                    category = category,
                    variables = variableList,
                    count = variableList.Count
                };
            }, writeIndented: true);
        }
    }
}
