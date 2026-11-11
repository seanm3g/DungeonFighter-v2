using System.Collections.Generic;
using RPGGame.Combat.Events;

namespace RPGGame.Combat
{
    /// <summary>
    /// Before/after picture of a target's debuffs across one hit, so every path that applies a status (the action, gear
    /// mods, item triggers, trigger bundles, on-hit skills) is seen the same way. <see cref="PublishGained"/> fires one
    /// <see cref="CombatEventType.StatusEffectApplied"/> per status that was added or stacked higher.
    /// Status names: poison, burn, acid, bleed, stun, weaken, slow, vulnerability, expose, armorbreak, mark, confusion,
    /// silence, statdrain.
    /// </summary>
    public readonly struct StatusApplySnapshot
    {
        private readonly double _poison;
        private readonly int _burn, _acid, _bleed, _stun, _weaken, _slowTurns, _expose, _mark,
            _confusion, _silence, _statDrain;
        private readonly double _slowMult, _exposeStrip;

        public static readonly string[] StatusNames =
        {
            "poison", "burn", "acid", "bleed", "stun", "weaken", "slow", "expose",
            "mark", "confusion", "silence", "statdrain"
        };

        private StatusApplySnapshot(Actor t)
        {
            _poison = t.PoisonPercentOfMaxHealth;
            _burn = t.BurnIntensity + t.PendingBurnFromHits;
            _acid = t.AcidIntensity + t.PendingAcidFromHits;
            _bleed = t.BleedIntensity + t.PendingBleedFromHits;
            _stun = t.IsStunned ? t.StunTurnsRemaining : 0;
            _weaken = t.IsWeakened ? t.WeakenTurns : 0;
            var c = t as Character;
            _slowTurns = c?.Effects.SlowTurns ?? 0;
            _slowMult = c?.Effects.SlowMultiplier ?? 1.0;
            _expose = t.ExposeStacks ?? 0;
            _exposeStrip = t.ExposeArmorStrip;
            _mark = t.IsMarked ? t.MarkTurns : 0;
            _confusion = t.IsConfused ? t.ConfusionTurns : 0;
            _silence = t.IsSilenced ? t.SilenceTurns : 0;
            _statDrain = t.StatDrainStacks ?? 0;
        }

        public static StatusApplySnapshot Take(Actor target) => new(target);

        /// <summary>Statuses the target gained since this snapshot (added, stacked higher or extended).</summary>
        public List<string> Gained(Actor target)
        {
            var now = new StatusApplySnapshot(target);
            var gained = new List<string>();
            if (now._poison > _poison + 1e-9) gained.Add("poison");
            if (now._burn > _burn) gained.Add("burn");
            if (now._acid > _acid) gained.Add("acid");
            if (now._bleed > _bleed) gained.Add("bleed");
            if (now._stun > _stun) gained.Add("stun");
            if (now._weaken > _weaken) gained.Add("weaken");
            if (now._slowTurns > _slowTurns || now._slowMult < _slowMult - 1e-9) gained.Add("slow");
            if (now._expose > _expose || now._exposeStrip > _exposeStrip + 1e-9) gained.Add("expose");
            if (now._mark > _mark) gained.Add("mark");
            if (now._confusion > _confusion) gained.Add("confusion");
            if (now._silence > _silence) gained.Add("silence");
            if (now._statDrain > _statDrain) gained.Add("statdrain");
            return gained;
        }

        /// <summary>Publishes one StatusEffectApplied per gained status (source applied it to target). Returns them.</summary>
        public List<string> PublishGained(Actor source, Actor target, Action? action)
        {
            var gained = Gained(target);
            if (ReferenceEquals(source, target)) return gained;
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
