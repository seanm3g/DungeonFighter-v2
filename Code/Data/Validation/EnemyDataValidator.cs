using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Data;

namespace RPGGame.Data.Validation
{
    /// <summary>
    /// Validates Enemy data from Enemies.json
    /// </summary>
    public class EnemyDataValidator : IDataValidator
    {
        private const string FileName = "Enemies.json";
        private HashSet<string>? _validActionNames;
        private HashSet<string>? _validRarityNames;

        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            
            // Load valid action names for reference validation
            _validActionNames = new HashSet<string>(
                ActionLoader.GetAllActionNames(), 
                StringComparer.OrdinalIgnoreCase);

            _validRarityNames = LoadValidRarityNames();

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

            // Base attributes / growth validation (optional)
            if (enemy.BaseAttributes != null)
            {
                ValidateNonNegative(result, entityName, "baseAttributes.strength", enemy.BaseAttributes.Strength);
                ValidateNonNegative(result, entityName, "baseAttributes.agility", enemy.BaseAttributes.Agility);
                ValidateNonNegative(result, entityName, "baseAttributes.technique", enemy.BaseAttributes.Technique);
                ValidateNonNegative(result, entityName, "baseAttributes.intelligence", enemy.BaseAttributes.Intelligence);
            }

            if (enemy.GrowthPerLevel != null)
            {
                ValidateNonNegative(result, entityName, "growthPerLevel.strength", enemy.GrowthPerLevel.Strength);
                ValidateNonNegative(result, entityName, "growthPerLevel.agility", enemy.GrowthPerLevel.Agility);
                ValidateNonNegative(result, entityName, "growthPerLevel.technique", enemy.GrowthPerLevel.Technique);
                ValidateNonNegative(result, entityName, "growthPerLevel.intelligence", enemy.GrowthPerLevel.Intelligence);
            }

            if (enemy.Tags != null)
            {
                foreach (var message in GameDataTagHelper.ValidateRegistryTags(RPGGame.World.Tags.TagEntityScope.Enemy, enemy.Tags))
                    result.AddWarning(FileName, entityName, "tags", message);
            }

            if (enemy.BaseHealth.HasValue)
                ValidateNonNegative(result, entityName, "baseHealth", enemy.BaseHealth.Value);
            if (enemy.HealthGrowthPerLevel.HasValue)
                ValidateNonNegative(result, entityName, "healthGrowthPerLevel", enemy.HealthGrowthPerLevel.Value);

            if (!string.IsNullOrWhiteSpace(enemy.Rarity)
                && _validRarityNames != null
                && !_validRarityNames.Contains(enemy.Rarity.Trim()))
            {
                result.AddWarning(FileName, entityName, "rarity",
                    $"Unknown rarity '{enemy.Rarity}'. Valid rarities: {string.Join(", ", _validRarityNames.OrderBy(r => r))}");
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
                    else if (!actionName.Any(char.IsLetterOrDigit))
                    {
                        result.AddError(FileName, entityName, "actions",
                            $"Action entry '{actionName}' has no letters or digits (check for stray JSON brackets or markup)");
                    }
                    else if (_validActionNames != null && !_validActionNames.Contains(actionName))
                    {
                        result.AddError(FileName, entityName, "actions", 
                            $"Action '{actionName}' does not exist in Actions.json");
                    }
                    else
                    {
                        var ad = ActionLoader.GetActionData(actionName);
                        if (ad != null && GameDataTagHelper.HasEnvironmentTag(ad.Tags))
                        {
                            result.AddError(FileName, entityName, "actions",
                                $"Action '{actionName}' is tagged environment and must not be assigned to an enemy");
                        }
                    }
                }
            }
        }

        private void ValidateNonNegative(ValidationResult result, string entityName, string fieldName, double value)
        {
            if (value < 0)
                result.AddError(FileName, entityName, fieldName, $"{fieldName} must be >= 0 (was {value})");
        }

        private void ValidateNonNegative(ValidationResult result, string entityName, string fieldName, double? value)
        {
            if (value.HasValue && value.Value < 0)
                result.AddError(FileName, entityName, fieldName, $"{fieldName} must be >= 0 (was {value.Value})");
        }

        private static HashSet<string> LoadValidRarityNames()
        {
            try
            {
                string? filePath = JsonLoader.FindGameDataFile("RarityTable.json");
                if (filePath == null || !System.IO.File.Exists(filePath))
                    return new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Common" };

                string json = System.IO.File.ReadAllText(filePath);
                var rows = System.Text.Json.JsonSerializer.Deserialize<List<RarityData>>(json);
                if (rows == null || rows.Count == 0)
                    return new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Common" };

                return new HashSet<string>(
                    rows.Select(r => r.Name?.Trim() ?? "").Where(n => n.Length > 0),
                    StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Common" };
            }
        }

    }
}
