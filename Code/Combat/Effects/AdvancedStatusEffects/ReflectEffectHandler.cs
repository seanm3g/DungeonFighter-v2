using System.Collections.Generic;
using RPGGame;

namespace RPGGame.Combat.Effects.AdvancedStatusEffects
{
    /// <summary>
    /// Handler for Reflect status effect - returns damage to attacker
    /// </summary>
    public class ReflectEffectHandler : IEffectHandler
    {
        public bool Apply(Actor target, Action action, List<string> results)
        {
            if (target == null) return false;

            // Apply reflect - returns damage to attacker
            target.ReflectStacks = (target.ReflectStacks ?? 0) + 1;
            target.ReflectTurns = 3; // Default duration
            target.ReflectPercentage = (target.ReflectPercentage ?? 0) + 25; // 25% damage reflected per stack

            results.Add($"    {target.Name} gains damage reflection!");
            return true;
        }

        public string GetEffectType()
        {
            return "reflect";
        }
    }
}

