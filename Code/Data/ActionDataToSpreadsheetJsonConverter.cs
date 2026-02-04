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
            row.Cadence = data.Cadence ?? "";
            row.SpeedMod = data.SpeedMod ?? baseRow?.SpeedMod ?? "";
            row.DamageMod = data.DamageMod ?? baseRow?.DamageMod ?? "";
            row.MultiHitMod = data.MultiHitMod ?? baseRow?.MultiHitMod ?? "";
            row.AmpMod = data.AmpMod ?? baseRow?.AmpMod ?? "";
            row.ChainPosition = data.ChainPosition ?? baseRow?.ChainPosition ?? "";
            row.ModifyBasedOnChainPosition = data.ModifyBasedOnChainPosition ?? baseRow?.ModifyBasedOnChainPosition ?? "";
            row.Jump = data.Jump ?? baseRow?.Jump ?? "";
            row.ChainLength = data.ChainLength ?? baseRow?.ChainLength ?? "";
            row.Reset = data.Reset ?? baseRow?.Reset ?? "";
            row.ResetBlockerBuffer = data.ResetBlockerBuffer ?? baseRow?.ResetBlockerBuffer ?? "";
            row.Opener = data.IsOpener ? "true" : (baseRow?.Opener ?? "");
            row.Finisher = data.IsFinisher ? "true" : (baseRow?.Finisher ?? "");

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

            // Roll bonuses (form fields → spreadsheet columns for round-trip)
            row.HeroAccuracy = data.RollBonus != 0 ? data.RollBonus.ToString() : (baseRow?.HeroAccuracy ?? "");
            row.HeroHit = data.HitThresholdAdjustment != 0 ? data.HitThresholdAdjustment.ToString() : (baseRow?.HeroHit ?? "");
            row.HeroCombo = data.ComboThresholdAdjustment != 0 ? data.ComboThresholdAdjustment.ToString() : (baseRow?.HeroCombo ?? "");
            row.HeroCrit = data.CriticalHitThresholdAdjustment != 0 ? data.CriticalHitThresholdAdjustment.ToString() : (baseRow?.HeroCrit ?? "");

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

            // TargetType round-trip (form edits must persist when baseRow exists)
            row.Target = data.TargetType == "Self" ? "SELF" : "ENEMY";

            // Default/starting action round-trip (Settings Actions form)
            row.IsDefaultAction = data.IsDefaultAction ? "1" : (baseRow?.IsDefaultAction ?? "");

            // Weapon types round-trip (Assign to Weapon Types in Actions settings)
            row.WeaponTypes = data.WeaponTypes != null && data.WeaponTypes.Count > 0 ? string.Join(", ", data.WeaponTypes) : (baseRow?.WeaponTypes ?? "");

            if (baseRow == null)
            {
                if (data.SelfDamagePercent != 0)
                {
                    row.SelfDamage = (data.SelfDamagePercent / 100.0).ToString("F2");
                }
                row.Stun = data.CausesStun ? "1" : "";
                row.Poison = data.CausesPoison ? "1" : "";
                row.Burn = data.CausesBurn ? "1" : "";
                row.Bleed = data.CausesBleed ? "1" : "";
                row.Weaken = data.CausesWeaken ? "1" : "";
                row.Slow = data.CausesSlow ? "1" : "";
                row.Vulnerability = data.CausesVulnerability ? "1" : "";
                row.Harden = data.CausesHarden ? "1" : "";
                row.Expose = data.CausesExpose ? "1" : "";
                row.Silence = data.CausesSilence ? "1" : "";
                row.Pierce = data.CausesPierce ? "1" : "";
                row.StatDrain = data.CausesStatDrain ? "1" : "";
                row.Fortify = data.CausesFortify ? "1" : "";
                row.Focus = data.CausesFocus ? "1" : "";
                row.Cleanse = data.CausesCleanse ? "1" : "";
                row.Reflect = data.CausesReflect ? "1" : "";
            }

            return row;
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
