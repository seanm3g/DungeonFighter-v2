using System;

namespace RPGGame
{
    public enum SettlementType
    {
        Rural,
        Town,
        City
    }

    /// <summary>
    /// Chooses which spawn-tier weight profile applies for an encounter.
    /// Order: location override in tuning config → region default → location-name heuristic → Rural.
    /// </summary>
    public static class SettlementTypeResolver
    {
        public static SettlementType Resolve(string? location, TravelRegion? region, EnemySystemConfig? config = null)
        {
            config ??= GameConfiguration.Instance?.EnemySystem;

            if (!string.IsNullOrWhiteSpace(location) && config?.LocationSettlementOverrides != null)
            {
                var key = location.Trim();
                if (config.LocationSettlementOverrides.TryGetValue(key, out var overrideType))
                    return Parse(overrideType);
            }

            if (region != null && !string.IsNullOrWhiteSpace(region.SettlementType))
                return Parse(region.SettlementType);

            return ClassifyFromLocationName(location);
        }

        public static SettlementType ClassifyFromLocationName(string? location)
        {
            if (string.IsNullOrWhiteSpace(location))
                return SettlementType.Rural;

            var lower = location.Trim().ToLowerInvariant();
            if (lower.Contains("city"))
                return SettlementType.City;
            if (lower.Contains("town") || lower.Contains("village"))
                return SettlementType.Town;

            return SettlementType.Rural;
        }

        public static SettlementType Parse(string? value)
        {
            if (string.Equals(value, "Town", StringComparison.OrdinalIgnoreCase))
                return SettlementType.Town;
            if (string.Equals(value, "City", StringComparison.OrdinalIgnoreCase))
                return SettlementType.City;
            return SettlementType.Rural;
        }

        public static string ToConfigKey(SettlementType type) => type switch
        {
            SettlementType.Town => "Town",
            SettlementType.City => "City",
            _ => "Rural"
        };
    }
}
