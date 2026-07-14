using System;

namespace RPGGame.Data
{
    /// <summary>
    /// Active Actions workshop set: only actions with <c>tier ≤ max</c> are available in gameplay,
    /// loot, and the Action Lab catalog. Settings → Actions still loads the full ingested JSON so
    /// switching to a higher set (or Through Tier max) can edit hidden rows.
    /// </summary>
    public static class ActionSetVisibility
    {
        /// <summary>
        /// Max inclusive tier from <see cref="GameSettings.ActionsActiveSetMaxTier"/>.
        /// <c>null</c> means no filter (all loaded actions are visible).
        /// </summary>
        public static int? MaxTierInclusive =>
            GameSettings.Instance?.ActionsActiveSetMaxTier;

        /// <summary>True when an action of this tier is in the active set.</summary>
        public static bool IsIncluded(int actionTier)
        {
            int? max = MaxTierInclusive;
            if (max == null)
                return true;
            return actionTier <= max.Value;
        }

        /// <summary>True when this action definition is in the active set.</summary>
        public static bool IsIncluded(ActionData? data)
        {
            if (data == null)
                return false;
            return IsIncluded(data.Tier);
        }

        /// <summary>
        /// Updates the persisted active set max tier and applies it immediately for gameplay lookups.
        /// </summary>
        /// <param name="maxTierInclusive">Inclusive max tier, or <c>null</c> for all tiers.</param>
        /// <param name="persist">When true, writes general settings to disk.</param>
        public static void SetMaxTierInclusive(int? maxTierInclusive, bool persist = true)
        {
            var settings = GameSettings.Instance;
            if (settings == null)
                return;

            if (maxTierInclusive.HasValue && maxTierInclusive.Value < 0)
                maxTierInclusive = null;

            if (settings.ActionsActiveSetMaxTier == maxTierInclusive)
                return;

            settings.ActionsActiveSetMaxTier = maxTierInclusive;
            if (persist)
                settings.SaveSettings();
        }

        /// <summary>
        /// Resolves the combobox selection: prefer a stored setting when still valid, otherwise max tier (show all).
        /// </summary>
        public static int ResolveInitialSelection(int dataMaxTier, int? storedMaxTier)
        {
            if (dataMaxTier < 0)
                dataMaxTier = 0;
            if (storedMaxTier.HasValue && storedMaxTier.Value >= 0 && storedMaxTier.Value <= dataMaxTier)
                return storedMaxTier.Value;
            return dataMaxTier;
        }
    }
}
