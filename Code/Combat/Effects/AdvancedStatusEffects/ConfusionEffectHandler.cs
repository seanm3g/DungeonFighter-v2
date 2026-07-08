using System.Collections.Generic;
using RPGGame;

namespace RPGGame.Combat.Effects.AdvancedStatusEffects
{
    /// <summary>
    /// Handler for Confusion status effect — randomizes combat targets for the duration.
    /// </summary>
    public class ConfusionEffectHandler : IEffectHandler
    {
        public bool Apply(Actor target, Action action, List<string> results)
        {
            if (target == null) return false;

            int duration = action.ComboBonusDuration > 0 ? action.ComboBonusDuration : 2;
            target.IsConfused = true;
            target.ConfusionTurns = duration;
            target.ConfusionChance = 1.0;

            results.Add($"    {target.Name} is confused!");
            return true;
        }

        public string GetEffectType()
        {
            return "confusion";
        }
    }
}

