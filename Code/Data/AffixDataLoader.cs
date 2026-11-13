using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RPGGame.Data
{
    /// <summary>One enemy affix effect (a region family at a rank, or a dungeon affix). See <c>EnemyAffixes.json</c>.</summary>
    public sealed class EnemyAffixDef
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        /// <summary>onhit / start / hp / damage / speed / lifesteal.</summary>
        [JsonPropertyName("lever")] public string Lever { get; set; } = "";
        [JsonPropertyName("status")] public string Status { get; set; } = "";
        [JsonPropertyName("chance")] public double Chance { get; set; }
        [JsonPropertyName("stacks")] public int Stacks { get; set; }
        [JsonPropertyName("value")] public double Value { get; set; }
        /// <summary>Damage lever only: applies while HP is at or below this fraction (0 = always).</summary>
        [JsonPropertyName("belowHealth")] public double BelowHealth { get; set; }
        /// <summary>Taken lever only: hero weapon types it applies to (mace / sword / dagger / wand); empty = every hit.</summary>
        [JsonPropertyName("weapons")] public List<string> Weapons { get; set; } = new();
        [JsonPropertyName("description")] public string Description { get; set; } = "";
    }

    /// <summary>Region element family: one affix whose strength scales with the region's cycle rank (1-5).</summary>
    public sealed class EnemyRegionFamily
    {
        [JsonPropertyName("theme")] public string Theme { get; set; } = "";
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("lever")] public string Lever { get; set; } = "";
        [JsonPropertyName("status")] public string Status { get; set; } = "";
        [JsonPropertyName("valuePerRank")] public double ValuePerRank { get; set; }
        [JsonPropertyName("chancePerRank")] public double ChancePerRank { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; } = "";

        /// <summary>The family's affix at <paramref name="rank"/> ("Rooted III").</summary>
        public EnemyAffixDef AtRank(int rank)
        {
            rank = Math.Max(1, rank);
            return new EnemyAffixDef
            {
                Name = $"{Name} {ToRoman(rank)}",
                Lever = Lever,
                Status = Status,
                Value = ValuePerRank * rank,
                Chance = Math.Min(1.0, ChancePerRank * rank),
                Description = Description
                    .Replace("{pct}", $"{ValuePerRank * rank * 100:0}%")
                    .Replace("{chance}", $"{Math.Min(1.0, ChancePerRank * rank) * 100:0}%")
            };
        }

        internal static string ToRoman(int n) => n switch
        {
            1 => "I", 2 => "II", 3 => "III", 4 => "IV", 5 => "V", 6 => "VI", 7 => "VII", _ => n.ToString()
        };
    }

    /// <summary>A dungeon theme's identity: signature kit, material, affix and Demon title.</summary>
    public sealed class EnemyDungeonIdentity
    {
        [JsonPropertyName("theme")] public string Theme { get; set; } = "";
        [JsonPropertyName("region")] public string Region { get; set; } = "";
        [JsonPropertyName("material")] public string Material { get; set; } = "";
        [JsonPropertyName("signature")] public List<string> Signature { get; set; } = new();
        [JsonPropertyName("demon")] public string Demon { get; set; } = "";
        [JsonPropertyName("affix")] public EnemyAffixDef? Affix { get; set; }
    }

    public sealed class EnemyWanderConfig
    {
        [JsonPropertyName("roomChance")] public double RoomChance { get; set; }
        [JsonPropertyName("enemyChance")] public double EnemyChance { get; set; }
    }

    public sealed class EnemyDemonConfig
    {
        [JsonPropertyName("regionRankBonus")] public int RegionRankBonus { get; set; } = 1;
        [JsonPropertyName("maxActions")] public int MaxActions { get; set; } = 5;
        [JsonPropertyName("phaseAtHealth")] public double PhaseAtHealth { get; set; } = 0.5;
        [JsonPropertyName("phaseRegionMultiplier")] public double PhaseRegionMultiplier { get; set; } = 2.0;
    }

    public sealed class EnemyAffixFile
    {
        [JsonPropertyName("wander")] public EnemyWanderConfig Wander { get; set; } = new();
        [JsonPropertyName("regionFamilies")] public List<EnemyRegionFamily> RegionFamilies { get; set; } = new();
        [JsonPropertyName("dungeons")] public List<EnemyDungeonIdentity> Dungeons { get; set; } = new();
        [JsonPropertyName("demon")] public EnemyDemonConfig Demon { get; set; } = new();

        public EnemyRegionFamily? FamilyFor(string? regionTheme) =>
            string.IsNullOrWhiteSpace(regionTheme) ? null
                : RegionFamilies.FirstOrDefault(f => string.Equals(f.Theme, regionTheme.Trim(), StringComparison.OrdinalIgnoreCase));

        public EnemyDungeonIdentity? DungeonFor(string? dungeonTheme) =>
            string.IsNullOrWhiteSpace(dungeonTheme) ? null
                : Dungeons.FirstOrDefault(d => string.Equals(d.Theme, dungeonTheme.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Authored room tag that changes the room (always on).</summary>
    public sealed class RoomTerrainDef
    {
        [JsonPropertyName("tag")] public string Tag { get; set; } = "";
        [JsonPropertyName("hazardStatus")] public string HazardStatus { get; set; } = "";
        [JsonPropertyName("critShift")] public int CritShift { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; } = "";
    }

    /// <summary>Rolled room affix (one per room at <see cref="RoomAffixFile.RollChance"/>).</summary>
    public sealed class RoomAffixDef
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("weight")] public double Weight { get; set; } = 1;
        [JsonPropertyName("hazardDamage")] public double HazardDamage { get; set; } = 1.0;
        [JsonPropertyName("activityStep")] public int ActivityStep { get; set; }
        [JsonPropertyName("hazardStatus")] public string HazardStatus { get; set; } = "";
        [JsonPropertyName("hazardTargets")] public string HazardTargets { get; set; } = "";
        [JsonPropertyName("extraHazardActs")] public int ExtraHazardActs { get; set; }
        [JsonPropertyName("extraEnemies")] public int ExtraEnemies { get; set; }
        [JsonPropertyName("forceDungeonAffix")] public bool ForceDungeonAffix { get; set; }
        [JsonPropertyName("prefixChanceMultiplier")] public double PrefixChanceMultiplier { get; set; } = 1.0;
        [JsonPropertyName("enemyStartStatus")] public string EnemyStartStatus { get; set; } = "";
        [JsonPropertyName("enemyStartStacks")] public int EnemyStartStacks { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; } = "";
    }

    public sealed class RoomAffixFile
    {
        [JsonPropertyName("rollChance")] public double RollChance { get; set; }
        [JsonPropertyName("maxEnemies")] public int MaxEnemies { get; set; } = 3;
        [JsonPropertyName("elementStatus")] public Dictionary<string, string> ElementStatus { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        [JsonPropertyName("terrain")] public List<RoomTerrainDef> Terrain { get; set; } = new();
        [JsonPropertyName("affixes")] public List<RoomAffixDef> Affixes { get; set; } = new();
    }

    /// <summary>Loads and caches <c>EnemyAffixes.json</c> and <c>RoomAffixes.json</c>. Missing files mean no affixes.</summary>
    public static class AffixDataLoader
    {
        public const string EnemyFileName = "EnemyAffixes.json";
        public const string RoomFileName = "RoomAffixes.json";
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
        private static readonly object CacheLock = new();
        private static EnemyAffixFile? _enemy;
        private static RoomAffixFile? _room;

        public static EnemyAffixFile Enemy
        {
            get { lock (CacheLock) return _enemy ??= Load<EnemyAffixFile>(EnemyFileName); }
        }

        public static RoomAffixFile Room
        {
            get { lock (CacheLock) return _room ??= Load<RoomAffixFile>(RoomFileName); }
        }

        /// <summary>Replaces the cached files (tests); null clears so the next read reloads from disk.</summary>
        public static void SetForTests(EnemyAffixFile? enemy, RoomAffixFile? room)
        {
            lock (CacheLock)
            {
                _enemy = enemy;
                _room = room;
            }
        }

        private static T Load<T>(string fileName) where T : new()
        {
            string? path = GameConstants.TryGetExistingGameDataFilePath(fileName);
            try
            {
                return path != null && File.Exists(path)
                    ? JsonSerializer.Deserialize<T>(File.ReadAllText(path), JsonOptions) ?? new T()
                    : new T();
            }
            catch (JsonException ex)
            {
                ErrorHandler.LogWarning($"{fileName} failed to load: {ex.Message}", "AffixDataLoader");
                return new T();
            }
        }
    }
}
