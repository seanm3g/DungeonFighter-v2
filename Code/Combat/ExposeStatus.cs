using System;

namespace RPGGame
{
    /// <summary>
    /// Expose, the single "breaker" status (Vulnerability and Armor Break fold into it, 2026-11-11; see
    /// Documentation/BUILD_SYSTEM_CONDENSED.md §3).
    /// <list type="bullet">
    /// <item>Tier 1: each stack up to <see cref="CombatConfig.StatusMaxStacks"/> removes
    /// <see cref="CombatConfig.StatusExposeDefensePerStack"/> × K Defense (unchanged Expose).</item>
    /// <item>Tier 2: each application while already at the cap strips
    /// <see cref="CombatConfig.StatusExposeArmorStripPerStack"/> of the holder's armor (Defense rating), up to
    /// <see cref="CombatConfig.StatusExposeArmorStripMax"/>. The strip ends when Expose does.</item>
    /// </list>
    /// Every Expose / Vulnerability / Armor Break source applies through <see cref="Apply"/>.
    /// </summary>
    public static class ExposeStatus
    {
        /// <summary>Applies <paramref name="stacks"/> Expose applications and keeps it up for at least <paramref name="turns"/>.</summary>
        public static void Apply(Actor? target, int stacks, int turns)
        {
            if (target == null || stacks <= 0)
                return;
            var combat = GameConfiguration.Instance.Combat;
            int cap = combat.StatusMaxStacks;
            double perStrip = Math.Max(0.0, combat.StatusExposeArmorStripPerStack);
            double maxStrip = Math.Clamp(combat.StatusExposeArmorStripMax, 0.0, 1.0);

            for (int i = 0; i < stacks; i++)
            {
                int current = target.ExposeStacks ?? 0;
                if (cap <= 0 || current < cap)
                    target.ExposeStacks = current + 1;
                else if (perStrip > 0 && maxStrip > 0)
                    target.ExposeArmorStrip = Math.Min(maxStrip, target.ExposeArmorStrip + perStrip);
            }
            target.ExposeTurns = Math.Max(target.ExposeTurns, Math.Max(1, turns));
        }

        /// <summary>True while the holder has any Expose stacks.</summary>
        public static bool IsExposed(Actor? actor) => actor != null && (actor.ExposeStacks ?? 0) > 0 && actor.ExposeTurns > 0;

        /// <summary>True while Expose is at tier 2 (armor stripped).</summary>
        public static bool IsArmorStripped(Actor? actor) => IsExposed(actor) && actor!.ExposeArmorStrip > 0;

        /// <summary>Armor after the tier-2 strip.</summary>
        public static int ApplyArmorStrip(Actor actor, int armor)
        {
            if (armor <= 0 || actor.ExposeArmorStrip <= 0 || !IsExposed(actor))
                return armor;
            return Math.Max(0, armor - (int)Math.Round(armor * actor.ExposeArmorStrip, MidpointRounding.AwayFromZero));
        }

        /// <summary>Ends Expose (both tiers).</summary>
        public static void Clear(Actor actor)
        {
            actor.ExposeStacks = null;
            actor.ExposeTurns = 0;
            actor.ExposeArmorStrip = 0;
        }
    }
}
