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
        /// <summary>Serializes access to <see cref="_actions"/> and <see cref="CreateAction"/> mapping (shared <see cref="ActionData"/> is mutated during map).</summary>
        private static readonly object ActionsLock = new object();
        private static Dictionary<string, ActionData>? _actions;
        /// <summary>Legacy or misspelled action names mapped to the loaded canonical key.</summary>
        private static Dictionary<string, string>? _actionNameAliases;
        private static bool _wasSpreadsheetFormat;
        private static List<Data.SpreadsheetActionJson>? _originalSpreadsheetActions;
        /// <summary>Path of the Actions.json file that was actually loaded. Used so editors save to the same file.</summary>
        private static string? _loadedActionsFilePath;

        public static void LoadActions()
        {
            LoadActions(validate: false);
        }

        /// <summary>
        /// Raised after <see cref="ReloadActions"/> completes so UI that mirrors ActionLoader (e.g. Settings Actions tab) can refresh.
        /// </summary>
        public static event System.Action? ActionsReloaded;

        /// <summary>
        /// Resolves the Actions.json path used for load, save, and spreadsheet pull.
        /// Prefers the file already loaded in this session, then any existing file on disk, then the canonical GameData path.
        /// </summary>
        public static string ResolveActionsFilePath()
        {
            lock (ActionsLock)
            {
                if (!string.IsNullOrEmpty(_loadedActionsFilePath))
                {
                    try
                    {
                        string full = Path.GetFullPath(_loadedActionsFilePath);
                        if (File.Exists(full))
                            return full;
                    }
                    catch
                    {
                        if (File.Exists(_loadedActionsFilePath))
                            return _loadedActionsFilePath;
                    }
                }
            }

            string? existing = GameConstants.TryGetExistingGameDataFilePath(GameConstants.ActionsJson);
            if (!string.IsNullOrEmpty(existing))
                return existing;

            return GameConstants.GetGameDataFilePath(GameConstants.ActionsJson);
        }

        /// <summary>
        /// Reloads actions from disk, clearing caches so updated Actions.json is used.
        /// Call when actions may have been modified (e.g., between dungeon runs or after a spreadsheet pull).
        /// </summary>
        /// <param name="explicitFilePath">When set (e.g. after an external pull wrote Actions.json), load from this path instead of the previously cached path.</param>
        public static void ReloadActions(string? explicitFilePath = null)
        {
            lock (ActionsLock)
            {
                if (!string.IsNullOrWhiteSpace(explicitFilePath))
                {
                    try { _loadedActionsFilePath = Path.GetFullPath(explicitFilePath); }
                    catch { _loadedActionsFilePath = explicitFilePath; }
                }

                ClearActionsJsonCache(_loadedActionsFilePath ?? GameConstants.GetGameDataFilePath(GameConstants.ActionsJson));
                _actions = null;
                _actionNameAliases = null;
                _wasSpreadsheetFormat = false;
                _originalSpreadsheetActions = null;
                LoadActions();
                ActionsReloaded?.Invoke();
            }
        }

        private static void ClearActionsJsonCache(string pathToClear)
        {
            if (string.IsNullOrEmpty(pathToClear))
                return;

            JsonLoader.ClearCacheForFile(pathToClear);
            try { JsonLoader.ClearCacheForFile(Path.GetFullPath(pathToClear)); } catch { }
        }

        /// <summary>
        /// Loads actions from JSON with optional validation
        /// Supports both legacy ActionData format and new spreadsheet-compatible format
        /// </summary>
        /// <param name="validate">If true, validates loaded actions and logs any issues</param>
        public static void LoadActions(bool validate)
        {
            lock (ActionsLock)
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
                    _actions = new Dictionary<string, ActionData>(StringComparer.OrdinalIgnoreCase);
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
                _actions = new Dictionary<string, ActionData>(StringComparer.OrdinalIgnoreCase);
                
                if (actionList.Count > 0)
                {
                    foreach (var action in actionList)
                    {
                        action.NormalizeStatBonuses();
                        action.NormalizeChainPositionBonuses();
                        action.NormalizeThresholds();
                        action.NormalizeAccumulations();
                        action.NormalizeTags();
                        if (!string.IsNullOrEmpty(action.Name))
                        {
                            if (_actions.ContainsKey(action.Name))
                            {
                                ErrorHandler.LogWarning(
                                    $"Duplicate action name '{action.Name}' in Actions.json; keeping the last row.",
                                    "ActionLoader");
                            }
                            _actions[action.Name] = action;
                        }
                        else
                        {
                            ErrorHandler.LogWarning("Found action with null/empty name", "ActionLoader");
                        }
                    }

                    RegisterActionNameAliases();
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
                _actions = new Dictionary<string, ActionData>(StringComparer.OrdinalIgnoreCase);
                _wasSpreadsheetFormat = false;
                _originalSpreadsheetActions = null;
                _loadedActionsFilePath = null;
                ErrorHandler.LogError(new Exception("Failed to load actions"), "ActionLoader.LoadActions", "Action loading failed, using empty dictionary");
            });
            }
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

        /// <summary>
        /// Maps a requested action name to the key present in loaded action data (handles legacy typos/aliases).
        /// </summary>
        public static string? ResolveActionName(string? actionName)
        {
            if (string.IsNullOrWhiteSpace(actionName))
                return actionName;

            lock (ActionsLock)
            {
                if (_actions == null)
                    LoadActions();

                return ResolveActionNameCore(actionName);
            }
        }

        /// <summary>Caller must hold <see cref="ActionsLock"/>.</summary>
        private static string? ResolveActionNameCore(string? actionName)
        {
            if (string.IsNullOrWhiteSpace(actionName))
                return actionName;

            if (_actions != null && _actions.ContainsKey(actionName))
                return actionName;

            if (_actionNameAliases != null)
            {
                if (_actionNameAliases.TryGetValue(actionName, out var canonical) &&
                    _actions != null &&
                    _actions.ContainsKey(canonical))
                    return canonical;

                // Canonical name requested (e.g. MAGIC MISSILE) but only the legacy key exists (MAGIC MISSLE).
                foreach (var kv in _actionNameAliases)
                {
                    if (string.Equals(kv.Value, actionName, StringComparison.OrdinalIgnoreCase) &&
                        _actions != null &&
                        _actions.ContainsKey(kv.Key))
                        return kv.Key;
                }
            }

            return actionName;
        }

        public static Action? GetAction(string actionName)
        {
            lock (ActionsLock)
            {
                if (_actions == null)
                    LoadActions();

                var resolvedName = ResolveActionNameCore(actionName);
                if (_actions != null && !string.IsNullOrEmpty(resolvedName) &&
                    _actions.TryGetValue(resolvedName, out var actionData) &&
                    ActionSetVisibility.IsIncluded(actionData))
                    return ActionDataToActionMapper.CreateAction(actionData);

                return null;
            }
        }

        /// <summary>
        /// Raw action definition from the last load (tags, weapon types, etc.) without building a runtime <see cref="Action"/>.
        /// Does not apply the active Action set filter (definitions remain available for editors/tag checks).
        /// </summary>
        public static ActionData? GetActionData(string actionName)
        {
            lock (ActionsLock)
            {
                if (_actions == null)
                    LoadActions();

                var resolvedName = ResolveActionNameCore(actionName);
                if (_actions != null && !string.IsNullOrEmpty(resolvedName) &&
                    _actions.TryGetValue(resolvedName, out var data))
                    return data;

                return null;
            }
        }

        private static void RegisterActionNameAliases()
        {
            _actionNameAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "MAGIC MISSLE", "MAGIC MISSILE" },
                { "SOUL DRAIN", "Soul Drain" },
            };

            if (_actions == null)
                return;

            // Keep aliases when either the canonical value or the legacy key exists in loaded data
            // (e.g. data key "MAGIC MISSLE" with canonical "MAGIC MISSILE" must stay for reverse lookup).
            var stale = _actionNameAliases
                .Where(kv => !_actions.ContainsKey(kv.Value) && !_actions.ContainsKey(kv.Key))
                .Select(kv => kv.Key)
                .ToList();
            foreach (var key in stale)
                _actionNameAliases.Remove(key);
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
            lock (ActionsLock)
            {
                if (_actions == null)
                    LoadActions();

                var resolvedName = ResolveActionNameCore(actionName);
                if (string.IsNullOrEmpty(resolvedName) || _actions == null)
                    return false;
                if (!_actions.TryGetValue(resolvedName, out var data))
                    return false;
                return ActionSetVisibility.IsIncluded(data);
            }
        }

        /// <summary>
        /// Action names available under the active Action set (gameplay / Action Lab catalog).
        /// </summary>
        public static List<string> GetAllActionNames()
        {
            lock (ActionsLock)
            {
                if (_actions == null)
                    LoadActions();

                if (_actions == null)
                    return new List<string>();

                return _actions
                    .Where(kv => ActionSetVisibility.IsIncluded(kv.Value))
                    .Select(kv => kv.Key)
                    .ToList();
            }
        }

        /// <summary>
        /// Every loaded action name (ignores the active Action set). Use for sheet/data validation.
        /// </summary>
        public static List<string> GetAllLoadedActionNames()
        {
            lock (ActionsLock)
            {
                if (_actions == null)
                    LoadActions();

                return _actions?.Keys.ToList() ?? new List<string>();
            }
        }

        /// <summary>
        /// Runtime actions available under the active Action set.
        /// </summary>
        public static List<Action> GetAllActions()
        {
            lock (ActionsLock)
            {
                if (_actions == null)
                    LoadActions();
                var actions = new List<Action>();
                if (_actions != null)
                {
                    foreach (var actionData in _actions.Values)
                    {
                        if (!ActionSetVisibility.IsIncluded(actionData))
                            continue;
                        actions.Add(ActionDataToActionMapper.CreateAction(actionData));
                    }
                }
                return actions;
            }
        }

        /// <summary>
        /// Gets all ActionData objects (for editing / validation). Includes tiers outside the active Action set.
        /// </summary>
        public static List<ActionData> GetAllActionData()
        {
            lock (ActionsLock)
            {
                if (_actions == null)
                    LoadActions();
                return _actions?.Values.ToList() ?? new List<ActionData>();
            }
        }

        /// <summary>
        /// ActionData rows included in the active Action set (gameplay, loot, defaults, environments).
        /// </summary>
        public static List<ActionData> GetActiveSetActionData()
        {
            lock (ActionsLock)
            {
                if (_actions == null)
                    LoadActions();
                if (_actions == null)
                    return new List<ActionData>();
                return _actions.Values.Where(ActionSetVisibility.IsIncluded).ToList();
            }
        }

        /// <summary>
        /// True if the last load detected spreadsheet-compatible JSON format (action/rarity/category/cadence).
        /// When true, save should write back as SpreadsheetActionJson to preserve columns.
        /// </summary>
        public static bool IsSpreadsheetFormat()
        {
            lock (ActionsLock)
            {
                if (_actions == null)
                    LoadActions();
                return _wasSpreadsheetFormat;
            }
        }

        /// <summary>
        /// Path of the Actions.json file that was last successfully loaded.
        /// Use this path when saving so edits persist to the same file the game loads from.
        /// </summary>
        public static string? GetLoadedActionsFilePath()
        {
            lock (ActionsLock)
                return _loadedActionsFilePath;
        }

        /// <summary>
        /// Original spreadsheet JSON rows from the last load, or null if legacy format was used.
        /// Used when saving to merge edited ActionData back into spreadsheet format.
        /// </summary>
        public static List<Data.SpreadsheetActionJson>? GetOriginalSpreadsheetActions()
        {
            lock (ActionsLock)
            {
                if (_actions == null)
                    LoadActions();
                return _originalSpreadsheetActions;
            }
        }

        public static string GetRandomActionNameByType(ActionType type)
        {
            lock (ActionsLock)
            {
                if (_actions == null)
                    LoadActions();

                var actionsOfType = new List<string>();
                if (_actions != null)
                {
                    foreach (var actionData in _actions.Values)
                    {
                        if (!ActionSetVisibility.IsIncluded(actionData))
                            continue;
                        if (actionData.Type.Equals(type.ToString(), StringComparison.OrdinalIgnoreCase))
                        {
                            actionsOfType.Add(actionData.Name);
                        }
                    }
                }

                if (actionsOfType.Count > 0)
                    return actionsOfType[Random.Shared.Next(actionsOfType.Count)];

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
} 