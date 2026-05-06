using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.ColorSystem.Applications;

namespace RPGGame.Combat.Formatting
{
    /// <summary>
    /// Builds combat-log status lines with actor names using <see cref="EntityColorHelper.AppendActorNameColored"/>
    /// (same as primary action lines), instead of legacy <c>{{player|Name}}</c> markup.
    /// </summary>
    public static class StatusEffectCombatLogMessageBuilder
    {
        /// <summary>
        /// Appends a line like "     Name is weakened!" with themed coloring on the effect word.
        /// </summary>
        /// <param name="closing">Text after the effect word (default "!" if null).</param>
        public static void AppendIsStatusLine(
            List<string> results,
            Actor target,
            string effectWordForTemplateAndDisplay,
            string? closing = null)
        {
            var builder = new ColoredTextBuilder();
            builder.Add("     ", Colors.White);
            EntityColorHelper.AppendActorNameColored(builder, target);
            builder.AddSpace();
            builder.Add("is", Colors.White);
            builder.AddSpace();
            builder.AddRange(StatusEffectColorHelper.GetColoredStatusEffect(effectWordForTemplateAndDisplay));
            builder.Add(closing ?? "!", Colors.White);
            results.Add(ColoredTextRenderer.RenderAsMarkup(builder.Build()));
        }

        /// <summary>
        /// Appends "     Name {suffix}" with the name colored as an actor (suffix is plain white).
        /// </summary>
        public static void AppendNameWithPlainSuffix(List<string> results, Actor target, string suffix)
        {
            var builder = new ColoredTextBuilder();
            builder.Add("     ", Colors.White);
            EntityColorHelper.AppendActorNameColored(builder, target);
            builder.AddSpace();
            builder.Add(suffix, Colors.White);
            results.Add(ColoredTextRenderer.RenderAsMarkup(builder.Build()));
        }
    }
}
