using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.Combat.Formatting;

namespace RPGGame.Combat.Effects
{
    /// <summary>
    /// Data-driven handler for status effects that only set stacks/turns and a message.
    /// Replaces 13 nearly identical AdvancedStatusEffects handler classes.
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
                { "vulnerability", new StackTurnEffectHandler("vulnerability", defaultTurns, "is now vulnerable to damage!", t => { t.VulnerabilityStacks = (t.VulnerabilityStacks ?? 0) + 1; t.VulnerabilityTurns = defaultTurns; }) },
                { "harden", new StackTurnEffectHandler("harden", defaultTurns, "hardens, reducing incoming damage!", t => { t.HardenStacks = (t.HardenStacks ?? 0) + 1; t.HardenTurns = defaultTurns; }) },
                { "fortify", new StackTurnEffectHandler("fortify", defaultTurns, "is fortified!", t => { t.FortifyStacks = (t.FortifyStacks ?? 0) + 1; t.FortifyTurns = defaultTurns; t.FortifyArmorBonus = (t.FortifyArmorBonus ?? 0) + 2; }) },
                { "focus", new StackTurnEffectHandler("focus", defaultTurns, "focuses, increasing damage dealt!", t => { t.FocusStacks = (t.FocusStacks ?? 0) + 1; t.FocusTurns = defaultTurns; }) },
                { "expose", new StackTurnEffectHandler("expose", defaultTurns, "is exposed!", t => { t.ExposeStacks = (t.ExposeStacks ?? 0) + 1; t.ExposeTurns = defaultTurns; }) },
                { "hpregen", new StackTurnEffectHandler("hpregen", defaultTurns, "begins regenerating health!", t => { t.HPRegenStacks = (t.HPRegenStacks ?? 0) + 1; t.HPRegenTurns = defaultTurns; t.HPRegenAmount = (t.HPRegenAmount ?? 0) + 5; }) },
                { "armorbreak", new StackTurnEffectHandler("armorbreak", defaultTurns, "armor is broken!", t => { t.ArmorBreakStacks = (t.ArmorBreakStacks ?? 0) + 1; t.ArmorBreakTurns = defaultTurns; }) },
                { "pierce", new StackTurnEffectHandler("pierce", defaultTurns, "armor is pierced!", t => { t.HasPierce = true; t.PierceTurns = defaultTurns; }) },
                { "reflect", new StackTurnEffectHandler("reflect", defaultTurns, "gains damage reflect!", t => { t.ReflectStacks = (t.ReflectStacks ?? 0) + 1; t.ReflectTurns = defaultTurns; }) },
                { "silence", new StackTurnEffectHandler("silence", defaultTurns, "is silenced!", t => { t.IsSilenced = true; t.SilenceTurns = defaultTurns; }) },
                { "statdrain", new StackTurnEffectHandler("statdrain", defaultTurns, "stats are drained!", t => { t.StatDrainStacks = (t.StatDrainStacks ?? 0) + 1; t.StatDrainTurns = defaultTurns; }) },
                { "temporaryhp", new StackTurnEffectHandler("temporaryhp", defaultTurns, "gains 10 temporary HP!", t => { t.TemporaryHP = (t.TemporaryHP ?? 0) + 10; t.TemporaryHPTurns = defaultTurns; }) },
                { "mark", new StackTurnEffectHandler("mark", 1, "is marked, next hit will crit!", t => { t.IsMarked = true; t.MarkTurns = 1; }) }
            };
        }
    }
}
