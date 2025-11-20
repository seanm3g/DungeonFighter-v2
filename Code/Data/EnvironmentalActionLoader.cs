using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using RPGGame.Actions;
using Action = RPGGame.Action;

namespace RPGGame.Data
{
    /// <summary>
    /// Loads and manages environmental actions from JSON configuration.
    /// Eliminates the need for hardcoded switch statements in Environment.cs.
    /// </summary>
    public class EnvironmentalActionLoader
    {
        private List<EnvironmentalActionData>? cachedActions;
        private static readonly Random random = new Random();

        public class EnvironmentalActionData
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }

            [JsonPropertyName("name")]
            public string? Name { get; set; }

            [JsonPropertyName("theme")]
            public string? Theme { get; set; }

            [JsonPropertyName("roomType")]
            public string? RoomType { get; set; }

            [JsonPropertyName("type")]
            public string? Type { get; set; }

            [JsonPropertyName("damageMultiplier")]
            public double DamageMultiplier { get; set; } = 1.0;

            [JsonPropertyName("length")]
            public double Length { get; set; } = 2.0;

            [JsonPropertyName("description")]
            public string? Description { get; set; }

            [JsonPropertyName("causesStun")]
            public bool CausesStun { get; set; }

            [JsonPropertyName("causesBleed")]
            public bool CausesBleed { get; set; }

            [JsonPropertyName("causesWeaken")]
            public bool CausesWeaken { get; set; }

            [JsonPropertyName("causesSlow")]
            public bool CausesSlow { get; set; }

            [JsonPropertyName("causesPoison")]
            public bool CausesPoison { get; set; }
        }

        /// <summary>
        /// Loads all environmental actions from JSON file
        /// </summary>
        public List<EnvironmentalActionData> LoadAllActions()
        {
            if (cachedActions != null)
                return cachedActions;

            try
            {
                // Use JsonLoader to find and load the file
                cachedActions = JsonLoader.LoadJsonList<EnvironmentalActionData>("EnvironmentalActions.json");
                return cachedActions;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading environmental actions: {ex.Message}");
                return new List<EnvironmentalActionData>();
            }
        }

        /// <summary>
        /// Gets actions for a specific theme
        /// </summary>
        public List<Action> GetThemeActions(string theme)
        {
            var allActions = LoadAllActions();
            var themeActionData = allActions
                .Where(a => a.Theme?.Equals(theme, StringComparison.OrdinalIgnoreCase) == true)
                .ToList();

            return ConvertToActions(themeActionData);
        }

        /// <summary>
        /// Gets actions for a specific room type
        /// </summary>
        public List<Action> GetRoomTypeActions(string roomType)
        {
            var allActions = LoadAllActions();
            var roomActionData = allActions
                .Where(a => a.RoomType?.Equals(roomType, StringComparison.OrdinalIgnoreCase) == true)
                .ToList();

            return ConvertToActions(roomActionData);
        }

        /// <summary>
        /// Gets combined actions for both theme and room type
        /// Room-specific actions take priority
        /// </summary>
        public List<Action> GetCombinedActions(string theme, string roomType)
        {
            var roomActions = GetRoomTypeActions(roomType);
            var themeActions = GetThemeActions(theme);

            // Room-specific actions take priority
            var combinedNames = new HashSet<string>(
                roomActions.Select(a => a.Name)
            );

            var result = new List<Action>(roomActions);
            result.AddRange(themeActions.Where(a => !combinedNames.Contains(a.Name)));

            return result;
        }

        /// <summary>
        /// Converts EnvironmentalActionData to Action objects
        /// </summary>
        private List<Action> ConvertToActions(List<EnvironmentalActionData> actionData)
        {
            var actions = new List<Action>();

            foreach (var data in actionData)
            {
                var action = ConvertToAction(data);
                if (action != null)
                    actions.Add(action);
            }

            return actions;
        }

        /// <summary>
        /// Converts a single EnvironmentalActionData to an Action object
        /// </summary>
        private Action? ConvertToAction(EnvironmentalActionData data)
        {
            if (string.IsNullOrEmpty(data.Name))
                return null;

            try
            {
                var actionType = Enum.TryParse<ActionType>(data.Type, true, out var parsedType)
                    ? parsedType
                    : ActionType.Attack;

                var action = new Action(
                    name: data.Name,
                    type: actionType,
                    targetType: TargetType.AreaOfEffect, // Environmental actions are always area of effect
                    baseValue: 0,
                    range: 0,
                    cooldown: 0,
                    description: data.Description ?? "",
                    comboOrder: -1,
                    damageMultiplier: data.DamageMultiplier,
                    length: data.Length,
                    causesBleed: data.CausesBleed,
                    causesWeaken: data.CausesWeaken,
                    isComboAction: false,
                    comboBonusAmount: 0,
                    comboBonusDuration: 0
                );

                // Set additional effect flags
                action.CausesStun = data.CausesStun;
                action.CausesSlow = data.CausesSlow;
                action.CausesPoison = data.CausesPoison;

                return action;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting environmental action '{data.Name}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Clears the cached actions (useful for testing or reloading)
        /// </summary>
        public void ClearCache()
        {
            cachedActions = null;
        }
    }
}

