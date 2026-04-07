using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Actions.RollModification;
using RPGGame.Data;
using RPGGame.Items.Helpers;

namespace RPGGame
{
    /// <summary>
    /// Per-action data for one panel in the action strip (left-to-right layout).
    /// </summary>
    public readonly struct ActionPanelInfo
    {
        public string Name { get; }
        /// <summary>Damage as % of character base damage (DamageMultiplier × 100), before slot DAMAGE_MOD.</summary>
        public double DamageBase { get; }
        /// <summary>Effective damage % after pending slot DAMAGE_MOD (same basis as DamageBase).</summary>
        public double DamageModified { get; }
        /// <summary>Intrinsic action speed % (same as action details: 100 / Length), before slot SPEED_MOD.</summary>
        public double SpeedBase { get; }
        /// <summary>Effective speed % after pending slot SPEED_MOD (higher = faster).</summary>
        public double SpeedModified { get; }
        public string ThresholdText { get; }

        public ActionPanelInfo(string name, double damageBase, double damageModified, double speedBase, double speedModified, string thresholdText)
        {
            Name = name ?? "";
            DamageBase = damageBase;
            DamageModified = damageModified;
            SpeedBase = speedBase;
            SpeedModified = speedModified;
            ThresholdText = thresholdText ?? "";
        }
    }

    /// <summary>
    /// Builds the list of lines for the combat action-info strip: abilities with effective accuracy,
    /// current thresholds, and status effects each action causes. Recomputed each render for dynamic updates.
    /// </summary>
    public static class CombatActionStripBuilder
    {
        /// <summary>
        /// Builds per-action panel data for the action strip (one panel per combo action, left to right).
        /// Used to render individual panels with selection highlight. Selected index is character.ComboStep % count.
        /// </summary>
        /// <param name="character">Current player character (combat actor).</param>
        /// <returns>List of panel info (name, damage, speed, thresholds) in combo order; empty if null or no actions.</returns>
        public static List<ActionPanelInfo> BuildPanelData(Character? character)
        {
            if (character == null)
                return new List<ActionPanelInfo>();

            var actions = character.GetComboActions();
            if (actions == null || actions.Count == 0)
                return new List<ActionPanelInfo>();

            var list = new List<ActionPanelInfo>(actions.Count);
            for (int i = 0; i < actions.Count; i++)
            {
                var action = actions[i];
                string name = action.Name ?? "";

                // Damage line: multiplier as % of character base (matches Spd line style), not raw HP output.
                double baseDamagePct = action.DamageMultiplier * 100.0;

                // Pending bonuses for this slot (peek, do not consume)
                var slotBonuses = character.Effects.GetPendingActionBonusesForSlot(i);
                double damageModPercent = 0;
                double speedModPercent = 0;
                foreach (var b in slotBonuses)
                {
                    switch ((b.Type ?? "").ToUpper())
                    {
                        case "DAMAGE_MOD": damageModPercent += b.Value; break;
                        case "SPEED_MOD": speedModPercent += b.Value; break;
                    }
                }

                double modifiedDamagePct = damageModPercent != 0
                    ? baseDamagePct * (1.0 + damageModPercent / 100.0)
                    : baseDamagePct;

                // Speed % matches action details (ActionDisplayFormatter), not wall-clock seconds.
                double baseSpeedPct = action.Length > 0
                    ? ActionDisplayFormatter.CalculateActionSpeedPercentage(action)
                    : 0;
                double modifiedSpeedPct = speedModPercent != 0
                    ? baseSpeedPct * (1.0 + speedModPercent / 100.0)
                    : baseSpeedPct;

                string thresholdText = GetThresholdText(action);

                list.Add(new ActionPanelInfo(name, baseDamagePct, modifiedDamagePct, baseSpeedPct, modifiedSpeedPct, thresholdText));
            }
            return list;
        }

        private static string GetThresholdText(Action action)
        {
            if (action?.RollMods == null) return "";
            var rm = action.RollMods;
            var parts = new List<string>();
            if (rm.HitThresholdAdjustment != 0) parts.Add($"H:{rm.HitThresholdAdjustment:+0;-0;0}");
            if (rm.ComboThresholdAdjustment != 0) parts.Add($"C:{rm.ComboThresholdAdjustment:+0;-0;0}");
            if (rm.CriticalHitThresholdAdjustment != 0) parts.Add($"Cr:{rm.CriticalHitThresholdAdjustment:+0;-0;0}");
            if (rm.CriticalMissThresholdAdjustment != 0) parts.Add($"Cm:{rm.CriticalMissThresholdAdjustment:+0;-0;0}");
            return parts.Count == 0 ? "" : string.Join(" ", parts);
        }

        /// <summary>
        /// Builds display lines for the action strip. Header with thresholds, then one line per action
        /// (name, effective acc, causes). Truncates to fit maxWidth and caps at maxLines.
        /// </summary>
        /// <param name="character">Current player character (combat actor).</param>
        /// <param name="maxWidth">Maximum characters per line (e.g. strip content width).</param>
        /// <param name="maxLines">Maximum number of lines to return (e.g. strip content height).</param>
        /// <returns>Lines to render in the strip.</returns>
        public static List<string> BuildLines(Character character, int maxWidth = 80, int maxLines = 8)
        {
            if (character == null)
                return new List<string>();

            var lines = new List<string>();
            var actions = character.GetComboActions();
            if (actions == null || actions.Count == 0)
            {
                lines.Add("(No abilities)");
                return TruncateLines(lines, maxWidth, maxLines);
            }

            var tm = RollModificationManager.GetThresholdManager();
            int hit = tm.GetHitThreshold(character);
            int combo = tm.GetComboThreshold(character);
            int crit = tm.GetCriticalHitThreshold(character);
            lines.Add($"H:{hit} C:{combo} Cr:{crit}");
            int remainingLines = Math.Max(0, maxLines - 1);

            foreach (var action in actions)
            {
                if (remainingLines <= 0) break;
                int acc = ActionUtilities.CalculateRollBonus(character, action, consumeTempBonus: false);
                string causes = GetCausesShort(action);
                string name = action.Name ?? "";
                string line = string.IsNullOrEmpty(causes)
                    ? $"{name} | Acc {acc:+0;-0;0}"
                    : $"{name} | Acc {acc:+0;-0;0} | {causes}";
                if (line.Length > maxWidth)
                    line = line.Substring(0, maxWidth - 3) + "...";
                lines.Add(line);
                remainingLines--;
            }

            return TruncateLines(lines, maxWidth, maxLines);
        }

        private static List<string> TruncateLines(List<string> lines, int maxWidth, int maxLines)
        {
            var result = new List<string>();
            foreach (var line in lines.Take(maxLines))
            {
                result.Add(line.Length <= maxWidth ? line : line.Substring(0, maxWidth - 3) + "...");
            }
            return result;
        }

        /// <summary>
        /// Builds display lines for active action/attack modifiers during combat.
        /// Examples: "Next Action 2: +3 COMBO, 2x DMG", "Next 3 Attacks: +1 HIT", "STR +1 (Enemy)".
        /// </summary>
        /// <param name="character">Current player character (combat actor).</param>
        /// <returns>Lines describing active modifiers; empty if none.</returns>
        public static List<string> BuildActiveModifierLines(Character? character)
        {
            if (character?.Effects == null)
                return new List<string>();

            var lines = new List<string>();

            // Pending ACTION bonuses by slot (e.g. "Next Action 2: +3 COMBO, 2x DMG")
            foreach (var slot in character.Effects.GetPendingActionBonusSlots().OrderBy(s => s))
            {
                var bonuses = character.Effects.GetPendingActionBonusesForSlot(slot);
                if (bonuses.Count == 0) continue;
                string desc = FormatBonusItemsShort(bonuses);
                if (string.IsNullOrEmpty(desc)) continue;
                int displaySlot = slot + 1; // 1-based for UI
                lines.Add($"Next Action {displaySlot}: {desc}");
            }

            // ATTACK cadence bonuses (e.g. "Next 3 Attacks: +1 HIT")
            foreach (var group in character.Effects.AttackBonuses ?? new List<ActionAttackBonusGroup>())
            {
                if (group.Bonuses == null || group.Bonuses.Count == 0) continue;
                string desc = FormatBonusItemsShort(group.Bonuses);
                if (string.IsNullOrEmpty(desc)) continue;
                string label = group.Count > 1 ? $"Next {group.Count} Attacks" : "Next Attack";
                lines.Add($"{label}: {desc}");
            }

            // ABILITY cadence bonuses (e.g. "STR +1 (Enemy)")
            foreach (var group in character.Effects.AbilityBonuses ?? new List<ActionAttackBonusGroup>())
            {
                if (group.Bonuses == null || group.Bonuses.Count == 0) continue;
                string desc = FormatBonusItemsShort(group.Bonuses);
                if (string.IsNullOrEmpty(desc)) continue;
                string scope = string.IsNullOrEmpty(group.DurationType) ? "" : $" ({group.DurationType})";
                lines.Add($"{desc}{scope}");
            }

            return lines;
        }

        private static string FormatBonusItemsShort(List<ActionAttackBonusItem> bonuses)
        {
            if (bonuses == null || bonuses.Count == 0) return "";
            var parts = new List<string>();
            foreach (var b in bonuses)
            {
                string part = FormatBonusItemShort(b);
                if (!string.IsNullOrEmpty(part)) parts.Add(part);
            }
            return string.Join(", ", parts);
        }

        private static string FormatBonusItemShort(ActionAttackBonusItem b)
        {
            if (b == null) return "";
            string sign = b.Value >= 0 ? "+" : "";
            return b.Type switch
            {
                "ACCURACY" => $"{sign}{b.Value:0} ACC",
                "HIT" => $"{sign}{b.Value:0} HIT",
                "COMBO" => $"{sign}{b.Value:0} COMBO",
                "CRIT" => $"{sign}{b.Value:0} CRIT",
                "STR" => $"STR {sign}{b.Value:0}",
                "AGI" => $"AGI {sign}{b.Value:0}",
                "TECH" => $"TECH {sign}{b.Value:0}",
                "INT" => $"INT {sign}{b.Value:0}",
                "DAMAGE_MOD" when b.Value >= 0 => b.Value >= 100 ? $"{(b.Value / 100.0):0.#}x DMG" : $"{sign}{b.Value:0}% DMG",
                "DAMAGE_MOD" => $"{b.Value:0}% DMG",
                "SPEED_MOD" => $"{sign}{b.Value:0}% SPD",
                "MULTIHIT_MOD" => $"{sign}{b.Value:0} MH",
                "AMP_MOD" => $"{sign}{b.Value:0}% AMP",
                _ => $"{sign}{b.Value:0} {b.Type}"
            };
        }

        /// <summary>
        /// Builds wrapped lines for the action strip hover tooltip (full description, stats, accuracy, panel summary).
        /// </summary>
        /// <param name="character">Player whose combo is shown.</param>
        /// <param name="panelIndex">0-based combo slot.</param>
        /// <param name="maxWidth">Maximum characters per line (inner width).</param>
        /// <param name="maxLines">Cap on total lines (excluding hard truncation).</param>
        public static List<string> BuildActionTooltipLines(Character? character, int panelIndex, int maxWidth, int maxLines = 18)
        {
            var result = new List<string>();
            if (character == null || panelIndex < 0 || maxWidth < 4)
                return result;

            var actions = character.GetComboActions();
            if (actions == null || panelIndex >= actions.Count)
                return result;

            var action = actions[panelIndex];
            void AddWrapped(string? paragraph)
            {
                if (result.Count >= maxLines || string.IsNullOrWhiteSpace(paragraph))
                    return;
                foreach (var line in WrapTextToLines(paragraph.Trim(), maxWidth))
                {
                    if (result.Count >= maxLines) break;
                    result.Add(line);
                }
            }

            result.Add(string.IsNullOrEmpty(action.Name) ? "?" : action.Name);
            if (result.Count >= maxLines) return result;

            AddWrapped(action.Description);
            AddWrapped(ActionDisplayFormatter.GetActionStats(action).Trim());

            int acc = ActionUtilities.CalculateRollBonus(character, action, consumeTempBonus: false);
            AddWrapped($"Accuracy (roll bonus): {acc:+0;-0;0}");

            string causes = GetCausesShort(action);
            if (!string.IsNullOrEmpty(causes))
                AddWrapped($"Status: {causes}");

            var panelRow = BuildPanelData(character);
            if (panelIndex < panelRow.Count)
            {
                var info = panelRow[panelIndex];
                AddWrapped($"Effective: Dmg {info.DamageModified:F0}% of base | Spd {info.SpeedModified:F0}%");
                if (!string.IsNullOrEmpty(info.ThresholdText))
                    AddWrapped($"Roll thresholds: {info.ThresholdText}");
            }

            return result;
        }

        /// <summary>
        /// Tooltip lines for an action that may or may not be in the current combo (e.g. pool row on the inventory right panel).
        /// When the action is already in the sequence, delegates to <see cref="BuildActionTooltipLines"/> for full panel metrics.
        /// </summary>
        public static List<string> BuildActionTooltipLinesForAction(Character? character, Action? action, int maxWidth, int maxLines = 18)
        {
            var result = new List<string>();
            if (character == null || action == null || maxWidth < 4)
                return result;

            var combo = character.GetComboActions();
            for (int i = 0; i < combo.Count; i++)
            {
                if (ReferenceEquals(combo[i], action))
                    return BuildActionTooltipLines(character, i, maxWidth, maxLines);
            }

            void AddWrapped(string? paragraph)
            {
                if (result.Count >= maxLines || string.IsNullOrWhiteSpace(paragraph))
                    return;
                foreach (var line in WrapTextToLines(paragraph.Trim(), maxWidth))
                {
                    if (result.Count >= maxLines) break;
                    result.Add(line);
                }
            }

            result.Add(string.IsNullOrEmpty(action.Name) ? "?" : action.Name);
            if (result.Count >= maxLines) return result;

            AddWrapped(action.Description);
            AddWrapped(ActionDisplayFormatter.GetActionStats(action).Trim());

            int acc = ActionUtilities.CalculateRollBonus(character, action, consumeTempBonus: false);
            AddWrapped($"Accuracy (roll bonus): {acc:+0;-0;0}");

            string causes = GetCausesShort(action);
            if (!string.IsNullOrEmpty(causes))
                AddWrapped($"Status: {causes}");

            return result;
        }

        /// <summary>
        /// Word-wraps a single paragraph to fixed-width lines.
        /// </summary>
        internal static List<string> WrapTextToLines(string text, int maxWidth)
        {
            var lines = new List<string>();
            if (string.IsNullOrEmpty(text) || maxWidth < 1)
                return lines;

            var words = text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            var current = new System.Text.StringBuilder();
            foreach (var word in words)
            {
                if (word.Length > maxWidth)
                {
                    if (current.Length > 0)
                    {
                        lines.Add(current.ToString());
                        current.Clear();
                    }
                    for (int o = 0; o < word.Length; o += maxWidth)
                        lines.Add(word.Substring(o, Math.Min(maxWidth, word.Length - o)));
                    continue;
                }

                int extra = current.Length == 0 ? word.Length : word.Length + 1;
                if (current.Length + extra > maxWidth)
                {
                    lines.Add(current.ToString());
                    current.Clear();
                }
                if (current.Length > 0)
                    current.Append(' ');
                current.Append(word);
            }
            if (current.Length > 0)
                lines.Add(current.ToString());
            return lines;
        }

        private static string GetCausesShort(Action action)
        {
            if (action == null) return "";
            var parts = new List<string>();
            if (action.CausesWeaken) parts.Add("Weaken");
            if (action.CausesStun) parts.Add("Stun");
            if (action.CausesBleed) parts.Add("Bleed");
            if (action.CausesPoison) parts.Add("Poison");
            if (action.CausesBurn) parts.Add("Burn");
            if (action.CausesSlow) parts.Add("Slow");
            if (action.CausesVulnerability) parts.Add("Vuln");
            if (action.CausesHarden) parts.Add("Harden");
            if (action.CausesFortify) parts.Add("Fortify");
            if (action.CausesFocus) parts.Add("Focus");
            if (action.CausesExpose) parts.Add("Expose");
            if (action.CausesHPRegen) parts.Add("Regen");
            if (action.CausesArmorBreak) parts.Add("ArmBrk");
            if (action.CausesPierce) parts.Add("Pierce");
            if (action.CausesReflect) parts.Add("Reflect");
            if (action.CausesSilence) parts.Add("Silence");
            if (action.CausesAbsorb) parts.Add("Absorb");
            if (action.CausesTemporaryHP) parts.Add("TempHP");
            if (action.CausesConfusion) parts.Add("Confuse");
            if (action.CausesCleanse) parts.Add("Cleanse");
            if (action.CausesMark) parts.Add("Mark");
            if (action.CausesDisrupt) parts.Add("Disrupt");
            if (action.CausesStatDrain) parts.Add("Drain");
            return parts.Count == 0 ? "" : string.Join(" ", parts);
        }
    }
}
