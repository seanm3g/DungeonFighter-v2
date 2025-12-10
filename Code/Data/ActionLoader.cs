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
        [JsonPropertyName("causesSlow")]
        public bool CausesSlow { get; set; }
        [JsonPropertyName("causesPoison")]
        public bool CausesPoison { get; set; }
        [JsonPropertyName("causesBurn")]
        public bool CausesBurn { get; set; }
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
    }

    public static class ActionLoader
    {
        private static Dictionary<string, ActionData>? _actions;
        private static readonly string[] PossibleActionPaths = GameConstants.GetPossibleGameDataFilePaths(GameConstants.ActionsJson);

        public static void LoadActions()
        {
            ErrorHandler.TryExecute(() =>
            {
                // Use the new JsonLoader for consistent loading
                var actionList = JsonLoader.LoadJsonFromPaths<List<ActionData>>(
                    PossibleActionPaths, 
                    useCache: true, 
                    fallbackValue: new List<ActionData>()
                );
                
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
                    
                }
                else
                {
                    ErrorHandler.LogWarning("No actions loaded from JSON files", "ActionLoader");
                }
            }, "ActionLoader.LoadActions", () => _actions = new Dictionary<string, ActionData>());
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
            
            // Set the new debuff properties
            action.CausesSlow = data.CausesSlow;
            action.CausesPoison = data.CausesPoison;
            
            // Set additional properties (using nested property structure)
            action.Advanced.RollBonus = data.RollBonus;
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
            
            if (data.CausesSlow)
            {
                modifiers.Add("Causes Slow");
            }
            
            if (data.CausesPoison)
            {
                modifiers.Add("Causes Poison");
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
            
            // Add stat bonus information
            if (data.StatBonus > 0 && !string.IsNullOrEmpty(data.StatBonusType))
            {
                string durationText = data.StatBonusDuration == -1 ? "dungeon" : $"{data.StatBonusDuration} turns";
                modifiers.Add($"+{data.StatBonus} {data.StatBonusType} ({durationText})");
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
                ActionType.Attack => GameConstants.BasicAttackName,
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