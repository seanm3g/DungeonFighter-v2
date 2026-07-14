using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.ActionInteractionLab
{
    /// <summary>
    /// Builds seeded <see cref="Dungeon"/> instances for Action Lab room-play and dungeon batch sims.
    /// </summary>
    public static class ActionLabDungeonFactory
    {
        public sealed class Result
        {
            public Result(Dungeon dungeon, int seedUsed, string catalogName, string theme)
            {
                Dungeon = dungeon;
                SeedUsed = seedUsed;
                CatalogName = catalogName;
                Theme = theme;
            }

            public Dungeon Dungeon { get; }
            public int SeedUsed { get; }
            public string CatalogName { get; }
            public string Theme { get; }
        }

        /// <summary>
        /// Lists distinct dungeon catalog names from <c>Dungeons.json</c> (sorted).
        /// </summary>
        public static IReadOnlyList<string> ListCatalogDungeonNames()
        {
            var manager = new DungeonManagerWithRegistry();
            var all = manager.LoadAllDungeons();
            return all.Select(d => d.name)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Lists distinct themes from the dungeon catalog (sorted).
        /// </summary>
        public static IReadOnlyList<string> ListCatalogThemes()
        {
            var manager = new DungeonManagerWithRegistry();
            var all = manager.LoadAllDungeons();
            return all.Select(d => d.theme)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Generates a dungeon for the lab. When <paramref name="seed"/> is null, a random seed is chosen and returned.
        /// <paramref name="dungeonLevel"/> is both min and max level for room/enemy scaling.
        /// </summary>
        public static Result Generate(
            string catalogNameOrTheme,
            int dungeonLevel,
            int? seed = null,
            string? spawnRegionId = null)
        {
            int level = Math.Clamp(dungeonLevel, 1, 99);
            int seedUsed = seed ?? Random.Shared.Next();

            var manager = new DungeonManagerWithRegistry();
            var all = manager.LoadAllDungeons();
            DungeonData? data = all.FirstOrDefault(d =>
                string.Equals(d.name, catalogNameOrTheme, StringComparison.OrdinalIgnoreCase));
            if (data == null)
            {
                data = all.FirstOrDefault(d =>
                    string.Equals(d.theme, catalogNameOrTheme, StringComparison.OrdinalIgnoreCase));
            }

            string name;
            string theme;
            List<string>? possibleEnemies;
            ColorOverride? colorOverride;
            if (data != null)
            {
                name = data.name;
                theme = data.theme;
                possibleEnemies = data.possibleEnemies;
                colorOverride = data.colorOverride;
            }
            else
            {
                name = string.IsNullOrWhiteSpace(catalogNameOrTheme) ? "Action Lab Dungeon" : catalogNameOrTheme.Trim();
                theme = name;
                possibleEnemies = null;
                colorOverride = null;
            }

            var dungeon = new Dungeon(
                name,
                level,
                level,
                theme,
                possibleEnemies,
                colorOverride,
                spawnRegionId,
                generationSeed: seedUsed);
            dungeon.Generate();
            return new Result(dungeon, seedUsed, name, theme);
        }

        /// <summary>Hostile rooms only (combat targets for lab room stepping).</summary>
        public static IReadOnlyList<Environment> GetHostileRooms(Dungeon dungeon) =>
            dungeon.Rooms.Where(r => r.IsHostile).ToList();

        public static int DeriveDungeonRunSeed(int batchSeed, int runIndex)
        {
            unchecked
            {
                uint h = (uint)batchSeed;
                h ^= (uint)runIndex * 0x9E3779B1u;
                h ^= h >> 16;
                h *= 0x85EBCA6Bu;
                h ^= h >> 13;
                h *= 0xC2B2AE35u;
                h ^= h >> 16;
                return (int)h;
            }
        }
    }
}
