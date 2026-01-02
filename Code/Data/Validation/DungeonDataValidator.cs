using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Data.Validation
{
    /// <summary>
    /// Validates Dungeon data from Dungeons.json
    /// </summary>
    public class DungeonDataValidator : IDataValidator
    {
        private const string FileName = "Dungeons.json";
        private HashSet<string>? _validEnemyNames;

        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            
            // Load valid enemy names for reference validation
            _validEnemyNames = new HashSet<string>(
                EnemyLoader.GetAllEnemyTypes(), 
                StringComparer.OrdinalIgnoreCase);

            var dungeons = JsonLoader.LoadJsonList<DungeonData>(FileName);

            if (dungeons == null || dungeons.Count == 0)
            {
                result.AddWarning(FileName, "Dungeons", "", "No dungeons loaded. Cannot validate.");
                return result;
            }

            result.IncrementStatistic("Total Dungeons");

            foreach (var dungeon in dungeons)
            {
                ValidateDungeon(dungeon, result);
            }

            return result;
        }

        private void ValidateDungeon(DungeonData dungeon, ValidationResult result)
        {
            var entityName = string.IsNullOrEmpty(dungeon.name) ? "<unnamed>" : dungeon.name;

            // Required fields
            if (string.IsNullOrEmpty(dungeon.name))
            {
                result.AddError(FileName, entityName, "name", "Dungeon name is required");
            }

            if (string.IsNullOrEmpty(dungeon.theme))
            {
                result.AddError(FileName, entityName, "theme", "Dungeon theme is required");
            }

            // Theme validation
            if (!string.IsNullOrEmpty(dungeon.theme) && 
                !ValidationRules.Dungeons.ValidThemes.Contains(dungeon.theme))
            {
                result.AddWarning(FileName, entityName, "theme", 
                    $"Unknown theme '{dungeon.theme}'. Valid themes: {string.Join(", ", ValidationRules.Dungeons.ValidThemes)}");
            }

            // Level range checks
            if (!ValidationRules.IsInRange(dungeon.minLevel, ValidationRules.Dungeons.MinLevel, ValidationRules.Dungeons.MaxLevel))
            {
                result.AddError(FileName, entityName, "minLevel", 
                    ValidationRules.FormatRangeError("minLevel", dungeon.minLevel, 
                        ValidationRules.Dungeons.MinLevel, ValidationRules.Dungeons.MaxLevel));
            }

            if (!ValidationRules.IsInRange(dungeon.maxLevel, ValidationRules.Dungeons.MinLevel, ValidationRules.Dungeons.MaxLevel))
            {
                result.AddError(FileName, entityName, "maxLevel", 
                    ValidationRules.FormatRangeError("maxLevel", dungeon.maxLevel, 
                        ValidationRules.Dungeons.MinLevel, ValidationRules.Dungeons.MaxLevel));
            }

            // Business rule: minLevel <= maxLevel
            if (dungeon.minLevel > dungeon.maxLevel)
            {
                result.AddError(FileName, entityName, "minLevel/maxLevel", 
                    $"minLevel ({dungeon.minLevel}) must be less than or equal to maxLevel ({dungeon.maxLevel})");
            }

            // Enemy reference validation
            if (dungeon.possibleEnemies != null && dungeon.possibleEnemies.Count > 0)
            {
                foreach (var enemyName in dungeon.possibleEnemies)
                {
                    if (string.IsNullOrEmpty(enemyName))
                    {
                        result.AddWarning(FileName, entityName, "possibleEnemies", "Empty enemy name in possibleEnemies array");
                    }
                    else if (_validEnemyNames != null && !_validEnemyNames.Contains(enemyName))
                    {
                        result.AddError(FileName, entityName, "possibleEnemies", 
                            $"Enemy '{enemyName}' does not exist in Enemies.json");
                    }
                }
            }
        }
    }
}
