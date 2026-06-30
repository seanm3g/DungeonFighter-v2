using System;

namespace RPGGame
{
    /// <summary>
    /// Tunable knobs for naiveté: early-game hit forgiveness that fades as base attributes grow.
    /// </summary>
    public class NaiveteConfig
    {
        public bool Enabled { get; set; } = true;

        /// <summary>When base STR+AGI+TEC+INT reaches this sum, naiveté is zero.</summary>
        public int AttributeTotalCap { get; set; } = 66;

        /// <summary>Naiveté points required for one HIT threshold step (easier to hit).</summary>
        public int NaivetePointsPerHitStep { get; set; } = 2;

        /// <summary>Maximum HIT threshold steps granted from naiveté.</summary>
        public int MaxHitStepsFromNaivete { get; set; } = 4;

        public void EnsureValidDefaults()
        {
            if (AttributeTotalCap <= 0)
                AttributeTotalCap = 66;
            if (NaivetePointsPerHitStep <= 0)
                NaivetePointsPerHitStep = 2;
            if (MaxHitStepsFromNaivete < 0)
                MaxHitStepsFromNaivete = 0;
        }
    }
}
