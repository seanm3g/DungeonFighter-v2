using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using RPGGame;

namespace RPGGame.Data
{
    /// <summary>
    /// Loads and caches <c>Triggers.json</c> (item trigger identity catalog from the triggers sheet).
    /// </summary>
    public static class TriggersLoader
    {
        private static readonly object Gate = new();
        private static List<TriggerIdentityData>? _cache;
        private static Dictionary<string, TriggerIdentityData>? _byName;

        private static readonly JsonSerializerOptions ReadOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        public static IReadOnlyList<TriggerIdentityData> GetAll()
        {
            EnsureLoaded();
            return _cache!;
        }

        public static int Count
        {
            get
            {
                EnsureLoaded();
                return _cache!.Count;
            }
        }

        public static TriggerIdentityData GetByIndex(int index)
        {
            EnsureLoaded();
            if (_cache!.Count == 0)
                throw new InvalidOperationException("Triggers.json has no identities.");
            if (index < 0)
                index = 0;
            return _cache[index % _cache.Count];
        }

        public static bool TryGetByName(string? name, out TriggerIdentityData identity)
        {
            EnsureLoaded();
            identity = null!;
            if (string.IsNullOrWhiteSpace(name) || _byName == null)
                return false;
            return _byName.TryGetValue(name.Trim(), out identity!);
        }

        public static TriggerIdentityData? FindByName(string? name) =>
            TryGetByName(name, out var id) ? id : null;

        /// <summary>Resolve a gear <c>triggerName</c> into combat or equip channel bundles.</summary>
        public static void ApplyTriggerNameToLists(
            string? triggerName,
            out List<ActionTriggerBundle>? triggerBundles,
            out List<ActionTriggerBundle>? equipEffects)
        {
            triggerBundles = null;
            equipEffects = null;
            if (!TryGetByName(triggerName, out var identity))
                return;
            var bundle = identity.ToBundle();
            var list = new List<ActionTriggerBundle> { bundle };
            if (identity.IsEquipEffect)
                equipEffects = list;
            else
                triggerBundles = list;
        }

        public static void ClearCache()
        {
            lock (Gate)
            {
                _cache = null;
                _byName = null;
            }
        }

        public static void Reload()
        {
            ClearCache();
            EnsureLoaded();
        }

        /// <summary>
        /// Writes seed identities to <c>Triggers.json</c> when the file is missing or <paramref name="forceOverwrite"/> is true.
        /// </summary>
        public static string EnsureTriggersJsonFromSeed(bool forceOverwrite = false, string? gameDataDirectory = null)
        {
            string dir = gameDataDirectory
                ?? Path.GetDirectoryName(JsonLoader.FindGameDataFile(GameConstants.WeaponsJson)!)
                ?? Path.GetDirectoryName(GameConstants.GetGameDataFilePath(GameConstants.TriggersJson))
                ?? throw new InvalidOperationException("GameData directory not found.");

            string path = Path.Combine(dir, GameConstants.TriggersJson);
            if (File.Exists(path) && !forceOverwrite)
                return path;

            var rows = ItemTriggerIdentityCatalog.BuildSeedRows();
            var writeOpts = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            string json = JsonSerializer.Serialize(rows, writeOpts) + System.Environment.NewLine;
            File.WriteAllText(path, json);
            ClearCache();
            return path;
        }

        private static void EnsureLoaded()
        {
            lock (Gate)
            {
                if (_cache != null)
                    return;

                string? path = JsonLoader.FindGameDataFile(GameConstants.TriggersJson);
                List<TriggerIdentityData> list;
                if (path != null && File.Exists(path))
                {
                    string text = File.ReadAllText(path);
                    list = JsonSerializer.Deserialize<List<TriggerIdentityData>>(text, ReadOptions)
                           ?? new List<TriggerIdentityData>();
                }
                else
                {
                    list = ItemTriggerIdentityCatalog.BuildSeedRows();
                }

                list = list
                    .Where(r => r != null && !string.IsNullOrWhiteSpace(r.Name))
                    .OrderBy(r => r.Id)
                    .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                _cache = list;
                _byName = new Dictionary<string, TriggerIdentityData>(StringComparer.OrdinalIgnoreCase);
                foreach (var row in list)
                {
                    string key = row.Name.Trim();
                    if (!_byName.ContainsKey(key))
                        _byName[key] = row;
                }
            }
        }
    }
}
