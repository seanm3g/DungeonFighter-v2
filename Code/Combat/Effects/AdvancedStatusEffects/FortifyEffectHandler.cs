using System.Collections.Generic;
using RPGGame;

namespace RPGGame.Combat.Effects.AdvancedStatusEffects
{
    /// <summary>
    /// Handler for Fortify status effect - increases armor
    /// </summary>
    public class FortifyEffectHandler : IEffectHandler
    {
        public bool Apply(Actor target, Action action, List<string> results)
        {
            if (target == null) return false;

            // Apply fortify - increases armor
            target.FortifyStacks = (target.FortifyStacks ?? 0) + 1;
            target.FortifyTurns = 3; // Default duration
            target.FortifyArmorBonus = (target.FortifyArmorBonus ?? 0) + 2; // +2 armor per stack

            results.Add($"    {target.Name} is fortified, gaining armor!");
            return true;
        }

        public string GetEffectType()
        {
            return "fortify";
        }
    }
}

