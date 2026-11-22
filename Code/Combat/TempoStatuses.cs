using System;
using System.Collections.Generic;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>
    /// Statuses added 2026-11-16 (Documentation/SYNERGY_PASS_2026-11-16.md): each one hooks a place combat already reads,
    /// so every store-reading verb (APPLY, CASH, SCALE, DETONATE) and rule (PER TARGET stagger) works on them. Brittle was
    /// retired in step 12 (it loads as Mark).
    /// <list type="bullet">
    /// <item><b>Stagger</b> (on a foe): its next action takes +<see cref="CombatConfig.StatusStaggerLengthPerStack"/> longer per stack
    /// (it acts later), then Stagger is spent.</item>
    /// <item><b>Haste</b> (on yourself): your actions take less time, <see cref="CombatConfig.StatusHasteSpeedPerStack"/> faster per
    /// stack; one stack falls off after each of your actions.</item>
    /// <item><b>Rattle</b> (on a foe): −1 to its rolls per stack for its next attack (the roll penalty status).</item>
    /// <item><b>Mark</b> (existing, now enforced): the next hit that lands on the target is a critical.</item>
    /// </list>
    /// </summary>
    public static class TempoStatuses
    {
        private static CombatConfig Cfg => GameConfiguration.Instance.Combat;

        public static int MaxStacks => Math.Max(1, Cfg.StatusExtraMaxStacks);

        public static int Haste(Actor? a) => a != null && a.HasteStacks > 0 ? a.HasteStacks : 0;
        public static int Stagger(Actor? a) => a?.StaggerStacks ?? 0;
        public static int Rattle(Actor? a) => a != null && a.RollPenaltyTurns > 0 ? Math.Max(0, a.RollPenalty) : 0;

        public static int AddStagger(Actor a, int n)
        {
            int before = a.StaggerStacks;
            a.StaggerStacks = Math.Min(MaxStacks, a.StaggerStacks + n);
            return a.StaggerStacks - before;
        }

        public static int AddHaste(Actor a, int n)
        {
            int before = a.HasteStacks;
            a.HasteStacks = Math.Min(MaxStacks, a.HasteStacks + n);
            return a.HasteStacks - before;
        }

        public static int AddRattle(Actor a, int n)
        {
            int current = Rattle(a);
            int next = Math.Min(MaxStacks, current + n);
            a.ApplyRollPenalty(next, Math.Max(1, a.RollPenaltyTurns));
            return next - current;
        }

        /// <summary>Spends up to <paramref name="max"/> of a stack status; returns how much was spent.</summary>
        public static int Spend(Actor a, string id, int max)
        {
            switch (id)
            {
                case "stagger":
                {
                    int n = Math.Min(max, a.StaggerStacks);
                    a.StaggerStacks -= n;
                    return n;
                }
                case "haste":
                {
                    int n = Math.Min(max, a.HasteStacks);
                    a.HasteStacks -= n;
                    return n;
                }
                case "rattle":
                {
                    int have = Rattle(a);
                    int n = Math.Min(max, have);
                    if (n >= have) { a.RollPenalty = 0; a.RollPenaltyTurns = 0; }
                    else a.RollPenalty = have - n;
                    return n;
                }
            }
            return 0;
        }

        // ------------------------------------------------------------------ combat hooks

        /// <summary>
        /// Before the roll: a marked target takes a critical from the next hit that lands (the attacker's crit threshold
        /// drops to 1 for this swing; the damage path reads the same threshold).
        /// </summary>
        public static void BeforeRoll(Actor source, Actor? target, Combat.ThresholdManager thresholds)
        {
            if (target == null || ReferenceEquals(source, target) || !target.IsMarked || target.MarkTurns <= 0)
                return;
            thresholds.SetCriticalHitThreshold(source, 1);
        }

        /// <summary>
        /// After hit / crit are known: Mark is spent by a landed hit.
        /// </summary>
        public static void AfterRoll(Actor source, Actor? target, bool hit, bool critical)
        {
            if (target == null || ReferenceEquals(source, target) || !hit)
                return;
            if (target.IsMarked && target.MarkTurns > 0)
            {
                target.IsMarked = false;
                target.MarkTurns = 0;
            }
        }

        /// <summary>One action's time with Stagger (spent here) and Haste applied.</summary>
        public static double AdjustDuration(Actor entity, double duration)
        {
            int stagger = entity.StaggerStacks;
            if (stagger > 0)
            {
                duration *= 1.0 + stagger * Math.Max(0, Cfg.StatusStaggerLengthPerStack);
                entity.StaggerStacks = 0;
            }
            int haste = Haste(entity);
            if (haste > 0)
                duration /= 1.0 + haste * Math.Max(0, Cfg.StatusHasteSpeedPerStack);
            return duration;
        }

        /// <summary>After the holder acts: one Haste stack falls off.</summary>
        public static void AfterOwnAction(Actor actor)
        {
            if (actor.HasteStacks > 0)
                actor.HasteStacks--;
        }
    }
}
