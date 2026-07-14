using System;
using System.Collections.Generic;
using System.Globalization;
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
        /// <summary>Effective number of damage ticks for strip preview (base multi-hit + pending ACTION cadence + chain), excluding redeemed Consumed* so cards reset after the swing.</summary>
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
        /// Resolves damage % and speed % factors for <paramref name="mode"/> (used for color diffs and to derive flat/seconds display).
        /// Effective mode: damage is slot-modified % × strip amp (TECH combo-slot multiplier + pending sheet AMP_MOD).
        /// </summary>
        public static void GetStripSwingDisplayPercents(
            in ActionPanelInfo info,
            Character character,
            Action action,
            ActionStripDamageLineMode mode,
            out double damagePercentForDisplay,
            out double speedPercentForDisplay,
            int comboSlotIndex = -1)
        {
            if (mode == ActionStripDamageLineMode.BaseIntrinsic)
            {
                damagePercentForDisplay = info.DamageBase;
                speedPercentForDisplay = info.SpeedBase;
                return;
            }

            double amp = GetStripSwingDisplayAmp(character, action, comboSlotIndex);
            damagePercentForDisplay = info.DamageModified * amp;
            speedPercentForDisplay = info.SpeedModified;
        }

        /// <summary>
        /// Resolves flat damage and wall-clock seconds shown on strip cards / hover swing line for <paramref name="mode"/>.
        /// </summary>
        public static void GetStripSwingDisplayValues(
            in ActionPanelInfo info,
            Character character,
            Action action,
            ActionStripDamageLineMode mode,
            out int damageForDisplay,
            out double secondsForDisplay,
            int comboSlotIndex = -1)
        {
            GetStripSwingDisplayPercents(in info, character, action, mode, out double damagePercent, out double speedPercent, comboSlotIndex);
            damageForDisplay = ConvertDamagePercentToFlat(character, action, damagePercent);
            secondsForDisplay = ConvertSpeedPercentToSeconds(character, action, info.SpeedBase, speedPercent);
        }

        /// <summary>
        /// Hero base attack × sheet/mod/amp percent factors, plus early-game starting-action and class weapon multipliers
        /// (same legs as <see cref="Combat.Calculators.DamageCalculator.CalculateRawDamage"/> without consuming next-attack state or applying roll bands).
        /// </summary>
        public static int ConvertDamagePercentToFlat(Character character, Action action, double damagePercentForDisplay)
        {
            int baseDamage = character.Combat.GetTotalDamage();
            if (baseDamage <= 0)
                baseDamage = 1;

            double total = baseDamage * (damagePercentForDisplay / 100.0);
            total *= EarlyGameBalanceHelper.GetStartingActionDamageMultiplier(character, action);
            if (character.Weapon is WeaponItem classWeapon)
                total *= ClassBalanceHelper.GetDamageMultiplier(classWeapon.WeaponType);

            int result = (int)total;
            int maxCap = Math.Max(1, GameConfiguration.Instance.Combat.MaximumDamageCap);
            if (result > maxCap)
                result = maxCap;
            if (result <= 0)
                result = 1;
            return result;
        }

        /// <summary>
        /// Wall-clock swing time: <see cref="Character.GetTotalAttackSpeed"/> × action length, scaled by speed-% ratio
        /// so pending SPEED_MOD matches <see cref="Combat.Formatting.ActionSpeedCalculator"/> (higher % ⇒ shorter seconds).
        /// </summary>
        public static double ConvertSpeedPercentToSeconds(
            Character character,
            Action action,
            double intrinsicSpeedPercent,
            double speedPercentForDisplay)
        {
            if (action == null || action.Length <= 0)
                return 0;

            double attackSpeed = character.GetTotalAttackSpeed();
            double length = action.Length;
            if (intrinsicSpeedPercent > 0 && speedPercentForDisplay > 0
                && Math.Abs(speedPercentForDisplay - intrinsicSpeedPercent) > 0.0001)
            {
                length = length * (intrinsicSpeedPercent / speedPercentForDisplay);
            }

            return attackSpeed * length;
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

                // Pending bonuses for this slot (peek, do not consume). ACTION bank sticks to preview slot.
                var slotBonuses = new List<ActionAttackBonusItem>(character.Effects.GetPendingActionBonusesForSlot(i));
                int actionCount = actions.Count;
                if (actionCount > 0
                    && character.Effects.SlotShowsActionCadenceBank(i, character.ComboStep, actionCount))
                {
                    slotBonuses.AddRange(character.Effects.PeekPendingActionBonusesNextHeroRoll());
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

                // Strip preview: pending ACTION cadence only — not Consumed* from a just-resolved swing.
                int effectiveHits = RollModificationManager.GetEffectiveMultiHitCountForModifierScaling(
                    action, character, i, includeConsumedMods: false);

                list.Add(new ActionPanelInfo(name, baseDamagePct, modifiedDamagePct, baseSpeedPct, modifiedSpeedPct, thresholdText, accuracyRollBonus, effectiveHits));
            }
            return list;
        }

        /// <summary>
        /// Compact damage segment for strip cards: multihit uses <c>2x25 damage</c>; single hit uses <c>25 damage</c>.
        /// </summary>
        public static string FormatSwingDamageLine(int effectiveHitCount, int damageForDisplay)
        {
            int hits = Math.Max(1, effectiveHitCount);
            int dmg = Math.Max(0, damageForDisplay);
            return hits > 1
                ? $"{hits}x{dmg} damage"
                : $"{dmg} damage";
        }

        /// <summary>
        /// Compact speed segment for strip cards: wall-clock seconds (e.g. <c>8.3s</c>).
        /// </summary>
        public static string FormatSwingSpeedLine(double secondsForDisplay)
        {
            if (secondsForDisplay < 0)
                secondsForDisplay = 0;
            return $"{secondsForDisplay.ToString("0.#", CultureInfo.InvariantCulture)}s";
        }

        /// <summary>
        /// Compact damage segment for hover tooltips: multihit uses <c>2x50% damage</c>; single hit uses <c>Dmg 50%</c>.
        /// </summary>
        public static string FormatSwingDamagePercentLine(int effectiveHitCount, double damagePercentForDisplay)
        {
            int hits = Math.Max(1, effectiveHitCount);
            return hits > 1
                ? $"{hits}x{damagePercentForDisplay:F0}% damage"
                : $"Dmg {damagePercentForDisplay:F0}%";
        }

        /// <summary>
        /// Compact speed segment for hover tooltips (e.g. <c>Spd 100%</c>).
        /// </summary>
        public static string FormatSwingSpeedPercentLine(double speedPercentForDisplay) =>
            $"Spd {speedPercentForDisplay:F0}%";

        /// <summary>
        /// Compact amp segment matching combat roll footers (e.g. <c>amp: 1.02x</c>).
        /// </summary>
        public static string FormatSwingAmpLine(double ampForDisplay) =>
            $"amp: {ampForDisplay.ToString("0.00", CultureInfo.InvariantCulture)}x";

        /// <summary>
        /// Resolves the combo-slot amp used for this strip action (TECH baseline^strip index),
        /// then multiplies by pending sheet <c>AMP_MOD</c> for the slot the same way combat stacks consumed amp.
        /// </summary>
        /// <param name="comboSlotIndex">0-based strip index; when negative, resolve by action identity in the combo list.</param>
        public static double GetStripSwingDisplayAmp(Character character, Action action, int comboSlotIndex = -1)
        {
            if (character == null || action == null)
                return 1.0;

            int slot = comboSlotIndex;
            var combo = character.GetComboActions();
            if (slot < 0 && combo != null)
            {
                for (int i = 0; i < combo.Count; i++)
                {
                    if (ReferenceEquals(combo[i], action))
                    {
                        slot = i;
                        break;
                    }
                }
            }

            double slotMult = ActionUtilities.CalculateDamageMultiplier(character, action);
            double ampModPct = PeekAmpModPercentForStripSlot(character, slot);
            if (Math.Abs(ampModPct) < 0.0001)
                return slotMult;

            // Match DamageCalculator.GetDisplayedComboMultiplier / CalculateRawDamage amp-mod path.
            double baseline = slotMult;
            if (action.IsComboAction)
                baseline = Math.Max(baseline, character.GetComboAmplifier());
            return baseline * (1.0 + ampModPct / 100.0);
        }

        /// <summary>
        /// Tooltip breakdown of strip amp: TEC baseline, strip exponent, optional pending sheet AMP_MOD.
        /// </summary>
        public static string FormatSwingAmpCalculationLine(Character character, Action action, int comboSlotIndex = -1)
        {
            if (character == null || action == null)
                return "";

            double baseline = character.GetComboAmplifier();
            var combo = character.GetComboActions() ?? new List<Action>();
            int slot = comboSlotIndex;
            if (slot < 0)
            {
                for (int i = 0; i < combo.Count; i++)
                {
                    if (ReferenceEquals(combo[i], action))
                    {
                        slot = i;
                        break;
                    }
                }
            }

            int exponent = 0;
            if (action.IsComboAction && combo.Count > 0 && slot >= 0)
                exponent = ActionUtilities.GetComboAmplificationExponent(character, action, combo);
            else if (!action.IsComboAction)
                exponent = 0;

            double ampModPct = PeekAmpModPercentForStripSlot(character, slot);
            double finalAmp = GetStripSwingDisplayAmp(character, action, slot);

            string powPart = action.IsComboAction
                ? $"Pow({baseline.ToString("0.00", CultureInfo.InvariantCulture)}, {Math.Max(0, exponent)})"
                : "1.00 (not combo)";

            if (Math.Abs(ampModPct) < 0.0001)
                return $"AMP: {finalAmp.ToString("0.00", CultureInfo.InvariantCulture)}x = {powPart}";

            string sheetPart = ampModPct >= 0
                ? $"+{ampModPct.ToString("0.#", CultureInfo.InvariantCulture)}% sheet"
                : $"{ampModPct.ToString("0.#", CultureInfo.InvariantCulture)}% sheet";
            return $"AMP: {finalAmp.ToString("0.00", CultureInfo.InvariantCulture)}x = {powPart} × ({sheetPart})";
        }

        private static double PeekAmpModPercentForStripSlot(Character character, int comboSlotIndex)
        {
            if (character?.Effects == null || comboSlotIndex < 0)
                return 0;

            double sum = 0;
            foreach (var b in character.Effects.GetPendingActionBonusesForSlot(comboSlotIndex))
            {
                if (string.Equals(b.Type, "AMP_MOD", StringComparison.OrdinalIgnoreCase))
                    sum += b.Value;
            }

            var actions = character.GetComboActions();
            int actionCount = actions?.Count ?? 0;
            if (actionCount > 0
                && character.Effects.SlotShowsActionCadenceBank(comboSlotIndex, character.ComboStep, actionCount))
            {
                foreach (var b in character.Effects.PeekPendingActionBonusesNextHeroRoll())
                {
                    if (string.Equals(b.Type, "AMP_MOD", StringComparison.OrdinalIgnoreCase))
                        sum += b.Value;
                }
            }

            return sum;
        }

        /// <summary>
        /// Full swing readout for strip cards: <c>25 damage | 8.3s</c> (or multihit <c>2x25 damage | …</c>).
        /// Combo amp is baked into the damage number; the separate amp label is omitted on cards.
        /// </summary>
        public static string FormatStripSwingLine(
            in ActionPanelInfo info,
            Character character,
            Action action,
            ActionStripDamageLineMode mode,
            int comboSlotIndex = -1)
        {
            GetStripSwingDisplayValues(in info, character, action, mode, out int damage, out double seconds, comboSlotIndex);
            return $"{FormatSwingDamageLine(info.EffectiveMultiHitCount, damage)} | {FormatSwingSpeedLine(seconds)}";
        }

        /// <summary>
        /// Full swing readout for hover tooltips: <c>Dmg 50% | Spd 100%</c> (or multihit <c>2x50% damage | …</c>).
        /// Amp is baked into Effective damage %; hover still shows a separate AMP calc line elsewhere.
        /// </summary>
        public static string FormatStripSwingPercentLine(
            in ActionPanelInfo info,
            Character character,
            Action action,
            ActionStripDamageLineMode mode,
            int comboSlotIndex = -1)
        {
            GetStripSwingDisplayPercents(in info, character, action, mode, out double damagePct, out double speedPct, comboSlotIndex);
            return $"{FormatSwingDamagePercentLine(info.EffectiveMultiHitCount, damagePct)} | {FormatSwingSpeedPercentLine(speedPct)}";
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
        /// Extra modifier lines for compact action strip cards after the swing (damage/seconds) line: deferred sheet accuracy
        /// (and related lines) so cards match tooltip behavior, compact stat bonuses, and cadence bonus groups.
        /// Current-roll accuracy is shown only under TURN/ACTION cadence headers, not as a standalone Acc line.
        /// ACTION cadence groups on a card describe grantor “next action” capability and stay on the grantor while
        /// pending. Recipient application is shown via swing numbers (e.g. <c>Nx</c> damage) and cyan shimmer, not
        /// relocated ACTION/Multihit text — ACTION labels bonuses for the following action, not the current one.
        /// </summary>
        public static List<string> BuildActionStripModifierTailLines(
            Action? action,
            int maxWidth,
            int maxLines,
            Character? character = null,
            int comboSlotIndex = -1)
        {
            var lines = new List<string>();
            if (action == null || maxLines <= 0 || maxWidth < 4)
                return lines;

            // character / comboSlotIndex kept for caller API parity (damage/shimmer use other paths).
            _ = (character, comboSlotIndex);

            bool deferSheet = Action.DefersSheetCombatPackagesToNextHeroRoll(action);

            void add(string? s)
            {
                if (string.IsNullOrWhiteSpace(s) || lines.Count >= maxLines)
                    return;
                s = s.Trim();
                string t = s.Length > maxWidth ? s.Substring(0, Math.Max(1, maxWidth - 3)) + "..." : s;
                lines.Add(t);
            }

            void addBlank()
            {
                if (lines.Count >= maxLines)
                    return;
                lines.Add("");
            }

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
                    if (lines.Count >= maxLines)
                        break;
                    if (group?.Bonuses == null || group.Bonuses.Count == 0)
                        continue;
                    int displayCount = ActionCadenceDurationResolver.GetDisplayCount(action, group);
                    addBlank();
                    foreach (string line in UI.CadenceCardLineFormatter.FormatGroupLines(group, displayCount))
                        add(line);
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
