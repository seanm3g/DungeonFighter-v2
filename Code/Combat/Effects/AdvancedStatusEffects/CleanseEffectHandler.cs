using System;
using System.Collections.Generic;
using RPGGame;

namespace RPGGame.Combat.Effects.AdvancedStatusEffects
{
    /// <summary>
    /// Handler for Cleanse status effect - reduces stacks of negative effects
    /// </summary>
    public class CleanseEffectHandler : IEffectHandler
    {
        public bool Apply(Actor target, Action action, List<string> results)
        {
            if (target == null) return false;

            // Apply cleanse - reduces negative effect stacks
            int stacksRemoved = 0;
            
            if (target.PoisonStacks > 0)
            {
                target.PoisonStacks = Math.Max(0, target.PoisonStacks - 1);
                stacksRemoved++;
            }
            if (target.BurnStacks > 0)
            {
                target.BurnStacks = Math.Max(0, target.BurnStacks - 1);
                stacksRemoved++;
            }
            if (target.BleedStacks > 0)
            {
                target.BleedStacks = Math.Max(0, target.BleedStacks - 1);
                stacksRemoved++;
            }

            if (stacksRemoved > 0)
            {
                results.Add($"    {target.Name} is cleansed, negative effects reduced!");
            }

            return stacksRemoved > 0;
        }

        public string GetEffectType()
        {
            return "cleanse";
        }
    }
}

