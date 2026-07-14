using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.World.Tags;

namespace RPGGame.Data.Validation
{
    /// <summary>
    /// Validates Room data from Rooms.json
    /// </summary>
    public class RoomDataValidator : IDataValidator
    {
        private const string FileName = "Rooms.json";
        private HashSet<string>? _validActionNames;
        private HashSet<string>? _validEnemyNames;

        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            
            // Load valid action names for reference validation
            _validActionNames = new HashSet<string>(
                ActionLoader.GetAllLoadedActionNames(), 
                StringComparer.OrdinalIgnoreCase);

            _validEnemyNames = new HashSet<string>(
                EnemyLoader.GetAllEnemyTypes(),
                StringComparer.OrdinalIgnoreCase);

            var rooms = RoomLoader.GetAllRoomData();

            if (rooms == null || rooms.Count == 0)
            {
                result.AddWarning(FileName, "Rooms", "", "No rooms loaded. Cannot validate.");
                return result;
            }

            result.IncrementStatistic("Total Rooms");

            foreach (var room in rooms)
            {
                ValidateRoom(room, result);
            }

            return result;
        }

        private void ValidateRoom(RoomData room, ValidationResult result)
        {
            var entityName = string.IsNullOrEmpty(room.GetLocationKey()) ? "<unnamed>" : room.GetLocationKey();

            if (string.IsNullOrEmpty(room.GetLocationKey()))
            {
                result.AddError(FileName, entityName, "location", "Location is required");
            }

            var biome = room.GetBiomeForThemeMatch();
            if (!string.IsNullOrEmpty(biome) &&
                !ValidationRules.Dungeons.ValidThemes.Contains(biome))
            {
                result.AddWarning(FileName, entityName, "biome",
                    $"Unknown biome '{biome}'. Valid dungeon themes: {string.Join(", ", ValidationRules.Dungeons.ValidThemes)}");
            }

            // Action reference validation
            if (room.Actions != null && room.Actions.Count > 0)
            {
                foreach (var actionData in room.Actions)
                {
                    if (string.IsNullOrEmpty(actionData.Name))
                    {
                        result.AddWarning(FileName, entityName, "actions", "Empty action name in actions array");
                    }
                    else if (_validActionNames != null && !_validActionNames.Contains(actionData.Name))
                    {
                        result.AddError(FileName, entityName, "actions", 
                            $"Action '{actionData.Name}' does not exist in Actions.json");
                    }

                    // Weight validation (should be positive)
                    if (actionData.Weight < 0)
                    {
                        result.AddWarning(FileName, entityName, "actions", 
                            $"Action '{actionData.Name}' has negative weight ({actionData.Weight}). Weights should be positive.");
                    }
                }
            }

            if (room.Tags != null && room.Tags.Count > 0)
            {
                foreach (var message in GameDataTagHelper.ValidateRegistryTags(TagEntityScope.Environment, room.Tags))
                    result.AddWarning(FileName, entityName, "tags", message);
            }

            if (room.UnstableThresholdMod != 0 && room.UnstableThresholdMod is not (4 or -2 or 2 or 0))
            {
                result.AddWarning(FileName, entityName, "unstableThresholdMod",
                    $"Unstable threshold mod '{room.UnstableThresholdMod}' is non-standard (expected 4, -2, 2, or 0).");
            }

            if (room.Enemies != null && room.Enemies.Count > 0)
            {
                foreach (var enemyEntry in room.Enemies)
                {
                    if (string.IsNullOrEmpty(enemyEntry.Name))
                    {
                        result.AddWarning(FileName, entityName, "enemies", "Empty enemy name in enemies array");
                    }
                    else if (_validEnemyNames != null && !_validEnemyNames.Contains(enemyEntry.Name))
                    {
                        result.AddError(FileName, entityName, "enemies",
                            $"Enemy '{enemyEntry.Name}' does not exist in Enemies.json");
                    }

                    if (enemyEntry.Weight <= 0)
                    {
                        result.AddWarning(FileName, entityName, "enemies",
                            $"Enemy '{enemyEntry.Name}' has non-positive weight ({enemyEntry.Weight}). Weights should be positive.");
                    }
                }
            }
        }
    }
}
