using System.Collections.Generic;
using RPGGame;

namespace RPGGame.Combat.Effects.AdvancedStatusEffects
{
    /// <summary>
    /// Handler for Absorb status effect - stores damage and releases at threshold
    /// </summary>
    public class AbsorbEffectHandler : IEffectHandler
    {
        public bool Apply(Actor target, Action action, List<string> results)
        {
            if (target == null) return false;

            // Apply absorb - stores damage
            target.HasAbsorb = true;
            target.AbsorbTurns = 5; // Default duration
            target.AbsorbThreshold = 50; // Release at 50 damage stored
            target.AbsorbedDamage = 0; // Initialize

            results.Add($"    {target.Name} begins absorbing damage!");
            return true;
        }

        public string GetEffectType()
        {
            return "absorb";
        }
    }
}

