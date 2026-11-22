using System.Collections.Generic;
using RPGGame.Combat.Events;

namespace RPGGame.Combat
{
    /// <summary>
    /// Before/after picture of a target's debuffs across one hit, so every path that applies a status (the action, gear
    /// mods, item triggers, trigger bundles, on-hit skills) is seen the same way. <see cref="PublishGained"/> fires one
    /// <see cref="CombatEventType.StatusEffectApplied"/> per status that was added or stacked higher.
    /// Status names: burn, bleed, slow, expose, mark, stagger, rattle.
    /// </summary>
    public readonly struct StatusApplySnapshot
    {
        private readonly int _burn, _bleed, _slowTurns, _expose, _mark, _stagger, _rattle;
        private readonly double _slowMult, _exposeStrip;

        public static readonly string[] StatusNames =
        {
            "burn", "bleed", "slow", "expose", "mark", "stagger", "rattle"
        };

        private StatusApplySnapshot(Actor t)
        {
            _burn = t.BurnIntensity + t.PendingBurnFromHits;
            _bleed = t.BleedIntensity + t.PendingBleedFromHits;
            var c = t as Character;
            _slowTurns = c?.Effects.SlowTurns ?? 0;
            _slowMult = c?.Effects.SlowMultiplier ?? 1.0;
            _expose = t.ExposeStacks ?? 0;
            _exposeStrip = t.ExposeArmorStrip;
            _mark = t.IsMarked ? t.MarkTurns : 0;
            _stagger = TempoStatuses.Stagger(t);
            _rattle = TempoStatuses.Rattle(t);
        }

        public static StatusApplySnapshot Take(Actor target) => new(target);

        /// <summary>Statuses the target gained since this snapshot (added, stacked higher or extended).</summary>
        public List<string> Gained(Actor target)
        {
            var now = new StatusApplySnapshot(target);
            var gained = new List<string>();
            if (now._burn > _burn) gained.Add("burn");
            if (now._bleed > _bleed) gained.Add("bleed");
            if (now._slowTurns > _slowTurns || now._slowMult < _slowMult - 1e-9) gained.Add("slow");
            if (now._expose > _expose || now._exposeStrip > _exposeStrip + 1e-9) gained.Add("expose");
            if (now._mark > _mark) gained.Add("mark");
            if (now._stagger > _stagger) gained.Add("stagger");
            if (now._rattle > _rattle) gained.Add("rattle");
            return gained;
        }

        /// <summary>Publishes one StatusEffectApplied per gained status (source applied it to target). Returns them.</summary>
        public List<string> PublishGained(Actor source, Actor target, Action? action)
        {
            var gained = Gained(target);
            if (ReferenceEquals(source, target)) return gained;
            RPGGame.Combat.Events.HeroEventHub.OnStatusesApplied(source, target, gained);
            foreach (var status in gained)
            {
                CombatEventBus.Instance.Publish(new CombatEvent(CombatEventType.StatusEffectApplied, source)
                {
                    Target = target,
                    Action = action,
                    StatusEffectType = status
                });
            }
            return gained;
        }
    }
}
