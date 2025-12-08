using System.Collections.Generic;
using RPGGame;

namespace RPGGame.Combat.Effects.AdvancedStatusEffects
{
    /// <summary>
    /// Handler for Silence status effect - disables combo
    /// </summary>
    public class SilenceEffectHandler : IEffectHandler
    {
        public bool Apply(Actor target, Action action, List<string> results)
        {
            if (target == null) return false;

            // Apply silence - disables combo
            target.IsSilenced = true;
            target.SilenceTurns = 2; // Default duration

            results.Add($"    {target.Name} is silenced, combos disabled!");
            return true;
        }

        public string GetEffectType()
        {
            return "silence";
        }
    }
}

