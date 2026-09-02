using System;

namespace RPGGame.Combat.Calculators
{
    /// <summary>
    /// Per-swing energy budget (3). Cost 1–3; leftover 2/1/0 lasts until the hero's next action.
    /// </summary>
    public static class LeftoverEnergy
    {
        public const int MaxEnergy = 3;
        public const int DefaultCost = 2;

        public static int ClampCost(int cost)
        {
            if (cost < 1 || cost > 3)
                return DefaultCost;
            return cost;
        }

        public static int LeftoverFromCost(int cost) => MaxEnergy - ClampCost(cost);

        public static int ResolveCost(Action? action) => ClampCost(action?.EnergyCost ?? DefaultCost);

        /// <summary>Hero-only: leftover from this swing covers incoming hits until the next hero action.</summary>
        public static void ApplyFromAction(Actor? actor, Action? action)
        {
            if (actor is Character hero && hero is not Enemy)
                hero.LeftoverEnergy = LeftoverFromCost(ResolveCost(action));
        }

        /// <summary>Combat / room start: Open until the hero acts.</summary>
        public static void Reset(Actor? actor)
        {
            if (actor is Character hero && hero is not Enemy)
            {
                hero.LeftoverEnergy = 0;
                hero.EnergyShieldCurrent = 0;
                hero.EnergyShieldMax = 0;
            }
        }
    }
}
