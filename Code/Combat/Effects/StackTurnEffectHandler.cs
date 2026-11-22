using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.Combat.Formatting;

namespace RPGGame.Combat.Effects
{
    /// <summary>
    /// Data-driven handler for status effects that only set stacks/turns and a message.
    /// Harden (Guard), Focus, Expose and Mark; retired ids resolve to these first (CoreStatuses).
    /// </summary>
    public sealed class StackTurnEffectHandler : IEffectHandler
    {
        private readonly string _effectType;
        private readonly int _defaultTurns;
        private readonly string _messageSuffix;
        private readonly Action<Actor> _apply;

        public StackTurnEffectHandler(string effectType, int defaultTurns, string messageSuffix, Action<Actor> apply)
        {
            _effectType = effectType ?? throw new ArgumentNullException(nameof(effectType));
            _defaultTurns = defaultTurns;
            _messageSuffix = messageSuffix ?? "";
            _apply = apply ?? throw new ArgumentNullException(nameof(apply));
        }

        public bool Apply(Actor target, Action action, List<string> results)
        {
            if (target == null) return false;
            _apply(target);
            StatusEffectCombatLogMessageBuilder.AppendNameWithPlainSuffix(results, target, _messageSuffix);
            return true;
        }

        public string GetEffectType() => _effectType;

        /// <summary>
        /// Creates registry entries for all stack/turn-based advanced status effects.
        /// </summary>
        public static Dictionary<string, IEffectHandler> CreateRegistryEntries()
        {
            const int defaultTurns = 3;
            return new Dictionary<string, IEffectHandler>(StringComparer.OrdinalIgnoreCase)
            {
                { "harden", new StackTurnEffectHandler("harden", defaultTurns, "hardens, reducing incoming damage!", t => { t.HardenStacks = (t.HardenStacks ?? 0) + 1; t.HardenTurns = defaultTurns; }) },
                { "focus", new StackTurnEffectHandler("focus", defaultTurns, "focuses, increasing damage dealt!", t => { t.FocusStacks = (t.FocusStacks ?? 0) + 1; t.FocusTurns = defaultTurns; }) },
                { "expose", new StackTurnEffectHandler("expose", defaultTurns, "is exposed!", t => ExposeStatus.Apply(t, 1, defaultTurns)) },
                { "mark", new StackTurnEffectHandler("mark", 1, "is marked, next hit will crit!", t => { t.IsMarked = true; t.MarkTurns = 1; }) }
            };
        }
    }
}
