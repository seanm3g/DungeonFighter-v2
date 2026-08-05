using System;

namespace RPGGame
{
    /// <summary>
    /// Tunable knobs for naiveté: early-game miss→advantage charges that fade one point per hero level.
    /// Displayed value is a single digit (legacy attribute remainder ÷ 10 scale).
    /// </summary>
    public class NaiveteConfig
    {
        public bool Enabled { get; set; } = true;

        /// <summary>Naiveté at level 1 (single digit). Decreases by 1 each level after.</summary>
        public int StartingNaivete { get; set; } = 5;

        /// <summary>
        /// Legacy: points required for one HIT threshold step. Unused — naiveté is miss→advantage charges.
        /// Kept for older balance patches.
        /// </summary>
        public int NaivetePointsPerHitStep { get; set; } = 1;

        /// <summary>
        /// Legacy: maximum HIT threshold steps from naiveté. Unused — kept for older balance patches.
        /// </summary>
        public int MaxHitStepsFromNaivete { get; set; } = 0;

        /// <summary>
        /// Legacy: when base STR+AGI+TEC+INT reached this sum, naiveté was zero.
        /// Kept for older balance patches; unused by the level-based formula.
        /// </summary>
        public int AttributeTotalCap { get; set; } = 66;

        public void EnsureValidDefaults()
        {
            if (StartingNaivete < 0)
                StartingNaivete = 0;
            if (NaivetePointsPerHitStep <= 0)
                NaivetePointsPerHitStep = 1;
            if (MaxHitStepsFromNaivete < 0)
                MaxHitStepsFromNaivete = 0;
            if (AttributeTotalCap <= 0)
                AttributeTotalCap = 66;
        }
    }
}
