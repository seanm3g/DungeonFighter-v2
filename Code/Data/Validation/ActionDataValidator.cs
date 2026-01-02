using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Data.Validation
{
    /// <summary>
    /// Validates Action data from Actions.json
    /// </summary>
    public class ActionDataValidator : IDataValidator
    {
        private const string FileName = "Actions.json";

        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            var actions = ActionLoader.GetAllActions();

            if (actions == null || actions.Count == 0)
            {
                result.AddWarning(FileName, "Actions", "", "No actions loaded. Cannot validate.");
                return result;
            }

            result.IncrementStatistic("Total Actions");
            var actionDataList = GetActionDataList();

            foreach (var actionData in actionDataList)
            {
                ValidateAction(actionData, result);
            }

            return result;
        }

        private List<ActionData> GetActionDataList()
        {
            // We need to get the raw ActionData, not the Action objects
            // ActionLoader doesn't expose this directly, so we'll load it ourselves
            var filePath = JsonLoader.FindGameDataFile(FileName);
            if (filePath == null)
            {
                return new List<ActionData>();
            }

            return JsonLoader.LoadJsonList<ActionData>(filePath) ?? new List<ActionData>();
        }

        private void ValidateAction(ActionData action, ValidationResult result)
        {
            var entityName = string.IsNullOrEmpty(action.Name) ? "<unnamed>" : action.Name;

            // Required fields
            if (string.IsNullOrEmpty(action.Name))
            {
                result.AddError(FileName, entityName, "name", "Action name is required");
            }

            if (string.IsNullOrEmpty(action.Type))
            {
                result.AddError(FileName, entityName, "type", "Action type is required");
            }

            // Type validation
            if (!string.IsNullOrEmpty(action.Type) && 
                !ValidationRules.Actions.ValidTypes.Contains(action.Type))
            {
                result.AddError(FileName, entityName, "type", 
                    $"Invalid action type '{action.Type}'. Valid types: {string.Join(", ", ValidationRules.Actions.ValidTypes)}");
            }

            // Target type validation
            if (!string.IsNullOrEmpty(action.TargetType) && 
                !ValidationRules.Actions.ValidTargetTypes.Contains(action.TargetType))
            {
                result.AddWarning(FileName, entityName, "targetType", 
                    $"Unknown target type '{action.TargetType}'. Valid types: {string.Join(", ", ValidationRules.Actions.ValidTargetTypes)}");
            }

            // Range checks
            ValidateRange(result, entityName, "damageMultiplier", action.DamageMultiplier, 
                ValidationRules.Actions.MinDamageMultiplier, ValidationRules.Actions.MaxDamageMultiplier);
            
            ValidateRange(result, entityName, "length", action.Length, 
                ValidationRules.Actions.MinLength, ValidationRules.Actions.MaxLength);
            
            ValidateRange(result, entityName, "cooldown", action.Cooldown, 
                ValidationRules.Actions.MinCooldown, ValidationRules.Actions.MaxCooldown);
            
            ValidateRange(result, entityName, "rollBonus", action.RollBonus, 
                ValidationRules.Actions.MinRollBonus, ValidationRules.Actions.MaxRollBonus);
            
            ValidateRange(result, entityName, "multiHitCount", action.MultiHitCount, 
                ValidationRules.Actions.MinMultiHitCount, ValidationRules.Actions.MaxMultiHitCount);
            
            ValidateRange(result, entityName, "selfDamagePercent", action.SelfDamagePercent, 
                ValidationRules.Actions.MinSelfDamagePercent, ValidationRules.Actions.MaxSelfDamagePercent);

            // Threshold validations
            ValidateRange(result, entityName, "criticalMissThresholdOverride", action.CriticalMissThresholdOverride, 
                ValidationRules.Actions.MinThreshold, ValidationRules.Actions.MaxThreshold);
            
            ValidateRange(result, entityName, "criticalHitThresholdOverride", action.CriticalHitThresholdOverride, 
                ValidationRules.Actions.MinThreshold, ValidationRules.Actions.MaxThreshold);
            
            ValidateRange(result, entityName, "comboThresholdOverride", action.ComboThresholdOverride, 
                ValidationRules.Actions.MinThreshold, ValidationRules.Actions.MaxThreshold);
            
            ValidateRange(result, entityName, "hitThresholdOverride", action.HitThresholdOverride, 
                ValidationRules.Actions.MinThreshold, ValidationRules.Actions.MaxThreshold);

            // Multiple dice validation
            ValidateRange(result, entityName, "multipleDiceCount", action.MultipleDiceCount, 
                ValidationRules.Actions.MinMultipleDiceCount, ValidationRules.Actions.MaxMultipleDiceCount);

            if (!string.IsNullOrEmpty(action.MultipleDiceMode) && 
                !ValidationRules.Actions.ValidMultipleDiceModes.Contains(action.MultipleDiceMode))
            {
                result.AddError(FileName, entityName, "multipleDiceMode", 
                    $"Invalid multipleDiceMode '{action.MultipleDiceMode}'. Valid modes: {string.Join(", ", ValidationRules.Actions.ValidMultipleDiceModes)}");
            }

            // Stat bonus type validation
            if (!string.IsNullOrEmpty(action.StatBonusType) && 
                !ValidationRules.Actions.ValidStatBonusTypes.Contains(action.StatBonusType))
            {
                result.AddWarning(FileName, entityName, "statBonusType", 
                    $"Unknown stat bonus type '{action.StatBonusType}'. Valid types: {string.Join(", ", ValidationRules.Actions.ValidStatBonusTypes)}");
            }

            // Business rules
            if (action.IsComboAction && action.ComboOrder <= 0)
            {
                result.AddError(FileName, entityName, "comboOrder", 
                    "If isComboAction is true, comboOrder must be greater than 0");
            }

            if (action.MultiHitCount > 1 && action.DamageMultiplier <= 0)
            {
                result.AddWarning(FileName, entityName, "damageMultiplier", 
                    "Multi-hit actions should typically have a positive damage multiplier");
            }

            if (action.SelfDamagePercent > 0 && action.DamageMultiplier <= 0)
            {
                result.AddWarning(FileName, entityName, "damageMultiplier", 
                    "Actions with self-damage should typically have a positive damage multiplier");
            }

            // Health threshold validation (0.0 to 1.0 for percentage)
            if (action.HealthThreshold < 0.0 || action.HealthThreshold > 1.0)
            {
                result.AddError(FileName, entityName, "healthThreshold", 
                    $"healthThreshold must be between 0.0 and 1.0 (percentage), got {action.HealthThreshold}");
            }

            // Conditional damage multiplier validation
            if (action.ConditionalDamageMultiplier < 0.1 || action.ConditionalDamageMultiplier > 10.0)
            {
                result.AddWarning(FileName, entityName, "conditionalDamageMultiplier", 
                    $"conditionalDamageMultiplier {action.ConditionalDamageMultiplier} is outside typical range (0.1 to 10.0)");
            }
        }

        private void ValidateRange(ValidationResult result, string entityName, string fieldName, double value, double min, double max)
        {
            if (!ValidationRules.IsInRange(value, min, max))
            {
                result.AddError(FileName, entityName, fieldName, 
                    ValidationRules.FormatRangeError(fieldName, value, min, max));
            }
        }

        private void ValidateRange(ValidationResult result, string entityName, string fieldName, int value, int min, int max)
        {
            if (!ValidationRules.IsInRange(value, min, max))
            {
                result.AddError(FileName, entityName, fieldName, 
                    ValidationRules.FormatRangeError(fieldName, value, min, max));
            }
        }
    }
}
