using System;
using System.Collections.Generic;
using RPGGame.Combat.Effects;
using RPGGame.Combat.Formatting;
using RPGGame.Combat;

namespace RPGGame
{
    /// <summary>
    /// Interface for effect handlers to standardize effect application
    /// </summary>
    public interface IEffectHandler
    {
        bool Apply(Actor target, Action action, List<string> results);
        string GetEffectType();
    }

    /// <summary>
    /// Registry for managing different types of status effects
    /// Simplifies CombatEffects by using strategy pattern
    /// </summary>
    public class EffectHandlerRegistry
    {
        private readonly Dictionary<string, IEffectHandler> _handlers;

        public EffectHandlerRegistry()
        {
            _handlers = new Dictionary<string, IEffectHandler>(StringComparer.OrdinalIgnoreCase)
            {
                { "bleed", new BleedEffectHandler() },
                { "burn", new BurnEffectHandler() }
            };
            foreach (var kv in StackTurnEffectHandler.CreateRegistryEntries())
                _handlers[kv.Key] = kv.Value;
        }

        /// <summary>
        /// Gets the handler for an effect type (for tests or inspection).
        /// </summary>
        public IEffectHandler? GetHandler(string effectType)
        {
            return _handlers.TryGetValue(effectType?.ToLower() ?? "", out var h) ? h : null;
        }

        /// <summary>
        /// Applies a status effect using the appropriate handler
        /// </summary>
        public bool ApplyEffect(string effectType, Actor target, Action? action, List<string> results)
        {
            if (action == null || string.IsNullOrWhiteSpace(effectType))
                return false;
            // Step 5 (D3.13-D3.16): a retired status applies as its core replacement; a dropped one applies nothing.
            string? core = CoreStatuses.Resolve(effectType);
            if (core == null)
                return false;
            // Poison loads as Bleed (save migration table): a poison card, kit, affix or mod applies one Bleed
            // application, though its action carries the poison flag, not the bleed one.
            if (core == "bleed" && !action.CausesBleed && action.CausesPoison
                && !string.Equals(effectType.Trim(), "bleed", StringComparison.OrdinalIgnoreCase))
                action = new Action { Name = action.Name, CausesBleed = true, BleedAmountToAdd = Math.Max(1, action.BleedAmountToAdd) };
            effectType = core;
            // Immune targets never receive the status, so nothing is logged ("is poisoned" on a skeleton).
            if (StatusAffinity.IsImmune(target, effectType))
                return false;
            if (_handlers.TryGetValue(effectType.ToLower(), out var handler))
            {
                bool applied = handler.Apply(target, action, results);
                if (applied)
                    target?.NoteStatusSource(effectType, action.Name);
                return applied;
            }
            // Core statuses without a handler (Stagger, Rattle) apply one stack through their store.
            if (target != null && Stores.TryGet(effectType, out var def) && def.AddFn != null
                && Stores.Add(target, def.Id, 1) > 0)
            {
                results?.Add($"{target.Name} gains 1 {CoreStatuses.Label(def.Id)}");
                target.NoteStatusSource(def.Id, action.Name);
                return true;
            }
            return false;
        }

    }

    /// <summary>
    /// Handler for bleed effects
    /// </summary>
    public class BleedEffectHandler : IEffectHandler
    {
        public bool Apply(Actor target, Action action, List<string> results)
        {
            if (action.CausesBleed)
            {
                int amount = action.BleedAmountToAdd > 0 ? action.BleedAmountToAdd : 1;
                target.QueueBleedFromHit(amount);
                StatusEffectCombatLogMessageBuilder.AppendIsStatusLine(results, target, "bleeding");
                return true;
            }
            return false;
        }

        public string GetEffectType() => "Bleeding";
    }

    /// <summary>
    /// Handler for burn effects
    /// </summary>
    public class BurnEffectHandler : IEffectHandler
    {
        public bool Apply(Actor target, Action action, List<string> results)
        {
            if (action.CausesBurn)
            {
                int amount = action.BurnAmountToAdd > 0 ? action.BurnAmountToAdd : 1;
                target.QueueBurnFromHit(amount);
                StatusEffectCombatLogMessageBuilder.AppendIsStatusLine(results, target, "burning");
                return true;
            }
            return false;
        }

        public string GetEffectType() => "Burning";
    }
}
