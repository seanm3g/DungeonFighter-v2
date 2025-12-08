using System.Collections.Generic;
using RPGGame;

namespace RPGGame.Combat.Effects.AdvancedStatusEffects
{
    /// <summary>
    /// Handler for Expose status effect - reduces target armor
    /// </summary>
    public class ExposeEffectHandler : IEffectHandler
    {
        public bool Apply(Actor target, Action action, List<string> results)
        {
            if (target == null) return false;

            // Apply expose - reduces target armor
            target.ExposeStacks = (target.ExposeStacks ?? 0) + 1;
            target.ExposeTurns = 3; // Default duration
            target.ExposeArmorReduction = (target.ExposeArmorReduction ?? 0) + 2; // -2 armor per stack

            results.Add($"    {target.Name} is exposed, armor reduced!");
            return true;
        }

        public string GetEffectType()
        {
            return "expose";
        }
    }
}

