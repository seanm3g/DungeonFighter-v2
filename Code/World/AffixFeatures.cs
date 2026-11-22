using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Tool switches for the enemy / room affix features (DUNGEONSIM <c>--features-off</c>), so each one can be priced
    /// against the same seeds with everything else on. All on in play. Rolls still draw their random numbers when a
    /// feature is off, so the rest of a seeded run stays on the same stream as far as possible.
    /// </summary>
    public static class AffixFeatures
    {
        public const string RegionKey = "region";
        public const string DungeonKey = "dungeon";
        public const string PrefixKey = "prefix";
        public const string RoomKey = "room";
        public const string TerrainKey = "terrain";
        public const string WanderKey = "wander";
        public const string DemonKey = "demon";
        public const string KitsKey = "kits";
        public const string AllKey = "all";

        public static readonly string[] Keys = { RegionKey, DungeonKey, PrefixKey, RoomKey, TerrainKey, WanderKey, DemonKey, KitsKey };

        private static readonly HashSet<string> Off = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Region family affix on every enemy.</summary>
        public static bool Region => !Off.Contains(RegionKey);
        /// <summary>Dungeon affix on Rare enemies / Lair rooms / the Demon.</summary>
        public static bool Dungeon => !Off.Contains(DungeonKey);
        /// <summary>Prefix effects and prefix-tag affinities (off = prefixes only rename, as before).</summary>
        public static bool Prefix => !Off.Contains(PrefixKey);
        /// <summary>Rolled room affix.</summary>
        public static bool Room => !Off.Contains(RoomKey);
        /// <summary>Terrain tags (scorched / flooded / overgrown hazards, exposed crits).</summary>
        public static bool Terrain => !Off.Contains(TerrainKey);
        /// <summary>10% wandering rooms and enemies.</summary>
        public static bool Wander => !Off.Contains(WanderKey);
        /// <summary>Demon build (signature kit, affixes, phase) and the always-hostile last room.</summary>
        public static bool Demon => !Off.Contains(DemonKey);
        /// <summary>Per-enemy signature kits (off = the flavor aliases, as before).</summary>
        public static bool Kits => !Off.Contains(KitsKey);

        /// <summary>Comma list of feature keys to switch off ("all" = every feature); null or empty = all on.</summary>
        public static void SetOff(string? list)
        {
            Off.Clear();
            foreach (string key in (list ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (key.Equals(AllKey, StringComparison.OrdinalIgnoreCase))
                {
                    foreach (string k in Keys)
                        Off.Add(k);
                    continue;
                }
                if (!Keys.Contains(key, StringComparer.OrdinalIgnoreCase))
                    throw new ArgumentException($"--features-off: unknown feature '{key}' (use {string.Join(", ", Keys)} or all).");
                Off.Add(key);
            }
        }

        /// <summary>Features switched off, for logs.</summary>
        public static IReadOnlyCollection<string> OffList => Off.ToList();
    }
}
