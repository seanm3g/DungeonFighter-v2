using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Interface for environmental effect handlers
    /// </summary>
    public interface IEnvironmentalEffectHandler
    {
        bool Apply(Actor target, Action action, List<string> results);
        string GetEffectType();
    }

    /// <summary>
    /// Registry for managing environmental effects
    /// Simplifies DungeonManager by using strategy pattern
    /// </summary>
    public class EnvironmentalEffectRegistry
    {
        private readonly Dictionary<string, IEnvironmentalEffectHandler> _handlers;

        public EnvironmentalEffectRegistry()
        {
            _handlers = new Dictionary<string, IEnvironmentalEffectHandler>
            {
                { "poison", new EnvironmentalPoisonHandler() },
                { "slow", new EnvironmentalSlowHandler() },
                { "weaken", new EnvironmentalWeakenHandler() },
                { "stun", new EnvironmentalStunHandler() },
                { "burn", new EnvironmentalBurnHandler() }
            };
        }

        /// <summary>
        /// Applies an environmental effect using the appropriate handler
        /// </summary>
        /// <param name="debuffType">Type of environmental effect</param>
        /// <param name="target">Target Actor</param>
        /// <param name="action">Action causing the effect</param>
        /// <param name="results">List to add effect messages to</param>
        /// <returns>True if effect was applied</returns>
        public bool ApplyEnvironmentalEffect(string debuffType, Actor target, Action action, List<string> results)
        {
            if (_handlers.TryGetValue(debuffType.ToLower(), out var handler))
            {
                return handler.Apply(target, action, results);
            }
            return false;
        }
    }

    /// <summary>
    /// Handler for environmental poison effects
    /// </summary>
    public class EnvironmentalPoisonHandler : IEnvironmentalEffectHandler
    {
        public bool Apply(Actor target, Action action, List<string> results)
        {
            if (action.CausesPoison)
            {
                target.ApplyPoison(3, 1);
                EnvironmentalMessageHelper.AddEnvironmentalMessage(results, $"[{target.Name}] is poisoned by the environment!");
                return true;
            }
            return false;
        }

        public string GetEffectType() => "poison";
    }

    /// <summary>
    /// Handler for environmental slow effects
    /// </summary>
    public class EnvironmentalSlowHandler : IEnvironmentalEffectHandler
    {
        public bool Apply(Actor target, Action action, List<string> results)
        {
            if (action.CausesSlow)
            {
                EnvironmentalMessageHelper.AddEnvironmentalMessage(results, $"[{target.Name}] is slowed by the environment!");
                return true;
            }
            return false;
        }

        public string GetEffectType() => "slow";
    }

    /// <summary>
    /// Handler for environmental weaken effects
    /// </summary>
    public class EnvironmentalWeakenHandler : IEnvironmentalEffectHandler
    {
        public bool Apply(Actor target, Action action, List<string> results)
        {
            if (action.CausesWeaken)
            {
                target.ApplyWeaken(2);
                EnvironmentalMessageHelper.AddEnvironmentalMessage(results, $"[{target.Name}] is weakened by the environment!");
                return true;
            }
            return false;
        }

        public string GetEffectType() => "weaken";
    }

    /// <summary>
    /// Handler for environmental stun effects
    /// </summary>
    public class EnvironmentalStunHandler : IEnvironmentalEffectHandler
    {
        public bool Apply(Actor target, Action action, List<string> results)
        {
            if (action.CausesStun)
            {
                target.IsStunned = true;
                target.StunTurnsRemaining = 1;
                EnvironmentalMessageHelper.AddEnvironmentalMessage(results, $"[{target.Name}] is stunned by the environment!");
                return true;
            }
            return false;
        }

        public string GetEffectType() => "stun";
    }

    /// <summary>
    /// Handler for environmental burn effects
    /// </summary>
    public class EnvironmentalBurnHandler : IEnvironmentalEffectHandler
    {
        public bool Apply(Actor target, Action action, List<string> results)
        {
            if (action.CausesBurn)
            {
                target.ApplyBurn(3, 1);
                EnvironmentalMessageHelper.AddEnvironmentalMessage(results, $"[{target.Name}] is burning from the environment!");
                return true;
            }
            return false;
        }

        public string GetEffectType() => "burn";
    }

    /// <summary>
    /// Helper class for adding environmental messages to results
    /// </summary>
    public static class EnvironmentalMessageHelper
    {
        /// <summary>
        /// Adds an environmental message to the results list
        /// </summary>
        /// <param name="results">Results list to add to</param>
        /// <param name="message">Message to add</param>
        public static void AddEnvironmentalMessage(List<string> results, string message)
        {
            // Append to last result if available, otherwise add as new result
            if (results.Count > 0)
            {
                results[results.Count - 1] += $"\n    {message}";
            }
            else
            {
                results.Add(message);
            }
        }
    }
}


