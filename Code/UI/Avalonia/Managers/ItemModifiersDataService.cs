using RPGGame.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Handles data persistence operations for item modifiers.
    /// Extracted from ItemModifiersTabManager to improve Single Responsibility Principle compliance.
    /// </summary>
    public class ItemModifiersDataService
    {
        private readonly Action<string, bool>? showStatusMessage;
        
        public ItemModifiersDataService(Action<string, bool>? showStatusMessage = null)
        {
            this.showStatusMessage = showStatusMessage;
        }
        
        /// <summary>
        /// Load modifications from Modifications.json
        /// </summary>
        public List<ModificationData> LoadModifications()
        {
            try
            {
                string? filePath = JsonLoader.FindGameDataFile("Modifications.json");
                if (filePath != null && File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    return JsonSerializer.Deserialize<List<ModificationData>>(json, options) ?? new List<ModificationData>();
                }
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Error loading modifications: {ex.Message}", false);
            }

            return new List<ModificationData>();
        }
        
        /// <summary>
        /// Get available rarities from RarityTable.json
        /// </summary>
        public List<string> GetAvailableRarities()
        {
            try
            {
                string? filePath = JsonLoader.FindGameDataFile("RarityTable.json");
                if (filePath != null && File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    var rarityData = JsonSerializer.Deserialize<List<RarityData>>(json, options) ?? new List<RarityData>();
                    return rarityData.Select(r => r.Name?.Trim() ?? "").Where(n => !string.IsNullOrEmpty(n)).ToList();
                }
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Error loading rarity table: {ex.Message}", false);
            }

            // Fallback to default rarities
            return new List<string> { "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic", "Transcendent" };
        }
        
        /// <summary>
        /// Save modifier rarity assignments back to Modifications.json
        /// </summary>
        public void SaveModifierRarities(List<ModificationData> modifiers)
        {
            try
            {
                string? filePath = JsonLoader.FindGameDataFile("Modifications.json");
                if (filePath != null)
                {
                    // Use the same format as the original file (PascalCase property names)
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = null // Use PascalCase to match original format
                    };
                    string json = JsonSerializer.Serialize(modifiers, options);
                    File.WriteAllText(filePath, json);
                    
                    showStatusMessage?.Invoke("Modifiers saved successfully", true);
                }
                else
                {
                    showStatusMessage?.Invoke("Could not find Modifications.json file", false);
                }
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Error saving modifiers: {ex.Message}", false);
            }
        }
        
        /// <summary>
        /// Data class for loading modification JSON
        /// </summary>
        public class ModificationData
        {
            [JsonPropertyName("DiceResult")]
            public int DiceResult { get; set; }

            [JsonPropertyName("ItemRank")]
            public string? ItemRank { get; set; }

            [JsonPropertyName("Name")]
            public string? Name { get; set; }

            [JsonPropertyName("Description")]
            public string? Description { get; set; }

            [JsonPropertyName("Effect")]
            public string? Effect { get; set; }

            [JsonPropertyName("MinValue")]
            public double MinValue { get; set; }

            [JsonPropertyName("MaxValue")]
            public double MaxValue { get; set; }
        }

        /// <summary>
        /// Data class for loading rarity JSON
        /// </summary>
        private class RarityData
        {
            [JsonPropertyName("Name")]
            public string? Name { get; set; }

            [JsonPropertyName("Weight")]
            public double Weight { get; set; }
        }
    }
}
