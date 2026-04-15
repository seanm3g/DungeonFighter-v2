using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using RPGGame;

namespace RPGGame.Data
{
    /// <summary>
    /// Converts spreadsheet-form action data (JSON or row DTO) to ActionData.
    /// Single entry point for CSV (SpreadsheetActionData) and JSON (SpreadsheetActionJson) load paths.
    /// </summary>
    public static class SpreadsheetToActionDataConverter
    {
        /// <summary>
        /// Converts a SpreadsheetActionJson to ActionData (JSON load path).
        /// </summary>
        public static ActionData Convert(SpreadsheetActionJson json)
        {
            if (json == null) return new ActionData();
            return Convert(json.ToSpreadsheetActionData());
        }

        /// <summary>
        /// Converts a list of SpreadsheetActionJson to ActionData (used by ActionLoader).
        /// </summary>
        public static List<ActionData> ConvertList(List<SpreadsheetActionJson> jsonList)
        {
            var list = new List<ActionData>();
            if (jsonList == null) return list;
            foreach (var json in jsonList)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(json.Action))
                    {
                        var actionData = Convert(json);
                        if (!string.IsNullOrEmpty(actionData.Name))
                            list.Add(actionData);
                    }
                }
                catch (Exception ex)
                {
                    ErrorHandler.LogWarning($"Error converting action {json.Action}: {ex.Message}", "SpreadsheetToActionDataConverter");
                }
            }
            return list;
        }

        /// <summary>
        /// Converts a SpreadsheetActionData to ActionData (CSV/row path).
        /// </summary>
        public static ActionData Convert(SpreadsheetActionData spreadsheet)
        {
            var actionData = new ActionData();
            
            // Basic properties
            actionData.Name = spreadsheet.Action;
            actionData.Description = spreadsheet.Description;
            actionData.Type = DetermineActionType(spreadsheet);
            actionData.TargetType = DetermineTargetType(spreadsheet);
            
            // Damage and speed
            actionData.DamageMultiplier = SpreadsheetActionData.ParseNumericValue(spreadsheet.Damage);
            if (actionData.DamageMultiplier == 0)
            {
                actionData.DamageMultiplier = 1.0; // Default
            }
            
            actionData.Length = SpreadsheetActionData.ParseNumericValue(spreadsheet.Speed);
            if (actionData.Length == 0)
            {
                actionData.Length = 1.0; // Default
            }
            
            // Multi-hit
            actionData.MultiHitCount = SpreadsheetActionData.ParseIntValue(spreadsheet.NumberOfHits);
            if (actionData.MultiHitCount == 0)
            {
                actionData.MultiHitCount = 1; // Default
            }
            
            // Status effects
            actionData.CausesStun = !string.IsNullOrWhiteSpace(spreadsheet.Stun) && spreadsheet.Stun != "0";
            actionData.CausesPoison = !string.IsNullOrWhiteSpace(spreadsheet.Poison) && spreadsheet.Poison != "0";
            actionData.CausesBurn = !string.IsNullOrWhiteSpace(spreadsheet.Burn) && spreadsheet.Burn != "0";
            actionData.CausesBleed = !string.IsNullOrWhiteSpace(spreadsheet.Bleed) && spreadsheet.Bleed != "0";
            actionData.CausesWeaken = !string.IsNullOrWhiteSpace(spreadsheet.Weaken) && spreadsheet.Weaken != "0";
            actionData.CausesSlow = !string.IsNullOrWhiteSpace(spreadsheet.Slow) && spreadsheet.Slow != "0";
            
            // Advanced status effects
            actionData.CausesVulnerability = !string.IsNullOrWhiteSpace(spreadsheet.Vulnerability) && spreadsheet.Vulnerability != "0";
            actionData.CausesHarden = !string.IsNullOrWhiteSpace(spreadsheet.Harden) && spreadsheet.Harden != "0";
            actionData.CausesExpose = !string.IsNullOrWhiteSpace(spreadsheet.Expose) && spreadsheet.Expose != "0";
            actionData.CausesSilence = !string.IsNullOrWhiteSpace(spreadsheet.Silence) && spreadsheet.Silence != "0";
            actionData.CausesPierce = !string.IsNullOrWhiteSpace(spreadsheet.Pierce) && spreadsheet.Pierce != "0";
            actionData.CausesStatDrain = !string.IsNullOrWhiteSpace(spreadsheet.StatDrain) && spreadsheet.StatDrain != "0";
            actionData.CausesFortify = !string.IsNullOrWhiteSpace(spreadsheet.Fortify) && spreadsheet.Fortify != "0";
            actionData.CausesFocus = !string.IsNullOrWhiteSpace(spreadsheet.Focus) && spreadsheet.Focus != "0";
            actionData.CausesCleanse = !string.IsNullOrWhiteSpace(spreadsheet.Cleanse) && spreadsheet.Cleanse != "0";
            actionData.CausesReflect = !string.IsNullOrWhiteSpace(spreadsheet.Reflect) && spreadsheet.Reflect != "0";
            
            // Self damage
            if (!string.IsNullOrWhiteSpace(spreadsheet.SelfDamage))
            {
                actionData.SelfDamagePercent = (int)Math.Round(SpreadsheetActionData.ParseNumericValue(spreadsheet.SelfDamage) * 100);
            }
            
            // Heal
            if (!string.IsNullOrWhiteSpace(spreadsheet.HeroHeal))
            {
                // This would need to be handled differently - heal actions are a different type
                // For now, we'll note it in the description or handle via tags
            }
            
            // Spreadsheet-origin fields (round-trip)
            actionData.Rarity = spreadsheet.Rarity ?? "";
            actionData.Category = spreadsheet.Category ?? "";
            actionData.Cadence = spreadsheet.Cadence ?? "";
            actionData.ComboBonusDuration = SpreadsheetActionData.ParseIntValue(spreadsheet.Duration);
            actionData.SpeedMod = spreadsheet.SpeedMod ?? "";
            actionData.DamageMod = spreadsheet.DamageMod ?? "";
            actionData.MultiHitMod = spreadsheet.MultiHitMod ?? "";
            actionData.AmpMod = spreadsheet.AmpMod ?? "";

            // Combo & position (round-trip)
            actionData.ChainPosition = spreadsheet.ChainPosition ?? "";
            actionData.ModifyBasedOnChainPosition = spreadsheet.ModifyBasedOnChainPosition ?? "";
            actionData.Jump = spreadsheet.Jump ?? "";
            actionData.ChainLength = spreadsheet.ChainLength ?? "";
            actionData.Reset = spreadsheet.Reset ?? "";
            actionData.ResetBlockerBuffer = spreadsheet.ResetBlockerBuffer ?? "";
            actionData.IsOpener = !string.IsNullOrWhiteSpace(spreadsheet.Opener) && spreadsheet.Opener != "0";
            actionData.IsFinisher = !string.IsNullOrWhiteSpace(spreadsheet.Finisher) && spreadsheet.Finisher != "0";

            // Combo properties: all actions are combo actions (show in action menu). Cadence only indicates bonus to next action; no cadence = no bonus.
            actionData.IsComboAction = true;
            actionData.ComboOrder = 0; // Default, can be set based on chain position

            // Default/starting action (round-trip from Actions settings form)
            actionData.IsDefaultAction = !string.IsNullOrWhiteSpace(spreadsheet.IsDefaultAction) && spreadsheet.IsDefaultAction != "0";
            actionData.IsStartingAction = actionData.IsDefaultAction;

            // Weapon types (round-trip from Actions settings "Assign to Weapon Types")
            actionData.WeaponTypes = new List<string>();
            if (!string.IsNullOrWhiteSpace(spreadsheet.WeaponTypes))
            {
                var parts = spreadsheet.WeaponTypes.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    var trimmed = part.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                        actionData.WeaponTypes.Add(trimmed);
                }
            }
            
            // Tags (keep in sync for backward compatibility; Category/Rarity also stored as first-class)
            actionData.Tags = new List<string>();
            if (!string.IsNullOrWhiteSpace(spreadsheet.Category))
            {
                actionData.Tags.Add(spreadsheet.Category.ToLower());
            }
            if (!string.IsNullOrWhiteSpace(spreadsheet.Rarity))
            {
                actionData.Tags.Add(spreadsheet.Rarity.ToLower());
            }
            if (!string.IsNullOrWhiteSpace(spreadsheet.Tags))
            {
                var tagList = spreadsheet.Tags
                    .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim().ToLower())
                    .Where(t => !string.IsNullOrEmpty(t));
                foreach (var tag in tagList)
                {
                    actionData.Tags.Add(tag);
                }
            }
            // No duplicate tags: deduplicate (category + rarity + tags string can repeat the same tag)
            actionData.Tags = actionData.Tags.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            
            // Trigger conditions (ONHIT, ONMISS, ONCOMBO, ONCRITICAL)
            actionData.TriggerConditions = new List<string>();
            if (!string.IsNullOrWhiteSpace(spreadsheet.TriggerConditions))
            {
                var parts = spreadsheet.TriggerConditions.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    var upper = part.Trim().ToUpper();
                    if (upper == "ONHIT" || upper == "ONMISS" || upper == "ONCOMBO" || upper == "ONCRITICAL" || upper == "ONCRIT")
                    {
                        actionData.TriggerConditions.Add(upper == "ONCRIT" ? "ONCRITICAL" : upper);
                    }
                }
            }
            
            // ACTION/ATTACK bonuses
            actionData.ActionAttackBonuses = ActionAttackKeywordProcessor.ProcessBonuses(spreadsheet);

            // Roll bonus fields (form edits these; round-trip from Hero / Enemy columns)
            actionData.RollBonus = SpreadsheetActionData.ParseIntValue(spreadsheet.HeroAccuracy);
            actionData.HitThresholdAdjustment = SpreadsheetActionData.ParseIntValue(spreadsheet.HeroHit);
            actionData.ComboThresholdAdjustment = SpreadsheetActionData.ParseIntValue(spreadsheet.HeroCombo);
            actionData.CriticalHitThresholdAdjustment = SpreadsheetActionData.ParseIntValue(spreadsheet.HeroCrit);
            actionData.CriticalMissThresholdAdjustment = SpreadsheetActionData.ParseIntValue(spreadsheet.HeroCritMiss);

            actionData.EnemyRollBonus = SpreadsheetActionData.ParseIntValue(spreadsheet.EnemyAccuracy);
            actionData.EnemyHitThresholdAdjustment = SpreadsheetActionData.ParseIntValue(spreadsheet.EnemyHit);
            actionData.EnemyComboThresholdAdjustment = SpreadsheetActionData.ParseIntValue(spreadsheet.EnemyCombo);
            actionData.EnemyCriticalHitThresholdAdjustment = SpreadsheetActionData.ParseIntValue(spreadsheet.EnemyCrit);
            actionData.EnemyCriticalMissThresholdAdjustment = SpreadsheetActionData.ParseIntValue(spreadsheet.EnemyCritMiss);

            // StatBonuses, Thresholds, Accumulations (JSON round-trip from spreadsheet)
            actionData.StatBonuses = DeserializeStatBonuses(spreadsheet.StatBonusesJson);
            actionData.Thresholds = DeserializeThresholds(spreadsheet.ThresholdsJson);
            actionData.Accumulations = DeserializeAccumulations(spreadsheet.AccumulationsJson);
            actionData.ChainPositionBonuses = DeserializeChainPositionBonuses(spreadsheet.ChainPositionBonusesJson);

            // Description is taken only from the stored field (spreadsheet.Description). Do not append
            // keyword text here: that caused descriptions to grow on every load (append on load, save,
            // then append again on next load). Updates should overwrite the field, not amend.

            // Modifiers
            // SpeedMod, DamageMod, MultiHitMod, AmpMod would need special handling
            // These might affect the action's properties directly rather than being bonuses
            
            return actionData;
        }
        
        /// <summary>
        /// Determines the action type from spreadsheet data
        /// </summary>
        private static string DetermineActionType(SpreadsheetActionData spreadsheet)
        {
            // Check if it's a heal action
            if (!string.IsNullOrWhiteSpace(spreadsheet.HeroHeal))
            {
                return "Heal";
            }
            
            // Default to Attack
            return "Attack";
        }
        
        /// <summary>
        /// Determines the target type from spreadsheet data
        /// </summary>
        private static string DetermineTargetType(SpreadsheetActionData spreadsheet)
        {
            if (!string.IsNullOrWhiteSpace(spreadsheet.Target))
            {
                string target = spreadsheet.Target.ToUpper();
                if (target == "SELF")
                    return "Self";
                if (target == "ENEMY")
                    return "SingleTarget";
            }
            
            // Default based on action type
            if (!string.IsNullOrWhiteSpace(spreadsheet.HeroHeal))
            {
                return "Self"; // Heal actions typically target self
            }
            
            return "SingleTarget"; // Default
        }

        private static List<StatBonusEntry> DeserializeStatBonuses(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<StatBonusEntry>();
            try
            {
                var list = JsonSerializer.Deserialize<List<StatBonusEntry>>(json);
                return list ?? new List<StatBonusEntry>();
            }
            catch { return new List<StatBonusEntry>(); }
        }

        private static List<ThresholdEntry> DeserializeThresholds(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<ThresholdEntry>();
            try
            {
                var list = JsonSerializer.Deserialize<List<ThresholdEntry>>(json);
                return list ?? new List<ThresholdEntry>();
            }
            catch { return new List<ThresholdEntry>(); }
        }

        private static List<AccumulationEntry> DeserializeAccumulations(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<AccumulationEntry>();
            try
            {
                var list = JsonSerializer.Deserialize<List<AccumulationEntry>>(json);
                return list ?? new List<AccumulationEntry>();
            }
            catch { return new List<AccumulationEntry>(); }
        }

        private static List<ChainPositionBonusEntry> DeserializeChainPositionBonuses(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<ChainPositionBonusEntry>();
            try
            {
                var list = JsonSerializer.Deserialize<List<ChainPositionBonusEntry>>(json);
                return list ?? new List<ChainPositionBonusEntry>();
            }
            catch { return new List<ChainPositionBonusEntry>(); }
        }
    }
}
