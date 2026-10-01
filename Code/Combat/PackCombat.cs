using System;
using System.Collections.Generic;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Combat
{
    /// <summary>
    /// Pack enemies are one combat role split into bodies. A swing can kill at most the front body,
    /// and the pack's own attack hit count is the number of bodies still alive.
    /// </summary>
    public static class PackCombat
    {
        /// <summary>
        /// Pack attackers swing once per living body. Other actors keep <paramref name="authoredCount"/>.
        /// </summary>
        public static int ResolveOutgoingHitCount(Actor source, int authoredCount)
        {
            if (source is Enemy enemy && enemy.IsPack && enemy.LivingBodyCount > 0)
                return enemy.LivingBodyCount;
            return Math.Max(1, authoredCount);
        }

        internal static void SnapshotHeadlines(ActionExecutionResult result, Actor source, Actor target)
        {
            if (source is Enemy sourceEnemy && sourceEnemy.IsPack)
                result.SourcePackHeadlineCount = sourceEnemy.LivingBodyCount;
            if (target is Enemy targetEnemy && targetEnemy.IsPack)
                result.TargetPackHeadlineCount = targetEnemy.LivingBodyCount;
        }

        internal static void ApplyHeadlineSnapshot(ActionExecutionResult result, Actor source, Actor target)
        {
            if (source is Enemy sourceEnemy)
                sourceEnemy.PackHeadlineCount = result.SourcePackHeadlineCount;
            if (target is Enemy targetEnemy && !ReferenceEquals(target, source))
                targetEnemy.PackHeadlineCount = result.TargetPackHeadlineCount;
            else if (target is Enemy same && ReferenceEquals(target, source))
                same.PackHeadlineCount = result.TargetPackHeadlineCount ?? result.SourcePackHeadlineCount;
        }

        public static void ClearHeadlineSnapshot(Actor source, Actor target)
        {
            if (source is Enemy sourceEnemy)
                sourceEnemy.PackHeadlineCount = null;
            if (target is Enemy targetEnemy)
                targetEnemy.PackHeadlineCount = null;
        }

        /// <summary>
        /// After damage, record wasted overkill and a body-down line when the pack is still up.
        /// When <paramref name="replaceDamage"/> is set, the logged damage becomes the HP the pack actually lost.
        /// </summary>
        internal static void AccountAfterDamage(ActionExecutionResult result, Actor actor, int livingBefore, int hpBefore, bool replaceDamage)
        {
            if (actor is not Enemy enemy || !enemy.IsPack)
                return;
            result.OverkillWasted += Math.Max(0, enemy.LastOverkillWasted);
            if (replaceDamage)
                result.Damage = Math.Max(0, hpBefore - enemy.CurrentHealth);
            if (enemy.LivingBodyCount < livingBefore && enemy.LivingBodyCount > 0 && string.IsNullOrEmpty(result.PackBodyFellText))
                result.PackBodyFellText = $"One falls. {enemy.Name}({enemy.LivingBodyCount}x) remain.";
        }

        public static List<ColoredText> FormatBodyFellLine(string text)
        {
            var builder = new ColoredTextBuilder();
            builder.Add(text, ColorPalette.Info);
            return builder.Build();
        }
    }
}
