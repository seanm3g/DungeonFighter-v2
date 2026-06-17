using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Actions.RollModification;
using RPGGame.Data;

namespace RPGGame
{
    public static partial class CombatActionStripBuilder
    {
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

            for (int slotIndex = 0; slotIndex < actions.Count; slotIndex++)
            {
                if (remainingLines <= 0) break;
                var action = actions[slotIndex];
                // Per-slot combo step so chain-position accuracy updates when the sequence is reordered.
                int acc = CombatCalculator.CalculateRollBonus(character, action, actions, slotIndex, consumeTempBonus: false);
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

            // Pending ACTION bonuses for the next hero attack roll (combo-step-independent); FIFO layers
            var nextRollBonuses = character.Effects.PeekPendingActionBonusesNextHeroRoll();
            int layerCount = character.Effects.GetPendingActionCadenceLayerCount();
            if (nextRollBonuses.Count > 0)
            {
                string nextRollDesc = FormatBonusItemsShort(nextRollBonuses);
                if (!string.IsNullOrEmpty(nextRollDesc))
                {
                    string prefix = layerCount > 1 ? $"Next roll ({layerCount}): " : "Next roll: ";
                    lines.Add(prefix + nextRollDesc);
                }
            }
            // Legacy slot-only ACTION pending (tests / tooling)
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
                "CRIT_MISS" => $"{sign}{b.Value:0} CRIT MISS",
                RollModificationManager.SetCriticalMissThresholdType => $"Crit miss={b.Value:0}",
                RollModificationManager.SetCriticalHitThresholdType => $"Crit={b.Value:0}",
                RollModificationManager.SetComboThresholdType => $"Combo={b.Value:0}",
                RollModificationManager.SetHitThresholdType => $"Hit={b.Value:0}",
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
