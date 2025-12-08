using System.Collections.Generic;
using RPGGame;

namespace RPGGame.Combat.Effects.AdvancedStatusEffects
{
    /// <summary>
    /// Handler for Mark status effect - next hit is guaranteed crit
    /// </summary>
    public class MarkEffectHandler : IEffectHandler
    {
        public bool Apply(Actor target, Action action, List<string> results)
        {
            if (target == null) return false;

            // Apply mark - next hit is guaranteed crit
            target.IsMarked = true;
            target.MarkTurns = 1; // Usually just for next attack

            results.Add($"    {target.Name} is marked, next hit will crit!");
            return true;
        }

        public string GetEffectType()
        {
            return "mark";
        }
    }
}

