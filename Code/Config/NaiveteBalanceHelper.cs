using System;
using RPGGame.Actions.Conditional;

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
        /// Max naiveté charges for this hero: starts at <see cref="NaiveteConfig.StartingNaivete"/> (default 5)
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

        /// <summary>
        /// Fight remaining charges when a battle pool is active; otherwise the level-based max.
        /// </summary>
        public static int GetDisplayNaivete(Character character)
        {
            if (!AppliesTo(character))
                return 0;

            if (CombatTriggerContext.HasNaivetePool(character))
                return CombatTriggerContext.GetNaiveteCharges(character);

            return ComputeNaivete(character);
        }

        /// <summary>
        /// Legacy HIT-step helper — always 0. Naiveté no longer shifts hit thresholds.
        /// </summary>
        public static int GetHitStepsFromNaivete(int naivete, NaiveteConfig? config = null) => 0;

        /// <summary>
        /// Legacy HIT-step helper — always 0. Naiveté no longer shifts hit thresholds.
        /// </summary>
        public static int GetHitSteps(Character character) => 0;
    }
}
