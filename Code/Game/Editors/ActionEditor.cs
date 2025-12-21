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
                }
            }
            catch (Exception)
            {
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

            if (string.IsNullOrWhiteSpace(action.Type))
            {
                return "Action type cannot be empty.";
            }

            if (string.IsNullOrWhiteSpace(action.TargetType))
            {
                return "Target type cannot be empty.";
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

            // Validate type enum values
            var validTypes = new[] { "Attack", "Heal", "Buff", "Debuff", "Spell", "Interact", "Move", "UseItem" };
            if (!validTypes.Contains(action.Type, StringComparer.OrdinalIgnoreCase))
            {
                return $"Invalid action type '{action.Type}'. Must be one of: {string.Join(", ", validTypes)}";
            }

            var validTargetTypes = new[] { "Self", "SingleTarget", "AreaOfEffect", "Environment", "SelfAndTarget" };
            if (!validTargetTypes.Contains(action.TargetType, StringComparer.OrdinalIgnoreCase))
            {
                return $"Invalid target type '{action.TargetType}'. Must be one of: {string.Join(", ", validTargetTypes)}";
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
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}

