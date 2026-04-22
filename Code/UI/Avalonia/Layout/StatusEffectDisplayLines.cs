using System.Collections.Generic;
using RPGGame;

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
                lines.Add($"Accuracy -{actor.RollPenalty} ({actor.RollPenaltyTurns} atk{(actor.RollPenaltyTurns != 1 ? "s" : "")})");
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
                // Single "Accuracy" line: FIFO ACCURACY bonuses for the next attack(s) plus any legacy temp roll bonus slot.
                int accFromQueues = ActionSelector.PeekQueuedAccuracyBonus(charHud);
                int accTemp = charHud.Effects.TempRollBonusTurns > 0 ? charHud.Effects.GetTempRollBonus() : 0;
                int accTotal = accFromQueues + accTemp;
                if (accTotal != 0)
                    lines.Add(accTotal > 0 ? $"Accuracy +{accTotal} (next attack)" : $"Accuracy {accTotal} (next attack)");

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

        /// <summary>
        /// Enemies with <see cref="Enemy.IsLiving"/> false (spreadsheet <c>isLiving</c> false: undead, elementals, etc.)
        /// do not take poison DoT or bleed damage — surface that in the status-effects HUD.
        /// </summary>
        /// <returns>A single display line, or null when the enemy has no such template immunities.</returns>
        public static string? GetNonLivingEnemyImmunityLine(Enemy enemy)
        {
            if (enemy == null || enemy.IsLiving)
                return null;
            return "Immune: Bleed, Poison";
        }
    }
}
