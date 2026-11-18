using System;

namespace RPGGame
{
    /// <summary>
    /// Rebuild Q7 (D2.5, answered 2026-11-18): while a critical hit resolves, its other effects are amplified too, not
    /// just damage and the action's statuses. <see cref="Begin"/> opens a scope for the critting actor's swing;
    /// <see cref="For"/> returns the multiplier (the crit's applications, x2 today) for that actor and 1 for anyone else
    /// (a defender's ONTAKEHIT rules aren't amplified by the attacker's crit).
    /// <para>Amplified inside the scope: flat heals (heal actions, heal bundles, rule HEAL n), cadence grants (the
    /// action's TURN / ACTION / FIGHT / DUNGEON grants, its sheet modifiers and roll bonus), trigger bundle bonuses and
    /// statuses, and rule amounts (MINT, ADD, APPLY, BUFF, NEXT). Not amplified again: anything already read off the
    /// crit's damage (lifesteal, HEAL n% of damage, REFLECT) and same-swing damage / speed / AMP mods.</para>
    /// </summary>
    public static class CritAmplifier
    {
        [ThreadStatic] private static Actor? _source;
        [ThreadStatic] private static int _multiplier;

        /// <summary>Switch: false = a crit amplifies only damage and the action's own statuses (Rebuild 1.5).</summary>
        public static bool AmplifiesOtherEffects { get; set; } = true;

        /// <summary>Opens the crit scope for <paramref name="source"/>'s swing (a no-op scope when it isn't a crit).</summary>
        public static IDisposable Begin(Actor source, bool isCritical)
        {
            int mult = isCritical && AmplifiesOtherEffects ? GameConfiguration.Instance.Combat.CritEffectApplications : 1;
            if (mult <= 1 || source == null)
                return Scope.None;
            var previous = new Scope(_source, _multiplier);
            _source = source;
            _multiplier = mult;
            return previous;
        }

        /// <summary>The crit multiplier for effects <paramref name="actor"/> creates right now (1 outside its crit).</summary>
        public static int For(Actor? actor) =>
            actor != null && _multiplier > 1 && ReferenceEquals(actor, _source) ? _multiplier : 1;

        private sealed class Scope : IDisposable
        {
            public static readonly Scope None = new(null, 0, restores: false);
            private readonly Actor? source;
            private readonly int multiplier;
            private readonly bool restores;

            public Scope(Actor? source, int multiplier, bool restores = true)
            {
                this.source = source;
                this.multiplier = multiplier;
                this.restores = restores;
            }

            public void Dispose()
            {
                if (!restores)
                    return;
                _source = source;
                _multiplier = multiplier;
            }
        }
    }
}
