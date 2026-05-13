using System;
using System.Collections.Generic;
using System.Globalization;
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

                // Pending bonuses for this slot (peek, do not consume). Next-hero-roll ACTION queue applies to the current strip slot.
                var slotBonuses = new List<ActionAttackBonusItem>(character.Effects.GetPendingActionBonusesForSlot(i));
                if (actions.Count > 0 && i == character.ComboStep % actions.Count)
                    slotBonuses.AddRange(character.Effects.PeekPendingActionBonusesNextHeroRoll());
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

        /// <summary>
        /// Swing line for tooltips: same effective Dmg/Spd percentages as the action strip cards
        /// (<see cref="BuildPanelData"/> when <paramref name="panelIndex"/> is a valid combo slot; otherwise intrinsic action only).
        /// </summary>
        private static string BuildTooltipSwingModsLine(Character? character, Action action, int panelIndex)
        {
            if (character != null && panelIndex >= 0)
            {
                var panels = BuildPanelData(character);
                if (panelIndex < panels.Count)
                {
                    var info = panels[panelIndex];
                    const double damageCmpEps = 0.0001;
                    double damageDisplay = Math.Abs(info.DamageModified - info.DamageBase) > damageCmpEps ? info.DamageModified : info.DamageBase;
                    double speedDisplay = info.SpeedModified != info.SpeedBase ? info.SpeedModified : info.SpeedBase;
                    return $"{FormatSwingDamageLine(info.EffectiveMultiHitCount, damageDisplay)} | Spd {speedDisplay:F0}%";
                }
            }

            double baseDamagePct = action.DamageMultiplier * 100.0;
            double baseSpeedPct = action.Length > 0
                ? ActionDisplayFormatter.CalculateActionSpeedPercentage(action)
                : 0;
            int hits = character != null
                ? RollModificationManager.GetEffectiveMultiHitCountForModifierScaling(action, character)
                : Math.Max(1, action.Advanced?.MultiHitCount ?? 1);
            return $"{FormatSwingDamageLine(hits, baseDamagePct)} | Spd {baseSpeedPct:F0}%";
        }

        private static string FormatTooltipActionName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "?";
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name.Trim().ToLowerInvariant());
        }

        /// <summary>
        /// Row-1 spreadsheet fields (HERO / ENEMY base stats), one display line each: e.g. <c>enemy damage +10%</c>, <c>Hero Amp +10%</c>.
        /// </summary>
        private static List<string> BuildSpreadsheetFriendlyModLines(Action action)
        {
            var lines = new List<string>();
            if (action == null)
                return lines;

            static string F(double v, bool asPercent)
            {
                string n = v.ToString("0.#", CultureInfo.InvariantCulture);
                if (!asPercent)
                    return v > 0 ? $"+{n}" : n;
                return v > 0 ? $"+{n}%" : $"{n}%";
            }

            void tryPercent(string? raw, string label)
            {
                if (string.IsNullOrWhiteSpace(raw))
                    return;
                double v = SpreadsheetActionData.ParseNumericValue(raw.Trim());
                if (Math.Abs(v) < 1e-9)
                    return;
                lines.Add($"{label} {F(v, asPercent: true)}");
            }

            void tryMultihit(string? raw, string label)
            {
                if (string.IsNullOrWhiteSpace(raw))
                    return;
                double v = SpreadsheetActionData.ParseNumericValue(raw.Trim());
                if (Math.Abs(v) < 1e-9)
                    return;
                lines.Add($"{label} {F(v, asPercent: false)}");
            }

            tryPercent(action.EnemySpeedMod, "enemy action speed");
            tryPercent(action.EnemyDamageMod, "enemy damage");
            tryMultihit(action.EnemyMultiHitMod, "enemy multihit");
            tryPercent(action.EnemyAmpMod, "enemy amp");

            tryPercent(action.SpeedMod, "Hero action speed");
            tryPercent(action.DamageMod, "Hero damage");
            tryMultihit(action.MultiHitMod, "Hero multihit");
            tryPercent(action.AmpMod, "Hero Amp");

            return lines;
        }

        private static void AddSegment(List<string> segments, string? segment)
        {
            if (segments == null || string.IsNullOrWhiteSpace(segment))
                return;
            if (!segments.Contains(segment))
                segments.Add(segment);
        }

        private static string FormatSignedValue(double value, string suffix = "")
        {
            string n = Math.Abs(value % 1) < 0.0001
                ? value.ToString("0", CultureInfo.InvariantCulture)
                : value.ToString("0.##", CultureInfo.InvariantCulture);
            return value >= 0 ? $"+{n}{suffix}" : $"{n}{suffix}";
        }

        private static string FormatValue(double value, string suffix = "")
        {
            string n = Math.Abs(value % 1) < 0.0001
                ? value.ToString("0", CultureInfo.InvariantCulture)
                : value.ToString("0.##", CultureInfo.InvariantCulture);
            return $"{n}{suffix}";
        }

        private static string FormatTarget(TargetType target) => target switch
        {
            TargetType.Self => "Self",
            TargetType.SingleTarget => "Enemy",
            TargetType.AreaOfEffect => "AOE",
            TargetType.Environment => "Environment",
            TargetType.SelfAndTarget => "Self + Enemy",
            _ => target.ToString()
        };

        private static string BuildActionMetadataLine(Action action)
        {
            string comboText = action.IsComboAction ? "combo action" : "non-combo action";
            return $"Type {action.Type} | Target {FormatTarget(action.Target)} | {comboText}";
        }

        /// <summary>Cadence bonus groups labeled Ability are hidden on action strip cards and hover tooltips (sheet still applies).</summary>
        private static bool ShouldOmitAbilityCadenceBonusGroup(ActionAttackBonusGroup? group)
        {
            if (group == null)
                return true;
            string cad = string.IsNullOrWhiteSpace(group.CadenceType)
                ? (string.IsNullOrWhiteSpace(group.Keyword) ? "BONUS" : group.Keyword)
                : group.CadenceType;
            return string.Equals(cad, "Ability", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Sheet-defined hero/enemy accuracy lines (timing differs from <see cref="CombatCalculator.CalculateRollBonus"/> when cadence defers packages).
        /// </summary>
        private static IEnumerable<string> EnumerateSheetAccuracyModifierLines(Action action)
        {
            if (action?.Advanced == null)
                yield break;

            bool deferred = Action.DefersSheetCombatPackagesToNextHeroRoll(action);
            string timing = deferred ? "on hit: next roll" : "current roll";
            if (action.Advanced.RollBonus != 0)
                yield return $"Hero accuracy {FormatSignedValue(action.Advanced.RollBonus)} ({timing})";
            if (action.Advanced.EnemyRollBonus != 0)
            {
                string enemyTiming = deferred && action.Advanced.EnemyRollBonus < 0
                    ? "on hit: target roll penalty"
                    : timing;
                yield return $"Enemy accuracy {FormatSignedValue(action.Advanced.EnemyRollBonus)} ({enemyTiming})";
            }
            if (action.Advanced.RollBonusDuration > 0)
                yield return $"Accuracy duration: {action.Advanced.RollBonusDuration} roll(s)";
        }

        private static void AppendAccuracyLines(List<string> segments, Action action)
        {
            foreach (var line in EnumerateSheetAccuracyModifierLines(action))
                AddSegment(segments, line);
        }

        private static void AppendRollMechanicLines(List<string> segments, Action action)
        {
            var rm = action.RollMods;
            if (rm == null)
                return;

            if (rm.MultipleDiceCount > 1)
                AddSegment(segments, $"Roll dice: {rm.MultipleDiceCount}d20 {rm.MultipleDiceMode}");
            if (rm.ExplodingDice)
                AddSegment(segments, $"Roll dice: explode on {rm.ExplodingDiceThreshold}+");
            if (rm.AllowReroll && rm.RerollChance > 0)
                AddSegment(segments, $"Roll dice: reroll chance {FormatValue(rm.RerollChance * 100, "%")}");
            if (rm.Additive != 0)
                AddSegment(segments, $"Roll modifier: {FormatSignedValue(rm.Additive)}");
            if (Math.Abs(rm.Multiplier - 1.0) > 0.0001)
                AddSegment(segments, $"Roll multiplier: {FormatValue(rm.Multiplier, "x")}");
            if (rm.Min != 1 || rm.Max != 20)
                AddSegment(segments, $"Roll clamp: {rm.Min}-{rm.Max}");
        }

        private static void AppendThresholdLines(List<string> segments, Action action)
        {
            var rm = action.RollMods;
            if (rm == null)
                return;

            string hero = GetThresholdText(action);
            if (!string.IsNullOrEmpty(hero))
                AddSegment(segments, $"Hero thresholds {hero}");

            var enemyParts = new List<string>();
            if (rm.EnemyHitThresholdAdjustment != 0) enemyParts.Add($"H:{rm.EnemyHitThresholdAdjustment:+0;-0;0}");
            if (rm.EnemyComboThresholdAdjustment != 0) enemyParts.Add($"C:{rm.EnemyComboThresholdAdjustment:+0;-0;0}");
            if (rm.EnemyCriticalHitThresholdAdjustment != 0) enemyParts.Add($"Cr:{rm.EnemyCriticalHitThresholdAdjustment:+0;-0;0}");
            if (rm.EnemyCriticalMissThresholdAdjustment != 0) enemyParts.Add($"Cm:{rm.EnemyCriticalMissThresholdAdjustment:+0;-0;0}");
            if (enemyParts.Count > 0)
                AddSegment(segments, $"Enemy thresholds {string.Join(" ", enemyParts)}");

            var overrides = new List<string>();
            if (rm.HitThresholdOverride > 0) overrides.Add($"H={rm.HitThresholdOverride}");
            if (rm.ComboThresholdOverride > 0) overrides.Add($"C={rm.ComboThresholdOverride}");
            if (rm.CriticalHitThresholdOverride > 0) overrides.Add($"Cr={rm.CriticalHitThresholdOverride}");
            if (rm.CriticalMissThresholdOverride > 0) overrides.Add($"Cm={rm.CriticalMissThresholdOverride}");
            if (overrides.Count > 0)
            {
                string both = rm.ApplyThresholdAdjustmentsToBoth ? " (source + target)" : "";
                AddSegment(segments, $"Set thresholds {string.Join(" ", overrides)}{both}");
            }
        }

        private static void AppendTriggerLine(List<string> segments, Action action)
        {
            var conditions = action.Triggers?.TriggerConditions;
            if (conditions == null || conditions.Count == 0)
                return;
            AddSegment(segments, "Triggers: " + string.Join(", ", conditions.Where(c => !string.IsNullOrWhiteSpace(c))));
        }

        private static void AppendStatusLine(List<string> segments, Action action)
        {
            if (action == null)
                return;

            var parts = new List<string>();
            if (action.CausesWeaken) parts.Add("Weaken");
            if (action.CausesStun) parts.Add("Stun");
            if (action.CausesBleed) parts.Add($"Bleed +{(action.BleedAmountToAdd > 0 ? action.BleedAmountToAdd : 1)}");
            if (action.CausesPoison) parts.Add($"Poison +{FormatValue(action.PoisonPercentToAdd > 0 ? action.PoisonPercentToAdd : 1, "% max HP")}");
            if (action.CausesBurn) parts.Add($"Burn +{(action.BurnAmountToAdd > 0 ? action.BurnAmountToAdd : 1)}");
            if (action.CausesSlow) parts.Add("Slow");
            if (action.CausesVulnerability) parts.Add("Vulnerability");
            if (action.CausesHarden) parts.Add("Harden");
            if (action.CausesFortify) parts.Add("Fortify");
            if (action.CausesFocus) parts.Add("Focus");
            if (action.CausesExpose) parts.Add("Expose");
            if (action.CausesHPRegen) parts.Add("HP Regen");
            if (action.CausesArmorBreak) parts.Add("Armor Break");
            if (action.CausesPierce) parts.Add("Pierce");
            if (action.CausesReflect) parts.Add("Reflect");
            if (action.CausesSilence) parts.Add("Silence");
            if (action.CausesAbsorb) parts.Add("Absorb");
            if (action.CausesTemporaryHP) parts.Add("Temporary HP");
            if (action.CausesConfusion) parts.Add("Confusion");
            if (action.CausesCleanse) parts.Add("Cleanse");
            if (action.CausesMark) parts.Add("Mark");
            if (action.CausesDisrupt) parts.Add("Disrupt");
            if (action.CausesStatDrain) parts.Add("Stat Drain");

            if (parts.Count == 0)
                return;

            bool hasDot = action.CausesPoison || action.CausesBurn || action.CausesBleed;
            bool hasTrigger = action.Triggers?.TriggerConditions != null && action.Triggers.TriggerConditions.Count > 0;
            string dotNote = hasDot
                ? hasTrigger ? " (trigger-gated)" : " (DoT applies on crit)"
                : "";
            AddSegment(segments, $"Statuses: {string.Join(", ", parts)}{dotNote}");
        }

        private static string FormatStatBonusDuration(Action action)
        {
            string cadence = (action.Cadence ?? "").Trim();
            if (!string.IsNullOrEmpty(cadence))
                return cadence;
            int duration = action.Advanced?.StatBonusDuration ?? 0;
            return duration > 0 ? $"{duration} turn(s)" : "next use";
        }

        private static void AppendAdvancedMechanicLines(List<string> segments, Action action)
        {
            var adv = action.Advanced;
            if (adv == null)
                return;

            if (action.ComboBonusAmount > 0 && action.ComboBonusDuration > 0)
                AddSegment(segments, $"Combo bonus: +{action.ComboBonusAmount} for {action.ComboBonusDuration} turn(s)");

            if (adv.StatBonuses != null && adv.StatBonuses.Count > 0)
            {
                var parts = adv.StatBonuses
                    .Where(s => s != null && (s.Value != 0 || !string.IsNullOrWhiteSpace(s.Type)))
                    .Select(s => $"{s.Type} {FormatSignedValue(s.Value)}")
                    .ToList();
                if (parts.Count > 0)
                    AddSegment(segments, $"Stat bonus ({FormatStatBonusDuration(action)}): {string.Join(", ", parts)}");
            }
            else if (adv.StatBonus != 0 && !string.IsNullOrWhiteSpace(adv.StatBonusType))
            {
                AddSegment(segments, $"Stat bonus ({FormatStatBonusDuration(action)}): {adv.StatBonusType} {FormatSignedValue(adv.StatBonus)}");
            }

            if (adv.SelfDamagePercent > 0)
                AddSegment(segments, $"Self damage: {adv.SelfDamagePercent}%");
            if (adv.SkipNextTurn)
                AddSegment(segments, adv.GuaranteeNextSuccess ? "Skips next turn; guarantees next success." : "Skips next turn.");
            if (adv.RepeatLastAction)
                AddSegment(segments, "Repeats last action.");
            if (adv.EnemyRollPenalty != 0)
                AddSegment(segments, $"Enemy roll penalty: {FormatSignedValue(-adv.EnemyRollPenalty)}");
            if (Math.Abs(adv.ConditionalDamageMultiplier - 1.0) > 0.0001)
                AddSegment(segments, $"Conditional damage: {FormatValue(adv.ConditionalDamageMultiplier, "x")}");
            if (adv.HealAmount > 0)
                AddSegment(segments, $"Heal: {adv.HealAmount}");
            if (adv.HealthThreshold > 0)
                AddSegment(segments, $"Health threshold: {FormatValue(adv.HealthThreshold * 100, "%")}");
            if (adv.ComboAmplifierMultiplier != 1.0)
                AddSegment(segments, $"Combo amp multiplier: {FormatValue(adv.ComboAmplifierMultiplier, "x")}");
            if (adv.ExtraAttacks != 0)
                AddSegment(segments, $"Extra attacks: {FormatSignedValue(adv.ExtraAttacks)}");
            if (adv.ExtraDamage != 0)
                AddSegment(segments, $"Extra damage: {FormatSignedValue(adv.ExtraDamage)}");
            if (adv.DamageReduction != 0)
                AddSegment(segments, $"Damage reduction: {FormatValue(adv.DamageReduction * 100, "%")}");
            if (adv.SelfAttackChance > 0)
                AddSegment(segments, $"Self-attack chance: {FormatValue(adv.SelfAttackChance * 100, "%")}");
            if (adv.ResetEnemyCombo)
                AddSegment(segments, "Resets enemy combo.");
            if (adv.StunEnemy)
                AddSegment(segments, adv.StunDuration > 0 ? $"Stuns enemy for {adv.StunDuration} turn(s)" : "Stuns enemy.");
            if (adv.ReduceLengthNextActions && adv.LengthReduction > 0)
                AddSegment(segments, $"Reduces next action length by {FormatValue(adv.LengthReduction * 100, "%")} for {adv.LengthReductionDuration} turn(s)");

            AppendThresholdEntryLines(segments, adv);
            AppendAccumulationLines(segments, adv);
        }

        private static void AppendThresholdEntryLines(List<string> segments, AdvancedMechanicsProperties adv)
        {
            if (adv.Thresholds == null || adv.Thresholds.Count == 0)
                return;

            var parts = adv.Thresholds
                .Where(t => t != null)
                .Select(t =>
                {
                    string who = string.IsNullOrWhiteSpace(t.Qualifier) ? "" : $"{t.Qualifier} ";
                    string op = string.IsNullOrWhiteSpace(t.Operator) ? "<=" : t.Operator;
                    string value = string.Equals(t.ValueKind, "%", StringComparison.OrdinalIgnoreCase) || t.Value <= 1.0
                        ? FormatValue(t.Value * 100, "%")
                        : FormatValue(t.Value);
                    return $"{who}{t.Type} {op} {value}";
                })
                .ToList();
            if (parts.Count > 0)
                AddSegment(segments, $"Threshold rules: {string.Join("; ", parts)}");
        }

        private static void AppendAccumulationLines(List<string> segments, AdvancedMechanicsProperties adv)
        {
            if (adv.Accumulations == null || adv.Accumulations.Count == 0)
                return;

            var parts = adv.Accumulations
                .Where(a => a != null)
                .Select(a =>
                {
                    string value = string.Equals(a.ValueKind, "%", StringComparison.OrdinalIgnoreCase)
                        ? FormatSignedValue(a.Value, "%")
                        : FormatSignedValue(a.Value);
                    return $"{a.Type} -> {a.ModifiesParam} {value}";
                })
                .ToList();
            if (parts.Count > 0)
                AddSegment(segments, $"Accumulation: {string.Join("; ", parts)}");
        }

        private static void AppendComboRoutingLines(List<string> segments, Action action)
        {
            var route = action.ComboRouting;
            if (route == null)
                return;

            var parts = new List<string>();
            if (route.JumpToSlot > 0) parts.Add($"jump to slot {route.JumpToSlot}");
            if (route.JumpRelativeSlots > 0) parts.Add($"shift +{route.JumpRelativeSlots} slot(s)");
            if (route.SkipNext) parts.Add("skip next slot");
            if (route.RepeatPrevious) parts.Add("repeat previous slot");
            if (route.LoopToStart) parts.Add("loop to start");
            if (route.StopEarly) parts.Add("stop chain early");
            if (route.DisableSlot) parts.Add("disable slot");
            if (route.RandomAction) parts.Add("random next slot");
            if (route.TriggerOnlyInSlot > 0) parts.Add($"only in slot {route.TriggerOnlyInSlot}");
            if (!string.IsNullOrWhiteSpace(route.ChainPosition)) parts.Add($"position {route.ChainPosition}");
            if (!string.IsNullOrWhiteSpace(route.ChainLength)) parts.Add($"chain length {route.ChainLength}");
            if (!string.IsNullOrWhiteSpace(route.Reset)) parts.Add($"reset {route.Reset}");
            if (parts.Count > 0)
                AddSegment(segments, $"Combo route: {string.Join("; ", parts)}");

            if (ChainPositionBonusApplier.IsModifyChainPositionEnabled(route))
            {
                if (route.ChainPositionBonuses != null && route.ChainPositionBonuses.Count > 0)
                {
                    var bonusParts = route.ChainPositionBonuses
                        .Where(b => b != null)
                        .Select(b =>
                        {
                            string param = ChainPositionBonusApplier.GetDisplayNameForModifiesParam(b.ModifiesParam);
                            string value = string.Equals(b.ValueKind, "%", StringComparison.OrdinalIgnoreCase)
                                ? FormatSignedValue(b.Value, "%")
                                : FormatSignedValue(b.Value);
                            string basis = string.IsNullOrWhiteSpace(b.PositionBasis) ? "ComboSlotIndex1" : b.PositionBasis;
                            return $"{param} {value} per {basis}";
                        })
                        .ToList();
                    if (bonusParts.Count > 0)
                        AddSegment(segments, $"Chain position bonuses: {string.Join("; ", bonusParts)}");
                }
                else
                    AddSegment(segments, "Chain position scaling enabled.");
            }
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
        /// Combo routing lines for action hover tooltips (opener/finisher tags).
        /// </summary>
        private static void AppendComboRoleAndWeaponRequirementNotation(List<string> segments, Character? character, Action? action)
        {
            foreach (var line in BuildActionStripComboRoleLines(character, action))
                segments.Add(line);
        }

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
                    string count = group.Count > 1 ? $" x{group.Count}" : "";
                    add($"{cad}{count}: {items}");
                }
            }

            return lines;
        }

        /// <summary>
        /// Ordered mechanical segments after the title line: swing Dmg/Spd, then each spreadsheet / keyword / roll / status block.
        /// </summary>
        private static List<string> BuildMechanicalDetailSegments(Character? character, Action action, int panelIndex)
        {
            var segments = new List<string>();
            AppendComboRoleAndWeaponRequirementNotation(segments, character, action);
            segments.Add(BuildTooltipSwingModsLine(character, action, panelIndex));
            AppendAccuracyLines(segments, action);
            segments.AddRange(BuildSpreadsheetFriendlyModLines(action));

            if (action.ActionAttackBonuses?.BonusGroups != null)
            {
                foreach (var group in action.ActionAttackBonuses.BonusGroups)
                {
                    if (ShouldOmitAbilityCadenceBonusGroup(group))
                        continue;
                    if (group?.Bonuses == null || group.Bonuses.Count == 0)
                        continue;
                    string items = FormatBonusItemsShort(group.Bonuses);
                    if (string.IsNullOrEmpty(items))
                        continue;
                    string cad = string.IsNullOrWhiteSpace(group.CadenceType)
                        ? (string.IsNullOrWhiteSpace(group.Keyword) ? "BONUS" : group.Keyword)
                        : group.CadenceType;
                    string count = group.Count > 1 ? $" x{group.Count}" : "";
                    AddSegment(segments, $"{cad}{count}: {items}");
                }
            }

            AppendRollMechanicLines(segments, action);
            AppendThresholdLines(segments, action);
            AppendTriggerLine(segments, action);
            AppendStatusLine(segments, action);
            AppendComboRoutingLines(segments, action);
            AppendAdvancedMechanicLines(segments, action);

            return segments;
        }

        /// <summary>
        /// Single-line summary for inventory rows (pipe-separated). Tooltips use <see cref="BuildMechanicalDetailSegments"/> one segment per line.
        /// </summary>
        public static string BuildActionMechanicalModSummary(Character? character, Action? action, int comboSlotIndex)
        {
            if (action == null)
                return "";
            return string.Join(" | ", BuildMechanicalDetailSegments(character, action, comboSlotIndex));
        }

        /// <summary>
        /// Builds wrapped lines for the action strip hover tooltip: title-cased name, then Dmg/Spd on its own line,
        /// then one line per spreadsheet mod and other mechanical details (no narrative <see cref="Action.Description"/>).
        /// </summary>
        /// <param name="character">Player whose combo is shown.</param>
        /// <param name="panelIndex">0-based combo slot.</param>
        /// <param name="maxWidth">Maximum characters per line (inner width).</param>
        /// <param name="maxLines">Cap on total lines (excluding hard truncation).</param>
        public static List<string> BuildActionTooltipLines(Character? character, int panelIndex, int maxWidth, int maxLines = 28)
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
                AddWrappedTooltipParagraph(result, paragraph, maxWidth, maxLines);
            }

            result.Add(FormatTooltipActionName(action.Name));
            if (result.Count >= maxLines) return result;

            foreach (string segment in BuildMechanicalDetailSegments(character, action, panelIndex))
            {
                if (result.Count >= maxLines)
                    return result;
                AddWrapped(segment);
            }

            return result;
        }

        /// <summary>
        /// Tooltip lines for an action that may or may not be in the current combo (e.g. pool row on the inventory right panel).
        /// When the action is already in the sequence, delegates to <see cref="BuildActionTooltipLines"/>.
        /// </summary>
        public static List<string> BuildActionTooltipLinesForAction(Character? character, Action? action, int maxWidth, int maxLines = 28)
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
                AddWrappedTooltipParagraph(result, paragraph, maxWidth, maxLines);
            }

            result.Add(FormatTooltipActionName(action.Name));
            if (result.Count >= maxLines) return result;

            foreach (string segment in BuildMechanicalDetailSegments(character, action, -1))
            {
                if (result.Count >= maxLines)
                    return result;
                AddWrapped(segment);
            }

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

        /// <summary>
        /// Adds one wrapped tooltip paragraph, inserting a blank line between existing paragraphs when there is room.
        /// </summary>
        internal static void AddWrappedTooltipParagraph(List<string> result, string? paragraph, int maxWidth, int maxLines)
        {
            if (result == null || result.Count >= maxLines || string.IsNullOrWhiteSpace(paragraph))
                return;

            var wrappedLines = WrapTextToLines(paragraph.Trim(), maxWidth);
            if (wrappedLines.Count == 0)
                return;

            if (result.Count > 0 && result[result.Count - 1].Length > 0 && result.Count + 1 < maxLines)
                result.Add("");

            foreach (var line in wrappedLines)
            {
                if (result.Count >= maxLines)
                    break;
                result.Add(line);
            }
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
