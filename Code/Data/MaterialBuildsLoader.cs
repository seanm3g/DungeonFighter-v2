using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using RPGGame;

namespace RPGGame.Data
{
    /// <summary>Loads <c>MaterialBuilds.json</c> (MATERIAL BUILDS sheet) and caches parsed rows.</summary>
    public static class MaterialBuildsLoader
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        private static List<MaterialBuildData>? _cache;
        private static readonly object CacheLock = new();

        public static IReadOnlyList<MaterialBuildData> GetAll()
        {
            lock (CacheLock)
            {
                EnsureLoadedLocked();
                return _cache!;
            }
        }

        public static MaterialBuildData? FindByMaterial(string? material)
        {
            string key = MaterialBuildData.CanonicalMaterialName(material);
            if (key.Length == 0)
                return null;
            foreach (var row in GetAll())
            {
                if (string.Equals(row.Material, key, StringComparison.OrdinalIgnoreCase))
                    return row;
            }
            return null;
        }

        public static MaterialBuildData? FindByConvertAction(string? actionName)
        {
            if (string.IsNullOrWhiteSpace(actionName))
                return null;
            foreach (var row in GetAll())
            {
                if (string.Equals(row.ConvertAction, actionName.Trim(), StringComparison.OrdinalIgnoreCase))
                    return row;
            }
            return null;
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

        public static void Save(IReadOnlyList<MaterialBuildData> rows, string? path = null)
        {
            path ??= GameConstants.TryGetExistingGameDataFilePath(GameConstants.MaterialBuildsJson)
                ?? GameConstants.GetGameDataFilePath(GameConstants.MaterialBuildsJson);
            var list = new List<MaterialBuildData>(rows ?? Array.Empty<MaterialBuildData>());
            foreach (var row in list)
                row.RefreshParsedFields();
            string json = JsonSerializer.Serialize(list, JsonOptions);
            File.WriteAllText(path, json);
            lock (CacheLock)
                _cache = list;
        }

        private static void EnsureLoadedLocked()
        {
            if (_cache != null)
                return;

            var list = new List<MaterialBuildData>();
            string? path = JsonLoader.FindGameDataFile(GameConstants.MaterialBuildsJson);
            if (path != null && File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    var loaded = JsonSerializer.Deserialize<List<MaterialBuildData>>(json, JsonOptions);
                    if (loaded != null)
                        list.AddRange(loaded);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"MaterialBuildsLoader: failed to read {path}: {ex.Message}");
                }
            }

            foreach (var row in list)
                row.RefreshParsedFields();
            _cache = list;
        }
    }
}
