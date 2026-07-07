using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Actions;

namespace RPGGame.Data
{
    /// <summary>Loads and applies cadence-grouped editor blocks to <see cref="ActionData"/>.</summary>
    public static class ActionCadenceEditorSync
    {
        public static List<CadenceEditorBlock> LoadBlocks(ActionData action)
        {
            if (action == null)
                return new List<CadenceEditorBlock>();

            if (action.ActionAttackBonuses?.BonusGroups != null
                && action.ActionAttackBonuses.BonusGroups.Count > 0
                && HasPersistedMultiBlockOrRichGroups(action))
            {
                var fromGroups = LoadFromBonusGroups(action);
                AppendStatusMechanicsNotInBlocks(action, fromGroups);
                return fromGroups;
            }

            var single = LoadFromFlatFields(action);
            return single.Count > 0 ? single : new List<CadenceEditorBlock>();
        }

        public static void ApplyBlocks(ActionData action, List<CadenceEditorBlock> blocks)
        {
            if (action == null)
                return;
            blocks ??= new List<CadenceEditorBlock>();

            ClearCadenceFlatFields(action);
            action.ActionAttackBonuses = new ActionAttackBonuses();

            var nonEmpty = blocks
                .Where(b => b.Mechanics.Any(m => !string.IsNullOrWhiteSpace(m.MechanicId) && m.Quantity != 0))
                .ToList();

            foreach (var block in nonEmpty)
            {
                var group = BuildBonusGroup(block);
                if (group.Bonuses.Count > 0 || block.Mechanics.Any(m => IsStatusMechanic(m.MechanicId)))
                    action.ActionAttackBonuses.BonusGroups.Add(group);
            }

            if (nonEmpty.Count > 0)
                ApplyPrimaryBlockToFlatFields(action, nonEmpty[0]);

            foreach (var block in nonEmpty)
                ApplyStatusMechanicsToFlatFields(action, block);

            if (action.ActionAttackBonuses.BonusGroups.Count == 0)
                action.ActionAttackBonuses = null;

            ActionCadenceDurationResolver.SyncBonusGroupCountsFromDuration(action);
            RebuildMechanicsList(action);
        }

        private static bool HasPersistedMultiBlockOrRichGroups(ActionData action)
        {
            var groups = action.ActionAttackBonuses!.BonusGroups;
            if (groups.Count > 1)
                return true;
            if (groups.Count == 1 && groups[0].Bonuses.Count > 0)
            {
                string cadence = ResolveGroupCadence(groups[0]);
                string flatCadence = CadenceKeywords.Normalize(action.Cadence ?? "");
                int flatDuration = action.ComboBonusDuration > 0 ? action.ComboBonusDuration : 1;
                if (!string.Equals(cadence, flatCadence, StringComparison.OrdinalIgnoreCase)
                    || groups[0].Count != flatDuration)
                    return true;
            }
            return false;
        }

        private static List<CadenceEditorBlock> LoadFromBonusGroups(ActionData action)
        {
            var blocks = new List<CadenceEditorBlock>();
            foreach (var group in action.ActionAttackBonuses!.BonusGroups)
            {
                if (group?.Bonuses == null || group.Bonuses.Count == 0)
                    continue;
                var block = new CadenceEditorBlock
                {
                    Cadence = ToEditorCadence(ResolveGroupCadence(group)),
                    Duration = group.Count > 0 ? group.Count : Math.Max(1, action.ComboBonusDuration)
                };
                foreach (var bonus in group.Bonuses)
                {
                    if (ActionMechanicsRegistry.TryGetMechanicIdFromBonusType(bonus.Type, out string mechanicId, out string? statSubType))
                    {
                        block.Mechanics.Add(new CadenceMechanicRow
                        {
                            MechanicId = mechanicId,
                            Quantity = bonus.Value,
                            StatSubType = statSubType ?? ""
                        });
                    }
                }
                if (block.Mechanics.Count > 0)
                    blocks.Add(block);
            }
            return blocks;
        }

        private static List<CadenceEditorBlock> LoadFromFlatFields(ActionData action)
        {
            string cadence = string.IsNullOrWhiteSpace(action.Cadence)
                ? ActionMechanicsRegistry.ResolveDefaultCadence(DetectMechanicsFromFlat(action)) ?? "Turn"
                : ActionFormCadenceToEditor(action.Cadence);
            int duration = action.ComboBonusDuration > 0 ? action.ComboBonusDuration : 1;

            var block = new CadenceEditorBlock { Cadence = cadence, Duration = duration };
            AddFlatMechanicRows(action, block);
            if (block.Mechanics.Count == 0)
                return new List<CadenceEditorBlock>();
            return new List<CadenceEditorBlock> { block };
        }

        private static void AppendStatusMechanicsNotInBlocks(ActionData action, List<CadenceEditorBlock> blocks)
        {
            var present = new HashSet<string>(
                blocks.SelectMany(b => b.Mechanics.Select(m => m.MechanicId)),
                StringComparer.OrdinalIgnoreCase);

            var statusRows = CollectStatusRows(action)
                .Where(r => !present.Contains(r.MechanicId))
                .ToList();
            if (statusRows.Count == 0)
                return;

            string cadence = blocks.Count > 0
                ? blocks[0].Cadence
                : (string.IsNullOrWhiteSpace(action.Cadence) ? "Turn" : ActionFormCadenceToEditor(action.Cadence));
            int duration = blocks.Count > 0 && blocks[0].Duration > 0
                ? blocks[0].Duration
                : Math.Max(1, action.ComboBonusDuration);

            CadenceEditorBlock target = blocks.Count > 0
                ? blocks[0]
                : new CadenceEditorBlock { Cadence = cadence, Duration = duration };

            if (blocks.Count == 0)
                blocks.Add(target);

            foreach (var row in statusRows)
                target.Mechanics.Add(row);
        }

        private static void AddFlatMechanicRows(ActionData action, CadenceEditorBlock block)
        {
            void Add(string mechanicId, double qty, string statSubType = "")
            {
                if (qty == 0 && !IsStatusMechanic(mechanicId))
                    return;
                block.Mechanics.Add(new CadenceMechanicRow { MechanicId = mechanicId, Quantity = qty, StatSubType = statSubType });
            }

            Add("hero_accuracy", action.RollBonus);
            Add("hero_hit_threshold", action.HitThresholdAdjustment);
            Add("hero_combo_threshold", action.ComboThresholdAdjustment);
            Add("hero_crit_threshold", action.CriticalHitThresholdAdjustment);
            Add("hero_crit_miss_threshold", action.CriticalMissThresholdAdjustment);

            Add("enemy_accuracy", action.EnemyRollBonus);
            Add("enemy_hit_threshold", action.EnemyHitThresholdAdjustment);
            Add("enemy_combo_threshold", action.EnemyComboThresholdAdjustment);
            Add("enemy_crit_threshold", action.EnemyCriticalHitThresholdAdjustment);
            Add("enemy_crit_miss_threshold", action.EnemyCriticalMissThresholdAdjustment);

            AddModRow(block, "hero_next_action_speed", action.SpeedMod);
            AddModRow(block, "hero_next_action_damage", action.DamageMod);
            AddModRow(block, "hero_next_action_multihit", action.MultiHitMod);
            AddModRow(block, "hero_next_action_amp", action.AmpMod);
            AddModRow(block, "enemy_next_action_speed", action.EnemySpeedMod);
            AddModRow(block, "enemy_next_action_damage", action.EnemyDamageMod);
            AddModRow(block, "enemy_next_action_multihit", action.EnemyMultiHitMod);
            AddModRow(block, "enemy_next_action_amp", action.EnemyAmpMod);

            action.NormalizeStatBonuses();
            foreach (var stat in action.StatBonuses)
            {
                if (stat.Value == 0 || string.IsNullOrWhiteSpace(stat.Type))
                    continue;
                string sub = stat.Type.Trim().ToUpperInvariant() switch
                {
                    "STRENGTH" => "STR",
                    "AGILITY" => "AGI",
                    "TECHNIQUE" => "TECH",
                    "INTELLIGENCE" => "INT",
                    _ => stat.Type.Trim().ToUpperInvariant()
                };
                Add("hero_stat_bonus", stat.Value, sub);
            }

            foreach (var row in CollectStatusRows(action))
                block.Mechanics.Add(row);

            if (action.HealAmount > 0)
                Add("heal", action.HealAmount);
            if (action.MaxHealthIncrease > 0)
                Add("max_health", action.MaxHealthIncrease);
        }

        private static void AddModRow(CadenceEditorBlock block, string mechanicId, string? modValue)
        {
            if (string.IsNullOrWhiteSpace(modValue))
                return;
            double qty = ActionMechanicsRegistry.IsPercentQuantityMechanic(mechanicId)
                ? ParsePercentMod(modValue)
                : (ModifierParser.ParseValue(modValue) ?? 0);
            if (qty != 0)
                block.Mechanics.Add(new CadenceMechanicRow { MechanicId = mechanicId, Quantity = qty });
        }

        private static double ParsePercentMod(string value)
        {
            if (ModifierParser.ParsePercent(value) is { } p)
                return p * 100.0;
            if (double.TryParse(value.Trim(), System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double v))
                return v;
            return 0;
        }

        private static List<CadenceMechanicRow> CollectStatusRows(ActionData action)
        {
            var rows = new List<CadenceMechanicRow>();
            void Status(string id, bool on, double qty = 1)
            {
                if (on)
                    rows.Add(new CadenceMechanicRow { MechanicId = id, Quantity = qty });
            }
            Status("weaken", action.CausesWeaken);
            Status("slow", action.CausesSlow);
            Status("vulnerability", action.CausesVulnerability);
            Status("harden", action.CausesHarden);
            Status("focus", action.CausesFocus);
            Status("confuse", action.CausesConfusion);
            Status("stat_drain", action.CausesStatDrain);
            Status("fortify", action.CausesFortify, action.FortifyArmorPerStack > 0 ? action.FortifyArmorPerStack : 1);
            Status("disrupt", action.CausesDisrupt);
            Status("pierce", action.CausesPierce);
            return rows;
        }

        private static List<string> DetectMechanicsFromFlat(ActionData action)
        {
            var block = new CadenceEditorBlock();
            AddFlatMechanicRows(action, block);
            return block.Mechanics.Select(m => m.MechanicId).ToList();
        }

        private static ActionAttackBonusGroup BuildBonusGroup(CadenceEditorBlock block)
        {
            string cadence = CadenceKeywords.Normalize(block.Cadence);
            int duration = block.Duration > 0 ? block.Duration : 1;
            var group = new ActionAttackBonusGroup
            {
                Count = duration,
                DurationType = cadence
            };

            if (CadenceKeywords.IsAction(cadence))
            {
                group.Keyword = CadenceKeywords.Action;
                group.CadenceType = CadenceKeywords.Action;
            }
            else if (CadenceKeywords.IsTurn(cadence))
            {
                group.Keyword = CadenceKeywords.Turn;
                group.CadenceType = CadenceKeywords.Turn;
            }
            else
            {
                group.Keyword = CadenceKeywords.Turn;
                group.CadenceType = CadenceKeywords.Turn;
            }

            foreach (var row in block.Mechanics)
            {
                if (string.IsNullOrWhiteSpace(row.MechanicId) || row.Quantity == 0)
                    continue;
                if (IsStatusMechanic(row.MechanicId))
                    continue;
                if (!ActionMechanicsRegistry.TryGetBonusTypeForMechanic(row.MechanicId, row.StatSubType, out string bonusType))
                    continue;
                group.Bonuses.Add(new ActionAttackBonusItem { Type = bonusType, Value = row.Quantity });
            }
            return group;
        }

        private static void ApplyPrimaryBlockToFlatFields(ActionData action, CadenceEditorBlock block)
        {
            action.Cadence = EditorCadenceToStorage(block.Cadence);
            action.ComboBonusDuration = block.Duration > 0 ? block.Duration : 1;

            foreach (var row in block.Mechanics)
            {
                if (string.IsNullOrWhiteSpace(row.MechanicId))
                    continue;
                ApplyMechanicToFlatField(action, row);
            }
        }

        private static void ApplyStatusMechanicsToFlatFields(ActionData action, CadenceEditorBlock block)
        {
            foreach (var row in block.Mechanics.Where(m => IsStatusMechanic(m.MechanicId)))
                ApplyStatusToFlatField(action, row);
        }

        private static void ApplyMechanicToFlatField(ActionData action, CadenceMechanicRow row)
        {
            string id = ActionMechanicsRegistry.NormalizeMechanicId(row.MechanicId);
            switch (id)
            {
                case "hero_accuracy": action.RollBonus = (int)row.Quantity; break;
                case "hero_hit_threshold": action.HitThresholdAdjustment = (int)row.Quantity; break;
                case "hero_combo_threshold": action.ComboThresholdAdjustment = (int)row.Quantity; break;
                case "hero_crit_threshold": action.CriticalHitThresholdAdjustment = (int)row.Quantity; break;
                case "hero_crit_miss_threshold": action.CriticalMissThresholdAdjustment = (int)row.Quantity; break;
                case "enemy_accuracy": action.EnemyRollBonus = (int)row.Quantity; break;
                case "enemy_hit_threshold": action.EnemyHitThresholdAdjustment = (int)row.Quantity; break;
                case "enemy_combo_threshold": action.EnemyComboThresholdAdjustment = (int)row.Quantity; break;
                case "enemy_crit_threshold": action.EnemyCriticalHitThresholdAdjustment = (int)row.Quantity; break;
                case "enemy_crit_miss_threshold": action.EnemyCriticalMissThresholdAdjustment = (int)row.Quantity; break;
                case "hero_next_action_speed": action.SpeedMod = FormatPercentMod(row.Quantity); break;
                case "hero_next_action_damage": action.DamageMod = FormatPercentMod(row.Quantity); break;
                case "hero_next_action_multihit": action.MultiHitMod = row.Quantity.ToString("0"); break;
                case "hero_next_action_amp": action.AmpMod = FormatPercentMod(row.Quantity); break;
                case "enemy_next_action_speed": action.EnemySpeedMod = FormatPercentMod(row.Quantity); break;
                case "enemy_next_action_damage": action.EnemyDamageMod = FormatPercentMod(row.Quantity); break;
                case "enemy_next_action_multihit": action.EnemyMultiHitMod = row.Quantity.ToString("0"); break;
                case "enemy_next_action_amp": action.EnemyAmpMod = FormatPercentMod(row.Quantity); break;
                case "hero_stat_bonus":
                    action.StatBonuses ??= new List<StatBonusEntry>();
                    action.StatBonuses.Add(new StatBonusEntry
                    {
                        Value = (int)row.Quantity,
                        Type = StatSubTypeToEntryType(row.StatSubType)
                    });
                    break;
                case "heal": action.HealAmount = (int)row.Quantity; break;
                case "max_health": action.MaxHealthIncrease = (int)row.Quantity; break;
            }
        }

        private static void ApplyStatusToFlatField(ActionData action, CadenceMechanicRow row)
        {
            string id = ActionMechanicsRegistry.NormalizeMechanicId(row.MechanicId);
            switch (id)
            {
                case "weaken": action.CausesWeaken = row.Quantity != 0; break;
                case "slow": action.CausesSlow = row.Quantity != 0; break;
                case "vulnerability": action.CausesVulnerability = row.Quantity != 0; break;
                case "harden": action.CausesHarden = row.Quantity != 0; break;
                case "focus": action.CausesFocus = row.Quantity != 0; break;
                case "confuse": action.CausesConfusion = row.Quantity != 0; break;
                case "stat_drain": action.CausesStatDrain = row.Quantity != 0; break;
                case "fortify":
                    action.CausesFortify = row.Quantity != 0;
                    action.FortifyArmorPerStack = (int)Math.Max(1, row.Quantity);
                    break;
                case "disrupt": action.CausesDisrupt = row.Quantity != 0; break;
                case "pierce": action.CausesPierce = row.Quantity != 0; break;
            }
        }

        private static void ClearCadenceFlatFields(ActionData action)
        {
            action.Cadence = "";
            action.ComboBonusDuration = 0;
            action.RollBonus = 0;
            action.HitThresholdAdjustment = 0;
            action.ComboThresholdAdjustment = 0;
            action.CriticalHitThresholdAdjustment = 0;
            action.CriticalMissThresholdAdjustment = 0;
            action.EnemyRollBonus = 0;
            action.EnemyHitThresholdAdjustment = 0;
            action.EnemyComboThresholdAdjustment = 0;
            action.EnemyCriticalHitThresholdAdjustment = 0;
            action.EnemyCriticalMissThresholdAdjustment = 0;
            action.SpeedMod = "";
            action.DamageMod = "";
            action.MultiHitMod = "";
            action.AmpMod = "";
            action.EnemySpeedMod = "";
            action.EnemyDamageMod = "";
            action.EnemyMultiHitMod = "";
            action.EnemyAmpMod = "";
            action.StatBonuses = new List<StatBonusEntry>();
            action.HealAmount = 0;
            action.MaxHealthIncrease = 0;
            action.CausesWeaken = false;
            action.CausesSlow = false;
            action.CausesVulnerability = false;
            action.CausesHarden = false;
            action.CausesFocus = false;
            action.CausesConfusion = false;
            action.CausesStatDrain = false;
            action.CausesFortify = false;
            action.FortifyArmorPerStack = 0;
            action.CausesDisrupt = false;
            action.CausesPierce = false;
        }

        private static void RebuildMechanicsList(ActionData action)
        {
            var row = new SpreadsheetActionData { Cadence = action.Cadence, Duration = action.ComboBonusDuration.ToString() };
            CopyFlatToSpreadsheetRow(action, row);
            action.Mechanics = ActionMechanicsRegistry.FilterForMechanicsColumn(
                ActionMechanicsRegistry.DetectFromSpreadsheetRow(row));
        }

        private static void CopyFlatToSpreadsheetRow(ActionData action, SpreadsheetActionData row)
        {
            row.HeroAccuracy = action.RollBonus != 0 ? action.RollBonus.ToString() : "";
            row.HeroHit = action.HitThresholdAdjustment != 0 ? action.HitThresholdAdjustment.ToString() : "";
            row.HeroCombo = action.ComboThresholdAdjustment != 0 ? action.ComboThresholdAdjustment.ToString() : "";
            row.HeroCrit = action.CriticalHitThresholdAdjustment != 0 ? action.CriticalHitThresholdAdjustment.ToString() : "";
            row.HeroCritMiss = action.CriticalMissThresholdAdjustment != 0 ? action.CriticalMissThresholdAdjustment.ToString() : "";
            row.SpeedMod = action.SpeedMod ?? "";
            row.DamageMod = action.DamageMod ?? "";
            row.MultiHitMod = action.MultiHitMod ?? "";
            row.AmpMod = action.AmpMod ?? "";
            row.Weaken = action.CausesWeaken ? "1" : "";
            row.Slow = action.CausesSlow ? "1" : "";
            row.HeroHeal = action.HealAmount > 0 ? action.HealAmount.ToString() : "";
            row.HeroHealMaxHealth = action.MaxHealthIncrease > 0 ? action.MaxHealthIncrease.ToString() : "";
        }

        private static bool IsStatusMechanic(string mechanicId)
        {
            string id = ActionMechanicsRegistry.NormalizeMechanicId(mechanicId);
            return id is "weaken" or "slow" or "vulnerability" or "harden" or "focus" or "confuse"
                or "stat_drain" or "fortify" or "disrupt" or "pierce";
        }

        private static string ResolveGroupCadence(ActionAttackBonusGroup group)
        {
            if (!string.IsNullOrWhiteSpace(group.DurationType)
                && !CadenceKeywords.IsKeywordCadence(group.DurationType))
                return group.DurationType;
            return string.IsNullOrWhiteSpace(group.CadenceType) ? group.Keyword : group.CadenceType;
        }

        private static string ToEditorCadence(string cadence)
        {
            string n = CadenceKeywords.Normalize(cadence);
            return n switch
            {
                CadenceKeywords.Turn => "Turn",
                CadenceKeywords.Action => "Action",
                "FIGHT" => "Fight",
                "DUNGEON" => "Dungeon",
                _ => ActionFormCadenceToEditor(cadence)
            };
        }

        private static string ActionFormCadenceToEditor(string? cadence)
        {
            if (string.IsNullOrWhiteSpace(cadence))
                return "Turn";
            string u = cadence.Trim();
            if (string.Equals(u, "TURN", StringComparison.OrdinalIgnoreCase)) return "Turn";
            if (string.Equals(u, "ACTION", StringComparison.OrdinalIgnoreCase)) return "Action";
            if (string.Equals(u, "FIGHT", StringComparison.OrdinalIgnoreCase)) return "Fight";
            if (string.Equals(u, "DUNGEON", StringComparison.OrdinalIgnoreCase)) return "Dungeon";
            return u;
        }

        private static string EditorCadenceToStorage(string cadence)
        {
            return cadence?.Trim() switch
            {
                "Turn" => "Turn",
                "Action" => "Action",
                "Fight" => "Fight",
                "Dungeon" => "Dungeon",
                _ => cadence ?? ""
            };
        }

        private static string FormatPercentMod(double quantity) =>
            quantity.ToString(quantity % 1 == 0 ? "0" : "0.##", System.Globalization.CultureInfo.InvariantCulture);

        private static string StatSubTypeToEntryType(string statSubType) =>
            statSubType.Trim().ToUpperInvariant() switch
            {
                "STR" => "Strength",
                "AGI" => "Agility",
                "TECH" => "Technique",
                "INT" => "Intelligence",
                _ => statSubType
            };
    }
}
