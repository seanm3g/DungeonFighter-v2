using System.Collections.Generic;
using RPGGame;

namespace RPGGame.Combat.Effects.AdvancedStatusEffects
{
    /// <summary>
    /// Handler for HP Regen status effect - heals over time
    /// </summary>
    public class HPRegenEffectHandler : IEffectHandler
    {
        public bool Apply(Actor target, Action action, List<string> results)
        {
            if (target == null) return false;

            // Apply HP regen - heals over time
            target.HPRegenStacks = (target.HPRegenStacks ?? 0) + 1;
            target.HPRegenTurns = 3; // Default duration
            target.HPRegenAmount = (target.HPRegenAmount ?? 0) + 5; // +5 HP per turn per stack

            results.Add($"    {target.Name} begins regenerating health!");
            return true;
        }

        public string GetEffectType()
        {
            return "hpregen";
        }
    }
}

