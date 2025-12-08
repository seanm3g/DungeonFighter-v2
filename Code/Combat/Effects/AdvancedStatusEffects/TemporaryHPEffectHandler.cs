using System.Collections.Generic;
using RPGGame;

namespace RPGGame.Combat.Effects.AdvancedStatusEffects
{
    /// <summary>
    /// Handler for Temporary HP status effect - overheal/shields
    /// </summary>
    public class TemporaryHPEffectHandler : IEffectHandler
    {
        public bool Apply(Actor target, Action action, List<string> results)
        {
            if (target == null) return false;

            // Apply temporary HP - overheal
            int tempHPAmount = 10; // Default amount
            target.TemporaryHP = (target.TemporaryHP ?? 0) + tempHPAmount;
            target.TemporaryHPTurns = 3; // Default duration

            results.Add($"    {target.Name} gains {tempHPAmount} temporary HP!");
            return true;
        }

        public string GetEffectType()
        {
            return "temporaryhp";
        }
    }
}

