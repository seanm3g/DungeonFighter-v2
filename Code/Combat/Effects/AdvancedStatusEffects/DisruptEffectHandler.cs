using System.Collections.Generic;
using RPGGame;

namespace RPGGame.Combat.Effects.AdvancedStatusEffects
{
    /// <summary>
    /// Handler for Disrupt status effect - resets combo
    /// </summary>
    public class DisruptEffectHandler : IEffectHandler
    {
        public bool Apply(Actor target, Action action, List<string> results)
        {
            if (target == null) return false;

            // Apply disrupt - resets combo
            if (target is Character character)
            {
                character.ResetCombo();
                results.Add($"    {target.Name}'s combo is disrupted!");
                return true;
            }

            return false;
        }

        public string GetEffectType()
        {
            return "disrupt";
        }
    }
}

