using System;
using System.Collections.Generic;
using RPGGame.Combat.Effects;
using RPGGame.UI.ColorSystem.Applications;

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
                { "weaken", new WeakenEffectHandler() },
                { "slow", new SlowEffectHandler() },
                { "poison", new PoisonEffectHandler() },
                { "stun", new StunEffectHandler() },
                { "burn", new BurnEffectHandler() }
            };
            foreach (var kv in StackTurnEffectHandler.CreateRegistryEntries())
                _handlers[kv.Key] = kv.Value;
            _handlers["absorb"] = new Combat.Effects.AdvancedStatusEffects.AbsorbEffectHandler();
            _handlers["confusion"] = new Combat.Effects.AdvancedStatusEffects.ConfusionEffectHandler();
            _handlers["cleanse"] = new Combat.Effects.AdvancedStatusEffects.CleanseEffectHandler();
            _handlers["disrupt"] = new Combat.Effects.AdvancedStatusEffects.DisruptEffectHandler();
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
        public bool ApplyEffect(string effectType, Actor target, Action action, List<string> results)
        {
            if (_handlers.TryGetValue(effectType.ToLower(), out var handler))
            {
                return handler.Apply(target, action, results);
            }
            return false;
        }

        /// <summary>
        /// Gets the effect type description for an action
        /// </summary>
        public string GetEffectType(Action action)
        {
            foreach (var handler in _handlers.Values)
            {
                if (IsActionCausingEffect(action, handler.GetEffectType()))
                {
                    return handler.GetEffectType();
                }
            }
            return "Damage";
        }

        /// <summary>
        /// Checks if an action causes a specific effect
        /// </summary>
        private bool IsActionCausingEffect(Action action, string effectType)
        {
            return effectType.ToLower() switch
            {
                "bleeding" => action.CausesBleed,
                "weakening" => action.CausesWeaken,
                "slowing" => action.CausesSlow,
                "poisoning" => action.CausesPoison,
                "stunning" => action.CausesStun,
                "burning" => action.CausesBurn,
                "vulnerability" => action.CausesVulnerability,
                "harden" => action.CausesHarden,
                "fortify" => action.CausesFortify,
                "focus" => action.CausesFocus,
                "expose" => action.CausesExpose,
                "hpregen" => action.CausesHPRegen,
                "armorbreak" => action.CausesArmorBreak,
                "pierce" => action.CausesPierce,
                "reflect" => action.CausesReflect,
                "silence" => action.CausesSilence,
                "statdrain" => action.CausesStatDrain,
                "absorb" => action.CausesAbsorb,
                "temporaryhp" => action.CausesTemporaryHP,
                "confusion" => action.CausesConfusion,
                "cleanse" => action.CausesCleanse,
                "mark" => action.CausesMark,
                "disrupt" => action.CausesDisrupt,
                _ => false
            };
        }
    }

    /// <summary>
    /// Handler for bleed effects
    /// </summary>
    public class BleedEffectHandler : IEffectHandler
    {
        public bool Apply(Actor target, Action action, List<string> results)
        {
            // Apply bleed if action causes it (guaranteed application when flag is set)
            if (action.CausesBleed)
            {
                var bleedConfig = GameConfiguration.Instance.StatusEffects.Bleed;
                // Use fallback defaults if config values are 0 (e.g., in tests or if config not loaded)
                int damagePerTick = bleedConfig.DamagePerTick > 0 ? bleedConfig.DamagePerTick : 1;
                int stacksPerApplication = bleedConfig.StacksPerApplication > 0 ? bleedConfig.StacksPerApplication : 1;
                target.ApplyPoison(damagePerTick, stacksPerApplication, true);
                
                // Format with proper indentation and color markup (5 spaces to match roll info)
                string actorPattern = target is Enemy ? "enemy" : "player";
                results.Add($"     {{{{actorPattern}}|" + $"{target.Name}" + "}} is " + $"{{{{error|bleeding}}}}!");
                return true;
            }
            return false;
        }

        public string GetEffectType() => "Bleeding";
    }

    /// <summary>
    /// Handler for weaken effects
    /// </summary>
    public class WeakenEffectHandler : IEffectHandler
    {
        public bool Apply(Actor target, Action action, List<string> results)
        {
            // Apply weaken if action causes it (guaranteed application when flag is set)
            if (action.CausesWeaken)
            {
                target.ApplyWeaken(2); // 2 turns of weaken
                // Format with proper indentation and color markup (5 spaces to match roll info)
                string actorPattern = target is Enemy ? "enemy" : "player";
                results.Add($"     {{{{actorPattern}}|" + $"{target.Name}" + "}} is " + $"{{{{weakened|weakened}}}}!");
                return true;
            }
            return false;
        }

        public string GetEffectType() => "Weakening";
    }

    /// <summary>
    /// Handler for slow effects
    /// </summary>
    public class SlowEffectHandler : IEffectHandler
    {
        public bool Apply(Actor target, Action action, List<string> results)
        {
            // Apply slow if action causes it (guaranteed application when flag is set)
            if (action.CausesSlow)
            {
                // Apply slow effect to the target (Enemy inherits from Character, so this works for both)
                if (target is Character character)
                {
                    // Use Freeze config for Slow since they're functionally similar (both reduce speed)
                    var freezeConfig = GameConfiguration.Instance.StatusEffects.Freeze;
                    // Use fallback defaults if config values are 0 (e.g., in tests or if config not loaded)
                    double speedReduction = freezeConfig.SpeedReduction > 0 ? freezeConfig.SpeedReduction : 0.5;
                    int duration = freezeConfig.Duration > 0 ? (int)freezeConfig.Duration : 3;
                    character.ApplySlow(speedReduction, duration);
                }
                
                // Format with proper indentation and color markup (5 spaces to match roll info)
                string actorPattern = target is Enemy ? "enemy" : "player";
                results.Add($"     {{{{actorPattern}}|" + $"{target.Name}" + "}} is " + $"{{{{slowed|slowed}}}}!");
                return true;
            }
            return false;
        }

        public string GetEffectType() => "Slowing";
    }

    /// <summary>
    /// Handler for poison effects
    /// </summary>
    public class PoisonEffectHandler : IEffectHandler
    {
        public bool Apply(Actor target, Action action, List<string> results)
        {
            // Apply poison if action causes it (guaranteed application when flag is set)
            if (action.CausesPoison)
            {
                var poisonConfig = GameConfiguration.Instance.StatusEffects.Poison;
                // Use fallback defaults if config values are 0 (e.g., in tests or if config not loaded)
                int damagePerTick = poisonConfig.DamagePerTick > 0 ? poisonConfig.DamagePerTick : 3;
                int stacksPerApplication = poisonConfig.StacksPerApplication > 0 ? poisonConfig.StacksPerApplication : 1;
                target.ApplyPoison(damagePerTick, stacksPerApplication);
                // Format with proper indentation and color markup (5 spaces to match roll info)
                string actorPattern = target is Enemy ? "enemy" : "player";
                results.Add($"     {{{{actorPattern}}|" + $"{target.Name}" + "}} is " + $"{{{{poisoned|poisoned}}}}!");
                return true;
            }
            return false;
        }

        public string GetEffectType() => "Poisoning";
    }

    /// <summary>
    /// Handler for stun effects
    /// </summary>
    public class StunEffectHandler : IEffectHandler
    {
        public bool Apply(Actor target, Action action, List<string> results)
        {
            // Apply stun if action causes it (guaranteed application when flag is set)
            if (action.CausesStun)
            {
                var stunConfig = GameConfiguration.Instance.StatusEffects.Stun;
                target.IsStunned = true;
                // Use fallback defaults if config values are 0 (e.g., in tests or if config not loaded)
                int skipTurns = stunConfig.SkipTurns > 0 ? stunConfig.SkipTurns : 1;
                target.StunTurnsRemaining = skipTurns;
                // Format with proper indentation and color markup (5 spaces to match roll info)
                string actorPattern = target is Enemy ? "enemy" : "player";
                results.Add($"     {{{{actorPattern}}|" + $"{target.Name}" + "}} is " + $"{{{{stunned|stunned}}}}!");
                return true;
            }
            return false;
        }

        public string GetEffectType() => "Stunning";
    }

    /// <summary>
    /// Handler for burn effects
    /// </summary>
    public class BurnEffectHandler : IEffectHandler
    {
        public bool Apply(Actor target, Action action, List<string> results)
        {
            // Apply burn if action causes it (guaranteed application when flag is set)
            if (action.CausesBurn)
            {
                var burnConfig = GameConfiguration.Instance.StatusEffects.Burn;
                // Use fallback defaults if config values are 0 (e.g., in tests or if config not loaded)
                int damagePerTick = burnConfig.DamagePerTick > 0 ? burnConfig.DamagePerTick : 3;
                int stacksPerApplication = burnConfig.StacksPerApplication > 0 ? burnConfig.StacksPerApplication : 1;
                target.ApplyBurn(damagePerTick, stacksPerApplication);
                // Format with proper indentation and color markup (5 spaces to match roll info)
                string actorPattern = target is Enemy ? "enemy" : "player";
                results.Add($"     {{{{actorPattern}}|" + $"{target.Name}" + "}} is " + $"{{{{burning|burning}}}}!");
                return true;
            }
            return false;
        }

        public string GetEffectType() => "Burning";
    }
}


