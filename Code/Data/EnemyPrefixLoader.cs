using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using RPGGame;

namespace RPGGame.Data
{
    /// <summary>One enemy prefix row from <c>EnemyPrefixes.json</c> (Undead, Giant, Demon, ...).</summary>
    public sealed class EnemyPrefixData
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
        /// <summary>Relative weight among eligible prefixes in the room roll (0 = never rolled).</summary>
        [JsonPropertyName("weight")]
        public double Weight { get; set; }
        /// <summary>Only the final boss gets it (<see cref="EnemyPrefixFile.BossPrefix"/>); never rolled for rooms.</summary>
        [JsonPropertyName("bossOnly")]
        public bool BossOnly { get; set; }
        [JsonPropertyName("minLevel")]
        public int MinLevel { get; set; }
        /// <summary>The enemy must have every one of these tags.</summary>
        [JsonPropertyName("requiresTags")]
        public List<string> RequiresTags { get; set; } = new();
        /// <summary>The enemy must have none of these tags.</summary>
        [JsonPropertyName("excludesTags")]
        public List<string> ExcludesTags { get; set; } = new();
        /// <summary>Tags the prefix adds (stored apart from the enemy's own tags until mechanics read them).</summary>
        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new();
        /// <summary>Intended mechanic (design note, not implemented).</summary>
        [JsonPropertyName("idea")]
        public string Idea { get; set; } = "";
    }

    /// <summary>Chance that a room enemy rolls a prefix: chance + level × chancePerLevel, capped at maxChance.</summary>
    public sealed class EnemyPrefixRoomRoll
    {
        [JsonPropertyName("chance")]
        public double Chance { get; set; }
        [JsonPropertyName("chancePerLevel")]
        public double ChancePerLevel { get; set; }
        [JsonPropertyName("maxChance")]
        public double MaxChance { get; set; } = 1.0;

        public double ChanceAt(int level) => Math.Clamp(Chance + Math.Max(0, level) * ChancePerLevel, 0.0, Math.Max(0.0, MaxChance));
    }

    public sealed class EnemyPrefixFile
    {
        [JsonPropertyName("roomRoll")]
        public EnemyPrefixRoomRoll RoomRoll { get; set; } = new();
        /// <summary>Prefix every dungeon's final boss gets.</summary>
        [JsonPropertyName("bossPrefix")]
        public string BossPrefix { get; set; } = "";
        [JsonPropertyName("prefixes")]
        public List<EnemyPrefixData> Prefixes { get; set; } = new();
    }

    /// <summary>Loads and caches <c>EnemyPrefixes.json</c>. A missing file means no prefixes.</summary>
    public static class EnemyPrefixLoader
    {
        public const string FileName = "EnemyPrefixes.json";
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
        private static EnemyPrefixFile? _cache;
        private static readonly object CacheLock = new();

        public static EnemyPrefixFile Get()
        {
            lock (CacheLock)
            {
                if (_cache != null)
                    return _cache;
                string? path = GameConstants.TryGetExistingGameDataFilePath(FileName);
                try
                {
                    _cache = path != null && File.Exists(path)
                        ? JsonSerializer.Deserialize<EnemyPrefixFile>(File.ReadAllText(path), JsonOptions) ?? new EnemyPrefixFile()
                        : new EnemyPrefixFile();
                }
                catch (JsonException)
                {
                    _cache = new EnemyPrefixFile();
                }
                return _cache;
            }
        }

        /// <summary>Replaces the cached file (tests) or clears it so the next <see cref="Get"/> reloads (null).</summary>
        public static void SetForTests(EnemyPrefixFile? file)
        {
            lock (CacheLock)
                _cache = file;
        }

        public static EnemyPrefixData? Find(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;
            foreach (var p in Get().Prefixes)
            {
                if (string.Equals(p.Name, name.Trim(), StringComparison.OrdinalIgnoreCase))
                    return p;
            }
            return null;
        }
    }
}
