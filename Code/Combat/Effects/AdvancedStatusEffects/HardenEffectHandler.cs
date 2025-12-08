using System.Collections.Generic;
using RPGGame;

namespace RPGGame.Combat.Effects.AdvancedStatusEffects
{
    /// <summary>
    /// Handler for Harden status effect - target takes less damage
    /// </summary>
    public class HardenEffectHandler : IEffectHandler
    {
        public bool Apply(Actor target, Action action, List<string> results)
        {
            if (target == null) return false;

            // Apply harden - reduces damage taken
            target.HardenStacks = (target.HardenStacks ?? 0) + 1;
            target.HardenTurns = 3; // Default duration

            results.Add($"    {target.Name} hardens, reducing incoming damage!");
            return true;
        }

        public string GetEffectType()
        {
            return "harden";
        }
    }
}

