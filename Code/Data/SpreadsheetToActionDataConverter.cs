using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using RPGGame.Actions;
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
            SpreadsheetDurationSemantics.NormalizeDurationAndCadence(spreadsheet);
            ActionMechanicsSheetSync.ApplyCadenceDefaultsForMechanics(spreadsheet);
            var actionData = new ActionData();
            
            // Basic properties
            actionData.Name = spreadsheet.Action;
            actionData.Description = spreadsheet.Description;

            // Damage and speed (preserve explicit 0% damage for buff/debuff rows)
            double rawDamage = SpreadsheetActionData.ParseNumericValue(spreadsheet.Damage);
            bool damageExplicitlyZero = !string.IsNullOrWhiteSpace(spreadsheet.Damage) && rawDamage <= 0;
            actionData.DamageMultiplier = damageExplicitlyZero ? 0 : (rawDamage == 0 ? 1.0 : rawDamage);
            
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
            
            // Item-applied status only (stun/poison/burn/bleed) — see ActionMechanicsRegistry.ItemAppliedStatusEffectIds.
            // Sheet columns may still round-trip in SpreadsheetActionData but are not applied to ActionData on pull.
            actionData.CausesWeaken = !string.IsNullOrWhiteSpace(spreadsheet.Weaken) && spreadsheet.Weaken != "0";
            actionData.CausesSlow = !string.IsNullOrWhiteSpace(spreadsheet.Slow) && spreadsheet.Slow != "0";
            // Advanced status effects
            actionData.CausesVulnerability = !string.IsNullOrWhiteSpace(spreadsheet.Vulnerability) && spreadsheet.Vulnerability != "0";
            actionData.CausesHarden = !string.IsNullOrWhiteSpace(spreadsheet.Harden) && spreadsheet.Harden != "0";
            actionData.CausesExpose = !string.IsNullOrWhiteSpace(spreadsheet.Expose) && spreadsheet.Expose != "0";
            actionData.CausesSilence = !string.IsNullOrWhiteSpace(spreadsheet.Silence) && spreadsheet.Silence != "0";
            actionData.CausesPierce = !string.IsNullOrWhiteSpace(spreadsheet.Pierce) && spreadsheet.Pierce != "0";
            actionData.CausesStatDrain = !string.IsNullOrWhiteSpace(spreadsheet.StatDrain) && spreadsheet.StatDrain != "0";
            actionData.CausesFocus = !string.IsNullOrWhiteSpace(spreadsheet.Focus) && spreadsheet.Focus != "0";
            actionData.CausesConfusion = !string.IsNullOrWhiteSpace(spreadsheet.Confuse) && spreadsheet.Confuse != "0";
            actionData.CausesDisrupt = !string.IsNullOrWhiteSpace(spreadsheet.Disrupt) && spreadsheet.Disrupt != "0";
            actionData.CausesFortify = !string.IsNullOrWhiteSpace(spreadsheet.Fortify) && spreadsheet.Fortify != "0";
            if (actionData.CausesFortify)
                actionData.FortifyArmorPerStack = ParsePositiveIntStatusMagnitude(spreadsheet.Fortify, 1);

            if (!string.IsNullOrWhiteSpace(spreadsheet.Lifesteal) && spreadsheet.Lifesteal != "0")
                actionData.LifestealPercent = ParseLifestealPercent(spreadsheet.Lifesteal);

            if (!string.IsNullOrWhiteSpace(spreadsheet.HeroHeal) && spreadsheet.HeroHeal != "0")
            {
                int heal = SpreadsheetActionData.ParseIntValue(spreadsheet.HeroHeal);
                if (heal <= 0)
                    heal = (int)Math.Round(SpreadsheetActionData.ParseNumericValue(spreadsheet.HeroHeal));
                actionData.HealAmount = Math.Max(1, heal);
            }

            if (!string.IsNullOrWhiteSpace(spreadsheet.HeroHealMaxHealth) && spreadsheet.HeroHealMaxHealth != "0")
            {
                int maxHp = SpreadsheetActionData.ParseIntValue(spreadsheet.HeroHealMaxHealth);
                if (maxHp <= 0)
                    maxHp = (int)Math.Round(SpreadsheetActionData.ParseNumericValue(spreadsheet.HeroHealMaxHealth));
                actionData.MaxHealthIncrease = Math.Max(1, maxHp);
            }

            if (spreadsheet.SelfTargetEffects != null && spreadsheet.SelfTargetEffects.Count > 0)
            {
                actionData.SelfTargetEffects = spreadsheet.SelfTargetEffects
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .Select(e => e.Trim().ToLowerInvariant())
                    .Where(e => e is not ("reflect" or "cleanse"))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (actionData.SelfTargetEffects.Contains("fortify"))
                    actionData.CausesFortify = true;
            }

            actionData.Type = DetermineActionType(spreadsheet, actionData.DamageMultiplier);
            actionData.TargetType = DetermineTargetType(spreadsheet);

            // Spreadsheet-origin fields (round-trip)
            actionData.Rarity = spreadsheet.Rarity ?? "";
            actionData.Category = spreadsheet.Category ?? "";
            actionData.Cadence = spreadsheet.Cadence ?? "";
            actionData.ComboBonusDuration = SpreadsheetDurationSemantics.ResolveCadenceDuration(spreadsheet);
            actionData.SpeedMod = spreadsheet.SpeedMod ?? "";
            actionData.DamageMod = spreadsheet.DamageMod ?? "";
            actionData.MultiHitMod = spreadsheet.MultiHitMod ?? "";
            actionData.AmpMod = spreadsheet.AmpMod ?? "";
            actionData.EnemySpeedMod = spreadsheet.EnemySpeedMod ?? "";
            actionData.EnemyDamageMod = spreadsheet.EnemyDamageMod ?? "";
            actionData.EnemyMultiHitMod = spreadsheet.EnemyMultiHitMod ?? "";
            actionData.EnemyAmpMod = spreadsheet.EnemyAmpMod ?? "";

            // Combo & position (round-trip)
            actionData.ChainPosition = spreadsheet.ChainPosition ?? "";
            actionData.ModifyBasedOnChainPosition = spreadsheet.ModifyBasedOnChainPosition ?? "";
            actionData.Jump = spreadsheet.Jump ?? "";
            actionData.JumpRelative = spreadsheet.JumpRelative ?? "";
            actionData.ChainLength = spreadsheet.ChainLength ?? "";
            actionData.Reset = spreadsheet.Reset ?? "";
            actionData.ResetBlockerBuffer = spreadsheet.ResetBlockerBuffer ?? "";
            actionData.IsOpener = !string.IsNullOrWhiteSpace(spreadsheet.Opener) && spreadsheet.Opener != "0";
            actionData.IsFinisher = !string.IsNullOrWhiteSpace(spreadsheet.Finisher) && spreadsheet.Finisher != "0";

            // Combo properties: hero actions use the combo strip; environment hazards override below.
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

            ActionTagSyncHelper.SyncCanonicalTags(actionData);

            ApplyNonHeroSpreadsheetDefaults(actionData);
            
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
            var persistedBonuses = DeserializeActionAttackBonuses(spreadsheet.ActionAttackBonusesJson);
            if (persistedBonuses?.BonusGroups != null && persistedBonuses.BonusGroups.Count > 0)
                actionData.ActionAttackBonuses = persistedBonuses;
            ActionCadenceDurationResolver.SyncBonusGroupCountsFromDuration(actionData);
            EnsureKeywordBonusGroups(actionData, spreadsheet);
            MultiDiceRollMapper.ApplyRollAdvantageBonuses(actionData, spreadsheet);

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

            var declaredMechanics = ActionMechanicsRegistry.ParseMechanicsCell(spreadsheet.Mechanics);
            actionData.Mechanics = declaredMechanics.Count > 0
                ? ActionMechanicsRegistry.FilterForMechanicsColumn(declaredMechanics)
                : ActionMechanicsRegistry.FilterForMechanicsColumn(ActionMechanicsRegistry.DetectFromSpreadsheetRow(spreadsheet));
            
            return actionData;
        }
        
        private static string DetermineActionType(SpreadsheetActionData spreadsheet, double damageMultiplier)
        {
            if (!string.IsNullOrWhiteSpace(spreadsheet.HeroHeal) && spreadsheet.HeroHeal != "0")
                return "Heal";

            if (!string.IsNullOrWhiteSpace(spreadsheet.HeroHealMaxHealth) && spreadsheet.HeroHealMaxHealth != "0")
                return "Heal";

            if (IsSelfTarget(spreadsheet) && HasDefensiveStatus(spreadsheet) && !HasOffensiveStatus(spreadsheet))
                return "Buff";

            if (HasOffensiveStatus(spreadsheet) && damageMultiplier <= 0)
                return "Debuff";

            if (damageMultiplier <= 0 && HasDefensiveStatus(spreadsheet) && !HasOffensiveStatus(spreadsheet))
                return "Buff";

            if (damageMultiplier <= 0 && HasOffensiveStatus(spreadsheet))
                return "Debuff";

            return "Attack";
        }

        private static bool IsSelfTarget(SpreadsheetActionData spreadsheet)
        {
            if (string.IsNullOrWhiteSpace(spreadsheet.Target))
                return false;
            return spreadsheet.Target.Trim().Equals("self", StringComparison.OrdinalIgnoreCase)
                || spreadsheet.Target.Trim().Equals("SELF", StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasDefensiveStatus(SpreadsheetActionData s) =>
            IsStatusSet(s.Harden) || IsStatusSet(s.Focus) || IsStatusSet(s.Fortify);

        private static bool HasOffensiveStatus(SpreadsheetActionData s) =>
            IsStatusSet(s.Stun) || IsStatusSet(s.Poison) || IsStatusSet(s.Burn) || IsStatusSet(s.Bleed)
            || IsStatusSet(s.Weaken) || IsStatusSet(s.Slow) || IsStatusSet(s.Vulnerability) || IsStatusSet(s.Expose)
            || IsStatusSet(s.Silence) || IsStatusSet(s.Pierce) || IsStatusSet(s.StatDrain)
            || IsStatusSet(s.Confuse) || IsStatusSet(s.Disrupt);

        private static bool IsStatusSet(string value) =>
            !string.IsNullOrWhiteSpace(value) && value != "0";

        private static double ParseLifestealPercent(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value == "0")
                return 0;
            string trimmed = value.Trim();
            if (trimmed.EndsWith("%", StringComparison.Ordinal))
                return SpreadsheetActionData.ParseNumericValue(trimmed);
            double v = SpreadsheetActionData.ParseNumericValue(trimmed);
            if (v > 1.0)
                return v / 100.0;
            return v;
        }
        
        /// <summary>
        /// Determines the target type from spreadsheet data
        /// </summary>
        private static string DetermineTargetType(SpreadsheetActionData spreadsheet)
        {
            if (!string.IsNullOrWhiteSpace(spreadsheet.Target))
            {
                string target = spreadsheet.Target.Trim();
                switch (target.ToUpperInvariant())
                {
                    case "SELF":
                        return "Self";
                    case "ENEMY":
                        return "SingleTarget";
                    case "ENVIRONMENT":
                    case "AOE":
                    case "AREA":
                    case "ALL":
                    case "EVERYONE":
                    case "ROOM":
                        return "Environment";
                }
            }

            if (!string.IsNullOrWhiteSpace(spreadsheet.HeroHeal) && spreadsheet.HeroHeal != "0")
                return "Self";

            return "SingleTarget";
        }

        /// <summary>
        /// Ensures rows tagged for enemies or the environment never inherit weapon assignment from the sheet,
        /// and applies defaults for environmental hazards (non-combo, AoE debuff) so they are not offered as hero actions.
        /// </summary>
        private static void ApplyNonHeroSpreadsheetDefaults(ActionData actionData)
        {
            bool hasEnemy = actionData.Tags != null && actionData.Tags.Any(t => string.Equals(t, "enemy", StringComparison.OrdinalIgnoreCase));
            bool hasEnvironment = actionData.Tags != null && actionData.Tags.Any(t => string.Equals(t, "environment", StringComparison.OrdinalIgnoreCase));
            if (!hasEnemy && !hasEnvironment)
                return;

            // Never assign weapon types from the spreadsheet for these pools (prevents hero gear picking them up).
            actionData.WeaponTypes = new List<string>();

            if (hasEnvironment)
            {
                actionData.Type = "Debuff";
                if (actionData.TargetType == "SingleTarget")
                    actionData.TargetType = "Environment";
                actionData.IsComboAction = false;
                actionData.ComboOrder = -1;
            }
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

        private static ActionAttackBonuses? DeserializeActionAttackBonuses(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                return JsonSerializer.Deserialize<ActionAttackBonuses>(json);
            }
            catch { return null; }
        }

        private static double ParsePositiveDoubleStatusMagnitude(string? cell, double defaultVal)
        {
            if (string.IsNullOrWhiteSpace(cell)) return defaultVal;
            if (double.TryParse(cell.Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double v) && v > 0)
                return v;
            return defaultVal;
        }

        private static int ParsePositiveIntStatusMagnitude(string? cell, int defaultVal)
        {
            if (string.IsNullOrWhiteSpace(cell)) return defaultVal;
            if (int.TryParse(cell.Trim(), out int v) && v > 0)
                return v;
            return defaultVal;
        }

        /// <summary>
        /// Hero dice / AJ–AM modifier columns without an explicit CADENCE cell still defer to the next ACTION/ATTACK.
        /// Infer cadence + duration from DURATION (including combined cells like <c>3 ACTION</c>) so layer count is not stuck at 1.
        /// </summary>
        private static void EnsureKeywordBonusGroups(ActionData actionData, SpreadsheetActionData spreadsheet)
        {
            if (actionData.ActionAttackBonuses?.BonusGroups != null && actionData.ActionAttackBonuses.BonusGroups.Count > 0)
                return;
            if (!SpreadsheetRowHasKeywordBonusSource(spreadsheet) && !ActionMechanicsRegistry.RowHasCadenceGatedMechanic(spreadsheet))
                return;

            ActionMechanicsSheetSync.ApplyCadenceDefaultsForMechanics(spreadsheet);

            actionData.Cadence = spreadsheet.Cadence;
            actionData.ComboBonusDuration = SpreadsheetDurationSemantics.ResolveCadenceDuration(spreadsheet);
            if (actionData.ComboBonusDuration <= 0)
                actionData.ComboBonusDuration = 1;

            actionData.ActionAttackBonuses = ActionAttackKeywordProcessor.ProcessBonuses(spreadsheet);
            ActionCadenceDurationResolver.SyncBonusGroupCountsFromDuration(actionData);
        }

        private static bool SpreadsheetRowHasKeywordBonusSource(SpreadsheetActionData spreadsheet)
        {
            static bool nz(string? v) => !string.IsNullOrWhiteSpace(v) && v != "0";
            return nz(spreadsheet.HeroAccuracy)
                || nz(spreadsheet.HeroHit)
                || nz(spreadsheet.HeroCombo)
                || nz(spreadsheet.HeroCrit)
                || nz(spreadsheet.HeroCritMiss)
                || nz(spreadsheet.HeroSTR)
                || nz(spreadsheet.HeroAGI)
                || nz(spreadsheet.HeroTECH)
                || nz(spreadsheet.HeroINT)
                || nz(spreadsheet.SpeedMod)
                || nz(spreadsheet.DamageMod)
                || nz(spreadsheet.MultiHitMod)
                || nz(spreadsheet.AmpMod);
        }
    }
}
