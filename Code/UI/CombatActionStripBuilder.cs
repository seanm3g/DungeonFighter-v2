using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Actions;
using RPGGame.Actions.RollModification;
using RPGGame.Data;
using RPGGame.Items.Helpers;
using RPGGame.UI.Avalonia.Managers;

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
        /// <summary>Total roll bonus for this combo slot (same basis as hover tooltip / <see cref="CombatCalculator.CalculateRollBonus"/>).</summary>
        public int AccuracyRollBonus { get; }
        /// <summary>Effective number of damage ticks (base multi-hit + consumed mod + chain position), same basis as combat execution.</summary>
        public int EffectiveMultiHitCount { get; }

        public ActionPanelInfo(string name, double damageBase, double damageModified, double speedBase, double speedModified, string thresholdText, int accuracyRollBonus, int effectiveMultiHitCount)
        {
            Name = name ?? "";
            DamageBase = damageBase;
            DamageModified = damageModified;
            SpeedBase = speedBase;
            SpeedModified = speedModified;
            ThresholdText = thresholdText ?? "";
            AccuracyRollBonus = accuracyRollBonus;
            EffectiveMultiHitCount = Math.Max(1, effectiveMultiHitCount);
        }
    }

    /// <summary>
    /// Builds the list of lines for the combat action-info strip: abilities with effective accuracy,
    /// current thresholds, and status effects each action causes. Recomputed each render for dynamic updates.
    /// </summary>
    public static partial class CombatActionStripBuilder
    {
        /// <summary>
        /// Resolves damage % and speed % shown on strip cards and matching hover swing line for <paramref name="mode"/>.
        /// Effective mode: damage is slot-modified % × combo amplification for the action's strip index (see combat raw damage pipeline).
        /// </summary>
        public static void GetStripSwingDisplayPercents(
            in ActionPanelInfo info,
            Character character,
            Action action,
            ActionStripDamageLineMode mode,
            out double damagePercentForDisplay,
            out double speedPercentForDisplay)
        {
            if (mode == ActionStripDamageLineMode.BaseIntrinsic)
            {
                damagePercentForDisplay = info.DamageBase;
                speedPercentForDisplay = info.SpeedBase;
                return;
            }

            double amp = ActionUtilities.CalculateDamageMultiplier(character, action);
            damagePercentForDisplay = info.DamageModified * amp;
            speedPercentForDisplay = info.SpeedModified;
        }

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

                // Pending bonuses for this slot (peek, do not consume). FIFO ACTION layers apply to the next N combo slots from ComboStep.
                var slotBonuses = new List<ActionAttackBonusItem>(character.Effects.GetPendingActionBonusesForSlot(i));
                int actionCount = actions.Count;
                if (actionCount > 0)
                {
                    int currentStep = character.ComboStep % actionCount;
                    int fifoLayers = character.Effects.GetPendingActionCadenceLayerCount();
                    for (int layer = 0; layer < fifoLayers; layer++)
                    {
                        int targetSlot = (currentStep + layer) % actionCount;
                        if (i == targetSlot)
                            slotBonuses.AddRange(character.Effects.PeekPendingActionCadenceLayerAt(layer));
                    }
                }
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

                int accuracyRollBonus = CombatCalculator.CalculateRollBonus(character, action, actions, i, consumeTempBonus: false);

                int effectiveHits = RollModificationManager.GetEffectiveMultiHitCountForModifierScaling(action, character, i);

                list.Add(new ActionPanelInfo(name, baseDamagePct, modifiedDamagePct, baseSpeedPct, modifiedSpeedPct, thresholdText, accuracyRollBonus, effectiveHits));
            }
            return list;
        }

        /// <summary>
        /// Compact damage line for the action strip and matching tooltip swing segment: multihit uses <c>2x50% damage</c>; single hit uses <c>Dmg 50%</c>.
        /// </summary>
        public static string FormatSwingDamageLine(int effectiveHitCount, double damagePercentForDisplay)
        {
            int hits = Math.Max(1, effectiveHitCount);
            return hits > 1
                ? $"{hits}x{damagePercentForDisplay:F0}% damage"
                : $"Dmg {damagePercentForDisplay:F0}%";
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
        /// Opener / finisher lines shown on action strip cards and in hover tooltips (same order as mechanical segments).
        /// </summary>
        public static List<string> BuildActionStripComboRoleLines(Character? character, Action? action)
        {
            var lines = new List<string>();
            if (action == null)
                return lines;
            if (action.ComboRouting?.IsOpener == true)
                lines.Add("Opener — first combo slot.");
            if (action.ComboRouting?.IsFinisher == true)
                lines.Add("Finisher — last combo slot.");
            return lines;
        }

        /// <summary>
        /// Single-line Type | Target | combo metadata (same text as hover tooltip segment).
        /// </summary>
        public static string BuildActionStripMetadataLine(Action? action) =>
            action == null ? "" : BuildActionMetadataLine(action);

        /// <summary>
        /// Extra modifier lines for compact action strip cards after the swing (Dmg/Spd) line: total roll bonus when non-zero,
        /// deferred sheet accuracy (and related lines) so cards match tooltip behavior, compact stat bonuses, and cadence bonus groups.
        /// When sheet accuracy applies on the <em>current</em> roll it is already included in <paramref name="rollBonusThisSwing"/>; that hero line is omitted to avoid duplication.
        /// </summary>
        public static List<string> BuildActionStripModifierTailLines(Action? action, int rollBonusThisSwing, int maxWidth, int maxLines)
        {
            var lines = new List<string>();
            if (action == null || maxLines <= 0 || maxWidth < 4)
                return lines;

            bool deferSheet = Action.DefersSheetCombatPackagesToNextHeroRoll(action);

            void add(string? s)
            {
                if (string.IsNullOrWhiteSpace(s) || lines.Count >= maxLines)
                    return;
                s = s.Trim();
                string t = s.Length > maxWidth ? s.Substring(0, Math.Max(1, maxWidth - 3)) + "..." : s;
                lines.Add(t);
            }

            if (rollBonusThisSwing != 0)
                add($"Acc {rollBonusThisSwing:+0;-0;0}");

            foreach (var line in EnumerateSheetAccuracyModifierLines(action))
            {
                if (!deferSheet && line.StartsWith("Hero accuracy", StringComparison.Ordinal))
                    continue;
                add(line);
            }

            var adv = action.Advanced;
            if (adv != null)
            {
                if (adv.StatBonuses != null && adv.StatBonuses.Count > 0)
                {
                    var parts = adv.StatBonuses
                        .Where(s => s != null && (s.Value != 0 || !string.IsNullOrWhiteSpace(s.Type)))
                        .Select(s => $"{s.Type} {FormatSignedValue(s.Value)}")
                        .ToList();
                    if (parts.Count > 0)
                        add($"Stats: {string.Join(", ", parts)}");
                }
                else if (adv.StatBonus != 0 && !string.IsNullOrWhiteSpace(adv.StatBonusType))
                    add($"Stats: {adv.StatBonusType} {FormatSignedValue(adv.StatBonus)}");
            }

            if (action.ActionAttackBonuses?.BonusGroups != null)
            {
                foreach (var group in action.ActionAttackBonuses.BonusGroups)
                {
                    if (ShouldOmitAbilityCadenceBonusGroup(group))
                        continue;
                    if (lines.Count >= maxLines)
                        break;
                    if (group?.Bonuses == null || group.Bonuses.Count == 0)
                        continue;
                    string items = FormatBonusItemsShort(group.Bonuses);
                    if (string.IsNullOrEmpty(items))
                        continue;
                    string cad = string.IsNullOrWhiteSpace(group.CadenceType)
                        ? (string.IsNullOrWhiteSpace(group.Keyword) ? "BONUS" : group.Keyword)
                        : group.CadenceType;
                    int displayCount = ActionCadenceDurationResolver.GetDisplayCount(action, group);
                    string count = displayCount > 1 ? $" x{displayCount}" : "";
                    add($"{cad}{count}: {items}");
                }
            }

            return lines;
        }

        /// <summary>
        /// Single-line summary for inventory rows (pipe-separated). Tooltips use <see cref="BuildMechanicalDetailSegments"/> one segment per line.
        /// </summary>
        public static string BuildActionMechanicalModSummary(Character? character, Action? action, int comboSlotIndex, ActionStripDamageLineMode swingLineMode = ActionStripDamageLineMode.EffectiveWithComboAmp)
        {
            if (action == null)
                return "";
            return string.Join(" | ", BuildMechanicalDetailSegments(character, action, comboSlotIndex, swingLineMode));
        }
    }
}
