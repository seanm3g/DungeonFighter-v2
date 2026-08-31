using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.Actions.RollModification;
using RPGGame.Combat.Calculators;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Combat.Sequence
{
    /// <summary>
    /// Sequential formula pieces shown in a sequence-HUD column (die → keep → bonus, base → ×action → final).
    /// </summary>
    internal static class CombatSequenceMathBeats
    {
        public static List<List<ColoredText>> ForRoll(ActionExecutionResult result)
        {
            var beats = new List<List<ColoredText>>();
            int face = result.ModifiedBaseRoll;
            var detail = result.MultiDiceRollDetail;

            if (detail.HasLuckDetail && detail.Mode != MultiDiceLuckMode.Cancelled)
            {
                beats.Add(Plain($"{detail.HighDie}/{detail.LowDie}", Colors.White));
                beats.Add(ArrowTo(face));
            }
            else if (detail.Mode == MultiDiceLuckMode.Cancelled)
            {
                beats.Add(Plain($"{face} (cancel)", ColorPalette.Gray));
            }
            else
            {
                beats.Add(Plain(face.ToString(), Colors.White));
            }

            if (result.RollBonus != 0)
            {
                int total = result.AttackRoll != 0
                    ? result.AttackRoll
                    : face + result.RollBonus;
                beats.Add(BonusToTotal(result.RollBonus, total));
            }

            return beats;
        }

        public static List<List<ColoredText>> ForOutcome(ActionExecutionResult result)
        {
            var beats = new List<List<ColoredText>>();
            int total = result.AttackRoll != 0
                ? result.AttackRoll
                : result.ModifiedBaseRoll + result.RollBonus;
            int critMiss = result.ResolvedCritMissThreshold ?? 1;
            int hitBar = result.ResolvedHitThreshold ?? 5;
            int hitMin = hitBar + 1;
            int combo = result.ResolvedComboThreshold ?? 14;
            int crit = result.ResolvedCritThreshold ?? 20;

            if (result.IsCriticalMiss)
            {
                beats.Add(Compare(total, "≤", critMiss, ColorPalette.Miss));
                beats.Add(Plain("CRIT MISS", ColorPalette.Miss));
                return beats;
            }

            if (!result.Hit)
            {
                beats.Add(Compare(total, "<", hitMin, ColorPalette.Miss));
                beats.Add(Plain("MISS", ColorPalette.Miss));
                return beats;
            }

            beats.Add(Compare(total, "≥", hitMin, ColorPalette.Success));
            if (result.IsCombo)
                beats.Add(Compare(total, "≥", combo, ColorPalette.Gold));
            if (result.IsCritical)
                beats.Add(Compare(total, "≥", crit, ColorPalette.Critical));
            beats.Add(OutcomeLabel(result));
            return beats;
        }

        public static List<List<ColoredText>> ForDefense(
            int defenseFace,
            int attackFace,
            Actor target,
            Action? action)
        {
            int margin = DefenseBlockCalculator.GetMargin(attackFace, defenseFace);
            int block = DefenseBlockCalculator.ResolveMitigation(target, action, defenseFace, attackFace);
            var beats = new List<List<ColoredText>>
            {
                Plain($"def {defenseFace}", ColorPalette.Info),
                Plain(DefenseBlockCalculator.FormatMarginSigned(margin), Colors.White),
                Plain($"{block} block", ColorPalette.Block)
            };
            return beats;
        }

        public static List<List<ColoredText>> ForDamage(ActionExecutionResult result, Action? action)
        {
            var beats = new List<List<ColoredText>>();
            var trace = result.DamageTrace;
            if (trace != null && trace.BaseDamage > 0)
            {
                beats.Add(Plain($"base {trace.BaseDamage}", Colors.White));
                if (Math.Abs(trace.ActionMultiplier - 1.0) > 0.001)
                    beats.Add(Plain($"×{trace.ActionMultiplier:0.##}", ColorPalette.Success));
                if (Math.Abs(trace.Amp - 1.0) > 0.001)
                    beats.Add(Plain($"×{trace.Amp:0.##} amp", ColorPalette.Gold));
                if (trace.CritDamage)
                    beats.Add(Plain("crit ×", ColorPalette.Critical));
                if (trace.ConvertFlat != 0)
                    beats.Add(SignedFlat(trace.ConvertFlat));
                if (trace.Block > 0)
                    beats.Add(Plain($"-{trace.Block} block", ColorPalette.Block));
            }
            else if (action != null && Math.Abs(action.DamageMultiplier - 1.0) > 0.001)
            {
                beats.Add(Plain($"×{action.DamageMultiplier:0.##}", ColorPalette.Success));
            }

            int final = result.Damage;
            string finalText = final.ToString();
            if (beats.Count == 0 || !PlainText(beats[beats.Count - 1]).Equals(finalText, StringComparison.Ordinal))
                beats.Add(Plain(finalText, ColorPalette.Damage));
            return beats;
        }

        public static List<List<ColoredText>> ForHeal(int amount)
        {
            return new List<List<ColoredText>> { Plain(amount.ToString(), ColorPalette.Healing) };
        }

        private static List<ColoredText> OutcomeLabel(ActionExecutionResult result)
        {
            if (result.IsCritical && result.IsCombo)
                return Plain("CRIT COMBO", ColorPalette.Critical);
            if (result.IsCritical)
                return Plain("CRIT", ColorPalette.Critical);
            if (result.IsCombo)
                return Plain("COMBO", ColorPalette.Gold);
            return Plain("HIT", ColorPalette.Success);
        }

        private static List<ColoredText> Compare(int left, string op, int right, ColorPalette palette)
        {
            var builder = new ColoredTextBuilder();
            builder.Add(left.ToString(), Colors.White);
            builder.Add($" {op} ", Colors.White);
            builder.Add(right.ToString(), palette);
            return builder.Build();
        }

        private static List<ColoredText> ArrowTo(int value)
        {
            var builder = new ColoredTextBuilder();
            builder.Add("→ ", Colors.White);
            builder.Add(value.ToString(), Colors.White);
            return builder.Build();
        }

        private static List<ColoredText> BonusToTotal(int bonus, int total)
        {
            var builder = new ColoredTextBuilder();
            if (bonus > 0)
            {
                builder.Add("+", ColorPalette.Success);
                builder.Add(bonus.ToString(), ColorPalette.Success);
            }
            else
            {
                builder.Add("-", ColorPalette.Error);
                builder.Add((-bonus).ToString(), ColorPalette.Error);
            }
            builder.Add(" = ", Colors.White);
            builder.Add(total.ToString(), Colors.White);
            return builder.Build();
        }

        private static List<ColoredText> SignedFlat(int value)
        {
            var builder = new ColoredTextBuilder();
            if (value > 0)
            {
                builder.Add("+", ColorPalette.Success);
                builder.Add(value.ToString(), ColorPalette.Success);
            }
            else
            {
                builder.Add("-", ColorPalette.Error);
                builder.Add((-value).ToString(), ColorPalette.Error);
            }
            return builder.Build();
        }

        private static List<ColoredText> Plain(string text, ColorPalette palette)
        {
            var builder = new ColoredTextBuilder();
            builder.Add(text, palette);
            return builder.Build();
        }

        private static List<ColoredText> Plain(string text, Color color)
        {
            var builder = new ColoredTextBuilder();
            builder.Add(text, color);
            return builder.Build();
        }

        private static string PlainText(List<ColoredText> segments) =>
            ColoredTextRenderer.RenderAsPlainText(segments);
    }
}
