using System.Collections.Generic;
using RPGGame;

namespace RPGGame.Combat.Effects.AdvancedStatusEffects
{
    /// <summary>
    /// Handler for Armor Break status effect - reduces armor significantly
    /// </summary>
    public class ArmorBreakEffectHandler : IEffectHandler
    {
        public bool Apply(Actor target, Action action, List<string> results)
        {
            if (target == null) return false;

            // Apply armor break - significantly reduces armor
            target.ArmorBreakStacks = (target.ArmorBreakStacks ?? 0) + 1;
            target.ArmorBreakTurns = 2; // Default duration
            target.ArmorBreakReduction = (target.ArmorBreakReduction ?? 0) + 5; // -5 armor per stack

            results.Add($"    {target.Name}'s armor is broken!");
            return true;
        }

        public string GetEffectType()
        {
            return "armorbreak";
        }
    }
}

