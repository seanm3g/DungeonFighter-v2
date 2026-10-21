using System;
using System.Collections.Generic;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Combat
{
    /// <summary>
    /// Pack enemies are one combat role split into bodies. A single hit can kill at most the front body (overkill is
    /// wasted), but each hit of a multi-hit swing lands on whichever body is in front, so 3 hits can drop 3 bodies.
    /// The pack's own attack hit count is the number of bodies still alive.
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

        /// <summary>
        /// Per-body damage scale for a pack attacker. The bodies share one enemy's swing: an action authored as one hit
        /// is split across the full pack (each body deals 1/PackSize), so pack damage falls as bodies die. Actions that
        /// author their own hit count (e.g. 30 × 4% ANT SWARM) keep their per-hit damage.
        /// </summary>
        public static double GetPerBodyDamageScale(Actor source, int authoredCount)
        {
            if (source is Enemy enemy && enemy.IsPack && enemy.PackSize > 1)
                return Math.Min(1.0, Math.Max(1, authoredCount) / (double)enemy.PackSize);
            return 1.0;
        }

        /// <summary>
        /// Applies each hit separately to the pack's current front body: a hit that kills a body wastes its overkill,
        /// and the next hit moves on to the next body. Returns HP actually removed; the enemy's
        /// <see cref="Enemy.LastOverkillWasted"/> becomes the overkill summed over all hits.
        /// </summary>
        public static int ApplyHitsAcrossBodies(Enemy pack, IReadOnlyList<int> hits)
        {
            int before = pack.CurrentHealth;
            int overkill = 0;
            foreach (int hit in hits)
            {
                if (hit <= 0)
                    continue;
                if (pack.CurrentHealth <= 0)
                {
                    overkill += hit;
                    continue;
                }
                RPGGame.ActionUtilities.ApplyDamage(pack, hit);
                overkill += Math.Max(0, pack.LastOverkillWasted);
            }
            pack.SetLastOverkillWasted(overkill);
            return Math.Max(0, before - pack.CurrentHealth);
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
                result.PackBodyFellText = FormatBodiesFell(enemy, livingBefore - enemy.LivingBodyCount);
        }

        /// <summary>"One falls." / "Two fall." prose for bodies dropped by one swing, plus how many remain.</summary>
        public static string FormatBodiesFell(Enemy enemy, int fallen)
        {
            string[] words = { "None", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten" };
            string count = fallen >= 0 && fallen < words.Length ? words[fallen] : fallen.ToString();
            string verb = fallen == 1 ? "falls" : "fall";
            string remain = enemy.LivingBodyCount == 1 ? "remains" : "remain";
            return $"{count} {verb}. {enemy.Name}({enemy.LivingBodyCount}x) {remain}.";
        }

        public static List<ColoredText> FormatBodyFellLine(string text)
        {
            var builder = new ColoredTextBuilder();
            builder.Add(text, ColorPalette.Info);
            return builder.Build();
        }
    }
}
