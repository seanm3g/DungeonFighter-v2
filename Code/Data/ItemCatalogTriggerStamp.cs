using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>
    /// Stamps <c>triggerName</c> onto every Weapons.json / Armor.json row from
    /// <see cref="TriggersLoader"/> (<c>index % Count</c>, weapons then armor).
    /// Removes nested <c>triggerBundles</c> / <c>equipEffects</c> (resolved at load from Triggers.json).
    /// </summary>
    public static class ItemCatalogTriggerStamp
    {
        private static readonly JsonSerializerOptions WriteOptions = new()
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        /// <summary>
        /// Ensures Triggers.json exists, then rewrites Weapons.json and Armor.json in place.
        /// Returns (weaponsStamped, armorStamped).
        /// </summary>
        public static (int Weapons, int Armor) StampGameDataFiles(string? gameDataDirectory = null)
        {
            string dir = gameDataDirectory
                ?? Path.GetDirectoryName(JsonLoader.FindGameDataFile("Weapons.json")!)
                ?? throw new InvalidOperationException("GameData directory not found.");

            TriggersLoader.EnsureTriggersJsonFromSeed(forceOverwrite: false, gameDataDirectory: dir);
            TriggersLoader.ClearCache();

            string weaponsPath = Path.Combine(dir, "Weapons.json");
            string armorPath = Path.Combine(dir, "Armor.json");
            if (!File.Exists(weaponsPath) || !File.Exists(armorPath))
                throw new FileNotFoundException("Weapons.json or Armor.json missing under " + dir);

            int w = StampArrayFile(weaponsPath, startingIndex: 0);
            int a = StampArrayFile(armorPath, startingIndex: w);
            return (w, a);
        }

        private static int StampArrayFile(string path, int startingIndex)
        {
            string json = File.ReadAllText(path);
            var root = JsonNode.Parse(json) as JsonArray
                ?? throw new InvalidOperationException("Expected JSON array: " + path);

            int index = startingIndex;
            for (int i = 0; i < root.Count; i++)
            {
                if (root[i] is not JsonObject obj)
                    continue;
                var identity = ItemTriggerIdentityCatalog.Get(index);
                obj["triggerName"] = identity.Name;
                obj.Remove("triggerBundles");
                obj.Remove("equipEffects");
                index++;
            }

            File.WriteAllText(path, root.ToJsonString(WriteOptions) + System.Environment.NewLine);
            return index - startingIndex;
        }
    }
}
