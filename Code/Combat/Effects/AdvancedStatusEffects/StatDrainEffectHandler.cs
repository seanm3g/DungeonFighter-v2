using System.Collections.Generic;
using RPGGame;

namespace RPGGame.Combat.Effects.AdvancedStatusEffects
{
    /// <summary>
    /// Handler for Stat Drain status effect - steals stats from target
    /// </summary>
    public class StatDrainEffectHandler : IEffectHandler
    {
        public bool Apply(Actor target, Action action, List<string> results)
        {
            if (target == null) return false;

            // Apply stat drain - steals stats
            target.StatDrainStacks = (target.StatDrainStacks ?? 0) + 1;
            target.StatDrainTurns = 3; // Default duration
            target.StatDrainAmount = (target.StatDrainAmount ?? 0) + 1; // -1 stat per stack

            results.Add($"    {target.Name} has stats drained!");
            return true;
        }

        public string GetEffectType()
        {
            return "statdrain";
        }
    }
}

