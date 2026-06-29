using System;
using RPGGame.Combat.Calculators;

namespace RPGGame.Tuning
{
    /// <summary>
    /// Applies saved tuning configuration to a running player character (e.g. after Settings save).
    /// </summary>
    public static class PlayerTuningApplier
    {
        public static void ApplyToCurrentPlayer(Character? player)
        {
            if (player == null) return;
            ApplyMaxHealthFromTuning(player);
            ApplyBaseAttributesFromTuning(player);
            DamageCalculator.InvalidateCache(player);
        }

        private static void ApplyMaxHealthFromTuning(Character player)
        {
            int oldEffective = player.GetEffectiveMaxHealth();
            int computedMax = ComputeMaxHealthFromTuning(player);
            if (computedMax == player.MaxHealth)
                return;

            player.MaxHealth = computedMax;
            player.Health.AdjustHealthForMaxHealthChange(oldEffective, player.GetEffectiveMaxHealth());
        }

        /// <summary>
        /// Reapplies player base attributes from tuning config so in-combat damage reflects slider changes.
        /// Equipment and temporary bonuses remain on top of these bases.
        /// </summary>
        private static void ApplyBaseAttributesFromTuning(Character player)
        {
            var attrs = GameConfiguration.Instance.Attributes.PlayerBaseAttributes;
            player.Strength = Math.Max(0, attrs.Strength);
            player.Agility = Math.Max(0, attrs.Agility);
            player.Technique = Math.Max(0, attrs.Technique);
            player.Intelligence = Math.Max(0, attrs.Intelligence);
        }

        /// <summary>
        /// Recomputes base max health from current tuning and level-up history (class health multipliers included).
        /// </summary>
        internal static int ComputeMaxHealthFromTuning(Character character)
        {
            var tuning = GameConfiguration.Instance;
            int baseHealth = tuning.Character.PlayerBaseHealth;
            if (baseHealth <= 0)
                baseHealth = 60;

            int max = baseHealth;
            if (character.Level <= 1)
                return max;

            var levelUp = new LevelUpManager(character);
            for (int level = 2; level <= character.Level; level++)
                max += levelUp.GetHealthIncreaseForLevelStep();

            var prog = tuning.EnemySystem.ProgressionScales;
            prog?.EnsurePositiveScales();
            if (prog != null)
                max = Math.Max(1, (int)Math.Round(max * (1 + prog.PlayerEnemyParity * EnemyProgressionCurveEvaluator.ParityPlayerHpFactor)));

            return max;
        }
    }
}
