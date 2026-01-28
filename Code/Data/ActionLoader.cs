using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using RPGGame.Data;

namespace RPGGame
{
    public class ActionData
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
        [JsonPropertyName("type")]
        public string Type { get; set; } = "";
        [JsonPropertyName("targetType")]
        public string TargetType { get; set; } = "";
        [JsonPropertyName("cooldown")]
        public int Cooldown { get; set; }
        [JsonPropertyName("description")]
        public string Description { get; set; } = "";
        [JsonPropertyName("damageMultiplier")]
        public double DamageMultiplier { get; set; }
        [JsonPropertyName("length")]
        public double Length { get; set; }
        [JsonPropertyName("causesBleed")]
        public bool CausesBleed { get; set; }
        [JsonPropertyName("causesWeaken")]
        public bool CausesWeaken { get; set; }
        [JsonPropertyName("causesSlow")]
        public bool CausesSlow { get; set; }
        [JsonPropertyName("causesPoison")]
        public bool CausesPoison { get; set; }
        [JsonPropertyName("causesBurn")]
        public bool CausesBurn { get; set; }
        [JsonPropertyName("causesStun")]
        public bool CausesStun { get; set; }
        [JsonPropertyName("causesVulnerability")]
        public bool CausesVulnerability { get; set; }
        [JsonPropertyName("causesHarden")]
        public bool CausesHarden { get; set; }
        [JsonPropertyName("causesExpose")]
        public bool CausesExpose { get; set; }
        [JsonPropertyName("causesSilence")]
        public bool CausesSilence { get; set; }
        [JsonPropertyName("causesPierce")]
        public bool CausesPierce { get; set; }
        [JsonPropertyName("causesStatDrain")]
        public bool CausesStatDrain { get; set; }
        [JsonPropertyName("causesFortify")]
        public bool CausesFortify { get; set; }
        [JsonPropertyName("causesFocus")]
        public bool CausesFocus { get; set; }
        [JsonPropertyName("causesCleanse")]
        public bool CausesCleanse { get; set; }
        [JsonPropertyName("causesReflect")]
        public bool CausesReflect { get; set; }
        [JsonPropertyName("comboBonusAmount")]
        public int ComboBonusAmount { get; set; }
        [JsonPropertyName("comboBonusDuration")]
        public int ComboBonusDuration { get; set; }
        [JsonPropertyName("comboOrder")]
        public int ComboOrder { get; set; }
        [JsonPropertyName("isComboAction")]
        public bool IsComboAction { get; set; }
        [JsonPropertyName("rollBonus")]
        public int RollBonus { get; set; }
        [JsonPropertyName("rollBonusDuration")]
        public int RollBonusDuration { get; set; }
        [JsonPropertyName("statBonus")]
        public int StatBonus { get; set; }
        [JsonPropertyName("statBonusType")]
        public string StatBonusType { get; set; } = "";
        [JsonPropertyName("statBonusDuration")]
        public int StatBonusDuration { get; set; }
        [JsonPropertyName("multiHitCount")]
        public int MultiHitCount { get; set; }
        [JsonPropertyName("selfDamagePercent")]
        public int SelfDamagePercent { get; set; }
        [JsonPropertyName("skipNextTurn")]
        public bool SkipNextTurn { get; set; }
        [JsonPropertyName("repeatLastAction")]
        public bool RepeatLastAction { get; set; }
        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new List<string>();
        [JsonPropertyName("enemyRollPenalty")]
        public int EnemyRollPenalty { get; set; }
        [JsonPropertyName("healthThreshold")]
        public double HealthThreshold { get; set; }
        [JsonPropertyName("conditionalDamageMultiplier")]
        public double ConditionalDamageMultiplier { get; set; } = 1.0;
        
        // Threshold overrides (absolute values)
        [JsonPropertyName("criticalMissThresholdOverride")]
        public int CriticalMissThresholdOverride { get; set; }
        [JsonPropertyName("criticalHitThresholdOverride")]
        public int CriticalHitThresholdOverride { get; set; }
        [JsonPropertyName("comboThresholdOverride")]
        public int ComboThresholdOverride { get; set; }
        [JsonPropertyName("hitThresholdOverride")]
        public int HitThresholdOverride { get; set; }
        
        // Threshold adjustments (adds to current/default)
        [JsonPropertyName("criticalMissThresholdAdjustment")]
        public int CriticalMissThresholdAdjustment { get; set; }
        [JsonPropertyName("criticalHitThresholdAdjustment")]
        public int CriticalHitThresholdAdjustment { get; set; }
        [JsonPropertyName("comboThresholdAdjustment")]
        public int ComboThresholdAdjustment { get; set; }
        [JsonPropertyName("hitThresholdAdjustment")]
        public int HitThresholdAdjustment { get; set; }
        
        // Whether to apply threshold adjustments to both source and target
        [JsonPropertyName("applyThresholdAdjustmentsToBoth")]
        public bool ApplyThresholdAdjustmentsToBoth { get; set; }
        
        // Roll modification properties
        [JsonPropertyName("multipleDiceCount")]
        public int MultipleDiceCount { get; set; } = 1;
        [JsonPropertyName("multipleDiceMode")]
        public string MultipleDiceMode { get; set; } = "Sum";
        
        // Starting action flag
        [JsonPropertyName("isStartingAction")]
        public bool IsStartingAction { get; set; }
        
        // ACTION/ATTACK keyword bonuses
        [JsonPropertyName("actionAttackBonuses")]
        public Data.ActionAttackBonuses? ActionAttackBonuses { get; set; }
    }

    public static class ActionLoader
    {
        private static Dictionary<string, ActionData>? _actions;
        private static readonly string[] PossibleActionPaths = GameConstants.GetPossibleGameDataFilePaths(GameConstants.ActionsJson);

        public static void LoadActions()
        {
            LoadActions(validate: false);
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
                // Find the first existing file
                string? filePath = null;
                foreach (var path in PossibleActionPaths)
                {
                    if (File.Exists(path))
                    {
                        filePath = path;
                        break;
                    }
                }
                
                if (filePath == null)
                {
                    ErrorHandler.LogWarning($"No actions JSON file found. Searched paths: {string.Join(", ", PossibleActionPaths)}", "ActionLoader");
                    _actions = new Dictionary<string, ActionData>();
                    return;
                }
                
                // Detect format and load accordingly
                var actionList = LoadActionsFromFile(filePath);
                
                _actions = new Dictionary<string, ActionData>();
                
                if (actionList.Count > 0)
                {
                    foreach (var action in actionList)
                    {
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
                    
                    // Convert to ActionData
                    return Data.SpreadsheetJsonToActionDataConverter.ConvertList(spreadsheetList);
                }
                else
                {
                    // Load as legacy ActionData format
                    DebugLogger.LogFormat("ActionLoader", "Detected legacy ActionData JSON format");
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
                return CreateActionFromData(actionData);
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

        private static Action CreateActionFromData(ActionData data)
        {
            var actionType = ParseActionType(data.Type);
            var targetType = ParseTargetType(data.TargetType);

            // Enhance description with modifiers
            string enhancedDescription = ActionDescriptionEnhancer.EnhanceActionDescription(data);

            var action = new Action(
                name: data.Name,
                type: actionType,
                targetType: targetType,
                cooldown: data.Cooldown,
                description: enhancedDescription,
                comboOrder: data.ComboOrder,
                damageMultiplier: data.DamageMultiplier,
                length: data.Length,
                causesBleed: data.CausesBleed,
                causesWeaken: data.CausesWeaken,
                isComboAction: data.IsComboAction,
                comboBonusAmount: data.ComboBonusAmount,
                comboBonusDuration: data.ComboBonusDuration
            );
            
            // Set the new debuff properties
            action.CausesSlow = data.CausesSlow;
            action.CausesPoison = data.CausesPoison;
            action.CausesStun = data.CausesStun;
            
            // Set advanced status effects
            action.CausesVulnerability = data.CausesVulnerability;
            action.CausesHarden = data.CausesHarden;
            action.CausesExpose = data.CausesExpose;
            action.CausesSilence = data.CausesSilence;
            action.CausesPierce = data.CausesPierce;
            action.CausesStatDrain = data.CausesStatDrain;
            action.CausesFortify = data.CausesFortify;
            action.CausesFocus = data.CausesFocus;
            action.CausesCleanse = data.CausesCleanse;
            action.CausesReflect = data.CausesReflect;
            
            // Set additional properties (using nested property structure)
            action.Advanced.RollBonus = data.RollBonus;
            action.Advanced.RollBonusDuration = data.RollBonusDuration;
            action.Advanced.StatBonus = data.StatBonus;
            action.Advanced.StatBonusType = data.StatBonusType;
            action.Advanced.StatBonusDuration = data.StatBonusDuration;
            action.Advanced.MultiHitCount = data.MultiHitCount;
            action.Advanced.SelfDamagePercent = data.SelfDamagePercent;
            action.Advanced.SkipNextTurn = data.SkipNextTurn;
            action.Advanced.RepeatLastAction = data.RepeatLastAction;
            action.Tags = data.Tags ?? new List<string>();
            action.Advanced.EnemyRollPenalty = data.EnemyRollPenalty;
            action.Advanced.HealthThreshold = data.HealthThreshold;
            action.Advanced.ConditionalDamageMultiplier = data.ConditionalDamageMultiplier;
            
            // Set roll modification properties
            action.RollMods.MultipleDiceCount = data.MultipleDiceCount;
            action.RollMods.MultipleDiceMode = data.MultipleDiceMode;
            
            // Set threshold overrides
            action.RollMods.CriticalMissThresholdOverride = data.CriticalMissThresholdOverride;
            action.RollMods.CriticalHitThresholdOverride = data.CriticalHitThresholdOverride;
            action.RollMods.ComboThresholdOverride = data.ComboThresholdOverride;
            action.RollMods.HitThresholdOverride = data.HitThresholdOverride;
            
            // Set threshold adjustments
            action.RollMods.CriticalMissThresholdAdjustment = data.CriticalMissThresholdAdjustment;
            action.RollMods.CriticalHitThresholdAdjustment = data.CriticalHitThresholdAdjustment;
            action.RollMods.ComboThresholdAdjustment = data.ComboThresholdAdjustment;
            action.RollMods.HitThresholdAdjustment = data.HitThresholdAdjustment;
            
            // Set flag for applying to both actors
            action.RollMods.ApplyThresholdAdjustmentsToBoth = data.ApplyThresholdAdjustmentsToBoth;
            
            // Set ACTION/ATTACK bonuses
            action.ActionAttackBonuses = data.ActionAttackBonuses;
            
            return action;
        }


        private static ActionType ParseActionType(string type)
        {
            return type.ToLower() switch
            {
                "attack" => ActionType.Attack,
                "heal" => ActionType.Heal,
                "buff" => ActionType.Buff,
                "debuff" => ActionType.Debuff,
                "interact" => ActionType.Interact,
                "move" => ActionType.Move,
                "useitem" => ActionType.UseItem,
                "spell" => ActionType.Spell,
                _ => ActionType.Attack
            };
        }

        private static TargetType ParseTargetType(string targetType)
        {
            return targetType.ToLower() switch
            {
                "self" => TargetType.Self,
                "singletarget" => TargetType.SingleTarget,
                "areaofeffect" => TargetType.AreaOfEffect,
                "environment" => TargetType.Environment,
                "selfandtarget" => TargetType.SelfAndTarget,
                _ => TargetType.SingleTarget
            };
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
                    actions.Add(CreateActionFromData(actionData));
                }
            }
            else
            {
            }
            return actions;
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