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
                    Console.WriteLine($"Reading JSON from {foundPath}, content length: {jsonContent.Length}");
                    
                    var actionList = JsonSerializer.Deserialize<List<ActionData>>(jsonContent);
                    
                    _actions = new Dictionary<string, ActionData>();
                    if (actionList != null)
                    {
                        Console.WriteLine($"Deserialized {actionList.Count} actions from JSON");
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
                    Console.WriteLine($"Successfully loaded {_actions.Count} actions from {foundPath}");
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

            return new Action(
                name: data.Name,
                type: actionType,
                targetType: targetType,
                baseValue: data.BaseValue,
                range: data.Range,
                cooldown: data.Cooldown,
                description: data.Description,
                comboOrder: -1,
                damageMultiplier: data.DamageMultiplier,
                length: data.Length,
                causesBleed: data.CausesBleed,
                causesWeaken: data.CausesWeaken,
                isComboAction: false,
                comboBonusAmount: data.ComboBonusAmount,
                comboBonusDuration: data.ComboBonusDuration
            );
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