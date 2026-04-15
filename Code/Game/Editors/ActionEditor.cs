using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using RPGGame;
using RPGGame.Data;
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
            // Use canonical path (same as ActionLoader) so save and load always use the same file
            actionsFilePath = GameConstants.GetGameDataFilePath(GameConstants.ActionsJson);
            LoadActions();
        }

        /// <summary>
        /// Load actions from JSON file
        /// Supports both legacy ActionData format and SpreadsheetActionJson format
        /// Uses ActionLoader to handle format detection and conversion automatically
        /// </summary>
        private void LoadActions()
        {
            try
            {
                // Always use ActionLoader so we share the same path and format; editor saves to GetLoadedActionsFilePath().
                ActionLoader.LoadActions();
                actions = ActionLoader.GetAllActionData();
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError(ex, "ActionEditor.LoadActions", "Failed to load actions");
                actions = new List<ActionData>();
            }
        }

        /// <summary>
        /// Reloads in-memory actions from disk via <see cref="ActionLoader"/> (e.g. after Google Sheets resync).
        /// </summary>
        public void ReloadFromDisk()
        {
            LoadActions();
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
                    return false;
                }

                actions.Add(actionData);
                return SaveActions();
            }
            catch (Exception)
            {
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
                    return false;
                }

                // If name changed, check for conflicts
                if (!originalName.Equals(updatedActionData.Name, StringComparison.OrdinalIgnoreCase))
                {
                    if (actions.Any(a => a.Name.Equals(updatedActionData.Name, StringComparison.OrdinalIgnoreCase) && !a.Name.Equals(originalName, StringComparison.OrdinalIgnoreCase)))
                    {
                        return false;
                    }
                }

                // Update the action
                int index = actions.IndexOf(existingAction);
                actions[index] = updatedActionData;
                return SaveActions();
            }
            catch (Exception)
            {
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
                    return false;
                }

                actions.Remove(action);
                return SaveActions();
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Validate an action
        /// </summary>
        public string? ValidateAction(ActionData action, string? originalName = null)
        {
            if (string.IsNullOrWhiteSpace(action.Name))
            {
                return "Action name cannot be empty.";
            }

            // Check name uniqueness (only for new actions or if name changed)
            if (originalName == null || !originalName.Equals(action.Name, StringComparison.OrdinalIgnoreCase))
            {
                var existingAction = GetAction(action.Name);
                if (existingAction != null)
                {
                    return $"An action with the name '{action.Name}' already exists.";
                }
            }

            // Validate numeric ranges
            if (action.DamageMultiplier < 0)
            {
                return "Damage multiplier cannot be negative.";
            }

            if (action.Length < 0)
            {
                return "Length cannot be negative.";
            }

            if (action.Cooldown < 0)
            {
                return "Cooldown cannot be negative.";
            }

            return null; // Validation passed
        }

        /// <summary>
        /// Persists the current in-memory actions to the Actions.json file (same path ActionLoader uses).
        /// Call after flushing form edits into the editor's list so the global Settings Save can persist actions.
        /// </summary>
        public bool SaveActionsToFile()
        {
            return SaveActions();
        }

        /// <summary>
        /// Save actions to JSON file.
        /// Uses the same path ActionLoader loaded from so edits persist to the file the game uses.
        /// When the file was loaded as spreadsheet format, saves as SpreadsheetActionJson to preserve all columns.
        /// </summary>
        private bool SaveActions()
        {
            try
            {
                foreach (var a in actions)
                {
                    a.EnsureLegacyStatBonusFromList();
                    a.EnsureLegacyHealthThresholdFromList();
                }

                string pathToSave = ActionLoader.GetLoadedActionsFilePath() ?? actionsFilePath;
                try { pathToSave = Path.GetFullPath(pathToSave); } catch { }
                bool isSpreadsheet = ActionLoader.IsSpreadsheetFormat();
                DebugLogger.LogFormat("ActionEditor", "Saving Actions to: {0}", pathToSave);
                string? dir = Path.GetDirectoryName(pathToSave);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                // Create backup
                if (File.Exists(pathToSave))
                {
                    string backupPath = pathToSave + ".backup";
                    File.Copy(pathToSave, backupPath, overwrite: true);
                }

                if (isSpreadsheet)
                {
                    var originalRows = ActionLoader.GetOriginalSpreadsheetActions();
                    var spreadsheetList = ActionDataToSpreadsheetJsonConverter.ConvertList(actions, originalRows);
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        Converters = { new SpreadsheetActionJsonConverter() }
                    };
                    string jsonContent = JsonSerializer.Serialize(spreadsheetList, options);
                    File.WriteAllText(pathToSave, jsonContent);
                }
                else
                {
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };
                    string jsonContent = JsonSerializer.Serialize(actions, options);
                    File.WriteAllText(pathToSave, jsonContent);
                }

                // Clear JSON cache so LoadActions() reads fresh content from disk (clear both path forms in case cache key differs)
                JsonLoader.ClearCacheForFile(pathToSave);
                try { JsonLoader.ClearCacheForFile(Path.GetFullPath(pathToSave)); } catch { }
                // Reload actions to ensure consistency
                ActionLoader.LoadActions();
                var loadedPath = ActionLoader.GetLoadedActionsFilePath();
                var loaderCount = ActionLoader.GetAllActionData().Count;
                var didSync = loadedPath != null;
                if (didSync)
                    actions = new List<ActionData>(ActionLoader.GetAllActionData());
                return true;
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError(ex, "ActionEditor.SaveActions", "Failed to save actions to file");
                return false;
            }
        }
    }
}

