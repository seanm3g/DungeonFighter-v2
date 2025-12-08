using System.Collections.Generic;
using RPGGame;

namespace RPGGame.Combat.Effects.AdvancedStatusEffects
{
    /// <summary>
    /// Handler for Focus status effect - increases outgoing damage
    /// </summary>
    public class FocusEffectHandler : IEffectHandler
    {
        public bool Apply(Actor target, Action action, List<string> results)
        {
            if (target == null) return false;

            // Apply focus - increases outgoing damage
            target.FocusStacks = (target.FocusStacks ?? 0) + 1;
            target.FocusTurns = 3; // Default duration

            results.Add($"    {target.Name} focuses, increasing damage dealt!");
            return true;
        }

        public string GetEffectType()
        {
            return "focus";
        }
    }
}

