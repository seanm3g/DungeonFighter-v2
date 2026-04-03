using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using RPGGame.Data;

namespace RPGGame
{
    public static class ActionLoader
    {
        private static Dictionary<string, ActionData>? _actions;
        private static bool _wasSpreadsheetFormat;
        private static List<Data.SpreadsheetActionJson>? _originalSpreadsheetActions;
        /// <summary>Path of the Actions.json file that was actually loaded. Used so editors save to the same file.</summary>
        private static string? _loadedActionsFilePath;

        public static void LoadActions()
        {
            LoadActions(validate: false);
        }

        /// <summary>
        /// Reloads actions from disk, clearing caches so updated Actions.json is used.
        /// Call when actions may have been modified (e.g., between dungeon runs).
        /// </summary>
        public static void ReloadActions()
        {
            var pathToClear = _loadedActionsFilePath ?? GameConstants.GetGameDataFilePath(GameConstants.ActionsJson);
            if (!string.IsNullOrEmpty(pathToClear))
            {
                JsonLoader.ClearCacheForFile(pathToClear);
                try { JsonLoader.ClearCacheForFile(Path.GetFullPath(pathToClear)); } catch { }
            }
            _actions = null;
            LoadActions();
        }

        /// <summary>
        /// Loads actions from JSON with optional validation
        /// Supports both legacy ActionData format and new spreadsheet-compatible format
        /// </summary>
        /// <param name="validate">If true, validates loaded actions and logs any issues</param>
        public static void LoadActions(bool validate)
        {
            ErrorHandler.TryExecute(() =>
            {
                // Prefer the path we already loaded from (so save then reload uses the same file)
                string? filePath = null;
                if (!string.IsNullOrEmpty(_loadedActionsFilePath) && File.Exists(_loadedActionsFilePath))
                {
                    filePath = _loadedActionsFilePath;
                }
                if (filePath == null)
                {
                    // Use single canonical path (same as GameSettings) so load and save always use the same file
                    filePath = GameConstants.GetGameDataFilePath(GameConstants.ActionsJson);
                }

                if (filePath == null || !File.Exists(filePath))
                {
                    ErrorHandler.LogWarning($"No actions JSON file found at canonical path. Path used: {filePath ?? "(null)"}", "ActionLoader");
                    _actions = new Dictionary<string, ActionData>();
                    _wasSpreadsheetFormat = false;
                    _originalSpreadsheetActions = null;
                    // Keep canonical path so first save creates the file there
                    try { _loadedActionsFilePath = filePath != null ? Path.GetFullPath(filePath) : null; } catch { _loadedActionsFilePath = filePath; }
                    return;
                }

                // Store normalized path so save/cache use the same canonical path
                try { _loadedActionsFilePath = Path.GetFullPath(filePath); } catch { _loadedActionsFilePath = filePath; }
                
                // Detect format and load accordingly (use normalized path so cache key matches save path)
                var actionList = LoadActionsFromFile(_loadedActionsFilePath);
                _actions = new Dictionary<string, ActionData>();
                
                if (actionList.Count > 0)
                {
                    foreach (var action in actionList)
                    {
                        action.NormalizeStatBonuses();
                        action.NormalizeThresholds();
                        action.NormalizeAccumulations();
                        action.NormalizeTags();
                        if (!string.IsNullOrEmpty(action.Name))
                        {
                            _actions[action.Name] = action;
                        }
                        else
                        {
                            ErrorHandler.LogWarning("Found action with null/empty name", "ActionLoader");
                        }
                    }
                    
                    DebugLogger.LogFormat("ActionLoader", "Loaded {0} actions from JSON", _actions.Count);
                }
                else
                {
                    ErrorHandler.LogWarning($"No actions loaded from JSON file: {filePath}", "ActionLoader");
                }

                // Optional validation
                if (validate)
                {
                    ValidateLoadedActions();
                }
            }, "ActionLoader.LoadActions", () => 
            {
                _actions = new Dictionary<string, ActionData>();
                _wasSpreadsheetFormat = false;
                _originalSpreadsheetActions = null;
                _loadedActionsFilePath = null;
                ErrorHandler.LogError(new Exception("Failed to load actions"), "ActionLoader.LoadActions", "Action loading failed, using empty dictionary");
            });
        }
        
        /// <summary>
        /// Detects JSON format and loads actions accordingly
        /// </summary>
        private static List<ActionData> LoadActionsFromFile(string filePath)
        {
            try
            {
                // Try to detect format by reading a sample
                string jsonContent = File.ReadAllText(filePath);
                
                // Check if it's spreadsheet format (has "action" property) or legacy format (has "name" property)
                // Spreadsheet format uses "action" while legacy uses "name"
                bool isSpreadsheetFormat = DetectSpreadsheetFormat(jsonContent);
                
                if (isSpreadsheetFormat)
                {
                    // Load as spreadsheet format and convert
                    DebugLogger.LogFormat("ActionLoader", "Detected spreadsheet-compatible JSON format");
                    var spreadsheetList = JsonLoader.LoadJson<List<Data.SpreadsheetActionJson>>(
                        filePath, 
                        useCache: true, 
                        fallbackValue: new List<Data.SpreadsheetActionJson>()
                    );
                    _wasSpreadsheetFormat = true;
                    _originalSpreadsheetActions = spreadsheetList;
                    return Data.SpreadsheetToActionDataConverter.ConvertList(spreadsheetList);
                }
                else
                {
                    // Load as legacy ActionData format
                    DebugLogger.LogFormat("ActionLoader", "Detected legacy ActionData JSON format");
                    _wasSpreadsheetFormat = false;
                    _originalSpreadsheetActions = null;
                    return JsonLoader.LoadJson<List<ActionData>>(
                        filePath, 
                        useCache: true, 
                        fallbackValue: new List<ActionData>()
                    );
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError(ex, "ActionLoader.LoadActionsFromFile", $"Error loading actions from {filePath}");
                return new List<ActionData>();
            }
        }
        
        /// <summary>
        /// Detects if JSON is in spreadsheet format by checking for "action" property
        /// </summary>
        private static bool DetectSpreadsheetFormat(string jsonContent)
        {
            if (string.IsNullOrWhiteSpace(jsonContent))
                return false;
            
            // Check if JSON contains "action" property (spreadsheet format) vs "name" property (legacy format)
            // Spreadsheet format will have "action" as a top-level property in the first object
            // We'll check if the first non-whitespace object has "action" instead of "name"
            try
            {
                // Quick check: if JSON contains "action" property and doesn't have "name" in the first object,
                // it's likely spreadsheet format
                // More reliable: try to parse first object and check properties
                using (var doc = JsonDocument.Parse(jsonContent))
                {
                    if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
                    {
                        var firstElement = doc.RootElement[0];
                        if (firstElement.ValueKind == JsonValueKind.Object)
                        {
                            // Check if it has "action" property (spreadsheet format)
                            if (firstElement.TryGetProperty("action", out _))
                            {
                                return true;
                            }
                            // Check if it has "name" property (legacy format)
                            if (firstElement.TryGetProperty("name", out _))
                            {
                                return false;
                            }
                        }
                    }
                }
            }
            catch
            {
                // If parsing fails, default to legacy format
                return false;
            }
            
            // Default to legacy format if detection fails
            return false;
        }

        /// <summary>
        /// Validates loaded actions and logs any issues
        /// </summary>
        private static void ValidateLoadedActions()
        {
            try
            {
                var validator = new Data.Validation.ActionDataValidator();
                var result = validator.Validate();
                
                if (!result.IsValid)
                {
                    ErrorHandler.LogWarning($"Action validation found {result.Errors.Count} errors and {result.Warnings.Count} warnings", "ActionLoader");
                    foreach (var error in result.Errors)
                    {
                        ErrorHandler.LogWarning($"Action validation error: {error.Message}", "ActionLoader");
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.LogWarning($"Action validation failed: {ex.Message}", "ActionLoader");
            }
        }

        public static Action? GetAction(string actionName)
        {
            if (_actions == null)
            {
                LoadActions();
            }

            if (_actions != null && _actions.TryGetValue(actionName, out var actionData))
            {
                return ActionDataToActionMapper.CreateAction(actionData);
            }

            return null;
        }

        public static List<Action> GetActions(params string[] actionNames)
        {
            var actions = new List<Action>();
            foreach (var name in actionNames)
            {
                var action = GetAction(name);
                if (action != null)
                {
                    actions.Add(action);
                }
            }
            return actions;
        }

        public static bool HasAction(string actionName)
        {
            if (_actions == null)
            {
                LoadActions();
            }

            return _actions?.ContainsKey(actionName) ?? false;
        }

        public static List<string> GetAllActionNames()
        {
            if (_actions == null)
            {
                LoadActions();
            }

            return _actions?.Keys.ToList() ?? new List<string>();
        }

        public static List<Action> GetAllActions()
        {
            if (_actions == null)
            {
                LoadActions();
            }
            var actions = new List<Action>();
            if (_actions != null)
            {
                foreach (var actionData in _actions.Values)
                {
                    actions.Add(ActionDataToActionMapper.CreateAction(actionData));
                }
            }
            return actions;
        }

        /// <summary>
        /// Gets all ActionData objects (for editing purposes)
        /// </summary>
        public static List<ActionData> GetAllActionData()
        {
            if (_actions == null)
            {
                LoadActions();
            }
            return _actions?.Values.ToList() ?? new List<ActionData>();
        }

        /// <summary>
        /// True if the last load detected spreadsheet-compatible JSON format (action/rarity/category/cadence).
        /// When true, save should write back as SpreadsheetActionJson to preserve columns.
        /// </summary>
        public static bool IsSpreadsheetFormat()
        {
            if (_actions == null)
            {
                LoadActions();
            }
            return _wasSpreadsheetFormat;
        }

        /// <summary>
        /// Path of the Actions.json file that was last successfully loaded.
        /// Use this path when saving so edits persist to the same file the game loads from.
        /// </summary>
        public static string? GetLoadedActionsFilePath()
        {
            return _loadedActionsFilePath;
        }

        /// <summary>
        /// Original spreadsheet JSON rows from the last load, or null if legacy format was used.
        /// Used when saving to merge edited ActionData back into spreadsheet format.
        /// </summary>
        public static List<Data.SpreadsheetActionJson>? GetOriginalSpreadsheetActions()
        {
            if (_actions == null)
            {
                LoadActions();
            }
            return _originalSpreadsheetActions;
        }

        public static string GetRandomActionNameByType(ActionType type)
        {
            if (_actions == null)
            {
                LoadActions();
            }

            var actionsOfType = new List<string>();
            if (_actions != null)
            {
                foreach (var actionData in _actions.Values)
                {
                    if (actionData.Type.Equals(type.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        actionsOfType.Add(actionData.Name);
                    }
                }
            }

            if (actionsOfType.Count > 0)
            {
                return actionsOfType[Random.Shared.Next(actionsOfType.Count)];
            }

            // Fallback names if no actions found
            return type switch
            {
                ActionType.Attack => "Attack",
                ActionType.Heal => "Heal",
                ActionType.Buff => "Buff",
                ActionType.Debuff => "Debuff",
                ActionType.Spell => "Spell",
                ActionType.Interact => "Interact",
                ActionType.Move => "Move",
                ActionType.UseItem => "Use Item",
                _ => "Unknown Action"
            };
        }
    }
} 