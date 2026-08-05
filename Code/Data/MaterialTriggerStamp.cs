using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>
    /// Merges material-owned trigger identities into <c>Triggers.json</c>.
    /// Supersedes main's <c>AnimalSuffixTriggerStamp</c>.
    /// </summary>
    public static class MaterialTriggerStamp
    {
        private static readonly JsonSerializerOptions WriteOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        /// <summary>Returns count of material identities written/updated in Triggers.json.</summary>
        public static int StampGameDataFiles(string? gameDataDirectory = null)
        {
            string dir = gameDataDirectory
                ?? Path.GetDirectoryName(JsonLoader.FindGameDataFile("Triggers.json")!)
                ?? throw new InvalidOperationException("GameData directory not found.");

            int triggers = MergeTriggers(Path.Combine(dir, "Triggers.json"));
            TriggersLoader.ClearCache();
            return triggers;
        }

        private static int MergeTriggers(string path)
        {
            var existing = new List<TriggerIdentityData>();
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                existing = JsonSerializer.Deserialize<List<TriggerIdentityData>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<TriggerIdentityData>();
            }

            var byName = new Dictionary<string, TriggerIdentityData>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in existing)
            {
                if (row == null || string.IsNullOrWhiteSpace(row.Name))
                    continue;
                byName[row.Name.Trim()] = row;
            }

            int nextId = existing.Count == 0 ? 0 : existing.Max(r => r.Id) + 1;
            int touched = 0;

            foreach (var def in MaterialTriggerCatalog.All)
            {
                var identity = new TriggerIdentityData
                {
                    Id = byName.TryGetValue(def.TriggerName, out var old) ? old.Id : nextId++,
                    Name = def.TriggerName,
                    Description = def.Description,
                    When = def.When,
                    Count = "1",
                    Scope = def.Scope ?? "",
                    Mechanics = def.Mechanics,
                    Value = def.Value,
                    Filters = def.Filters,
                    Channel = string.IsNullOrWhiteSpace(def.Channel) ? "combat" : def.Channel
                };

                byName[def.TriggerName] = identity;
                touched++;
            }

            // Drop obsolete *Suffix animal identities so merge from main prefers material ownership.
            var kept = byName.Values
                .Where(r => r != null && !string.IsNullOrWhiteSpace(r.Name))
                .Where(r => !r.Name.EndsWith("Suffix", StringComparison.OrdinalIgnoreCase))
                .Where(r => !IsTaxonSynergyName(r.Name))
                .OrderBy(r => r.Id)
                .ToList();

            // Re-assign sequential ids for stability
            for (int i = 0; i < kept.Count; i++)
                kept[i].Id = i;

            File.WriteAllText(path, JsonSerializer.Serialize(kept, WriteOptions) + System.Environment.NewLine);
            return touched;
        }

        private static bool IsTaxonSynergyName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;
            string n = name.Trim();
            return n.EndsWith("SetFrom", StringComparison.OrdinalIgnoreCase)
                   || n.EndsWith("AmpTo", StringComparison.OrdinalIgnoreCase);
        }
    }
}
