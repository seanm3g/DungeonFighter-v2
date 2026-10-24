using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.Actions.RollModification;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Combat.Sequence
{
    /// <summary>
    /// Plain-language "how this swing resolved" lines for the F7 prose hover: the d20 roll against the bands
    /// in force, then the damage formula from <see cref="CombatSequenceDamageTrace"/>.
    /// </summary>
    internal static class CombatSwingMathExplainer
    {
        public static List<List<ColoredText>> Build(ActionExecutionResult result, Actor source, Actor target)
        {
            var lines = new List<List<ColoredText>>();
            if (result?.SelectedAction == null)
                return lines;

            lines.Add(Line("How this swing resolved", ColorPalette.Warning));

            int face = result.ModifiedBaseRoll;
            int total = result.AttackRoll != 0 ? result.AttackRoll : face + result.RollBonus;
            var detail = result.MultiDiceRollDetail;
            string dieText = detail.HasLuckDetail && detail.Mode != MultiDiceLuckMode.Cancelled
                ? $"d20 rolled {detail.HighDie} and {detail.LowDie}, kept {face}"
                : $"d20 rolled {face}";
            string bonusText = result.RollBonus != 0 ? $" {result.RollBonus:+0;-0} accuracy/roll bonus = {total}" : "";
            lines.Add(Line($"Roll: {dieText}{bonusText}.", Colors.White));

            int critMiss = result.ResolvedCritMissThreshold ?? 1;
            int hitMin = (result.ResolvedHitThreshold ?? 5) + 1;
            int combo = result.ResolvedComboThreshold ?? 14;
            int crit = result.ResolvedCritThreshold ?? 20;
            lines.Add(Line(
                $"Bands: crit miss ≤ {critMiss} · hit ≥ {hitMin} · action ≥ {combo} · crit ≥ {crit} (crit checks the die without accuracy).",
                Colors.Gray));
            lines.Add(Line($"Result: {OutcomeText(result)}.", Colors.White));

            var trace = result.DamageTrace;
            if (result.Hit && trace != null && trace.BaseDamage > 0)
            {
                double known = trace.BaseDamage * trace.ActionMultiplier * trace.Amp * trace.StrengthMultiplier
                    * trace.RollBandMultiplier * trace.ClassMultiplier;
                double other = known > 0 && trace.PreRoundTotal > 0 ? trace.PreRoundTotal / known : 1.0;

                var parts = new List<string> { $"base {trace.BaseDamage}" };
                if (Math.Abs(trace.ActionMultiplier - 1.0) > 0.001) parts.Add($"action ×{trace.ActionMultiplier:0.##}");
                if (Math.Abs(trace.Amp - 1.0) > 0.001) parts.Add($"AMP ×{trace.Amp:0.##}");
                if (Math.Abs(trace.StrengthMultiplier - 1.0) > 0.001) parts.Add($"STR ×{trace.StrengthMultiplier:0.##}");
                if (!string.IsNullOrEmpty(trace.RollBand) && Math.Abs(trace.RollBandMultiplier - 1.0) > 0.001)
                    parts.Add($"{trace.RollBand} ×{trace.RollBandMultiplier:0.##}");
                if (Math.Abs(trace.ClassMultiplier - 1.0) > 0.001) parts.Add($"class ×{trace.ClassMultiplier:0.##}");
                if (Math.Abs(other - 1.0) > 0.005) parts.Add($"other ×{other:0.##}");
                string rawText = $"Damage: {string.Join(" ", parts)} = {trace.PreRoundTotal:0.#} → {trace.Raw}";
                if (trace.ConvertFlat != 0)
                    rawText += $" (incl. {trace.ConvertFlat:+0;-0} convert)";
                lines.Add(Line(rawText + ".", Colors.White));

                if (trace.BeforeArmor != trace.Raw)
                    lines.Add(Line($"Status/tag modifiers: {trace.Raw} → {trace.BeforeArmor}.", Colors.Gray));
                string defenseWord = target is Character && target is not Enemy ? "defense reduction" : "armor";
                lines.Add(Line($"Then {trace.BeforeArmor} − {trace.Block} {defenseWord} = {trace.Final} dealt.", Colors.White));
                lines.Add(Line(
                    "Base = attribute damage + weapon + gear + mods. Other = card multipliers, set/charm bonuses, queued damage mods and level curves.",
                    Colors.DarkGray));
            }

            return lines;
        }

        private static string OutcomeText(ActionExecutionResult result)
        {
            if (result.IsCriticalMiss) return "critical miss";
            if (!result.Hit) return "miss";
            if (result.IsCritical && result.IsCombo) return "critical action";
            if (result.IsCritical) return "critical hit";
            if (result.IsCombo) return "action (combo) hit";
            return "normal hit";
        }

        private static List<ColoredText> Line(string text, ColorPalette palette)
        {
            var builder = new ColoredTextBuilder();
            builder.Add(text, palette);
            return builder.Build();
        }

        private static List<ColoredText> Line(string text, Color color)
        {
            var builder = new ColoredTextBuilder();
            builder.Add(text, color);
            return builder.Build();
        }
    }
}
