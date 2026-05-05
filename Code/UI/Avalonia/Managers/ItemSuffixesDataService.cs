using RPGGame;
using RPGGame.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>Loads and saves <see cref="StatBonus"/> rows for the Item Suffixes settings tab (<c>StatBonuses.json</c>).</summary>
    public class ItemSuffixesDataService
    {
        private readonly Action<string, bool>? showStatusMessage;

        public ItemSuffixesDataService(Action<string, bool>? showStatusMessage = null)
        {
            this.showStatusMessage = showStatusMessage;
        }

        public List<StatBonus> LoadStatBonuses()
        {
            try
            {
                string? filePath = JsonLoader.FindGameDataFile(GameConstants.StatBonusesJson);
                if (filePath != null && File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    json = GameDataJsonNormalizer.NormalizeForGameDataFile(GameConstants.StatBonusesJson, json);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    return JsonSerializer.Deserialize<List<StatBonus>>(json, options) ?? new List<StatBonus>();
                }
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Error loading stat bonuses: {ex.Message}", false);
            }

            return new List<StatBonus>();
        }

        public List<string> GetAvailableRarities()
        {
            try
            {
                string? filePath = JsonLoader.FindGameDataFile("RarityTable.json");
                if (filePath != null && File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var rarityData = JsonSerializer.Deserialize<List<RarityRow>>(json, options) ?? new List<RarityRow>();
                    return rarityData.Select(r => r.Name?.Trim() ?? "").Where(n => !string.IsNullOrEmpty(n)).ToList();
                }
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Error loading rarity table: {ex.Message}", false);
            }

            return new List<string> { "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic" };
        }

        public void SaveStatBonuses(List<StatBonus> bonuses)
        {
            try
            {
                string? filePath = JsonLoader.FindGameDataFile(GameConstants.StatBonusesJson);
                if (filePath != null)
                {
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = null
                    };
                    string json = JsonSerializer.Serialize(bonuses, options);
                    File.WriteAllText(filePath, json);
                    showStatusMessage?.Invoke("Suffixes saved successfully", true);
                }
                else
                {
                    showStatusMessage?.Invoke("Could not find StatBonuses.json", false);
                }
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Error saving stat bonuses: {ex.Message}", false);
            }
        }

        private class RarityRow
        {
            [JsonPropertyName("Name")]
            public string? Name { get; set; }

            [JsonPropertyName("Weight")]
            public double Weight { get; set; }
        }
    }
}
