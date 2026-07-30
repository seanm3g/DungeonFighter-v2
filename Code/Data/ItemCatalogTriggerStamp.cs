using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>
    /// Clears <c>triggerName</c> / nested trigger blobs from Weapons.json and Armor.json
    /// so combat procs come from animal StatBonus suffixes instead of base catalog stamps.
    /// </summary>
    public static class ItemCatalogTriggerStamp
    {
        private static readonly JsonSerializerOptions WriteOptions = new()
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        /// <summary>
        /// Clears base-gear trigger stamps in GameData. Returns (weaponsCleared, armorCleared).
        /// </summary>
        public static (int Weapons, int Armor) StampGameDataFiles(string? gameDataDirectory = null)
        {
            string dir = gameDataDirectory
                ?? Path.GetDirectoryName(JsonLoader.FindGameDataFile("Weapons.json")!)
                ?? throw new InvalidOperationException("GameData directory not found.");

            string weaponsPath = Path.Combine(dir, "Weapons.json");
            string armorPath = Path.Combine(dir, "Armor.json");
            if (!File.Exists(weaponsPath) || !File.Exists(armorPath))
                throw new FileNotFoundException("Weapons.json or Armor.json missing under " + dir);

            int w = ClearArrayFile(weaponsPath);
            int a = ClearArrayFile(armorPath);
            return (w, a);
        }

        private static int ClearArrayFile(string path)
        {
            string json = File.ReadAllText(path);
            var root = JsonNode.Parse(json) as JsonArray
                ?? throw new InvalidOperationException("Expected JSON array: " + path);

            int cleared = 0;
            for (int i = 0; i < root.Count; i++)
            {
                if (root[i] is not JsonObject obj)
                    continue;
                obj["triggerName"] = "";
                obj.Remove("triggerBundles");
                obj.Remove("equipEffects");
                cleared++;
            }

            File.WriteAllText(path, root.ToJsonString(WriteOptions) + System.Environment.NewLine);
            return cleared;
        }
    }
}
