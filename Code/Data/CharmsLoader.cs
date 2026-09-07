using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using RPGGame;

namespace RPGGame.Data
{
    /// <summary>Loads <c>Charms.json</c> (CHARMS sheet) and caches catalog rows.</summary>
    public static class CharmsLoader
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        private static List<CharmData>? _cache;
        private static readonly object CacheLock = new();

        public static IReadOnlyList<CharmData> GetAll()
        {
            lock (CacheLock)
            {
                EnsureLoadedLocked();
                return _cache!;
            }
        }

        public static CharmData? FindByName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;
            foreach (var row in GetAll())
            {
                if (string.Equals(row.Name, name.Trim(), StringComparison.OrdinalIgnoreCase))
                    return row;
            }
            return null;
        }

        public static CharmItem? CreateItemByName(string? name)
        {
            var row = FindByName(name);
            return row?.ToItem();
        }

        public static CharmItem? CreateRandomItem(Random? rng = null)
        {
            var all = GetAll();
            if (all.Count == 0)
                return null;
            rng ??= new Random();
            return all[rng.Next(all.Count)].ToItem();
        }

        public static void ClearCache()
        {
            lock (CacheLock)
                _cache = null;
        }

        public static void Reload()
        {
            ClearCache();
            _ = GetAll();
        }

        public static void Save(IReadOnlyList<CharmData> rows, string? path = null)
        {
            path ??= GameConstants.TryGetExistingGameDataFilePath(GameConstants.CharmsJson)
                ?? GameConstants.GetGameDataFilePath(GameConstants.CharmsJson);
            var list = new List<CharmData>(rows ?? Array.Empty<CharmData>());
            string json = JsonSerializer.Serialize(list, JsonOptions);
            File.WriteAllText(path, json);
            lock (CacheLock)
                _cache = list;
        }

        private static void EnsureLoadedLocked()
        {
            if (_cache != null)
                return;

            var list = new List<CharmData>();
            string? path = JsonLoader.FindGameDataFile(GameConstants.CharmsJson);
            if (path != null && File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    var loaded = JsonSerializer.Deserialize<List<CharmData>>(json, JsonOptions);
                    if (loaded != null)
                        list.AddRange(loaded);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"CharmsLoader: failed to read {path}: {ex.Message}");
                }
            }

            _cache = list;
        }
    }
}
