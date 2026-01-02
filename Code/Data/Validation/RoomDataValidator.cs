using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Data.Validation
{
    /// <summary>
    /// Validates Room data from Rooms.json
    /// </summary>
    public class RoomDataValidator : IDataValidator
    {
        private const string FileName = "Rooms.json";
        private HashSet<string>? _validActionNames;

        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            
            // Load valid action names for reference validation
            _validActionNames = new HashSet<string>(
                ActionLoader.GetAllActionNames(), 
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
            var entityName = string.IsNullOrEmpty(room.Name) ? "<unnamed>" : room.Name;

            // Required fields
            if (string.IsNullOrEmpty(room.Name))
            {
                result.AddError(FileName, entityName, "name", "Room name is required");
            }

            // Theme validation (optional but should be valid if provided)
            if (!string.IsNullOrEmpty(room.Theme) && 
                !ValidationRules.Dungeons.ValidThemes.Contains(room.Theme))
            {
                result.AddWarning(FileName, entityName, "theme", 
                    $"Unknown theme '{room.Theme}'. Valid themes: {string.Join(", ", ValidationRules.Dungeons.ValidThemes)}");
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
        }
    }
}
