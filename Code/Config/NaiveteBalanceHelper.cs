using System;

namespace RPGGame
{
    public static class NaiveteBalanceHelper
    {
        public static int GetBaseAttributeSum(Character character)
        {
            if (character == null)
                return 0;

            return character.Strength
                + character.Agility
                + character.Technique
                + character.Intelligence;
        }

        public static NaiveteConfig? GetConfig()
        {
            return GameConfiguration.Instance?.EarlyGame?.Naivete;
        }

        public static bool AppliesTo(Character? character)
        {
            if (character == null || character is Enemy)
                return false;

            var config = GetConfig();
            return config != null && config.Enabled;
        }

        /// <summary>
        /// Single-digit naiveté: starts at <see cref="NaiveteConfig.StartingNaivete"/> (default 3)
        /// and subtracts 1 per hero level after level 1.
        /// </summary>
        public static int ComputeNaivete(Character character)
        {
            if (!AppliesTo(character))
                return 0;

            var config = GetConfig()!;
            int level = Math.Max(1, character.Level);
            int levelPenalty = level - 1;
            return Math.Max(0, config.StartingNaivete - levelPenalty);
        }

        public static int GetHitStepsFromNaivete(int naivete, NaiveteConfig? config = null)
        {
            config ??= GetConfig();
            if (config == null || !config.Enabled || naivete <= 0)
                return 0;

            if (config.MaxHitStepsFromNaivete <= 0)
                return 0;

            int pointsPerStep = config.NaivetePointsPerHitStep > 0
                ? config.NaivetePointsPerHitStep
                : 1;
            int steps = naivete / pointsPerStep;
            return Math.Min(config.MaxHitStepsFromNaivete, steps);
        }

        public static int GetHitSteps(Character character)
        {
            if (!AppliesTo(character))
                return 0;

            return GetHitStepsFromNaivete(ComputeNaivete(character));
        }
    }
}
