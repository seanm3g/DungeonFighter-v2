using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Data.Validation
{
    /// <summary>
    /// Validates Enemy data from Enemies.json
    /// </summary>
    public class EnemyDataValidator : IDataValidator
    {
        private const string FileName = "Enemies.json";
        private HashSet<string>? _validActionNames;

        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            
            // Load valid action names for reference validation
            _validActionNames = new HashSet<string>(
                ActionLoader.GetAllActionNames(), 
                StringComparer.OrdinalIgnoreCase);

            var enemies = EnemyLoader.GetAllEnemyData();

            if (enemies == null || enemies.Count == 0)
            {
                result.AddWarning(FileName, "Enemies", "", "No enemies loaded. Cannot validate.");
                return result;
            }

            result.IncrementStatistic("Total Enemies");

            foreach (var enemy in enemies)
            {
                ValidateEnemy(enemy, result);
            }

            return result;
        }

        private void ValidateEnemy(EnemyData enemy, ValidationResult result)
        {
            var entityName = string.IsNullOrEmpty(enemy.Name) ? "<unnamed>" : enemy.Name;

            // Required fields
            if (string.IsNullOrEmpty(enemy.Name))
            {
                result.AddError(FileName, entityName, "name", "Enemy name is required");
            }

            if (string.IsNullOrEmpty(enemy.Archetype))
            {
                result.AddError(FileName, entityName, "archetype", "Enemy archetype is required");
            }

            // Archetype validation
            if (!string.IsNullOrEmpty(enemy.Archetype) && 
                !ValidationRules.Enemies.ValidArchetypes.Contains(enemy.Archetype))
            {
                result.AddWarning(FileName, entityName, "archetype", 
                    $"Unknown archetype '{enemy.Archetype}'. Valid archetypes: {string.Join(", ", ValidationRules.Enemies.ValidArchetypes)}");
            }

            // Stat override validation
            if (enemy.Overrides != null)
            {
                ValidateStatOverride(result, entityName, "health", enemy.Overrides.Health);
                ValidateStatOverride(result, entityName, "strength", enemy.Overrides.Strength);
                ValidateStatOverride(result, entityName, "agility", enemy.Overrides.Agility);
                ValidateStatOverride(result, entityName, "technique", enemy.Overrides.Technique);
                ValidateStatOverride(result, entityName, "intelligence", enemy.Overrides.Intelligence);
                ValidateStatOverride(result, entityName, "armor", enemy.Overrides.Armor);
            }

            // Action reference validation
            if (enemy.Actions != null && enemy.Actions.Count > 0)
            {
                foreach (var actionName in enemy.Actions)
                {
                    if (string.IsNullOrEmpty(actionName))
                    {
                        result.AddWarning(FileName, entityName, "actions", "Empty action name in actions array");
                    }
                    else if (_validActionNames != null && !_validActionNames.Contains(actionName))
                    {
                        result.AddError(FileName, entityName, "actions", 
                            $"Action '{actionName}' does not exist in Actions.json");
                    }
                }
            }
        }

        private void ValidateStatOverride(ValidationResult result, string entityName, string statName, double? value)
        {
            if (value.HasValue)
            {
                if (!ValidationRules.IsInRange(value.Value, ValidationRules.Enemies.MinStatOverride, ValidationRules.Enemies.MaxStatOverride))
                {
                    result.AddError(FileName, entityName, $"overrides.{statName}", 
                        ValidationRules.FormatRangeError($"{statName} override", value.Value, 
                            ValidationRules.Enemies.MinStatOverride, ValidationRules.Enemies.MaxStatOverride));
                }
            }
        }
    }
}
