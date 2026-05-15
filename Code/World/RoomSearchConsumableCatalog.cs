using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>One row from <c>Consumables.json</c> (or Google Sheets CONSUMABLES tab after pull).</summary>
    public sealed class RoomSearchConsumableDefinition
    {
        public RoomSearchConsumableDefinition(string displayName, RoomSearchConsumableKind kind, string potencyRaw)
        {
            DisplayName = displayName ?? "";
            Kind = kind;
            PotencyRaw = string.IsNullOrWhiteSpace(potencyRaw) ? "1" : potencyRaw.Trim();
        }

        public string DisplayName { get; }
        public RoomSearchConsumableKind Kind { get; }
        public string PotencyRaw { get; }
    }

    /// <summary>Loads room-search consumable definitions from <see cref="GameConstants.ConsumablesJson"/>.</summary>
    public static class RoomSearchConsumableCatalog
    {
        private static readonly object Gate = new();
        private static IReadOnlyList<RoomSearchConsumableDefinition>? _cache;

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        /// <summary>Clears cached definitions so the next read reloads from disk (e.g. after Sheets PULL).</summary>
        public static void Reload()
        {
            lock (Gate)
            {
                _cache = null;
            }
        }

        public static IReadOnlyList<RoomSearchConsumableDefinition> GetDefinitions()
        {
            if (_cache != null)
                return _cache;
            lock (Gate)
            {
                _cache ??= LoadOrDefault();
                return _cache;
            }
        }

        private static IReadOnlyList<RoomSearchConsumableDefinition> LoadOrDefault()
        {
            try
            {
                string? path = JsonLoader.FindGameDataFile(GameConstants.ConsumablesJson);
                if (path != null && File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var rows = JsonSerializer.Deserialize<List<ConsumableJsonRow>>(json, SerializerOptions);
                    var list = new List<RoomSearchConsumableDefinition>();
                    if (rows != null)
                    {
                        foreach (var r in rows)
                        {
                            if (r == null || string.IsNullOrWhiteSpace(r.DisplayName))
                                continue;
                            RoomSearchConsumableKind kind = ParseKind(r.InternalKind);
                            if (kind == RoomSearchConsumableKind.None)
                                continue;
                            list.Add(new RoomSearchConsumableDefinition(r.DisplayName.Trim(), kind, r.Potency ?? "1"));
                        }
                    }

                    if (list.Count > 0)
                        return list;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("RoomSearchConsumableCatalog: " + ex.Message);
            }

            return ShippedDefaultDefinitions();
        }

        private sealed class ConsumableJsonRow
        {
            public string DisplayName { get; set; } = "";
            public string InternalKind { get; set; } = "";
            public string? Effect { get; set; }
            public string Potency { get; set; } = "1";
        }

        private static RoomSearchConsumableKind ParseKind(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return RoomSearchConsumableKind.None;
            raw = raw.Trim();
            if (Enum.TryParse<RoomSearchConsumableKind>(raw, ignoreCase: true, out var k))
                return k;
            string compact = raw.Replace(" ", "", StringComparison.Ordinal);
            return Enum.TryParse<RoomSearchConsumableKind>(compact, ignoreCase: true, out var k2) ? k2 : RoomSearchConsumableKind.None;
        }

        /// <summary>Matches shipped <c>Consumables.json</c> when the file is missing or invalid.</summary>
        private static IReadOnlyList<RoomSearchConsumableDefinition> ShippedDefaultDefinitions()
        {
            RoomSearchConsumableDefinition D(string name, RoomSearchConsumableKind kind, string potency) =>
                new(name, kind, potency);

            return new[]
            {
                D("Vial of Iron Blood", RoomSearchConsumableKind.PotionStrength, "25%"),
                D("Elixir of Quicksilver", RoomSearchConsumableKind.PotionAgility, "25%"),
                D("Tincture of Fine Motion", RoomSearchConsumableKind.PotionTechnique, "25%"),
                D("Draught of Clear Thought", RoomSearchConsumableKind.PotionIntelligence, "25%"),
                D("Oil of True Aim", RoomSearchConsumableKind.PotionHit, "2"),
                D("Serum of Flow", RoomSearchConsumableKind.PotionCombo, "3"),
                D("Essence of Razor's Edge", RoomSearchConsumableKind.PotionCrit, "2"),
                D("Balm of Steady Hands", RoomSearchConsumableKind.PotionCritMiss, "1"),
                D("Waxed Apple", RoomSearchConsumableKind.Food, "1"),
                D("Travel's Ration", RoomSearchConsumableKind.Food, "3"),
                D("Salted Bread", RoomSearchConsumableKind.Food, "5"),
                D("Camp Jerky", RoomSearchConsumableKind.Food, "5%"),
                D("Smoked Cheese", RoomSearchConsumableKind.Food, "10%"),
                D("Honeyed Sausage", RoomSearchConsumableKind.Food, "25%")
            };
        }
    }
}
