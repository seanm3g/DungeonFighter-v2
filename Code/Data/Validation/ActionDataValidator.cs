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
            // Use ActionLoader so we validate the same data the game uses (handles both
            // legacy ActionData JSON and spreadsheet format with "action" property).
            ActionLoader.LoadActions();
            return ActionLoader.GetAllActionData() ?? new List<ActionData>();
        }

        private void ValidateAction(ActionData action, ValidationResult result)
        {
            var entityName = string.IsNullOrEmpty(action.Name) ? "<unnamed>" : action.Name;

            // Required fields
            if (string.IsNullOrEmpty(action.Name))
            {
                result.AddError(FileName, entityName, "name", "Action name is required");
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

            // Roll bonus threshold adjustments (Crit Miss, Hit, Combo, Crit)
            ValidateRange(result, entityName, "criticalMissThresholdAdjustment", action.CriticalMissThresholdAdjustment,
                ValidationRules.Actions.MinThresholdAdjustment, ValidationRules.Actions.MaxThresholdAdjustment);
            ValidateRange(result, entityName, "hitThresholdAdjustment", action.HitThresholdAdjustment,
                ValidationRules.Actions.MinThresholdAdjustment, ValidationRules.Actions.MaxThresholdAdjustment);
            ValidateRange(result, entityName, "comboThresholdAdjustment", action.ComboThresholdAdjustment,
                ValidationRules.Actions.MinThresholdAdjustment, ValidationRules.Actions.MaxThresholdAdjustment);
            ValidateRange(result, entityName, "criticalHitThresholdAdjustment", action.CriticalHitThresholdAdjustment,
                ValidationRules.Actions.MinThresholdAdjustment, ValidationRules.Actions.MaxThresholdAdjustment);
            ValidateRange(result, entityName, "enemyCriticalMissThresholdAdjustment", action.EnemyCriticalMissThresholdAdjustment,
                ValidationRules.Actions.MinThresholdAdjustment, ValidationRules.Actions.MaxThresholdAdjustment);
            ValidateRange(result, entityName, "enemyHitThresholdAdjustment", action.EnemyHitThresholdAdjustment,
                ValidationRules.Actions.MinThresholdAdjustment, ValidationRules.Actions.MaxThresholdAdjustment);
            ValidateRange(result, entityName, "enemyComboThresholdAdjustment", action.EnemyComboThresholdAdjustment,
                ValidationRules.Actions.MinThresholdAdjustment, ValidationRules.Actions.MaxThresholdAdjustment);
            ValidateRange(result, entityName, "enemyCriticalHitThresholdAdjustment", action.EnemyCriticalHitThresholdAdjustment,
                ValidationRules.Actions.MinThresholdAdjustment, ValidationRules.Actions.MaxThresholdAdjustment);

            // Multiple dice validation
            ValidateRange(result, entityName, "multipleDiceCount", action.MultipleDiceCount, 
                ValidationRules.Actions.MinMultipleDiceCount, ValidationRules.Actions.MaxMultipleDiceCount);

            if (!string.IsNullOrEmpty(action.MultipleDiceMode) && 
                !ValidationRules.Actions.ValidMultipleDiceModes.Contains(action.MultipleDiceMode))
            {
                result.AddError(FileName, entityName, "multipleDiceMode", 
                    $"Invalid multipleDiceMode '{action.MultipleDiceMode}'. Valid modes: {string.Join(", ", ValidationRules.Actions.ValidMultipleDiceModes)}");
            }

            // Stat bonus type validation (list and legacy single)
            if (action.StatBonuses != null)
            {
                for (int i = 0; i < action.StatBonuses.Count; i++)
                {
                    var entry = action.StatBonuses[i];
                    if (!string.IsNullOrEmpty(entry.Type) && 
                        !ValidationRules.Actions.ValidStatBonusTypes.Contains(entry.Type))
                    {
                        result.AddWarning(FileName, entityName, "statBonuses", 
                            $"Unknown stat bonus type '{entry.Type}' at index {i}. Valid types: {string.Join(", ", ValidationRules.Actions.ValidStatBonusTypes)}");
                    }
                }
            }
            if (!string.IsNullOrEmpty(action.StatBonusType) && 
                !ValidationRules.Actions.ValidStatBonusTypes.Contains(action.StatBonusType))
            {
                result.AddWarning(FileName, entityName, "statBonusType", 
                    $"Unknown stat bonus type '{action.StatBonusType}'. Valid types: {string.Join(", ", ValidationRules.Actions.ValidStatBonusTypes)}");
            }

            action.NormalizeChainPositionBonuses();
            if (action.ChainPositionBonuses != null && ChainPositionBonusApplier.IsModifyChainPositionEnabled(
                    new ComboRoutingProperties { ModifyBasedOnChainPosition = action.ModifyBasedOnChainPosition, ChainPositionBonuses = action.ChainPositionBonuses }))
            {
                for (int i = 0; i < action.ChainPositionBonuses.Count; i++)
                {
                    var e = action.ChainPositionBonuses[i];
                    if (!string.IsNullOrEmpty(e.ModifiesParam) &&
                        !ValidationRules.Actions.ValidChainPositionModifiesParam.Contains(e.ModifiesParam))
                    {
                        result.AddWarning(FileName, entityName, "chainPositionBonuses",
                            $"Unknown chainPositionBonuses[{i}].modifiesParam '{e.ModifiesParam}'. Valid: Accuracy, EnemyAccuracy, Damage, MultiHit (legacy: RollBonus, EnemyRollBonus).");
                    }
                    if (!string.IsNullOrEmpty(e.PositionBasis) &&
                        !ValidationRules.Actions.ValidChainPositionBasis.Contains(e.PositionBasis))
                    {
                        result.AddWarning(FileName, entityName, "chainPositionBonuses",
                            $"Unknown chainPositionBonuses[{i}].positionBasis '{e.PositionBasis}'. Valid: empty, ComboSlotIndex0, ComboSlotIndex1, AmpTier.");
                    }
                    if (!string.IsNullOrEmpty(e.ValueKind) &&
                        !ValidationRules.Actions.ValidChainPositionValueKind.Contains(e.ValueKind))
                    {
                        result.AddWarning(FileName, entityName, "chainPositionBonuses",
                            $"Unknown chainPositionBonuses[{i}].valueKind '{e.ValueKind}'. Valid: #, %.");
                    }
                    if (double.IsNaN(e.Value) || double.IsInfinity(e.Value))
                    {
                        result.AddError(FileName, entityName, "chainPositionBonuses",
                            $"chainPositionBonuses[{i}].value must be finite.");
                    }
                }
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

            // Threshold validation (qualifier, type, operator, value, valueKind # or %)
            if (action.Thresholds != null)
            {
                for (int i = 0; i < action.Thresholds.Count; i++)
                {
                    var entry = action.Thresholds[i];
                    bool hasOperator = !string.IsNullOrEmpty(entry.Operator);
                    bool isPercent = string.Equals(entry.ValueKind, "%", StringComparison.OrdinalIgnoreCase);
                    if (isPercent && (entry.Value < 0.0 || entry.Value > 100.0))
                        result.AddWarning(FileName, entityName, "thresholds", 
                            $"thresholds[{i}].value should be 0-100 when valueKind is %, got {entry.Value}");
                    else if (!hasOperator && !isPercent && (entry.Value < 0.0 || entry.Value > 1.0))
                        result.AddError(FileName, entityName, "thresholds", 
                            $"thresholds[{i}].value must be between 0.0 and 1.0 when no operator is set (legacy fraction), got {entry.Value}");
                    if (!string.IsNullOrEmpty(entry.Type) && !ValidationRules.Actions.ValidThresholdStatTypes.Contains(entry.Type))
                        result.AddWarning(FileName, entityName, "thresholds", 
                            $"thresholds[{i}].type '{entry.Type}' is not a known attribute. Valid: Health, Strength, Agility, Technique, Intelligence.");
                }
            }
            if (action.Thresholds == null || action.Thresholds.Count == 0)
            {
                if (action.HealthThreshold < 0.0 || action.HealthThreshold > 1.0)
                    result.AddError(FileName, entityName, "healthThreshold", 
                        $"healthThreshold must be between 0.0 and 1.0 (percentage), got {action.HealthThreshold}");
            }

            if (action.Accumulations != null)
            {
                for (int i = 0; i < action.Accumulations.Count; i++)
                {
                    var entry = action.Accumulations[i];
                    if (!string.IsNullOrEmpty(entry.Type) && !ValidationRules.Actions.ValidAccumulationTypes.Contains(entry.Type))
                        result.AddWarning(FileName, entityName, "accumulations",
                            $"Unknown accumulation type '{entry.Type}' at index {i}. Valid: {string.Join(", ", ValidationRules.Actions.ValidAccumulationTypes)}");
                    if (!string.IsNullOrEmpty(entry.ModifiesParam) && !ValidationRules.Actions.ValidAccumulationModifiesParam.Contains(entry.ModifiesParam))
                        result.AddWarning(FileName, entityName, "accumulations",
                            $"Unknown accumulation modifies param '{entry.ModifiesParam}' at index {i}. Valid: {string.Join(", ", ValidationRules.Actions.ValidAccumulationModifiesParam)}");
                }
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
