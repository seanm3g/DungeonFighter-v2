using System.Collections.Generic;
using RPGGame;

namespace RPGGame.Combat.Effects.AdvancedStatusEffects
{
    /// <summary>
    /// Handler for Pierce status effect - ignores armor
    /// </summary>
    public class PierceEffectHandler : IEffectHandler
    {
        public bool Apply(Actor target, Action action, List<string> results)
        {
            if (target == null) return false;

            // Apply pierce - next attack ignores armor
            target.HasPierce = true;
            target.PierceTurns = 1; // Usually just for next attack

            results.Add($"    {target.Name} is pierced, armor will be ignored!");
            return true;
        }

        public string GetEffectType()
        {
            return "pierce";
        }
    }
}

