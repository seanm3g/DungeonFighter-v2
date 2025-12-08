using System.Collections.Generic;
using RPGGame;

namespace RPGGame.Combat.Effects.AdvancedStatusEffects
{
    /// <summary>
    /// Handler for Confusion status effect - chance to attack self/ally
    /// </summary>
    public class ConfusionEffectHandler : IEffectHandler
    {
        public bool Apply(Actor target, Action action, List<string> results)
        {
            if (target == null) return false;

            // Apply confusion - chance to attack self/ally
            target.IsConfused = true;
            target.ConfusionTurns = 2; // Default duration
            target.ConfusionChance = 0.3; // 30% chance

            results.Add($"    {target.Name} is confused!");
            return true;
        }

        public string GetEffectType()
        {
            return "confusion";
        }
    }
}

