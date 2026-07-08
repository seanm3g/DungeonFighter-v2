using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Data;

namespace RPGGame.UI.Avalonia.Layout
{
    /// <summary>
    /// Builds human-readable lines for the status-effect HUD (hero left panel, enemy right panel).
    /// </summary>
    public static class StatusEffectDisplayLines
    {
        /// <summary>
        /// Builds display strings for an actor. Pass <paramref name="asCharacter"/> when <paramref name="actor"/> is a <see cref="Character"/> to include character-only effects.
        /// </summary>
        public static List<string> Build(Actor actor, Character? asCharacter)
        {
            var lines = new List<string>();
            if (actor.IsWeakened && actor.WeakenTurns > 0)
                lines.Add($"Weakened ({actor.WeakenTurns} turn{(actor.WeakenTurns != 1 ? "s" : "")})");
            if (actor.IsStunned && actor.StunTurnsRemaining > 0)
                lines.Add($"Stunned ({actor.StunTurnsRemaining} turn{(actor.StunTurnsRemaining != 1 ? "s" : "")})");
            if (actor.RollPenalty != 0 && actor.RollPenaltyTurns > 0)
                lines.Add($"Accuracy -{actor.RollPenalty} {CadenceBonusDisplayFormatter.FormatTurnDurationSuffix(actor.RollPenaltyTurns)}");
            if (actor.PoisonPercentOfMaxHealth > 0)
                lines.Add($"Poison {actor.PoisonPercentOfMaxHealth:0.##}% max HP");
            if (actor.BleedIntensity > 0 || actor.PendingBleedFromHits > 0)
                lines.Add($"Bleed {actor.BleedIntensity + actor.PendingBleedFromHits}");
            if (actor.BurnIntensity > 0 || actor.PendingBurnFromHits > 0)
                lines.Add($"Burn {actor.BurnIntensity + actor.PendingBurnFromHits}");
            if (actor.HasCriticalMissPenalty && actor.CriticalMissPenaltyTurns > 0)
                lines.Add($"Crit miss ({actor.CriticalMissPenaltyTurns} turn{(actor.CriticalMissPenaltyTurns != 1 ? "s" : "")})");
            if (actor.VulnerabilityStacks > 0 && actor.VulnerabilityTurns > 0)
                lines.Add($"Vuln x{actor.VulnerabilityStacks} ({actor.VulnerabilityTurns}t)");
            if (actor.HardenStacks > 0 && actor.HardenTurns > 0)
                lines.Add($"Harden x{actor.HardenStacks} ({actor.HardenTurns}t)");
            if (actor.FortifyStacks > 0 && actor.FortifyTurns > 0)
                lines.Add($"Fortify x{actor.FortifyStacks} ({actor.FortifyTurns}t)");
            if (actor.FocusStacks > 0 && actor.FocusTurns > 0)
                lines.Add($"Focus x{actor.FocusStacks} ({actor.FocusTurns}t)");
            if (actor.ExposeStacks > 0 && actor.ExposeTurns > 0)
                lines.Add($"Expose x{actor.ExposeStacks} ({actor.ExposeTurns}t)");
            if (actor.HPRegenStacks > 0 && actor.HPRegenTurns > 0)
                lines.Add($"HP Regen x{actor.HPRegenStacks} ({actor.HPRegenTurns}t)");
            if (actor.ArmorBreakStacks > 0 && actor.ArmorBreakTurns > 0)
                lines.Add($"Armor brk x{actor.ArmorBreakStacks} ({actor.ArmorBreakTurns}t)");
            if (actor.HasPierce && actor.PierceTurns > 0)
                lines.Add($"Pierce ({actor.PierceTurns} turn{(actor.PierceTurns != 1 ? "s" : "")})");
            if (actor.ReflectStacks > 0 && actor.ReflectTurns > 0)
                lines.Add($"Reflect x{actor.ReflectStacks} ({actor.ReflectTurns}t)");
            if (actor.IsSilenced && actor.SilenceTurns > 0)
                lines.Add($"Silence ({actor.SilenceTurns} turn{(actor.SilenceTurns != 1 ? "s" : "")})");
            if (actor.HasAbsorb && actor.AbsorbTurns > 0)
                lines.Add($"Absorb ({actor.AbsorbTurns} turn{(actor.AbsorbTurns != 1 ? "s" : "")})");
            if (actor.TemporaryHP > 0 && actor.TemporaryHPTurns > 0)
                lines.Add($"Temp HP ({actor.TemporaryHPTurns} turn{(actor.TemporaryHPTurns != 1 ? "s" : "")})");
            if (actor.IsConfused && actor.ConfusionTurns > 0)
                lines.Add($"Confused ({actor.ConfusionTurns} turn{(actor.ConfusionTurns != 1 ? "s" : "")})");
            if (actor.IsMarked && actor.MarkTurns > 0)
                lines.Add($"Marked ({actor.MarkTurns} turn{(actor.MarkTurns != 1 ? "s" : "")})");
            Character? charHud = asCharacter ?? actor as Character;
            if (charHud != null)
            {
                lines.AddRange(BuildTurnBonusStatusLines(charHud));
                lines.AddRange(BuildPendingActionBonusStatusLines(charHud));
                lines.AddRange(BuildScopedCadenceStatusLines(charHud));

                if (charHud.Effects.SlowTurns > 0)
                    lines.Add($"Slow ({charHud.Effects.SlowTurns} turn{(charHud.Effects.SlowTurns != 1 ? "s" : "")})");
                if (charHud.Effects.HasShield)
                    lines.Add("Shield");
                if (charHud.Effects.SkipNextTurn)
                    lines.Add("Skip turn");
                if (charHud.Effects.GuaranteeNextSuccess)
                    lines.Add("Guarantee hit");
                if (charHud.Effects.ExtraAttacks > 0)
                    lines.Add($"Extra atk x{charHud.Effects.ExtraAttacks}");
                if (charHud.Effects.ComboModeActive)
                    lines.Add("Combo mode");
                if (charHud.Effects.RerollCharges > 0)
                    lines.Add($"Reroll x{charHud.Effects.RerollCharges}");
            }
            return lines;
        }

        /// <summary>Per-group TURN bonus lines so stacking (e.g. TURN x3 from multiple sources) is visible.</summary>
        internal static List<string> BuildTurnBonusStatusLines(Character c)
        {
            var lines = new List<string>();
            bool hasTurnGroups = c.Effects.TurnBonuses.Any(g => g.Count > 0 && g.Bonuses?.Count > 0);

            foreach (var group in c.Effects.TurnBonuses)
            {
                if (group.Count <= 0 || group.Bonuses == null || group.Bonuses.Count == 0)
                    continue;
                string items = CadenceBonusDisplayFormatter.FormatBonusItemsShort(group.Bonuses);
                if (string.IsNullOrEmpty(items))
                    continue;
                string suffix = CadenceBonusDisplayFormatter.FormatTurnDurationSuffix(group.Count);
                if (group.Bonuses.Count == 1 && IsSingleAccuracyOnly(group.Bonuses))
                    lines.Add($"Accuracy +{group.Bonuses[0].Value:0} {suffix}");
                else if (group.Bonuses.Count == 1 && IsSingleAdvantageOnly(group.Bonuses))
                    lines.Add($"Advantage {suffix}");
                else if (group.Bonuses.Count == 1 && IsSingleDisadvantageOnly(group.Bonuses))
                    lines.Add($"Disadvantage {suffix}");
                else
                    lines.Add($"{CadenceKeywords.GetDisplayLabel(CadenceKeywords.Turn, group.Count)}: {items} {suffix}");
            }

            if (!hasTurnGroups)
            {
                int accTemp = c.Effects.TempRollBonusTurns > 0 ? c.Effects.GetTempRollBonus() : 0;
                if (accTemp != 0)
                {
                    string suffix = CadenceBonusDisplayFormatter.FormatTurnDurationSuffix(c.Effects.TempRollBonusTurns);
                    lines.Add(accTemp > 0 ? $"Accuracy +{accTemp} {suffix}" : $"Accuracy {accTemp} {suffix}");
                }
            }

            return lines;
        }

        internal static List<string> BuildPendingActionBonusStatusLines(Character c)
        {
            var lines = new List<string>();

            var nextRollBonuses = c.Effects.PeekPendingActionBonusesNextHeroRoll();
            if (nextRollBonuses.Count > 0)
            {
                string items = CadenceBonusDisplayFormatter.FormatBonusItemsShort(nextRollBonuses);
                if (!string.IsNullOrEmpty(items))
                {
                    int layers = c.Effects.GetPendingActionCadenceLayerCount();
                    string suffix = layers > 1 ? $"(x{layers} stacked, next action)" : "(next action)";
                    if (IsSingleAdvantageOnly(nextRollBonuses))
                        lines.Add($"Advantage {suffix}");
                    else if (IsSingleDisadvantageOnly(nextRollBonuses))
                        lines.Add($"Disadvantage {suffix}");
                    else
                        lines.Add($"Next action: {items}");
                }
            }

            foreach (var slot in c.Effects.GetPendingActionBonusSlots().OrderBy(s => s))
            {
                var bonuses = c.Effects.GetPendingActionBonusesForSlot(slot);
                if (bonuses.Count == 0) continue;
                string items = CadenceBonusDisplayFormatter.FormatBonusItemsShort(bonuses);
                if (string.IsNullOrEmpty(items)) continue;
                int displaySlot = slot + 1;
                if (IsSingleAdvantageOnly(bonuses))
                    lines.Add($"Advantage (action {displaySlot})");
                else if (IsSingleDisadvantageOnly(bonuses))
                    lines.Add($"Disadvantage (action {displaySlot})");
                else
                    lines.Add($"Action {displaySlot}: {items}");
            }

            return lines;
        }

        internal static List<string> BuildScopedCadenceStatusLines(Character c)
        {
            var lines = new List<string>();
            AppendScopedLine(lines, "Fight", c.FightCadenceBuffs);
            AppendScopedLine(lines, "Dungeon", c.DungeonCadenceBuffs);
            return lines;
        }

        private static void AppendScopedLine(List<string> lines, string label, CadenceScopedBonusState scope)
        {
            if (!scope.HasAny) return;
            string items = CadenceBonusDisplayFormatter.FormatBonusItemsShort(scope.CopyBonuses());
            if (!string.IsNullOrEmpty(items))
                lines.Add($"{label}: {items}");
        }

        private static bool IsSingleAccuracyOnly(List<ActionAttackBonusItem> bonuses)
        {
            if (bonuses.Count != 1) return false;
            return string.Equals(ActionAttackBonusItem.NormalizeBonusType(bonuses[0].Type), "ACCURACY", StringComparison.OrdinalIgnoreCase)
                && bonuses[0].Value != 0;
        }

        private static bool IsSingleAdvantageOnly(List<ActionAttackBonusItem> bonuses) =>
            bonuses.Count == 1
            && string.Equals(ActionAttackBonusItem.NormalizeBonusType(bonuses[0].Type), MultiDiceRollMapper.AdvantageBonusType, StringComparison.OrdinalIgnoreCase);

        private static bool IsSingleDisadvantageOnly(List<ActionAttackBonusItem> bonuses) =>
            bonuses.Count == 1
            && string.Equals(ActionAttackBonusItem.NormalizeBonusType(bonuses[0].Type), MultiDiceRollMapper.DisadvantageBonusType, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Enemies with <see cref="Enemy.IsLiving"/> false (spreadsheet <c>isLiving</c> false: undead, elementals, etc.)
        /// do not take poison DoT or bleed damage — surface that in the status-effects HUD.
        /// </summary>
        public static string? GetNonLivingEnemyImmunityLine(Enemy enemy)
        {
            if (enemy == null || enemy.IsLiving)
                return null;
            return "Immune: Bleed, Poison";
        }
    }
}
