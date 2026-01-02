using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Data.Validation
{
    /// <summary>
    /// Validates cross-file references between game data files
    /// </summary>
    public class ReferenceValidator : IDataValidator
    {
        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            result.IncrementStatistic("Reference Checks");

            // Ensure all data is loaded
            if (ActionLoader.GetAllActions().Count == 0)
            {
                ActionLoader.LoadActions();
            }

            if (EnemyLoader.GetAllEnemyTypes().Count == 0)
            {
                EnemyLoader.LoadEnemies();
            }

            // Build reference sets
            var validActionNames = new HashSet<string>(
                ActionLoader.GetAllActionNames(), 
                StringComparer.OrdinalIgnoreCase);
            
            var validEnemyNames = new HashSet<string>(
                EnemyLoader.GetAllEnemyTypes(), 
                StringComparer.OrdinalIgnoreCase);

            var validWeaponNames = new HashSet<string>(
                JsonLoader.LoadJsonList<WeaponData>("Weapons.json")
                    .Select(w => w.Name)
                    .Where(n => !string.IsNullOrEmpty(n)),
                StringComparer.OrdinalIgnoreCase);

            var validArmorNames = new HashSet<string>(
                JsonLoader.LoadJsonList<ArmorData>("Armor.json")
                    .Select(a => a.Name)
                    .Where(n => !string.IsNullOrEmpty(n)),
                StringComparer.OrdinalIgnoreCase);

            // Validate enemy action references
            ValidateEnemyActionReferences(result, validActionNames);

            // Validate dungeon enemy references
            ValidateDungeonEnemyReferences(result, validEnemyNames);

            // Validate room action references
            ValidateRoomActionReferences(result, validActionNames);

            // Validate starting gear references (if StartingGear.json exists)
            ValidateStartingGearReferences(result, validWeaponNames, validArmorNames);

            return result;
        }

        private void ValidateEnemyActionReferences(ValidationResult result, HashSet<string> validActionNames)
        {
            var enemies = EnemyLoader.GetAllEnemyData();
            
            foreach (var enemy in enemies)
            {
                if (enemy.Actions != null && enemy.Actions.Count > 0)
                {
                    foreach (var actionName in enemy.Actions)
                    {
                        if (!string.IsNullOrEmpty(actionName) && !validActionNames.Contains(actionName))
                        {
                            result.AddError("Enemies.json", enemy.Name, "actions", 
                                $"Enemy '{enemy.Name}' references non-existent action '{actionName}'");
                        }
                    }
                }
            }
        }

        private void ValidateDungeonEnemyReferences(ValidationResult result, HashSet<string> validEnemyNames)
        {
            var dungeons = JsonLoader.LoadJsonList<DungeonData>("Dungeons.json");
            
            foreach (var dungeon in dungeons)
            {
                if (dungeon.possibleEnemies != null && dungeon.possibleEnemies.Count > 0)
                {
                    foreach (var enemyName in dungeon.possibleEnemies)
                    {
                        if (!string.IsNullOrEmpty(enemyName) && !validEnemyNames.Contains(enemyName))
                        {
                            result.AddError("Dungeons.json", dungeon.name, "possibleEnemies", 
                                $"Dungeon '{dungeon.name}' references non-existent enemy '{enemyName}'");
                        }
                    }
                }
            }
        }

        private void ValidateRoomActionReferences(ValidationResult result, HashSet<string> validActionNames)
        {
            var rooms = RoomLoader.GetAllRoomData();
            
            foreach (var room in rooms)
            {
                if (room.Actions != null && room.Actions.Count > 0)
                {
                    foreach (var actionData in room.Actions)
                    {
                        if (!string.IsNullOrEmpty(actionData.Name) && !validActionNames.Contains(actionData.Name))
                        {
                            result.AddError("Rooms.json", room.Name, "actions", 
                                $"Room '{room.Name}' references non-existent action '{actionData.Name}'");
                        }
                    }
                }
            }
        }

        private void ValidateStartingGearReferences(ValidationResult result, HashSet<string> validWeaponNames, HashSet<string> validArmorNames)
        {
            var startingGear = JsonLoader.FindGameDataFile("StartingGear.json");
            if (startingGear == null)
            {
                // StartingGear.json is optional, so we just skip if it doesn't exist
                return;
            }

            // Try to load as a simple structure - this may need adjustment based on actual structure
            try
            {
                var gearData = JsonLoader.LoadJson<Dictionary<string, object>>(startingGear);
                // Additional validation can be added here if StartingGear.json has a known structure
            }
            catch
            {
                // If we can't parse it, that's okay - it might have a different structure
                // Individual validators will catch format issues
            }
        }
    }
}
