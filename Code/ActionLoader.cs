using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

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
        [JsonPropertyName("baseValue")]
        public int BaseValue { get; set; }
        [JsonPropertyName("range")]
        public int Range { get; set; }
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
        [JsonPropertyName("statBonus")]
        public int StatBonus { get; set; }
        [JsonPropertyName("statBonusType")]
        public string StatBonusType { get; set; } = "";
        [JsonPropertyName("statBonusDuration")]
        public int StatBonusDuration { get; set; }
        [JsonPropertyName("multiHitCount")]
        public int MultiHitCount { get; set; }
        [JsonPropertyName("multiHitDamagePercent")]
        public double MultiHitDamagePercent { get; set; }
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
    }

    public static class ActionLoader
    {
        private static Dictionary<string, ActionData>? _actions;
        private static readonly string[] PossibleActionPaths = {
            Path.Combine("GameData", "Actions.json"),
            Path.Combine("..", "GameData", "Actions.json"),
            Path.Combine("..", "..", "GameData", "Actions.json"),
            Path.Combine("DF4 - CONSOLE", "GameData", "Actions.json"),
            Path.Combine("..", "DF4 - CONSOLE", "GameData", "Actions.json")
        };

        public static void LoadActions()
        {
            try
            {
                string? foundPath = null;
                foreach (string path in PossibleActionPaths)
                {
                    if (File.Exists(path))
                    {
                        foundPath = path;
                        break;
                    }
                }

                if (foundPath != null)
                {
                    string jsonContent = File.ReadAllText(foundPath);
                    
                    var actionList = JsonSerializer.Deserialize<List<ActionData>>(jsonContent);
                    
                    _actions = new Dictionary<string, ActionData>();
                    if (actionList != null)
                    {
                        foreach (var action in actionList)
                        {
                            if (!string.IsNullOrEmpty(action.Name))
                            {
                                _actions[action.Name] = action;
                            }
                            else
                            {
                                Console.WriteLine($"Warning: Found action with null/empty name");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Warning: JSON deserialization returned null");
                    }
                }
                else
                {
                    Console.WriteLine($"Warning: Actions file not found. Tried paths: {string.Join(", ", PossibleActionPaths)}");
                    _actions = new Dictionary<string, ActionData>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading actions: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                _actions = new Dictionary<string, ActionData>();
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
            string enhancedDescription = EnhanceActionDescription(data);

            var action = new Action(
                name: data.Name,
                type: actionType,
                targetType: targetType,
                baseValue: data.BaseValue,
                range: data.Range,
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
            
            // Set additional properties
            action.RollBonus = data.RollBonus;
            action.StatBonus = data.StatBonus;
            action.StatBonusType = data.StatBonusType;
            action.StatBonusDuration = data.StatBonusDuration;
            action.MultiHitCount = data.MultiHitCount;
            action.MultiHitDamagePercent = data.MultiHitDamagePercent;
            action.SelfDamagePercent = data.SelfDamagePercent;
            action.SkipNextTurn = data.SkipNextTurn;
            action.RepeatLastAction = data.RepeatLastAction;
            action.Tags = data.Tags ?? new List<string>();
            action.EnemyRollPenalty = data.EnemyRollPenalty;
            
            return action;
        }

        private static string EnhanceActionDescription(ActionData data)
        {
            var modifiers = new List<string>();
            
            // Add roll bonus information
            if (data.RollBonus != 0)
            {
                string rollText = data.RollBonus > 0 ? $"+{data.RollBonus}" : data.RollBonus.ToString();
                modifiers.Add($"Roll: {rollText}");
            }
            
            // Add damage multiplier information
            if (data.DamageMultiplier != 1.0)
            {
                modifiers.Add($"Damage: {data.DamageMultiplier:F1}x");
            }
            
            // Add combo bonus information
            if (data.ComboBonusAmount > 0 && data.ComboBonusDuration > 0)
            {
                modifiers.Add($"Combo: +{data.ComboBonusAmount} for {data.ComboBonusDuration} turns");
            }
            
            // Add status effect information
            if (data.CausesBleed)
            {
                modifiers.Add("Causes Bleed");
            }
            
            if (data.CausesWeaken)
            {
                modifiers.Add("Causes Weaken");
            }
            
            // Add multi-hit information
            if (data.MultiHitCount > 1)
            {
                modifiers.Add($"Multi-hit: {data.MultiHitCount} attacks");
            }
            
            // Add self-damage information
            if (data.SelfDamagePercent > 0)
            {
                modifiers.Add($"Self-damage: {data.SelfDamagePercent}%");
            }
            
            // Add special effects
            if (data.SkipNextTurn)
            {
                modifiers.Add("Skips next turn");
            }
            
            if (data.RepeatLastAction)
            {
                modifiers.Add("Repeats last action");
            }
            
            // Combine base description with modifiers
            string result = data.Description;
            if (modifiers.Count > 0)
            {
                result += $" | {string.Join(", ", modifiers)}";
            }
            
            return result;
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
            return actions;
        }
    }
} 