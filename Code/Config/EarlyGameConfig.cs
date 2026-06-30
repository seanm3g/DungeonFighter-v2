using System;

namespace RPGGame
{
    /// <summary>
    /// Tunable knobs for level 1–early progression: starter gear, starting actions, and bonus class points.
    /// </summary>
    public class EarlyGameConfig
    {
        /// <summary>Hero levels 1 through this cap receive <see cref="StartingActionDamageMultiplier"/> on starting-weapon actions.</summary>
        public int StartingActionBonusLevelCap { get; set; } = 5;

        public StartingClassPointsBonusConfig StartingClassPointsBonus { get; set; } = new();
        public StartingActionDamageMultiplierConfig StartingActionDamageMultiplier { get; set; } = new();
        public NaiveteConfig Naivete { get; set; } = new();

        public void EnsureValidDefaults()
        {
            if (StartingActionBonusLevelCap <= 0)
                StartingActionBonusLevelCap = 5;

            StartingClassPointsBonus ??= new StartingClassPointsBonusConfig();
            StartingClassPointsBonus.EnsureValidDefaults();

            StartingActionDamageMultiplier ??= new StartingActionDamageMultiplierConfig();
            StartingActionDamageMultiplier.EnsureValidDefaults();

            Naivete ??= new NaiveteConfig();
            Naivete.EnsureValidDefaults();
        }
    }

    public class StartingClassPointsBonusConfig
    {
        public int Mace { get; set; }
        public int Sword { get; set; }
        public int Dagger { get; set; }
        public int Wand { get; set; }

        public void EnsureValidDefaults()
        {
            Mace = Math.Clamp(Mace, 0, 5);
            Sword = Math.Clamp(Sword, 0, 5);
            Dagger = Math.Clamp(Dagger, 0, 5);
            Wand = Math.Clamp(Wand, 0, 5);
        }
    }

    public class StartingActionDamageMultiplierConfig
    {
        public double Mace { get; set; } = 1.0;
        public double Sword { get; set; } = 1.0;
        public double Dagger { get; set; } = 1.0;
        public double Wand { get; set; } = 1.0;

        public void EnsureValidDefaults()
        {
            if (Mace <= 0) Mace = 1.0;
            if (Sword <= 0) Sword = 1.0;
            if (Dagger <= 0) Dagger = 1.0;
            if (Wand <= 0) Wand = 1.0;
        }
    }
}
