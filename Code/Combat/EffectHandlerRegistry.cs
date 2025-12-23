using System;
using System.Collections.Generic;

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
            _handlers = new Dictionary<string, IEffectHandler>
            {
                { "bleed", new BleedEffectHandler() },
                { "weaken", new WeakenEffectHandler() },
                { "slow", new SlowEffectHandler() },
                { "poison", new PoisonEffectHandler() },
                { "stun", new StunEffectHandler() },
                { "burn", new BurnEffectHandler() },
                // Advanced status effects (Phase 2)
                { "vulnerability", new Combat.Effects.AdvancedStatusEffects.VulnerabilityEffectHandler() },
                { "harden", new Combat.Effects.AdvancedStatusEffects.HardenEffectHandler() },
                { "fortify", new Combat.Effects.AdvancedStatusEffects.FortifyEffectHandler() },
                { "focus", new Combat.Effects.AdvancedStatusEffects.FocusEffectHandler() },
                { "expose", new Combat.Effects.AdvancedStatusEffects.ExposeEffectHandler() },
                { "hpregen", new Combat.Effects.AdvancedStatusEffects.HPRegenEffectHandler() },
                { "armorbreak", new Combat.Effects.AdvancedStatusEffects.ArmorBreakEffectHandler() },
                { "pierce", new Combat.Effects.AdvancedStatusEffects.PierceEffectHandler() },
                { "reflect", new Combat.Effects.AdvancedStatusEffects.ReflectEffectHandler() },
                { "silence", new Combat.Effects.AdvancedStatusEffects.SilenceEffectHandler() },
                { "statdrain", new Combat.Effects.AdvancedStatusEffects.StatDrainEffectHandler() },
                { "absorb", new Combat.Effects.AdvancedStatusEffects.AbsorbEffectHandler() },
                { "temporaryhp", new Combat.Effects.AdvancedStatusEffects.TemporaryHPEffectHandler() },
                { "confusion", new Combat.Effects.AdvancedStatusEffects.ConfusionEffectHandler() },
                { "cleanse", new Combat.Effects.AdvancedStatusEffects.CleanseEffectHandler() },
                { "mark", new Combat.Effects.AdvancedStatusEffects.MarkEffectHandler() },
                { "disrupt", new Combat.Effects.AdvancedStatusEffects.DisruptEffectHandler() }
            };
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
            // Create a dummy attacker for the calculation
            var dummyAttacker = new Character("Dummy", 1);
            if (CombatCalculator.CalculateStatusEffectChance(action, dummyAttacker, target))
            {
                var bleedConfig = GameConfiguration.Instance.StatusEffects.Bleed;
                target.ApplyPoison(bleedConfig.DamagePerTick, bleedConfig.StacksPerApplication, true);
                
                // Format with proper indentation and color markup
                string actorPattern = target is Enemy ? "enemy" : "player";
                results.Add($"    [{{{{{actorPattern}|{target.Name}}}}}] is {{{{error|bleeding}}}}!");
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
            // Create a dummy attacker for the calculation
            var dummyAttacker = new Character("Dummy", 1);
            if (CombatCalculator.CalculateStatusEffectChance(action, dummyAttacker, target))
            {
                target.ApplyWeaken(2); // 2 turns of weaken
                results.Add($"    [{target.Name}] is weakened!");
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
            // Create a dummy attacker for the calculation
            var dummyAttacker = new Character("Dummy", 1);
            if (CombatCalculator.CalculateStatusEffectChance(action, dummyAttacker, target))
            {
                // For now, just add a message - would need proper slow implementation
                results.Add($"    [{target.Name}] is slowed!");
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
            // Create a dummy attacker for the calculation
            var dummyAttacker = new Character("Dummy", 1);
            if (CombatCalculator.CalculateStatusEffectChance(action, dummyAttacker, target))
            {
                var poisonConfig = GameConfiguration.Instance.StatusEffects.Poison;
                target.ApplyPoison(poisonConfig.DamagePerTick, poisonConfig.StacksPerApplication);
                results.Add($"    [{target.Name}] is poisoned!");
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
            // Create a dummy attacker for the calculation
            var dummyAttacker = new Character("Dummy", 1);
            if (CombatCalculator.CalculateStatusEffectChance(action, dummyAttacker, target))
            {
                var stunConfig = GameConfiguration.Instance.StatusEffects.Stun;
                target.IsStunned = true;
                target.StunTurnsRemaining = stunConfig.SkipTurns;
                results.Add($"    [{target.Name}] is stunned!");
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
            // Create a dummy attacker for the calculation
            var dummyAttacker = new Character("Dummy", 1);
            if (CombatCalculator.CalculateStatusEffectChance(action, dummyAttacker, target))
            {
                var burnConfig = GameConfiguration.Instance.StatusEffects.Burn;
                target.ApplyBurn(burnConfig.DamagePerTick, burnConfig.MaxStacks);
                results.Add($"    [{target.Name}] is burning!");
                return true;
            }
            return false;
        }

        public string GetEffectType() => "Burning";
    }
}


