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
            
            if (target.PoisonPercentOfMaxHealth > 0)
            {
                target.PoisonPercentOfMaxHealth = Math.Max(0, target.PoisonPercentOfMaxHealth - 1);
                stacksRemoved++;
            }
            if (target.BurnIntensity > 0)
            {
                target.BurnIntensity = Math.Max(0, target.BurnIntensity - 1);
                stacksRemoved++;
            }
            else if (target.PendingBurnFromHits > 0)
            {
                target.PendingBurnFromHits = Math.Max(0, target.PendingBurnFromHits - 1);
                stacksRemoved++;
            }
            if (target.BleedIntensity > 0)
            {
                target.BleedIntensity = Math.Max(0, target.BleedIntensity - 1);
                stacksRemoved++;
            }
            else if (target.PendingBleedFromHits > 0)
            {
                target.PendingBleedFromHits = Math.Max(0, target.PendingBleedFromHits - 1);
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

