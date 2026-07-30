using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace RPGGame.Data
{
    /// <summary>
    /// Writes animal suffix trigger identities + taxon synergies into Triggers.json,
    /// and rewrites of-the-* StatBonuses rows to trigger-only + taxon tags.
    /// </summary>
    public static class AnimalSuffixTriggerStamp
    {
        private static readonly JsonSerializerOptions WriteOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        /// <summary>Returns (triggersAddedOrUpdated, suffixesRewritten).</summary>
        public static (int Triggers, int Suffixes) StampGameDataFiles(string? gameDataDirectory = null)
        {
            string dir = gameDataDirectory
                ?? Path.GetDirectoryName(JsonLoader.FindGameDataFile("Triggers.json")!)
                ?? throw new InvalidOperationException("GameData directory not found.");

            int triggers = MergeTriggers(Path.Combine(dir, "Triggers.json"));
            TriggersLoader.ClearCache();
            int suffixes = RewriteStatBonuses(Path.Combine(dir, "StatBonuses.json"));
            try
            {
                JsonLoader.ClearCache();
            }
            catch
            {
                // optional; older JsonLoader may not expose ClearCache
            }
            return (triggers, suffixes);
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

            foreach (var def in AnimalSuffixCatalog.All)
            {
                var identity = new TriggerIdentityData
                {
                    Id = byName.TryGetValue(def.TriggerName, out var old) ? old.Id : nextId++,
                    Name = def.TriggerName,
                    Description = def.Description,
                    EffectTarget = def.EffectTarget,
                    When = def.When,
                    Count = "1",
                    Scope = def.Scope ?? "",
                    Mechanics = def.Mechanics,
                    Value = def.Value,
                    Filters = def.Filters,
                    Channel = def.Channel
                };
                if (!byName.ContainsKey(def.TriggerName))
                    nextId = Math.Max(nextId, identity.Id + 1);
                byName[def.TriggerName] = identity;
                touched++;
            }

            foreach (var syn in AnimalSuffixCatalog.BuildTaxonSynergies(nextId))
            {
                if (byName.TryGetValue(syn.Name, out var old))
                    syn.Id = old.Id;
                byName[syn.Name] = syn;
                touched++;
            }

            var ordered = byName.Values.OrderBy(r => r.Id).ToList();
            // Re-pack ids contiguous for sheet friendliness
            for (int i = 0; i < ordered.Count; i++)
                ordered[i].Id = i;

            File.WriteAllText(path, JsonSerializer.Serialize(ordered, WriteOptions) + System.Environment.NewLine);
            return touched;
        }

        private static int RewriteStatBonuses(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("StatBonuses.json missing", path);

            string json = File.ReadAllText(path);
            var root = JsonNode.Parse(json) as JsonArray
                ?? throw new InvalidOperationException("Expected JSON array: StatBonuses.json");

            var bySuffix = AnimalSuffixCatalog.All.ToDictionary(
                d => d.SuffixName,
                d => d,
                StringComparer.OrdinalIgnoreCase);

            int rewritten = 0;
            for (int i = 0; i < root.Count; i++)
            {
                if (root[i] is not JsonObject obj)
                    continue;
                string name = obj["Name"]?.GetValue<string>() ?? obj["name"]?.GetValue<string>() ?? "";
                if (!bySuffix.TryGetValue(name, out var def))
                    continue;

                obj["Description"] = def.Description;
                obj["Value"] = 0;
                obj["StatType"] = "";
                obj["Mechanics"] = new JsonArray();
                obj["triggerName"] = def.TriggerName;
                obj["tags"] = new JsonArray(def.Taxon);
                // Taxon synergies auto-merge from tags via StatBonusTriggerMerge
                obj.Remove("triggerNames");
                rewritten++;
            }

            File.WriteAllText(path, root.ToJsonString(WriteOptions) + System.Environment.NewLine);
            return rewritten;
        }
    }
}
