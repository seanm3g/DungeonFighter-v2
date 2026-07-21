using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using RPGGame;

namespace RPGGame.Data
{
    /// <summary>
    /// Converts ActionData back to SpreadsheetActionJson for saving.
    /// Merges edited ActionData with optional original SpreadsheetActionJson row to preserve columns not edited in the UI.
    /// </summary>
    public static class ActionDataToSpreadsheetJsonConverter
    {
        /// <summary>
        /// Merges ActionData into a SpreadsheetActionJson row. If baseRow is provided, copies it and overwrites with ActionData fields; otherwise creates a new row from ActionData only.
        /// </summary>
        public static SpreadsheetActionJson Merge(ActionData data, SpreadsheetActionJson? baseRow)
        {
            var row = baseRow != null ? Clone(baseRow) : new SpreadsheetActionJson();

            row.Action = data.Name ?? "";
            row.Description = data.Description ?? "";
            row.Rarity = data.Rarity ?? "";
            row.Category = data.Category ?? "";
            row.Tier = data.Tier;
            row.Cadence = data.Cadence ?? "";
            row.Duration = data.ComboBonusDuration > 0 ? data.ComboBonusDuration.ToString() : (baseRow?.Duration ?? "");

            // Prefer CADENCES triples from editor blocks; clear legacy compact cells when present
            var cadenceBlocks = ActionCadenceEditorSync.LoadBlocks(data);
            if (cadenceBlocks.Count > 0 && cadenceBlocks.Any(b => b.Mechanics.Count > 0))
            {
                var sheetData = new SpreadsheetActionData();
                ActionCadenceSheetColumns.ApplyBundlesToSpreadsheetRow(sheetData, cadenceBlocks);
                row.CadenceBundlesJson = sheetData.CadenceBundlesJson;
                row.Cadence = "";
                row.Duration = "";
                row.Mechanics = "";
            }
            else
            {
                row.CadenceBundlesJson = baseRow?.CadenceBundlesJson ?? "";
            }

            row.SpeedMod = data.SpeedMod ?? baseRow?.SpeedMod ?? "";
            row.DamageMod = data.DamageMod ?? baseRow?.DamageMod ?? "";
            row.MultiHitMod = data.MultiHitMod ?? baseRow?.MultiHitMod ?? "";
            row.AmpMod = data.AmpMod ?? baseRow?.AmpMod ?? "";
            row.EnemySpeedMod = data.EnemySpeedMod ?? baseRow?.EnemySpeedMod ?? "";
            row.EnemyDamageMod = data.EnemyDamageMod ?? baseRow?.EnemyDamageMod ?? "";
            row.EnemyMultiHitMod = data.EnemyMultiHitMod ?? baseRow?.EnemyMultiHitMod ?? "";
            row.EnemyAmpMod = data.EnemyAmpMod ?? baseRow?.EnemyAmpMod ?? "";
            row.WeaponSpeedMod = data.WeaponSpeedMod ?? baseRow?.WeaponSpeedMod ?? "";
            row.WeaponDamageMod = data.WeaponDamageMod ?? baseRow?.WeaponDamageMod ?? "";
            row.EnemyWeaponSpeedMod = data.EnemyWeaponSpeedMod ?? baseRow?.EnemyWeaponSpeedMod ?? "";
            row.EnemyWeaponDamageMod = data.EnemyWeaponDamageMod ?? baseRow?.EnemyWeaponDamageMod ?? "";
            row.ChainPosition = data.ChainPosition ?? baseRow?.ChainPosition ?? "";
            row.ModifyBasedOnChainPosition = data.ModifyBasedOnChainPosition ?? baseRow?.ModifyBasedOnChainPosition ?? "";
            row.Jump = data.Jump ?? baseRow?.Jump ?? "";
            row.JumpRelative = data.JumpRelative ?? baseRow?.JumpRelative ?? "";
            row.ChainLength = data.ChainLength ?? baseRow?.ChainLength ?? "";
            row.Reset = data.Reset ?? baseRow?.Reset ?? "";
            row.ResetBlockerBuffer = data.ResetBlockerBuffer ?? baseRow?.ResetBlockerBuffer ?? "";
            row.Opener = data.IsOpener ? "true" : (baseRow?.Opener ?? "");
            row.Finisher = data.IsFinisher ? "true" : (baseRow?.Finisher ?? "");
            row.ReservePool = data.IsReservePool ? "true" : (baseRow?.ReservePool ?? "");

            row.Damage = FormatDamage(data.DamageMultiplier);
            row.Speed = data.Length.ToString("F2");
            row.NumberOfHits = data.MultiHitCount <= 0 ? "1" : data.MultiHitCount.ToString();
            if (baseRow == null && data.DamageMultiplier > 0 && data.Length > 0)
            {
                int hits = data.MultiHitCount <= 0 ? 1 : data.MultiHitCount;
                double dps = (data.DamageMultiplier * hits) / data.Length;
                row.DPS = dps.ToString("F0") + "%";
            }

            // Always use edited Tags so clearing tags persists (no baseRow fallback when empty). No duplicate tags.
            var tagsList = data.Tags != null ? data.Tags.Distinct(StringComparer.OrdinalIgnoreCase).ToList() : new List<string>();
            row.Tags = tagsList.Count > 0 ? string.Join(", ", tagsList) : "";

            // Always use edited TriggerConditions so clearing them persists (no baseRow fallback when empty)
            row.TriggerConditions = data.TriggerConditions != null && data.TriggerConditions.Count > 0 ? string.Join(", ", data.TriggerConditions) : "";
            row.OnRollValue = data.ExactRollTriggerValue > 0 ? data.ExactRollTriggerValue.ToString() : "";
            if (data.RoomsClearedTriggerValue > 0)
                row.OnRoomsCleared = data.RoomsClearedTriggerValue.ToString();
            else if (data.TriggerConditions != null
                     && data.TriggerConditions.Exists(c => c.StartsWith("ONROOMSCLEARED", StringComparison.OrdinalIgnoreCase)))
                row.OnRoomsCleared = "true";
            else
                row.OnRoomsCleared = "";

            if (data.TriggerBundles != null && data.TriggerBundles.Count > 0)
            {
                var sheetData = new SpreadsheetActionData();
                ActionTriggerSheetColumns.ApplyBundlesToSpreadsheetRow(sheetData, data.TriggerBundles);
                row.TriggerBundlesJson = sheetData.TriggerBundlesJson;
                row.OnHit = sheetData.OnHit;
                row.OnMiss = sheetData.OnMiss;
                row.OnCrit = sheetData.OnCrit;
                row.OnKill = sheetData.OnKill;
                if (!string.IsNullOrEmpty(sheetData.OnRoomsCleared))
                    row.OnRoomsCleared = sheetData.OnRoomsCleared;
                if (!string.IsNullOrEmpty(sheetData.OnRollValue))
                    row.OnRollValue = sheetData.OnRollValue;
            }
            else
            {
                row.TriggerBundlesJson = baseRow?.TriggerBundlesJson ?? "";
            }

            // Roll bonuses: ActionData is source of truth (zero → "" so save does not resurrect baseRow values)
            row.HeroAccuracy = data.RollBonus != 0 ? data.RollBonus.ToString() : "";
            row.HeroHit = data.HitThresholdAdjustment != 0 ? data.HitThresholdAdjustment.ToString() : "";
            row.HeroCombo = data.ComboThresholdAdjustment != 0 ? data.ComboThresholdAdjustment.ToString() : "";
            row.HeroCrit = data.CriticalHitThresholdAdjustment != 0 ? data.CriticalHitThresholdAdjustment.ToString() : "";
            row.HeroCritMiss = data.CriticalMissThresholdAdjustment != 0 ? data.CriticalMissThresholdAdjustment.ToString() : "";

            row.EnemyAccuracy = data.EnemyRollBonus != 0 ? data.EnemyRollBonus.ToString() : "";
            row.EnemyHit = data.EnemyHitThresholdAdjustment != 0 ? data.EnemyHitThresholdAdjustment.ToString() : "";
            row.EnemyCombo = data.EnemyComboThresholdAdjustment != 0 ? data.EnemyComboThresholdAdjustment.ToString() : "";
            row.EnemyCrit = data.EnemyCriticalHitThresholdAdjustment != 0 ? data.EnemyCriticalHitThresholdAdjustment.ToString() : "";
            row.EnemyCritMiss = data.EnemyCriticalMissThresholdAdjustment != 0 ? data.EnemyCriticalMissThresholdAdjustment.ToString() : "";

            // Stat bonuses from ActionAttackBonuses (HeroSTR, HeroAGI, HeroTECH, HeroINT round-trip)
            if (data.ActionAttackBonuses?.BonusGroups != null)
            {
                foreach (var group in data.ActionAttackBonuses.BonusGroups)
                {
                    if (group.Bonuses == null) continue;
                    foreach (var b in group.Bonuses)
                    {
                        var t = (b.Type ?? "").ToUpper();
                        var v = b.Value;
                        if (v == 0) continue;
                        string s = v % 1 == 0 ? ((int)v).ToString() : v.ToString("F2");
                        if (t == "STR") row.HeroSTR = s;
                        else if (t == "AGI") row.HeroAGI = s;
                        else if (t == "TECH") row.HeroTECH = s;
                        else if (t == "INT") row.HeroINT = s;
                    }
                }
            }

            // StatBonuses, Thresholds, Accumulations (JSON round-trip for Actions settings form)
            var jsonOptions = new JsonSerializerOptions { WriteIndented = false };
            if (data.StatBonuses != null && data.StatBonuses.Count > 0)
                row.StatBonusesJson = JsonSerializer.Serialize(data.StatBonuses, jsonOptions);
            else if (baseRow != null)
                row.StatBonusesJson = baseRow.StatBonusesJson ?? "";
            if (data.Thresholds != null && data.Thresholds.Count > 0)
                row.ThresholdsJson = JsonSerializer.Serialize(data.Thresholds, jsonOptions);
            else if (baseRow != null)
                row.ThresholdsJson = baseRow.ThresholdsJson ?? "";
            if (data.Accumulations != null && data.Accumulations.Count > 0)
                row.AccumulationsJson = JsonSerializer.Serialize(data.Accumulations, jsonOptions);
            else if (baseRow != null)
                row.AccumulationsJson = baseRow.AccumulationsJson ?? "";

            data.NormalizeChainPositionBonuses();
            if (data.ChainPositionBonuses != null && data.ChainPositionBonuses.Count > 0)
                row.ChainPositionBonusesJson = JsonSerializer.Serialize(data.ChainPositionBonuses, jsonOptions);
            else if (baseRow != null)
                row.ChainPositionBonusesJson = baseRow.ChainPositionBonusesJson ?? "";

            bool hasPersistedBonusGroups = data.ActionAttackBonuses?.BonusGroups != null
                && data.ActionAttackBonuses.BonusGroups.Any(g => g?.Bonuses != null && g.Bonuses.Count > 0);
            if (hasPersistedBonusGroups)
                row.ActionAttackBonusesJson = JsonSerializer.Serialize(data.ActionAttackBonuses, jsonOptions);
            else
                row.ActionAttackBonusesJson = "";

            // TargetType round-trip (form edits must persist when baseRow exists)
            row.Target = FormatTargetForSheet(data.TargetType);

            if (data.HealAmount > 0)
                row.HeroHeal = data.HealAmount.ToString();
            if (data.MaxHealthIncrease > 0)
                row.HeroHealMaxHealth = data.MaxHealthIncrease.ToString();
            if (data.LifestealPercent > 0)
                row.Lifesteal = FormatLifestealForSheet(data.LifestealPercent);

            // Default/starting action round-trip (Settings Actions form)
            row.IsDefaultAction = data.IsDefaultAction ? "1" : (baseRow?.IsDefaultAction ?? "");

            // Weapon types round-trip (Assign to Weapon Types in Actions settings)
            row.WeaponTypes = data.WeaponTypes != null && data.WeaponTypes.Count > 0 ? string.Join(", ", data.WeaponTypes) : (baseRow?.WeaponTypes ?? "");

            row.Weaken = data.CausesWeaken ? "1" : "";
            row.Slow = data.CausesSlow ? "1" : "";
            row.Vulnerability = data.CausesVulnerability ? "1" : "";
            row.Harden = data.CausesHarden ? "1" : "";
            row.Expose = data.CausesExpose ? "1" : "";
            row.Silence = data.CausesSilence ? "1" : "";
            row.Pierce = data.CausesPierce ? "1" : "";
            row.StatDrain = data.CausesStatDrain ? "1" : "";
            row.Focus = data.CausesFocus ? "1" : "";
            row.Confuse = data.CausesConfusion ? "1" : "";
            row.Disrupt = data.CausesDisrupt ? "1" : "";
            row.Fortify = data.CausesFortify
                ? (data.FortifyArmorPerStack > 0 ? data.FortifyArmorPerStack.ToString() : "1")
                : "";

            var spreadsheetRow = row.ToSpreadsheetActionData();
            // Preserve CADENCES triples through SyncRow
            if (!string.IsNullOrWhiteSpace(row.CadenceBundlesJson))
                spreadsheetRow.CadenceBundlesJson = row.CadenceBundlesJson;
            ActionMechanicsSheetSync.SyncRow(spreadsheetRow);
            return SpreadsheetActionJson.FromSpreadsheetActionData(spreadsheetRow);
        }

        /// <summary>
        /// Converts the current list of ActionData (editor state) to List of SpreadsheetActionJson for saving.
        /// Uses originalRows to merge by action name so columns not edited in the UI are preserved.
        /// Order follows the current actions list. New actions get no base row; deleted actions (in original but not in actions) are omitted.
        /// </summary>
        public static List<SpreadsheetActionJson> ConvertList(List<ActionData> actions, List<SpreadsheetActionJson>? originalRows)
        {
            // Build base-row map; allow duplicate action names in file by keeping first occurrence (avoids ToDictionary duplicate-key throw so save can complete)
            var byName = new Dictionary<string, SpreadsheetActionJson>(StringComparer.OrdinalIgnoreCase);
            if (originalRows != null)
            {
                foreach (var r in originalRows)
                {
                    if (string.IsNullOrEmpty(r.Action)) continue;
                    if (!byName.ContainsKey(r.Action))
                        byName[r.Action] = r;
                }
            }

            var result = new List<SpreadsheetActionJson>();
            foreach (var action in actions)
            {
                if (string.IsNullOrEmpty(action.Name))
                    continue;
                byName.TryGetValue(action.Name, out var baseRow);
                result.Add(Merge(action, baseRow));
            }
            return result;
        }

        private static string FormatTargetForSheet(string? targetType)
        {
            if (string.IsNullOrWhiteSpace(targetType))
                return "";
            return targetType.Trim() switch
            {
                "Self" => "self",
                "Environment" => "environment",
                _ => ""
            };
        }

        private static string FormatLifestealForSheet(double lifestealPercent)
        {
            double pct = lifestealPercent <= 1.0 ? lifestealPercent * 100 : lifestealPercent;
            return pct % 1 == 0 ? $"{(int)pct}%" : pct.ToString("F1") + "%";
        }

        private static string FormatDamage(double damageMultiplier)
        {
            if (damageMultiplier <= 0)
                return "100%";
            double pct = damageMultiplier * 100;
            return pct % 1 == 0 ? $"{(int)pct}%" : pct.ToString("F1") + "%";
        }

        private static SpreadsheetActionJson Clone(SpreadsheetActionJson source)
        {
            return SpreadsheetActionJson.FromSpreadsheetActionData(source.ToSpreadsheetActionData());
        }
    }
}
