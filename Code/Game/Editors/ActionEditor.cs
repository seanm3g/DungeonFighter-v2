using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using RPGGame;
using RPGGame.Utils;

namespace RPGGame.Editors
{
    /// <summary>
    /// Editor for creating, editing, and deleting actions
    /// </summary>
    public class ActionEditor
    {
        private readonly string actionsFilePath;
        private List<ActionData> actions = new List<ActionData>();

        public ActionEditor()
        {
            // Find the Actions.json file
            var possiblePaths = GameConstants.GetPossibleGameDataFilePaths(GameConstants.ActionsJson);
            actionsFilePath = possiblePaths.FirstOrDefault(p => File.Exists(p)) ?? Path.Combine("GameData", "Actions.json");
            
            LoadActions();
        }

        /// <summary>
        /// Load actions from JSON file
        /// </summary>
        private void LoadActions()
        {
            try
            {
                if (File.Exists(actionsFilePath))
                {
                    string jsonContent = File.ReadAllText(actionsFilePath);
                    actions = JsonSerializer.Deserialize<List<ActionData>>(jsonContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<ActionData>();
                }
                else
                {
                    actions = new List<ActionData>();
                    DebugLogger.Log("ActionEditor", $"Actions.json not found at {actionsFilePath}, starting with empty list");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Log("ActionEditor", $"Error loading actions: {ex.Message}");
                actions = new List<ActionData>();
            }
        }

        /// <summary>
        /// Get all actions
        /// </summary>
        public List<ActionData> GetActions()
        {
            return actions;
        }

        /// <summary>
        /// Get an action by name
        /// </summary>
        public ActionData? GetAction(string name)
        {
            return actions.FirstOrDefault(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Create a new action
        /// </summary>
        public bool CreateAction(ActionData actionData)
        {
            try
            {
                if (actions.Any(a => a.Name.Equals(actionData.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    DebugLogger.Log("ActionEditor", $"Action '{actionData.Name}' already exists");
                    return false;
                }

                actions.Add(actionData);
                return SaveActions();
            }
            catch (Exception ex)
            {
                DebugLogger.Log("ActionEditor", $"Error creating action: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Update an existing action
        /// </summary>
        public bool UpdateAction(string originalName, ActionData updatedActionData)
        {
            try
            {
                var existingAction = actions.FirstOrDefault(a => a.Name.Equals(originalName, StringComparison.OrdinalIgnoreCase));
                if (existingAction == null)
                {
                    DebugLogger.Log("ActionEditor", $"Action '{originalName}' not found");
                    return false;
                }

                // If name changed, check for conflicts
                if (!originalName.Equals(updatedActionData.Name, StringComparison.OrdinalIgnoreCase))
                {
                    if (actions.Any(a => a.Name.Equals(updatedActionData.Name, StringComparison.OrdinalIgnoreCase) && !a.Name.Equals(originalName, StringComparison.OrdinalIgnoreCase)))
                    {
                        DebugLogger.Log("ActionEditor", $"Action name '{updatedActionData.Name}' already exists");
                        return false;
                    }
                }

                // Update the action
                int index = actions.IndexOf(existingAction);
                actions[index] = updatedActionData;
                return SaveActions();
            }
            catch (Exception ex)
            {
                DebugLogger.Log("ActionEditor", $"Error updating action: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Delete an action
        /// </summary>
        public bool DeleteAction(string name)
        {
            try
            {
                var action = actions.FirstOrDefault(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (action == null)
                {
                    DebugLogger.Log("ActionEditor", $"Action '{name}' not found");
                    return false;
                }

                actions.Remove(action);
                return SaveActions();
            }
            catch (Exception ex)
            {
                DebugLogger.Log("ActionEditor", $"Error deleting action: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Save actions to JSON file
        /// </summary>
        private bool SaveActions()
        {
            try
            {
                // Create backup
                if (File.Exists(actionsFilePath))
                {
                    string backupPath = actionsFilePath + ".backup";
                    File.Copy(actionsFilePath, backupPath, overwrite: true);
                }

                // Save to file
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                string jsonContent = JsonSerializer.Serialize(actions, options);
                File.WriteAllText(actionsFilePath, jsonContent);

                // Reload actions to ensure consistency
                ActionLoader.LoadActions();
                
                DebugLogger.Log("ActionEditor", $"Actions saved successfully to {actionsFilePath}");
                return true;
            }
            catch (Exception ex)
            {
                DebugLogger.Log("ActionEditor", $"Error saving actions: {ex.Message}");
                return false;
            }
        }
    }
}

